// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Ember.UIExtension
{
    /// <summary>预制选项的下拉选择，不创建列表实例。容量由 Prefab 决定。</summary>
    public sealed class EUIStaticChoiceMenu : MonoBehaviour, ICancelHandler
    {
        #region 编辑器面板参数
        [SerializeField] private Button _trigger, _blocker;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private Button[] _options;
        [SerializeField] private TMP_Text[] _labels;
        [SerializeField] private UnityEvent<int> _changed = new();
        #endregion
        #region 内部参数
        private UnityAction[] _listeners;
        private int _index = -1, _sibling;
        private bool _opened;
        private readonly Vector3[] _corners = new Vector3[4];
        #endregion
        #region 生命周期
        private void OnEnable()
        {
            _trigger.onClick.AddListener(Toggle); _blocker.onClick.AddListener(Close);
            _listeners = new UnityAction[_options.Length];
            for (var i = 0; i < _options.Length; i++) { var index = i; _listeners[i] = () => Select(index); _options[i].onClick.AddListener(_listeners[i]); }
        }
        private void OnDisable()
        {
            _trigger.onClick.RemoveListener(Toggle); _blocker.onClick.RemoveListener(Close);
            if (_listeners != null) for (var i = 0; i < _options.Length; i++) _options[i].onClick.RemoveListener(_listeners[i]);
            Close();
        }
        #endregion
        #region 内部方法
        private void Toggle() { if (_opened) Close(); else Open(); }
        #endregion
        #region 外部方法
        public int SelectedIndex => _index;
        public UnityEvent<int> Changed => _changed;
        public void SetOption(int index, string title, bool enabled = true)
        { if (index < 0 || index >= _options.Length) throw new ArgumentOutOfRangeException(nameof(index)); _labels[index].text = title; _options[index].interactable = enabled; if (_index == index) _caption.text = title; }
        public void Select(int index)
        {
            if (index < 0 || index >= _options.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (!_options[index].interactable) return;
            var changed = _index != index; _index = index; _caption.text = _labels[index].text; Close(); if (changed) _changed.Invoke(index);
        }
        public void Open()
        {
            if (_opened || !_trigger.interactable) return;
            _opened = true; _sibling = transform.GetSiblingIndex(); transform.SetAsLastSibling();
            _blocker.gameObject.SetActive(true); _panel.gameObject.SetActive(true);
            var canvas = GetComponentInParent<Canvas>().rootCanvas; ((RectTransform)canvas.transform).GetWorldCorners(_corners);
            var bottom = transform.InverseTransformPoint(_corners[0]).y;
            var root = (RectTransform)transform;
            var below = -root.rect.height * .5f - _panel.rect.height * .5f - 4;
            _panel.anchoredPosition = new Vector2(0, below - _panel.rect.height * .5f < bottom + 8 ? -below : below);
            foreach (var option in _options) if (option.interactable) { option.Select(); break; }
        }
        public void Close()
        {
            _panel.gameObject.SetActive(false); _blocker.gameObject.SetActive(false);
            if (_opened) { transform.SetSiblingIndex(_sibling); _opened = false; if (_trigger.isActiveAndEnabled) _trigger.Select(); }
        }
        public void OnCancel(BaseEventData eventData) => Close();
        #endregion
    }
}
