using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    /// <summary>仅独立战斗方案使用的根声明，普通移动图不增加攻击出边。</summary>
    public class PlayerCombatLocomotionStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLocomotionState>
    {
        public override PlayerLocomotionState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
        {
            PlayerLocomotionState.HardLand, PlayerLocomotionState.Roll, PlayerLocomotionState.Land,
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-dash-ground", "combat-dash-ground", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(10, (s,c) => c.Dash.CanStart(true), "起手前冲刺优先", segment:"combat") }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-dash-air", "combat-dash-air", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(11, (s,c) => c.Dash.CanStart(false), "起手前冲刺优先", segment:"combat") }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-jump", "combat-jump", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(20, (s,c) => c.CanStartJump, "起手前跳跃优先", segment:"combat") }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-air-jump", "combat-air-jump", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(21, (s,c) => c.CanStartAirJump, "起手前空跳优先", segment:"combat") }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-ground", "combat-ground", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(30, (s,c) => c.JumpWindow.HasGroundSupport && c.Combat.CanAttack(c.Combat.Entry), "地面环境起手", segment:"combat", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Entry)) }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("combat-air", "combat-air", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(31, (s,c) => !c.JumpWindow.HasGroundSupport && c.Combat.CanAttack(c.Combat.Entry), "空中环境起手", segment:"combat", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Entry)) })
        };
    }
}
