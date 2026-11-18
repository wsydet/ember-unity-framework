/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassShowcasePage
 * page name    : EUISoftGlassShowcasePage
 * update time  : 2026/11/18 0:45:50
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassShowcasePage : Ember.UI.EUILogic
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
        /// Animator/EUISafeArea/Center/Viewport/Content/EUISoftGlassPrimaryButtonItem
        /// </summary>
        private EUISoftGlassPrimaryButtonItem StaticExample;

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
            StaticExample = GetChildLogic<EUISoftGlassPrimaryButtonItem>("StaticExample");
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;

    }
}
}
