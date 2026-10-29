/*=============================================================
 * author       : Bingo
 * prefab name  : EUIOptionRowItem
 * page name    : 
 * update time  : 2026/10/26 19:04:19
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIOptionRowItem : Ember.UI.EUILogic
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
