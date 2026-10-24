/*=============================================================
 * author       : Bingo
 * prefab name  : EUIMainPanel
 * page name    : EUIMainPage
 * update time  : 2026/10/8 21:32:52
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIMainPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Start
        /// </summary>
        private Button Btn_Start;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Settings
        /// </summary>
        private Button Btn_Settings;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Btn_Continue
        /// </summary>
        private Button Btn_Continue;

        /// <summary>
        /// Animator/EUISafeArea/Center/m_Txt_Status
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Status;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Start = ControlMap["Btn_Start"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;
            Btn_Continue = ControlMap["Btn_Continue"] as Button;
            Txt_Status = ControlMap["Txt_Status"] as TMPro.TextMeshProUGUI;

    }
}
}
