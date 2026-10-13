using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>编辑校验与运行准备共享相同装配依赖；只检查，不开启动作或触碰实例。</summary>
    internal static class PlayerSchemeValidation
    {
        internal static bool Validate(PlayerConfig config, PlayerFeelRuntime feel, PlayerPowerSession powers)
        {
            bool normalLandingFound = false, hardLandingFound = false, rollingFound = false;
            bool accelerateFound = false, decelerateFound = false;
            foreach (var node in config.Graph.Nodes)
            {
                accelerateFound |= node.Definition is PlayerAccelerateStateDefinition;
                decelerateFound |= node.Definition is PlayerDecelerateStateDefinition;
                rollingFound |= node.Definition is PlayerRollingStateDefinition;
                normalLandingFound |= node.Definition is PlayerLandStateDefinition;
                hardLandingFound |= node.Definition is PlayerHardLandStateDefinition;
            }
            if (feel.Accelerated && (!accelerateFound || !decelerateFound))
                throw new InvalidOperationException("已启用地面渐变但状态图缺少 Accelerate/Decelerate 状态。");
            if (powers.Abilities.TryGet(PlayerLedgeClimbState.ABILITY_ID, out var climbAbility) && climbAbility.IsEnabled &&
                (!powers.Abilities.TryGet(PlayerLedgeHangState.ABILITY_ID, out var hangAbility) || !hangAbility.IsEnabled))
                throw new InvalidOperationException("启用翻越需要同时启用挂边；请修正方案装配，不会自动启用依赖。");
            if (feel.Landing.NormalEnabled && !normalLandingFound || feel.Landing.HardEnabled && !hardLandingFound)
                throw new InvalidOperationException("已启用落地停顿但状态图缺少对应 Land/HardLand 状态。");
            if (powers.Abilities.TryGet(PlayerAirJumpState.ABILITY_ID, out var airJump))
            {
                foreach (var slot in config.Powers.AbilitySlots)
                    if (slot.Definition.Id == PlayerAirJumpState.ABILITY_ID && slot.Definition is PlayerAbilityDefinition definition && definition.RefillOnGroundSupport)
                        throw new InvalidOperationException("二段跳只能在有效普通起跳时补满，不能配置地面支撑补充。");
                if (airJump.IsEnabled)
                {
                    if (!float.IsFinite(feel.AirJumpSpeed) || feel.AirJumpSpeed <= 0 ||
                        !Enum.IsDefined(typeof(PlayerAirJumpHorizontalMode), feel.AirJumpHorizontalMode) ||
                        feel.AirJumpHorizontalMode == PlayerAirJumpHorizontalMode.FacingSpeed &&
                        (!float.IsFinite(feel.AirJumpHorizontalSpeed) || feel.AirJumpHorizontalSpeed < 0))
                        throw new InvalidOperationException("启用二段跳时需要有效的起跳速度及水平策略参数。");
                    bool stateFound = false;
                    foreach (var node in config.Graph.Nodes) if (node.Definition is PlayerAirJumpStateDefinition) stateFound = true;
                    if (!stateFound) throw new InvalidOperationException("已启用二段跳但状态图缺少 AirJump 状态。");
                }
            }
            foreach (var wallId in new[] { PlayerWallJumpState.ABILITY_ID, PlayerWallSlideState.ABILITY_ID })
            {
                if (!powers.Abilities.TryGet(wallId, out var wallAbility) || !wallAbility.IsEnabled) continue;
                bool found = false;
                foreach (var node in config.Graph.Nodes)
                    if (wallId == PlayerWallJumpState.ABILITY_ID ? node.Definition is PlayerWallJumpStateDefinition : node.Definition is PlayerWallSlideStateDefinition) found = true;
                if (!found) throw new InvalidOperationException("已启用墙面能力但状态图缺少对应状态：" + wallId);
                if (!float.IsFinite(feel.WallSlideSpeed) || feel.WallSlideSpeed < 0 ||
                    !float.IsFinite(feel.WallFastSlideSpeed) || feel.WallFastSlideSpeed < feel.WallSlideSpeed ||
                    !float.IsFinite(feel.WallJumpX) || feel.WallJumpX <= 0 || !float.IsFinite(feel.WallJumpY) || feel.WallJumpY <= 0 ||
                    !float.IsFinite(feel.WallProtection) || feel.WallProtection < 0 ||
                    !float.IsFinite(feel.WallReturnDuration) || feel.WallReturnDuration < 0 ||
                    !float.IsFinite(feel.WallReturnMultiplier) || feel.WallReturnMultiplier <= 0 || feel.WallReturnMultiplier > 1)
                    throw new InvalidOperationException("墙面运动速度、时间或回墙倍率无效。");
            }
            if (powers.Abilities.TryGet(PlayerDropThroughState.ABILITY_ID, out var drop) && drop.IsEnabled)
            {
                bool found = false;
                foreach (var node in config.Graph.Nodes) if (node.Definition is PlayerDropThroughStateDefinition) found = true;
                if (!found || !float.IsFinite(feel.DropThroughSpeed) || feel.DropThroughSpeed <= 0)
                    throw new InvalidOperationException("启用下穿需要对应状态和有效向下速度。");
            }
            if (feel.Rolling.Enabled && !rollingFound) throw new InvalidOperationException("已启用翻滚但状态图缺少 Rolling 状态。");
            if (powers.Abilities.TryGet(PlayerHeadJumpState.ABILITY_ID, out var head) && head.IsEnabled)
            {
                bool found = false;
                foreach (var node in config.Graph.Nodes) if (node.Definition is PlayerHeadJumpStateDefinition) found = true;
                if (!found || !float.IsFinite(feel.HeadJumpSpeed) || feel.HeadJumpSpeed <= 0)
                    throw new InvalidOperationException("启用踩头反弹需要 HeadJump 状态及有效起跳速度。");
            }
            bool bindDash = false;
            foreach (var id in new[] { PlayerLedgeHangState.ABILITY_ID, PlayerLedgeClimbState.ABILITY_ID })
            {
                if (!powers.Abilities.TryGet(id, out var ability) || !ability.IsEnabled) continue;
                feel.Ledge.Validate();
                bool found = false;
                foreach (var node in config.Graph.Nodes)
                    if (id == PlayerLedgeHangState.ABILITY_ID ? node.Definition is PlayerLedgeHangStateDefinition : node.Definition is PlayerLedgeClimbStateDefinition) found = true;
                if (!found) throw new InvalidOperationException("已启用挂边／翻越但图缺少对应状态：" + id);
            }
            foreach (var dashId in new[] { PlayerGroundDashState.SKILL_ID, PlayerAirDashState.SKILL_ID })
            {
                if (!powers.Skills.TryGet(dashId, out var dash) || !dash.IsEnabled) continue;
                bindDash = true;
                bool found = false;
                foreach (var node in config.Graph.Nodes)
                    if (dashId == PlayerGroundDashState.SKILL_ID ? node.Definition is PlayerGroundDashStateDefinition : node.Definition is PlayerAirDashStateDefinition) found = true;
                if (!found) throw new InvalidOperationException("已启用冲刺但状态图缺少对应状态：" + dashId);
            }
            if (bindDash && string.IsNullOrWhiteSpace(config.DashAction))
                throw new InvalidOperationException("已启用冲刺但未指定输入 Action。");
            return bindDash;
        }
    }
}