/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : GMPage
 * page name    : GMPage
 * update time  : 2026/8/18 20:11:33
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ember.UI
{
    public partial class GMPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// m_Btn_GM
        /// </summary>
        private Button Btn_GM;

        /// <summary>
        /// m_Panel_GM
        /// </summary>
        private Component Panel_GM;

        /// <summary>
        /// m_Panel_GM/Info/Time/m_Slider_TimeScale
        /// </summary>
        private Slider Slider_TimeScale;

        /// <summary>
        /// m_Panel_GM/Info/Time/m_Txt_TimeScale
        /// </summary>
        private TMP_Text Txt_TimeScale;



    public override void OnBind()
    {
        base.OnBind();
            Btn_GM = ControlMap["Btn_GM"] as Button;
            Panel_GM = ControlMap["Panel_GM"] as Component;
            Slider_TimeScale = ControlMap["Slider_TimeScale"] as Slider;
            Txt_TimeScale = ControlMap["Txt_TimeScale"] as TMP_Text;

    }
}
}
