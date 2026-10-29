using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName="Game/Player/Topdown Powers",fileName="PlayerPowers")]
    public sealed class PlayerPowersDefinition:EntityPowerConfig
    {
        #region 外部方法
        public void Configure(EntityAbilitySlot[] abilities,EntitySkillSlot[] skills)=>ConfigureSlots(abilities,skills);
        internal static PlayerPowersDefinition Freeze(EntityPowerConfig source,List<Object> owned)
        {
            if(source==null)return null;
            var result=CreateInstance<PlayerPowersDefinition>();result.hideFlags=HideFlags.HideAndDontSave;owned.Add(result);
            var abilities=new List<EntityAbilitySlot>();var skills=new List<EntitySkillSlot>();
            foreach(var slot in source.AbilitySlots)
            {
                if(slot?.Definition==null)throw new System.InvalidOperationException("缺少 Ability 配置。");
                var copy=Instantiate(slot.Definition);copy.hideFlags=HideFlags.HideAndDontSave;owned.Add(copy);abilities.Add(new EntityAbilitySlot(copy,slot.Enabled));
            }
            foreach(var slot in source.SkillSlots)
            {
                if(slot?.Definition==null)throw new System.InvalidOperationException("缺少 Skill 配置。");
                var copy=Instantiate(slot.Definition);copy.hideFlags=HideFlags.HideAndDontSave;owned.Add(copy);skills.Add(new EntitySkillSlot(copy,slot.Enabled));
            }
            result.Configure(abilities.ToArray(),skills.ToArray());return result;
        }
        #endregion
    }
}
