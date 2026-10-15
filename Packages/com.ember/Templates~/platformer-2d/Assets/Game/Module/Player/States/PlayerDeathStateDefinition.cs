using System;
using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerDeathStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerDeathState>, IPlayerLifeNodeDefinition
    {
        public override PlayerDeathState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
    }
    public sealed class PlayerDeathState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => Context.Life is { IsDead: true, DeathCommitted: false }
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Life.BeginDeath(); }
        public override void FixedUpdate(float dt) => Context.Life.HoldDeath();
    }
}
