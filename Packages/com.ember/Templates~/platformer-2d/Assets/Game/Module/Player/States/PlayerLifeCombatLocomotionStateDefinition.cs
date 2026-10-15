using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerLifeCombatLocomotionStateDefinition : PlayerCombatLocomotionStateDefinition, IPlayerLifeRootDefinition
    {
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => base.GetTransitions().Concat(PlayerLifeTransitions.Build()).ToArray();
    }
}
