using UnityEngine;

namespace Game.Player
{
    /// <summary>落地翻滚的每宿主接管；计时仅由真实 Rolling 状态推进。</summary>
    public sealed class PlayerRollingMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private bool _wallStopped;
        public bool IsActive { get; private set; }
        public int Direction { get; private set; }
        public float Remaining { get; private set; }
        internal bool CanStart => !IsActive && _context.Feel.Rolling.Enabled && _context.Landing.Pending &&
            !_context.Landing.HardPending && _context.Input.Horizontal != 0 &&
            !_context.CanStartJump && !_context.DropThrough.CanStart && !_context.Dash.CanStart(true);
        internal bool WallAhead => Direction > 0 ? _context.Sensors.RightWall != null : _context.Sensors.LeftWall != null;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerRollingMotion(PlayerContext context) => _context = context;
        internal void Begin()
        {
            Direction = _context.Input.Horizontal > 0 ? 1 : -1;
            IsActive = true; _wallStopped = false; Remaining = _context.Feel.Rolling.Duration;
            _context.Wall.Cancel(); _context.Facing.SetFacing(Direction > 0); Apply();
        }
        internal void Apply()
        {
            _context.Motor.SetHorizontalVelocity(Direction * _context.Feel.Rolling.Speed);
            _context.Motor.SetVerticalVelocity(0);
        }
        internal void Advance(float dt) => Remaining = Mathf.Max(0, Remaining - dt);
        internal void StopAtWall() => _wallStopped = true;
        internal void End()
        {
            if (!IsActive) return;
            IsActive = false; Remaining = 0;
            float limit = _context.JumpWindow.HasGroundSupport ? _context.Feel.MoveSpeed : _context.Feel.EffectiveAirSpeed;
            _context.Motor.SetHorizontalVelocity(_wallStopped ? 0 : Mathf.Clamp(_context.ExternalMotion.SelfVelocity.x, -limit, limit));
            if (!_context.JumpWindow.HasGroundSupport) _context.VerticalMotion.NotifyJump();
        }
        #endregion
    }
}
