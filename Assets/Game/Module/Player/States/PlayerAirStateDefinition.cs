using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Air", fileName = "PlayerAir")]
    public sealed class PlayerAirStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAirState>
    {
        #region 外部方法
        public override PlayerAirState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerAirState.DropThrough, PlayerAirState.Jump, PlayerAirState.HeadJump, PlayerAirState.WallJump, PlayerAirState.AirJump, PlayerAirState.Hang, PlayerAirState.WallSlide, PlayerAirState.Land, PlayerAirState.GroundDash, PlayerAirState.AirDash };
        #endregion
    }
}
