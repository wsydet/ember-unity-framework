using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.Scene
{
    /// <summary>
    /// 场景管理器 —— 场景异步加载/卸载与过渡控制。
    ///
    /// 参考 burner 的 <c>GameSceneManager</c>，提取核心：
    /// - 异步加载场景（通过 EmberResourceManager 或 Unity SceneManager）
    /// - 激活前回调（场景加载完成后、激活前）
    /// - 场景过渡（加载新场景 → 卸载旧场景）
    /// - 广播生命周期事件（SceneLoaded / SceneUnloading）
    ///
    /// 使用方式：
    /// <code>
    /// // 加载场景
    /// EmberSceneManager.Instance.LoadSceneAsync("Battle", () => {
    ///     Debug.Log("战斗场景就绪");
    /// });
    ///
    /// // 过渡切换
    /// EmberSceneManager.Instance.TransitionTo("Battle", "MainMenu");
    /// </code>
    /// </summary>
    public class EmberSceneManager : EmberMonoSingleton<EmberSceneManager>
    {
        #region 参数

        /// <summary>当前活跃的场景名</summary>
        public string CurrentScene { get; private set; }

        /// <summary>是否正在加载场景中</summary>
        public bool IsLoading { get; private set; }

        /// <summary>真实加载进度（0.0 ~ 1.0），未加载时为 1.0</summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 展示用进度（0.0 ~ 1.0），经过平滑处理，适合 UI 绑定。
        /// 真实加载完成时约为 <see cref="_displayMaxRatio"/>，
        /// 随后在 <see cref="_smoothDuration"/> 秒内平滑过渡到 1.0。
        /// </summary>
        public float DisplayProgress { get; private set; }

        /// <summary>
        /// 真实加载完成时，展示进度映射到的比例（0.0 ~ 1.0），默认 0.6。
        /// 值越大，玩家看到的进度条"填充感"越强，但平滑收尾的余地越小。
        /// 调参建议：小场景 0.5 / 中型场景 0.6 / 大型场景 0.7。
        /// </summary>
        [SerializeField] private float _displayMaxRatio = 0.6f;

        /// <summary>
        /// 加载完成后，展示进度平滑过渡到 1.0 的时长（秒），默认 1.0。
        /// 调参建议：小场景 0.5s / 中型场景 1.0s / 大型场景 1.5s。
        /// </summary>
        [SerializeField] private float _smoothDuration = 1f;

        /// <summary>
        /// 场景加载完成、尚未激活时的回调。
        /// 在此阶段可以进行初始化操作（注册服务、加载资源等），
        /// 完成后调用 <c>activate</c> 激活场景。
        /// </summary>
        public event Action<UnityEngine.SceneManagement.Scene, Action> OnBeforeActivate;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 加载 ========

        /// <summary>
        /// 异步加载场景（Additive 模式，不会自动卸载当前场景）。
        ///
        /// 流程：
        /// 1. 调用 Unity SceneManager.LoadSceneAsync
        /// 2. 加载到 90% 时触发 OnBeforeActivate
        /// 3. 激活场景
        /// 4. 派发 SceneLoaded 事件
        /// </summary>
        /// <param name="sceneName">场景名（Build Settings 中的名称）</param>
        /// <param name="onComplete">完成回调</param>
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[Ember] EmberSceneManager is already loading a scene.");
                onComplete?.Invoke();
                return;
            }

            StartLoad(sceneName, LoadSceneMode.Additive, onComplete);
        }

        /// <summary>
        /// 异步加载场景并设为 Active（Single 模式，会卸载所有已加载的场景）。
        /// </summary>
        public void LoadSceneSingleAsync(string sceneName, Action onComplete = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[Ember] EmberSceneManager is already loading a scene.");
                onComplete?.Invoke();
                return;
            }

            StartLoad(sceneName, LoadSceneMode.Single, onComplete);
        }

        // ======== 卸载 ========

        /// <summary>
        /// 异步卸载场景。
        /// </summary>
        public void UnloadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                EmberEventBus.Dispatch(EmberBroadcastEvent.SceneUnloading);

                var op = SceneManager.UnloadSceneAsync(sceneName);
                op.completed += _ =>
                {
                    if (CurrentScene == sceneName)
                        CurrentScene = null;
                    onComplete?.Invoke();
                };
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        // ======== 过渡 ========

        /// <summary>
        /// 过渡到新场景：加载新场景 → 激活 → 卸载旧场景。
        /// </summary>
        /// <param name="newScene">新场景名</param>
        /// <param name="oldScene">要卸载的旧场景名（可选，null 则保留）</param>
        /// <param name="onComplete">完成回调</param>
        public void TransitionTo(string newScene, string oldScene = null, Action onComplete = null)
        {
            LoadSceneAsync(newScene, () =>
            {
                if (!string.IsNullOrEmpty(oldScene))
                {
                    UnloadSceneAsync(oldScene, onComplete);
                }
                else
                {
                    onComplete?.Invoke();
                }
            });
        }

        #endregion

        // ============================================================

        #region 生命周期

        protected override void OnSingletonDestroy()
        {
            EmberEventBus.Dispatch(EmberBroadcastEvent.SceneUnloading);
        }

        #endregion

        // ============================================================

        #region 内部方法

        private void StartLoad(string sceneName, LoadSceneMode mode, Action onComplete)
        {
            IsLoading = true;
            Progress = 0f;
            DisplayProgress = 0f;
            CurrentScene = sceneName;

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[Ember] EmberSceneManager: scene '{sceneName}' not found in Build Settings.");
                IsLoading = false;
                Progress = 1f;
                DisplayProgress = 1f;
                onComplete?.Invoke();
                return;
            }

            // allowSceneActivation = false 时加载停在 0.9，
            // 触发 OnBeforeActivate 后再激活
            op.allowSceneActivation = false;

            // 使用 UniTask 异步驱动加载流程（比协程性能更优）
            LoadAsync(op, sceneName, onComplete).Forget();
        }

        private async UniTask LoadAsync(AsyncOperation op, string sceneName, Action onComplete)
        {
            // Phase 1: 等待加载到 0.9，同时更新展示进度（按比例映射）
            while (op.progress < 0.9f)
            {
                Progress = op.progress;
                DisplayProgress = Progress * _displayMaxRatio;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Progress = 0.9f;
            DisplayProgress = Progress * _displayMaxRatio;

            // Phase 2: OnBeforeActivate 回调（使用 TCS 桥接回调到 async/await）
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneByName(sceneName);
            if (OnBeforeActivate != null)
            {
                var tcs = new UniTaskCompletionSource();
                OnBeforeActivate.Invoke(scene, () =>
                {
                    op.allowSceneActivation = true;
                    tcs.TrySetResult();
                });
                await tcs.Task;
            }
            else
            {
                op.allowSceneActivation = true;
            }

            // Phase 3: 等待场景激活完成
            while (!op.isDone)
                await UniTask.Yield(PlayerLoopTiming.Update);

            Progress = 1f;

            // Phase 4: 展示进度平滑过渡（60% → 100%），作为场景切换的视觉缓冲
            await SmoothProgressAsync();

            IsLoading = false;
            EmberEventBus.Dispatch(EmberBroadcastEvent.SceneLoaded);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 将 <see cref="DisplayProgress"/> 从当前值平滑过渡到 1.0，
        /// 持续 <see cref="_smoothDuration"/> 秒，消除真实进度的跳跃感。
        /// </summary>
        private async UniTask SmoothProgressAsync()
        {
            float elapsed = 0f;
            float start = DisplayProgress;

            while (elapsed < _smoothDuration)
            {
                elapsed += Time.deltaTime;
                DisplayProgress = Mathf.Lerp(start, 1f, elapsed / _smoothDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            DisplayProgress = 1f;
        }

        #endregion
    }
}
