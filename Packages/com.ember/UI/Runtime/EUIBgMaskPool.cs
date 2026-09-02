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
    public class EUIBgMaskPool
    {
        #region 内部参数

        private const string MaskName = "EmberBgMask";
        private const int PoolCapacity = 10;

        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private readonly Transform _parent;
        private readonly Camera _uiCamera;
        private readonly int _sortingOrderOffset;
        private readonly Color _maskColor = new Color(0f, 0f, 0f, 0.5f);

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        /// <summary>
        /// 创建 BG Mask 对象池。
        /// </summary>
        /// <param name="parent">Mask 的父 Transform</param>
        /// <param name="uiCamera">UI 相机（与页面 Canvas 一致，保证遮罩与页面同空间排序）</param>
        /// <param name="sortingOrderOffset">相对于 Canvas sortingOrder 的偏移</param>
        public EUIBgMaskPool(Transform parent, Camera uiCamera, int sortingOrderOffset = -1)
        {
            _parent = parent;
            _uiCamera = uiCamera;
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
        /// <param name="maskColor">遮罩颜色（可配置，如 EUIManager.PopupMaskColor）；null 则保持默认/池内原色</param>
        /// <param name="layer">所属弹窗的 Layer；传入有效值时同步到遮罩，保证 UI Camera 能渲染</param>
        public GameObject Get(int sortingOrder, System.Action onClick = null, Color? maskColor = null,
            int layer = -1)
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

            if (layer >= 0)
                mask.layer = layer;

            mask.transform.SetAsLastSibling();

            // 设置遮罩颜色（创建与复用都刷新，支持运行时改配置）
            if (maskColor.HasValue)
            {
                var img = mask.GetComponent<Image>();
                if (img) img.color = maskColor.Value;
            }

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
            var go = new GameObject(MaskName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GraphicRaycaster));
            go.transform.SetParent(_parent, false);

            // RectTransform 填满父级
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Image
            var img = go.GetComponent<Image>();
            img.color = _maskColor;
            img.raycastTarget = true;

            // Canvas：与页面一致走 ScreenSpaceCamera + UI 相机。
            // 若保持默认 ScreenSpaceOverlay，遮罩会永远渲染在所有 Camera 模式 Canvas（页面）之上，盖住弹窗。
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.overrideSorting = true;

            return go;
        }

        #endregion
    }
}
