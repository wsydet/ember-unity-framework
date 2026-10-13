using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerRollingState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<PlayerContext, PlayerRollingState> Ground =
            new("finished", "ground", description: "翻滚到期或撞墙，恢复地面控制",
                sites: new[] { PlayerRule.Fixed<PlayerRollingState>(50, (state, ctx) => ctx.Rolling.WallAhead, "ctx.Rolling.WallAhead", segment: "before-motion", stopOnMatch: true, id: "wall", commit: (state, ctx) => ctx.Rolling.StopAtWall(), commitDescription: "最终预检通过后标记撞墙；Exit 按此归零水平速度，拒绝不标记"),
                    PlayerRule.Fixed<PlayerRollingState>(10, (state, ctx) => ctx.Rolling.Remaining <= .000001f, "ctx.Rolling.Remaining <= .000001f", segment: "elapsed", stopOnMatch: true, id: "elapsed") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerRollingState> Fall =
            new("lost-support", "fall", description: "翻滚失去支撑，立即下落",
                sites: new[] { PlayerRule.Fixed<PlayerRollingState>(10, (state, ctx) => !ctx.JumpWindow.HasGroundSupport, "!ctx.JumpWindow.HasGroundSupport", segment: "before-motion", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerRollingState> Jump =
            new("jump", "jump", description: "有效起跳取消翻滚",
                sites: new[] { PlayerRule.Fixed<PlayerRollingState>(30, (state, ctx) => ctx.CanStartJump, "ctx.CanStartJump", segment: "before-motion") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerRollingState> Dash =
            new("dash", "ground-dash", description: "地面冲刺取消翻滚",
                sites: new[] { PlayerRule.Fixed<PlayerRollingState>(40, (state, ctx) => ctx.Dash.CanStart(true), "ctx.Dash.CanStart(true)", segment: "before-motion") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerRollingState> Drop =
            new("drop", "drop-through", description: "下穿取消翻滚",
                sites: new[] { PlayerRule.Fixed<PlayerRollingState>(20, (state, ctx) => ctx.DropThrough.CanStart, "ctx.DropThrough.CanStart", segment: "before-motion") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.Rolling.CanStart ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Rolling.Begin(); }
        public override void FixedUpdate(float deltaTime)
        {
            if (Activation.AttemptGroup("before-motion").ShouldStop) return;
            Context.Rolling.Advance(deltaTime);
            if (Activation.AttemptGroup("elapsed").ShouldStop) return;
            Context.Rolling.Apply();
        }
        public override void Exit() => Context.Rolling.End();
        public override void Dispose() => Context?.Rolling.End();
        #endregion
    }
}
