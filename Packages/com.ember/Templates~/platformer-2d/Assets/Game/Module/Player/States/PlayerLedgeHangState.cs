using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerLedgeHangState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "ledge-hang";
        public static readonly EmberFsmTransition<PlayerContext, PlayerLedgeHangState> Climb =
            new("climb", "ledge-climb", description: "向上或新按跳跃，通路允许时消费翻越能力",
                sites: new[] { PlayerRule.Fixed<PlayerLedgeHangState>(20, (state, ctx) => ctx.Ledge.CanClimb, "ctx.Ledge.CanClimb", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLedgeHangState> Fall =
            new("release", "fall", description: "向下／反向松手，或挂点失效后恢复下落",
                sites: new[] { PlayerRule.Fixed<PlayerLedgeHangState>(10, (state, ctx) => ctx.Ledge.Failed || ctx.Ledge.ShouldRelease, "ctx.Ledge.Failed || ctx.Ledge.ShouldRelease", stopOnMatch: true, commit: (state, ctx) => ctx.Ledge.ReleaseInput(), commitDescription: "最终预检通过后屏蔽原挂点并消费松手时的 Jump；拒绝不消费") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.Ledge.TryCandidate(out _)
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Ledge.BeginHang(); }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        public override void Exit() => Context.Ledge.End();
        public override void Dispose() => Context?.Ledge.Dispose();
        #endregion
    }
}
