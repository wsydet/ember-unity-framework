/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassGMStateItem
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
    public partial class EUISoftGlassGMStateItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// State
        /// </summary>
        private Ember.UIExtension.TMPEx State;

        /// <summary>
        /// UnityTime
        /// </summary>
        private Ember.UIExtension.TMPEx UnityTime;

        /// <summary>
        /// EmberTime
        /// </summary>
        private Ember.UIExtension.TMPEx EmberTime;



    public override void OnBind()
    {
        base.OnBind();
            State = ControlMap["State"] as Ember.UIExtension.TMPEx;
            UnityTime = ControlMap["UnityTime"] as Ember.UIExtension.TMPEx;
            EmberTime = ControlMap["EmberTime"] as Ember.UIExtension.TMPEx;

    }
}
}
