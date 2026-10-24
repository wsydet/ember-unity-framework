using System;
using System.Reflection;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Tests
{
    internal static class PlayerPowerTestAssets
    {
        #region 内部方法
        private static void Set(object target, string name, object value)
        {
            for(var type=target.GetType();type!=null;type=type.BaseType)
            {
                var field=type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
                if(field==null)continue;
                field.SetValue(target,value);return;
            }
            throw new MissingFieldException(name);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Install(PlayerConfig target, Action<UnityEngine.Object> keep)
        {
            var ability=ScriptableObject.CreateInstance<PlayerAbilityDefinition>(); keep(ability);
            Set(ability,"_id","test.air");Set(ability,"_defaultOpen",true);
            Set(ability,"_limitedUses",true);Set(ability,"_maximumUses",2);Set(ability,"_refillOnGroundSupport",true);
            var skill=ScriptableObject.CreateInstance<PlayerSkillDefinition>(); keep(skill);
            Set(skill,"_id","test.skill");Set(skill,"_defaultOpen",true);Set(skill,"_cooldownSeconds",10f);
            var config=ScriptableObject.CreateInstance<PlayerPowerConfig>();keep(config);
            Set(config,"_abilities",new[]{new EntityAbilitySlot(ability)});
            Set(config,"_skills",new[]{new EntitySkillSlot(skill)});Set(target,"_powers",config);
        }
        #endregion
    }
}
