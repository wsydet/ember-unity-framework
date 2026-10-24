using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>单宿主下穿豁免；状态持有、Controller 恢复模拟后重施加，不持久化动作。</summary>
    public sealed class PlayerDropThroughMotion : IDisposable
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly List<EntityCollisionIgnoreLease2D> _leases = new();
        private Collider2D[] _shapes;
        public Collider2D Target { get; private set; }
        public bool IsActive => _leases.Count != 0;
        internal bool CanStart => !IsActive && _context.Input.FreshJumpRequested && _context.Input.Vertical < 0
            && _context.JumpWindow.HasGroundSupport && _context.Sensors.OneWayGround != null
            && _context.Abilities.Query(PlayerDropThroughState.ABILITY_ID) == EntityUseStatus.Available;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerDropThroughMotion(PlayerContext context) => _context = context;
        internal void Begin()
        {
            if (!CanStart || !_context.Abilities.TryGet(PlayerDropThroughState.ABILITY_ID, out var ability))
                throw new InvalidOperationException("下穿提交条件已失效。");
            Target = _context.Sensors.OneWayGround;
            _shapes = _context.Entity.GetComponentsInChildren<Collider2D>(true);
            try
            {
                foreach (var shape in _shapes)
                    if (_context.Entity.Owns(shape) && shape.enabled && shape.gameObject.activeInHierarchy && !shape.isTrigger)
                        _leases.Add(EntityCollisionIgnoreLease2D.Acquire(shape, Target));
                if (!IsActive || ability.TryCommit(ability.CreateAttempt()) != EntityUseStatus.Available)
                    throw new InvalidOperationException("下穿资源提交失败。");
                if (!_context.Input.ConsumeJump()) throw new InvalidOperationException("下穿输入提交失败。");
                _context.Combat?.Dash?.Cancel();
                _context.JumpWindow.ClearSupport();
                _context.VerticalMotion.NotifyJump(); // 清悬停并抑制本次主动离地的悬停。
                _context.Wall.Cancel();
                _context.Motor.SetVerticalVelocity(-_context.Feel.DropThroughSpeed);
            }
            catch { Dispose(); throw; }
        }
        /// <summary>全部实体碰撞体离开目标下方或侧方才释放，不因起始脚底间隙提前恢复。</summary>
        internal bool HasCleared()
        {
            if (Target == null || !Target.enabled || !Target.gameObject.activeInHierarchy) return true;
            var target = Target.bounds;
            foreach (var shape in _shapes)
            {
                if (shape == null || !_context.Entity.Owns(shape) || !shape.enabled || !shape.gameObject.activeInHierarchy || shape.isTrigger) continue;
                var bounds = shape.bounds;
                if (bounds.max.y >= target.min.y - .01f && bounds.max.x >= target.min.x - .01f && bounds.min.x <= target.max.x + .01f)
                    return false;
            }
            return true;
        }
        internal void Reapply() { foreach (var lease in _leases) lease.Reapply(); }
        public void Dispose()
        {
            foreach (var lease in _leases) lease.Dispose();
            _leases.Clear(); Target = null; _shapes = null;
        }
        #endregion
    }
}
