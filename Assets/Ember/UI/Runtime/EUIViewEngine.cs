// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Core;
using Ember.Basic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 视图引擎（框架内部）。
    ///
    /// 管理 Canvas 层级、页面生命周期状态机、排序层级计算、延迟销毁池、
    /// Frame Time Budget 等底层机制。开发者不应直接调用此类。
    ///
    /// <para>开发者入口为 <see cref="EUIManager"/>（Show / Close / Preload 等 API）。</para>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.UI)]
    public class EUIViewEngine : EmberMonoSingleton<EUIViewEngine>, IEmberManager
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        // Canvas 层管理
        private readonly Dictionary<int, Canvas> _layerCanvases = new();
        private Transform _uiRoot;
        private Camera _uiCamera;

        // 页面追踪
        private readonly List<EUIPage> _activePages = new();
        private readonly Queue<Action> _pendingOperations = new();
        private readonly Queue<Action> _nextFrameCallbacks = new();
        private bool _initialized;

        // 上下文
        private EUIPageContext _pageContext;
        private EUIBgMaskPool _bgMaskPool;

        // 延迟销毁中的页面（key = PrefabPath，对标 Burner GamePage.isClosing）
        private readonly Dictionary<string, EUIPage> _closingPages = new();

        // 资源 & 过渡
        private IEUIResourceProvider _resourceProvider;
        private IEUITransitionHandler _transitionHandler;

        // Frame Time Budget（对标 Burner MaximalFrameTimeBudget = 500ms）
        // 默认 100ms，平衡响应性与吞吐量。可在运行时调整。
        private int _timeBudgetMs = 100;
        private readonly System.Diagnostics.Stopwatch _loadTimer = new System.Diagnostics.Stopwatch();

        // CanvasScaler 自适应（对标 Burner AutoAdjustCanvasScaler）
        private bool _autoAdjustCanvasScaler = true;
        private Vector2Int _lastScreenResolution;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
        }

        private void Update()
        {
            Profiler.BeginSample("EUIViewEngine.Update");
            ProcessPendingOperations();
            ProcessNextFrameCallbacks();
            ProcessClosingPages();
            CheckScreenResolution();
            Profiler.EndSample();
        }

        private void LateUpdate()
        {
            // 如果活跃页面需要 LateUpdate 驱动
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        // ── 属性 ──

        /// <summary>页面上下文（MainPage + Popup 栈关系）</summary>
        public EUIPageContext PageContext => _pageContext;

        /// <summary>资源加载提供者</summary>
        public IEUIResourceProvider ResourceProvider
        {
            get => _resourceProvider;
            set => _resourceProvider = value;
        }

        /// <summary>过渡动画处理器</summary>
        public IEUITransitionHandler TransitionHandler
        {
            get => _transitionHandler;
            set => _transitionHandler = value;
        }

        /// <summary>UI 根节点</summary>
        public Transform UIRoot => _uiRoot;

        /// <summary>UI 相机</summary>
        public Camera UICamera => _uiCamera;

        /// <summary>
        /// 每帧用于处理页面操作的最大时间预算（毫秒），默认 100ms。
        /// 对标 Burner MaximalFrameTimeBudget（500ms），可运行时调整。
        /// 增大可减少页面打开的延迟帧数，减小可保证帧率。
        /// </summary>
        public int FrameTimeBudgetMs
        {
            get => _timeBudgetMs;
            set => _timeBudgetMs = Math.Max(1, value);
        }

        /// <summary>
        /// 是否自动调整 CanvasScaler.matchWidthOrHeight（屏幕适配）。
        /// 默认 true。设为 false 则跳过自适应逻辑。
        /// 对标 Burner AutoAdjustCanvasScaler。
        /// </summary>
        public bool AutoAdjustCanvasScaler
        {
            get => _autoAdjustCanvasScaler;
            set => _autoAdjustCanvasScaler = value;
        }

        // ── IEmberManager ──

        void IEmberManager.Init()
        {
            if (_initialized) return;

            var launcher = GameLauncher.Instance;
            _uiRoot = launcher.UIRoot?.transform;
            _uiCamera = launcher.UICamera;

            if (_uiRoot == null)
            {
                EmberDebug.LogError(TAG, "UIRoot 为空，EUIViewEngine 无法初始化。");
                return;
            }

            // 初始化时隐藏 UIRoot 下所有子节点（编辑时放的预览节点）
            // 但跳过标记了 IEUIPersistentUI 的持久元素（如 BootSplash）
            foreach (Transform child in _uiRoot)
            {
                if (!child.TryGetComponent(out IEUIPersistentUI _))
                    child.gameObject.SetActive(false);
            }

            // 默认实现
            _resourceProvider ??= new DefaultUIResourceProvider();
            _transitionHandler ??= new DefaultUITransitionHandler();

            _pageContext = new EUIPageContext(this);
            _bgMaskPool = new EUIBgMaskPool(_uiRoot);

            _initialized = true;
            _lastScreenResolution = new Vector2Int(Screen.width, Screen.height);
            EmberEventBus.OnNext(EUIEvents.UIViewEngineReady);
            EmberDebug.LogInit(TAG, "EUIViewEngine 初始化完成。");
        }

        void IEmberManager.Destroy()
        {
            Shutdown();
        }

        // ── 页面生命周期（由 PageRouter 调用） ──

        /// <summary>
        /// 打开一个页面。完整流程：Init → PlayShow → [Opened]。
        /// </summary>
        /// <param name="page">已实例化的页面</param>
        /// <param name="pageDef">页面定义</param>
        /// <param name="args">传递给 Init 的参数</param>
        /// <param name="onComplete">完成回调</param>
        public void OpenPage(EUIPage page, EUIPageDef pageDef, object args, Action onComplete = null)
        {
            Profiler.BeginSample("EUIViewEngine.OpenPage");
            EnsureLayerCanvas(pageDef.Layer);

            page.EUIPageDef = pageDef;
            page.Transform.SetParent(_uiRoot, false);

            var canvas = page.Canvas;
            if (!canvas)
                canvas = page.GameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.overrideSorting = true;

            // 添加到追踪列表
            if (!_activePages.Contains(page))
                _activePages.Add(page);

            // Phase 1: Init
            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).Init(args);

                // 注入完成回调：动画真正结束时由 CompleteShow() 触发
                page.SetShowCallback(() =>
                {
                    EmberDebug.LogEvent(TAG, $"页面已打开: {pageDef}");
                    onComplete?.Invoke();

                    // 过渡完成 → 调度挂起操作（对标 Burner ExecutePendingOperationIfAny）
                    FlushPendingPageOp(page);
                }, args);

                // Phase 2: PlayShow
                EnqueuePageOperation(() =>
                {
                    ((IEUIView)page).PlayShow();
                    // NotifyOpened + 日志 + onComplete 现在在 EUIPage.CompleteShow() 中触发
                    // 不再使用 _nextFrameCallbacks（之前这里动画未完成就播报，是个 bug）
                });
            });
            Profiler.EndSample();
        }

        /// <summary>
        /// 关闭一个页面。流程：PlayHide → Cleanup → Destroy。
        /// </summary>
        public void ClosePage(EUIPage page, Action onComplete = null)
        {
            ClosePageInternal(page, onComplete);
        }

        internal void ClosePageInternal(EUIPage page, Action onComplete = null)
        {
            if (page == null) return;

            Profiler.BeginSample("EUIViewEngine.ClosePageInternal");

            // 页面处于过渡状态（Showing / Hiding）时，挂起 Close 操作
            // 对标 Burner PageTargetState
            if (page.TryQueuePendingOp(EUIPage.PagePendingOp.Close, onComplete))
            {
                Profiler.EndSample();
                return;
            }

            var pageDef = page.EUIPageDef;

            // 注入完成回调：动画真正结束时由 CompleteHide() 触发
            page.SetHideCallback(() =>
            {
                EmberDebug.LogCleanup(TAG, $"页面已关闭: {pageDef}");

                // Cleanup + 销毁调度
                _nextFrameCallbacks.Enqueue(() =>
                {
                    if (page != null)
                    {
                        ((IEUIView)page).Cleanup();
                        _activePages.Remove(page);

                        if (page.AutoDestroy && page.DestroyDelay > 0)
                        {
                            // 延迟销毁：隐藏 + 加入 _closingPages 等待复用或到期销毁
                            page.EnterClosingState();
                            var prefabPath = page.PrefabPath;
                            if (!string.IsNullOrEmpty(prefabPath))
                                _closingPages[prefabPath] = page;
                        }
                        else
                        {
                            // 立即销毁
                            if (page.GameObject != null)
                                Destroy(page.GameObject);
                        }
                        onComplete?.Invoke();
                    }
                });

                // 过渡完成 → 调度挂起操作（对标 Burner ExecutePendingOperationIfAny）
                FlushPendingPageOp(page);
            });

            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).PlayHide();
                // NotifyClosed + 日志现在在 EUIPage.CompleteHide() 中触发
                // Cleanup + Destroy 由上面的 SetHideCallback → _nextFrameCallbacks 调度
            });
            Profiler.EndSample();
        }

        /// <summary>
        /// 暂停一个页面。
        /// </summary>
        public void PausePage(EUIPage page)
        {
            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).OnPause();
                EUIObserver.NotifyPaused(page.EUIPageDef);
            });
        }

        /// <summary>
        /// 恢复一个页面。
        /// </summary>
        public void ResumePage(EUIPage page)
        {
            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).OnResume();
                EUIObserver.NotifyResumed(page.EUIPageDef);
            });
        }

        /// <summary>
        /// 重新打开已关闭的页面（OnReopen → PlayShow）。
        /// </summary>
        public void ReopenPage(EUIPage page, object args, Action onComplete = null)
        {
            // 页面处于过渡状态（Hiding）时，挂起 Reopen 操作
            // 对标 Burner PageTargetState
            if (page.TryQueuePendingOp(EUIPage.PagePendingOp.Reopen, args))
                return;

            // 注入完成回调：动画真正结束时由 CompleteShow() 触发
            page.SetShowCallback(() =>
            {
                EmberDebug.LogEvent(TAG, $"页面已重新打开: {page.EUIPageDef}");
                onComplete?.Invoke();

                // 过渡完成 → 调度挂起操作
                FlushPendingPageOp(page);
            }, args);

            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).OnReopen(args);
                EUIObserver.NotifyReopened(page.EUIPageDef, args);
                // OnReopen 内部会调用 PlayShow → CompleteShow → NotifyOpened + 日志 + onComplete
                // 不再使用 _nextFrameCallbacks（之前这里动画未完成就播报，且与 CompleteShow 重复播报）
            });
        }

        // ── 预加载 ──

        /// <summary>
        /// 预加载页面：执行 Init 但不执行 PlayShow。页面处于 Loaded 状态，
        /// 后续 <see cref="EUIPageRouter.ShowMainPage"/> 等调用时直接进入 PlayShow，跳过加载。
        /// 对标 Burner GamePage 预加载机制。
        /// </summary>
        internal void InitPageOnly(EUIPage page, EUIPageDef pageDef, object args, Action onComplete = null)
        {
            Profiler.BeginSample("EUIViewEngine.InitPageOnly");
            EnsureLayerCanvas(pageDef.Layer);

            page.EUIPageDef = pageDef;
            page.Transform.SetParent(_uiRoot, false);

            var canvas = page.Canvas;
            if (!canvas)
                canvas = page.GameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.overrideSorting = true;

            if (!_activePages.Contains(page))
                _activePages.Add(page);

            EnqueuePageOperation(() =>
            {
                ((IEUIView)page).Init(args);
                onComplete?.Invoke();
            });
            Profiler.EndSample();
        }

        // ── Canvas 层管理 ──

        /// <summary>
        /// 确保指定层级的 Canvas 已创建。
        /// </summary>
        public Canvas EnsureLayerCanvas(int layer)
        {
            if (_layerCanvases.TryGetValue(layer, out var canvas))
                return canvas;

            var go = new GameObject($"UI_Layer_{layer}");
            go.transform.SetParent(_uiRoot);

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.sortingOrder = layer;

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            _layerCanvases[layer] = canvas;
            return canvas;
        }

        // ── BG Mask ──

        /// <summary>显示背景遮罩</summary>
        public GameObject ShowBgMask(int sortingOrder, Action onClick)
        {
            return _bgMaskPool.Get(sortingOrder, onClick);
        }

        /// <summary>隐藏背景遮罩</summary>
        public void HideBgMask(GameObject mask)
        {
            _bgMaskPool.Return(mask);
        }

        // ── 安全操作队列 ──

        /// <summary>
        /// 将页面操作加入队列，避免在遍历 _activePages 时修改集合。
        /// </summary>
        public void EnqueuePageOperation(Action op)
        {
            _pendingOperations.Enqueue(op);
        }

        // ── 查询 ──

        /// <summary>活跃页面列表（运行时）</summary>
        public IReadOnlyList<EUIPage> ActivePages => _activePages;

        /// <summary>
        /// 驱动所有活跃页面的 OnUpdate（由 EUIManager 每帧调用）。
        /// 仅 NeedUpdate=true 的页面会被驱动。
        /// </summary>
        public void BroadcastPageUpdate()
        {
            for (int i = _activePages.Count - 1; i >= 0; i--)
                _activePages[i].Logic?.BroadcastUpdate();
        }

        /// <summary>按层级从高到低查找返回键处理者</summary>
        public bool HandleEscapeKey()
        {
            bool handled = false;
            _pageContext.ForEachVisiblePage(page =>
            {
                handled = ((IEUIView)page).TryEscapeKeyClose();
                return handled; // true = 停止遍历
            });
            return handled;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void ProcessPendingOperations()
        {
            if (_pendingOperations.Count == 0) return;

            Profiler.BeginSample("EUIViewEngine.ProcessPendingOperations");
            _loadTimer.Restart();

            while (_pendingOperations.Count > 0)
            {
                var op = _pendingOperations.Dequeue();
                op?.Invoke();

                if (_loadTimer.ElapsedMilliseconds > _timeBudgetMs && _pendingOperations.Count > 0)
                    break;
            }

            Profiler.EndSample();
        }

        private void ProcessNextFrameCallbacks()
        {
            if (_nextFrameCallbacks.Count == 0) return;

            var count = _nextFrameCallbacks.Count;
            for (int i = 0; i < count; i++)
            {
                _nextFrameCallbacks.Dequeue()?.Invoke();
            }
        }

        private void ProcessClosingPages()
        {
            if (_closingPages.Count == 0) return;

            var expired = new List<string>();
            foreach (var kv in _closingPages)
            {
                if (kv.Value.ShouldDisposeNow())
                {
                    kv.Value.ForceDispose();
                    expired.Add(kv.Key);
                }
            }

            foreach (var key in expired)
                _closingPages.Remove(key);
        }

        /// <summary>
        /// 查找可复用的延迟销毁页面。命中后自动退出 closing 状态。
        /// </summary>
        internal EUIPage FindReusablePage(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) return null;

            if (_closingPages.TryGetValue(prefabPath, out var page) && page.IsClosing)
            {
                _closingPages.Remove(prefabPath);
                page.CancelClosing();
                EmberDebug.Log(TAG, $"复用延迟销毁页面: {prefabPath}");
                return page;
            }
            return null;
        }

        /// <summary>
        /// 检测屏幕分辨率变化，若变化则对所有活跃页面的 CanvasScaler 重新适配。
        /// 对标 Burner AutoAdjustCanvasScaler + OnScreenResolutionChanged。
        /// </summary>
        private void CheckScreenResolution()
        {
            if (!_autoAdjustCanvasScaler) return;

            var current = new Vector2Int(Screen.width, Screen.height);
            if (current == _lastScreenResolution) return;

            _lastScreenResolution = current;
            EmberDebug.Log(TAG, $"屏幕分辨率变化: {current.x}x{current.y}，重新适配 CanvasScaler。");

            foreach (var page in _activePages)
            {
                if (page.GameObject)
                    AdjustCanvasScaler(page.GameObject);
            }
        }

        /// <summary>
        /// 动态调整 CanvasScaler 的 matchWidthOrHeight，根据当前屏幕宽高比与参考分辨率的关系自动切换匹配策略。
        /// 仅对 screenMatchMode = MatchWidthOrHeight 的 CanvasScaler 生效。
        /// 对标 Burner BurnerUIManager.AdjustCanvasScaler。
        /// </summary>
        [NoGC]
        public static void AdjustCanvasScaler(GameObject go)
        {
            if (!go) return;

            var scaler = go.GetComponent<CanvasScaler>();
            if (!scaler || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                return;

            float refWToH = scaler.referenceResolution.x / scaler.referenceResolution.y;
            float screenWToH = (float)Screen.width / Screen.height;

            // 横竖屏交叉：交换参考分辨率
            if ((screenWToH > 1f && refWToH < 1f) || (screenWToH < 1f && refWToH > 1f))
            {
                scaler.referenceResolution = new Vector2(scaler.referenceResolution.y, scaler.referenceResolution.x);
                refWToH = 1f / refWToH;
            }

            float match;
            if (Screen.width < Screen.height)
            {
                // 竖屏：比 9:16 更宽则匹配高度，否则匹配宽度
                match = screenWToH > (9f / 16f) ? 1f : 0f;
            }
            else
            {
                // 横屏：比 16:9 更宽则匹配高度，否则匹配宽度
                match = screenWToH > (16f / 9f) ? 1f : 0f;
            }

            scaler.matchWidthOrHeight = match;
        }

        /// <summary>
        /// 页面过渡完成后，检查并调度挂起的操作（对标 Burner ExecutePendingOperationIfAny）。
        /// 由 <see cref="OpenPage"/> 的 CompleteShow 回调和 <see cref="ClosePageInternal"/> 的 CompleteHide 回调调用。
        /// </summary>
        private void FlushPendingPageOp(EUIPage page)
        {
            if (page == null || page.PendingOp == EUIPage.PagePendingOp.None) return;

            var pendingOp = page.PendingOp;
            page.ClearPendingOp();

            EmberDebug.Log(TAG, $"调度挂起操作: {page.Name} op={pendingOp}");

            EnqueuePageOperation(() =>
            {
                if (pendingOp == EUIPage.PagePendingOp.Close)
                {
                    // 页面刚完成 Show（State=Opened），立即执行 Close
                    ClosePageInternal(page);
                }
                else if (pendingOp == EUIPage.PagePendingOp.Reopen)
                {
                    // 页面刚完成 Hide（State=Closed），立即执行 Reopen
                    // 注意：此时 page 已经 Cleanup，不能直接 Reopen，需要重新走 Open 流程
                    // 此处仅记录，实际 Reopen 由外部重新调用 ShowPage 触发
                    EmberDebug.LogWarning(TAG, $"页面 '{page.Name}' Reopen 挂起操作需要在外部重新触发 ShowPage。");
                }
            });
        }

        private void Shutdown()
        {
            _bgMaskPool?.Clear();
            _pageContext?.CloseAll();
            _activePages.Clear();
            _pendingOperations.Clear();
            _layerCanvases.Clear();

            // 强制清理所有延迟销毁的页面
            foreach (var page in _closingPages.Values)
                page.ForceDispose();
            _closingPages.Clear();

            EmberEventBus.OnNext(EUIEvents.UIViewEngineShutdown);
            EmberDebug.LogShutdown(TAG, "EUIViewEngine 已关闭。");
            _initialized = false;
        }

        protected override void OnDestroy()
        {
            Shutdown();
        }

        #endregion
    }
}
