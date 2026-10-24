using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerDecelerateState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerDecelerateState> Idle =
            new("idle", "idle", description: "地面阶段：Idle",
                sites: new[] { PlayerRule.Fixed<PlayerDecelerateState>(10, (state, ctx) => ctx.GroundMotionState == "idle", "ctx.GroundMotionState == \"idle\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerDecelerateState> Move =
            new("move", "move", description: "地面阶段：Move",
                sites: new[] { PlayerRule.Fixed<PlayerDecelerateState>(20, (state, ctx) => ctx.GroundMotionState == "move", "ctx.GroundMotionState == \"move\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerDecelerateState> Accelerate =
            new("accelerate", "accelerate", description: "地面阶段：Accelerate",
                sites: new[] { PlayerRule.Fixed<PlayerDecelerateState>(30, (state, ctx) => ctx.GroundMotionState == "accelerate", "ctx.GroundMotionState == \"accelerate\"", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.GroundMotionState == "decelerate"
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect(Context.GroundMotionState);
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}

