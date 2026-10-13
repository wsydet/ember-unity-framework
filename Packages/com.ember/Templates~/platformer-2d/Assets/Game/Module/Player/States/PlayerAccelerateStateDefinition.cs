using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Accelerate", fileName = "PlayerAccelerate")]
    public sealed class PlayerAccelerateStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAccelerateState>
    {
        #region 外部方法
        public override PlayerAccelerateState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerAccelerateState.Idle, PlayerAccelerateState.Move, PlayerAccelerateState.Decelerate };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "idle", "move", "decelerate" };
        #endregion
    }
}

