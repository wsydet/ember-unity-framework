using System;
using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerJumpState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private float _launchSpeed => Context.Feel.LaunchSpeed;
        private float _releaseSpeed => Context.Feel.ReleaseSpeed;
        private bool _enableShortJump => Context.Feel.ShortJump;
        private bool _heightControl;
        private bool _releasePending;
        private float _launchHeight;
        private float _responseRemaining;
        public float ResponseRemaining => _responseRemaining;
        private ulong _launchPressVersion;
        private ulong _launchClearVersion;
        public static readonly EmberFsmTransition<PlayerContext, PlayerJumpState> Fall =
            new("apex", "fall", description: "上升结束或顶头，转为下落",
                sites: new[] { PlayerRule.Fixed<PlayerJumpState>(10, (state, ctx) => ctx.Motor.Velocity.y <= 0, "ctx.Motor.Velocity.y <= 0", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ApplyJumpResponse()
        {
            // Jump 在父级常规水平运动之后明确取得此窗口的水平控制权。
            if (_responseRemaining > .000001f && Context.Input.Horizontal != 0)
                Context.Motor.SetHorizontalVelocity(Context.Input.Horizontal * Context.Feel.EffectiveAirSpeed);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.Motor.Velocity.y > 0 || Context.CanStartJump
                ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            _heightControl = false;
            _releasePending = false;
            _responseRemaining = 0;
            // 空中进度只恢复速度；没有本次合法起跳机会就不接续旧跳跃的按键高度控制。
            if (!Context.CanStartJump && Context.Motor.Velocity.y > 0) return;
            if (!Context.Input.ConsumeJump()) throw new InvalidOperationException("进入跳跃时起跳请求已失效。");
            Context.Combat?.Dash?.Cancel();
            Context.JumpWindow.Consume();
            // 二段跳专用规则：仅本次有效普通起跳补满，恢复上升路径已在上面返回。
            if (Context.Abilities.TryGet(PlayerAirJumpState.ABILITY_ID, out var airJump)) airJump.RefillUses();
            Context.Wall.Cancel();
            Context.VerticalMotion.NotifyJump();
            _launchPressVersion = Context.Input.JumpPressVersion;
            _launchClearVersion = Context.Input.ClearVersion;
            _launchHeight = Context.Motor.Position.y;
            _heightControl = _enableShortJump;
            _releasePending = !Context.Input.JumpHeld;
            bool immediateLimit = _heightControl && _releasePending &&
                Context.Feel.ShortJumpMode == PlayerShortJumpMode.ReleaseSpeedLimit;
            bool immediateReduction = _heightControl && _releasePending &&
                Context.Feel.ShortJumpMode == PlayerShortJumpMode.MinimumHeightVelocityReduction && Context.Feel.MinimumHeight == 0;
            // 已释放的缓冲请求仍只消费一次，并遵循所选短跳策略。
            Context.Motor.SetVerticalVelocity(immediateLimit ? _releaseSpeed :
                immediateReduction ? _launchSpeed * Context.Feel.ReleaseMultiplier : _launchSpeed);
            if (immediateLimit || immediateReduction) _heightControl = false;
            _responseRemaining = Context.Feel.JumpResponseDuration;
            ApplyJumpResponse();
        }
        public override void FixedUpdate(float deltaTime)
        {
            if (Activation.AttemptGroup("decision").ShouldStop) return;
            _responseRemaining = UnityEngine.Mathf.Max(0, _responseRemaining - deltaTime);
            ApplyJumpResponse();
            if (Context.Input.ClearVersion != _launchClearVersion) _heightControl = false;
            // 重新按下必然经过释放；版本号也能识别两次物理步之间已经释放又按住的情况。
            if (_heightControl && (!Context.Input.JumpHeld || Context.Input.JumpPressVersion != _launchPressVersion))
                _releasePending = true;
            if (_heightControl && _releasePending)
            {
                if (Context.Feel.ShortJumpMode == PlayerShortJumpMode.MinimumHeightVelocityReduction)
                {
                    if (Context.Motor.Position.y - _launchHeight < Context.Feel.MinimumHeight) return;
                    Context.Motor.SetVerticalVelocity(Context.ExternalMotion.SelfVelocity.y * Context.Feel.ReleaseMultiplier);
                }
                else Context.Motor.SetVerticalVelocity(UnityEngine.Mathf.Min(Context.ExternalMotion.SelfVelocity.y, _releaseSpeed));
                _heightControl = false;
            }
        }
        public override void Pause() => _heightControl = false;
        public override void Exit() { _heightControl = false; _releasePending = false; _responseRemaining = 0; }
        #endregion
    }
}
