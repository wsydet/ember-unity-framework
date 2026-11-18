/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassAlertPage
 * page name    : EUISoftGlassAlertPage
 * update time  : 2026/11/17 19:44:16
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassAlertPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/Title
        /// </summary>
        private Ember.UIExtension.TMPEx Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/BodyViewport/Body
        /// </summary>
        private Ember.UIExtension.TMPEx Body;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/Accept
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Accept;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/Cancel
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Cancel;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/Close
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Close;

        /// <summary>
        /// Animator/EUISafeArea/Center/Panel/BodyViewport
        /// </summary>
        private UnityEngine.UI.ScrollRect BodyScroll;



    public override void OnBind()
    {
        base.OnBind();
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;
            Body = ControlMap["Body"] as Ember.UIExtension.TMPEx;
            Accept = ControlMap["Accept"] as Ember.UIExtension.EUIButtonEx;
            Cancel = ControlMap["Cancel"] as Ember.UIExtension.EUIButtonEx;
            Close = ControlMap["Close"] as Ember.UIExtension.EUIButtonEx;
            BodyScroll = ControlMap["BodyScroll"] as UnityEngine.UI.ScrollRect;

    }
}
}
