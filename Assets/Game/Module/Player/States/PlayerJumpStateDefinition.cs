using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Jump", fileName = "PlayerJump")]
    public sealed class PlayerJumpStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerJumpState>
    {
        #region 外部方法
        // 配置引用由 PlayerConfig.Feel 统一提供；状态只读本宿主准备时捕获的分组参数。
        public override PlayerJumpState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerJumpState.Fall };
        #endregion
    }
}
