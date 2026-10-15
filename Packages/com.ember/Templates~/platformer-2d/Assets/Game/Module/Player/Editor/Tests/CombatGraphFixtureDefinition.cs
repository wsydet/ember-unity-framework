using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class CombatGraphFixtureDefinition : EmberFsmStateDefinition<PlayerContext, CombatGraphFixtureState>, IPlayerCombatNodeDefinition
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerCombatNodeRole _role;
        [SerializeField] private bool _rejectClone;
        [SerializeField] private Game.Entity.EntityEffectDefinition _effect;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerCombatNodeRole Role { get => _role; set => _role = value; }
        public bool RejectClone { get => _rejectClone; set => _rejectClone = value; }
        public Game.Entity.EntityEffectDefinition Effect { get => _effect; set => _effect = value; }
        // 测试专用：预检用原件，提交时复制故意产生无效角色，验证目标修改后的回滚。
        private void OnEnable() { if (_rejectClone) _role = PlayerCombatNodeRole.Recovery; }
        public override CombatGraphFixtureState CreateState() => throw new InvalidOperationException("静态图校验不能创建运行状态。");
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "finish", "normal", "delayed", "ground", "fall", "jump", "ground-dash", "air-dash", "air-jump", "restart-ground", "restart-air" };
        #endregion
    }
}
