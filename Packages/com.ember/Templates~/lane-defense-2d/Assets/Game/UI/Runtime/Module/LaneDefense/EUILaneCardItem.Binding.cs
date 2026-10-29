/*=============================================================
 * author       : Bingo
 * prefab name  : EUILaneCardItem
 * page name    : 
 * update time  : 2026/10/29 14:28:45
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILaneCardItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// LaneContent/Select
        /// </summary>
        private Button Btn_Select;

        /// <summary>
        /// LaneContent/Card
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_Card;

        /// <summary>
        /// LaneContent/State
        /// </summary>
        private TMPro.TextMeshProUGUI Txt_State;



    public override void OnBind()
    {
        base.OnBind();
            Btn_Select = ControlMap["Btn_Select"] as Button;
            Txt_Card = ControlMap["Txt_Card"] as TMPro.TextMeshProUGUI;
            Txt_State = ControlMap["Txt_State"] as TMPro.TextMeshProUGUI;

    }
}
}
