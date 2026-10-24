using Ember.Core.Fsm;

namespace Game.Player
{
    public abstract class PlayerLandingState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        protected abstract bool Hard { get; }
        public static readonly EmberFsmTransition<PlayerContext, PlayerLandingState> Ground =
            new("finished", "ground", description: "停顿结束，回到地面分支",
                sites: new[] { PlayerRule.Fixed<PlayerLandingState>(10, (state, ctx) => ctx.Landing.Remaining <= 0, "ctx.Landing.Remaining <= 0", segment: "elapsed", stopOnMatch: true, id: "elapsed") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLandingState> Fall =
            new("lost-support", "fall", description: "失去支撑，立即下落",
                sites: new[] { PlayerRule.Fixed<PlayerLandingState>(10, (state, ctx) => !ctx.JumpWindow.HasGroundSupport, "!ctx.JumpWindow.HasGroundSupport", segment: "before-motion", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLandingState> Jump =
            new("jump", "jump", description: "有效起跳取消普通停顿",
                sites: new[] { PlayerRule.Fixed<PlayerLandingState>(30, (state, ctx) => !state.Hard && ctx.CanStartJump, "!state.Hard && ctx.CanStartJump", segment: "before-motion") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLandingState> Dash =
            new("dash", "ground-dash", description: "地面冲刺取消普通停顿",
                sites: new[] { PlayerRule.Fixed<PlayerLandingState>(40, (state, ctx) => !state.Hard && ctx.Dash.CanStart(true), "!state.Hard && ctx.Dash.CanStart(true)", segment: "before-motion") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLandingState> Drop =
            new("drop", "drop-through", description: "下穿取消普通停顿",
                sites: new[] { PlayerRule.Fixed<PlayerLandingState>(20, (state, ctx) => !state.Hard && ctx.DropThrough.CanStart, "!state.Hard && ctx.DropThrough.CanStart", segment: "before-motion") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => (Hard ? Context.Landing.HardPending : Context.Landing.NormalPending)
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Landing.Begin(Hard); }
        public override void FixedUpdate(float deltaTime)
        {
            if (Activation.AttemptGroup("before-motion").ShouldStop) return;
            Context.Landing.Advance(deltaTime);
            if (Activation.AttemptGroup("elapsed").ShouldStop) return;
            Context.Landing.Hold();
        }
        public override void Exit() => Context.Landing.End();
        public override void Dispose() => Context?.Landing.End();
        #endregion
    }
    public sealed class PlayerLandState : PlayerLandingState { protected override bool Hard => false; }
    public sealed class PlayerHardLandState : PlayerLandingState { protected override bool Hard => true; }
}
