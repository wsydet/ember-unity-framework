using System;
using System.Collections.Generic;

namespace Game.Entity
{
    /// <summary>一个资源的普通运行数据；项目启用和环境条件仍来自配置及宿主。</summary>
    public readonly struct EntityPowerRecord
    {
        #region 内部参数
        public string Id { get; }
        public bool IsOpen { get; }
        public int? RemainingUses { get; }
        public float RemainingCooldown { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityPowerRecord(string id, bool isOpen, int? remainingUses, float remainingCooldown)
        {
            Id = id; IsOpen = isOpen; RemainingUses = remainingUses; RemainingCooldown = remainingCooldown;
            Validate();
        }
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id) || Id != Id.Trim() || RemainingUses < 0 ||
                !float.IsFinite(RemainingCooldown) || RemainingCooldown < 0)
                throw new ArgumentException("能力快照 ID、次数或剩余 CD 无效。");
        }
        #endregion
    }

    /// <summary>两类资源分别保存开放与剩余资源，不通过可用性反推。构造时复制数组。</summary>
    public sealed class EntityPowerSnapshot
    {
        #region 内部参数
        private readonly EntityPowerRecord[] _abilities, _skills;
        public int AbilityCount => _abilities.Length;
        public int SkillCount => _skills.Length;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EntityPowerRecord[] Copy(EntityPowerRecord[] source, bool skill)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = (EntityPowerRecord[])source.Clone();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in result)
            {
                item.Validate();
                if (!ids.Add(item.Id)) throw new ArgumentException("能力快照 ID 重复：" + item.Id);
                if (!skill && item.RemainingCooldown != 0) throw new ArgumentException("Ability 不包含技能冷却。");
            }
            return result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityPowerSnapshot(EntityPowerRecord[] abilities, EntityPowerRecord[] skills)
        { _abilities = Copy(abilities, false); _skills = Copy(skills, true); }
        public EntityPowerRecord GetAbility(int index) => _abilities[index];
        public EntityPowerRecord GetSkill(int index) => _skills[index];
        #endregion
    }
}
