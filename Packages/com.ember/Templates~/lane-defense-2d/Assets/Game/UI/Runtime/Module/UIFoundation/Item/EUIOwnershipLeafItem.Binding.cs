/*=============================================================
 * author       : Bingo
 * prefab name  : EUIOwnershipLeafItem
 * page name    : 
 * update time  : 2026/11/11 15:26:38
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIOwnershipLeafItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// m_Txt_Label
        /// </summary>
        private TMP_Text Txt_Label;



    public override void OnBind()
    {
        base.OnBind();
            Txt_Label = ControlMap["Txt_Label"] as TMP_Text;

    }
}
}
