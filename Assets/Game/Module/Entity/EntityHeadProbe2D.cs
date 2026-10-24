using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Entity
{
    /// <summary>宿主显式采样的脚底扫掠；只生成本物理步从上方穿入的候选。</summary>
    public sealed class EntityHeadProbe2D
    {
        #region 内部参数
        private readonly Entity2D _entity;
        private readonly Collider2D _shape;
        private readonly List<Collider2D> _hits = new(8);
        private readonly HashSet<EntityHeadTarget2D> _occupied = new();
        private readonly List<EntityHeadTarget2D> _released = new();
        private Bounds _previous;
        private bool _initialized;
        public EntityHeadTarget2D Candidate { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityHeadProbe2D(Entity2D entity, Collider2D shape) { _entity = entity; _shape = shape; }
        public void Sample(bool enabled)
        {
            Candidate = null;
            var current = _shape.bounds;
            _released.Clear();
            foreach (var target in _occupied)
                if (target == null || !target.IsAvailable || !current.Intersects(target.Head.bounds)) _released.Add(target);
            foreach (var target in _released) _occupied.Remove(target);
            if (_initialized && enabled && current.min.y < _previous.min.y)
            {
                var sweep = _previous; sweep.Encapsulate(current);
                var filter = new ContactFilter2D { useTriggers = true }; filter.SetLayerMask(Physics2D.AllLayers);
                _shape.gameObject.scene.GetPhysicsScene2D().OverlapBox(sweep.center, sweep.size, 0, filter, _hits);
                float bestTop = float.NegativeInfinity;
                foreach (var hit in _hits)
                {
                    if (!hit.isTrigger || _entity.Owns(hit) || !hit.TryGetComponent<EntityHeadTarget2D>(out var target) ||
                        !target.IsAvailable || target.Head != hit || _occupied.Contains(target)) continue;
                    var head = hit.bounds; float top = head.max.y;
                    if (_previous.min.y < top || current.min.y > top) continue;
                    float t = (_previous.min.y - top) / (_previous.min.y - current.min.y);
                    float x = Mathf.Lerp(_previous.center.x, current.center.x, t);
                    float halfWidth = Mathf.Min(_previous.extents.x, current.extents.x);
                    if (x + halfWidth <= head.min.x || x - halfWidth >= head.max.x) continue;
                    _occupied.Add(target);
                    // 优先沿下降路径最先到达的头部；本步最多提交一个候选。
                    if (top > bestTop)
                    { Candidate = target; bestTop = top; }
                }
            }
            _previous = current; _initialized = true;
        }
        public void Clear() { Candidate = null; _occupied.Clear(); _initialized = false; }
        #endregion
    }
}
