using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>宿主独占攻击/收刀会话。请求提交在 FSM 最终预检之后，运动与时钟只在固定步推进。</summary>
    internal sealed class PlayerCombatMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly PlayerCombatConfiguration _config;
        private readonly Dictionary<string, PlayerAttackSegment> _segments;
        private readonly Dictionary<string, string> _nodeSegments;
        private readonly HashSet<string> _delayedNodes = new(StringComparer.Ordinal);
        private EntityEffectHandle _prompt;
        private readonly PlayerActionClock _clock = new();
        private string _committed;
        private bool _preserveExit;
        internal PlayerAttackSegment Segment { get; private set; }
        internal bool IsActive { get; private set; }
        internal bool IsRecovery { get; private set; }
        internal bool IsDelayedRecovery { get; private set; }
        internal PlayerCombatDashMotion Dash { get; }
        internal PlayerAttackHitSession Hits { get; set; }
        internal int Direction { get; private set; }
        internal double Progress => _clock.Current;
        internal float AnimationSpeed => IsActive && !IsRecovery ? Segment.Speed : 1;
        internal bool Finished => _clock.Reached(IsRecovery ? IsDelayedRecovery ? Recovery.DelayedTotalDuration : Recovery.NormalDuration : Segment.Duration);
        private PlayerAttackRecovery Recovery => _context.Feel.Attack.Recoveries.First(r => r.Id == Segment.RecoveryId);
        internal string Next => Segment == null ? null : _config.Combo.Connections.FirstOrDefault(c => c.Source == Segment.Id)?.Normal;
        internal string Delayed => Segment == null ? null : _config.Combo.Connections.FirstOrDefault(c => c.Source == Segment.Id && c.DelayedEnabled)?.Delayed;
        internal bool CanSelectDelayed => !string.IsNullOrEmpty(Delayed) && _segments.TryGetValue(Delayed, out var target) && target.Enabled &&
            _context.Abilities.TryGet(target.AbilityId, out var ability) && ability.IsEnabled && ability.IsOpen;
        internal bool DelayedWindow => IsActive && IsRecovery && IsDelayedRecovery &&
            _clock.Reached(Recovery.DelayedNormalDuration) && !_clock.Reached(Recovery.DelayedTotalDuration);
        internal string Entry => _context.JumpWindow.HasGroundSupport ? _config.Combo.GroundEntry : _config.Combo.AirEntry;
        internal bool CanOfferEntry => !IsActive && Dash?.InTail != true && !_context.Dash.IsActive && !_context.Ledge.IsActive &&
            !_context.DropThrough.IsActive && !_context.Landing.IsActive && !_context.Rolling.IsActive;
        internal bool EnvironmentLost => IsActive && !EnvironmentAllows(Segment);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool EnvironmentAllows(PlayerAttackSegment segment) => segment.Environment == PlayerAttackEnvironment.Both ||
            (segment.Environment == PlayerAttackEnvironment.Ground) == _context.JumpWindow.HasGroundSupport;
        private string RequestedSegment()
        {
            if (!_context.Input.Combat.TryGet(PlayerCombatButton.Attack, out var request)) return null;
            if (Dash?.OpportunityActive == true)
                return Dash.Gate && request.Intent == PlayerAttackIntent.DashAttack ? Dash.Target : null;
            if (!IsActive) return request.Intent == PlayerAttackIntent.EnvironmentStart ? Entry : null;
            if (IsDelayedRecovery && request.Intent == PlayerAttackIntent.Delayed)
                return DelayedWindow ? Delayed : null;
            if (IsDelayedRecovery && _clock.Reached(Recovery.DelayedNormalDuration)) return null;
            if (IsRecovery && string.IsNullOrEmpty(Next))
                return request.Intent == PlayerAttackIntent.EnvironmentStart ? Entry : null;
            return request.Intent == PlayerAttackIntent.Normal ? Next : null;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCombatMotion(PlayerContext context, PlayerCombatConfiguration config)
        {
            _context = context; _config = config ?? throw new ArgumentNullException(nameof(config));
            _segments = context.Feel.Attack.Segments.ToDictionary(s => s.Id, StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(config.Graph.DashRouteNodeId)) Dash = new PlayerCombatDashMotion(context, config, _segments);
            _nodeSegments = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in config.Graph.Nodes)
            {
                _nodeSegments.Add(pair.AttackNodeId, pair.SegmentId); _nodeSegments.Add(pair.RecoveryNodeId, pair.SegmentId);
                if (!string.IsNullOrEmpty(pair.DelayedRecoveryNodeId))
                { _nodeSegments.Add(pair.DelayedRecoveryNodeId, pair.SegmentId); _delayedNodes.Add(pair.DelayedRecoveryNodeId); }
            }
            if (config.DelayedAttackPrompt != null && context.Effects == null)
                throw new InvalidOperationException("冻结配置依赖停顿提示效果，但当前会话未装配效果容器。");
        }
        internal string SegmentForNode(string node) => _nodeSegments[node];
        internal string VisualNode(string node) => IsActive && IsRecovery && Segment.Motion == PlayerAttackMotion.Moving &&
            _context.Input.Horizontal != 0 ? "move" : node;
        internal bool CanAttack(string id)
        {
            if (string.IsNullOrEmpty(id) || !_context.Feel.Attack.Enabled || !_segments.TryGetValue(id, out var segment) ||
                !segment.Enabled || !EnvironmentAllows(segment) || RequestedSegment() != id) return false;
            return _context.Abilities.Query(segment.AbilityId) == EntityUseStatus.Available;
        }
        internal bool Gate(PlayerCombatButton button) => IsRecovery || (button switch
        {
            PlayerCombatButton.Dash => Segment.Dash.Enabled && _clock.Reached(Segment.Dash.OpensAt),
            PlayerCombatButton.Jump => Segment.Jump.Enabled && _clock.Reached(Segment.Jump.OpensAt),
            _ => Segment.Attack.Enabled && _clock.Reached(Segment.Attack.OpensAt)
        });
        internal bool MoveCancel => Segment.Motion == PlayerAttackMotion.Stationary && _context.Input.Horizontal != 0 &&
            (IsRecovery || Segment.Move.Enabled && _clock.Reached(Segment.Move.OpensAt));
        internal void CommitAttack(string id)
        {
            if (!CanAttack(id) || !_context.Abilities.TryGet(_segments[id].AbilityId, out var ability) ||
                ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available || !_context.Input.ConsumeAttack())
                throw new InvalidOperationException("攻击提交条件失效：" + id);
            _committed = id; _preserveExit = true;
        }
        internal void PreserveForTransition() => _preserveExit = true;
        internal void PrepareDash() { Dash?.Carry(Next); _preserveExit = true; }
        internal void BeginAttack(string node)
        {
            string id = SegmentForNode(node);
            if (_committed != id) throw new InvalidOperationException("攻击必须经过已声明的 FSM 提交：" + id);
            Dash?.Cancel();
            _committed = null; Segment = _segments[id]; IsActive = true; IsRecovery = false; IsDelayedRecovery = false; _preserveExit = false;
            _clock.Reset(Segment.Speed);
            Direction = _context.Input.Horizontal != 0 ? (_context.Input.Horizontal > 0 ? 1 : -1) : _context.Facing.FacesRight ? 1 : -1;
            _context.Facing.SetFacing(Direction > 0); _context.Wall.Cancel(); _context.VerticalMotion.NotifyJump();
            Hits?.Begin(Segment, Direction, _context.Motor.Position);
            var velocity = _context.Motor.Velocity;
            if (Segment.EntryVelocity == PlayerAttackEntryVelocity.Scale) velocity = Vector2.Scale(velocity, Segment.Velocity);
            else if (Segment.EntryVelocity == PlayerAttackEntryVelocity.Set) velocity = new Vector2(Direction * Segment.Velocity.x, Segment.Velocity.y);
            _context.Motor.SetHorizontalVelocity(velocity.x); _context.Motor.SetVerticalVelocity(velocity.y);
            _context.Input.Combat.Begin(PlayerAttackIntent.Normal, double.PositiveInfinity, double.PositiveInfinity);
            _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal | PlayerMotionChannels.Facing |
                PlayerMotionChannels.AutomaticTransitions | PlayerMotionChannels.Wall | PlayerMotionChannels.Ledge | PlayerMotionChannels.Correction);
        }
        internal bool CanRecover(string node) => IsActive && !IsRecovery && SegmentForNode(node) == Segment.Id &&
            (!_delayedNodes.Contains(node) || CanSelectDelayed);
        internal void BeginRecovery(string node)
        {
            Segment = _segments[SegmentForNode(node)]; IsActive = true; IsRecovery = true; _preserveExit = false; _clock.Reset();
            IsDelayedRecovery = _delayedNodes.Contains(node);
            double normal = IsDelayedRecovery ? Recovery.DelayedNormalDuration : Recovery.NormalDuration;
            double total = IsDelayedRecovery ? Recovery.DelayedTotalDuration : Recovery.NormalDuration;
            if (string.IsNullOrEmpty(Next)) _context.Input.Combat.Begin(PlayerAttackIntent.EnvironmentStart, normal, total);
            else _context.Input.Combat.Continue(PlayerAttackIntent.Normal, normal, total);
            _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal | PlayerMotionChannels.AutomaticTransitions |
                PlayerMotionChannels.Wall | PlayerMotionChannels.Ledge | PlayerMotionChannels.Correction);
            SyncPrompt();
        }
        internal void Advance(float dt)
        {
            _clock.Advance(dt);
            if (!IsRecovery) Hits?.Sample(_clock.Previous, _clock.Current, _context.Motor.Position);
            float horizontal = 0;
            if (Segment.Motion == PlayerAttackMotion.Moving)
            {
                horizontal = Mathf.MoveTowards(_context.Motor.Velocity.x, _context.Input.Horizontal * Segment.MoveSpeed, Segment.Acceleration * dt);
                if (IsRecovery && _context.Input.Horizontal != 0) _context.Facing.SetFacing(_context.Input.Horizontal > 0);
            }
            _context.Motor.SetHorizontalVelocity(horizontal);
            SyncPrompt();
        }
        private void SyncPrompt()
        {
            bool eligible = DelayedWindow && !string.IsNullOrEmpty(Delayed) && _segments.TryGetValue(Delayed, out var target) &&
                target.Enabled && EnvironmentAllows(target) && _context.Abilities.Query(target.AbilityId) == EntityUseStatus.Available;
            if (!eligible) { _prompt?.Dispose(); _prompt = null; }
            else if (_config.DelayedAttackPrompt != null && (_prompt == null || !_prompt.IsActive))
                _prompt = _context.Effects.ApplyCaptured(_config.DelayedAttackPrompt, "player-delayed-attack:" + _context.Input.Combat.Opportunity);
        }
        internal void Leave()
        {
            _prompt?.Dispose(); _prompt = null;
            _context.MotionControl.Release(this); IsActive = false;
            if (!_preserveExit) { _context.Input.EndCombatOpportunity(); _committed = null; }
            _preserveExit = false;
        }
        #endregion
    }
}
