using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Move", fileName = "PlayerMove")]
    public sealed class PlayerMoveStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerMoveState>
    {
        #region 外部方法
        public override PlayerMoveState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerMoveState.Idle, PlayerMoveState.Accelerate, PlayerMoveState.Decelerate };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "idle", "accelerate", "decelerate" };
        #endregion
    }
}

