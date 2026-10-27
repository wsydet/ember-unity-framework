// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ember.UIExtension
{
    /// <summary>单实例轻提示，新消息替换旧消息；禁用即取消，不积压队列。</summary>
    public sealed class EUITimedNotice : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private GameObject _content;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _icon;
        #endregion
        #region 内部参数
        private float _deadline;
        private bool _showing;
        #endregion
        #region 生命周期
        private void OnDisable() => Hide();
        private void Update() { if (_showing && Time.unscaledTime >= _deadline) Hide(); }
        #endregion
        #region 外部方法
        public bool IsShowing => _showing;
        public void Show(string message, float seconds = 2, Sprite icon = null)
        {
            if (string.IsNullOrEmpty(message)) { Hide(); return; }
            if (!float.IsFinite(seconds) || seconds <= 0) throw new System.ArgumentOutOfRangeException(nameof(seconds));
            _label.text = message; _icon.sprite = icon; _icon.gameObject.SetActive(icon);
            _content.SetActive(true); _deadline = Time.unscaledTime + seconds; _showing = true;
        }
        public void Hide() { _showing = false; if (_content) _content.SetActive(false); }
        #endregion
    }
}
