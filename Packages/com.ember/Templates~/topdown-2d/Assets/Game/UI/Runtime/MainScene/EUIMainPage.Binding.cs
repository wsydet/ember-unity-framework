/*=============================================================
 * author       : Bingo
 * prefab name  : EUIMainPage
 * page name    : EUIMainPage
 * update time  : 2026/10/28 22:14:53
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
        /// Btn_Continue
        /// </summary>
        private Button Btn_Continue;

        /// <summary>
        /// Txt_Progress
        /// </summary>
        private TMP_Text Txt_Progress;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Start = ControlMap["Btn_Start"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;
            Btn_Continue = ControlMap["Btn_Continue"] as Button;
            Txt_Progress = ControlMap["Txt_Progress"] as TMP_Text;

    }
}
}
