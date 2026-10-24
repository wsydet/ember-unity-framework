using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerGroundDashState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly PlayerDashEffects _effects;
        public const string SKILL_ID = "ground-dash";
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundDashState> Ground =
            new("finish-ground", "ground", description: "冲刺结束或中断，返回有地面支撑的分支",
                sites: new[] { PlayerRule.Fixed<PlayerGroundDashState>(10, (state, ctx) => ctx.Dash.EndReason != PlayerDashEndReason.None && ctx.JumpWindow.HasGroundSupport, "ctx.Dash.EndReason != PlayerDashEndReason.None && ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerGroundDashState> Fall =
            new("finish-air", "fall", description: "冲刺结束或中断，返回下落",
                sites: new[] { PlayerRule.Fixed<PlayerGroundDashState>(20, (state, ctx) => ctx.Dash.EndReason != PlayerDashEndReason.None && !ctx.JumpWindow.HasGroundSupport, "ctx.Dash.EndReason != PlayerDashEndReason.None && !ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerGroundDashState(Game.Entity.EntityEffectDefinition action = null, Game.Entity.EntityEffectDefinition tail = null)
            => _effects = new PlayerDashEffects(action, tail);
        public override EmberFsmEntryDecision CanEnter() => Context.Dash.CanStart(true)
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        { base.Enter(activation); Context.Dash.Begin(true); _effects.Enter(Context, SKILL_ID); }
        public override void FixedUpdate(float deltaTime)
        {
            Context.Dash.Advance(deltaTime);
            Activation.AttemptGroup("decision");
        }
        public override void Exit()
        { Context.Dash.End(); _effects.Exit(Context, SKILL_ID); }
        public override void Dispose() => _effects.Dispose();
        #endregion
    }
}
