using System.Linq;
using Ember.Core.Editor;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>平台玩家蓝图适配；只读装配/资源事实，不调用 Guard 或运行状态。</summary>
    internal static class PlayerStateAvailability
    {
        #region 内部方法
        private static string PowerId(EmberFsmStateDefinition definition) => definition switch
        {
            PlayerAirJumpStateDefinition => PlayerAirJumpState.ABILITY_ID,
            PlayerWallJumpStateDefinition => PlayerWallJumpState.ABILITY_ID,
            PlayerWallSlideStateDefinition => PlayerWallSlideState.ABILITY_ID,
            PlayerDropThroughStateDefinition => PlayerDropThroughState.ABILITY_ID,
            PlayerLedgeHangStateDefinition => PlayerLedgeHangState.ABILITY_ID,
            PlayerLedgeClimbStateDefinition => PlayerLedgeClimbState.ABILITY_ID,
            PlayerHeadJumpStateDefinition => PlayerHeadJumpState.ABILITY_ID,
            PlayerGroundDashStateDefinition => PlayerGroundDashState.SKILL_ID,
            PlayerAirDashStateDefinition => PlayerAirDashState.SKILL_ID,
            _ => null
        };

        private static string Rule(EmberFsmStateDefinition definition, PlayerFeelRuntime feel)
        {
            bool? enabled = definition switch
            {
                PlayerAccelerateStateDefinition or PlayerDecelerateStateDefinition => feel.Accelerated,
                PlayerLandStateDefinition => feel.Landing.NormalEnabled,
                PlayerHardLandStateDefinition => feel.Landing.HardEnabled,
                PlayerRollingStateDefinition => feel.Rolling.Enabled,
                _ => null
            };
            return enabled == null ? "基础状态" : enabled.Value ? "运动规则已启用" :
                definition is PlayerAccelerateStateDefinition or PlayerDecelerateStateDefinition
                    ? "禁用：地面渐变关闭（空中独立渐变不启用此状态）" : "禁用：对应自动运动规则关闭";
        }

        private static string Resource(EntityPowerRuntime runtime) =>
            !runtime.IsEnabled ? "禁用：方案未启用" : !runtime.IsOpen ? "已启用，进度未解锁" :
            $"已启用且解锁；次数 {runtime.RemainingUses?.ToString() ?? "不限"} / CD {runtime.RemainingCooldown:F3}s；环境资格由真实运行时判断";
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static string Describe(EmberFsmStateDefinition definition, PlayerFeelRuntime feel, PlayerPowerConfig powers)
        {
            string id = PowerId(definition);
            if (id == null) return Rule(definition, feel);
            if (powers == null) return "禁用：未装配 Powers";
            bool skill = definition is PlayerGroundDashStateDefinition or PlayerAirDashStateDefinition;
            bool? enabled = skill ? powers.SkillSlots.FirstOrDefault(s => s?.Definition?.Id == id)?.Enabled
                : powers.AbilitySlots.FirstOrDefault(s => s?.Definition?.Id == id)?.Enabled;
            if (enabled != true) return enabled == null ? "禁用：未装配对应能力/技能" : "禁用：方案未启用";
            if (definition is PlayerLedgeClimbStateDefinition &&
                powers.AbilitySlots.FirstOrDefault(s => s?.Definition?.Id == PlayerLedgeHangState.ABILITY_ID)?.Enabled != true)
                return "装配无效：翻越依赖已启用的挂边，准备时将拒绝";
            return "方案已启用；解锁、次数、CD 与环境资格以所选运行实例为准";
        }

        internal static void Draw(EmberFsmBlueprintWindow window, PlayerConfig config)
        {
            if (window.ParameterNode == null) return;
            var definition = window.ParameterNode.Definition;
            EditorGUILayout.HelpBox("静态编辑方案：" + (config.Scheme != null ? config.Scheme.name : config.name) + "\n" +
                Describe(definition, config.Feel.Capture(Physics2D.gravity), config.Powers), MessageType.None);
            var id = window.ObservedSnapshot?.InstanceId;
            var player = id.HasValue ? Resources.FindObjectsOfTypeAll<PlayerController>().FirstOrDefault(p => p.IsPrepared && p.FsmInstanceId == id) : null;
            if (player == null)
            { EditorGUILayout.HelpBox("未选择有效运行实例；以上是资产装配提示，不是运行资格。", MessageType.None); return; }
            var context = player.Context;
            string powerId = PowerId(definition);
            string message;
            if (powerId == null) message = Rule(definition, context.Feel);
            else
            {
                EntityPowerRuntime runtime;
                bool found = definition is PlayerGroundDashStateDefinition or PlayerAirDashStateDefinition
                    ? context.Skills.TryGet(powerId, out runtime) : context.Abilities.TryGet(powerId, out runtime);
                message = found ? Resource(runtime) : "禁用：冻结会话未装配此能力/技能";
            }
            EditorGUILayout.HelpBox("所选冻结实例：" + id + "\n" + message + "\n编辑资产不改变当前会话。", MessageType.Info);
        }
        #endregion
    }
}
