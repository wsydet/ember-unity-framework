using System;
using Ember.Core.Fsm;
using Game.Entity;

namespace Game.Player
{
    /// <summary>空中动作独立状态；资源归 Ability，运动参数归本宿主的手感快照。</summary>
    public sealed class PlayerAirJumpState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        public const string ABILITY_ID = "air-jump";
        public static readonly EmberFsmTransition<PlayerContext, PlayerAirJumpState> Fall =
            new("apex", "fall", description: "二段跳上升结束或碰顶，转为下落",
                sites: new[] { PlayerRule.Fixed<PlayerAirJumpState>(10, (state, ctx) => ctx.Motor.Velocity.y <= 0, "ctx.Motor.Velocity.y <= 0", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.CanStartAirJump
            ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            if (!Context.CanStartAirJump || !Context.Abilities.TryGet(ABILITY_ID, out var ability))
                throw new InvalidOperationException("二段跳提交条件已失效。");
            // Guard/CanEnter 不申请尝试、不扣次数；实际进入才提交一次。
            if (ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available)
                throw new InvalidOperationException("二段跳资源提交失败。");
            if (!Context.Input.ConsumeJump()) throw new InvalidOperationException("二段跳输入提交失败。");
            Context.Combat?.Dash?.Cancel();
            Context.JumpWindow.Consume();
            Context.Wall.Cancel();
            Context.VerticalMotion.NotifyJump();
            Context.Motor.SetVerticalVelocity(Context.Feel.AirJumpSpeed);
            Context.Motor.SetHorizontalVelocity(Context.Feel.AirJumpHorizontalMode == PlayerAirJumpHorizontalMode.FacingSpeed
                ? (Context.Facing.FacesRight ? 1 : -1) * Context.Feel.AirJumpHorizontalSpeed : Context.Motor.Velocity.x);
        }
        public override void FixedUpdate(float deltaTime)
        {
            Activation.AttemptGroup("decision");
        }
        #endregion
    }
}
