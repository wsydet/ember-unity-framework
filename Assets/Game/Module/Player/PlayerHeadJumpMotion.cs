using Game.Entity;

namespace Game.Player
{
    public sealed class PlayerHeadJumpMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly EntityHeadProbe2D _probe;
        public bool IsActive { get; private set; }
        public EntityHeadTarget2D Target => _probe.Candidate;
        internal bool CanStart => Target != null && Target.IsAvailable && _context.Input.JumpRequested &&
            !_context.CanStartJump && !_context.JumpWindow.HasGroundSupport && _context.Motor.Velocity.y <= 0 &&
            _context.Abilities.Query(PlayerHeadJumpState.ABILITY_ID) == EntityUseStatus.Available;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerHeadJumpMotion(PlayerContext context)
        { _context = context; _probe = new EntityHeadProbe2D(context.Entity, context.Sensors.Shape); }
        internal void Observe() => _probe.Sample(!_context.Dash.IsActive && !_context.DropThrough.IsActive &&
            !_context.Ledge.IsActive && !_context.Landing.IsActive && !_context.Rolling.IsActive);
        internal void Begin() => IsActive = true;
        internal void End() => IsActive = false;
        internal void Clear() { End(); _probe.Clear(); }
        #endregion
    }
}
