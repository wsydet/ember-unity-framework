using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerLifeLocomotionStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLocomotionState>, IPlayerLifeRootDefinition
    {
        public override PlayerLocomotionState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() =>
            new EmberFsmTransition[] { PlayerLocomotionState.HardLand, PlayerLocomotionState.Roll, PlayerLocomotionState.Land }.Concat(PlayerLifeTransitions.Build()).ToArray();
    }
}
