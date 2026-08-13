// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 ContentSizeFitter。
    /// 在 Unity 原生 <see cref="ContentSizeFitter"/> 基础上增加了 maxWidth/maxHeight 约束，
    /// 允许内容自适应尺寸不超过指定上限。
    /// </summary>
    [AddComponentMenu("UI/EUI/Content Size Fitter Ex")]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeFitterEx : UIBehaviour, ILayoutSelfController
    {
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("水平方向自适应模式")]
        private ContentSizeFitter.FitMode _horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        [SerializeField]
        [Tooltip("水平方向的最大宽度，小于等于 0 表示不限")]
        private float _maxWidth;

        [SerializeField]
        [Tooltip("垂直方向自适应模式")]
        private ContentSizeFitter.FitMode _verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        [SerializeField]
        [Tooltip("垂直方向的最大高度，小于等于 0 表示不限")]
        private float _maxHeight;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private RectTransform _rect;
        private DrivenRectTransformTracker _tracker;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(_rect);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>水平方向自适应模式</summary>
        public ContentSizeFitter.FitMode HorizontalFit
        {
            get => _horizontalFit;
            set
            {
                if (_horizontalFit != value)
                {
                    _horizontalFit = value;
                    SetDirty();
                }
            }
        }

        /// <summary>垂直方向自适应模式</summary>
        public ContentSizeFitter.FitMode VerticalFit
        {
            get => _verticalFit;
            set
            {
                if (_verticalFit != value)
                {
                    _verticalFit = value;
                    SetDirty();
                }
            }
        }

        /// <summary>水平方向最大宽度，小于等于 0 表示不限制</summary>
        public float MaxWidth
        {
            get => _maxWidth;
            set
            {
                if (!Mathf.Approximately(_maxWidth, value))
                {
                    _maxWidth = value;
                    SetDirty();
                }
            }
        }

        /// <summary>垂直方向最大高度，小于等于 0 表示不限制</summary>
        public float MaxHeight
        {
            get => _maxHeight;
            set
            {
                if (!Mathf.Approximately(_maxHeight, value))
                {
                    _maxHeight = value;
                    SetDirty();
                }
            }
        }

        /// <summary>RectTransform 引用（缓存）</summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rect == null)
                    _rect = GetComponent<RectTransform>();
                return _rect;
            }
        }

        public virtual void SetLayoutHorizontal()
        {
            _tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }

        public virtual void SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void HandleSelfFittingAlongAxis(int axis)
        {
            var fitting = axis == 0 ? _horizontalFit : _verticalFit;

            if (fitting == ContentSizeFitter.FitMode.Unconstrained)
            {
                _tracker.Add(this, RectTransform, DrivenTransformProperties.None);
                return;
            }

            _tracker.Add(this, RectTransform,
                axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);

            if (fitting == ContentSizeFitter.FitMode.MinSize)
                RectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetMinSize(_rect, axis));
            else
                RectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, GetClampedValue(LayoutUtility.GetPreferredSize(_rect, axis), axis));
        }

        private float GetClampedValue(float value, int axis)
        {
            var limit = axis == 0 ? _maxWidth : _maxHeight;
            if (limit > 0)
                return Mathf.Min(limit, value);
            return value;
        }

        private void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }

        #endregion
    }
}
