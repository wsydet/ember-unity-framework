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
        internal void OverrideGravity(float gravity) => _overrideGravity = gravity;
        private bool _supported;
        private bool _suppressLeave;
        private bool _jumpThisStep;
        public float HoverRemaining { get; private set; }
        public float EffectiveGravityScale { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerVerticalMotion(PlayerFeelRuntime feel) { _feel = feel; EffectiveGravityScale = feel.GravityScale; }

        internal void Observe(bool supported, float verticalSpeed, float dt)
        {
            _overrideGravity = null;
            _jumpThisStep = false;
            HoverRemaining = Mathf.Max(0, HoverRemaining - dt);
            if (supported || verticalSpeed > 0) HoverRemaining = 0;
            else if (_supported && !_suppressLeave) HoverRemaining = _feel.EdgeHoverDuration;
            _supported = supported;
            _suppressLeave = false;
        }

        internal void NotifyJump()
        {
            HoverRemaining = 0;
            _jumpThisStep = true;
            _suppressLeave = true;
        }

        internal void Apply(EntityMotor2D motor, float worldGravityY, float dt)
        {
            float velocity = motor.CommandVelocity.y;
            float gravity = _feel.GravityScale;
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
