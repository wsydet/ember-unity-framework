using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>战斗冲刺的普通接续记录、独立收尾和运动交接。接续截止不等同于惯性截止。</summary>
    internal sealed class PlayerCombatDashMotion
    {
        private readonly PlayerContext _context;
        private readonly PlayerCombatConfiguration _config;
        private readonly Dictionary<string, PlayerAttackSegment> _segments;
        private string _pendingNext, _next;
        private bool _inertia;
        private bool _preserveInputOnExit;
        private double _tailTime;
        internal bool InDash { get; private set; }
        internal bool InTail { get; private set; }
        internal bool TailFinished => _tailTime >= _context.Feel.Attack.Dash.Duration;
        internal double TailTime => _tailTime;
        internal bool OpportunityActive => InDash || InTail && !TailFinished;
        internal bool HasResidualMotion => _inertia;
        internal bool Gate => InTail && !TailFinished || InDash && _context.Dash.EndReason == PlayerDashEndReason.None &&
            _context.Dash.Elapsed >= _context.Feel.Attack.Dash.OpensAt &&
            (_context.Dash.Direction > 0 ? _context.Sensors.RightWall == null : _context.Sensors.LeftWall == null) &&
            _context.Dash.IsGroundDash == _context.JumpWindow.HasGroundSupport;
        internal string Target
        {
            get
            {
                string id = _context.Feel.Attack.Dash.Mode == PlayerDashAttackMode.Dedicated ? _config.Combo.DashEntry : _next;
                if (!string.IsNullOrEmpty(id) && _segments.TryGetValue(id, out var segment) && EnvironmentAllows(segment)) return id;
                return _context.JumpWindow.HasGroundSupport ? _config.Combo.GroundEntry : _config.Combo.AirEntry;
            }
        }
        private bool EnvironmentAllows(PlayerAttackSegment segment) => segment.Environment == PlayerAttackEnvironment.Both ||
            (segment.Environment == PlayerAttackEnvironment.Ground) == _context.JumpWindow.HasGroundSupport;
        internal PlayerCombatDashMotion(PlayerContext context, PlayerCombatConfiguration config, Dictionary<string, PlayerAttackSegment> segments)
        { _context = context; _config = config; _segments = segments; }
        internal void Carry(string next) => _pendingNext = next;
        internal void PrepareActionExit() => _preserveInputOnExit = true;
        internal void ObserveEnvironment()
        {
            if (OpportunityActive && !string.IsNullOrEmpty(_next) && _segments.TryGetValue(_next, out var segment) && !EnvironmentAllows(segment))
                _next = null;
        }
        internal void Begin()
        {
            _next = _context.Feel.Attack.Dash.Mode == PlayerDashAttackMode.ContinueNormal ? _pendingNext : null;
            _pendingNext = null; InDash = true; InTail = false; _inertia = false;
            _context.MotionControl.Release(this);
            _context.Input.Combat.Begin(PlayerAttackIntent.DashAttack, double.PositiveInfinity, double.PositiveInfinity);
        }
        internal bool EndDash(bool ground, PlayerDashEndReason reason)
        {
            if (!InDash) return false;
            InDash = false;
            if (reason != PlayerDashEndReason.Completed) { Cancel(); return false; }
            InTail = true; _tailTime = 0; _inertia = !ground;
            _context.Input.Combat.Continue(PlayerAttackIntent.DashAttack, _context.Feel.Attack.Dash.Duration, _context.Feel.Attack.Dash.Duration);
            if (!ground) _context.Motor.SetHorizontalVelocity(_context.ExternalMotion.SelfVelocity.x * _context.Feel.Attack.Dash.AirVelocityRatio);
            _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal | PlayerMotionChannels.AutomaticTransitions |
                PlayerMotionChannels.Wall | PlayerMotionChannels.Ledge | PlayerMotionChannels.Correction);
            return true;
        }
        internal void AdvanceTail(float dt) { _tailTime += dt; ApplyMotion(dt); }
        internal void AdvanceResidual(float dt) { if (_inertia && !InTail) ApplyMotion(dt); }
        private void ApplyMotion(float dt)
        {
            float speed = _context.ExternalMotion.SelfVelocity.x, direction = _context.Input.Horizontal;
            bool wall = speed > 0 ? _context.Sensors.RightWall != null : speed < 0 && _context.Sensors.LeftWall != null;
            if (wall) { _context.Motor.SetHorizontalVelocity(0); _inertia = false; if (!InTail) _context.MotionControl.Release(this); return; }
            float limit = _context.JumpWindow.HasGroundSupport ? _context.Feel.MoveSpeed : _context.Feel.EffectiveAirSpeed;
            float target = direction * limit;
            float rate = direction != 0 && speed * direction < 0 ? _context.Feel.Attack.Dash.AirTurning : _context.Feel.Attack.Dash.AirBraking;
            float next = _inertia ? Mathf.MoveTowards(speed, target, rate * dt) :
                _context.Feel.CalculateHorizontalSpeed(speed, direction, _context.JumpWindow.HasGroundSupport, dt);
            _context.Motor.SetHorizontalVelocity(next);
            if (direction != 0) _context.Facing.SetFacing(direction > 0);
            if (_inertia && Mathf.Abs(next - target) < .0001f)
            { _inertia = false; if (!InTail) _context.MotionControl.Release(this); }
        }
        internal void LeaveTail()
        {
            if (!TailFinished) { Cancel(!_preserveInputOnExit); return; }
            InTail = false; _next = null; _context.Input.EndCombatOpportunity();
            if (_inertia) _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal);
            else _context.MotionControl.Release(this);
        }
        internal void Cancel(bool clearInput = true)
        {
            InDash = InTail = _inertia = false; _next = _pendingNext = null;
            _preserveInputOnExit = false;
            _context.MotionControl.Release(this);
            if (clearInput) _context.Input.EndCombatOpportunity();
        }
    }
}
