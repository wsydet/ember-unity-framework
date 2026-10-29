/*=============================================================
 * author       : Bingo
 * prefab name  : EUITopdownHudPage
 * page name    : EUITopdownHudPage
 * update time  : 2026/10/29 0:12:06
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUITopdownHudPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// StatusPanel/Txt_Stats
        /// </summary>
        private TMP_Text Txt_Stats;

        /// <summary>
        /// StatusPanel/Txt_Status
        /// </summary>
        private TMP_Text Txt_Status;

        /// <summary>
        /// Btn_Save
        /// </summary>
        private Button Btn_Save;

        /// <summary>
        /// Btn_SaveExit
        /// </summary>
        private Button Btn_SaveExit;

        /// <summary>
        /// Btn_Load
        /// </summary>
        private Button Btn_Load;

        /// <summary>
        /// Btn_Retry
        /// </summary>
        private Button Btn_Retry;

        /// <summary>
        /// Btn_Pause
        /// </summary>
        private Button Btn_Pause;

        /// <summary>
        /// Btn_Acknowledge
        /// </summary>
        private Button Btn_Acknowledge;



    public override void OnBind()
    {
        base.OnBind();
            Txt_Stats = ControlMap["Txt_Stats"] as TMP_Text;
            Txt_Status = ControlMap["Txt_Status"] as TMP_Text;
            Btn_Save = ControlMap["Btn_Save"] as Button;
            Btn_SaveExit = ControlMap["Btn_SaveExit"] as Button;
            Btn_Load = ControlMap["Btn_Load"] as Button;
            Btn_Retry = ControlMap["Btn_Retry"] as Button;
            Btn_Pause = ControlMap["Btn_Pause"] as Button;
            Btn_Acknowledge = ControlMap["Btn_Acknowledge"] as Button;

    }
}
}
