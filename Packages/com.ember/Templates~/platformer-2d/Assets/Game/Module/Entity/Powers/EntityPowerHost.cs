using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Game.Entity
{
    /// <summary>Ability 与 Skill 分别持有独立 ID 空间；共享的仅是小型资源实现。</summary>
    public sealed class EntityAbilitySet
    {
        #region 内部参数
        private readonly Dictionary<string, EntityPowerRuntime> _items;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EntityAbilitySet(Dictionary<string, EntityPowerRuntime> items) => _items = items;
        public bool TryGet(string id, out EntityPowerRuntime ability)
        { ability = null; return id != null && _items.TryGetValue(id, out ability); }
        public EntityUseStatus Query(string id, bool environmentAllowed = true, bool prohibited = false)
            => TryGet(id, out var item) ? item.Query(environmentAllowed, prohibited) : EntityUseStatus.NotInstalled;
        #endregion
    }

    public sealed class EntitySkillSet
    {
        #region 内部参数
        private readonly Dictionary<string, EntityPowerRuntime> _items;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EntitySkillSet(Dictionary<string, EntityPowerRuntime> items) => _items = items;
        public bool TryGet(string id, out EntityPowerRuntime skill)
        { skill = null; return id != null && _items.TryGetValue(id, out skill); }
        public EntityUseStatus Query(string id, bool environmentAllowed = true, bool prohibited = false)
            => TryGet(id, out var item) ? item.Query(environmentAllowed, prohibited) : EntityUseStatus.NotInstalled;
        #endregion
    }

    /// <summary>可选宿主能力容器；显式创建默认资源，宿主负责游戏时间、开放规则及清理，不注册更新。</summary>
    public sealed class EntityPowerHost : IDisposable
    {
        #region 内部参数
        private readonly Dictionary<string, EntityPowerRuntime> _abilities = new(StringComparer.Ordinal);
        private readonly Dictionary<string, EntityPowerRuntime> _skills = new(StringComparer.Ordinal);
        private bool _disposed;
        public EntityPowerAvailability Availability { get; } = new();
        public EntityAbilitySet Abilities { get; }
        public EntitySkillSet Skills { get; }
        public bool HasEntries => _abilities.Count != 0 || _skills.Count != 0;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EntityPowerHost(EntityPowerSpec[] abilities, EntityPowerSpec[] skills)
        {
            foreach (var spec in abilities) _abilities.Add(spec.Id, new EntityPowerRuntime(spec, Availability, false));
            foreach (var spec in skills) _skills.Add(spec.Id, new EntityPowerRuntime(spec, Availability, true));
            Abilities = new EntityAbilitySet(_abilities); Skills = new EntitySkillSet(_skills);
        }

        #endregion
        // --------------------------------------------------------
        #region 外部方法
        // 只允许会话驱动者调用；不注册 MonoBehaviour Update，也不依赖固定物理步长。
        public void Tick(float gameSeconds)
        {
            if (_disposed) return;
            if (!float.IsFinite(gameSeconds) || gameSeconds < 0) throw new ArgumentOutOfRangeException(nameof(gameSeconds));
            foreach (var item in _skills.Values) item.Tick(gameSeconds);
        }

        [HasGC]
        public static EntityPowerHost CreateEmpty() => new(Array.Empty<EntityPowerSpec>(), Array.Empty<EntityPowerSpec>());

        [HasGC]
        public static EntityPowerHost Create(EntityPowerConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            // 两类配置全部通过验证后才建立运行数据，失败不留下部分会话。
            var abilities = config.CaptureAbilities();
            var skills = config.CaptureSkills();
            return new EntityPowerHost(abilities, skills);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var item in _abilities.Values) item.Dispose();
            foreach (var item in _skills.Values) item.Dispose();
        }
        public EntityPowerSnapshot Capture()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntityPowerHost));
            var abilities = new EntityPowerRecord[_abilities.Count];
            var skills = new EntityPowerRecord[_skills.Count]; int index = 0;
            foreach (var item in _abilities.Values) abilities[index++] = item.Capture();
            index = 0;
            foreach (var item in _skills.Values) skills[index++] = item.Capture();
            return new EntityPowerSnapshot(abilities, skills);
        }
        /// <summary>只构造新宿主，全部校验后才应用；不会重用旧尝试或改动现有运行宿主。</summary>
        [HasGC]
        public static EntityPowerHost Restore(EntityPowerConfig config, EntityPowerSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var host = config == null ? CreateEmpty() : Create(config);
            try
            {
                if (host._abilities.Count != snapshot.AbilityCount || host._skills.Count != snapshot.SkillCount)
                    throw new ArgumentException("能力快照装配集合不一致，不能默认补充或丢弃资源。");
                for (int kind = 0; kind < 2; kind++)
                {
                    var items = kind == 0 ? host._abilities : host._skills;
                    int count = kind == 0 ? snapshot.AbilityCount : snapshot.SkillCount;
                    for (int i = 0; i < count; i++)
                    {
                        var record = kind == 0 ? snapshot.GetAbility(i) : snapshot.GetSkill(i);
                        if (!items.TryGetValue(record.Id, out var runtime)) throw new ArgumentException("未知能力 ID：" + record.Id);
                        runtime.ValidateRestore(record);
                    }
                }
                for (int i = 0; i < snapshot.AbilityCount; i++)
                { var record = snapshot.GetAbility(i); host._abilities[record.Id].ApplyRestore(record); }
                for (int i = 0; i < snapshot.SkillCount; i++)
                { var record = snapshot.GetSkill(i); host._skills[record.Id].ApplyRestore(record); }
                return host;
            }
            catch { host.Dispose(); throw; }
        }
        #endregion
    }
}


