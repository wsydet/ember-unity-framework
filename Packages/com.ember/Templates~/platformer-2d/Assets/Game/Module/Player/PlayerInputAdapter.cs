using System;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerInputAdapter : IDisposable
    {
        #region 内部参数
        private readonly GameInputChannel _channel;
        private readonly string _move;
        private readonly GameInputButton _jump;
        private readonly GameInputButton _dash;
        private readonly GameInputButton _attack;
        private bool _jumpRequested, _dashRequested;
        private bool _externallySuppressed;
        internal PlayerCombatRequests Combat { get; }
        private readonly float _bufferDuration;
        private float _bufferRemaining;
        public float BufferRemaining => _bufferRemaining;
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld => !_externallySuppressed && _jump.IsHeld;
        public bool JumpRequested
        {
            get => Combat?.InCombat == true ? Combat.TryGet(PlayerCombatButton.Jump, out _) : _jumpRequested;
            private set => _jumpRequested = value;
        }
        public bool FreshJumpRequested { get; private set; }
        public ulong JumpPressVersion { get; private set; }
        public ulong ClearVersion { get; private set; }
        public bool DashRequested
        {
            get => Combat?.InCombat == true ? Combat.TryGet(PlayerCombatButton.Dash, out _) : _dashRequested;
            private set => _dashRequested = value;
        }
        public ulong DashPressVersion { get; private set; }
        public ulong AttackPressVersion { get; private set; }
        public bool AttackRequested => Combat != null && Combat.TryGet(PlayerCombatButton.Attack, out _);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnAvailability(GameInputChannel channel)
        { if (!channel.IsAvailable) Clear(); }
        private void CapturePress()
        {
            if (_externallySuppressed) return;
            if (!_jump.ConsumePress()) return;
            JumpRequested = true; FreshJumpRequested = true;
            _bufferRemaining = _bufferDuration;
            JumpPressVersion++;
            if (Combat?.InCombat == true) Combat.Press(PlayerCombatButton.Jump, JumpPressVersion);
        }
        private void CaptureDashPress()
        {
            if (_externallySuppressed) return;
            if (_dash == null || !_dash.ConsumePress()) return;
            DashRequested = true; DashPressVersion++;
            if (Combat?.InCombat == true) Combat.Press(PlayerCombatButton.Dash, DashPressVersion);
        }
        private void CaptureAttackPress()
        {
            if (_externallySuppressed) return;
            if (_attack == null || !_attack.ConsumePress()) return;
            AttackPressVersion++; Combat.Press(PlayerCombatButton.Attack, AttackPressVersion);
        }
        private void ClearCommittedPeers()
        {
            JumpRequested = false; FreshJumpRequested = false; DashRequested = false; _bufferRemaining = 0;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerInputAdapter(GameInputChannel channel, string move, string jump, float bufferDuration = 0,
            string dash = null, string attack = null, PlayerFeelRuntime combatFeel = null, bool reactionBuffer = false)
        {
            if (!float.IsFinite(bufferDuration) || bufferDuration < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferDuration));
            _channel = channel; _move = move;
            _bufferDuration = bufferDuration;
            channel.GetAction(move);
            if (!string.IsNullOrEmpty(dash)) channel.GetAction(dash);
            if (!string.IsNullOrEmpty(attack) || reactionBuffer)
            {
                if (!string.IsNullOrEmpty(attack)) channel.GetAction(attack);
                if (combatFeel == null) throw new ArgumentNullException(nameof(combatFeel));
                Combat = new PlayerCombatRequests(combatFeel.CombatDashBufferDuration, combatFeel.CombatJumpBufferDuration,
                    combatFeel.AttackBufferDuration);
            }
            _jump = new GameInputButton(channel, jump);
            try
            {
                if (!string.IsNullOrEmpty(dash)) _dash = new GameInputButton(channel, dash);
                if (!string.IsNullOrEmpty(attack)) _attack = new GameInputButton(channel, attack);
            }
            catch { _jump.Dispose(); _dash?.Dispose(); _attack?.Dispose(); throw; }
            channel.AvailabilityChanged += OnAvailability;
        }
        public void Sample()
        {
            if (_externallySuppressed) { Clear(); return; }
            var direction = _channel.ReadContinuous<Vector2>(_move);
            Horizontal = Mathf.Clamp(direction.x, -1, 1);
            Vertical = Mathf.Clamp(direction.y, -1, 1);
            CapturePress();
            CaptureDashPress();
            CaptureAttackPress();
        }
        public bool ConsumeJump()
        {
            CapturePress();
            bool requested = JumpRequested;
            if (requested && Combat?.InCombat == true)
            { requested = Combat.Commit(PlayerCombatButton.Jump, JumpPressVersion); ClearCommittedPeers(); Combat.End(); }
            else if (requested && Combat != null) EndCombatOpportunity();
            JumpRequested = false; FreshJumpRequested = false;
            _bufferRemaining = 0;
            return requested;
        }
        public bool ConsumeDash()
        {
            CaptureDashPress();
            bool requested = DashRequested;
            if (requested && Combat?.InCombat == true)
            { requested = Combat.Commit(PlayerCombatButton.Dash, DashPressVersion); ClearCommittedPeers(); Combat.End(); }
            else if (requested && Combat != null) EndCombatOpportunity();
            DashRequested = false; return requested;
        }
        internal bool ConsumeAttack()
        {
            bool consumed = Combat != null && Combat.Commit(PlayerCombatButton.Attack, AttackPressVersion);
            if (consumed) ClearCommittedPeers();
            return consumed;
        }
        internal void EndCombatOpportunity() { ClearCommittedPeers(); Combat?.End(); }
        private ulong? _finishingReactionOpportunity;
        internal void BeginReactionBuffer()
        {
            _finishingReactionOpportunity = null;
            Combat?.Begin(PlayerAttackIntent.EnvironmentStart, double.PositiveInfinity, double.PositiveInfinity);
        }
        internal void FinishReactionBuffer() => _finishingReactionOpportunity = Combat?.Opportunity;
        // 只按角色实际执行的物理步计时；零窗口仍保留首次物理决策机会。
        public void EndPhysicsStep(float dt)
        {
            if (_finishingReactionOpportunity.HasValue && Combat != null)
            {
                if (Combat.Opportunity != _finishingReactionOpportunity.Value) _finishingReactionOpportunity = null;
                else if (!Combat.TryGet(PlayerCombatButton.Dash, out _) && !Combat.TryGet(PlayerCombatButton.Jump, out _) && !Combat.TryGet(PlayerCombatButton.Attack, out _))
                { Combat.End(); _finishingReactionOpportunity = null; }
            }
            _bufferRemaining = Mathf.Max(0, _bufferRemaining - dt);
            if (_bufferRemaining <= 0.000001f) JumpRequested = false;
            FreshJumpRequested = false; // 下穿只使用首个物理决策，不继承跳跃缓冲。
            // 冲刺无额外缓冲窗口：只保留到首个物理决策，拒绝后不在 CD 结束时补发。
            DashRequested = false;
            Combat?.Advance(dt);
        }
        public void Clear()
        {
            _finishingReactionOpportunity = null;
            Horizontal = 0; Vertical = 0; JumpRequested = false; FreshJumpRequested = false; _bufferRemaining = 0;
            DashRequested = false;
            ClearVersion++; _jump.ClearPending(); _dash?.ClearPending(); _attack?.ClearPending(); Combat?.Clear();
        }
        internal void SetExternalSuppression(bool suppressed)
        {
            if (_externallySuppressed == suppressed) return;
            _externallySuppressed = suppressed;
            Clear();
        }
        public void Dispose() { _channel.AvailabilityChanged -= OnAvailability; _jump.Dispose(); _dash?.Dispose(); _attack?.Dispose(); Clear(); Combat?.End(); }
        #endregion
    }
}
