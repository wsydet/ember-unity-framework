using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerIdleState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerIdleState> Move =
            new("move", "move", description: "地面阶段：Move",
                sites: new[] { PlayerRule.Fixed<PlayerIdleState>(10, (state, ctx) => ctx.GroundMotionState == "move", "ctx.GroundMotionState == \"move\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerIdleState> Accelerate =
            new("accelerate", "accelerate", description: "地面阶段：Accelerate",
                sites: new[] { PlayerRule.Fixed<PlayerIdleState>(20, (state, ctx) => ctx.GroundMotionState == "accelerate", "ctx.GroundMotionState == \"accelerate\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerIdleState> Decelerate =
            new("decelerate", "decelerate", description: "地面阶段：Decelerate",
                sites: new[] { PlayerRule.Fixed<PlayerIdleState>(30, (state, ctx) => ctx.GroundMotionState == "decelerate", "ctx.GroundMotionState == \"decelerate\"", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.GroundMotionState == "idle"
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect(Context.GroundMotionState);
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}

