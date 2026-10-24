using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entity
{
    [Serializable]
    public sealed class EntityAbilitySlot
    {
        #region 编辑器面板参数
        [SerializeField] private EntityAbilityDefinition _definition;
        [SerializeField] private bool _enabled = true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityAbilityDefinition Definition => _definition;
        public bool Enabled => _enabled;
        public EntityAbilitySlot(EntityAbilityDefinition definition, bool enabled = true)
        { _definition = definition; _enabled = enabled; }
        #endregion
    }

    [Serializable]
    public sealed class EntitySkillSlot
    {
        #region 编辑器面板参数
        [SerializeField] private EntitySkillDefinition _definition;
        [SerializeField] private bool _enabled = true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntitySkillDefinition Definition => _definition;
        public bool Enabled => _enabled;
        public EntitySkillSlot(EntitySkillDefinition definition, bool enabled = true)
        { _definition = definition; _enabled = enabled; }
        #endregion
    }

    /// <summary>项目能力装配。Enabled 不代表解锁；未装配 ID 不创建运行对象。</summary>
    [CreateAssetMenu(menuName = "Game/Entity/Power Config", fileName = "EntityPowers")]
    public class EntityPowerConfig : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private EntityAbilitySlot[] _abilities = Array.Empty<EntityAbilitySlot>();
        [SerializeField] private EntitySkillSlot[] _skills = Array.Empty<EntitySkillSlot>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public System.Collections.Generic.IReadOnlyList<EntityAbilitySlot> AbilitySlots => _abilities;
        public System.Collections.Generic.IReadOnlyList<EntitySkillSlot> SkillSlots => _skills;
        /// <summary>派生配置构造独立装配时使用；不修改已有宿主。</summary>
        protected void ConfigureSlots(EntityAbilitySlot[] abilities, EntitySkillSlot[] skills)
        { _abilities = abilities; _skills = skills; }
        internal EntityPowerSpec[] CaptureAbilities()
        {
            if (_abilities == null) throw new ArgumentException("Ability 装配列表缺失。");
            var result = new EntityPowerSpec[_abilities.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Length; i++)
            {
                var slot = _abilities[i];
                if (slot?.Definition == null) throw new ArgumentException("Ability 配置引用缺失。");
                result[i] = slot.Definition.Capture(slot.Enabled);
                if (!ids.Add(result[i].Id)) throw new ArgumentException("Ability ID 重复：" + result[i].Id);
            }
            return result;
        }

        internal EntityPowerSpec[] CaptureSkills()
        {
            if (_skills == null) throw new ArgumentException("Skill 装配列表缺失。");
            var result = new EntityPowerSpec[_skills.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Length; i++)
            {
                var slot = _skills[i];
                if (slot?.Definition == null) throw new ArgumentException("Skill 配置引用缺失。");
                result[i] = slot.Definition.Capture(slot.Enabled);
                if (!ids.Add(result[i].Id)) throw new ArgumentException("Skill ID 重复：" + result[i].Id);
            }
            return result;
        }
        #endregion
    }
}

