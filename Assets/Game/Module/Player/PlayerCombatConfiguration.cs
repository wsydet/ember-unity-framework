using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerDamageMode { Payload, Fixed }
    public enum PlayerDeathMode { NonLethal, ConfirmRecovery, Custom }
    public enum PlayerRecoveryHealthMode { Full, Fixed, Ratio, Saved }
    public enum PlayerRecoveryPositionMode { Checkpoint, Spawn, Custom }

    [Serializable]
    public sealed class PlayerCombo
    {
        [SerializeField] private string _groundEntry, _airEntry, _dashEntry;
        [SerializeField] private PlayerComboConnection[] _connections = Array.Empty<PlayerComboConnection>();
        public string GroundEntry => _groundEntry;
        public string AirEntry => _airEntry;
        public string DashEntry => _dashEntry;
        public IReadOnlyList<PlayerComboConnection> Connections => Array.AsReadOnly(_connections);
        internal void Validate(PlayerAttackProfile attack)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in attack.Segments) ids.Add(segment.Id);
            Target(_groundEntry, ids, "Combo.GroundEntry"); Target(_airEntry, ids, "Combo.AirEntry"); Target(_dashEntry, ids, "Combo.DashEntry");
            if (_connections == null) throw new InvalidOperationException("Combo.Connections：集合为空。");
            var sources = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in _connections)
            {
                if (connection == null || !ids.Contains(connection.Source) || !sources.Add(connection.Source))
                    throw new InvalidOperationException("Combo.Connections：来源为空、悬空或重复。");
                Target(connection.Normal, ids, "Combo[" + connection.Source + "].Normal");
                Target(connection.Delayed, ids, "Combo[" + connection.Source + "].Delayed");
                if (connection.DelayedEnabled && string.IsNullOrEmpty(connection.Delayed))
                    throw new InvalidOperationException("Combo[" + connection.Source + "].Delayed：已开启但没有目标。");
            }
            if (attack.Enabled && string.IsNullOrEmpty(_groundEntry) && string.IsNullOrEmpty(_airEntry))
                throw new InvalidOperationException("Combo：攻击开启时至少需要一个环境起手。");
            if (attack.Enabled && attack.Dash.Mode == PlayerDashAttackMode.Dedicated && string.IsNullOrEmpty(_dashEntry))
                throw new InvalidOperationException("Combo.DashEntry：指定冲刺攻击模式缺少目标。");
        }
        private void Target(string id, HashSet<string> ids, string path)
        { if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) throw new InvalidOperationException(path + "：悬空攻击 ID " + id); }
    }

    [Serializable]
    public sealed class PlayerComboConnection
    {
        [SerializeField] private string _source, _normal, _delayed;
        [SerializeField] private bool _delayedEnabled;
        public string Source => _source;
        public string Normal => _normal;
        public string Delayed => _delayed;
        public bool DelayedEnabled => _delayedEnabled;
    }

    [Serializable]
    public sealed class PlayerLifeConfiguration
    {
        [SerializeField] private bool _enabled, _showHealth;
        [SerializeField] private float _maximum = 100, _initial = 100, _multiplier = 1, _reduction, _fixedDamage = 1;
        [SerializeField] private PlayerDamageMode _damageMode;
        [SerializeField] private PlayerDeathMode _death = PlayerDeathMode.ConfirmRecovery;
        [SerializeField] private string _customDeathHandler;
        public bool Enabled => _enabled;
        public bool ShowHealth => _showHealth;
        public float Maximum => _maximum;
        public float Initial => _initial;
        public float Multiplier => _multiplier;
        public float Reduction => _reduction;
        public float FixedDamage => _fixedDamage;
        public PlayerDamageMode DamageMode => _damageMode;
        public PlayerDeathMode Death => _death;
        public string CustomDeathHandler => _customDeathHandler;
        internal void Validate()
        {
            PlayerCombatData.Positive(_maximum, "Life.Maximum"); PlayerCombatData.Range(_initial, 0, _maximum, "Life.Initial");
            PlayerCombatData.NonNegative(_multiplier, "Life.Multiplier"); PlayerCombatData.NonNegative(_reduction, "Life.Reduction");
            PlayerCombatData.NonNegative(_fixedDamage, "Life.FixedDamage");
            PlayerCombatData.Enum(_damageMode, "Life.DamageMode"); PlayerCombatData.Enum(_death, "Life.Death");
            if (_enabled && _death == PlayerDeathMode.Custom) PlayerCombatData.Id(_customDeathHandler, "Life.CustomDeathHandler");
            if (_showHealth && !_enabled) throw new InvalidOperationException("Life.ShowHealth：血条要求显式装配生命。");
        }
    }

    [Serializable]
    public sealed class PlayerReactionConfiguration
    {
        [SerializeField] private bool _enabled, _animation = true, _invulnerability = true, _protectReaction = true, _flash = true;
        [SerializeField] private float _invulnerabilitySeconds = .5f;
        [SerializeField] private float _flashAlpha = .3f, _flashHz = 8;
        [SerializeField] private string[] _damageCategories = { "physical" };
        public bool Enabled => _enabled;
        public bool Animation => _animation;
        public bool Invulnerability => _invulnerability;
        public bool ProtectReaction => _protectReaction;
        public bool Flash => _flash;
        public float InvulnerabilitySeconds => _invulnerabilitySeconds;
        public float FlashAlpha => _flashAlpha;
        public float FlashHz => _flashHz;
        public IReadOnlyList<string> DamageCategories => Array.AsReadOnly(_damageCategories);
        internal void Validate()
        {
            PlayerCombatData.NonNegative(_invulnerabilitySeconds, "Reactions.InvulnerabilitySeconds");
            PlayerCombatData.Range(_flashAlpha, 0, 1, "Reactions.FlashAlpha"); PlayerCombatData.NonNegative(_flashHz, "Reactions.FlashHz");
            if (_damageCategories == null) throw new InvalidOperationException("Reactions.DamageCategories：集合为空。");
            foreach (var category in _damageCategories) PlayerCombatData.Id(category, "Reactions.DamageCategories");
        }
    }

    [Serializable]
    public sealed class PlayerRecoveryConfiguration
    {
        [SerializeField] private PlayerRecoveryHealthMode _health;
        [SerializeField] private PlayerRecoveryPositionMode _position;
        [SerializeField] private float _fixedHealth = 100, _healthRatio = 1;
        [SerializeField] private string _customPositionProvider;
        public PlayerRecoveryHealthMode Health => _health;
        public PlayerRecoveryPositionMode Position => _position;
        public float FixedHealth => _fixedHealth;
        public float HealthRatio => _healthRatio;
        public string CustomPositionProvider => _customPositionProvider;
        internal void Validate()
        {
            PlayerCombatData.Enum(_health, "Recovery.Health"); PlayerCombatData.Enum(_position, "Recovery.Position");
            PlayerCombatData.NonNegative(_fixedHealth, "Recovery.FixedHealth"); PlayerCombatData.Range(_healthRatio, 0, 1, "Recovery.HealthRatio");
            if (_position == PlayerRecoveryPositionMode.Custom) PlayerCombatData.Id(_customPositionProvider, "Recovery.CustomPositionProvider");
        }
    }

    /// <summary>会话冻结的纯值装配；运行时不回读方案。HP、输入、时间和命中记录不属于这里。</summary>
    internal sealed class PlayerCombatConfiguration
    {
        internal PlayerCombo Combo { get; }
        internal PlayerCombatGraphMap Graph { get; }
        internal PlayerLifeConfiguration Life { get; }
        internal PlayerReactionConfiguration Reactions { get; }
        internal PlayerRecoveryConfiguration Recovery { get; }
        internal Game.Entity.EntityEffectSpec DelayedAttackPrompt { get; }
        internal Game.Entity.EntityEffectSpec DamageProtection { get; }
        private Game.Entity.EntityEffectSpec CaptureProtection(bool effectsEnabled)
        {
            if (!Life.Enabled || !Reactions.Invulnerability || Reactions.InvulnerabilitySeconds == 0) return null;
            if (!effectsEnabled) throw new InvalidOperationException("受击无敌要求显式启用效果容器。");
            return new Game.Entity.EntityEffectSpec("player-damage-protection", Game.Entity.EntityEffectKind.Buff,
                Game.Entity.EntityEffectLifetime.Timed, Reactions.InvulnerabilitySeconds,
                new Game.Entity.EntityEffectRules(System.Linq.Enumerable.ToArray(Reactions.DamageCategories), Reactions.ProtectReaction, 0),
                new Game.Entity.EntityEffectAppearance(Reactions.Flash, 100, Reactions.FlashAlpha, Reactions.FlashHz));
        }
        internal PlayerCombatConfiguration(PlayerScheme scheme, PlayerAttackProfile attack)
        {
            if (scheme != null && (scheme.Combo == null || scheme.CombatGraph == null || scheme.Life == null || scheme.Reactions == null || scheme.Recovery == null))
                throw new InvalidOperationException("Scheme.Combat：Combo、Life、Reactions、Recovery 必须显式存在。");
            Combo = PlayerCombatData.Copy(scheme?.Combo) ?? new PlayerCombo();
            Graph = PlayerCombatData.Copy(scheme?.CombatGraph) ?? new PlayerCombatGraphMap();
            Life = PlayerCombatData.Copy(scheme?.Life) ?? new PlayerLifeConfiguration();
            Reactions = PlayerCombatData.Copy(scheme?.Reactions) ?? new PlayerReactionConfiguration();
            Recovery = PlayerCombatData.Copy(scheme?.Recovery) ?? new PlayerRecoveryConfiguration();
            if (scheme != null && scheme.DelayedAttackPrompt != null)
            {
                if (!scheme.EnableEffects || scheme.DelayedAttackPrompt.Lifetime != Game.Entity.EntityEffectLifetime.Owned)
                    throw new InvalidOperationException("停顿提示要求显式启用效果容器并使用 Owned 生命周期。");
                DelayedAttackPrompt = scheme.DelayedAttackPrompt.Capture();
            }
            Combo.Validate(attack); Life.Validate(); Reactions.Validate(); Recovery.Validate();
            DamageProtection = CaptureProtection(scheme != null && scheme.EnableEffects);
        }
        internal PlayerCombatConfiguration(PlayerCombatConfiguration source, PlayerAttackProfile attack)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Combo = PlayerCombatData.Copy(source.Combo);
            Graph = PlayerCombatData.Copy(source.Graph);
            Life = PlayerCombatData.Copy(source.Life);
            Reactions = PlayerCombatData.Copy(source.Reactions);
            Recovery = PlayerCombatData.Copy(source.Recovery);
            DelayedAttackPrompt = source.DelayedAttackPrompt;
            DamageProtection = source.DamageProtection;
            Combo.Validate(attack); Life.Validate(); Reactions.Validate(); Recovery.Validate();
        }
    }
}
