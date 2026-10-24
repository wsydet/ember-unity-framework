using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class PlayerHitWindow
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private float _start = .1f, _end = .3f;
        [SerializeField] private bool _singleSample;
        [SerializeField] private PlayerHitRetry _retry;
        [SerializeField] private LayerMask _layers = -1;
        [SerializeField] private bool _allowFriendly;
        [SerializeField] private EntityHitPayload _payload = new();
        [SerializeField] private PlayerHitKeyframe[] _keyframes = Array.Empty<PlayerHitKeyframe>();
        #endregion
        #region 外部方法
        public string Id => _id;
        public float Start => _start;
        public float End => _end;
        public bool SingleSample => _singleSample;
        public PlayerHitRetry Retry => _retry;
        public LayerMask Layers => _layers;
        public bool AllowFriendly => _allowFriendly;
        public EntityHitPayload Payload => _payload;
        public IReadOnlyList<PlayerHitKeyframe> Keyframes => Array.AsReadOnly(_keyframes);
        internal void Validate(float duration, string parent)
        {
            string p = parent + ".Window[" + _id + "]";
            PlayerCombatData.Id(_id, p + ".Id");
            PlayerCombatData.Range(_start, 0, duration, p + ".Start"); PlayerCombatData.Range(_end, _start, duration, p + ".End");
            if (!_singleSample && _start == _end) throw new InvalidOperationException(p + "：持续窗口必须有非零长度。");
            PlayerCombatData.Enum(_retry, p + ".Retry");
            if (_payload == null || _keyframes == null || _keyframes.Length == 0) throw new InvalidOperationException(p + "：缺少 Payload 或形状关键帧。");
            try { _payload.Validate(); } catch (Exception e) { throw new InvalidOperationException(p + ".Payload：" + e.Message, e); }
            float previous = -1;
            for (int i = 0; i < _keyframes.Length; i++)
            {
                var frame = _keyframes[i];
                string f = p + ".Keyframes[" + i + "]";
                if (frame == null) throw new InvalidOperationException(f + "：空项。");
                PlayerCombatData.Range(frame.Time, _start, _singleSample ? _start : _end, f + ".Time");
                if (i == 0 && frame.Time != _start || frame.Time <= previous || !_singleSample && frame.Time >= _end)
                    throw new InvalidOperationException(f + ".Time：首帧须等于窗口起点，后续严格递增且处于左闭右开区间。");
                frame.Validate(f); previous = frame.Time;
            }
        }
        #endregion
    }

    [Serializable]
    public sealed class PlayerHitKeyframe
    {
        [SerializeField] private float _time;
        [SerializeField] private PlayerHitShape[] _shapes = Array.Empty<PlayerHitShape>();
        public float Time => _time;
        public IReadOnlyList<PlayerHitShape> Shapes => Array.AsReadOnly(_shapes);
        internal void Validate(string path)
        {
            if (_shapes == null || _shapes.Length == 0) throw new InvalidOperationException(path + ".Shapes：至少需要一个形状。");
            for (int i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i] == null) throw new InvalidOperationException(path + ".Shapes[" + i + "]：空项。");
                _shapes[i].Validate(path + ".Shapes[" + i + "]");
            }
        }
    }

    [Serializable]
    public sealed class PlayerHitShape
    {
        [SerializeField] private PlayerHitShapeKind _kind;
        [SerializeField] private Vector2 _offset, _size = Vector2.one;
        [SerializeField] private float _radius = .5f, _angle;
        [SerializeField] private Vector2[] _points = Array.Empty<Vector2>();
        public PlayerHitShapeKind Kind => _kind;
        public Vector2 Offset => _offset;
        public Vector2 Size => _size;
        public float Radius => _radius;
        public float Angle => _angle;
        public IReadOnlyList<Vector2> Points => Array.AsReadOnly(_points);
        internal void Validate(string path)
        {
            PlayerCombatData.Enum(_kind, path + ".Kind"); PlayerCombatData.Vector(_offset, path + ".Offset");
            if (!float.IsFinite(_angle)) throw new InvalidOperationException(path + ".Angle：非有限值。");
            if (_kind == PlayerHitShapeKind.Box)
            { PlayerCombatData.Positive(_size.x, path + ".Size.x"); PlayerCombatData.Positive(_size.y, path + ".Size.y"); }
            if (_kind == PlayerHitShapeKind.Circle) PlayerCombatData.Positive(_radius, path + ".Radius");
            if (_kind != PlayerHitShapeKind.Polygon) return;
            if (_points == null || _points.Length < 3) throw new InvalidOperationException(path + ".Points：Polygon 至少需要三个点。");
            double twiceArea = 0;
            for (int i = 0; i < _points.Length; i++)
            {
                PlayerCombatData.Vector(_points[i], path + ".Points[" + i + "]");
                var a = _points[i]; var b = _points[(i + 1) % _points.Length];
                if ((a - b).sqrMagnitude < .00000001f) throw new InvalidOperationException(path + ".Points：相邻点重合。");
                twiceArea += (double)a.x * b.y - (double)b.x * a.y;
                for (int j = i + 1; j < _points.Length; j++)
                {
                    if (j == i + 1 || i == 0 && j == _points.Length - 1) continue;
                    if (Intersects(a, b, _points[j], _points[(j + 1) % _points.Length]))
                        throw new InvalidOperationException(path + ".Points：存在自交或非相邻边接触。");
                }
            }
            if (Math.Abs(twiceArea) < .000001) throw new InvalidOperationException(path + ".Points：Polygon 面积为零。");
        }
        private double Cross(Vector2 a, Vector2 b, Vector2 c) => ((double)b.x - a.x) * (c.y - a.y) - ((double)b.y - a.y) * (c.x - a.x);
        private bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (Math.Max(a.x, b.x) < Math.Min(c.x, d.x) || Math.Max(c.x, d.x) < Math.Min(a.x, b.x) ||
                Math.Max(a.y, b.y) < Math.Min(c.y, d.y) || Math.Max(c.y, d.y) < Math.Min(a.y, b.y)) return false;
            return Cross(a, b, c) * Cross(a, b, d) <= 0 && Cross(c, d, a) * Cross(c, d, b) <= 0;
        }
    }
}
