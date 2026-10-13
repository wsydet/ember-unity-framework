using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Ledge Hang", fileName = "PlayerLedgeHang")]
    public sealed class PlayerLedgeHangStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLedgeHangState>
    {
        #region 外部方法
        public override PlayerLedgeHangState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[]
            { PlayerLedgeHangState.Climb, PlayerLedgeHangState.Fall };
        #endregion
    }
}
