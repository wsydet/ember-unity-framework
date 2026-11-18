/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassPanelItem
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
    public partial class EUISoftGlassPanelItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIGlassImage Surface;

        /// <summary>
        /// Header
        /// </summary>
        private Ember.UIExtension.TMPEx Header;

        /// <summary>
        /// Content
        /// </summary>
        private UnityEngine.RectTransform Content;



    public override void OnBind()
    {
        base.OnBind();
            Surface = ControlMap["Surface"] as Ember.UIExtension.EUIGlassImage;
            Header = ControlMap["Header"] as Ember.UIExtension.TMPEx;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;

    }
}
}
