/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelHistoryPage
 * page name    : EUINovelHistoryPage
 * update time  : 2026/9/20 20:18:51
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelHistoryPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/HistoryPanel/Close
        /// </summary>
        private UnityEngine.UI.Button Close;

        /// <summary>
        /// Animator/EUISafeArea/HistoryPanel/Saves
        /// </summary>
        private UnityEngine.UI.Button Saves;

        /// <summary>
        /// Animator/EUISafeArea/HistoryPanel/Viewport/Content
        /// </summary>
        private TMPro.TextMeshProUGUI Entries;

        /// <summary>
        /// Animator/EUISafeArea/HistoryPanel/Viewport
        /// </summary>
        private UnityEngine.UI.ScrollRect HistoryScroll;



    public override void OnBind()
    {
        base.OnBind();
            Close = ControlMap["Close"] as UnityEngine.UI.Button;
            Saves = ControlMap["Saves"] as UnityEngine.UI.Button;
            Entries = ControlMap["Entries"] as TMPro.TextMeshProUGUI;
            HistoryScroll = ControlMap["HistoryScroll"] as UnityEngine.UI.ScrollRect;

    }
}
}
