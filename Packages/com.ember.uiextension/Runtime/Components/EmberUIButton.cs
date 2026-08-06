// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Ember.Basic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// Button UI 控件封装。
    /// 包装 Unity Button，提供 Enable 状态控制和统一的点击事件管道。
    /// 兼容 <see cref="EmberButtonEx"/>（enhanced Button）。
    /// </summary>
    public class EmberUIButton : EmberUIComponent
    {
        #region 内部参数

        private Button _button;
        private EmberButtonEx _buttonEx;
        private bool _canClickWhenDisable;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _button = GetComponent<Button>();
            _buttonEx = _button as EmberButtonEx;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>是否在禁用状态下仍可触发点击</summary>
        public bool CanClickWhenDisable
        {
            get => _canClickWhenDisable;
            set
            {
                _canClickWhenDisable = value;
                if (value && !_buttonEx)
                    EmberDebug.LogWarning(LogTags.EmberUI, "只有 EmberButtonEx 支持 CanClickWhenDisable");
            }
        }

        /// <summary>按钮是否可交互</summary>
        public override bool Enable
        {
            get => _buttonEx ? _buttonEx.EnableState : _button.interactable;
            set
            {
                if (_buttonEx)
                {
                    _buttonEx.EnableState = value;
                    if (!_canClickWhenDisable && !value)
                        _buttonEx.interactable = false;
                    if (value)
                        _buttonEx.interactable = true;
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
    }
}
