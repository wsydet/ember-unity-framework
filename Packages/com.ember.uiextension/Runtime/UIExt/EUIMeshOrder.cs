// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Sirenix.OdinInspector;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// Mesh 渲染排序控制器。
    /// 将子节点中所有 Renderer 的 sortingOrder 同步为父 Canvas sortingOrder + 偏移量，
    /// 确保 3D Mesh 在 UI 中的渲染顺序正确。
    /// </summary>
    [AddComponentMenu("UI/EUI/Mesh Order")]
    [ExecuteAlways]
    public class EUIMeshOrder : MonoBehaviour, ICanvasSortingOrderHandler
    {
        #region 编辑器面板参数

        [SerializeField]
        [LabelText("排序偏移")]
        [Tooltip("相对于父 Canvas sortingOrder 的偏移量")]
        private int _orderOffset;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private int _baseSortingOrder;
        private int _curSortingOrder;
        private Renderer[] _allRenderers;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnEnable()
        {
            UpdateSortingOrder();
        }

        private void Update()
        {
            if (_curSortingOrder != _orderOffset + _baseSortingOrder)
                UpdateSortingOrderImpl();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>排序偏移量</summary>
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

        /// <summary>同步所有子 Renderer 的 sortingOrder</summary>
        public void UpdateSortingOrder()
        {
            var parentCanvas = transform.parent.GetComponentInParent<Canvas>(true);
            if (parentCanvas == null)
                return;

            _baseSortingOrder = parentCanvas.sortingOrder;
            UpdateSortingOrderImpl();
        }

        /// <summary>刷新 Renderer 列表（当子节点变化时调用）</summary>
        public void UpdateRenderers()
        {
            _allRenderers = GetComponentsInChildren<Renderer>(true);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void UpdateSortingOrderImpl()
        {
            _curSortingOrder = _baseSortingOrder + _orderOffset;
            if (_allRenderers == null)
                UpdateRenderers();

            foreach (var renderer in _allRenderers)
            {
                renderer.sortingOrder = _curSortingOrder;
            }
        }

        #endregion
    }
}
