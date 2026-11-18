/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassSwitchItem
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
    public partial class EUISoftGlassSwitchItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIToggleEx Toggle;

        /// <summary>
        /// Label
        /// </summary>
        private Ember.UIExtension.TMPEx Label;



    public override void OnBind()
    {
        base.OnBind();
            Toggle = ControlMap["Toggle"] as Ember.UIExtension.EUIToggleEx;
            Label = ControlMap["Label"] as Ember.UIExtension.TMPEx;

    }
}
}
