using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>每宿主独立的竖直组合规则；由 Controller 驱动，状态动作之后、Motor 执行之前结算。</summary>
    public sealed class PlayerVerticalMotion
    {
        #region 内部参数
        private readonly PlayerFeelRuntime _feel;
        private float? _overrideGravity;
        internal void OverrideGravity(float gravity) { if (OrdinaryActive) CancelOrdinary(); _overrideGravity = gravity; }
        private PlayerInputAdapter _ordinaryInput;
        private ulong _pressVersion, _clearVersion;
        private float _elapsed;
        private bool _released, _cutApplied, _holding;
        public PlayerFlightOrigin Origin { get; private set; }
        public bool OrdinaryActive { get; private set; }
        public float OrdinaryElapsed => _elapsed;
        private bool _supported;
        private bool _suppressLeave;
        private bool _jumpThisStep;
        public float HoverRemaining { get; private set; }
        public float EffectiveGravityScale { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ApplyOrdinary(ref float velocity, ref float gravity, float worldGravityY, float dt)
        {
            var spec = _feel.OrdinaryJump;
            if (_ordinaryInput.ClearVersion != _clearVersion) PauseOrdinary();
            if (!_ordinaryInput.JumpHeld || _ordinaryInput.JumpPressVersion != _pressVersion) _released = true;
            float holdUntil = _released ? spec.MinimumHold : spec.MaximumHold;
            float heldSeconds = _holding ? Mathf.Clamp(holdUntil - _elapsed, 0, dt) : 0;
            if (heldSeconds > 0) velocity = spec.LaunchSpeed;
            if (heldSeconds < dt || holdUntil <= _elapsed) _holding = false;
            if (_released && !_cutApplied && _elapsed >= spec.MinimumHold)
            {
                if (velocity > 0)
                {
                    if (spec.Release == PlayerOrdinaryJumpRelease.LimitSpeed) velocity = Mathf.Min(velocity, spec.ReleaseValue);
                    else if (spec.Release == PlayerOrdinaryJumpRelease.MultiplySpeed) velocity *= spec.ReleaseValue;
                }
                _cutApplied = true;
            }
            float acceleration = velocity < 0 ? spec.FallGravity : spec.RiseGravity;
            if (spec.ApexEnabled && (!spec.ApexRequiresHeld || !_released) && Mathf.Abs(velocity) <= spec.ApexSpeed)
                acceleration *= spec.ApexMultiplier;
            gravity = acceleration / -worldGravityY;
            if (dt > 0) gravity *= 1 - heldSeconds / dt;
            velocity = Mathf.Max(velocity, -spec.FallLimit);
            if (dt > 0) gravity = Mathf.Min(gravity, Mathf.Max(0, (velocity + spec.FallLimit) / (-worldGravityY * dt)));
            _elapsed += dt;
        }

        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerVerticalMotion(PlayerFeelRuntime feel) { _feel = feel; EffectiveGravityScale = feel.GravityScale; }

        internal void Observe(bool supported, float verticalSpeed, float dt, bool groundContact = true)
        {
            if (supported && groundContact) CancelOrdinary(PlayerFlightOrigin.Natural);
            // 实际物理解算已停止上升（包括碰顶），不得用恒速保持重新向顶推动。
            if (OrdinaryActive && verticalSpeed <= 0) { _holding = false; _cutApplied = true; }
            _overrideGravity = null;
            _jumpThisStep = false;
            HoverRemaining = Mathf.Max(0, HoverRemaining - dt);
            if (supported || verticalSpeed > 0) HoverRemaining = 0;
            else if (_supported && !_suppressLeave) HoverRemaining = _feel.EdgeHoverDuration;
            _supported = supported;
            _suppressLeave = false;
        }

        internal void NotifyJump(PlayerFlightOrigin origin = PlayerFlightOrigin.Action)
        {
            CancelOrdinary(origin);
            HoverRemaining = 0;
            _jumpThisStep = true;
            _suppressLeave = true;
        }

        internal void BeginOrdinary(PlayerInputAdapter input)
        {
            NotifyJump(PlayerFlightOrigin.OrdinaryJump);
            if (!_feel.OrdinaryJump.Enabled) return;
            OrdinaryActive = true; _ordinaryInput = input;
            _pressVersion = input.JumpPressVersion; _clearVersion = input.ClearVersion;
            _released = !input.JumpHeld; _cutApplied = false; _holding = true; _elapsed = 0;
        }

        internal void CancelOrdinary(PlayerFlightOrigin origin = PlayerFlightOrigin.Action)
        {
            OrdinaryActive = false; _ordinaryInput = null; _holding = false; _elapsed = 0;
            Origin = origin;
        }

        internal void PauseOrdinary()
        {
            // 暂停不切换飞行来源；恢复不续接旧按住、不补发松键削速。
            _released = true; _holding = false; _cutApplied = true;
        }

        internal void Apply(EntityMotor2D motor, float worldGravityY, float dt)
        {
            float velocity = motor.CommandVelocity.y;
            float gravity = _feel.GravityScale;
            if (OrdinaryActive)
            {
                ApplyOrdinary(ref velocity, ref gravity, worldGravityY, dt);
                motor.SetVerticalVelocity(velocity); motor.SetGravityScale(gravity);
                EffectiveGravityScale = gravity;
                return;
            }
            if (velocity > 0) HoverRemaining = 0;
            if (HoverRemaining > .000001f)
            {
                velocity = 0; gravity = 0;
            }
            else if (!_supported || _jumpThisStep)
            {
                if (_feel.ApexGravity && Mathf.Abs(velocity) <= _feel.ApexSpeed)
                    gravity *= _feel.ApexMultiplier;
                else if (_feel.FallGravity && velocity < 0) gravity *= _feel.FallMultiplier;
            }
            if (_overrideGravity.HasValue) gravity = _overrideGravity.Value;
            // 限速最后结算：先纠正已有超速，再限制本步重力可增加的下降速度。
            // 不手动积分位置；Unity 继续负责实际物理与碰撞。
            if (_feel.FallLimitEnabled)
            {
                velocity = Mathf.Max(velocity, -_feel.FallLimit);
                if (dt > 0 && worldGravityY < 0)
                    gravity = Mathf.Min(gravity, Mathf.Max(0, (velocity + _feel.FallLimit) / (-worldGravityY * dt)));
            }
            motor.SetVerticalVelocity(velocity);
            motor.SetGravityScale(gravity);
            EffectiveGravityScale = gravity;
        }
        #endregion
    }
}
