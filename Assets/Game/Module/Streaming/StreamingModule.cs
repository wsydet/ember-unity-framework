using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Ember.Scene;
using UnityEngine;

namespace Game.Module
{
    /// <summary>
    /// 无缝流送模块 —— 场景分块、触发器驱动、方向感知、分帧加载/卸载。
    ///
    /// <b>核心特性：</b>
    /// - 拓扑图描述场景区块连接与方向
    /// - 方向感知触发器只加载玩家前进方向上的邻居场景
    /// - 加载 / 激活 / 卸载分离，分帧执行避免卡顿
    /// - 基于拓扑距离的卸载策略保持内存稳定
    ///
    /// <b>定位：</b>
    /// 作为 <see cref="IEmberModule"/>（Phase = <see cref="ModulePhase.Gameplay"/>）叠加在框架之上，
    /// 分块流送统一走 <see cref="EmberSceneManager"/> 的静默加载/卸载方法
    /// （不广播事件、不受单加载锁限制），不触碰状态机的大场景切换链路。
    ///
    /// <b>启用：</b>
    /// 本模块 <see cref="Enabled"/> 默认 false（关闭）。启用时改为返回 true，
    /// 进入 Gameplay 后调用 <c>StreamingModule.Instance.Initialize(拓扑资产)</c> 启动流送。
    /// </summary>
    public class StreamingModule : EmberSingleton<StreamingModule>, IEmberModule, IEmberUpdate
    {
        private const string TAG = LogTags.Game + "." + nameof(StreamingModule);

        /// <summary>模块是否启用。默认关闭，需要时改为返回 true。</summary>
        public bool Enabled => false;

        public int Phase => ModulePhase.Gameplay;

        #region 内部参数

        // ---- 配置（可在 Initialize 前后调整） ----

        /// <summary>同时进行的异步加载数（避免 IO 峰值）</summary>
        public int MaxConcurrentLoads = 1;

        /// <summary>每帧激活的根物体数量（分帧激活）</summary>
        public int RootActivatePerFrame = 10;

        /// <summary>延迟卸载时间（秒），期间玩家折返可取消卸载</summary>
        public float UnloadDelay = 5f;

        /// <summary>保留的拓扑距离，卸载距离更远的场景</summary>
        public int MaxTopologyDistance = 2;

        // ---- 拓扑运行时 ----

        private SceneTopology _topology;
        private bool _isInitialized;

        // ---- 场景状态 ----

        private readonly HashSet<string> _loadedScenes = new();          // 已加载（可能未激活）
        private readonly HashSet<string> _loadingScenes = new();         // 正在异步加载
        private readonly HashSet<string> _activatedScenes = new();       // 已激活

        // ---- 队列 ----

        private readonly PriorityQueue<LoadRequest> _loadQueue = new();
        private readonly Queue<GameObject> _pendingRootActivation = new();
        private readonly Queue<string> _unloadQueue = new();

        // ---- 内部状态 ----

        private string _currentSceneId;   // 玩家当前所在场景

        private struct LoadRequest
        {
            public string SceneId;
        }

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberModule.OnInit() { }

        void IEmberModule.OnDestroy()
        {
            ClearAll();
        }

        void IEmberModule.ResetModuleData()
        {
            ClearAll();
        }

        #endregion

        // ============================================================

        #region 内部方法

        void IEmberUpdate.Update()
        {
            if (!_isInitialized) return;

            ProcessLoadQueue();
            ProcessPendingActivation();
            ProcessUnloadQueue();
        }

        // ======== 加载 ========

        /// <summary>请求加载场景（幂等：已加载或加载中则跳过）。</summary>
        private void RequestLoad(string sceneId, float priority)
        {
            if (_loadedScenes.Contains(sceneId) || _loadingScenes.Contains(sceneId))
                return;

            RemoveFromUnloadQueue(sceneId);

            _loadQueue.Enqueue(new LoadRequest { SceneId = sceneId }, priority);
        }

        private void ProcessLoadQueue()
        {
            if (_loadQueue.Count == 0) return;
            if (_loadingScenes.Count >= MaxConcurrentLoads) return;

            LoadRequest request = _loadQueue.Dequeue();
            LoadSceneAsync(request.SceneId).Forget();
        }

        /// <summary>
        /// 静默加载场景，加载完成后禁用根物体，等待触发器驱动的分帧激活。
        /// 推荐分块场景的根物体在编辑器里默认非激活，避免加载瞬间闪烁。
        /// </summary>
        private async UniTask LoadSceneAsync(string sceneId)
        {
            _loadingScenes.Add(sceneId);

            await EmberSceneManager.Instance.LoadSceneSilentlyAsync(sceneId);

            // 禁用根物体，等待 Start/Center 触发器驱动分帧激活
            var roots = EmberSceneManager.Instance.GetSceneRoots(sceneId);
            foreach (var root in roots)
                root.SetActive(false);

            _loadingScenes.Remove(sceneId);
            _loadedScenes.Add(sceneId);
        }

        // ======== 激活 ========

        private void ActivateScene(string sceneId)
        {
            if (!_loadedScenes.Contains(sceneId)) return;
            ActivateSceneRoots(sceneId);
            _activatedScenes.Add(sceneId);
        }

        private void ActivateSceneRoots(string sceneId)
        {
            var roots = EmberSceneManager.Instance.GetSceneRoots(sceneId);
            foreach (var root in roots)
            {
                if (!root.activeSelf)
                    _pendingRootActivation.Enqueue(root);
            }
        }

        private void ProcessPendingActivation()
        {
            int count = 0;
            while (_pendingRootActivation.Count > 0 && count < RootActivatePerFrame)
            {
                var root = _pendingRootActivation.Dequeue();
                if (root != null)
                    root.SetActive(true);
                count++;
            }
        }

