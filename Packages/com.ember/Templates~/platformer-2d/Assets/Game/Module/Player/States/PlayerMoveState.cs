using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerMoveState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerMoveState> Idle =
            new("idle", "idle", description: "地面阶段：Idle",
                sites: new[] { PlayerRule.Fixed<PlayerMoveState>(10, (state, ctx) => ctx.GroundMotionState == "idle", "ctx.GroundMotionState == \"idle\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerMoveState> Accelerate =
            new("accelerate", "accelerate", description: "地面阶段：Accelerate",
                sites: new[] { PlayerRule.Fixed<PlayerMoveState>(20, (state, ctx) => ctx.GroundMotionState == "accelerate", "ctx.GroundMotionState == \"accelerate\"", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerMoveState> Decelerate =
            new("decelerate", "decelerate", description: "地面阶段：Decelerate",
                sites: new[] { PlayerRule.Fixed<PlayerMoveState>(30, (state, ctx) => ctx.GroundMotionState == "decelerate", "ctx.GroundMotionState == \"decelerate\"", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.GroundMotionState == "move"
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect(Context.GroundMotionState);
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}

