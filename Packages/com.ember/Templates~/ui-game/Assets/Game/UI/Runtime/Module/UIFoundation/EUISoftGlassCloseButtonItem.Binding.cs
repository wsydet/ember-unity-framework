/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassCloseButtonItem
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
    public partial class EUISoftGlassCloseButtonItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Button;

        /// <summary>
        /// Label
        /// </summary>
        private Ember.UIExtension.TMPEx Label;

        /// <summary>
        /// Icon
        /// </summary>
        private Ember.UIExtension.TMPEx Icon;

        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIGlassImage Surface;



    public override void OnBind()
    {
        base.OnBind();
            Button = ControlMap["Button"] as Ember.UIExtension.EUIButtonEx;
            Label = ControlMap["Label"] as Ember.UIExtension.TMPEx;
            Icon = ControlMap["Icon"] as Ember.UIExtension.TMPEx;
            Surface = ControlMap["Surface"] as Ember.UIExtension.EUIGlassImage;

    }
}
}
