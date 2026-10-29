using System.Collections.Generic;
using System.Text;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>只读登记，不额外驱动状态机、评估条件或消费输入。</summary>
    [InitializeOnLoad]
    internal static class PlayerFsmObservationSource
    {
        static PlayerFsmObservationSource()=>EmberFsmObservationSources.Register("topdown.player",Read,
            ownerTemplateId:"topdown-2d",enabled:()=>PlayerEditorAccess.ModuleEnabled);
        private static IEnumerable<EmberFsmObservedInstance> Read()
        {
            if(!EditorApplication.isPlaying||!PlayerEditorAccess.Available)yield break;
            foreach(var player in Resources.FindObjectsOfTypeAll<PlayerController>())
            {
                if(!player.gameObject.scene.IsValid()||!player.IsPrepared)continue;
                var context=player.Context;var metrics=new StringBuilder();
                metrics.Append($"移动 {context.Move} · 朝向 {context.Facing} · 速度 {context.Velocity}\n");
                metrics.Append($"HP {context.Health?.Current.ToString()??"未装配"} · 效果 {context.Effects.Count} · 输入 {context.ControlsAvailable}\n");
                var powers=context.Powers.Capture();
                for(var i=0;i<powers.SkillCount;i++)
                {var skill=powers.GetSkill(i);metrics.Append($"Skill {skill.Id} · 开放 {skill.IsOpen} · 次数 {skill.RemainingUses?.ToString()??"不限"} · CD {skill.RemainingCooldown:F3}s\n");}
                yield return new EmberFsmObservedInstance(player,player.ObservationGraph,player.CaptureFsmObservation(),metrics.ToString());
                foreach(var weapon in context.Weapons.Weapons)
                {
                    var resource=weapon.Context.Powers.Capture();var text=new StringBuilder(weapon.Spec.DisplayName+(ReferenceEquals(weapon,context.Weapons.Current)?" · 当前装备":" · 已收起")+"\n");
                    for(var i=0;i<resource.SkillCount;i++){var skill=resource.GetSkill(i);text.Append($"武器Skill {skill.Id} · 次数 {skill.RemainingUses?.ToString()??"不限"} · CD {skill.RemainingCooldown:F3}s\n");}
                    yield return new EmberFsmObservedInstance(player,weapon.Spec.SourceGraph,weapon.CaptureObservation(),text.ToString());
                }
            }
        }
    }
}
