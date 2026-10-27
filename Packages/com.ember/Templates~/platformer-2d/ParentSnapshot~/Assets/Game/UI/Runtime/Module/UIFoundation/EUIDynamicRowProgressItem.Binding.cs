/*=============================================================
 * author       : Bingo
 * prefab name  : EUIDynamicRowProgressItem
 * page name    : 
 * update time  : 2026/10/27 10:34:45
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIDynamicRowProgressItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private UnityEngine.UI.Slider Progress;

        /// <summary>
        /// Value
        /// </summary>
        private Ember.UIExtension.TMPEx Value;



    public override void OnBind()
    {
        base.OnBind();
            Progress = ControlMap["Progress"] as UnityEngine.UI.Slider;
            Value = ControlMap["Value"] as Ember.UIExtension.TMPEx;

    }
}
}
