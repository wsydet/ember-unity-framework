using System.Collections.Generic;
using Game.Entity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player
{
    /// <summary>有限距离的顶角、冲刺和台阶补正；查询不驱动状态，位移只由 Motor 提交。</summary>
    public sealed class PlayerCollisionCorrection
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly List<RaycastHit2D> _hits = new(16);
        private readonly List<Collider2D> _overlaps = new(16);
        private readonly List<Collider2D> _shapes = new(4);
        private readonly ContactFilter2D _filter = new() { useTriggers = false };
        private Bounds _envelope;
        private bool _corrected;
        public Vector2 LastOffset { get; private set; }
        private Rigidbody2D Body => _context.Motor.Body;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool External(Collider2D other)
        {
            if (other == null || _context.Entity.Owns(other)) return false;
            foreach (var shape in _shapes)
                if (shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy &&
                    !Physics2D.GetIgnoreLayerCollision(shape.gameObject.layer, other.gameObject.layer) &&
                    !EntityCollisionIgnoreLease2D.IsIgnored(shape, other)) return true;
            return false;
        }

        private static bool StaticSolid(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.bodyType != RigidbodyType2D.Static) return false;
            // 单向平台与其他 Effector 不作为主动绕行目标；安全路径仍保守检查它们。
            return !other.usedByEffector;
        }

        private bool Blocked(Vector2 start, Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            Body.Cast(start, Body.rotation, direction, _filter, _hits, distance);
            nearest = default;
            foreach (var hit in _hits)
            {
                if (!External(hit.collider) || Vector2.Dot(hit.normal, direction) >= -.001f) continue;
                if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
            }
            return nearest.collider != null;
        }

        private bool Clear(Vector2 position, float skin)
        {
            Vector2 center = (Vector2)_envelope.center + position - Body.position;
            Vector2 size = (Vector2)_envelope.size + Vector2.one * (skin * 2);
            Body.gameObject.scene.GetPhysicsScene2D().OverlapBox(center, size, 0, _filter, _overlaps);
            foreach (var other in _overlaps)
            {
                if (!External(other)) continue;
                var bounds = other.bounds;
                if (bounds.max.x > center.x - size.x * .5f && bounds.min.x < center.x + size.x * .5f &&
                    bounds.max.y > center.y - size.y * .5f && bounds.min.y < center.y + size.y * .5f) return false;
            }
            return true;
        }

        private bool TryOffset(Vector2 offset, Vector2 forward, float travel, float actualTravel, PlayerCorrectionSpec spec, bool support)
        {
            Vector2 current = Body.position, target = current + offset;
            if (Blocked(current, offset.normalized, offset.magnitude + spec.Skin, out _) || !Clear(target, spec.Skin) ||
                Blocked(target, forward, travel + spec.Skin, out _) || !Clear(target + forward * travel, spec.Skin)) return false;
            if (support && (!Blocked(target + forward * actualTravel, Vector2.down, spec.SupportDistance, out var ground) ||
                ground.normal.y < .65f || !StaticSolid(ground.collider))) return false;
            if (!_context.Motor.TryCorrectPosition(offset, spec.Skin)) return false;
            LastOffset = offset; _corrected = true;
            _context.Sensors.Sample();
            return true;
        }

        private bool TryCorrect(Vector2 forward, float speed, float dt, PlayerCorrectionSpec spec, bool support, bool upOnly)
        {
            if (!spec.Enabled || _corrected || !float.IsFinite(dt) || dt <= 0 || speed <= 0 ||
                _context.Motor.IsFrozen || !Body.simulated) return false;
            Body.GetAttachedColliders(_shapes);
            _envelope = _context.Sensors.Shape.bounds;
            foreach (var shape in _shapes)
                if (shape.enabled && !shape.isTrigger && shape.gameObject.activeInHierarchy) _envelope.Encapsulate(shape.bounds);
            float travel = Mathf.Max(spec.LookAhead, speed * dt);
            if (!float.IsFinite(travel + spec.Skin)) return false;
            if (!Blocked(Body.position, forward, travel + spec.Skin, out var obstacle) ||
                !StaticSolid(obstacle.collider) || (spec.ObstacleLayers.value & (1 << obstacle.collider.gameObject.layer)) == 0) return false;
            // 上升补正只处理顶角，不把普通侧墙当作可横移穿越的天花板。
            if (forward.y > 0 && obstacle.normal.y > -.5f) return false;
            var axis = forward.y > 0 ? Vector2.right : Vector2.up;
            float input = forward.y > 0 ? _context.Input.Horizontal : _context.Input.Vertical;
            int preferred = spec.Direction == PlayerCorrectionDirection.NegativeFirst || spec.Direction == PlayerCorrectionDirection.NegativeOnly ||
                spec.Direction == PlayerCorrectionDirection.InputFirst && input < 0 ? -1 : 1;
            if (upOnly) preferred = 1;
            bool oneSide = upOnly || spec.Direction == PlayerCorrectionDirection.PositiveOnly || spec.Direction == PlayerCorrectionDirection.NegativeOnly;
            int steps = Mathf.CeilToInt(spec.MaximumDistance / spec.Increment);
            for (int i = 1; i <= steps; i++)
            {
                float distance = Mathf.Min(spec.MaximumDistance, i * spec.Increment);
                if (TryOffset(axis * (distance * preferred), forward, travel, speed * dt, spec, support)) return true;
                if (!oneSide && TryOffset(axis * (-distance * preferred), forward, travel, speed * dt, spec, support)) return true;
            }
            return false;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCollisionCorrection(PlayerContext context) => _context = context;
        internal void BeginStep() { _corrected = false; LastOffset = Vector2.zero; }
        internal bool TryDash(float dt, float speed)
        {
            if (!_context.Dash.IsActive) return false;
            bool ground = _context.Dash.IsGroundDash;
            return TryCorrect(Vector2.right * _context.Dash.Direction, speed, dt,
                ground ? _context.Feel.GroundDashCorrection : _context.Feel.AirDashCorrection, ground, ground);
        }
        internal void Apply(float dt, string state)
        {
            if (_context.Ledge.IsActive || _context.DropThrough.IsActive || _context.Rolling.IsActive) return;
            var velocity = _context.Motor.CommandVelocity;
            if (_context.Dash.IsActive) { TryDash(dt, Mathf.Abs(velocity.x)); return; }
            if (velocity.y > 0 && (state == "jump" || state == "air-jump" || state == "wall-jump" || state == "head-jump"))
                TryCorrect(velocity.normalized, velocity.magnitude, dt, _context.Feel.JumpCorrection, false, false);
            else if (_context.JumpWindow.HasGroundSupport && _context.Input.Horizontal != 0 && Mathf.Abs(velocity.x) > .001f &&
                (state == "move" || state == "accelerate" || state == "decelerate"))
                TryCorrect(Vector2.right * Mathf.Sign(velocity.x), Mathf.Abs(velocity.x), dt, _context.Feel.StepCorrection, true, true);
        }
        #endregion
    }
}
