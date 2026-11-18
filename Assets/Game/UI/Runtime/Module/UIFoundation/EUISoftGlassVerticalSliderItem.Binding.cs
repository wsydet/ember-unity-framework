/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassVerticalSliderItem
 * page name    : 
 * update time  : 2026/11/17 15:34:02
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUISoftGlassVerticalSliderItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private UnityEngine.UI.Slider Slider;

        /// <summary>
        /// Value
        /// </summary>
        private Ember.UIExtension.TMPEx Value;



    public override void OnBind()
    {
        base.OnBind();
            Slider = ControlMap["Slider"] as UnityEngine.UI.Slider;
            Value = ControlMap["Value"] as Ember.UIExtension.TMPEx;

    }
}
}
