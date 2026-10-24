using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerGroundState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundState> DropThrough =
            new("drop-through", "drop-through", description: "有效单向支撑时向下＋新按跳跃，消费下穿能力",
                sites: new[] { PlayerRule.Fixed<PlayerGroundState>(10, (state, ctx) => ctx.DropThrough.CanStart, "ctx.DropThrough.CanStart") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundState> GroundDash =
            new("ground-dash", "ground-dash", description: "有地面支撑时消费冲刺请求及地面技能资源",
                sites: new[] { PlayerRule.Fixed<PlayerGroundState>(30, (state, ctx) => ctx.Dash.CanStart(true), "ctx.Dash.CanStart(true)") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundState> AirDash =
            new("air-dash", "air-dash", description: "无地面支撑时消费冲刺请求及空中技能资源",
                sites: new[] { PlayerRule.Fixed<PlayerGroundState>(40, (state, ctx) => ctx.Dash.CanStart(false), "ctx.Dash.CanStart(false)") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundState> LeaveGround =
            new("leave-ground", "fall", description: "失去地面支撑，进入下落",
                sites: new[] { PlayerRule.Fixed<PlayerGroundState>(50, (state, ctx) => !ctx.JumpWindow.HasGroundSupport, "!ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundState> Jump =
            new("jump", "jump", description: "消费有效输入与地面/离地宽限机会，进入跳跃",
                sites: new[] { PlayerRule.Fixed<PlayerGroundState>(20, (state, ctx) => ctx.CanStartJump, "ctx.CanStartJump") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.JumpWindow.HasGroundSupport
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect("fall");
        public override void FixedUpdate(float deltaTime)
        {
            if (Context.MotionControl.Owns(PlayerMotionChannels.AutomaticTransitions)) return;
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}
