/*=============================================================
 * author       : Bingo
 * prefab name  : EUISettingPanel
 * page name    : EUISettingPage
 * update time  : 2026/9/21 21:29:05
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
        /// Animator/EUISafeArea/NovelPreferences/NovelTextSpeed
        /// </summary>
        private UnityEngine.UI.Slider NovelTextSpeed;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelAutoInterval
        /// </summary>
        private UnityEngine.UI.Slider NovelAutoInterval;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelBgmVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelBgmVolume;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelSfxVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelSfxVolume;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelVoiceVolume
        /// </summary>
        private UnityEngine.UI.Slider NovelVoiceVolume;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelPreferenceStatus
        /// </summary>
        private TMPro.TextMeshProUGUI NovelPreferenceStatus;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelShakePreference
        /// </summary>
        private UnityEngine.UI.Slider NovelShakePreference;

        /// <summary>
        /// Animator/EUISafeArea/NovelPreferences/NovelFlashPreference
        /// </summary>
        private UnityEngine.UI.Slider NovelFlashPreference;



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

    }
}
}
