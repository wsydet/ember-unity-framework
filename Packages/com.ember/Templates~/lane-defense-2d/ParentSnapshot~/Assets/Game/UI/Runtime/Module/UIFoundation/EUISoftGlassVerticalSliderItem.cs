using System;
namespace Game.UI
{
    public partial class EUISoftGlassVerticalSliderItem
    {
        #region 内部参数
        public event Action<float> Changed;
        public float CurrentValue => Slider.value;
        #endregion
        #region 生命周期
        public override void OnInit() { base.OnInit(); Slider.onValueChanged.AddListener(OnChanged); Refresh(Slider.value); }
        public override void OnDispose() { Slider.onValueChanged.RemoveListener(OnChanged); Changed = null; base.OnDispose(); }
        #endregion
        #region 内部方法
        private void Refresh(float value) => Value.text = (Slider.normalizedValue * 100).ToString("0") + "%";
        private void OnChanged(float value) { Refresh(value); Changed?.Invoke(value); }
        #endregion
        #region 外部方法
        public void SetValue(float value) { Slider.SetValueWithoutNotify(value); Refresh(Slider.value); }
        #endregion
    }
}