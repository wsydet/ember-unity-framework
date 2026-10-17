using System;
using Ember.Basic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>可选玩家弹射适配。原有内部互斥动作拒绝接管；项目动作通过接受事件释放自己的会话。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController))]
    public sealed class PlayerLaunchTarget2D : MonoBehaviour, ILaunchTarget2D
    {
        #region 内部参数
        private PlayerExternalMotionLease _lease;
        public bool IsLaunching => _lease?.IsActive == true;
        public event Action<LaunchRequest2D> Accepted;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnDisable() { _lease?.Dispose(); _lease = null; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public LaunchResult2D TryLaunch(Behaviour owner, LaunchRequest2D request)
        {
            var player = GetComponent<PlayerController>();
            if (!isActiveAndEnabled || owner == null || !owner.isActiveAndEnabled || !player.IsRunning ||
                string.IsNullOrWhiteSpace(request.SourceId) || player.Context.Life?.IsDead == true) return LaunchResult2D.Inactive;
            var context = player.Context;
            if (context.ExternalMotion.IsInteractionLocked || context.MotionControl.Owns((PlayerMotionChannels)127) ||
                context.Dash.IsActive || context.Ledge.IsActive || context.Rolling.IsActive || context.Landing.IsActive ||
                context.DropThrough.IsActive || context.HeadJump.IsActive || context.Life?.Reaction?.IsActive == true ||
                context.Combat?.IsActive == true) return LaunchResult2D.Busy;
            if (request.Interrupt == LaunchInterrupt2D.RejectWhileBusy && context.ExternalMotion.HasTransientMotion)
                return LaunchResult2D.Busy;
            context.ExternalMotion.Clear();
            _lease = context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest(request.SourceId,
                request.Mode == LaunchMode2D.InitialVelocity ? PlayerExternalMotionMode.InitialVelocity : PlayerExternalMotionMode.ForcedRise,
                request.Velocity, request.Duration, lockInput: request.Mode == LaunchMode2D.ConstantRise,
                stopVerticalOnEnd: request.End == LaunchEnd2D.StopVertical));
            if (Accepted != null)
                foreach (Action<LaunchRequest2D> callback in Accepted.GetInvocationList())
                    try { callback(request); }
                    catch (Exception error) { EmberDebug.LogError("Game.Player", "弹射表现/项目动作通知失败：" + error); }
            return LaunchResult2D.Accepted;
        }
        #endregion
    }
}
