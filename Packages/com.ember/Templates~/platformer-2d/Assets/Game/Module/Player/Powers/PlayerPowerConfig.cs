using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>玩家装配；可引用公共或玩家专用配置，不复制资源字段。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Power Config", fileName = "PlayerPowers")]
    public sealed class PlayerPowerConfig : EntityPowerConfig
    {
        #region 外部方法
        internal void CaptureSlots(PlayerPowerConfig source, System.Func<ScriptableObject, ScriptableObject> capture)
        {
            var abilities = new EntityAbilitySlot[source.AbilitySlots.Count];
            var skills = new EntitySkillSlot[source.SkillSlots.Count];
            for (int i = 0; i < abilities.Length; i++)
            {
                var slot = source.AbilitySlots[i];
                if (slot?.Definition == null) throw new System.ArgumentException("Ability 配置引用缺失。");
                abilities[i] = new EntityAbilitySlot((EntityAbilityDefinition)capture(slot.Definition), slot.Enabled);
            }
            for (int i = 0; i < skills.Length; i++)
            {
                var slot = source.SkillSlots[i];
                if (slot?.Definition == null) throw new System.ArgumentException("Skill 配置引用缺失。");
                skills[i] = new EntitySkillSlot((EntitySkillDefinition)capture(slot.Definition), slot.Enabled);
            }
            ConfigureSlots(abilities, skills);
        }
        #endregion
    }
}
