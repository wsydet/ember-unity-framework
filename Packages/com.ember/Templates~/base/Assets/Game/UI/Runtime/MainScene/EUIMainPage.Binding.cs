/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EUIMainPanel
 * page name    : EUIMainPage
 * update time  : 2026/8/28 12:20:53
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
        /// m_Btn_Start
        /// </summary>
        private Button Btn_Start;

        /// <summary>
        /// m_Btn_Settings
        /// </summary>
        private Button Btn_Settings;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Start = ControlMap["Btn_Start"] as Button;
            Btn_Settings = ControlMap["Btn_Settings"] as Button;

    }
}
}
