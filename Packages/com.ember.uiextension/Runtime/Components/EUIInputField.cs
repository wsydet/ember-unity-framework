// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Ember.UIExtension
{
    /// <summary>
    /// InputField UI 控件封装。
    /// 自动识别 InputField 或 TMP_InputField，统一 Text / 事件接口。
    /// </summary>
    public class EUIInputField : EUIComponent
    {
        #region 内部参数

        private InputField _input;
        private TMP_InputField _tmpInput;
        private bool _isLegacy;
        private Action<EUIInputField, string> _onValueChanged;
        private Action<EUIInputField, string> _onEndEdit;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public override void OnInit()
        {
            _input = GetComponent<InputField>();
            _tmpInput = GetComponent<TMP_InputField>();

            _isLegacy = _input != null;

            if (_input)
            {
                _input.onValueChanged.AddListener(v => _onValueChanged?.Invoke(this, v));
                _input.onEndEdit.AddListener(v => _onEndEdit?.Invoke(this, v));
            }
            else if (_tmpInput)
            {
                _tmpInput.onValueChanged.AddListener(v => _onValueChanged?.Invoke(this, v));
                _tmpInput.onEndEdit.AddListener(v => _onEndEdit?.Invoke(this, v));
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>文本内容</summary>
        public string Text
        {
            get => _isLegacy ? _input?.text : _tmpInput?.text;
            set
            {
                if (_isLegacy && _input) _input.text = value;
                else if (_tmpInput) _tmpInput.text = value;
            }
        }

        /// <summary>是否可交互</summary>
        public override bool Enable
        {
            get => _isLegacy ? (_input?.interactable ?? false) : (_tmpInput?.interactable ?? false);
            set
            {
                if (_isLegacy && _input) _input.interactable = value;
                else if (_tmpInput) _tmpInput.interactable = value;
            }
        }

        /// <summary>值变化回调</summary>
        public event Action<EUIInputField, string> OnValueChanged
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }

        /// <summary>编辑结束回调</summary>
        public event Action<EUIInputField, string> OnEndEdit
        {
            add => _onEndEdit += value;
            remove => _onEndEdit -= value;
        }

        /// <summary>是否为 Legacy InputField（非 TMP）</summary>
        public bool IsLegacy => _isLegacy;

        #endregion
    }
}
