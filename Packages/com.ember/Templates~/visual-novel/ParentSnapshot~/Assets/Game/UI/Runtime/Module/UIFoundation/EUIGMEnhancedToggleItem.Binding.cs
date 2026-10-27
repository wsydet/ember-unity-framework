/*=============================================================
 * author       : Bingo
 * prefab name  : EUIGMEnhancedToggleItem
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
    public partial class EUIGMEnhancedToggleItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIToggleEx Toggle;



    public override void OnBind()
    {
        base.OnBind();
            Toggle = ControlMap["Toggle"] as Ember.UIExtension.EUIToggleEx;

    }
}
}
