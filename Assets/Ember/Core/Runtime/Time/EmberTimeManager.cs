using UnityEngine;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 框架时间管理器 —— 统一的时间快照与缩放控制。
    ///
    /// 提供四种时间值，由 <see cref="EmberUpdateManager"/> 每帧自动驱动更新：
    ///
    /// <list type="bullet">
    ///   <item><see cref="DeltaTime"/> — 当前帧间隔（受 <see cref="TimeScale"/> 和 <see cref="IsPaused"/> 影响）</item>
    ///   <item><see cref="UnscaledDeltaTime"/> — 当前帧间隔（不受任何缩放/暂停影响）</item>
    ///   <item><see cref="Time"/> — 累计游戏运行时间（受 <see cref="TimeScale"/> 和 <see cref="IsPaused"/> 影响）</item>
    ///   <item><see cref="UnscaledTime"/> — 累计真实运行时间（不受任何缩放/暂停影响）</item>
    /// </list>
    ///
    /// <b>与 burner GameTime 的关键区别：</b>
    /// - 无需手动 Tick()，由 EmberUpdateManager 自动驱动
    /// - 语义清晰：Time = scaled，UnscaledTime = 真实时间（burner 的 CurTime 语义混乱）
    /// - TimeScale 独立于 UnityEngine.Time.timeScale，互不干扰
    /// - 内置 Pause/Resume，暂停只冻结 scaled 时间
    ///
    /// <b>使用示例：</b>
    /// <code>
    /// // 在任意 IEmberUpdate 实现中
    /// void Update() {
    ///     float dt = EmberTimeManager.Instance.DeltaTime;
    ///     transform.Translate(Vector3.forward * speed * dt);
    /// }
    ///
    /// // 慢动作
    /// EmberTimeManager.Instance.TimeScale = 0.5f;
    ///
    /// // 暂停游戏逻辑（UI 动画继续用 UnscaledDeltaTime）
    /// EmberTimeManager.Instance.Pause();
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Time)]
    public class EmberTimeManager : EmberSingleton<EmberTimeManager>, IEmberManager, IEmberUpdate
    {
        #region 内部参数

        private const string TAG = LogTags.CoreTimeManager;
        private const float DEFAULT_DELTA_TIME = 1f / 60f;

        /// <summary>当前帧 scaled deltaTime（已应用 TimeScale 和 Pause）</summary>
        private float _deltaTime = DEFAULT_DELTA_TIME;

        /// <summary>当前帧 unscaled deltaTime（不受任何影响）</summary>
        private float _unscaledDeltaTime = DEFAULT_DELTA_TIME;

        /// <summary>累计 scaled 运行时间</summary>
        private float _time;

        /// <summary>累计 unscaled 运行时间</summary>
        private float _unscaledTime;

        /// <summary>独立时间缩放（不影响 UnityEngine.Time.timeScale）</summary>
        private float _timeScale = 1f;

        /// <summary>是否暂停（冻结 scaled 时间）</summary>
        private bool _isPaused;

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberManager.Init()
        {
            _time = 0f;
            _unscaledTime = 0f;
            _deltaTime = DEFAULT_DELTA_TIME;
            _unscaledDeltaTime = DEFAULT_DELTA_TIME;
            _timeScale = 1f;
            _isPaused = false;

            EmberDebug.LogInit(TAG, "EmberTimeManager initialized. " +
                $"TimeScale={_timeScale}, Paused={_isPaused}");
        }

        void IEmberManager.Destroy()
        {
            EmberDebug.LogCleanup(TAG, "EmberTimeManager destroyed.");
        }

        #endregion

        // ============================================================

        #region 内部方法

        void IEmberUpdate.Update()
        {
            // 快照本帧的原始 deltaTime（Unity 在帧开始时已计算好）
            float rawDelta = UnityEngine.Time.deltaTime;
            float rawUnscaledDelta = UnityEngine.Time.unscaledDeltaTime;

            _unscaledDeltaTime = rawUnscaledDelta;
            _unscaledTime += rawUnscaledDelta;

            if (_isPaused)
            {
                _deltaTime = 0f;
                // _time 不累积 —— 暂停期间 scaled 时间冻结
            }
            else
            {
                _deltaTime = rawDelta * _timeScale;
                _time += _deltaTime;
            }
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 当前帧的 scaled deltaTime。
        /// = UnityEngine.Time.deltaTime 《《》》 TimeScale，暂停时为 0。
        /// 首帧之前默认为 1/60。
        /// </summary>
        [NoGC]
        public float DeltaTime => _deltaTime;

        /// <summary>
        /// 当前帧的 unscaled deltaTime。
        /// = UnityEngine.Time.unscaledDeltaTime，不受 TimeScale 和暂停影响。
        /// 首帧之前默认为 1/60。
        /// </summary>
        [NoGC]
        public float UnscaledDeltaTime => _unscaledDeltaTime;

        /// <summary>
        /// 累计 scaled 游戏运行时间。
        /// 受 <see cref="TimeScale"/> 影响，暂停时冻结。
        /// </summary>
        [NoGC]
        public float Time => _time;

        /// <summary>
        /// 累计 unscaled 真实运行时间。
        /// 不受任何缩放或暂停影响，从 Init() 开始持续递增。
        /// </summary>
        [NoGC]
        public float UnscaledTime => _unscaledTime;

        /// <summary>
        /// 独立时间缩放系数。默认 1。
        /// 只影响 <see cref="DeltaTime"/> 和 <see cref="Time"/>，
        /// 不影响 UnityEngine.Time.timeScale 和 <see cref="UnscaledDeltaTime"/> / <see cref="UnscaledTime"/>。
        ///
        /// 设为 0 等价于 Pause()（DeltaTime = 0，Time 冻结）。
        /// 可以大于 1 实现加速效果。
        /// </summary>
        public float TimeScale
        {
            [NoGC]
            get => _timeScale;
            set => _timeScale = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 是否处于暂停状态。
        /// 暂停期间 <see cref="DeltaTime"/> 始终为 0，<see cref="Time"/> 冻结。
        /// <see cref="UnscaledDeltaTime"/> 和 <see cref="UnscaledTime"/> 不受影响。
        /// </summary>
        [NoGC]
        public bool IsPaused => _isPaused;

        /// <summary>
        /// 暂停 scaled 时间。可安全重复调用。
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            _deltaTime = 0f;
            EmberDebug.LogEvent(TAG, "Time paused.");
        }

        /// <summary>
        /// 恢复 scaled 时间。可安全重复调用。
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            EmberDebug.LogEvent(TAG, "Time resumed.");
        }

        #endregion
    }
}
