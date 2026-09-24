/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelSaveSlotItem
 * page name    : 
 * update time  : 2026/9/24 14:21:45
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelSaveSlotItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Summary
        /// </summary>
        private TMPro.TextMeshProUGUI Summary;

        /// <summary>
        /// Write
        /// </summary>
        private UnityEngine.UI.Button Write;

        /// <summary>
        /// Read
        /// </summary>
        private UnityEngine.UI.Button Read;



    public override void OnBind()
    {
        base.OnBind();
            Summary = ControlMap["Summary"] as TMPro.TextMeshProUGUI;
            Write = ControlMap["Write"] as UnityEngine.UI.Button;
            Read = ControlMap["Read"] as UnityEngine.UI.Button;

    }
}
}
