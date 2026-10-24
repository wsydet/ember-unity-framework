using System;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerDashSpeedMode { Constant, Linear }

    /// <summary>地面或空中冲刺的运动参数，作为移动 SO 的序列化分组；不保存资源和启用状态。</summary>
    [Serializable]
    public sealed class PlayerDashSettings
    {
        #region 编辑器面板参数
        [SerializeField, Min(.001f), Tooltip("冲刺持续的游戏秒数，不使用动画帧数。")] private float _duration = .2f;
        [SerializeField] private PlayerDashSpeedMode _speedMode;
        [SerializeField, Min(0), Tooltip("恒速模式的速度，世界单位/秒。")] private float _speed = 16;
        [SerializeField, Min(0), Tooltip("线性模式起始速度，世界单位/秒。")] private float _startSpeed = 16;
        [SerializeField, Min(0), Tooltip("线性模式结束速度，世界单位/秒。")] private float _endSpeed = 8;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerDashMotionSpec Capture()
        {
            if (!float.IsFinite(_duration) || _duration <= 0 || !Enum.IsDefined(typeof(PlayerDashSpeedMode), _speedMode)
                || !float.IsFinite(_speed) || _speed < 0 || !float.IsFinite(_startSpeed) || _startSpeed < 0
                || !float.IsFinite(_endSpeed) || _endSpeed < 0)
                throw new InvalidOperationException("冲刺需要有限正持续时间、有效速度模式和有限非负速度。");
            return new PlayerDashMotionSpec(_duration, _speedMode, _speed, _startSpeed, _endSpeed);
        }
        #endregion
    }

    /// <summary>准备时冻结的冲刺运动值；不持有 SO 或可变分组引用。</summary>
    public readonly struct PlayerDashMotionSpec
    {
        #region 内部参数
        public float Duration { get; }
        public PlayerDashSpeedMode SpeedMode { get; }
        public float Speed { get; }
        public float StartSpeed { get; }
        public float EndSpeed { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerDashMotionSpec(float duration, PlayerDashSpeedMode mode, float speed, float start, float end)
        { Duration = duration; SpeedMode = mode; Speed = speed; StartSpeed = start; EndSpeed = end; }
        internal float SpeedAt(float elapsedSeconds) => SpeedMode == PlayerDashSpeedMode.Constant ? Speed
            : Mathf.Lerp(StartSpeed, EndSpeed, Mathf.Clamp01(elapsedSeconds / Duration));
        #endregion
    }
}
