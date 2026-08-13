// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 多边形 Raycast 组件。
    /// 使用 <see cref="PolygonCollider2D"/> 定义精确的点击区域，
    /// 替代 Unity 默认的矩形 Raycast。适用于不规则形状按钮。
    ///
    /// <para>使用方式：在 Button GameObject 上添加此组件和 PolygonCollider2D，编辑碰撞体形状即可。</para>
    /// </summary>
    [AddComponentMenu("UI/EUI/Polygon Raycast")]
    [RequireComponent(typeof(PolygonCollider2D), typeof(CanvasRenderer))]
    public class EUIPolygonRaycast : Graphic, ICanvasRaycastFilter
    {
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("用于判定点击的 PolygonCollider2D")]
        private PolygonCollider2D _collider;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            if (_collider == null)
                _collider = GetComponent<PolygonCollider2D>();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            Awake();
            if (_collider == null)
                return;

            transform.localPosition = Vector3.zero;

            var w = rectTransform.sizeDelta.x * 0.5f + 0.1f;
            var h = rectTransform.sizeDelta.y * 0.5f + 0.1f;
            _collider.points = new Vector2[]
            {
                new Vector2(-w, -h),
                new Vector2(w, -h),
                new Vector2(w, h),
                new Vector2(-w, h)
            };
        }
#endif

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>Graphic 重建（空实现，不需要渲染）</summary>
        public override void Rebuild(CanvasUpdate update) { }

        /// <summary>使用 PolygonCollider2D 判定点是否在区域内</summary>
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_collider == null)
                return false;

            var worldPoint = eventCamera.ScreenToWorldPoint(screenPoint);
            return _collider.OverlapPoint(worldPoint);
        }

        /// <summary>碰撞体引用</summary>
        public PolygonCollider2D Collider
        {
            get => _collider;
            set => _collider = value;
        }

        #endregion
    }
}
