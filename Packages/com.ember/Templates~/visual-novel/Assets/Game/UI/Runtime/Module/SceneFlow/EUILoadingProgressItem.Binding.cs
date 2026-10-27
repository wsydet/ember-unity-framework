/*=============================================================
 * author       : Bingo
 * prefab name  : EUILoadingProgressItem
 * page name    : 
 * update time  : 2026/10/27 10:18:38
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILoadingProgressItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private UnityEngine.UI.Slider Progress;

        /// <summary>
        /// Value
        /// </summary>
        private Ember.UIExtension.TMPEx Value;

        /// <summary>
        /// Track
        /// </summary>
        private UnityEngine.RectTransform Track;



    public override void OnBind()
    {
        base.OnBind();
            Progress = ControlMap["Progress"] as UnityEngine.UI.Slider;
            Value = ControlMap["Value"] as Ember.UIExtension.TMPEx;
            Track = ControlMap["Track"] as UnityEngine.RectTransform;

    }
}
}
