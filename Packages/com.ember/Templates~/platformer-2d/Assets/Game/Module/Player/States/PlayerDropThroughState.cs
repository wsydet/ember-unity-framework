using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerDropThroughState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "drop-through";
        public static readonly EmberFsmTransition<PlayerContext, PlayerDropThroughState> Ground =
            new("clear-ground", "ground", description: "完全离开目标平台，返回其他地面支撑",
                sites: new[] { PlayerRule.Fixed<PlayerDropThroughState>(10, (state, ctx) => ctx.DropThrough.HasCleared() && ctx.JumpWindow.HasGroundSupport, "ctx.DropThrough.HasCleared() && ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        public static readonly EmberFsmTransition<PlayerContext, PlayerDropThroughState> Fall =
            new("clear-air", "fall", description: "完全离开目标平台，恢复碰撞并继续下落",
                sites: new[] { PlayerRule.Fixed<PlayerDropThroughState>(20, (state, ctx) => ctx.DropThrough.HasCleared() && !ctx.JumpWindow.HasGroundSupport, "ctx.DropThrough.HasCleared() && !ctx.JumpWindow.HasGroundSupport", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.DropThrough.CanStart
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        { base.Enter(activation); Context.DropThrough.Begin(); }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        public override void Exit() => Context.DropThrough.Dispose();
        public override void Dispose() => Context?.DropThrough.Dispose();
        #endregion
    }
}
