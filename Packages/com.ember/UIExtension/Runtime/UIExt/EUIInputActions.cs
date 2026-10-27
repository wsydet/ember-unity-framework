// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ember.UIExtension
{
    /// <summary>输入清除与显式错误展示，验证规则由调用方提供。</summary>
    public sealed class EUIInputActions : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private Button _clear;
        [SerializeField] private TMP_Text _error;
        #endregion
        #region 生命周期
        private void OnEnable() { _input.onValueChanged.AddListener(Refresh); if (_clear) _clear.onClick.AddListener(Clear); Refresh(_input.text); }
        private void OnDisable() { _input.onValueChanged.RemoveListener(Refresh); if (_clear) _clear.onClick.RemoveListener(Clear); }
        #endregion
        #region 内部方法
        private void Refresh(string value) { if (_clear) _clear.interactable = _input.interactable && !_input.readOnly && !string.IsNullOrEmpty(value); }
        private void Clear() { if (!_input.interactable || _input.readOnly) return; _input.text = string.Empty; _input.ActivateInputField(); }
        #endregion
        #region 外部方法
        public TMP_InputField Input => _input;
        public void SetError(string error) { if (_error) { _error.text = error ?? string.Empty; _error.gameObject.SetActive(!string.IsNullOrEmpty(error)); } }
        public void SetReadOnly(bool value) { _input.readOnly = value; Refresh(_input.text); }
        public void SetInteractable(bool value) { _input.interactable = value; Refresh(_input.text); }
        #endregion
    }
}
