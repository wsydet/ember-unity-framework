using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Entity
{
    /// <summary>静止实体墙顶查询。墙顶来自真实射线；角色空间使用全部刚体碰撞体的保守包围盒。</summary>
    public sealed class EntityLedgeProbe2D
    {
        #region 内部参数
        private readonly Entity2D _entity;
        private readonly Collider2D _shape;
        private readonly Rigidbody2D _body;
        private readonly List<RaycastHit2D> _hits = new(8);
        private readonly List<Collider2D> _overlaps = new(8);
        private readonly List<Collider2D> _shapes = new(4);
        public readonly struct Candidate
        {
            public readonly Collider2D Surface;
            public readonly int Direction;
            public readonly Vector2 Corner, HangPosition, RaisedPosition, StandPosition;
            private readonly Matrix4x4 _matrix;
            private readonly Bounds _bounds;
            private readonly uint _shapeHash;
            internal Candidate(Collider2D surface, int direction, Vector2 corner, Vector2 hang, Vector2 raised, Vector2 stand)
            {
                Surface = surface; Direction = direction; Corner = corner;
                HangPosition = hang; RaisedPosition = raised; StandPosition = stand;
                _matrix = surface.transform.localToWorldMatrix; _bounds = surface.bounds;
                _shapeHash = surface.GetShapeHash();
            }
            /// <summary>候选绑定采集时的静止几何；销毁、禁用、移动或改变边界后失效。</summary>
            public bool IsValid => IsStaticSolid(Surface) && Surface.transform.localToWorldMatrix == _matrix &&
                Surface.bounds == _bounds && Surface.GetShapeHash() == _shapeHash;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static bool IsStaticSolid(Collider2D collider)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger) return false;
            var body = collider.attachedRigidbody;
            if (body != null && (!body.simulated || body.bodyType != RigidbodyType2D.Static)) return false;
            if (!collider.usedByEffector) return true;
            var effector = collider.GetComponent<PlatformEffector2D>();
            if (effector == null && body != null) effector = body.GetComponent<PlatformEffector2D>();
            return effector == null || !effector.enabled || !effector.useOneWay;
        }
        private bool Nearest(Vector2 origin, Vector2 direction, float distance, ContactFilter2D filter, out RaycastHit2D nearest)
        {
            _shape.gameObject.scene.GetPhysicsScene2D().Raycast(origin, direction, distance, filter, _hits);
            nearest = default;
            float best = float.PositiveInfinity;
            foreach (var hit in _hits)
            {
                if (_entity.Owns(hit.collider) || EntityCollisionIgnoreLease2D.IsIgnored(_shape, hit.collider)) continue;
                if (hit.distance >= best) continue;
                nearest = hit; best = hit.distance;
            }
            return nearest.collider != null;
        }
        private Bounds Envelope()
        {
            var bounds = _shape.bounds;
            _body.GetAttachedColliders(_shapes);
            foreach (var shape in _shapes)
                if (shape.enabled && shape.gameObject.activeInHierarchy && !shape.isTrigger) bounds.Encapsulate(shape.bounds);
            return bounds;
        }
        private bool Clear(Vector2 bodyPosition, Bounds envelope, ContactFilter2D filter)
        {
            var center = (Vector2)envelope.center + bodyPosition - _body.position;
            _shape.gameObject.scene.GetPhysicsScene2D().OverlapBox(center, envelope.size, 0, filter, _overlaps);
            foreach (var other in _overlaps)
            {
                if (other == null || _entity.Owns(other) || EntityCollisionIgnoreLease2D.IsIgnored(_shape, other)) continue;
                // 查询可能包含接触容差内的邻近体；先确认真实 AABB 相交，不能把预留间隙判为侵入。
                var b = other.bounds; var half = envelope.extents;
                if (b.max.x > center.x - half.x && b.min.x < center.x + half.x &&
                    b.max.y > center.y - half.y && b.min.y < center.y + half.y) return false;
            }
            return true;
        }
        private bool PathClear(Vector2 start, Vector2 end, ContactFilter2D filter, float skin)
        {
            var delta = end - start;
            if (delta.sqrMagnitude < .0000001f) return true;
            _body.Cast(start, _body.rotation, delta.normalized, filter, _hits, delta.magnitude + skin);
            foreach (var hit in _hits)
            {
                if (_entity.Owns(hit.collider) || EntityCollisionIgnoreLease2D.IsIgnored(_shape, hit.collider)) continue;
                if (Vector2.Dot(hit.normal, delta.normalized) < -.001f && hit.distance < delta.magnitude + skin) return false;
            }
            return true;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityLedgeProbe2D(Entity2D entity, Collider2D shape)
        {
            if (entity == null || shape == null || !entity.Owns(shape) || shape.attachedRigidbody == null)
                throw new ArgumentException("挂边查询需要属于实体且连接刚体的碰撞体。");
            _entity = entity; _shape = shape; _body = shape.attachedRigidbody;
        }
        /// <summary>只读几何查询；下降／朝墙和能力资格由玩家层判断，不在此消费。</summary>
        public bool TryFind(int direction, LayerMask layers, float reach, float verticalReach, float gripInset, float skin, out Candidate candidate)
        {
            candidate = default;
            if ((direction != -1 && direction != 1) || !float.IsFinite(reach) || reach <= 0 ||
                !float.IsFinite(verticalReach) || verticalReach <= 0 || !float.IsFinite(gripInset) || gripInset <= 0 ||
                !float.IsFinite(skin) || skin <= 0) throw new ArgumentOutOfRangeException(nameof(reach));
            if (!_shape.enabled || !_body.simulated || !_shape.gameObject.activeInHierarchy) return false;
            var bounds = Envelope();
            if (gripInset >= bounds.size.y) return false;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(layers);
            Vector2 hand = new(bounds.center.x, bounds.max.y - gripInset);
            if (!Nearest(hand, Vector2.right * direction, bounds.extents.x + reach, filter, out var wall) ||
                !IsStaticSolid(wall.collider) || Vector2.Dot(wall.normal, Vector2.left * direction) < .9f) return false;
            Vector2 topOrigin = new(wall.point.x + direction * skin * 2, hand.y + verticalReach);
            if (!Nearest(topOrigin, Vector2.down, verticalReach * 2, filter, out var top) ||
                top.collider != wall.collider || top.normal.y < .9f || top.distance <= 0) return false;
            Vector2 offset = (Vector2)bounds.center - _body.position;
            Vector2 hang = new Vector2(wall.point.x - direction * (bounds.extents.x + skin), top.point.y - bounds.extents.y + gripInset) - offset;
            Vector2 raised = new(hang.x, top.point.y + bounds.extents.y + skin - offset.y);
            Vector2 stand = new(top.point.x + direction * (bounds.extents.x + skin) - offset.x, raised.y);
            if (!Clear(hang, bounds, filter) || !PathClear(_body.position, hang, filter, skin * .5f)) return false;
            candidate = new Candidate(wall.collider, direction, top.point, hang, raised, stand);
            return true;
        }
        /// <summary>翻越入口完整路径检查；执行仍须逐步扫掠，不可依据结果直接传送。</summary>
        public bool CanClimb(Candidate candidate, LayerMask layers, float skin)
        {
            if (!float.IsFinite(skin) || skin <= 0) throw new ArgumentOutOfRangeException(nameof(skin));
            if (!candidate.IsValid || !_body.simulated) return false;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(layers);
            var bounds = Envelope();
            var center = (Vector2)bounds.center + candidate.StandPosition - _body.position;
            for (int i = -1; i <= 1; i++)
            {
                var foot = new Vector2(center.x + i * bounds.extents.x * .9f, center.y - bounds.extents.y);
                if (!Nearest(foot, Vector2.down, skin * 2, filter, out var support) ||
                    support.collider != candidate.Surface || support.normal.y < .9f) return false;
            }
            return Clear(candidate.RaisedPosition, bounds, filter) && Clear(candidate.StandPosition, bounds, filter) &&
                PathClear(candidate.HangPosition, candidate.RaisedPosition, filter, skin * .5f) &&
                PathClear(candidate.RaisedPosition, candidate.StandPosition, filter, skin * .5f);
        }
        #endregion
    }
}
