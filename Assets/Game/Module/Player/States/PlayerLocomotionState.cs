using Ember.Core.Fsm;

namespace Game.Player
{
    /// <summary>整条活动路径的公共水平运动。父节点先执行，子节点只决定状态与竖直动作。</summary>
    public sealed class PlayerLocomotionState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly bool _projectActions;
        public static readonly EmberFsmTransition<PlayerContext, PlayerLocomotionState> Roll =
            new("rolling", "rolling", description: "真实落地时有水平输入，衔接翻滚",
                sites: new[] { PlayerRule.Fixed<PlayerLocomotionState>(20, (state, ctx) => ctx.Rolling.CanStart, "ctx.Rolling.CanStart") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLocomotionState> HardLand =
            new("hard-land", "hard-land", description: "真实落地冲击达到阈值，进入不可取消停顿",
                sites: new[] { PlayerRule.Fixed<PlayerLocomotionState>(10, (state, ctx) => ctx.Landing.HardPending, "ctx.Landing.HardPending") });
        public static readonly EmberFsmTransition<PlayerContext, PlayerLocomotionState> Land =
            new("land-pause", "land", description: "没有优先动作时进入普通落地停顿",
                sites: new[] { PlayerRule.Fixed<PlayerLocomotionState>(30, (state, ctx) => !ctx.MotionControl.Owns(PlayerMotionChannels.AutomaticTransitions) && ctx.Landing.NormalPending && !ctx.CanStartJump && !ctx.DropThrough.CanStart && !ctx.Dash.CanStart(true), "普通落地需当前动作交回自动转换权") });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerLocomotionState(bool projectActions = false) => _projectActions = projectActions;
        public override void FixedUpdate(float deltaTime)
        {
            if (Context.Life != null && Activation.AttemptGroup("life").ShouldStop) return;
            if (Context.Life?.IsDead == true || Context.Life?.Reaction?.IsActive == true) return;
            if (!Context.Actions.IsActive || !Context.MotionControl.Owns(PlayerMotionChannels.AutomaticTransitions))
            {
                if (_projectActions && Activation.AttemptGroup("project-action").ShouldStop) return;
                if (Activation.AttemptGroup("decision").ShouldStop) return;
                if (Context.Combat?.Dash?.OpportunityActive == true && Activation.AttemptGroup("dash-combat").ShouldStop) return;
                if (Context.Combat?.CanOfferEntry == true && Activation.AttemptGroup("combat").ShouldStop) return;
            }
            if (Context.Landing.IsActive || Context.Rolling.IsActive) { Context.Combat?.Dash?.Cancel(); return; }
            if (Context.Dash.IsActive || Context.Ledge.IsActive) return;
            Context.Combat?.Dash?.AdvanceResidual(deltaTime);
            float direction = Context.Input.Horizontal;
            if (!Context.MotionControl.Owns(PlayerMotionChannels.Horizontal))
                Context.Motor.SetHorizontalVelocity(Context.Feel.CalculateHorizontalSpeed(
                    Context.ExternalMotion.SelfVelocity.x, direction, Context.JumpWindow.HasGroundSupport, deltaTime));
            if (direction != 0 && !Context.MotionControl.Owns(PlayerMotionChannels.Facing)) Context.Facing.SetFacing(direction > 0);
        }
        #endregion
    }
}
