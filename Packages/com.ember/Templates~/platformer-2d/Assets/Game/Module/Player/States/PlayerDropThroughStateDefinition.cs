using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Drop Through", fileName = "PlayerDropThrough")]
    public sealed class PlayerDropThroughStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerDropThroughState>
    {
        #region 外部方法
        public override PlayerDropThroughState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
            { PlayerDropThroughState.Ground, PlayerDropThroughState.Fall };
        #endregion
    }
}
