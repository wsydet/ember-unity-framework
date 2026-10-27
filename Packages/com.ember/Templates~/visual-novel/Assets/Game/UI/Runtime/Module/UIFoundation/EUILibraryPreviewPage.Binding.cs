/*=============================================================
 * author       : Bingo
 * prefab name  : EUILibraryPreviewPage
 * page name    : EUILibraryPreviewPage
 * update time  : 2026/10/27 12:14:32
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILibraryPreviewPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Title
        /// </summary>
        private Ember.UIExtension.TMPEx Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content
        /// </summary>
        private UnityEngine.RectTransform Content;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Primary
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Primary;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Choice
        /// </summary>
        private Ember.UIExtension.EUIToggleEx Choice;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/EUIProgressItem
        /// </summary>
        private EUILibraryProgressItem Progress;



    public override void OnBind()
    {
        base.OnBind();
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;
            Primary = ControlMap["Primary"] as Ember.UIExtension.EUIButtonEx;
            Choice = ControlMap["Choice"] as Ember.UIExtension.EUIToggleEx;
            Progress = GetChildLogic<EUILibraryProgressItem>("Progress");

    }
}
}
