/*=============================================================
 * author       : Bingo
 * prefab name  : EUISoftGlassInputItem
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
    public partial class EUISoftGlassInputItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// 
        /// </summary>
        private TMPro.TMP_InputField Input;

        /// <summary>
        /// 
        /// </summary>
        private Ember.UIExtension.EUIInputActions Actions;



    public override void OnBind()
    {
        base.OnBind();
            Input = ControlMap["Input"] as TMPro.TMP_InputField;
            Actions = ControlMap["Actions"] as Ember.UIExtension.EUIInputActions;

    }
}
}
