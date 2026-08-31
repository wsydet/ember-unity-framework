/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EUIGamePlayPanel
 * page name    : EUIGamePlayPage
 * update time  : 2026/8/28 12:20:55
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
        /// m_Btn_Back
        /// </summary>
        private Button Btn_Back;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Back = ControlMap["Btn_Back"] as Button;

    }
}
}
