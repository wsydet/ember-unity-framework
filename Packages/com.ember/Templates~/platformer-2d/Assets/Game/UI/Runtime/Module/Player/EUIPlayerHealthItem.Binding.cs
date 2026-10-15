/*=============================================================
 * author       : Bingo
 * prefab name  : EUIPlayerHealthItem
 * page name    : 
 * update time  : 2026/10/15 11:06:43
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIPlayerHealthItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Fill
        /// </summary>
        private UnityEngine.RectTransform HealthFill;

        /// <summary>
        /// Value
        /// </summary>
        private TMPro.TextMeshProUGUI HealthValue;



    public override void OnBind()
    {
        base.OnBind();
            HealthFill = ControlMap["HealthFill"] as UnityEngine.RectTransform;
            HealthValue = ControlMap["HealthValue"] as TMPro.TextMeshProUGUI;

    }
}
}
