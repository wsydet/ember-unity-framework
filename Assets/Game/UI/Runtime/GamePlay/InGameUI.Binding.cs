/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : InGameUI
 * page name    : InGameUI
 * update time  : 2026/8/11 17:05:16
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class InGameUI : Ember.UI.EUILogic
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
