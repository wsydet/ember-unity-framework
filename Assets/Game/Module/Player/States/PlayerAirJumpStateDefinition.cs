using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Air Jump", fileName = "PlayerAirJump")]
    public sealed class PlayerAirJumpStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAirJumpState>
    {
        #region 外部方法
        public override PlayerAirJumpState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerAirJumpState.Fall };
        #endregion
    }
}
