using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerAirDashState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly PlayerDashEffects _effects;
        public const string SKILL_ID = "air-dash";
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirDashState> Ground =
            new("finish-ground", "ground", description: "冲刺结束或中断，返回有地面支撑的分支",
                sites: new[] { PlayerRule.Fixed<PlayerAirDashState>(10, (state, ctx) => ctx.Dash.EndReason != PlayerDashEndReason.None && ctx.JumpWindow.HasGroundSupport, "ctx.Dash.EndReason != PlayerDashEndReason.None && ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirDashState> Fall =
            new("finish-air", "fall", description: "冲刺结束或中断，返回下落",
                sites: new[] { PlayerRule.Fixed<PlayerAirDashState>(20, (state, ctx) => ctx.Dash.EndReason != PlayerDashEndReason.None && !ctx.JumpWindow.HasGroundSupport, "ctx.Dash.EndReason != PlayerDashEndReason.None && !ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerAirDashState(Game.Entity.EntityEffectDefinition action = null, Game.Entity.EntityEffectDefinition tail = null)
            => _effects = new PlayerDashEffects(action, tail);
        public override EmberFsmEntryDecision CanEnter() => Context.Dash.CanStart(false)
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        { base.Enter(activation); Context.Dash.Begin(false); _effects.Enter(Context, SKILL_ID); }
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
