/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelChoiceItem
 * page name    : 
 * update time  : 2026/9/29 14:51:57
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelChoiceItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Select
        /// </summary>
        private UnityEngine.UI.Button Select;

        /// <summary>
        /// Select/Label
        /// </summary>
        private TMPro.TextMeshProUGUI Label;



    public override void OnBind()
    {
        base.OnBind();
            Select = ControlMap["Select"] as UnityEngine.UI.Button;
            Label = ControlMap["Label"] as TMPro.TextMeshProUGUI;

    }
}
}
