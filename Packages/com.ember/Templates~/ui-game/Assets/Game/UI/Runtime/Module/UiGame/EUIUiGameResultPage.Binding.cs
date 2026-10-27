/*=============================================================
 * author       : Bingo
 * prefab name  : EUIUiGameResultPage
 * page name    : EUIUiGameResultPage
 * update time  : 2026/10/27 1:46:51
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIUiGameResultPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Summary
        /// </summary>
        private Ember.UIExtension.TMPEx Summary;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Restart
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Restart;

        /// <summary>
        /// Animator/EUISafeArea/Center/Content/Home
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Home;



    public override void OnBind()
    {
        base.OnBind();
            Summary = ControlMap["Summary"] as Ember.UIExtension.TMPEx;
            Restart = ControlMap["Restart"] as Ember.UIExtension.EUIButtonEx;
            Home = ControlMap["Home"] as Ember.UIExtension.EUIButtonEx;

    }
}
}
