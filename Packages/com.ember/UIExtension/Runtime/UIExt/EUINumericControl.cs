// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
namespace Ember.UIExtension
{
    /// <summary>有界数值输入；可连接 Slider 和加减按钮，不负责业务数据。</summary>
    public sealed class EUINumericControl : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Slider _slider;
        [SerializeField] private Button _decrease, _increase;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _minimum, _maximum = 100, _step = 1, _value = 50;
        [SerializeField] private UnityEvent<float> _changed = new();
        private bool _synchronizing;
        #endregion
        #region 生命周期
        private void OnEnable()
        {
            if (_slider) _slider.onValueChanged.AddListener(OnSlide);
            if (_decrease) _decrease.onClick.AddListener(Decrease);
            if (_increase) _increase.onClick.AddListener(Increase);
            SetValue(_value, false);
        }
        private void OnDisable()
        {
            if (_slider) _slider.onValueChanged.RemoveListener(OnSlide);
            if (_decrease) _decrease.onClick.RemoveListener(Decrease);
            if (_increase) _increase.onClick.RemoveListener(Increase);
        }
        #endregion
        #region 内部方法
        private void OnSlide(float value) { if (!_synchronizing) SetValue(value); }
        private void Decrease() => SetValue(_value - _step);
        private void Increase() => SetValue(_value + _step);
        #endregion
        #region 外部方法
        public float Value => _value;
        public UnityEvent<float> Changed => _changed;
        public void Configure(float minimum, float maximum, float step, float value)
        {
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || !float.IsFinite(step) || maximum < minimum || step <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(step));
            _minimum = minimum; _maximum = maximum; _step = step; SetValue(value, false);
        }
        public void SetValue(float value, bool notify = true)
        {
            if (!float.IsFinite(value)) throw new System.ArgumentOutOfRangeException(nameof(value));
            var next = Mathf.Clamp(_minimum + Mathf.Round((value - _minimum) / Mathf.Max(.0001f, _step)) * _step, _minimum, _maximum);
            var different = !Mathf.Approximately(next, _value); _value = next;
            if (_slider) { _synchronizing = true; try { _slider.minValue = _minimum; _slider.maxValue = _maximum; _slider.SetValueWithoutNotify(next); } finally { _synchronizing = false; } }
            if (_label) _label.text = next.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            if (_decrease) _decrease.interactable = next > _minimum;
            if (_increase) _increase.interactable = next < _maximum;
            if (notify && different) _changed.Invoke(next);
        }
        #endregion
    }
}
