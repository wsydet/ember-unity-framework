using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerAttackRecoveryStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAttackRecoveryState>, IPlayerCombatNodeDefinition
    {
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.Recovery;
        public override PlayerAttackRecoveryState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => PlayerCombatTransitions.Build<PlayerAttackRecoveryState>(true);
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "delayed" };
    }
    public sealed class PlayerAttackRecoveryState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => Context.Combat != null && Context.Combat.CanRecover(NodeId)
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Combat.BeginRecovery(NodeId); }
        public override void FixedUpdate(float dt) { Context.Combat.Advance(dt); Activation.AttemptGroup("decision"); }
        public override void Exit() => Context.Combat.Leave();
    }
}
