using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;
namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Land", fileName = "PlayerLand")]
    public sealed class PlayerLandStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLandState>
    {
        #region 外部方法
        public override PlayerLandState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
            { PlayerLandingState.Ground, PlayerLandingState.Fall, PlayerLandingState.Jump, PlayerLandingState.Dash, PlayerLandingState.Drop };
        #endregion
    }
}
