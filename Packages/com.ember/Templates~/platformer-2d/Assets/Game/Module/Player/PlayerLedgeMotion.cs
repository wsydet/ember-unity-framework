using System;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>单角色挂边／翻越接管。状态提交资源，Controller 每物理步最后执行；不自行 Tick，不持久化动作。</summary>
    public sealed class PlayerLedgeMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly EntityLedgeProbe2D _probe;
        private readonly PlayerLedgeSpec _spec;
        private EntityLedgeProbe2D.Candidate _candidate;
        private Collider2D _blockedSurface;
        private Vector2 _blockedCorner;
        private int _blockedDirection;
        private Vector2 _climbStart;
        private bool _inward;
        private float _phaseElapsed;
        public bool IsActive { get; private set; }
        public bool IsClimbing { get; private set; }
        public bool Failed { get; private set; }
        public bool Completed { get; private set; }
        public float Elapsed { get; private set; }
        public Collider2D Target => IsActive ? _candidate.Surface : null;
        public int Direction => IsActive ? _candidate.Direction : 0;
        internal bool ShouldRelease => _context.Input.Vertical < 0 || _context.Input.Horizontal * Direction < 0;
        internal bool CanClimb => IsActive && !IsClimbing && !Failed && _candidate.IsValid &&
            (_context.Motor.Position - _candidate.HangPosition).sqrMagnitude < .0004f &&
            (_context.Input.Vertical > 0 || _context.Input.FreshJumpRequested) &&
            _context.Abilities.Query(PlayerLedgeClimbState.ABILITY_ID) == EntityUseStatus.Available &&
            _probe.CanClimb(_candidate, _spec.Layers, _spec.Skin);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Commit(string id)
        {
            if (!_context.Abilities.TryGet(id, out var ability) || ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available)
                throw new InvalidOperationException("挂边／翻越资源提交失败：" + id);
        }
        private void BlockCurrentEdge()
        { _blockedSurface = _candidate.Surface; _blockedCorner = _candidate.Corner; _blockedDirection = _candidate.Direction; }
        private void ZeroCommand()
        { _context.Motor.SetHorizontalVelocity(0); _context.Motor.SetVerticalVelocity(0); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLedgeMotion(PlayerContext context)
        { _context = context; _spec = context.Feel.Ledge; _probe = new EntityLedgeProbe2D(context.Entity, context.Sensors.Shape); }
        internal void ObserveInput()
        {
            if (_blockedSurface != null && _context.Input.Horizontal * _blockedDirection <= 0)
                _blockedSurface = null;
        }
        internal bool TryCandidate(out EntityLedgeProbe2D.Candidate candidate)
        {
            candidate = default;
            if (_context.ExternalMotion.PreservesHorizontalDuringRise ||
                IsActive || _context.DropThrough.IsActive || _context.Dash.IsActive || _context.JumpWindow.HasGroundSupport ||
                _context.Motor.Velocity.y > 0 || _context.Wall.ProtectionRemaining > 0 || _context.Input.Horizontal == 0 ||
                _context.Abilities.Query(PlayerLedgeHangState.ABILITY_ID) != EntityUseStatus.Available) return false;
            if (!_probe.TryFind(_context.Input.Horizontal > 0 ? 1 : -1, _spec.Layers, _spec.Reach,
                    _spec.VerticalReach, _spec.GripInset, _spec.Skin, out candidate)) return false;
            return candidate.Surface != _blockedSurface || candidate.Direction != _blockedDirection ||
                (candidate.Corner - _blockedCorner).sqrMagnitude > .01f;
        }
        internal void BeginHang()
        {
            if (!TryCandidate(out var candidate)) throw new InvalidOperationException("挂边入口已失效。");
            _context.Combat?.Dash?.Cancel();
            Commit(PlayerLedgeHangState.ABILITY_ID);
            _candidate = candidate; IsActive = true; IsClimbing = false; Failed = false; Completed = false; Elapsed = 0;
            _context.Wall.Cancel(); _context.JumpWindow.ClearSupport(); _context.VerticalMotion.NotifyJump();
            _context.Facing.SetFacing(candidate.Direction > 0); ZeroCommand();
        }
        internal void BeginClimb()
        {
            // Hang.Exit 已释放接管，但保留预检选中的几何值；此处不重新创建状态或重复提交挂边能力。
            if (!_candidate.IsValid || !_probe.CanClimb(_candidate, _spec.Layers, _spec.Skin))
                throw new InvalidOperationException("翻越几何在提交前失效。");
            Commit(PlayerLedgeClimbState.ABILITY_ID);
            if (_context.Input.FreshJumpRequested) _context.Input.ConsumeJump();
            IsActive = true; IsClimbing = true; Failed = false; Completed = false; Elapsed = 0;
            _phaseElapsed = 0; _inward = false; _climbStart = _context.Motor.Position;
            _context.VerticalMotion.NotifyJump(); ZeroCommand();
        }
        internal void Apply(float dt)
        {
            if (!IsActive || dt <= 0) return;
            _context.JumpWindow.ClearSupport(); _context.VerticalMotion.NotifyJump();
            _context.VerticalMotion.OverrideGravity(0);
            if (!_candidate.IsValid) { Failed = true; ZeroCommand(); return; }
            if (Failed || Completed) { ZeroCommand(); return; }
            Vector2 target = _candidate.HangPosition;
            if (IsClimbing)
            {
                // 在下一次物理步确认到达再换段／完成，不在发出速度命令时冒充已经抵达。
                float duration = _inward ? _spec.InwardDuration : _spec.RiseDuration;
                Vector2 end = _inward ? _candidate.StandPosition : _candidate.RaisedPosition;
                if (_phaseElapsed >= duration && (_context.Motor.Position - end).sqrMagnitude < .0004f)
                {
                    if (_inward) { Completed = true; ZeroCommand(); return; }
                    _inward = true; _phaseElapsed = 0; _climbStart = _context.Motor.Position;
                    duration = _spec.InwardDuration; end = _candidate.StandPosition;
                }
                _phaseElapsed = Mathf.Min(duration, _phaseElapsed + dt); Elapsed += dt;
                target = Vector2.Lerp(_climbStart, end, _phaseElapsed / duration);
            }
            if (!_context.Motor.SetSweptDisplacement(target - _context.Motor.Position, dt, _spec.Layers, _spec.Skin * .5f, out _))
            { Failed = true; BlockCurrentEdge(); }
        }
        internal void ReleaseInput() { BlockCurrentEdge(); _context.Input.ConsumeJump(); }
        internal void End()
        {
            if (!IsActive) return;
            IsActive = false; IsClimbing = false; ZeroCommand();
            _context.VerticalMotion.NotifyJump(); _context.Motor.SetGravityScale(_context.Feel.GravityScale);
        }
        internal void Dispose()
        { End(); _candidate = default; _blockedSurface = null; Failed = false; Completed = false; Elapsed = 0; }
        #endregion
    }
}
