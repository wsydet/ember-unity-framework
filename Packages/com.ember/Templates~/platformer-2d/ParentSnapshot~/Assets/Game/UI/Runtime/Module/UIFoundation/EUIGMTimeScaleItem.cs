// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUIGMTimeScaleItem
    {
        public event System.Action<float> Changed;
        public float CurrentValue => Slider.value;
        public override void OnInit()
        {
            base.OnInit(); Slider.onValueChanged.AddListener(OnValueChanged);
            Slider.SetValueWithoutNotify(1f); RefreshLabel(1f);
        }
        public override void OnDispose()
        { Slider.onValueChanged.RemoveListener(OnValueChanged); Changed = null; base.OnDispose(); }
        public void SetControlEnabled(bool enabled)
        {
            if (!enabled) { Slider.SetValueWithoutNotify(1f); RefreshLabel(1f); Changed?.Invoke(1f); }
            Slider.gameObject.SetActive(enabled);
        }
        private void OnValueChanged(float value) { RefreshLabel(value); Changed?.Invoke(value); }
        private void RefreshLabel(float value) => Label.SetSource($"TimeScale: {value:F2}x");
    }
}
