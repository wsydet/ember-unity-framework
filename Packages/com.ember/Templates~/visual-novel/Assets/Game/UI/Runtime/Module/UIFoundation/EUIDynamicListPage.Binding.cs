/*=============================================================
 * author       : Bingo
 * prefab name  : EUIDynamicListPage
 * page name    : EUIDynamicListPage
 * update time  : 2026/10/27 12:14:32
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIDynamicListPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Title
        /// </summary>
        private EUIDynamicSummaryItem Title;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content
        /// </summary>
        private UnityEngine.RectTransform Content;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Empty
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Empty;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/One
        /// </summary>
        private Ember.UIExtension.EUIButtonEx One;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Three
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Three;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Twenty
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Twenty;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Reverse
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Reverse;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Remove
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Remove;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Back
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Back;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/List
        /// </summary>
        private Ember.UIExtension.EUIDynamicListHost List;



    public override void OnBind()
    {
        base.OnBind();
            Title = GetChildLogic<EUIDynamicSummaryItem>("Title");
            Content = ControlMap["Content"] as UnityEngine.RectTransform;
            Empty = ControlMap["Empty"] as Ember.UIExtension.EUIButtonEx;
            One = ControlMap["One"] as Ember.UIExtension.EUIButtonEx;
            Three = ControlMap["Three"] as Ember.UIExtension.EUIButtonEx;
            Twenty = ControlMap["Twenty"] as Ember.UIExtension.EUIButtonEx;
            Reverse = ControlMap["Reverse"] as Ember.UIExtension.EUIButtonEx;
            Remove = ControlMap["Remove"] as Ember.UIExtension.EUIButtonEx;
            Back = ControlMap["Back"] as Ember.UIExtension.EUIButtonEx;
            List = ControlMap["List"] as Ember.UIExtension.EUIDynamicListHost;

    }
}
}
