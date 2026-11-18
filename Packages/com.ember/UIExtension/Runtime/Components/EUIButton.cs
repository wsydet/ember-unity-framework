// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using Ember.Basic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// Button UI 控件封装。
    /// 包装 Unity Button，提供 Enable 状态控制和统一的点击事件管道。
    /// 兼容 <see cref="EUIButtonEx"/>（enhanced Button）。
    /// </summary>
    public class EUIButton : EUIComponent
    {
        #region 内部参数

        private const string TAG = LogTags.EmberUI;

        private Button _button;
        private EUIButtonEx _buttonEx;
        private bool _canClickWhenDisable;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _button = GetComponent<Button>();
            _buttonEx = _button as EUIButtonEx;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 是否在业务禁用状态下仍可触发 OnClick，供页面显示条件不足等提示。
        /// 仅支持 EUIButtonEx；不会绕过 Button 或父级 CanvasGroup 的输入屏蔽。
        /// </summary>
        public bool CanClickWhenDisable
        {
            get => _canClickWhenDisable;
            set
            {
                if (_canClickWhenDisable == value) return;
                _canClickWhenDisable = value;
                if (value && !_buttonEx)
                    EmberDebug.LogWarning(TAG, "只有 EUIButtonEx 支持 CanClickWhenDisable");
                if (_buttonEx)
                    _buttonEx.interactable = _buttonEx.EnableState || value;
            }
        }

        /// <summary>业务启用状态。灰态可点击时仍返回 false，供 OnClick 区分提示与正式操作。</summary>
        public override bool Enable
        {
            get => _buttonEx ? _buttonEx.EnableState : _button.interactable;
            set
            {
                if (_buttonEx)
                {
                    _buttonEx.SetEnabled(value, _canClickWhenDisable);
                }
                else
                {
                    if (_button.interactable != value)
                        _button.interactable = value;
                }
            }
        }

        /// <summary>Unity Button 引用</summary>
        public Button UnityButton => _button;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        protected override bool CanInvokeClick => _button && _button.IsActive()
            && _button.IsInteractable() && (Enable || (_buttonEx && _canClickWhenDisable));

        protected override void AddClickCallback()
        {
            // 保留指针监听器的全局点击通知；业务回调统一由 Button 分发，避免重复调用。
            EUIEventTriggerListener.Get(GameObject);
            _button.onClick.AddListener(HandleButtonClick);
        }

        protected override void RemoveClickCallback()
        {
            if (_button) _button.onClick.RemoveListener(HandleButtonClick);
        }

        private void HandleButtonClick()
        {
            HandleClick(GameObject);
        }

        #endregion
    }
}
