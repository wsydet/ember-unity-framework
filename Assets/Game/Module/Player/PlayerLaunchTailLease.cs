using System;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>本次保持结束后的飞行所有权；不占行动输入，不刷新资格，释放旧凭证不影响新尾段。</summary>
    public sealed class PlayerLaunchTailLease : IDisposable
    {
        #region 内部参数
        internal PlayerVerticalMotion Session;
        internal readonly Behaviour Owner;
        internal readonly PlayerInputAdapter Input;
        internal readonly LaunchTailSpec2D Spec;
        public string SourceId { get; }
        public ulong Generation { get; }
        public bool SlowPermissionLatched { get; private set; }
        public bool IsActive => Session != null && Owner != null && Owner.isActiveAndEnabled;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLaunchTailLease(PlayerVerticalMotion session, Behaviour owner, PlayerInputAdapter input,
            string sourceId, ulong generation, LaunchTailSpec2D spec)
        {
            Session = session; Owner = owner; Input = input; SourceId = sourceId; Generation = generation; Spec = spec;
            SlowPermissionLatched = spec.LatchSlowUntilDescending;
        }

        internal float Acceleration(float selfY)
        {
            // 边界检查使用扣除上一步风/支撑后的真实自身速度。首次低于下边界后不再次闩锁。
            if (selfY < -Spec.SlowSpeed) SlowPermissionLatched = false;
            if (Spec.SlowBand && (SlowPermissionLatched || Input.JumpHeld) && Mathf.Abs(selfY) <= Spec.SlowSpeed)
                return Spec.SlowAcceleration;
            return selfY < 0 ? Spec.FallAcceleration : Spec.RiseAcceleration;
        }

        public void Dispose()
        {
            var session = Session; Session = null; session?.ReleaseLaunchTail(this);
        }
        #endregion
    }
}
