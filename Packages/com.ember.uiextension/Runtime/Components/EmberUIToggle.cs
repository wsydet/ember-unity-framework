// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// Toggle UI 控件封装。
    /// 包装 Unity Toggle，提供状态查询和值变化事件。
    /// </summary>
    public class EmberUIToggle : EmberUIComponent
    {
        #region 内部参数

        private Toggle _toggle;
        private EmberToggleEx _toggleEx;
        private Action<EmberUIToggle, bool> _onValueChanged;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _toggle = GetComponent<Toggle>();
            _toggleEx = _toggle as EmberToggleEx;

            if (_toggle)
                _toggle.onValueChanged.AddListener(HandleValueChanged);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>是否选中</summary>
        public bool IsOn
        {
            get => _toggle && _toggle.isOn;
            set { if (_toggle) _toggle.isOn = value; }
        }

        /// <summary>是否可交互</summary>
        public override bool Enable
        {
            get => _toggle && _toggle.interactable;
            set { if (_toggle) _toggle.interactable = value; }
        }

        /// <summary>值变化回调</summary>
        public event Action<EmberUIToggle, bool> OnValueChanged
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }

        /// <summary>Unity Toggle 引用</summary>
        public Toggle UnityToggle => _toggle;

        /// <summary>手动刷新状态节点（ToggleEx）</summary>
        public void Refresh()
        {
            _toggleEx?.Refresh();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void HandleValueChanged(bool isOn)
        {
            _onValueChanged?.Invoke(this, isOn);
        }

        #endregion
    }
}
