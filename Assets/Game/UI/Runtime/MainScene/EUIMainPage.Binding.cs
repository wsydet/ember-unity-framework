/*=============================================================
 * author       : Bingo
 * prefab name  : EUIMainPanel
 * page name    : EUIMainPage
 * update time  : 2026/9/26 23:49:30
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
        /// Animator/EUISafeArea/Center/MenuButtons/m_Btn_Start
        /// </summary>
        private Button Btn_Start;

        /// <summary>
        /// Animator/EUISafeArea/Center/MenuButtons/m_Btn_Settings
        /// </summary>
        private Button Btn_Settings;

        /// <summary>
        /// Animator/EUISafeArea/Center/MenuButtons/NovelContinue
        /// </summary>
        private UnityEngine.UI.Button NovelContinue;

        /// <summary>
        /// Animator/EUISafeArea/Center/MenuButtons/NovelLoad
        /// </summary>
        private UnityEngine.UI.Button NovelLoad;

        /// <summary>
        /// Animator/EUISafeArea/Center/NovelMessage
        /// </summary>
        private TMPro.TextMeshProUGUI NovelMessage;

        /// <summary>
        /// Animator/EUISafeArea/Center/MenuButtons/NovelQuit
        /// </summary>
        private UnityEngine.UI.Button NovelQuit;

        /// <summary>
        /// Animator/EUISafeArea/Center/TitleText
        /// </summary>
        private TMP_Text NovelTitle;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Start = ControlMap["Btn_Start"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;
            NovelContinue = ControlMap["NovelContinue"] as UnityEngine.UI.Button;
            NovelLoad = ControlMap["NovelLoad"] as UnityEngine.UI.Button;
            NovelMessage = ControlMap["NovelMessage"] as TMPro.TextMeshProUGUI;
            NovelQuit = ControlMap["NovelQuit"] as UnityEngine.UI.Button;
            NovelTitle = ControlMap["NovelTitle"] as TMP_Text;

    }
}
}
