using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerAirState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> HeadJump =
            new("head-jump", "head-jump", description: "下降从上方穿入头部，消费跳跃请求与踩头能力", mode: EmberFsmTransitionMode.ReenterTargetBranch,
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(30, (state, ctx) => ctx.HeadJump.CanStart, "ctx.HeadJump.CanStart") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> Hang =
            new("ledge-hang", "ledge-hang", description: "下降且朝向可挂墙顶，消费挂边能力",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(90, (state, ctx) => ctx.Ledge.TryCandidate(out _), "ctx.Ledge.TryCandidate(out _)") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> DropThrough =
            new("drop-through", "drop-through", description: "有效单向支撑时向下＋新按跳跃，消费下穿能力",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(10, (state, ctx) => ctx.DropThrough.CanStart, "ctx.DropThrough.CanStart") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> GroundDash =
            new("ground-dash", "ground-dash", description: "有地面支撑时消费冲刺请求及地面技能资源",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(60, (state, ctx) => ctx.Dash.CanStart(true), "ctx.Dash.CanStart(true)") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> AirDash =
            new("air-dash", "air-dash", description: "无地面支撑时消费冲刺请求及空中技能资源",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(70, (state, ctx) => ctx.Dash.CanStart(false), "ctx.Dash.CanStart(false)") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> Land =
            new("land", "ground", description: "统一接触支撑有效，返回地面分支",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(80, (state, ctx) => ctx.JumpWindow.HasGroundSupport, "ctx.JumpWindow.HasGroundSupport", stopOnMatch: true, commit: (state, ctx) => ctx.Wall.Cancel(), commitDescription: "最终预检通过后取消墙窗口，随后 Exit/Enter；拒绝不取消") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> Jump =
            new("buffer-or-coyote-jump", "jump", description: "离地宽限起跳，或本步落地时立即消费缓冲", mode: EmberFsmTransitionMode.ReenterTargetBranch,
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(20, (state, ctx) => ctx.CanStartJump, "ctx.CanStartJump") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> AirJump =
            new("air-jump", "air-jump", description: "消费空中起跳请求及一次二段跳资源", mode: EmberFsmTransitionMode.ReenterTargetBranch,
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(50, (state, ctx) => ctx.CanStartAirJump, "ctx.CanStartAirJump") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> WallJump =
            new("wall-jump", "wall-jump", description: "接触墙面，消费请求并离墙起跳", mode: EmberFsmTransitionMode.ReenterTargetBranch,
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(40, (state, ctx) => ctx.CanStartWallJump, "ctx.CanStartWallJump") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirState> WallSlide =
            new("wall-slide", "wall-slide", description: "空中下降且持续朝墙输入，开始墙滑",
                sites: new[] { PlayerRule.Fixed<PlayerAirState>(100, (state, ctx) => ctx.CanStartWallSlide, "ctx.CanStartWallSlide") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void FixedUpdate(float deltaTime)
        {
            if (Context.MotionControl.Owns(PlayerMotionChannels.AutomaticTransitions)) return;
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}
