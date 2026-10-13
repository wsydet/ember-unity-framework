using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;
namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/WallJump")]
    public sealed class PlayerWallJumpStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerWallJumpState>
    {
        #region 外部方法
        public override PlayerWallJumpState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerWallJumpState.Fall };
        #endregion
    }
}
