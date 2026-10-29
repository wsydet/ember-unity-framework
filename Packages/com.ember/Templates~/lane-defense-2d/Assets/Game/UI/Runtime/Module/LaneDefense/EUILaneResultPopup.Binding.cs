/*=============================================================
 * author       : Bingo
 * prefab name  : EUILaneResultPopup
 * page name    : EUILaneResultPopup
 * update time  : 2026/10/29 14:28:46
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILaneResultPopup : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Title
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Detail
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Detail;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Restart
        /// </summary>
        private Button Btn_Restart;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Menu
        /// </summary>
        private Button Btn_Menu;



    public override void OnBind()
    {
        base.OnBind();
            Txt_Title = ControlMap["Txt_Title"] as TMPro.TextMeshProUGUI;
            Txt_Detail = ControlMap["Txt_Detail"] as TMPro.TextMeshProUGUI;
            Btn_Restart = ControlMap["Btn_Restart"] as Button;
            Btn_Menu = ControlMap["Btn_Menu"] as Button;

    }
}
}
