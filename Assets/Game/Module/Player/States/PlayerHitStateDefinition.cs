using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerHitStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerHitState>, IPlayerLifeNodeDefinition
    {
        public override PlayerHitState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
        {
            new EmberFsmTransition<PlayerContext,PlayerHitState>("finish-ground", "ground", sites: new[] {
                PlayerRule.Fixed<PlayerHitState>(0, (s,c) => c.Life.Reaction.Remaining <= 0 && c.JumpWindow.HasGroundSupport, "硬直结束回地面") }),
            new EmberFsmTransition<PlayerContext,PlayerHitState>("finish-air", "fall", sites: new[] {
                PlayerRule.Fixed<PlayerHitState>(1, (s,c) => c.Life.Reaction.Remaining <= 0 && !c.JumpWindow.HasGroundSupport, "硬直结束回空中") })
        };
    }
    public sealed class PlayerHitState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => Context.Life?.Reaction?.HasPending == true && !Context.Life.IsDead
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Life.BeginReaction(); }
        public override void FixedUpdate(float dt) { Context.Life.Reaction.Advance(Context.Motor, dt); Activation.AttemptGroup("decision"); }
        public override void Exit() => Context.Life.EndReaction();
    }
}
