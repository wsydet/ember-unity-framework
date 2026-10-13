using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Rolling", fileName = "PlayerRolling")]
    public sealed class PlayerRollingStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerRollingState>
    {
        #region 外部方法
        public override PlayerRollingState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
            { PlayerRollingState.Ground, PlayerRollingState.Fall, PlayerRollingState.Jump, PlayerRollingState.Dash, PlayerRollingState.Drop };
        #endregion
    }
}
