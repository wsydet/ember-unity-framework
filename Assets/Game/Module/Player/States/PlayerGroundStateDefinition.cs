using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Ground", fileName = "PlayerGround")]
    public sealed class PlayerGroundStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerGroundState>
    {
        #region 外部方法
        public override PlayerGroundState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerGroundState.DropThrough, PlayerGroundState.LeaveGround, PlayerGroundState.Jump, PlayerGroundState.GroundDash, PlayerGroundState.AirDash };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "fall" };
        #endregion
    }
}
