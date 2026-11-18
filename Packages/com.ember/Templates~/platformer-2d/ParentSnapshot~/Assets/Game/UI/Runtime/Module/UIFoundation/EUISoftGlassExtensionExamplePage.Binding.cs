/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassExtensionExamplePage
 * page name    : EUISoftGlassExtensionExamplePage
 * update time  : 2026/11/17 19:44:17
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassExtensionExamplePage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/ShowPopup
        /// </summary>
        private Ember.UIExtension.EUIButtonEx ShowPopup;

        /// <summary>
        /// Animator/EUISafeArea/Center/BackgroundMode
        /// </summary>
        private Ember.UIExtension.EUIButtonEx BackgroundMode;

        /// <summary>
        /// Animator/EUISafeArea/Center/Viewport/Content
        /// </summary>
        private UnityEngine.RectTransform Content;

        /// <summary>
        /// Animator/EUISafeArea/Center/Backdrop
        /// </summary>
        private UnityEngine.RectTransform Backdrop;

        /// <summary>
        /// Animator/EUISafeArea/Center/Viewport/Content/DynamicHost
        /// </summary>
        private UnityEngine.RectTransform DynamicHost;

        /// <summary>
        /// Animator/EUISafeArea/Center/Viewport/Content/EUISoftGlassExtensionExampleItem
        /// </summary>
        private EUISoftGlassExtensionExampleItem StaticExample;

        /// <summary>
        /// Animator/EUISafeArea/Center/Title
        /// </summary>
        private Ember.UIExtension.TMPEx Title;



    public override void OnBind()
    {
        base.OnBind();
            ShowPopup = ControlMap["ShowPopup"] as Ember.UIExtension.EUIButtonEx;
            BackgroundMode = ControlMap["BackgroundMode"] as Ember.UIExtension.EUIButtonEx;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;
            Backdrop = ControlMap["Backdrop"] as UnityEngine.RectTransform;
            DynamicHost = ControlMap["DynamicHost"] as UnityEngine.RectTransform;
            StaticExample = GetChildLogic<EUISoftGlassExtensionExampleItem>("StaticExample");
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;

    }
}
}
