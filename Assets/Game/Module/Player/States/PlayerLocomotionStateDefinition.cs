using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Locomotion", fileName = "PlayerLocomotion")]
    public sealed class PlayerLocomotionStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerLocomotionState>
    {
        #region 外部方法
        // 配置引用由 PlayerConfig.Feel 统一提供；状态只读本宿主准备时捕获的分组参数。
        public override PlayerLocomotionState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerLocomotionState.HardLand, PlayerLocomotionState.Roll, PlayerLocomotionState.Land };
        #endregion
    }
}
