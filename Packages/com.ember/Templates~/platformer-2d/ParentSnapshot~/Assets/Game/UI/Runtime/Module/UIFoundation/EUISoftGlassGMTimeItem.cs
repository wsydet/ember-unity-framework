using Ember.Core;
namespace Game.UI
{
    public partial class EUISoftGlassGMTimeItem
    {
        public override void OnInit()
        {
            base.OnInit(); Enable.SetIsOnWithoutNotify(false); Slider.SetValueWithoutNotify(UnityEngine.Time.timeScale);
            Enable.onValueChanged.AddListener(SetControlEnabled); Slider.onValueChanged.AddListener(OnValueChanged); Reset.onClick.AddListener(ResetTime);
            Slider.interactable = false; Refresh(UnityEngine.Time.timeScale);
            var nestedValue = Slider.transform.Find("Value"); if(nestedValue) nestedValue.gameObject.SetActive(false);
        }
        public override void OnDispose()
        {
            Enable.onValueChanged.RemoveListener(SetControlEnabled); Slider.onValueChanged.RemoveListener(OnValueChanged); Reset.onClick.RemoveListener(ResetTime); base.OnDispose();
        }
        private void SetControlEnabled(bool enabled)
        {
            Slider.interactable = enabled; if(!enabled) { Slider.SetValueWithoutNotify(1); Apply(1); }
        }
        private void ResetTime() { Slider.SetValueWithoutNotify(1); Apply(1); }
        private void OnValueChanged(float value) { if(Enable.isOn) Apply(value); }
        private void Apply(float value) { UnityEngine.Time.timeScale = value; EmberTimeManager.Instance.TimeScale = value; Refresh(value); }
        private void Refresh(float value) => Value.text = value.ToString("0.00") + "×";
        public void RefreshValue() { Slider.SetValueWithoutNotify(UnityEngine.Time.timeScale); Refresh(UnityEngine.Time.timeScale); }
    }
}