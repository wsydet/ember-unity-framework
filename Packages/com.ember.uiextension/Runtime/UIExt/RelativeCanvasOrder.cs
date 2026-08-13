// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// 相对 Canvas 排序控制器。
    /// 自动将自身 Canvas 的 sortingOrder 设置为父 Canvas sortingOrder + 偏移量，
    /// 确保多层嵌套 Canvas 的渲染顺序始终相对父级保持一致。
    /// </summary>
    [AddComponentMenu("UI/EUI/Relative Canvas Order")]
    [ExecuteAlways]
    public class RelativeCanvasOrder : MonoBehaviour, ICanvasSortingOrderHandler
    {
        #region 编辑器面板参数

        [SerializeField]
        [LabelText("排序偏移")]
        [Tooltip("相对于父 Canvas sortingOrder 的偏移量")]
        private int _orderOffset;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private Canvas _parentCanvas;
        private Canvas _cachedCanvas;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            _cachedCanvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            if (!_parentCanvas && transform.parent)
                _parentCanvas = transform.parent.GetComponentInParent<Canvas>();

            UpdateSortingOrder();
        }

        private void OnDisable()
        {
            _parentCanvas = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 相对于父 Canvas sortingOrder 的偏移量。
        /// 正值表示渲染在父 Canvas 上方，负值表示下方。
        /// </summary>
        public int OrderOffset
        {
            get => _orderOffset;
            set
            {
                if (_orderOffset != value)
                {
                    _orderOffset = value;
                    UpdateSortingOrder();
                }
            }
        }

        /// <summary>
        /// 同步 sortingOrder：设置为父 Canvas sortingOrder + 偏移量。
        /// </summary>
        public void UpdateSortingOrder()
        {
            if (_cachedCanvas && _parentCanvas)
            {
                _cachedCanvas.overrideSorting = true;
                _cachedCanvas.sortingOrder = _parentCanvas.sortingOrder + _orderOffset;
            }
        }

        #endregion
    }
}
