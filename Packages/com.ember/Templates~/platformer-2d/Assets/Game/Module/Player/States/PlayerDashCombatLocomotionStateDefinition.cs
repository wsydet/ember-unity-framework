using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;

namespace Game.Player
{
    public class PlayerDashCombatLocomotionStateDefinition : PlayerCombatLocomotionStateDefinition
    {
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => base.GetTransitions().Concat(new EmberFsmTransition[]
        {
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("tail-dash-ground", "combat-dash-ground", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(10, (s,c) => c.Combat.Dash.InTail && c.Dash.CanStart(true), "收尾再冲优先", segment:"dash-combat", commit:(s,c) => c.Combat.Dash.PrepareActionExit()) }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("tail-dash-air", "combat-dash-air", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(11, (s,c) => c.Combat.Dash.InTail && c.Dash.CanStart(false), "收尾再冲优先", segment:"dash-combat", commit:(s,c) => c.Combat.Dash.PrepareActionExit()) }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("tail-jump", "combat-jump", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(20, (s,c) => c.Combat.Dash.InTail && c.CanStartJump, "收尾跳跃断连", segment:"dash-combat", commit:(s,c) => c.Combat.Dash.PrepareActionExit()) }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("tail-air-jump", "combat-air-jump", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(21, (s,c) => c.Combat.Dash.InTail && c.CanStartAirJump, "收尾空跳断连", segment:"dash-combat", commit:(s,c) => c.Combat.Dash.PrepareActionExit()) }),
            new EmberFsmTransition<PlayerContext,PlayerLocomotionState>("dash-attack", "combat-route", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(30, (s,c) => c.Combat.Dash.Gate && c.Combat.CanAttack(c.Combat.Dash.Target), "冲刺或收尾解析同一目标", segment:"dash-combat", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Dash.Target)) })
        }).ToArray();
    }
}
