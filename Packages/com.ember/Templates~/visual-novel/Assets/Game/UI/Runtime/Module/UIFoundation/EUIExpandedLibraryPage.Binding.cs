/*=============================================================
 * author       : Bingo
 * prefab name  : EUIExpandedLibraryPage
 * page name    : EUIExpandedLibraryPage
 * update time  : 2026/10/27 12:14:32
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIExpandedLibraryPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Tabs
        /// </summary>
        private Ember.UIExtension.EUIToggleGroupEx Tabs;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Controls
        /// </summary>
        private UnityEngine.RectTransform Controls;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Forms
        /// </summary>
        private UnityEngine.RectTransform Forms;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Feedback
        /// </summary>
        private UnityEngine.RectTransform Feedback;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Layout
        /// </summary>
        private UnityEngine.RectTransform Layout;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Feedback/OpenConfirm
        /// </summary>
        private Ember.UIExtension.EUIButtonEx OpenConfirm;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Feedback/OpenAlert
        /// </summary>
        private Ember.UIExtension.EUIButtonEx OpenAlert;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Feedback/Notify
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Notify;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Feedback/EUIToastItem
        /// </summary>
        private Ember.UIExtension.EUITimedNotice Notice;



    public override void OnBind()
    {
        base.OnBind();
            Tabs = ControlMap["Tabs"] as Ember.UIExtension.EUIToggleGroupEx;
            Controls = ControlMap["Controls"] as UnityEngine.RectTransform;
            Forms = ControlMap["Forms"] as UnityEngine.RectTransform;
            Feedback = ControlMap["Feedback"] as UnityEngine.RectTransform;
            Layout = ControlMap["Layout"] as UnityEngine.RectTransform;
            OpenConfirm = ControlMap["OpenConfirm"] as Ember.UIExtension.EUIButtonEx;
            OpenAlert = ControlMap["OpenAlert"] as Ember.UIExtension.EUIButtonEx;
            Notify = ControlMap["Notify"] as Ember.UIExtension.EUIButtonEx;
            Notice = ControlMap["Notice"] as Ember.UIExtension.EUITimedNotice;

    }
}
}
