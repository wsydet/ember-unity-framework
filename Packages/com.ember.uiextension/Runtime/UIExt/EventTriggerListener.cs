// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Ember.Basic;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 增强版 EventTrigger 监听器。
    /// 单个组件即可处理所有 UI 事件类型（click / down / up / enter / exit / longPress / drag），
    /// 比 Unity 原生 <see cref="EventTrigger"/> 更高效——不需要为每种事件类型添加一个 Trigger 组件。
    ///
    /// <para>使用方式：</para>
    /// <code>
    /// var listener = EmberEventTriggerListener.Get(gameObject);
    /// listener.onClick += (go) => Debug.Log("点击了");
    /// listener.onLongPressTime += (go, active) => { if (active) StartCharge(); else CancelCharge(); };
    /// </code>
    /// </summary>
    [AddComponentMenu("UI/Ember/Event Trigger Listener")]
    public class EmberEventTriggerListener : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerUpHandler
    {
        #region 编辑器面板参数

        [FoldoutGroup("长按设置")]
        [SerializeField]
        [LabelText("首次延迟 (秒)")]
        [Tooltip("按下后多久开始触发长按")]
        private float _longPressDelayTime = 1f;

        [FoldoutGroup("长按设置")]
        [SerializeField]
        [LabelText("重复间隔 (秒)")]
        [Tooltip("长按开始后每隔多久重复触发一次，0 表示只触发一次")]
        private float _longPressRepeatTime = 0.3f;

        [FoldoutGroup("长按设置")]
        [SerializeField]
        [LabelText("加速模式")]
        [Tooltip("长按触发后间隔是否逐渐缩短（加速效果）")]
        private bool _longPressSpeedUp = true;

        [FoldoutGroup("拖拽设置")]
        [SerializeField]
        [HideInInspector]
        private float _longTimeToDrag = 1.5f;

        [FoldoutGroup("拖拽设置")]
        [SerializeField]
        [HideInInspector]
        private float _longTimeDragWithPress = 0f;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        // 委托类型定义 —— 兼容 burner 的 DragEventTriggerListener 等扩展
        public delegate void VoidDelegate(GameObject go);
        public delegate void BoolDelegate(GameObject go, bool state);
        public delegate void FloatDelegate(GameObject go, float delta);
        public delegate void PointerEventDelegate(GameObject go, PointerEventData data);
        public delegate void ObjectDelegate(GameObject go, GameObject obj);
        public delegate void KeyCodeDelegate(GameObject go, KeyCode key);
        public delegate void ObjectGameObjectDelegate(object obj, GameObject targetObject);
        public delegate void ObjectGameObjectBoolDelegate(object obj, GameObject targetObject, bool success);
        public delegate void ObjectVoidDelegate(object obj);

        private GameObject _go;
        private bool _isClicking;
        private bool _longPressEnable;
        private float _longPressStartTime;
        private float _lastLongPressTriggerTime;
        private float _startDragTime;
        private bool _dragDropStarted;
        private bool _dragLongPressStart;
        private DragEventTriggerListener _dragEventListener;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Update()
        {
            // 拖拽转 Drop 判定
            if (_dragDropStarted)
            {
                var now = Time.realtimeSinceStartup;
                var diffTime = now - _startDragTime;
                if (diffTime >= _longTimeToDrag)
                    StartDragToDrop();
            }

            // 拖拽长按判定
            if (_dragLongPressStart)
            {
                var now = Time.realtimeSinceStartup;
                var diffTime = now - _startDragTime;
                if (diffTime >= _longTimeDragWithPress)
                    StartDragPress();
            }

            // 常规长按触发
            if (_longPressEnable)
            {
                var now = Time.realtimeSinceStartup;
                if (now - _longPressStartTime > _longPressDelayTime)
                {
                    if (_longPressRepeatTime > 0.00001f)
                    {
                        if (now - _lastLongPressTriggerTime > _longPressRepeatTime)
                        {
                            onLongPressTime?.Invoke(_go, true);
                            _lastLongPressTriggerTime = now;
                        }
                    }
                    else
                    {
                        if (_lastLongPressTriggerTime < _longPressStartTime)
                        {
                            onLongPressTime?.Invoke(_go, true);
                            _lastLongPressTriggerTime = now;
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            _longPressEnable = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>检查关联的 Button 是否可交互，不可交互时长按不生效。</summary>
        private bool CheckLongPressButtonEnable()
        {
            var button = _go.GetComponent<Button>();
            return !(button && !button.interactable);
        }

        internal void StartDragToDrop()
        {
            if (_isClicking)
            {
                _dragEventListener.StartDrag(PointerEventData);
                _isClicking = false;
            }
            _dragDropStarted = false;
        }

        private void StartDragPress()
        {
            _dragEventListener?.StartDragLongPress(PointerEventData);
            _dragLongPressStart = false;
        }

        internal void HandleClickingOut()
        {
            _isClicking = false;
            _dragDropStarted = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        // ── 公开委托（运行时订阅） ──

        /// <summary>单击回调</summary>
        public VoidDelegate onClick;
        /// <summary>按下回调</summary>
        public VoidDelegate onDown;
        /// <summary>指针进入回调</summary>
        public VoidDelegate onEnter;
        /// <summary>指针离开回调</summary>
        public VoidDelegate onExit;
        /// <summary>抬起回调</summary>
        public VoidDelegate onUp;
        /// <summary>选中回调</summary>
        public VoidDelegate onSelect;
        /// <summary>持续选中回调</summary>
        public VoidDelegate onUpdateSelect;
        /// <summary>长按状态变化回调（active=true 触发/active=false 取消）</summary>
        public BoolDelegate onLongPressTime;
        /// <summary>Drop 进入回调（配合 DragEventTriggerListener）</summary>
        public ObjectVoidDelegate onDropEnter;
        /// <summary>Drop 离开回调（配合 DragEventTriggerListener）</summary>
        public ObjectVoidDelegate onDropExit;
        /// <summary>Drop 放下回调（配合 DragEventTriggerListener）</summary>
        public ObjectVoidDelegate onDrop;

        /// <summary>自定义参数，由调用方自由赋值</summary>
        public object Parameter;

        /// <summary>最近一次事件携带的 PointerEventData</summary>
        public PointerEventData PointerEventData { get; private set; }

        /// <summary>全局点击回调，所有 EmberEventTriggerListener 共享</summary>
        public static VoidDelegate GlobalClickCallback { get; set; }

        /// <summary>关联的拖拽事件监听器</summary>
        public DragEventTriggerListener DragEventListener
        {
            get => _dragEventListener;
            set => _dragEventListener = value;
        }

        /// <summary>当前是否处于点按拖拽状态</summary>
        public bool IsClicking => _isClicking;

        // ── 静态工厂 ──

        /// <summary>
        /// 获取或自动添加 EmberEventTriggerListener 到指定 GameObject。
        /// 推荐使用此方法而非直接 AddComponent，避免重复添加。
        /// </summary>
        [NoGC]
        public static EmberEventTriggerListener Get(GameObject go)
        {
            var listener = go.GetComponent<EmberEventTriggerListener>();
            if (listener == null)
                listener = go.AddComponent<EmberEventTriggerListener>();
            listener._go = go;
            return listener;
        }

        /// <summary>
        /// 获取或自动添加 EmberEventTriggerListener 到指定 Transform 的 GameObject。
        /// </summary>
        [NoGC]
        public static EmberEventTriggerListener Get(Transform transform)
        {
            var listener = transform.GetComponent<EmberEventTriggerListener>();
            if (listener == null)
                listener = transform.gameObject.AddComponent<EmberEventTriggerListener>();
            return listener;
        }

        // ── 长按参数设置 ──

        /// <summary>
        /// 设置长按的首次延迟和重复间隔。
        /// </summary>
        /// <param name="delayTime">按下后多久开始触发长按（秒）</param>
        /// <param name="repeatTime">触发间隔（秒），0 表示只触发一次</param>
        public void SetLongPressTime(float delayTime, float repeatTime)
        {
            _longPressDelayTime = delayTime;
            _longPressRepeatTime = repeatTime;
        }

        // ── Unity EventSystem 接口实现 ──

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            PointerEventData = eventData;
            GlobalClickCallback?.Invoke(_go);

            onClick?.Invoke(_go);
            _longPressEnable = false;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            PointerEventData = eventData;

            if (_dragEventListener != null && !_isClicking)
            {
                _isClicking = true;
                _dragDropStarted = true;
                _startDragTime = Time.realtimeSinceStartup;
                if (_longTimeDragWithPress > 0 && !_dragLongPressStart)
                    _dragLongPressStart = true;
            }
            else
            {
                onDown?.Invoke(_go);
                _longPressEnable = true;
                OnLongPressTimeFuc(true);
            }
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            PointerEventData = eventData;

            if (onDropEnter != null && DragEventTriggerListener.PointDragObject != null)
            {
                onDropEnter(DragEventTriggerListener.PointDragObject);
            }
            else
            {
                onEnter?.Invoke(_go);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            PointerEventData = eventData;

            if (onDropExit != null && DragEventTriggerListener.PointDragObject != null)
            {
                onDropExit(DragEventTriggerListener.PointDragObject);
            }
            else
            {
                onExit?.Invoke(_go);
                OnLongPressTimeFuc(false, false);
            }

            _dragLongPressStart = false;
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            PointerEventData = eventData;

            if (_dragEventListener != null)
            {
                if (_isClicking)
                    HandleClickingOut();

                if (_dragEventListener.IsDraggingToDrop)
                    _dragEventListener.EndDrop(eventData);
            }
            else
            {
                onUp?.Invoke(_go);
                _longPressEnable = false;
                OnLongPressTimeFuc(false);
            }

            _dragLongPressStart = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            PointerEventData = null;
            onSelect?.Invoke(_go);
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            PointerEventData = null;
            onUpdateSelect?.Invoke(_go);
        }

        /// <summary>
        /// 手动触发或取消长按计时。
        /// </summary>
        /// <param name="start">true 开始计时，false 停止</param>
        /// <param name="cancel">false 时即使停止也不触发 onLongPressTime(false)</param>
        public void OnLongPressTimeFuc(bool start, bool cancel = true)
        {
            if (start && CheckLongPressButtonEnable())
            {
                _longPressStartTime = Time.realtimeSinceStartup;
            }
            else if (cancel)
            {
                onLongPressTime?.Invoke(_go, false);
            }
        }

        #endregion
    }
}
