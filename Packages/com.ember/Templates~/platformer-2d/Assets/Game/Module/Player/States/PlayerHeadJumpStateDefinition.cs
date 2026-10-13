using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;
namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/HeadJump", fileName = "PlayerHeadJump")]
    public sealed class PlayerHeadJumpStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerHeadJumpState>
    {
        #region 外部方法
        public override PlayerHeadJumpState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerHeadJumpState.Fall };
        #endregion
    }
}
