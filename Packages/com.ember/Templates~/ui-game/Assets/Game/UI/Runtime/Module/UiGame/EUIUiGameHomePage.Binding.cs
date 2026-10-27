/*=============================================================
 * author       : Bingo
 * prefab name  : EUIUiGameHomePage
 * page name    : EUIUiGameHomePage
 * update time  : 2026/10/27 1:46:48
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIUiGameHomePage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Description
        /// </summary>
        private Ember.UIExtension.TMPEx Description;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Start
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Start;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Settings
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Settings;



    public override void OnBind()
    {
        base.OnBind();
            Description = ControlMap["Description"] as Ember.UIExtension.TMPEx;
            Start = ControlMap["Start"] as Ember.UIExtension.EUIButtonEx;
            Settings = ControlMap["Settings"] as Ember.UIExtension.EUIButtonEx;

    }
}
}
