using System;
using Ember.Core.Fsm;
using Game.Entity;

namespace Game.Player
{
    public sealed class PlayerWallSlideState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "wall-slide";
        public static readonly EmberFsmTransition<PlayerContext, PlayerWallSlideState> Fall =
            new("leave-wall", "fall", description: "失墙、松方向或反向，退出墙滑",
                sites: new[] { PlayerRule.Fixed<PlayerWallSlideState>(10, (state, ctx) => !ctx.CanContinueWallSlide, "!ctx.CanContinueWallSlide", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.CanStartWallSlide
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            if (!Context.Abilities.TryGet(ABILITY_ID, out var ability) ||
                ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available)
                throw new InvalidOperationException("墙滑进入提交失效。");
            Context.Wall.Cancel(); Context.Wall.Sliding = true;
            Context.VerticalMotion.NotifyJump();
        }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        public override void Exit() => Context.Wall.EndSlide();
        #endregion
    }
}