        /// <summary>等待仍在加载中的场景完成后激活（极端情况：启动触发器先于加载完成触发）。</summary>
        private async UniTask WaitAndActivate(string sceneId)
        {
            while (_loadingScenes.Contains(sceneId))
                await UniTask.Yield(PlayerLoopTiming.Update);

            if (_loadedScenes.Contains(sceneId))
                ActivateScene(sceneId);
        }

        // ======== 卸载 ========

        /// <summary>基于拓扑距离卸载：距离大于等于 MaxTopologyDistance 或不可达的场景入卸载队列。</summary>
        private void UnloadDistantScenes()
        {
            if (string.IsNullOrEmpty(_currentSceneId)) return;

            var distances = ComputeDistances(_currentSceneId, MaxTopologyDistance);

            foreach (string loadedId in _loadedScenes)
            {
                if (!distances.ContainsKey(loadedId) || distances[loadedId] >= MaxTopologyDistance)
                {
                    if (!_unloadQueue.Contains(loadedId))
                        _unloadQueue.Enqueue(loadedId);
                }
            }
        }

        /// <summary>从当前场景 BFS 计算拓扑距离（深度限制 maxDepth）。</summary>
        private Dictionary<string, int> ComputeDistances(string startSceneId, int maxDepth)
        {
            var distances = new Dictionary<string, int>();
            var queue = new Queue<string>();

            distances[startSceneId] = 0;
            queue.Enqueue(startSceneId);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                int dist = distances[current];
                if (dist >= maxDepth) continue;

                foreach (var edge in _topology.GetOutgoingEdges(current))
                {
                    if (!distances.ContainsKey(edge.toNodeId))
                    {
                        distances[edge.toNodeId] = dist + 1;
                        queue.Enqueue(edge.toNodeId);
                    }
                }
            }

            return distances;
        }

        private void ProcessUnloadQueue()
        {
            if (_unloadQueue.Count == 0) return;

            string sceneId = _unloadQueue.Dequeue();
            if (!_loadedScenes.Contains(sceneId)) return;

            UnloadSceneAsync(sceneId).Forget();
        }

        private async UniTask UnloadSceneAsync(string sceneId)
        {
            if (UnloadDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(UnloadDelay));

            if (!_loadedScenes.Contains(sceneId))
                return;

            await EmberSceneManager.Instance.UnloadSceneSilentlyAsync(sceneId);

            _loadedScenes.Remove(sceneId);
            _activatedScenes.Remove(sceneId);
        }

        /// <summary>从卸载队列移除指定场景（玩家折返时取消卸载）。</summary>
        private void RemoveFromUnloadQueue(string sceneId)
        {
            var kept = new Queue<string>();
            while (_unloadQueue.Count > 0)
            {
                string id = _unloadQueue.Dequeue();
                if (id != sceneId)
                    kept.Enqueue(id);
            }

            while (kept.Count > 0)
                _unloadQueue.Enqueue(kept.Dequeue());
        }

        /// <summary>清空全部状态并卸载所有已加载的流送场景（退出玩法 / 热重启时调用）。</summary>
        private void ClearAll()
        {
            foreach (string sceneId in _loadedScenes)
                EmberSceneManager.Instance.UnloadSceneSilentlyAsync(sceneId).Forget();

            _loadedScenes.Clear();
            _loadingScenes.Clear();
            _activatedScenes.Clear();
            _loadQueue.Clear();
            _pendingRootActivation.Clear();
            _unloadQueue.Clear();

            _currentSceneId = null;
            _topology = null;
            _isInitialized = false;
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 用拓扑资产初始化流送系统。必须在进入玩法后调用一次。
        /// </summary>
        public void Initialize(SceneTopologyAsset asset)
        {
            if (asset == null)
            {
                EmberDebug.LogError(TAG, "Streaming: initialize failed, topology asset is null.");
                return;
            }

            _topology = asset.CreateRuntimeTopology();
            _isInitialized = true;
            EmberDebug.LogInit(TAG, $"Streaming initialized (topology asset: {asset.name}).");
        }

        /// <summary>边缘预加载触发器进入：加载玩家前进方向上的邻居场景。</summary>
        public void OnPreloadTriggerEntered(StreamingPreloadTrigger trigger)
        {
            if (!_isInitialized) return;

            Vector3 triggerDir = trigger.direction.ToVector3();
            foreach (var edge in _topology.GetOutgoingEdges(trigger.ownerSceneId))
            {
                float dot = Vector3.Dot(edge.direction.normalized, triggerDir.normalized);
                if (dot > 0.5f)  // 夹角 < 60°
                {
                    float priority = 5f + dot * 5f;   // 方向越匹配优先级越高
                    RequestLoad(edge.toNodeId, priority);
                }
            }
        }

        /// <summary>边缘启动触发器进入：激活目标场景，更新当前场景，触发卸载检查。</summary>
        public void OnStartTriggerEntered(StreamingStartTrigger trigger)
        {
            if (!_isInitialized) return;

            string target = trigger.targetSceneId;
            if (_loadedScenes.Contains(target))
            {
                ActivateScene(target);
            }
            else
            {
                WaitAndActivate(target).Forget();
            }

            _currentSceneId = target;
            UnloadDistantScenes();
        }

        /// <summary>中心激活触发器进入：激活当前场景剩余根物体（大场景模式）。</summary>
        public void OnCenterTriggerEntered(string sceneId)
        {
            if (!_isInitialized) return;
            if (_activatedScenes.Contains(sceneId)) return;
            ActivateSceneRoots(sceneId);
        }

        #endregion
    }
}
