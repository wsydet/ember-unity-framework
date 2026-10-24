using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Idle", fileName = "PlayerIdle")]
    public sealed class PlayerIdleStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerIdleState>
    {
        #region 外部方法
        public override PlayerIdleState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerIdleState.Move, PlayerIdleState.Accelerate, PlayerIdleState.Decelerate };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "move", "accelerate", "decelerate" };
        #endregion
    }
}

