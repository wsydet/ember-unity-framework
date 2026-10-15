using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerAttackStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAttackState>, IPlayerCombatNodeDefinition
    {
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.Attack;
        public override PlayerAttackState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => PlayerCombatTransitions.Build<PlayerAttackState>(false);
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "delayed", "restart-ground", "restart-air" };
    }
    public sealed class PlayerAttackState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => Context.Combat != null && Context.Combat.CanAttack(Context.Combat.SegmentForNode(NodeId))
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Combat.BeginAttack(NodeId); }
        public override void FixedUpdate(float dt) { Context.Combat.Advance(dt); Activation.AttemptGroup("decision"); }
        public override void Exit() => Context.Combat.Leave();
    }
}
