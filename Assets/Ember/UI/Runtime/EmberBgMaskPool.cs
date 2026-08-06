// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// 模态背景遮罩对象池。
    /// 当 Popup 打开时自动创建半透明背景遮罩（阻止点击穿透到下层页面），
    /// Popup 关闭时回池复用，避免频繁创建/销毁。
    /// </summary>
    public class EmberBgMaskPool
    {
        #region 内部参数

        private const string MaskName = "EmberBgMask";
        private const int PoolCapacity = 10;

        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private readonly Transform _parent;
        private readonly int _sortingOrderOffset;
        private readonly Color _maskColor = new Color(0f, 0f, 0f, 0.5f);

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        /// <summary>
        /// 创建 BG Mask 对象池。
        /// </summary>
        /// <param name="parent">Mask 的父 Transform</param>
        /// <param name="sortingOrderOffset">相对于 Canvas sortingOrder 的偏移</param>
        public EmberBgMaskPool(Transform parent, int sortingOrderOffset = -1)
        {
            _parent = parent;
            _sortingOrderOffset = sortingOrderOffset;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 获取一个 BG Mask（优先从池中取，池空则创建）。
        /// </summary>
        /// <param name="sortingOrder">遮罩的 sortingOrder</param>
        /// <param name="onClick">点击遮罩的回调（通常为关闭 Popup）</param>
        public GameObject Get(int sortingOrder, System.Action onClick = null)
        {
            GameObject mask;
            if (_pool.Count > 0)
            {
                mask = _pool.Pop();
                mask.SetActive(true);
            }
            else
            {
                mask = CreateMask();
            }

            mask.transform.SetAsLastSibling();

            // 设置 sortingOrder
            var canvas = mask.GetComponent<Canvas>();
            if (canvas) canvas.sortingOrder = sortingOrder + _sortingOrderOffset;

            // 绑定点击事件
            var button = mask.GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                    button.onClick.AddListener(() => onClick());
            }

            return mask;
        }

        /// <summary>
        /// 归还 BG Mask 到池中。
        /// </summary>
        public void Return(GameObject mask)
        {
            if (mask == null) return;

            mask.SetActive(false);

            var button = mask.GetComponent<Button>();
            if (button) button.onClick.RemoveAllListeners();

            if (_pool.Count < PoolCapacity)
                _pool.Push(mask);
            else
                Object.Destroy(mask);
        }

        /// <summary>
        /// 清理所有池中的 Mask。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var mask = _pool.Pop();
                if (mask != null) Object.Destroy(mask);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private GameObject CreateMask()
        {
            var go = new GameObject(MaskName, typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(_parent);

            // RectTransform 填满父级
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Image
            var img = go.GetComponent<Image>();
            img.color = _maskColor;
            img.raycastTarget = true;

            // Canvas
            var canvas = go.GetComponent<Canvas>();
            canvas.overrideSorting = true;

            return go;
        }

        #endregion
    }
}
