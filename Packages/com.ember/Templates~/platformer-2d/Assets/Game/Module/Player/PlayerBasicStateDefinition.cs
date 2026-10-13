using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    /// <summary>阶段 3 驱动链验证状态：只有水平移动，完整平台跳跃状态留给阶段 4。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Basic Drive State", fileName = "PlayerBasicState")]
    public sealed class PlayerBasicStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerBasicState>
    {
        #region 编辑器面板参数
        [SerializeField, Min(0)] private float _speed = 5;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override PlayerBasicState CreateState() => new(_speed);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
        #endregion
    }

    public sealed class PlayerBasicState : EmberFsmState<PlayerContext>
    {
        private readonly float _speed;
        public PlayerBasicState(float speed) => _speed = speed;
        public override void FixedUpdate(float deltaTime)
        {
            Context.Motor.SetHorizontalVelocity(Context.Input.Horizontal * _speed);
            if (Context.Input.Horizontal != 0) Context.Facing.SetFacing(Context.Input.Horizontal > 0);
        }
    }
}
