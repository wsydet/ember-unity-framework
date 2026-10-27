/*=============================================================
 * author       : Bingo
 * prefab name  : EUILibraryPopupPage
 * page name    : EUILibraryPopupPage
 * update time  : 2026/10/27 9:12:40
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILibraryPopupPage : Ember.UI.EUILogic
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
        /// Animator/EUISafeArea/Center/Close
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Close;



    public override void OnBind()
    {
        base.OnBind();
            Title = ControlMap["Title"] as Ember.UIExtension.TMPEx;
            Content = ControlMap["Content"] as UnityEngine.RectTransform;
            Close = ControlMap["Close"] as Ember.UIExtension.EUIButtonEx;

    }
}
}
