using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    /// <summary>独立刀光纯表现配置；以攻击逻辑时间采样，不创建Collider或派发命中。</summary>
    [Serializable]
    public sealed class PlayerAttackVisual
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField] private Sprite[] _frames = Array.Empty<Sprite>();
        [SerializeField] private float _opensAt = .1f, _fps = 24, _scale = 1, _angle;
        [SerializeField] private Vector2 _offset = new(.6f, 0);
        #endregion
        #region 外部方法
        public bool Enabled => _enabled;
        public IReadOnlyList<Sprite> Frames => Array.AsReadOnly(_frames);
        public float OpensAt => _opensAt;
        public float Fps => _fps;
        public float Scale => _scale;
        public float Angle => _angle;
        public Vector2 Offset => _offset;
        internal void Validate(float duration, string path)
        {
            PlayerCombatData.NonNegative(_opensAt, path + ".OpensAt");
            if (_enabled) PlayerCombatData.Range(_opensAt, 0, duration, path + ".OpensAt");
            PlayerCombatData.Positive(_fps, path + ".Fps"); PlayerCombatData.Positive(_scale, path + ".Scale");
            PlayerCombatData.Vector(_offset, path + ".Offset");
            if (!float.IsFinite(_angle) || _frames == null || _enabled && _frames.Length == 0)
                throw new InvalidOperationException(path + "：角度或序列帧无效。");
            foreach (var frame in _frames) if (frame == null) throw new InvalidOperationException(path + "：序列帧引用缺失。");
        }
        #endregion
    }

    internal sealed class PlayerAttackVisualSession : IDisposable
    {
        #region 内部参数
        private readonly PlayerController _owner;
        private readonly SpriteRenderer _body;
        private SpriteRenderer _renderer;
        #endregion
        #region 外部方法
        internal PlayerAttackVisualSession(PlayerController owner, SpriteRenderer body) { _owner = owner; _body = body; }
        internal void Sync()
        {
            var combat = _owner.Context?.Combat;
            var visual = combat?.Segment?.Visual;
            double elapsed = (combat?.Progress ?? 0) - (visual?.OpensAt ?? 0);
            int frame = visual == null ? -1 : (int)Math.Floor(elapsed * visual.Fps);
            bool visible = combat is { IsActive: true, IsRecovery: false } && visual is { Enabled: true } && frame >= 0 && frame < visual.Frames.Count;
            if (!visible) { if (_renderer != null) _renderer.enabled = false; return; }
            if (_renderer == null)
            {
                var go = new GameObject("Attack visual (session)") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(_owner.transform, false); _renderer = go.AddComponent<SpriteRenderer>();
            }
            _renderer.enabled = true; _renderer.sprite = visual.Frames[frame];
            _renderer.sortingLayerID = _body.sortingLayerID; _renderer.sortingOrder = _body.sortingOrder + 1;
            var transform = _renderer.transform;
            transform.localPosition = new Vector3(visual.Offset.x * combat.Direction, visual.Offset.y, 0);
            transform.localRotation = Quaternion.Euler(0, 0, visual.Angle * combat.Direction);
            transform.localScale = new Vector3(visual.Scale * combat.Direction, visual.Scale, 1);
        }
        public void Dispose()
        {
            if (_renderer != null) UnityEngine.Object.DestroyImmediate(_renderer.gameObject);
            _renderer = null;
        }
        #endregion
    }
}
