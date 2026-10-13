using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerAccelerateState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerAccelerateState> Idle =
            new("idle", "idle", description: "地面阶段：Idle",
                sites: new[] { PlayerRule.Fixed<PlayerAccelerateState>(10, (state, ctx) => ctx.GroundMotionState == "idle", "ctx.GroundMotionState == \"idle\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAccelerateState> Move =
            new("move", "move", description: "地面阶段：Move",
                sites: new[] { PlayerRule.Fixed<PlayerAccelerateState>(20, (state, ctx) => ctx.GroundMotionState == "move", "ctx.GroundMotionState == \"move\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAccelerateState> Decelerate =
            new("decelerate", "decelerate", description: "地面阶段：Decelerate",
                sites: new[] { PlayerRule.Fixed<PlayerAccelerateState>(30, (state, ctx) => ctx.GroundMotionState == "decelerate", "ctx.GroundMotionState == \"decelerate\"", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.GroundMotionState == "accelerate"
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect(Context.GroundMotionState);
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}

