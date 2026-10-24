using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Ledge Climb", fileName = "PlayerLedgeClimb")]
    public sealed class PlayerLedgeClimbStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLedgeClimbState>
    {
        #region 外部方法
        public override PlayerLedgeClimbState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerLedgeClimbState.Fall };
        #endregion
    }
}
