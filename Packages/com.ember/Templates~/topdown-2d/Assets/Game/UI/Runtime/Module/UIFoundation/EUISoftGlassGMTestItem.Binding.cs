/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMTestItem
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
    public partial class EUISoftGlassGMTestItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// EUISoftGlassSwitchItem
        /// </summary>
        private Ember.UIExtension.EUIToggleEx Enhanced;

        /// <summary>
        /// 
        /// </summary>
        private UnityEngine.UI.ScrollRect Scroll;

        /// <summary>
        /// Viewport/Content
        /// </summary>
        private UnityEngine.RectTransform Content;



    public override void OnBind()
    {
        base.OnBind();
            Enhanced = ControlMap["Enhanced"] as Ember.UIExtension.EUIToggleEx;
            Scroll = ControlMap["Scroll"] as UnityEngine.UI.ScrollRect;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;

    }
}
}
