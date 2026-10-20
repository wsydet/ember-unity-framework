using UnityEngine;

namespace Game.Player
{
    /// <summary>每宿主墙跳控制窗口；由唯一玩家物理驱动推进，不属于持久化动作。</summary>
    public sealed class PlayerWallMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private float _elapsed, _step;
        private bool _independent;
        public bool VerticalBranch { get; private set; }
        public bool VerticalAvailable { get; private set; } = true;
        public float ProtectionRemaining { get; private set; }
        public float ReturnRemaining { get; private set; }
        public int OriginWall { get; private set; }
        public bool Returning { get; private set; }
        public bool Sliding { get; internal set; }
        internal int ExitedSlideWall { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerWallMotion(PlayerContext context) => _context = context;
        internal void BeginJump(int wall)
        {
            Cancel(); OriginWall = wall;
            var spec = _context.Feel.WallJump;
            if (spec.Enabled)
            {
                _independent = true;
                VerticalBranch = spec.FirstTowardVertical && VerticalAvailable && _context.Input.Horizontal * wall > 0;
                if (VerticalBranch) VerticalAvailable = false;
                _context.Motor.SetHorizontalVelocity(VerticalBranch ? 0 : -wall * spec.OutwardSpeed);
                _context.Motor.SetVerticalVelocity(spec.Vertical.LaunchSpeed);
                return;
            }
            ProtectionRemaining = _context.Feel.WallProtection;
            ReturnRemaining = _context.Feel.WallReturnEnabled ? _context.Feel.WallReturnDuration : 0;
            _context.Motor.SetHorizontalVelocity(-wall * _context.Feel.WallJumpX);
            _context.Motor.SetVerticalVelocity(_context.Feel.WallJumpY);
        }
        internal void Observe(float dt)
        {
            ExitedSlideWall = 0; _step = dt;
            if (_context.JumpWindow.HasGroundSupport) { VerticalAvailable = true; Cancel(); return; }
            float remainder = Mathf.Max(0, dt - ProtectionRemaining);
            ProtectionRemaining = Mathf.Max(0, ProtectionRemaining - dt);
            ReturnRemaining = Mathf.Max(0, ReturnRemaining - remainder);
            if (ReturnRemaining <= 0) Returning = false;
        }
        internal void Apply()
        {
            var feel = _context.Feel;
            if (Sliding)
            {
                _context.Motor.SetHorizontalVelocity(0);
                _context.Motor.SetVerticalVelocity(-(_context.Input.Vertical < 0 ? feel.WallFastSlideSpeed : feel.WallSlideSpeed));
                _context.VerticalMotion.OverrideGravity(0);
                return;
            }
            if (_independent)
            {
                if (!_context.VerticalMotion.WallActive) { Cancel(); return; }
                var spec = feel.WallJump;
                if (VerticalBranch && _elapsed > 0 && _elapsed <= spec.ConversionWindow &&
                    _context.VerticalMotion.HoldingJump && _context.Input.Horizontal * OriginWall < 0)
                {
                    VerticalBranch = false;
                    if (spec.RestoreOnConversion) VerticalAvailable = true;
                    _context.Motor.SetHorizontalVelocity(-OriginWall * spec.OutwardSpeed);
                }
                else if (VerticalBranch && _context.VerticalMotion.HoldingJump)
                    _context.Motor.SetHorizontalVelocity(0);
                else if (_elapsed > 0)
                {
                    float speed = _context.ExternalMotion.SelfVelocity.x;
                    if (Mathf.Abs(speed) > spec.HorizontalLimit)
                        _context.Motor.SetHorizontalVelocity(Mathf.MoveTowards(speed,
                            Mathf.Sign(speed) * spec.HorizontalLimit, spec.OverspeedDeceleration * _step));
                }
                _elapsed += _step;
                return;
            }
            if (ProtectionRemaining > 0)
            { _context.Motor.SetHorizontalVelocity(-OriginWall * feel.WallJumpX); return; }
            if (ReturnRemaining > 0 && !Returning && _context.Input.Horizontal * OriginWall > 0)
            { Returning = true; _context.Motor.SetVerticalVelocity(0); }
            if (Returning)
            {
                _context.Motor.SetHorizontalVelocity(OriginWall * feel.EffectiveAirSpeed * feel.WallReturnMultiplier);
                _context.VerticalMotion.OverrideGravity(feel.GravityScale * feel.WallReturnMultiplier * feel.WallReturnMultiplier);
            }
        }
        internal void Cancel()
        { _independent = false; VerticalBranch = false; _elapsed = 0; ProtectionRemaining = 0; ReturnRemaining = 0; OriginWall = 0; Returning = false; Sliding = false; ExitedSlideWall = 0; }
        internal void EndSlide()
        { ExitedSlideWall = _context.ContactWall; Sliding = false; }
        #endregion
    }
}
