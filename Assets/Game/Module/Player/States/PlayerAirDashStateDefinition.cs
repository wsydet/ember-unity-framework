using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/States/Air Dash", fileName = "PlayerAirDash")]
    public sealed class PlayerAirDashStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAirDashState>
    {
        #region 编辑器面板参数
        [SerializeField, HideInInspector] private Game.Entity.EntityEffectDefinition _actionEffect;
        [SerializeField, HideInInspector] private Game.Entity.EntityEffectDefinition _tailEffect;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Game.Entity.EntityEffectDefinition _capturedAction, _capturedTail;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal Game.Entity.EntityEffectDefinition LegacyActionEffect => _actionEffect;
        internal Game.Entity.EntityEffectDefinition LegacyTailEffect => _tailEffect;
        public Game.Entity.EntityEffectDefinition ActionEffect => _capturedAction;
        public Game.Entity.EntityEffectDefinition TailEffect => _capturedTail;
        internal void SetCapturedEffects(Game.Entity.EntityEffectDefinition action, Game.Entity.EntityEffectDefinition tail)
        { _capturedAction = action; _capturedTail = tail; }
        public override PlayerAirDashState CreateState() => new(_capturedAction, _capturedTail);
        internal void ValidateEffects(Game.Entity.EntityEffectContainer container) => new PlayerDashEffects(_capturedAction, _capturedTail).ValidateContainer(container);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() =>
            new EmberFsmTransition[] { PlayerAirDashState.Ground, PlayerAirDashState.Fall };
        #endregion
    }
}
