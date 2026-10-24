using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player
{
    /// <summary>每检测会话复用三个原生形状；显式姿态查询，不依赖角色身体的接触排除层。</summary>
    internal sealed class PlayerHitQuery : IDisposable
    {
        private readonly GameObject _object;
        private readonly BoxCollider2D _box;
        private readonly CircleCollider2D _circle;
        private readonly PolygonCollider2D _polygon;
        private readonly List<Collider2D> _overlaps = new(32);
        private readonly List<RaycastHit2D> _casts = new(32);
        private readonly List<Vector2> _points = new();
        internal PlayerHitQuery(Scene scene)
        {
            _object = new GameObject("Combat query shapes") { hideFlags = HideFlags.HideAndDontSave, layer = 2 };
            SceneManager.MoveGameObjectToScene(_object, scene);
            var body = _object.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            _box = _object.AddComponent<BoxCollider2D>(); _circle = _object.AddComponent<CircleCollider2D>(); _polygon = _object.AddComponent<PolygonCollider2D>();
            foreach (var shape in new Collider2D[] { _box, _circle, _polygon })
            { shape.isTrigger = true; shape.excludeLayers = -1; shape.enabled = false; }
        }
        internal void Query(PlayerHitShape shape, int direction, Vector2 from, Vector2 to, LayerMask layers, Action<Collider2D> found)
        {
            Collider2D collider;
            if (shape.Kind == PlayerHitShapeKind.Box) { _box.size = shape.Size; collider = _box; }
            else if (shape.Kind == PlayerHitShapeKind.Circle) { _circle.radius = shape.Radius; collider = _circle; }
            else
            {
                _points.Clear(); foreach (var point in shape.Points) _points.Add(new Vector2(point.x * direction, point.y));
                _polygon.SetPath(0, _points); collider = _polygon;
            }
            collider.enabled = true;
            try
            {
                var filter = new ContactFilter2D { useTriggers = true }; filter.SetLayerMask(layers);
                var offset = new Vector2(shape.Offset.x * direction, shape.Offset.y);
                from += offset; to += offset; float angle = shape.Angle * direction;
                _overlaps.Clear(); collider.Overlap(from, angle, filter, _overlaps);
                foreach (var hit in _overlaps) found(hit);
                var delta = to - from;
                if (delta.sqrMagnitude > .00000001f)
                {
                    _casts.Clear(); collider.Cast(from, angle, delta.normalized, filter, _casts, delta.magnitude, true);
                    foreach (var hit in _casts) found(hit.collider);
                }
                _overlaps.Clear(); collider.Overlap(to, angle, filter, _overlaps);
                foreach (var hit in _overlaps) found(hit);
            }
            finally { collider.enabled = false; }
        }
        public void Dispose() { if (_object != null) UnityEngine.Object.DestroyImmediate(_object); }
    }
}
