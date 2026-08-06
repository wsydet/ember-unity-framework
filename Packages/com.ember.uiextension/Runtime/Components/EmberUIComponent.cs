// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Ember.Core;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 控件基类。
    /// 包装一个 GameObject，提供统一的位置/尺寸、可见性、事件注册。
    /// 所有 UI 控件（Button、Text、Image 等）继承此类。
    ///
    /// <para>与 burner 的区别：</para>
    /// <list type="bullet">
    ///   <item>不包含 Tweener 动画方法（改用 IUITransitionHandler）</item>
    ///   <item>不包含 Attachment 动态挂载</item>
    ///   <item>不包含异步资源加载</item>
    ///   <item>精简到核心：事件管道 + Transform 便捷属性 + 显隐控制</item>
    /// </list>
    /// </summary>
    public class EmberUIComponent : IEmberUIComponent
    {
        #region 内部参数

        private GameObject _gameObject;
        private RectTransform _rectTransform;
        private Transform _transform;
        private bool _visible;
        private bool _disposed;

        private EmberEventTriggerListener _eventListener;
        private DragEventTriggerListener _dragListener;

        // 事件
        private Action<EmberUIComponent> _onClick;
        private Action<EmberUIComponent, bool> _onLongPress;

        private bool _clickAdded;
        private bool _longPressAdded;
        private int _longPressFrameCount;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        /// <summary>由框架调用，绑定到 GameObject。</summary>
        internal void Initialize(GameObject go)
        {
            _gameObject = go;
            _transform = go.transform;
            _rectTransform = _transform as RectTransform;
            _visible = go.activeSelf;
            OnInit();
        }

        public virtual void OnInit() { }
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void OnDispose() { }
        public virtual void OnUpdate() { }

        public void DoDispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearEventCallbacks();
            OnDispose();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法 —— 基础属性

        /// <summary>关联的 GameObject</summary>
        public GameObject GameObject => _gameObject;

        /// <summary>RectTransform（UI 控件时为非空）</summary>
        public RectTransform RectTransform => _rectTransform;

        /// <summary>Transform</summary>
        public Transform Transform => _transform;

        /// <summary>用户自定义数据</summary>
        public object UserState { get; set; }

        /// <summary>是否已销毁</summary>
        public bool IsDisposed => _disposed;

        /// <summary>是否可见</summary>
        public bool Visible
        {
            get
            {
                if (!_gameObject) return false;
                _visible = _gameObject.activeSelf;
                return _visible;
            }
            set
            {
                if (!_gameObject) return;
                _visible = value;
                if (value != _gameObject.activeSelf)
                    _gameObject.SetActive(value);
            }
        }

        /// <summary>在层级中是否可见</summary>
        public bool VisibleInHierarchy => _gameObject && _gameObject.activeInHierarchy;

        #endregion

        // --------------------------------------------------------

        #region 外部方法 —— 位置 & 尺寸

        /// <summary>anchoredPosition.x</summary>
        public float X
        {
            get => _rectTransform ? _rectTransform.anchoredPosition.x : _transform.localPosition.x;
            set
            {
                if (_rectTransform)
                {
                    var pos = _rectTransform.anchoredPosition;
                    pos.x = value;
                    _rectTransform.anchoredPosition = pos;
                }
                else
                {
                    var pos = _transform.localPosition;
                    pos.x = value;
                    _transform.localPosition = pos;
                }
            }
        }

        public float Y
        {
            get => _rectTransform ? _rectTransform.anchoredPosition.y : _transform.localPosition.y;
            set
            {
                if (_rectTransform)
                {
                    var pos = _rectTransform.anchoredPosition;
                    pos.y = value;
                    _rectTransform.anchoredPosition = pos;
                }
                else
                {
                    var pos = _transform.localPosition;
                    pos.y = value;
                    _transform.localPosition = pos;
                }
            }
        }

        public float Width
        {
            get => _rectTransform ? _rectTransform.sizeDelta.x : 0f;
            set
            {
                if (_rectTransform) { var s = _rectTransform.sizeDelta; s.x = value; _rectTransform.sizeDelta = s; }
            }
        }

        public float Height
        {
            get => _rectTransform ? _rectTransform.sizeDelta.y : 0f;
            set
            {
                if (_rectTransform) { var s = _rectTransform.sizeDelta; s.y = value; _rectTransform.sizeDelta = s; }
            }
        }

        public Vector2 AnchoredPosition
        {
            get => _rectTransform ? _rectTransform.anchoredPosition : Vector2.zero;
            set { if (_rectTransform) _rectTransform.anchoredPosition = value; }
        }

        public Vector3 WorldPosition
        {
            get => _transform.position;
            set => _transform.position = value;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法 —— 事件

        /// <summary>点击事件</summary>
        public Action<EmberUIComponent> OnClick
        {
            get => _onClick;
            set
            {
                if (!_clickAdded)
                {
                    _clickAdded = true;
                    var lis = EmberEventTriggerListener.Get(_gameObject);
                    lis.onClick += HandleClick;
                }
                _onClick = value;
            }
        }

        /// <summary>长按事件（active=true 触发，active=false 取消）</summary>
        public Action<EmberUIComponent, bool> OnLongPress
        {
            get => _onLongPress;
            set
            {
                if (!_longPressAdded)
                {
                    _longPressAdded = true;
                    var lis = EmberEventTriggerListener.Get(_gameObject);
                    lis.onLongPressTime += HandleLongPress;
                }
                _onLongPress = value;
            }
        }

        /// <summary>设置长按参数</summary>
        public void SetLongPressTime(float delayTime, float repeatTime)
        {
            EmberEventTriggerListener.Get(_gameObject).SetLongPressTime(delayTime, repeatTime);
        }

        /// <summary>取消长按</summary>
        public void CancelLongPress()
        {
            EmberEventTriggerListener.Get(_gameObject).OnLongPressTimeFuc(false, true);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法 —— 其他

        /// <summary>是否不可交互（子类 override）</summary>
        public virtual bool Enable
        {
            get => true;
            set { }
        }

        /// <summary>置灰（子类 override）</summary>
        public virtual void SetGray(bool gray) { }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void HandleClick(GameObject go)
        {
            if (!Enable) return;
            if (_longPressFrameCount == Time.frameCount) return;
            _onClick?.Invoke(this);
        }

        private void HandleLongPress(GameObject go, bool state)
        {
            if (!state && _longPressFrameCount != Time.frameCount)
                _longPressFrameCount = Time.frameCount;
            _onLongPress?.Invoke(this, state);
        }

        /// <summary>清理所有事件回调</summary>
        protected virtual void ClearEventCallbacks()
        {
            _onClick = null;
            _onLongPress = null;

            if (_gameObject)
            {
                if (_clickAdded)
                {
                    var lis = _gameObject.GetComponent<EmberEventTriggerListener>();
                    if (lis) lis.onClick = null;
                }
                if (_longPressAdded)
                {
                    var lis = _gameObject.GetComponent<EmberEventTriggerListener>();
                    if (lis) lis.onLongPressTime = null;
                }
            }
        }

        /// <summary>获取或添加组件</summary>
        protected T GetComponent<T>() where T : Component
        {
            var res = _gameObject.GetComponent<T>();
            if (res == null)
            {
                var arr = _gameObject.GetComponentsInChildren<T>(true);
                if (arr.Length > 0) res = arr[0];
            }
            return res;
        }

        #endregion
    }
}
