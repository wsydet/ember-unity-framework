using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    /// <summary>有停顿连接的攻击声明；仍复用同一攻击运行状态。</summary>
    public sealed class PlayerDelayedAttackStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAttackState>, IPlayerCombatNodeDefinition
    {
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.Attack;
        public override PlayerAttackState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => PlayerCombatTransitions.Build<PlayerAttackState>(false, true);
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "delayed", "restart-ground", "restart-air" };
    }
}
