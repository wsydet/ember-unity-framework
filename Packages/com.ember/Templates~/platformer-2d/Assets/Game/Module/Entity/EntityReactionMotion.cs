using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>宿主推进的受击请求与运动。优先级仲裁不累计冲量，生命结算与动作提交分离。</summary>
    public sealed class EntityReactionMotion : IEntityReactionSink
    {
        #region 内部参数
        private EntityHit? _pending;
        private float _pendingDuration, _braking;
        private Vector2 _velocity;
        public bool HasPending => _pending.HasValue;
        public bool IsActive { get; private set; }
        public float Remaining { get; private set; }
        public int Priority { get; private set; }
        #endregion
        #region 外部方法
        public bool Request(in EntityHit hit)
        {
            int previousPriority = _pending?.Priority ?? (IsActive ? Priority : int.MinValue);
            if (hit.Priority < previousPriority) return false;
            float previousDuration = _pending.HasValue ? _pendingDuration : Remaining;
            _pending = hit;
            _pendingDuration = hit.Priority == previousPriority ? Math.Max(previousDuration, hit.StunSeconds) : hit.StunSeconds;
            return true;
        }
        public void Begin(EntityMotor2D motor)
        {
            if (!_pending.HasValue) throw new InvalidOperationException("受击动作必须有已接受的请求。");
            var hit = _pending.Value; _pending = null;
            IsActive = true; Remaining = _pendingDuration; Priority = hit.Priority; _braking = hit.Braking; _velocity = hit.Velocity;
            motor.SetHorizontalVelocity(_velocity.x); motor.SetVerticalVelocity(_velocity.y);
        }
        public void Advance(EntityMotor2D motor, float dt)
        {
            if (!IsActive || !float.IsFinite(dt) || dt < 0) throw new InvalidOperationException("受击推进状态或时间无效。");
            Remaining = Math.Max(0, Remaining - dt);
            // 使用碰撞后的实际水平速度，防止撞墙后又反复恢复初始冲量；竖直交给宿主重力和碰撞。
            _velocity.x = Mathf.MoveTowards(motor.Velocity.x, 0, _braking * dt);
            motor.SetHorizontalVelocity(_velocity.x);
        }
        public void End() { IsActive = false; Remaining = 0; }
        public void Clear() { _pending = null; _pendingDuration = 0; End(); }
        #endregion
    }
}
