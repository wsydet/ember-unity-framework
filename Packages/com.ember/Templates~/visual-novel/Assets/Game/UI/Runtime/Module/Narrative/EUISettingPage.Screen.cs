using Game.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public partial class EUISettingPage
    {
        #region 内部参数
        private Slider _shakePreferenceControl, _flashPreferenceControl;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void InitScreenPreferences()
        {
            // Optional until the EUI-center migration has generated the new bindings.
            if (ControlMap.TryGetValue("NovelShakePreference", out var shake)) _shakePreferenceControl = shake as Slider;
            if (ControlMap.TryGetValue("NovelFlashPreference", out var flash)) _flashPreferenceControl = flash as Slider;
            if (_shakePreferenceControl) _shakePreferenceControl.onValueChanged.AddListener(SaveScreenPreferences);
            if (_flashPreferenceControl) _flashPreferenceControl.onValueChanged.AddListener(SaveScreenPreferences);
        }
        private void RefreshScreenPreferences()
        {
            var account = _novelPreferences?.Account;
            if (_shakePreferenceControl) { _shakePreferenceControl.interactable = account != null; _shakePreferenceControl.SetValueWithoutNotify((int)(account?.ShakePreference ?? NovelEffectPreference.Normal)); }
            if (_flashPreferenceControl) { _flashPreferenceControl.interactable = account != null; _flashPreferenceControl.SetValueWithoutNotify((int)(account?.FlashPreference ?? NovelEffectPreference.Normal)); }
        }
        private string ScreenPreferenceSummary()
        {
            if (!_shakePreferenceControl || !_flashPreferenceControl) return "";
            string Label(float value) => Mathf.RoundToInt(value) switch { 1 => "减弱", 2 => "关闭", _ => "正常" };
            return $"\n震动 {Label(_shakePreferenceControl.value)} / 闪光 {Label(_flashPreferenceControl.value)}";
        }
        private void SaveScreenPreferences(float ignored)
        {
            if (!_shakePreferenceControl || !_flashPreferenceControl) return;
            _novelPreferences?.SaveScreenPreferences((NovelEffectPreference)Mathf.RoundToInt(_shakePreferenceControl.value),
                (NovelEffectPreference)Mathf.RoundToInt(_flashPreferenceControl.value));
        }
        private void DisposeScreenPreferences()
        {
            if (_shakePreferenceControl) _shakePreferenceControl.onValueChanged.RemoveListener(SaveScreenPreferences);
            if (_flashPreferenceControl) _flashPreferenceControl.onValueChanged.RemoveListener(SaveScreenPreferences);
        }
        #endregion
    }
}
