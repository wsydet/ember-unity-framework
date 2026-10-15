using System;
using Ember.Core.Fsm;
using Game.Entity;

namespace Game.Player
{
    public sealed class PlayerHeadJumpState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "head-jump";
        public static readonly EmberFsmTransition<PlayerContext, PlayerHeadJumpState> Fall =
            new("apex", "fall", description: "踩头反弹上升结束或碰顶，进入下落",
                sites: new[] { PlayerRule.Fixed<PlayerHeadJumpState>(10, (state, ctx) => ctx.Motor.Velocity.y <= 0, "ctx.Motor.Velocity.y <= 0", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.HeadJump.CanStart ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            if (!Context.HeadJump.CanStart || !Context.Abilities.TryGet(ABILITY_ID, out var ability) ||
                ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available)
                throw new InvalidOperationException("踩头反弹资源提交失败。");
            if (!Context.Input.ConsumeJump()) throw new InvalidOperationException("踩头反弹输入提交失败。");
            Context.Combat?.Dash?.Cancel();
            Context.HeadJump.Begin(); Context.JumpWindow.Consume(); Context.Wall.Cancel(); Context.VerticalMotion.NotifyJump();
            Context.Motor.SetHorizontalVelocity(Context.Motor.Velocity.x);
            Context.Motor.SetVerticalVelocity(Context.Feel.HeadJumpSpeed);
        }
        public override void FixedUpdate(float deltaTime) {
            Activation.AttemptGroup("decision");
        }
        public override void Exit() => Context.HeadJump.End();
        public override void Dispose() => Context?.HeadJump.End();
        #endregion
    }
}
