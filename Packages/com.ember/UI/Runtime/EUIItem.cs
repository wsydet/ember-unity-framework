// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// 由其他 UI 或业务模块创建并持有的轻量 EUI 单元。
    /// Item 不进入 EUIPage 栈，也不能通过 EUIManager 直接打开；它只复用 EUIBinding、
    /// EUILogic 与控件绑定生命周期。
    /// </summary>
    public sealed class EUIItem : IDisposable
    {
        private const string TAG = LogTags.UIManager;

        private readonly GameObject _gameObject;
        private readonly RectTransform _rectTransform;
        private EUILogic _logic;
        private EUIItemUpdateDriver _updateDriver;
        private bool _initialized;
        private bool _leaseOpened;
        private bool _visible;
        private bool _disposed;

        /// <summary>
        /// 构造一个已经实例化的 Item，并完成一次性的 Logic 创建与控件绑定阶段。
        /// 显示相关生命周期会延后到 <see cref="Show"/>。
        /// </summary>
        public EUIItem(
            GameObject gameObject,
            EUILogic logic,
            Action<Dictionary<string, Component>, EUILogic> populateControlMap = null,
            object customSettings = null)
        {
            if (!gameObject)
                throw new ArgumentNullException(nameof(gameObject));

            _rectTransform = gameObject.GetComponent<RectTransform>();
            if (!_rectTransform)
                throw new ArgumentException("EUIItem root requires a RectTransform.", nameof(gameObject));

            _gameObject = gameObject;
            _logic = logic;
            try
            {
                if (_logic != null)
                {
                    _logic.Page = null;
                    _logic.Item = this;
                    _logic.CustomSettings = customSettings;
                    _logic.ControlMap = new Dictionary<string, Component>();
                    populateControlMap?.Invoke(_logic.ControlMap, _logic);
                    _logic.OnBeginLoad();
                    _logic.OnBind();
                }

                _updateDriver = gameObject.GetComponent<EUIItemUpdateDriver>();
                if (!_updateDriver)
                    _updateDriver = gameObject.AddComponent<EUIItemUpdateDriver>();
                _updateDriver.hideFlags = HideFlags.HideInInspector;
                _updateDriver.Bind(this);
                RefreshUpdateState();
            }
            catch
            {
                if (_updateDriver)
                    _updateDriver.Bind(null);
                if (_logic != null)
                {
                    try
                    {
                        _logic.BroadcastDispose();
                    }
                    catch
                    {
                        // 保留最初的初始化异常。
                    }

                    _logic.Item = null;
                }

                throw;
            }
        }

        /// <summary>Item 根对象。</summary>
        public GameObject GameObject => _gameObject;

        /// <summary>Item 根矩形。</summary>
        public RectTransform RectTransform => _rectTransform;

        /// <summary>由 EUIBinding 创建的逻辑实例；无逻辑配置时为 null。</summary>
        public EUILogic Logic => _logic;

        /// <summary>Item 当前是否处于显示状态。</summary>
        public bool IsVisible => _visible;

        /// <summary>Item 是否已经释放。</summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// 开始一次 Item 借用。对象池应在业务写入内容之前调用本方法；
        /// OnInit 在实例生命周期中只执行一次，OnOpen 每次借用执行一次。
        /// </summary>
        public void BeginUse(object args = null)
        {
            if (_disposed || !_gameObject || _leaseOpened)
                return;

            if (_logic != null)
                InvokeLogic(_logic.BroadcastResetDefault, "OnResetDefault");
            if (!_initialized)
            {
                _initialized = true;
                if (_logic != null)
                    InvokeLogic(_logic.BroadcastInit, "OnInit");
            }

            InvokeLogic(() => _logic?.BroadcastOpen(args), "OnOpen");
            if (_logic != null)
                InvokeLogic(_logic.BroadcastReset, "OnReset");
            _leaseOpened = true;
        }

        /// <summary>
        /// 显示 Item。未显式调用 <see cref="BeginUse"/> 时会自动开始一次借用；
        /// 同一次借用期间由隐藏恢复显示时只重放 OnShow。
        /// </summary>
        public void Show(object args = null)
        {
            if (_disposed || !_gameObject || _visible)
                return;

            BeginUse(args);
            _gameObject.SetActive(true);
            if (_logic != null)
                InvokeLogic(_logic.BroadcastShow, "OnShow");
            _visible = true;
            RefreshUpdateState();
        }

        /// <summary>临时隐藏 Item，但保留本次借用状态。</summary>
        public void Hide()
        {
            if (_disposed || !_gameObject || !_visible)
                return;

            try
            {
                if (_logic != null)
                    InvokeLogic(_logic.BroadcastHide, "OnHide");
            }
            finally
            {
                _visible = false;
                RefreshUpdateState();
                _gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 结束本次借用并恢复默认状态，供对象池回收。不会销毁 Logic，下一次显示会重放 OnOpen。
        /// </summary>
        public void ResetForPool()
        {
            if (_disposed)
                return;

            Hide();
            if (!_leaseOpened)
                return;

            if (_logic != null)
            {
                InvokeLogic(_logic.BroadcastClose, "OnClose");
                InvokeLogic(_logic.BroadcastResetDefault, "OnResetDefault");
                InvokeLogic(_logic.BroadcastReset, "OnReset");
            }
            _leaseOpened = false;
        }

        /// <summary>结束生命周期并释放 Logic。GameObject 的销毁仍由持有者负责。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            ResetForPool();
            _disposed = true;
            if (_logic != null)
            {
                InvokeLogic(_logic.BroadcastDispose, "OnDispose");
                _logic.Item = null;
                _logic = null;
            }

            if (_updateDriver)
            {
                _updateDriver.enabled = false;
                _updateDriver.Bind(null);
            }
            _updateDriver = null;
        }

        /// <summary>
        /// 重新同步 NeedUpdate 驱动状态。使用自定义字段覆写 NeedUpdate 且运行时改变字段时调用。
        /// 基类 protected setter 会自动触发同步。
        /// </summary>
        public void RefreshUpdateState()
        {
            if (_updateDriver)
            {
                _updateDriver.enabled = !_disposed
                    && _visible
                    && _logic != null
                    && _logic.NeedUpdate;
            }
        }

        internal void Tick()
        {
            if (_disposed || !_visible || _logic == null)
                return;

            InvokeLogic(_logic.BroadcastUpdate, "OnUpdate");
        }

        private void InvokeLogic(Action action, string lifecycleName)
        {
            if (action == null)
                return;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                string itemName = _gameObject ? _gameObject.name : "(destroyed)";
                EmberDebug.LogError(TAG, $"EUIItem.{lifecycleName} '{itemName}' error: {ex}");
            }
        }
    }

}
