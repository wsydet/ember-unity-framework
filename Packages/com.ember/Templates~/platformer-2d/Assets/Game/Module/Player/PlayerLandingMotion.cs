using UnityEngine;

namespace Game.Player
{
    /// <summary>记录本角色最后发往物理步的速度与支撑边沿，不使用整段下落最大值。</summary>
    public sealed class PlayerLandingMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private bool _airborne;
        private Vector2 _stepVelocity;
        public bool IsActive { get; private set; }
        public bool IsHard { get; private set; }
        public float Remaining { get; private set; }
        public float ImpactSpeed { get; private set; }
        internal bool Pending { get; private set; }
        internal bool HardPending => Pending && _context.Feel.Landing.HardEnabled && ImpactSpeed >= _context.Feel.Landing.HardSpeed;
        internal bool NormalPending => Pending && !HardPending && _context.Feel.Landing.NormalEnabled;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLandingMotion(PlayerContext context) => _context = context;
        internal void Observe()
        {
            var sensors = _context.Sensors;
            // 探地仅用于区分出生附近的微小沉降；落地消费统一真实支撑。
            // 出生/恢复时已在探地范围内的微小沉降不构成一次空中落地。
            bool supported = _context.JumpWindow.HasGroundSupport;
            var contact = _context.JumpWindow.Contact;
            float impact = -Vector2.Dot(_stepVelocity - contact.SurfaceVelocity, contact.Normal);
            Pending = _airborne && supported && impact > 0 && !_context.Ledge.IsActive && !_context.DropThrough.IsActive;
            if (Pending) ImpactSpeed = impact;
            if (supported) _airborne = false;
            else if (!sensors.IsGrounded) _airborne = true;
        }
        internal void RecordStep() => _stepVelocity = _context.Motor.Velocity;
        internal void Begin(bool hard)
        {
            IsActive = true; IsHard = hard; Pending = false;
            Remaining = hard ? _context.Feel.Landing.HardDuration : _context.Feel.Landing.NormalDuration;
            _context.Wall.Cancel(); Hold();
        }
        internal void Advance(float dt) => Remaining = Mathf.Max(0, Remaining - dt);
        internal void Hold()
        {
            _context.Motor.SetHorizontalVelocity(0); _context.Motor.SetVerticalVelocity(0);
        }
        internal void End() { IsActive = false; IsHard = false; Remaining = 0; Pending = false; }
        #endregion
    }
}
