/*=============================================================
 * author       : Bingo
 * prefab name  : EUIDynamicOptionRowItem
 * page name    : 
 * update time  : 2026/10/27 10:34:48
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUIDynamicOptionRowItem : EUIOptionRowItem
    {
        /// <summary>
        /// Icon
        /// </summary>
        private UnityEngine.UI.Image Icon;

        /// <summary>
        /// Progress
        /// </summary>
        private EUIDynamicRowProgressItem Progress;



    public override void OnBind()
    {
        base.OnBind();
            Icon = ControlMap["Icon"] as UnityEngine.UI.Image;
            Progress = GetChildLogic<EUIDynamicRowProgressItem>("Progress");

    }
}
}
