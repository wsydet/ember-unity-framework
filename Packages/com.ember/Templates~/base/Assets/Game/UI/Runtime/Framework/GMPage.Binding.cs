/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : GMPanel
 * page name    : GMPage
 * update time  : 2026/8/28 12:20:59
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
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
        /// m_Panel_GM/Infos/Time/m_Pgb_TimeScale
        /// </summary>
        private Slider Pgb_TimeScale;

        /// <summary>
        /// m_Panel_GM/Infos/Time/m_Txt_TimeScale
        /// </summary>
        private TMP_Text Txt_TimeScale;

        /// <summary>
        /// m_Panel_GM/Infos/顶层状态/m_Txt_GameState
        /// </summary>
        private TMP_Text Txt_GameState;

        /// <summary>
        /// m_Panel_GM/Infos/开关/m_Tgl_Test
        /// </summary>
        private Toggle Tgl_Test;

        /// <summary>
        /// m_Panel_GM/Infos/ScrollRect/m_Scr_Test
        /// </summary>
        private ScrollRect Scr_Test;

        /// <summary>
        /// m_Panel_GM/Infos/Image/m_Img_Test
        /// </summary>
        private Image Img_Test;

        /// <summary>
        /// m_Panel_GM/Infos/RawIamge/m_Raw_Test
        /// </summary>
        private RawImage Raw_Test;

        /// <summary>
        /// m_Panel_GM/Buttons/m_EUIBtn_Exit
        /// </summary>
        private Ember.UIExtension.EUIButtonEx EUIBtn_Exit;

        /// <summary>
        /// m_Panel_GM/m_EUITgl_Test
        /// </summary>
        private Ember.UIExtension.EUIToggleEx EUITgl_Test;

        /// <summary>
        /// m_Panel_GM/m_EUIImg_Test
        /// </summary>
        private Ember.UIExtension.EUIImageEx EUIImg_Test;

        /// <summary>
        /// m_Panel_GM/m_Img_Circle
        /// </summary>
        private Ember.UIExtension.EUICircleImage Img_Circle;



    public override void OnBind()
    {
        base.OnBind();
            Btn_GM = ControlMap["Btn_GM"] as Button;
            Panel_GM = ControlMap["Panel_GM"] as Component;
            Pgb_TimeScale = ControlMap["Pgb_TimeScale"] as Slider;
            Txt_TimeScale = ControlMap["Txt_TimeScale"] as TMP_Text;
            Txt_GameState = ControlMap["Txt_GameState"] as TMP_Text;
            Tgl_Test = ControlMap["Tgl_Test"] as Toggle;
            Scr_Test = ControlMap["Scr_Test"] as ScrollRect;
            Img_Test = ControlMap["Img_Test"] as Image;
            Raw_Test = ControlMap["Raw_Test"] as RawImage;
            EUIBtn_Exit = ControlMap["EUIBtn_Exit"] as Ember.UIExtension.EUIButtonEx;
            EUITgl_Test = ControlMap["EUITgl_Test"] as Ember.UIExtension.EUIToggleEx;
            EUIImg_Test = ControlMap["EUIImg_Test"] as Ember.UIExtension.EUIImageEx;
            Img_Circle = ControlMap["Img_Circle"] as Ember.UIExtension.EUICircleImage;

    }
}
}
