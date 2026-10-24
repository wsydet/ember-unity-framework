using System;

namespace Game.Player
{
    internal enum PlayerCombatButton { Dash, Jump, Attack }
    internal enum PlayerAttackIntent { EnvironmentStart, Normal, Delayed, DashAttack }

    /// <summary>每角色请求与机会的双截止。按下时固定意图，只有最终提交才消费。</summary>
    internal sealed class PlayerCombatRequests
    {
        #region 内部参数
        internal readonly struct Request
        {
            internal readonly ulong Version, Opportunity;
            internal readonly double PressedAt, ExpiresAt;
            internal readonly PlayerAttackIntent Intent;
            internal Request(ulong version, ulong opportunity, double pressedAt, double expiresAt, PlayerAttackIntent intent)
            { Version = version; Opportunity = opportunity; PressedAt = pressedAt; ExpiresAt = expiresAt; Intent = intent; }
        }
        private readonly Request?[] _pending = new Request?[3];
        private readonly ulong[] _seen = new ulong[3];
        private readonly float[] _buffers;
        private double _normalEnd = double.PositiveInfinity, _totalEnd = double.PositiveInfinity;
        private PlayerAttackIntent _normalIntent = PlayerAttackIntent.EnvironmentStart;
        internal double Time { get; private set; }
        internal ulong Opportunity { get; private set; }
        internal bool InCombat { get; private set; }
        internal string Observe(PlayerCombatButton button)
        {
            var pending = _pending[(int)button];
            if (!pending.HasValue) return button + "：无请求";
            var value = pending.Value;
            string reason = value.Opportunity != Opportunity ? "旧机会" : Time >= _totalEnd ? "机会关闭" :
                Time > value.ExpiresAt ? "缓存过期" : button == PlayerCombatButton.Attack &&
                value.Intent != PlayerAttackIntent.Delayed && Time >= _normalEnd ? "普通窗口关闭" :
                TryGet(button, out _) ? "等待最终FSM仲裁" : "当前步已过期";
            return $"{button} #{value.Version} · {value.Intent} · 机会 {value.Opportunity} · 剩余 {Math.Max(0, Math.Min(value.ExpiresAt, _totalEnd) - Time):F3}s · {reason}";
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCombatRequests(float dashBuffer, float jumpBuffer, float attackBuffer)
        {
            PlayerCombatData.NonNegative(dashBuffer, "Input.CombatDashBuffer");
            PlayerCombatData.NonNegative(jumpBuffer, "Input.CombatJumpBuffer");
            PlayerCombatData.NonNegative(attackBuffer, "Input.AttackBuffer");
            _buffers = new[] { dashBuffer, jumpBuffer, attackBuffer };
        }

        internal void Begin(PlayerAttackIntent intent, double normalSeconds, double totalSeconds)
        {
            if (double.IsNaN(normalSeconds) || double.IsNaN(totalSeconds) || normalSeconds < 0 || totalSeconds < normalSeconds)
                throw new ArgumentOutOfRangeException(nameof(totalSeconds));
            Clear(); Opportunity++; InCombat = true;
            _normalIntent = intent; _normalEnd = Time + normalSeconds; _totalEnd = Time + totalSeconds;
        }

        // 本体自然进入收刀等连续机会：不换 ID、不刷新按键截止，只更新机会边界。
        internal void Continue(PlayerAttackIntent intent, double normalSeconds, double totalSeconds)
        {
            if (!InCombat || double.IsNaN(normalSeconds) || double.IsNaN(totalSeconds) || normalSeconds < 0 || totalSeconds < normalSeconds)
                throw new InvalidOperationException("无效的战斗机会延续。");
            _normalIntent = intent; _normalEnd = Time + normalSeconds; _totalEnd = Time + totalSeconds;
        }

        internal void Press(PlayerCombatButton button, ulong version)
        {
            int index = (int)button;
            if (version == 0 || version <= _seen[index]) return;
            _seen[index] = version;
            var intent = Time < _normalEnd ? _normalIntent : PlayerAttackIntent.Delayed;
            double end = button == PlayerCombatButton.Attack && intent != PlayerAttackIntent.Delayed ? _normalEnd : _totalEnd;
            if (Time >= end) { _pending[index] = null; return; }
            _pending[index] = new Request(version, Opportunity, Time, Math.Min(Time + _buffers[index], end), intent);
        }

        internal bool TryGet(PlayerCombatButton button, out Request request)
        {
            var value = _pending[(int)button]; request = value.GetValueOrDefault();
            if (!value.HasValue || request.Opportunity != Opportunity) return false;
            // 零缓冲仍有按下当步的一次决策机会；机会本身始终左闭右开。
            if (Time >= _totalEnd || Time > request.ExpiresAt || Time == request.ExpiresAt && Time != request.PressedAt) return false;
            if (button == PlayerCombatButton.Attack && request.Intent != PlayerAttackIntent.Delayed && Time >= _normalEnd) return false;
            return true;
        }

        internal bool Commit(PlayerCombatButton button, ulong version)
        {
            if (!TryGet(button, out var request) || request.Version != version) return false;
            Clear(); return true;
        }

        internal void Advance(float dt)
        {
            if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            Time += dt;
        }
        internal void Clear() => Array.Clear(_pending, 0, _pending.Length);
        internal void End()
        {
            Clear(); Opportunity++; InCombat = false;
            _normalIntent = PlayerAttackIntent.EnvironmentStart;
            _normalEnd = _totalEnd = double.PositiveInfinity;
        }
        #endregion
    }
}
