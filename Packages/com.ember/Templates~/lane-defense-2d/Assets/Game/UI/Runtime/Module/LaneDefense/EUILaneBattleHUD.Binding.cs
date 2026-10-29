/*=============================================================
 * author       : Bingo
 * prefab name  : EUILaneBattleHUD
 * page name    : EUILaneBattleHUD
 * update time  : 2026/10/29 14:28:44
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILaneBattleHUD : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Stats
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Stats;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Feedback
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Feedback;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Pause
        /// </summary>
        private Button Btn_Pause;

        /// <summary>
        /// Animator/EUISafeArea/Center/LaneContent/Start
        /// </summary>
        private Button Btn_Start;

        /// <summary>
        /// Animator/EUISafeArea/Center/Cards
        /// </summary>
        private UnityEngine.RectTransform Root_Cards;

        /// <summary>
        /// Animator/EUISafeArea/Center/Controls/Cancel
        /// </summary>
        private Button Btn_Cancel;

        /// <summary>
        /// Animator/EUISafeArea/Center/Controls/Dismantle
        /// </summary>
        private Button Btn_Demolish;



    public override void OnBind()
    {
        base.OnBind();
            Txt_Stats = ControlMap["Txt_Stats"] as TMPro.TextMeshProUGUI;
            Txt_Feedback = ControlMap["Txt_Feedback"] as TMPro.TextMeshProUGUI;
            Btn_Pause = ControlMap["Btn_Pause"] as Button;
            Btn_Start = ControlMap["Btn_Start"] as Button;
            Root_Cards = ControlMap["Root_Cards"] as UnityEngine.RectTransform;
            Btn_Cancel = ControlMap["Btn_Cancel"] as Button;
            Btn_Demolish = ControlMap["Btn_Demolish"] as Button;

    }
}
}
