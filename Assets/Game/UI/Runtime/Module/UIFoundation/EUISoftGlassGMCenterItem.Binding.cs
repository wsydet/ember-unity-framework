/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMCenterItem
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
    public partial class EUISoftGlassGMCenterItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Close
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Close;

        /// <summary>
        /// Title
        /// </summary>
        private Ember.UIExtension.TMPEx Title;

        /// <summary>
        /// Viewport/Content/EUISoftGlassGMStateItem
        /// </summary>
        private EUISoftGlassGMStateItem State;

        /// <summary>
        /// Viewport/Content/EUISoftGlassGMTimeItem
        /// </summary>
        private EUISoftGlassGMTimeItem Time;

        /// <summary>
        /// Viewport/Content/EUISoftGlassGMTestItem
        /// </summary>
        private EUISoftGlassGMTestItem Tests;

        /// <summary>
        /// Viewport/Content/Actions
        /// </summary>
        private UnityEngine.RectTransform Actions;

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
            Close = ControlMap["Close"] as Ember.UIExtension.EUIButtonEx;
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;
            State = GetChildLogic<EUISoftGlassGMStateItem>("State");
            Time = GetChildLogic<EUISoftGlassGMTimeItem>("Time");
            Tests = GetChildLogic<EUISoftGlassGMTestItem>("Tests");
            Actions = ControlMap["Actions"] as UnityEngine.RectTransform;
            Scroll = ControlMap["Scroll"] as UnityEngine.UI.ScrollRect;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;

    }
}
}
