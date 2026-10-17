using System;
using UnityEngine;

namespace Game.Entity
{
    public enum LaunchMode2D { InitialVelocity, ConstantRise }
    public enum LaunchEnd2D { ContinueVelocity, StopVertical }
    public enum LaunchInterrupt2D { RejectWhileBusy, ReplaceExternalMotion }
    public enum LaunchResult2D { Accepted, Inactive, Busy }

    /// <summary>世界空间速度请求；起点保持当前物理位置，不隐式传送。</summary>
    public readonly struct LaunchRequest2D
    {
        #region 内部参数

        public string SourceId { get; }
        public Vector2 Velocity { get; }
        public LaunchMode2D Mode { get; }
        public float Duration { get; }
        public LaunchEnd2D End { get; }
        public LaunchInterrupt2D Interrupt { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public LaunchRequest2D(string sourceId, Vector2 velocity, LaunchMode2D mode = LaunchMode2D.InitialVelocity,
            float duration = 0, LaunchEnd2D end = LaunchEnd2D.ContinueVelocity,
            LaunchInterrupt2D interrupt = LaunchInterrupt2D.ReplaceExternalMotion)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) ||
                !float.IsFinite(duration) || duration < 0 || !Enum.IsDefined(typeof(LaunchMode2D), mode) ||
                !Enum.IsDefined(typeof(LaunchEnd2D), end) || !Enum.IsDefined(typeof(LaunchInterrupt2D), interrupt) ||
                mode == LaunchMode2D.ConstantRise && (velocity.y <= 0 || duration <= 0))
                throw new ArgumentException("弹射需要来源、有限速度和合法策略；恒速上升需要正速度与正秒数。");
            SourceId = sourceId; Velocity = velocity; Mode = mode; Duration = duration; End = end; Interrupt = interrupt;
        }

        #endregion
    }

    public interface ILaunchTarget2D
    {
        LaunchResult2D TryLaunch(Behaviour owner, LaunchRequest2D request);
    }
}
