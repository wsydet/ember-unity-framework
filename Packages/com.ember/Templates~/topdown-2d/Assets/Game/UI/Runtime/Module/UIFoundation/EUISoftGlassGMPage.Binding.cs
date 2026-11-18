/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMPage
 * page name    : EUISoftGlassGMPage
 * update time  : 2026/11/18 2:01:47
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassGMPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Center/EUISoftGlassGMEntryItem
        /// </summary>
        private EUISoftGlassGMEntryItem Entry;

        /// <summary>
        /// Animator/EUISafeArea/Center/CenterHost/EUISoftGlassGMCenterItem
        /// </summary>
        private EUISoftGlassGMCenterItem Center;

        /// <summary>
        /// Animator/EUISafeArea/Center/CenterHost
        /// </summary>
        private UnityEngine.RectTransform CenterHost;



    public override void OnBind()
    {
        base.OnBind();
            Entry = GetChildLogic<EUISoftGlassGMEntryItem>("Entry");
            Center = GetChildLogic<EUISoftGlassGMCenterItem>("Center");
            CenterHost = ControlMap["CenterHost"] as UnityEngine.RectTransform;

    }
}
}
