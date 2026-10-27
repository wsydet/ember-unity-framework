/*=============================================================
 * author       : Bingo
 * prefab name  : EUIGamePlayPage
 * page name    : EUIGamePlayPage
 * update time  : 2026/10/27 12:04:13
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



    public override void OnBind()
    {
        base.OnBind();
            Btn_Back = ControlMap["Btn_Back"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;

    }
}
}
