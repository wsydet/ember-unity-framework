// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 拖拽组件 —— 挂到任意 UI 节点（Button 等 Selectable）上，使其可被拖拽移动。
    ///
    /// <para>拖拽期间会临时禁用 Selectable，避免松手时误触发 onClick；
    /// 纯点击（未超过拖拽阈值）不会进入 OnBeginDrag，按钮点击行为不受影响。</para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 内部参数

        private RectTransform _rect;
        private Selectable _control;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            _rect = transform as RectTransform;
            _control = GetComponent<Selectable>();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 拖拽期间禁用按钮，防止松手时触发 onClick
            if (_control) _control.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_rect) return;

            // ScreenSpaceCamera / Overlay 通用：把屏幕坐标转成世界坐标再移动
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out var world))
            {
                _rect.position = world;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_control) _control.enabled = true;
        }

        #endregion
    }
}
