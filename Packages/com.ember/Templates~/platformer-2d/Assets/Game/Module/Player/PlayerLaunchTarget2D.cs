using System;
using Ember.Basic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>可选玩家弹射适配。原有内部互斥动作拒绝接管；项目动作先合法退出，再登记运动并发接受通知。</summary>
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
            if (context.ExternalMotion.IsInteractionLocked || !context.Actions.IsActive && context.MotionControl.Owns((PlayerMotionChannels)127) ||
                context.Dash.IsActive || context.Ledge.IsActive || context.Rolling.IsActive || context.Landing.IsActive ||
                context.DropThrough.IsActive || context.HeadJump.IsActive || context.Life?.Reaction?.IsActive == true ||
                context.Combat?.IsActive == true) return LaunchResult2D.Busy;
            if (request.Interrupt == LaunchInterrupt2D.RejectWhileBusy && (context.ExternalMotion.HasTransientMotion ||
                context.VerticalMotion.LaunchTail?.IsActive == true || context.Actions.IsActive))
                return LaunchResult2D.Busy;
            bool preserveHorizontal = request.Horizontal == LaunchHorizontal2D.PreserveControl;
            // 在退出旧动作/清理旧来源前完成校验，失败的候选不能破坏已接受的请求。
            var motion = new PlayerExternalMotionRequest(request.SourceId,
                request.Mode == LaunchMode2D.InitialVelocity ? PlayerExternalMotionMode.InitialVelocity : PlayerExternalMotionMode.ForcedRise,
                request.Velocity, request.Duration, lockInput: request.Mode == LaunchMode2D.ConstantRise,
                stopVerticalOnEnd: request.End == LaunchEnd2D.StopVertical,
                preserveHorizontalControl: preserveHorizontal, launchTail: request.Tail);
            if (!context.Actions.InterruptLaunch()) return LaunchResult2D.Busy;
            // 退出回调可能禁用宿主；不能向失效或被替换的会话登记弹射。
            if (!isActiveAndEnabled || !player.IsRunning || !ReferenceEquals(player.Context, context) ||
                context.Life?.IsDead == true || owner == null || !owner.isActiveAndEnabled)
                return LaunchResult2D.Inactive;
            if (preserveHorizontal || request.Tail.Enabled) context.ExternalMotion.ClearOverridesForLaunch();
            else context.ExternalMotion.Clear();
            _lease = context.ExternalMotion.Register(owner, motion);
            // 登记已成功；资格先于同步 Accepted 回调可见。拒绝路径不会到达这里。
            if (request.WallQualification == LaunchWallQualification2D.RefreshFirstVertical)
                context.Wall.RefreshFirstVertical();
            if (Accepted != null)
                foreach (Action<LaunchRequest2D> callback in Accepted.GetInvocationList())
                    try { callback(request); }
                    catch (Exception error) { EmberDebug.LogError("Game.Player", "弹射表现/项目动作通知失败：" + error); }
            return LaunchResult2D.Accepted;
        }
        #endregion
    }
}
