using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class PlayerRollingSettings
    {
        #region 编辑器面板参数
        [SerializeField, Tooltip("真实落地时有水平输入，自动衔接翻滚")] private bool _enabled;
        [SerializeField, Min(.001f), Tooltip("锁定方向的水平速度，世界单位/秒")] private float _speed = 5;
        [SerializeField, Min(.001f), Tooltip("翻滚持续秒数，不依赖动画帧数")] private float _duration = .3f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerRollingSpec Capture() => new(_enabled, _speed, _duration);
        #endregion
    }

    public readonly struct PlayerRollingSpec
    {
        #region 内部参数
        public readonly bool Enabled;
        public readonly float Speed, Duration;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerRollingSpec(bool enabled, float speed, float duration)
        {
            if (enabled && (!float.IsFinite(speed) || speed <= 0 || !float.IsFinite(duration) || duration <= 0))
                throw new InvalidOperationException("启用翻滚时，速度与时长必须为有限正数。");
            Enabled = enabled; Speed = speed; Duration = duration;
        }
        #endregion
    }
}
