using System;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerDashEndReason { None, Completed, Wall, GroundLost, Landed, Interrupted }

    /// <summary>单宿主冲刺窗口。状态负责进入、推进和退出；不自行驱动，不持久化动作。</summary>
    public sealed class PlayerDashMotion
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private PlayerDashMotionSpec _spec;
        public bool IsActive { get; private set; }
        public bool IsGroundDash { get; private set; }
        public int Direction { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => IsActive ? Mathf.Max(0, _spec.Duration - Elapsed) : 0;
        public PlayerDashEndReason EndReason { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Apply()
        {
            _context.Motor.SetHorizontalVelocity(Direction * _spec.SpeedAt(Elapsed));
            _context.Motor.SetVerticalVelocity(0);
            _context.VerticalMotion.OverrideGravity(0);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerDashMotion(PlayerContext context) => _context = context;
        internal bool CanStart(bool ground) => !IsActive && !_context.Ledge.IsActive && !_context.DropThrough.IsActive && _context.Input.DashRequested
            && _context.Wall.ProtectionRemaining <= 0 && _context.JumpWindow.HasGroundSupport == ground
            && _context.Skills.Query(ground ? PlayerGroundDashState.SKILL_ID : PlayerAirDashState.SKILL_ID) == EntityUseStatus.Available;

        internal void Begin(bool ground)
        {
            string id = ground ? PlayerGroundDashState.SKILL_ID : PlayerAirDashState.SKILL_ID;
            if (!CanStart(ground) || !_context.Skills.TryGet(id, out var skill))
                throw new InvalidOperationException("冲刺提交条件已失效：" + id);
            if (skill.TryCommit(skill.CreateAttempt()) != EntityUseStatus.Available)
                throw new InvalidOperationException("冲刺资源提交失败：" + id);
            if (!_context.Input.ConsumeDash()) throw new InvalidOperationException("冲刺输入提交失败。");
            // 墙滑 Exit 记录本步离开墙面，Enter 再消费它；预检不写入方向或资源。
            Direction = _context.Input.Horizontal != 0 ? (_context.Input.Horizontal > 0 ? 1 : -1)
                : _context.Wall.ExitedSlideWall != 0 ? -_context.Wall.ExitedSlideWall
                : _context.Facing.FacesRight ? 1 : -1;
            IsGroundDash = ground; _spec = ground ? _context.Feel.GroundDash : _context.Feel.AirDash;
            Elapsed = 0; EndReason = PlayerDashEndReason.None; IsActive = true;
            _context.Wall.Cancel(); _context.VerticalMotion.NotifyJump();
            _context.Facing.SetFacing(Direction > 0);
            Apply();
        }
        internal bool Advance(float deltaTime)
        {
            // 环境中断优先于到期，边界同帧不生成正常结束语义。
            if (Direction > 0 ? _context.Sensors.RightWall != null : _context.Sensors.LeftWall != null)
                EndReason = PlayerDashEndReason.Wall;
            else if (IsGroundDash && !_context.JumpWindow.HasGroundSupport) EndReason = PlayerDashEndReason.GroundLost;
            else if (!IsGroundDash && _context.JumpWindow.HasGroundSupport) EndReason = PlayerDashEndReason.Landed;
            else
            {
                Elapsed = Mathf.Min(_spec.Duration, Elapsed + deltaTime);
                if (Elapsed >= _spec.Duration - .000001f) EndReason = PlayerDashEndReason.Completed;
            }
            if (EndReason != PlayerDashEndReason.None) return true;
            Apply(); return false;
        }
        internal void End()
        {
            if (!IsActive) return;
            if (EndReason == PlayerDashEndReason.None) EndReason = PlayerDashEndReason.Interrupted;
            IsActive = false;
            _context.VerticalMotion.NotifyJump(); // 冲刺离地不继承自然离地悬停。
            float limit = _context.JumpWindow.HasGroundSupport ? _context.Feel.MoveSpeed : _context.Feel.EffectiveAirSpeed;
            _context.Motor.SetHorizontalVelocity(Mathf.Clamp(_context.Motor.Velocity.x, -limit, limit));
            _context.Motor.SetVerticalVelocity(0);
        }
        #endregion
    }
}
