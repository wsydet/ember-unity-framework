/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelFontPage
 * page name    : EUINovelFontPage
 * update time  : 2026/9/20 20:18:51
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelFontPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Panel/Close
        /// </summary>
        private UnityEngine.UI.Button Close;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Small
        /// </summary>
        private UnityEngine.UI.Button Small;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Medium
        /// </summary>
        private UnityEngine.UI.Button Medium;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Large
        /// </summary>
        private UnityEngine.UI.Button Large;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Preview
        /// </summary>
        private TMPro.TextMeshProUGUI Preview;



    public override void OnBind()
    {
        base.OnBind();
            Close = ControlMap["Close"] as UnityEngine.UI.Button;
            Small = ControlMap["Small"] as UnityEngine.UI.Button;
            Medium = ControlMap["Medium"] as UnityEngine.UI.Button;
            Large = ControlMap["Large"] as UnityEngine.UI.Button;
            Preview = ControlMap["Preview"] as TMPro.TextMeshProUGUI;

    }
}
}
