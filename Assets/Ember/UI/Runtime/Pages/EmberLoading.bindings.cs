/*=============================================================
 * author       : DESKTOP-5SRUU55
 * prefab name  : EmberLoading
 * page name    : Loading
 * update time  : 2026/8/8 11:45:26
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ember.UI.Pages
{
    public partial class EmberLoading : Ember.UI.EmberUILogic
    {
        /// <summary>
        /// Background
        /// </summary>
        private Image _Background;

        /// <summary>
        /// ProgressBar
        /// </summary>
        private Image _ProgressBar;

        /// <summary>
        /// StatusText
        /// </summary>
        private TMP_Text _StatusText;



    public override void OnBind()
    {
        base.OnBind();
            _Background = ControlMap["_Background"] as Image;
            _ProgressBar = ControlMap["_ProgressBar"] as Image;
            _StatusText = ControlMap["_StatusText"] as TMP_Text;

    }
}
}
