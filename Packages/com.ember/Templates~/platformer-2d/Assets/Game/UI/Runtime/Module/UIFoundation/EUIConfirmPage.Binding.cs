/*=============================================================
 * author       : Bingo
 * prefab name  : EUIConfirmPage
 * page name    : EUIConfirmPage
 * update time  : 2026/10/27 9:12:40
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIConfirmPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Title
        /// </summary>
        private Ember.UIExtension.TMPEx Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/BodyViewport/Body
        /// </summary>
        private Ember.UIExtension.TMPEx Body;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Accept
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Accept;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Cancel
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Cancel;

        /// <summary>
        /// Animator/EUISafeArea/Center/Close
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Close;



    public override void OnBind()
    {
        base.OnBind();
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;
            Body = ControlMap["Body"] as Ember.UIExtension.TMPEx;
            Accept = ControlMap["Accept"] as Ember.UIExtension.EUIButtonEx;
            Cancel = ControlMap["Cancel"] as Ember.UIExtension.EUIButtonEx;
            Close = ControlMap["Close"] as Ember.UIExtension.EUIButtonEx;

    }
}
}
