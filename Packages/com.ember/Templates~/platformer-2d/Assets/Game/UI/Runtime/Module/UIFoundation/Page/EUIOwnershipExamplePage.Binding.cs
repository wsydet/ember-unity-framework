/*=============================================================
 * author       : Bingo
 * prefab name  : EUIOwnershipExamplePage
 * page name    : EUIOwnershipExamplePage
 * update time  : 2026/11/11 15:26:37
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIOwnershipExamplePage : Ember.UI.EUILogic
    {
        /// <summary>
        /// m_DynamicItem
        /// </summary>
        private UnityEngine.RectTransform DynamicItem;

        /// <summary>
        /// m_StaticItem
        /// </summary>
        private EUIOwnershipLeafItem StaticItem;



    public override void OnBind()
    {
        base.OnBind();
            DynamicItem = ControlMap["DynamicItem"] as UnityEngine.RectTransform;
            StaticItem = GetChildLogic<EUIOwnershipLeafItem>("StaticItem");

    }
}
}
