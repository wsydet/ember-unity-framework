using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Decelerate", fileName = "PlayerDecelerate")]
    public sealed class PlayerDecelerateStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerDecelerateState>
    {
        #region 外部方法
        public override PlayerDecelerateState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerDecelerateState.Idle, PlayerDecelerateState.Move, PlayerDecelerateState.Accelerate };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "idle", "move", "accelerate" };
        #endregion
    }
}

