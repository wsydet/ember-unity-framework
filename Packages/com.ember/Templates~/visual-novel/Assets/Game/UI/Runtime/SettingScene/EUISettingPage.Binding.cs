/*=============================================================
 * author       : Bingo
 * prefab name  : EUISettingPanel
 * page name    : EUISettingPage
 * update time  : 2026/9/26 23:49:30
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISettingPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/PanelBg/m_Btn_Close
        /// </summary>
        private Button Btn_Close;

        /// <summary>
        /// Animator/EUISafeArea/Center/PanelBg/m_Txt_NowScene
        /// </summary>
        private TMP_Text Txt_NowScene;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelTextSpeed
        /// </summary>
        private UnityEngine.UI.Slider NovelTextSpeed;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelAutoInterval
        /// </summary>
        private UnityEngine.UI.Slider NovelAutoInterval;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelBgmVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelBgmVolume;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelSfxVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelSfxVolume;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelVoiceVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelVoiceVolume;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelPreferenceStatus
        /// </summary>
        private TMPro.TextMeshProUGUI NovelPreferenceStatus;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelShakePreference
        /// </summary>
        private UnityEngine.UI.Slider NovelShakePreference;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelFlashPreference
        /// </summary>
        private UnityEngine.UI.Slider NovelFlashPreference;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelTextSpeedValue
        /// </summary>
        private TMP_Text NovelTextSpeedValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelAutoIntervalValue
        /// </summary>
        private TMP_Text NovelAutoIntervalValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelBgmVolumeValue
        /// </summary>
        private TMP_Text NovelBgmVolumeValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelSfxVolumeValue
        /// </summary>
        private TMP_Text NovelSfxVolumeValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelVoiceVolumeValue
        /// </summary>
        private TMP_Text NovelVoiceVolumeValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelShakePreferenceValue
        /// </summary>
        private TMP_Text NovelShakePreferenceValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/NovelFlashPreferenceValue
        /// </summary>
        private TMP_Text NovelFlashPreferenceValue;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/LanguageZhHans
        /// </summary>
        private UnityEngine.UI.Button LanguageZhHans;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/LanguageZhHant
        /// </summary>
        private UnityEngine.UI.Button LanguageZhHant;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/LanguageJa
        /// </summary>
        private UnityEngine.UI.Button LanguageJa;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelPreferences/LanguageEn
        /// </summary>
        private UnityEngine.UI.Button LanguageEn;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Close = ControlMap["Btn_Close"] as Button;
            Txt_NowScene = ControlMap["Txt_NowScene"] as TMP_Text;
            NovelTextSpeed = ControlMap["NovelTextSpeed"] as UnityEngine.UI.Slider;
            NovelAutoInterval = ControlMap["NovelAutoInterval"] as UnityEngine.UI.Slider;
            NovelBgmVolume = ControlMap["NovelBgmVolume"] as UnityEngine.UI.Slider;
            NovelSfxVolume = ControlMap["NovelSfxVolume"] as UnityEngine.UI.Slider;
            NovelVoiceVolume = ControlMap["NovelVoiceVolume"] as UnityEngine.UI.Slider;
            NovelPreferenceStatus = ControlMap["NovelPreferenceStatus"] as TMPro.TextMeshProUGUI;
            NovelShakePreference = ControlMap["NovelShakePreference"] as UnityEngine.UI.Slider;
            NovelFlashPreference = ControlMap["NovelFlashPreference"] as UnityEngine.UI.Slider;
            NovelTextSpeedValue = ControlMap["NovelTextSpeedValue"] as TMP_Text;
            NovelAutoIntervalValue = ControlMap["NovelAutoIntervalValue"] as TMP_Text;
            NovelBgmVolumeValue = ControlMap["NovelBgmVolumeValue"] as TMP_Text;
            NovelSfxVolumeValue = ControlMap["NovelSfxVolumeValue"] as TMP_Text;
            NovelVoiceVolumeValue = ControlMap["NovelVoiceVolumeValue"] as TMP_Text;
            NovelShakePreferenceValue = ControlMap["NovelShakePreferenceValue"] as TMP_Text;
            NovelFlashPreferenceValue = ControlMap["NovelFlashPreferenceValue"] as TMP_Text;
            LanguageZhHans = ControlMap["LanguageZhHans"] as UnityEngine.UI.Button;
            LanguageZhHant = ControlMap["LanguageZhHant"] as UnityEngine.UI.Button;
            LanguageJa = ControlMap["LanguageJa"] as UnityEngine.UI.Button;
            LanguageEn = ControlMap["LanguageEn"] as UnityEngine.UI.Button;

    }
}
}
