using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Fall", fileName = "PlayerFall")]
    public sealed class PlayerFallStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerFallState>
    {
        #region 外部方法
        public override PlayerFallState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "rising" };
        #endregion
    }
}
