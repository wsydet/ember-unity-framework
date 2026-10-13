using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>一个角色的效果输出适配。持有期间独占所传碰撞体的 excludeLayers 和 Sprite 的 alpha 写入。</summary>
    public sealed class EntityEffectBinding : IDisposable
    {
        #region 内部参数
        private readonly EntityEffectContainer _effects;
        private readonly Collider2D[] _colliders;
        private readonly SpriteRenderer[] _sprites;
        private readonly int[] _baseLayers;
        private readonly float[] _baseAlpha;
        private bool _disposed;
        public float AlphaMultiplier { get; private set; } = 1;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Refresh()
        {
            if (_disposed) return;
            int priority = int.MinValue;
            bool hasAppearance = false;
            float alpha = 1;
            for (int i = 0; i < _effects.Count; i++)
            {
                var view = _effects.GetAt(i);
                var request = view.Appearance;
                if (!request.Enabled) continue;
                float candidate = request.Evaluate(view.ElapsedSeconds);
                if (!hasAppearance || request.Priority > priority)
                { hasAppearance = true; priority = request.Priority; alpha = candidate; }
                else if (request.Priority == priority) alpha = Mathf.Min(alpha, candidate);
            }
            Apply(_effects.ExcludedLayers, alpha);
        }
        private void Apply(int excludedLayers, float alpha)
        {
            AlphaMultiplier = alpha;
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].excludeLayers = _baseLayers[i] | excludedLayers;
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] == null) continue;
                var color = _sprites[i].color;
                color.a = _baseAlpha[i] * alpha;
                _sprites[i].color = color;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityEffectBinding(EntityEffectContainer effects, Collider2D[] colliders, SpriteRenderer[] sprites)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            if (effects.IsDisposed) throw new ObjectDisposedException(nameof(effects));
            _colliders = (Collider2D[])(colliders ?? throw new ArgumentNullException(nameof(colliders))).Clone();
            _sprites = (SpriteRenderer[])(sprites ?? throw new ArgumentNullException(nameof(sprites))).Clone();
            _baseLayers = new int[_colliders.Length]; _baseAlpha = new float[_sprites.Length];
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] == null) throw new ArgumentException("效果碰撞体缺失。");
                _baseLayers[i] = _colliders[i].excludeLayers.value;
            }
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] == null) throw new ArgumentException("效果 Sprite 缺失。");
                _baseAlpha[i] = _sprites[i].color.a;
            }
            _effects.Changed += Refresh;
            Refresh();
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _effects.Changed -= Refresh;
            Apply(0, 1);
        }
        #endregion
    }
}
