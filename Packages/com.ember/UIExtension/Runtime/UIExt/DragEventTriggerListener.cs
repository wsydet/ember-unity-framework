// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using Ember.Basic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 拖拽事件监听器。
    /// 提供三种拖拽模式：
    /// <list type="bullet">
    ///   <item><b>普通拖拽</b>：转发 onDrag / onDragStart / onDragEnd 事件</item>
    ///   <item><b>拖拽到 Drop</b>（DragToDrop）：按住拖拽到目标位置松开，触发 Drop 回调</item>
    ///   <item><b>代理拖拽</b>：覆盖父级 ScrollRect 或父级 DragEventListener 的拖拽行为</item>
    /// </list>
    /// 与 <see cref="EUIEventTriggerListener"/> 配合使用。
    /// </summary>
    [AddComponentMenu("UI/EUI/Drag Event Trigger Listener")]
    public class DragEventTriggerListener : MonoBehaviour,
        IDragHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("是否代理父级 ScrollRect 的拖拽")]
        private bool _coverParentScrollRect;

        [SerializeField]
        [Tooltip("是否代理父级 DragEventListener 的拖拽事件")]
        private bool _coverDragEventListener;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private GameObject _go;
        private GameObject _targetMoveObj;
        private RectTransform _targetWidget;
        private RectTransform _targetParentWidget;
        private Vector2 _currentPos;
        private ScrollRect _parentScrollRect;
        private DragEventTriggerListener _parentDragEventListener;
        private EUIEventTriggerListener _eventTriggerListener;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnDisable()
        {
            if (_isDragToDrop)
            {
                PointDragObject = null;

                if (_isDraggingToDrop)
                    _isDraggingToDrop = false;

                if (_eventTriggerListener != null && _eventTriggerListener.IsClicking)
                    _eventTriggerListener.HandleClickingOut();
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void SetNowPos(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _targetParentWidget, eventData.position, eventData.pressEventCamera, out _currentPos))
            {
                _targetWidget.anchoredPosition = _currentPos;
            }
        }

        /// <summary>保证当前拖拽对象在最上层渲染，不被遮挡</summary>
        private void SetAsLastSibling()
        {
            _targetWidget.SetAsLastSibling();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        // ── 公开委托 ──

        /// <summary>拖拽中回调</summary>
        public EUIEventTriggerListener.PointerEventDelegate OnDragCallback;
        /// <summary>拖拽开始回调</summary>
        public EUIEventTriggerListener.PointerEventDelegate OnDragStartCallback;
        /// <summary>拖拽结束回调</summary>
        public EUIEventTriggerListener.PointerEventDelegate OnDragEndCallback;
        /// <summary>拖拽到 Drop 开始回调</summary>
        public EUIEventTriggerListener.ObjectGameObjectDelegate OnDragToDropStart;
        /// <summary>拖拽到 Drop 结束回调（第三个参数为是否成功）</summary>
        public EUIEventTriggerListener.ObjectGameObjectBoolDelegate OnDragToDropEnd;
        /// <summary>拖拽长按回调</summary>
        public EUIEventTriggerListener.ObjectVoidDelegate OnDragToDropLongPress;

        /// <summary>自定义参数，由调用方自由赋值</summary>
        public object Parameter;

        /// <summary>全局拖拽对象引用（静态，同一时间只有一个 DragToDrop 激活）</summary>
        public static object PointDragObject { get; set; }

        // ── 状态属性 ──

        /// <summary>是否正在拖拽到 Drop</summary>
        public bool IsDraggingToDrop
        {
            get => _isDraggingToDrop;
        }

        /// <summary>拖拽过程中是否发生了移动</summary>
        public bool IsDraggingMove
        {
            get => _isDraggingMove;
        }

        /// <summary>是否启用了拖拽到 Drop 模式</summary>
        public bool IsDragToDrop
        {
            get => _isDragToDrop;
        }

        // ── 关联组件 ──

        /// <summary>父级 ScrollRect（缓存）</summary>
        public ScrollRect ParentScrollRect
        {
            get
            {
                if (!_go)
                    _go = gameObject;
                if (_parentScrollRect == null)
                    _parentScrollRect = _go.GetComponentInParent<ScrollRect>();
                return _parentScrollRect;
            }
        }

        /// <summary>父级 DragEventListener（缓存）</summary>
        public DragEventTriggerListener ParentDragEventListener
        {
            get
            {
                if (_parentDragEventListener == null && _go)
                    _parentDragEventListener = _go.transform.parent.GetComponentInParent<DragEventTriggerListener>();
                return _parentDragEventListener;
            }
        }

        /// <summary>关联的 EventTriggerListener</summary>
        public EUIEventTriggerListener EventTriggerListener
        {
            get => _eventTriggerListener;
            set => _eventTriggerListener = value;
        }

        // ── 非序列化状态字段（运行时） ──

        [System.NonSerialized]
        private bool _isDraggingToDrop;

        [System.NonSerialized]
        private bool _isDraggingMove;

        [System.NonSerialized]
        private bool _isDragToDrop;

        // ── 静态工厂 ──

        /// <summary>获取或自动添加 DragEventListener 到指定 GameObject。</summary>
        [NoGC]
        public static DragEventTriggerListener Get(GameObject go)
        {
            var listener = go.GetComponent<DragEventTriggerListener>();
            if (listener == null)
                listener = go.AddComponent<DragEventTriggerListener>();
            listener._go = go;
            return listener;
        }

        /// <summary>获取或自动添加 DragEventListener 到指定 Transform 的 GameObject。</summary>
        [NoGC]
        public static DragEventTriggerListener Get(Transform transform)
        {
            var listener = transform.GetComponent<DragEventTriggerListener>();
            if (listener == null)
                listener = transform.gameObject.AddComponent<DragEventTriggerListener>();
            return listener;
        }

        /// <summary>
        /// 创建 DragToDrop 模式的监听器 —— 按住后拖拽目标对象，松手时检测 Drop 目标。
        /// </summary>
        /// <param name="go">挂载监听器的 GameObject</param>
        /// <param name="targetMoveObj">要跟随拖拽移动的 GameObject</param>
        /// <param name="coverParentScrollRect">是否代理父级 ScrollRect</param>
        /// <param name="coverDragEventListener">是否代理父级 DragEventListener</param>
        [HasGC]
        public static DragEventTriggerListener GetDragToDrop(
            GameObject go, GameObject targetMoveObj,
            bool coverParentScrollRect, bool coverDragEventListener)
        {
            var listener = Get(go);
            listener._targetMoveObj = targetMoveObj;
            listener._targetWidget = targetMoveObj.GetComponent<RectTransform>();
            listener._targetParentWidget = listener._targetWidget.parent.GetComponent<RectTransform>();
            listener._isDragToDrop = true;
            listener._isDraggingToDrop = false;
            listener._coverParentScrollRect = coverParentScrollRect;
            listener._coverDragEventListener = coverDragEventListener;

            var eventListener = EUIEventTriggerListener.Get(go);
            listener._eventTriggerListener = eventListener;
            eventListener.DragEventListener = listener;

            return listener;
        }

        // ── 操作方法 ──

        /// <summary>设置穿透模式：是否代理父级 ScrollRect 和 DragEventListener。</summary>
        public void SetPassThrough(bool val)
        {
            _coverDragEventListener = val;
            _coverParentScrollRect = val;
        }

        /// <summary>移除 DragToDrop 模式（无回调时自动清理）。</summary>
        public void HandleRemoveDragToDrop()
        {
            if (_isDragToDrop && OnDragToDropStart == null && OnDragToDropEnd == null)
            {
                _isDragToDrop = false;
                _isDraggingToDrop = false;
                _coverParentScrollRect = false;
                _coverDragEventListener = false;
                PointDragObject = null;

                if (_eventTriggerListener != null && _eventTriggerListener.IsClicking)
                    _eventTriggerListener.HandleClickingOut();

                if (_eventTriggerListener != null)
                    _eventTriggerListener.DragEventListener = null;
            }
        }

        /// <summary>开始拖拽到 Drop（由 EventTriggerListener 调用）。</summary>
        public void StartDrag(PointerEventData eventData)
        {
            _isDraggingToDrop = true;
            _isDraggingMove = false;
            SetAsLastSibling();
            OnDragToDropStart?.Invoke(Parameter, _targetMoveObj);
            SetNowPos(eventData);
            PointDragObject = Parameter;
        }

        /// <summary>开始拖拽长按（由 EventTriggerListener 调用）。</summary>
        public void StartDragLongPress(PointerEventData eventData)
        {
            if (!_isDraggingMove && OnDragToDropLongPress != null)
            {
                OnDragToDropLongPress(_targetMoveObj);
                EndDrop(eventData);
            }
        }

        /// <summary>结束拖拽到 Drop：检测 Drop 目标并触发回调。</summary>
        public void EndDrop(PointerEventData eventData)
        {
            var success = false;
            var hitObj = eventData.pointerCurrentRaycast.gameObject;

            if (hitObj != null && hitObj != _go)
            {
                EUIEventTriggerListener hitListener;
                var current = hitObj;
                do
                {
                    hitListener = current.GetComponent<EUIEventTriggerListener>();
                    if (!hitListener)
                    {
                        var parent = current.transform.parent;
                        if (parent)
                            current = parent.gameObject;
                        else
                            break;
                    }
                }
                while (current && !hitListener);

                if (hitListener != null && hitListener.onDrop != null)
                {
                    hitListener.onDrop(Parameter);
                    success = true;
                }
            }

            OnDragToDropEnd?.Invoke(Parameter, _targetMoveObj, success);
            PointDragObject = null;
            _isDraggingToDrop = false;
            _isDraggingMove = false;
        }

        // ── Unity EventSystem 接口实现 ──

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isDraggingToDrop)
            {
                SetNowPos(eventData);
            }
            else
            {
                if (_isDragToDrop)
                {
                    if (_eventTriggerListener != null && _eventTriggerListener.IsClicking
                        && (_coverParentScrollRect || _coverDragEventListener))
                    {
                        _eventTriggerListener.HandleClickingOut();
                    }
                }

                if (_coverParentScrollRect && ParentScrollRect)
                    ParentScrollRect.OnBeginDrag(eventData);

                if (_coverDragEventListener && ParentDragEventListener?.OnDragStartCallback != null)
                    _parentDragEventListener.OnDragStartCallback(_go, eventData);

                OnDragStartCallback?.Invoke(_go, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _isDraggingMove = true;

            if (_isDraggingToDrop)
            {
                SetNowPos(eventData);
            }
            else
            {
                if (_isDragToDrop)
                {
                    if (_coverParentScrollRect && ParentScrollRect)
                        ParentScrollRect.OnDrag(eventData);
                    else
                        _eventTriggerListener?.StartDragToDrop();
                }
                else if (_coverParentScrollRect && ParentScrollRect)
                {
                    ParentScrollRect.OnDrag(eventData);
                }

                if (_coverDragEventListener && ParentDragEventListener?.OnDragCallback != null)
                    _parentDragEventListener.OnDragCallback(_go, eventData);

                OnDragCallback?.Invoke(_go, eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDraggingMove = false;

            if (_isDraggingToDrop)
            {
                EndDrop(eventData);
            }
            else
            {
                if (_coverParentScrollRect && ParentScrollRect)
                    ParentScrollRect.OnEndDrag(eventData);

                if (_coverDragEventListener && ParentDragEventListener?.OnDragEndCallback != null)
                    _parentDragEventListener.OnDragEndCallback(_go, eventData);

                OnDragEndCallback?.Invoke(_go, eventData);
            }
        }

        #endregion
    }
}
