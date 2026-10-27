/*=============================================================
 * author       : Bingo
 * prefab name  : EUIGMStateItem
 * page name    : 
 * update time  : 2026/10/27 10:34:43
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIGMStateItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.TMPEx Label;



    public override void OnBind()
    {
        base.OnBind();
            Label = ControlMap["Label"] as Ember.UIExtension.TMPEx;

    }
}
}
