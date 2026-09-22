/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelSavePage
 * page name    : EUINovelSavePage
 * update time  : 2026/9/20 15:22:14
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelSavePage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/Panel/Close
        /// </summary>
        private UnityEngine.UI.Button Close;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Feedback
        /// </summary>
        private TMPro.TextMeshProUGUI Feedback;

        /// <summary>
        /// Animator/EUISafeArea/Panel/Rows
        /// </summary>
        private UnityEngine.RectTransform Rows;

        /// <summary>
        /// SlotTemplate
        /// </summary>
        private UnityEngine.RectTransform SlotTemplate;



    public override void OnBind()
    {
        base.OnBind();
            Close = ControlMap["Close"] as UnityEngine.UI.Button;
            Feedback = ControlMap["Feedback"] as TMPro.TextMeshProUGUI;
            Rows = ControlMap["Rows"] as UnityEngine.RectTransform;
            SlotTemplate = ControlMap["SlotTemplate"] as UnityEngine.RectTransform;

    }
}
}
