using System.Collections.Generic;
using System.Text;
using Game.Entity;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    [InitializeOnLoad]
    internal static class PlayerFsmObservationSource
    {
        static PlayerFsmObservationSource() => EmberFsmObservationSources.Register("platformer-player",Read);
        public static string Metrics(PlayerContext context)
        {
            var result = new StringBuilder();
            result.Append($"运动控制 {context.MotionControl.Observation}\n");
            result.Append(context.ExternalMotion.Describe()).Append('\n');
            var support = context.JumpWindow;
            result.Append($"支撑 {support.HasGroundSupport} · 原因 {support.SupportReason} · 接触 {support.Contact.Collider?.name ?? "无"} · 法线 {support.Contact.Normal.ToString("R")} · 相对分离 {support.Contact.SeparationSpeed:R} u/s · 等待离开 {support.AwaitingSeparation}\n");
            var combat = context.Combat;
            if (combat != null)
            {
                result.Append($"攻击 {combat.Segment?.Id ?? "无"} · 活动 {combat.IsActive} · 时间 {combat.Progress:F3}s · 收刀 {combat.IsRecovery} / 特殊 {combat.IsDelayedRecovery}\n");
                if (combat.IsActive)
                    result.Append($"开放 冲刺 {combat.Gate(PlayerCombatButton.Dash)} / 跳跃 {combat.Gate(PlayerCombatButton.Jump)} / 攻击 {combat.Gate(PlayerCombatButton.Attack)} · 下一刀 {combat.Next} · 停顿目标 {combat.Delayed}\n");
                if (combat.Dash != null)
                    result.Append($"冲刺续接 {combat.Dash.Target} · 收尾时间 {combat.Dash.TailTime:F3} · 惯性 {combat.Dash.HasResidualMotion}\n");
                if (combat.Hits != null) result.Append(combat.Hits.Observation + "\n");
            }
            foreach (PlayerCombatButton button in System.Enum.GetValues(typeof(PlayerCombatButton)))
                result.Append(context.Input.Combat.Observe(button) + "\n");
            result.Append($"HP {context.Health?.Current.ToString() ?? "未装配"} · 死亡 {context.Life?.IsDead} · 硬直 {context.Life?.Reaction?.Remaining:F3}s\n");
            result.Append($"速度 {context.Motor.Velocity} u/s · 当前有效重力倍率 {context.VerticalMotion.EffectiveGravityScale:F3} · 加速度 {Physics2D.gravity.y * context.VerticalMotion.EffectiveGravityScale:F3} u/s²");
            result.Append($"\n输入缓冲 {context.Input.BufferRemaining:F3}s / {context.Feel.BufferDuration:F3}s · 离地宽限 {context.JumpWindow.CoyoteRemaining:F3}s / {context.Feel.CoyoteDuration:F3}s · 悬停 {context.VerticalMotion.HoverRemaining:F3}s");
            result.Append($"\n墙面 左 {context.Sensors.LeftWall?.name ?? "无"} / 右 {context.Sensors.RightWall?.name ?? "无"} · 向下输入 {context.Input.Vertical:F2} · 墙滑 {context.Wall.Sliding} · 离墙保护 {context.Wall.ProtectionRemaining:F3}s · 回墙窗口 {context.Wall.ReturnRemaining:F3}s · 回墙中 {context.Wall.Returning}");
            result.Append($"\n下穿 {context.DropThrough.IsActive} · 目标平台 {context.DropThrough.Target?.name ?? "无"} · 碰撞豁免期间拒绝保存");
            var powers = context.CapturePowerSnapshot();
            result.Append($"\n踩头反弹 {context.HeadJump.IsActive} · 本步头部候选 {context.HeadJump.Target?.name ?? "无"} · 上升动作中拒绝保存");
            result.Append($"\n落地翻滚 {context.Rolling.IsActive} · 锁定方向 {context.Rolling.Direction} · 剩余 {context.Rolling.Remaining:F3}s");
            result.Append($"\n落地停顿 {context.Landing.IsActive} · 重落地 {context.Landing.IsHard} · 冲击速度 {context.Landing.ImpactSpeed:F3}u/s · 剩余 {context.Landing.Remaining:F3}s");
            result.Append($"\n挂边／翻越 {context.Ledge.IsActive} · 翻越中 {context.Ledge.IsClimbing} · 目标 {context.Ledge.Target?.name ?? "无"} · 已用时 {context.Ledge.Elapsed:F3}s · 受阻 {context.Ledge.Failed} · 动作期间拒绝保存");
            result.Append($"\n冲刺 {context.Dash.IsActive} · 地冲 {context.Dash.IsGroundDash} · 锁定方向 {context.Dash.Direction} · 剩余 {context.Dash.Remaining:F3}s · 结束原因 {context.Dash.EndReason}");
            result.Append($"\n实例已装配 Ability {powers.AbilityCount} / Skill {powers.SkillCount}；仅列资源事实，不重新评估环境或 Guard。");
            for (int i = 0; i < powers.AbilityCount; i++)
            {
                var item = powers.GetAbility(i);
                if (context.Abilities.TryGet(item.Id, out var runtime)) AppendPower(result, "Ability", item, runtime);
            }
            for (int i = 0; i < powers.SkillCount; i++)
            {
                var item = powers.GetSkill(i);
                if (context.Skills.TryGet(item.Id, out var runtime)) AppendPower(result, "Skill", item, runtime);
            }
            result.Append(context.Effects == null ? "\n效果容器未装配" : $"\n效果 {context.Effects.Count} 项");
            result.Append("\n" + context.FrozenActionEffects);
            if (context.Effects != null)
                for (int i = 0; i < context.Effects.Count; i++)
                {
                    var effect = context.Effects.GetAt(i);
                    result.Append($"\n{effect.EffectId} · 来源 {effect.SourceKey} · {effect.ApplicationId} · {effect.Lifetime} · " +
                        (effect.IsTiming ? $"剩余 {effect.RemainingSeconds:F3}s" : "未独立计时"));
                }
            return result.ToString();
        }
        private static void AppendPower(StringBuilder result, string kind, EntityPowerRecord item, EntityPowerRuntime runtime)
            => result.Append($"\n{kind} {item.Id} · 项目启用 {runtime.IsEnabled} · 已解锁 {item.IsOpen} · 次数 {item.RemainingUses?.ToString() ?? "不限"} · CD {item.RemainingCooldown:F3}s");
        private static IEnumerable<EmberFsmObservedInstance> Read()
        {
            if(!EditorApplication.isPlaying || !PlayerEditorAccess.Available)yield break;
            foreach(var player in Resources.FindObjectsOfTypeAll<PlayerController>())
            {
                if(!player.gameObject.scene.IsValid() || !player.IsPrepared)continue;
                var snapshot=player.CaptureFsmObservation();
                var context=player.Context;
                yield return new EmberFsmObservedInstance(player,player.ObservationGraph,snapshot,
                    Metrics(context));
            }
        }
    }
}
