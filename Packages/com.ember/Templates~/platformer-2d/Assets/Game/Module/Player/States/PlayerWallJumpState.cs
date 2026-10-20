using System;
using Ember.Core.Fsm;
using Game.Entity;

namespace Game.Player
{
    public sealed class PlayerWallJumpState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "wall-jump";
        public static readonly EmberFsmTransition<PlayerContext, PlayerWallJumpState> Fall =
            new("apex", "fall", description: "墙跳上升结束，转入下落并保留独立控制窗口",
                sites: new[] { PlayerRule.Fixed<PlayerWallJumpState>(10, (state, ctx) => ctx.Motor.Velocity.y <= 0, "ctx.Motor.Velocity.y <= 0", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.CanStartWallJump
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            int wall = Context.ContactWall;
            if (!Context.CanStartWallJump || !Context.Abilities.TryGet(ABILITY_ID, out var ability) ||
                ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available || !Context.Input.ConsumeJump())
                throw new InvalidOperationException("墙跳进入提交失效。");
            Context.Combat?.Dash?.Cancel();
            Context.JumpWindow.Consume(); Context.VerticalMotion.BeginWall(Context.Input);
            Context.Wall.BeginJump(wall);
        }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}
