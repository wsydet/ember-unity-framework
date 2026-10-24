using System;
using UnityEngine;

namespace Game.Entity
{
    public enum LaunchMode2D { InitialVelocity, ConstantRise }
    public enum LaunchEnd2D { ContinueVelocity, StopVertical }
    public enum LaunchInterrupt2D { RejectWhileBusy, ReplaceExternalMotion }
    public enum LaunchResult2D { Accepted, Inactive, Busy, UnsupportedPolicy }
    public enum LaunchHorizontal2D { HoldRequestVelocity, PreserveControl }
    public enum LaunchWallQualification2D { Preserve, RefreshFirstVertical }

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
        public LaunchHorizontal2D Horizontal { get; }
        public LaunchWallQualification2D WallQualification { get; }
        public LaunchTailSpec2D Tail { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public LaunchRequest2D(string sourceId, Vector2 velocity, LaunchMode2D mode = LaunchMode2D.InitialVelocity,
            float duration = 0, LaunchEnd2D end = LaunchEnd2D.ContinueVelocity,
            LaunchInterrupt2D interrupt = LaunchInterrupt2D.ReplaceExternalMotion,
            LaunchHorizontal2D horizontal = LaunchHorizontal2D.HoldRequestVelocity,
            LaunchWallQualification2D wallQualification = LaunchWallQualification2D.Preserve,
            LaunchTailSpec2D tail = default)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) ||
                !float.IsFinite(duration) || duration < 0 || !Enum.IsDefined(typeof(LaunchMode2D), mode) ||
                !Enum.IsDefined(typeof(LaunchEnd2D), end) || !Enum.IsDefined(typeof(LaunchInterrupt2D), interrupt) ||
                !Enum.IsDefined(typeof(LaunchHorizontal2D), horizontal) ||
                !Enum.IsDefined(typeof(LaunchWallQualification2D), wallQualification) ||
                horizontal == LaunchHorizontal2D.PreserveControl && mode != LaunchMode2D.ConstantRise ||
                mode == LaunchMode2D.ConstantRise && (velocity.y <= 0 || duration <= 0))
                throw new ArgumentException("弹射需要来源、有限速度和合法策略；恒速上升需要正速度与正秒数。");
            tail.Validate();
            if (tail.Enabled && (mode != LaunchMode2D.ConstantRise || end != LaunchEnd2D.ContinueVelocity))
                throw new ArgumentException("弹射尾段仅适用于 ConstantRise + ContinueVelocity。");
            SourceId = sourceId; Velocity = velocity; Mode = mode; Duration = duration; End = end; Interrupt = interrupt;
            Horizontal = horizontal; WallQualification = wallQualification; Tail = tail;
        }

        #endregion
    }

    public interface ILaunchTarget2D
    {
        LaunchResult2D TryLaunch(Behaviour owner, LaunchRequest2D request);
    }
}
