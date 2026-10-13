using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerLedgeClimbState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "ledge-climb";
        public static readonly EmberFsmTransition<PlayerContext, PlayerLedgeClimbState> Fall =
            new("finish-or-blocked", "fall", description: "抵达平台顶或途中受阻，交回常规接地／下落判断",
                sites: new[] { PlayerRule.Fixed<PlayerLedgeClimbState>(10, (state, ctx) => ctx.Ledge.Failed || ctx.Ledge.Completed, "ctx.Ledge.Failed || ctx.Ledge.Completed", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.Ledge.CanClimb
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Ledge.BeginClimb(); }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        public override void Exit() => Context.Ledge.End();
        public override void Dispose() => Context?.Ledge.Dispose();
        #endregion
    }
}
