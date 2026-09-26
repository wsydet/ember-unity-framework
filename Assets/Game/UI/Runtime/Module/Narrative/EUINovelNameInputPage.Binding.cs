/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelNameInputPage
 * page name    : EUINovelNameInputPage
 * update time  : 2026/9/26 23:50:20
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelNameInputPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/m_Txt_Title
        /// </summary>
        private TMP_Text Txt_Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/m_Inp_Name
        /// </summary>
        private TMP_InputField Inp_Name;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/m_Btn_Confirm
        /// </summary>
        private Button Btn_Confirm;



    public override void OnBind()
    {
        base.OnBind();
            Txt_Title = ControlMap["Txt_Title"] as TMP_Text;
            Inp_Name = ControlMap["Inp_Name"] as TMP_InputField;
            Btn_Confirm = ControlMap["Btn_Confirm"] as Button;

    }
}
}
