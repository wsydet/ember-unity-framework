using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;
namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Hard Land", fileName = "PlayerHardLand")]
    public sealed class PlayerHardLandStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerHardLandState>
    {
        #region 外部方法
        public override PlayerHardLandState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
            { PlayerLandingState.Ground, PlayerLandingState.Fall };
        #endregion
    }
}
