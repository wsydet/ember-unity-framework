/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMEntryItem
 * page name    : 
 * update time  : 2026/11/17 15:34:02
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassGMEntryItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Button;

        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIFloatingEntry Floating;



    public override void OnBind()
    {
        base.OnBind();
            Button = ControlMap["Button"] as Ember.UIExtension.EUIButtonEx;
            Floating = ControlMap["Floating"] as Ember.UIExtension.EUIFloatingEntry;

    }
}
}
