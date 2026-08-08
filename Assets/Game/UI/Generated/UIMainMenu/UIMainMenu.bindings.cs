/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : MainMenu
 * page name    : MainMenu
 * update time  : 2026/8/7 21:01:24
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class UIMainMenu : Ember.UI.EmberUILogic
    {
        /// <summary>
        /// BgImage
        /// </summary>
        private Image _BgImage;

        /// <summary>
        /// TitleText
        /// </summary>
        private TMP_Text _TitleText;

        /// <summary>
        /// BtnSettings
        /// </summary>
        private Button _BtnSettings;

        /// <summary>
        /// BtnSettings/Text
        /// </summary>
        private TMP_Text _Text;

        /// <summary>
        /// BtnStart
        /// </summary>
        private Button _BtnStart;

        /// <summary>
        /// BtnStart/Text
        /// </summary>
        private TMP_Text _Text_1;

        /// <summary>
        /// VersionText
        /// </summary>
        private TMP_Text _VersionText;



    public override void OnBind()
    {
        base.OnBind();
            _BgImage = ControlMap["_BgImage"] as Image;
            _TitleText = ControlMap["_TitleText"] as TMP_Text;
            _BtnSettings = ControlMap["_BtnSettings"] as Button;
            _Text = ControlMap["_Text"] as TMP_Text;
            _BtnStart = ControlMap["_BtnStart"] as Button;
            _Text_1 = ControlMap["_Text_1"] as TMP_Text;
            _VersionText = ControlMap["_VersionText"] as TMP_Text;

    }
}
}
