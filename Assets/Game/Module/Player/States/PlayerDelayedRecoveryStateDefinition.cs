using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    /// <summary>特殊收刀从进入起拥有独立节点和表现，复用普通收刀的状态实现。</summary>
    public sealed class PlayerDelayedRecoveryStateDefinition : EmberFsmStateDefinition<PlayerContext, PlayerAttackRecoveryState>, IPlayerCombatNodeDefinition
    {
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.Recovery;
        public override PlayerAttackRecoveryState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => PlayerCombatTransitions.Build<PlayerAttackRecoveryState>(true, true);
    }
}
