/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelReadingMenuPage
 * page name    : EUINovelReadingMenuPage
 * update time  : 2026/9/21 10:09:20
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelReadingMenuPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Panel/Close
        /// </summary>
        private UnityEngine.UI.Button Close;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Saves
        /// </summary>
        private UnityEngine.UI.Button Saves;

        /// <summary>
        /// Animator/EUISafeArea/Panel/QuickSave
        /// </summary>
        private UnityEngine.UI.Button QuickSave;

        /// <summary>
        /// Animator/EUISafeArea/Panel/QuickLoad
        /// </summary>
        private UnityEngine.UI.Button QuickLoad;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Settings
        /// </summary>
        private UnityEngine.UI.Button Settings;

        /// <summary>
        /// Animator/EUISafeArea/Panel/ReadSkip
        /// </summary>
        private UnityEngine.UI.Button ReadSkip;

        /// <summary>
        /// Animator/EUISafeArea/Panel/ReturnMenu
        /// </summary>
        private UnityEngine.UI.Button ReturnMenu;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Feedback
        /// </summary>
        private TMPro.TextMeshProUGUI Feedback;



    public override void OnBind()
    {
        base.OnBind();
            Close = ControlMap["Close"] as UnityEngine.UI.Button;
            Saves = ControlMap["Saves"] as UnityEngine.UI.Button;
            QuickSave = ControlMap["QuickSave"] as UnityEngine.UI.Button;
            QuickLoad = ControlMap["QuickLoad"] as UnityEngine.UI.Button;
            Settings = ControlMap["Settings"] as UnityEngine.UI.Button;
            ReadSkip = ControlMap["ReadSkip"] as UnityEngine.UI.Button;
            ReturnMenu = ControlMap["ReturnMenu"] as UnityEngine.UI.Button;
            Feedback = ControlMap["Feedback"] as TMPro.TextMeshProUGUI;

    }
}
}
