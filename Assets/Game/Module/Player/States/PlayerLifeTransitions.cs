using Ember.Core.Fsm;

namespace Game.Player
{
    public interface IPlayerLifeRootDefinition { }
    public interface IPlayerLifeNodeDefinition { }
    internal static class PlayerLifeTransitions
    {
        internal static EmberFsmTransition[] Build() => new EmberFsmTransition[]
        {
            new EmberFsmTransition<PlayerContext, PlayerLocomotionState>("life-death", "life-death", sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(0, (s,c) => c.Life.IsDead && !c.Life.DeathCommitted, "死亡优先且只提交一次", segment:"life") }),
            new EmberFsmTransition<PlayerContext, PlayerLocomotionState>("life-hit", "life-hit", mode: EmberFsmTransitionMode.ReenterTargetBranch, sites: new[] {
                PlayerRule.Fixed<PlayerLocomotionState>(1, (s,c) => !c.Life.IsDead && c.Life.Reaction?.HasPending == true, "已接受受击请求经FSM接管", segment:"life") })
        };
    }
}
