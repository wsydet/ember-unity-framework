using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;
namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/WallSlide")]
    public sealed class PlayerWallSlideStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerWallSlideState>
    {
        #region 外部方法
        public override PlayerWallSlideState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerWallSlideState.Fall };
        #endregion
    }
}
