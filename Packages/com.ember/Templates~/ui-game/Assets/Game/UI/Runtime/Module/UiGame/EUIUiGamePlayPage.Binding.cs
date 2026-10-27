/*=============================================================
 * author       : Bingo
 * prefab name  : EUIUiGamePlayPage
 * page name    : EUIUiGamePlayPage
 * update time  : 2026/10/27 1:46:50
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIUiGamePlayPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Status
        /// </summary>
        private Ember.UIExtension.TMPEx Status;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Advance
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Advance;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Settings
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Settings;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Home
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Home;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Steps
        /// </summary>
        private Ember.UIExtension.EUIDynamicListHost Steps;



    public override void OnBind()
    {
        base.OnBind();
            Status = ControlMap["Status"] as Ember.UIExtension.TMPEx;
            Advance = ControlMap["Advance"] as Ember.UIExtension.EUIButtonEx;
            Settings = ControlMap["Settings"] as Ember.UIExtension.EUIButtonEx;
            Home = ControlMap["Home"] as Ember.UIExtension.EUIButtonEx;
            Steps = ControlMap["Steps"] as Ember.UIExtension.EUIDynamicListHost;

    }
}
}
