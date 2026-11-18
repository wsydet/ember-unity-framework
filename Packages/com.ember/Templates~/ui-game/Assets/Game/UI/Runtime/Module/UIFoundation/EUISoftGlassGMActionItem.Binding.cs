/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMActionItem
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
    public partial class EUISoftGlassGMActionItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Action
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Button;

        /// <summary>
        /// Label
        /// </summary>
        private Ember.UIExtension.TMPEx Label;

        /// <summary>
        /// Action/Label
        /// </summary>
        private Ember.UIExtension.TMPEx Icon;

        /// <summary>
        /// Action
        /// </summary>
        private Ember.UIExtension.EUIGlassImage Surface;

        /// <summary>
        /// Action/Glyph
        /// </summary>
        private Ember.UIExtension.EUIControlGlyph Glyph;



    public override void OnBind()
    {
        base.OnBind();
            Button = ControlMap["Button"] as Ember.UIExtension.EUIButtonEx;
            Label = ControlMap["Label"] as Ember.UIExtension.TMPEx;
            Icon = ControlMap["Icon"] as Ember.UIExtension.TMPEx;
            Surface = ControlMap["Surface"] as Ember.UIExtension.EUIGlassImage;
            Glyph = ControlMap["Glyph"] as Ember.UIExtension.EUIControlGlyph;

    }
}
}
