/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMTimeItem
 * page name    : 
 * update time  : 2026/11/19 19:17:15
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassGMTimeItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Enable
        /// </summary>
        private Ember.UIExtension.EUIToggleEx Enable;

        /// <summary>
        /// TimeScale
        /// </summary>
        private UnityEngine.UI.Slider Slider;

        /// <summary>
        /// TimeScale/Value
        /// </summary>
        private Ember.UIExtension.TMPEx Value;

        /// <summary>
        /// Reset
        /// </summary>
        private Ember.UIExtension.EUIButtonEx Reset;



    public override void OnBind()
    {
        base.OnBind();
            Enable = ControlMap["Enable"] as Ember.UIExtension.EUIToggleEx;
            Slider = ControlMap["Slider"] as UnityEngine.UI.Slider;
            Value = ControlMap["Value"] as Ember.UIExtension.TMPEx;
            Reset = ControlMap["Reset"] as Ember.UIExtension.EUIButtonEx;

    }
}
}
