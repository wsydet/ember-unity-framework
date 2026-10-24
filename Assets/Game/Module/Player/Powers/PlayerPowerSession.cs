using System;
using System.Collections.Generic;
using Ember.Basic;
using Game.Entity;

namespace Game.Player
{
    /// <summary>玩家新游戏与支撑补充适配；公共基础不知道这些业务含义。</summary>
    public sealed class PlayerPowerSession : IDisposable
    {
        #region 内部参数
        private readonly EntityPowerHost _host;
        private readonly List<EntityPowerRuntime> _groundRefills = new();
        public PlayerPowerProgress Progress { get; }
        public EntityAbilitySet Abilities => _host.Abilities;
        public EntitySkillSet Skills => _host.Skills;
        public bool HasEntries => _host.HasEntries;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private PlayerPowerSession(PlayerPowerConfig config, EntityPowerSnapshot snapshot = null)
        {
            _host = snapshot != null ? EntityPowerHost.Restore(config, snapshot)
                : config == null ? EntityPowerHost.CreateEmpty() : EntityPowerHost.Create(config);
            Progress = new PlayerPowerProgress(_host.Availability);
            if (config == null) return;
            foreach (var slot in config.AbilitySlots)
                if (slot.Definition is PlayerAbilityDefinition definition && definition.RefillOnGroundSupport
                    && Abilities.TryGet(definition.Id, out var item)) _groundRefills.Add(item);
            foreach (var slot in config.SkillSlots)
                if (slot.Definition is PlayerSkillDefinition definition && definition.RefillOnGroundSupport
                    && Skills.TryGet(definition.Id, out var item)) _groundRefills.Add(item);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static PlayerPowerSession CreateNewGame(PlayerPowerConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new PlayerPowerSession(config);
        }
        // 未选装配置是明确的空装配，不是缺失存档字段的默认化。
        [HasGC]
        internal static PlayerPowerSession CreateEmpty() => new(null);
        internal void Tick(float gameSeconds) => _host.Tick(gameSeconds);
        internal void RefillGroundUses()
        { foreach (var item in _groundRefills) item.RefillUses(); }
        public void Dispose() => _host.Dispose();
        public EntityPowerSnapshot Capture() => _host.Capture();
        [HasGC]
        public static PlayerPowerSession Restore(PlayerPowerConfig config, EntityPowerSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return new PlayerPowerSession(config, snapshot);
        }
        #endregion
    }
}
