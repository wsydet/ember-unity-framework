using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerDashRecoveryStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerDashRecoveryState>, IPlayerCombatNodeDefinition
    {
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.DashRecovery;
        public override PlayerDashRecoveryState CreateState() => new();
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "ground", "fall" };
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
        {
            new EmberFsmTransition<PlayerContext,PlayerDashRecoveryState>("finish-ground", "ground", sites:new[] {
                PlayerRule.Fixed<PlayerDashRecoveryState>(10, (s,c) => c.Combat.Dash.TailFinished && c.JumpWindow.HasGroundSupport, "收尾截止，交回地面") }),
            new EmberFsmTransition<PlayerContext,PlayerDashRecoveryState>("finish-air", "fall", sites:new[] {
                PlayerRule.Fixed<PlayerDashRecoveryState>(20, (s,c) => c.Combat.Dash.TailFinished && !c.JumpWindow.HasGroundSupport, "收尾截止，交回空中") })
        };
    }
    public sealed class PlayerDashRecoveryState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => Context.Combat?.Dash != null && Context.Dash.EndReason == PlayerDashEndReason.Completed
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect(Context.JumpWindow.HasGroundSupport ? "ground" : "fall");
        public override void FixedUpdate(float dt) { Context.Combat.Dash.AdvanceTail(dt); Activation.AttemptGroup("decision"); }
        public override void Exit() => Context.Combat.Dash.LeaveTail();
    }
}
