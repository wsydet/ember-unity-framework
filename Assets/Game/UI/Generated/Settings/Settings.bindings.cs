/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : Settings
 * page name    : Settings
 * update time  : 2026/8/7 21:25:37
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class Settings : Ember.UI.EmberUILogic
    {
        /// <summary>
        /// BgMask
        /// </summary>
        private Image _BgMask;

        /// <summary>
        /// Panel
        /// </summary>
        private Image _Panel;

        /// <summary>
        /// Panel/TitleText
        /// </summary>
        private TMP_Text _TitleText;

        /// <summary>
        /// Panel/ToggleSound
        /// </summary>
        private Toggle _ToggleSound;

        /// <summary>
        /// Panel/SliderVolume
        /// </summary>
        private Slider _SliderVolume;

        /// <summary>
        /// Panel/BtnClose
        /// </summary>
        private Button _BtnClose;

        /// <summary>
        /// Panel/BtnClose/Text
        /// </summary>
        private TMP_Text _Text;

        /// <summary>
        /// Panel/BtnLogout
        /// </summary>
        private Button _BtnLogout;

        /// <summary>
        /// Panel/BtnLogout/Text
        /// </summary>
        private TMP_Text _Text_1;



    public override void OnBind()
    {
        base.OnBind();
            _BgMask = ControlMap["_BgMask"] as Image;
            _Panel = ControlMap["_Panel"] as Image;
            _TitleText = ControlMap["_TitleText"] as TMP_Text;
            _ToggleSound = ControlMap["_ToggleSound"] as Toggle;
            _SliderVolume = ControlMap["_SliderVolume"] as Slider;
            _BtnClose = ControlMap["_BtnClose"] as Button;
            _Text = ControlMap["_Text"] as TMP_Text;
            _BtnLogout = ControlMap["_BtnLogout"] as Button;
            _Text_1 = ControlMap["_Text_1"] as TMP_Text;

    }
}
}
