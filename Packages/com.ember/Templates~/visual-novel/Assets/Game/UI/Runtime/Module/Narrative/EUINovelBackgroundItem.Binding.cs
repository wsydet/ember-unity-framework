/*=============================================================
 * author       : Bingo
 * prefab name  : EUINovelBackgroundItem
 * page name    : 
 * update time  : 2026/9/17 20:43:44
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUINovelBackgroundItem : Ember.UI.EUILogic
    {
        /// <summary>
        /// Picture
        /// </summary>
        private UnityEngine.UI.Image Picture;



    public override void OnBind()
    {
        base.OnBind();
            Picture = ControlMap["Picture"] as UnityEngine.UI.Image;

    }
}
}
