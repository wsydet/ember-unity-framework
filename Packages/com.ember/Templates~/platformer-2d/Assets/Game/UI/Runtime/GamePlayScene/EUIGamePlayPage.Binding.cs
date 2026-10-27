/*=============================================================
 * author       : Bingo
 * prefab name  : EUIGamePlayPage
 * page name    : EUIGamePlayPage
 * update time  : 2026/10/27 11:35:07
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIGamePlayPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Back
        /// </summary>
        private Button Btn_Back;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Settings
        /// </summary>
        private Button Btn_Settings;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Save
        /// </summary>
        private Button Btn_Save;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_SaveExit
        /// </summary>
        private Button Btn_SaveExit;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Respawn
        /// </summary>
        private Button Btn_Respawn;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Txt_Status
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Status;

        /// <summary>
        /// Animator/EUISafeArea/EUIPlayerHealthItem
        /// </summary>
        private UnityEngine.RectTransform HealthRoot;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Back = ControlMap["Btn_Back"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;
            Btn_Save = ControlMap["Btn_Save"] as Button;
            Btn_SaveExit = ControlMap["Btn_SaveExit"] as Button;
            Btn_Respawn = ControlMap["Btn_Respawn"] as Button;
            Txt_Status = ControlMap["Txt_Status"] as TMPro.TextMeshProUGUI;
            HealthRoot = ControlMap["HealthRoot"] as UnityEngine.RectTransform;

    }
}
}
