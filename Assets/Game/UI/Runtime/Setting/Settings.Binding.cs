/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : Settings
 * page name    : Settings
 * update time  : 2026/8/11 21:20:57
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class Settings : Ember.UI.EUILogic
    {
        /// <summary>
        /// Panel/m_Btn_Close
        /// </summary>
        private Button Btn_Close;

        /// <summary>
        /// Panel/m_Txt_NowScene
        /// </summary>
        private TMP_Text Txt_NowScene;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Close = ControlMap["Btn_Close"] as Button;
            Txt_NowScene = ControlMap["Txt_NowScene"] as TMP_Text;

    }
}
}
