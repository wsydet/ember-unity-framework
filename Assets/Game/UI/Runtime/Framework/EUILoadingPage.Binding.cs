/*=============================================================
 * author       : Bingo
 * prefab name  : EUILoadingPanel
 * page name    : EUILoadingPage
 * update time  : 2026/9/29 14:51:54
 * ============================================================
 * 本文件为自动生成，请勿修改
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.UI
{
    public partial class EUILoadingPage : Ember.UI.EUILogic
    {
        /// <summary>
        /// Animator/EUISafeArea/BottomCenter/m_Cg_Progress
        /// </summary>
        private CanvasGroup Cg_Progress;

        /// <summary>
        /// Animator/EUISafeArea/BottomCenter/m_Cg_Progress/m_Img_ProgressBar
        /// </summary>
        private Image Img_ProgressBar;

        /// <summary>
        /// Animator/EUISafeArea/BottomCenter/m_Cg_Progress/m_Txt_ProgressNum
        /// </summary>
        private TMP_Text Txt_ProgressNum;

        /// <summary>
        /// Animator/m_TransitionBlock
        /// </summary>
        private Component TransitionBlock;



    public override void OnBind()
    {
        base.OnBind();
            Cg_Progress = ControlMap["Cg_Progress"] as CanvasGroup;
            Img_ProgressBar = ControlMap["Img_ProgressBar"] as Image;
            Txt_ProgressNum = ControlMap["Txt_ProgressNum"] as TMP_Text;
            TransitionBlock = ControlMap["TransitionBlock"] as Component;

    }
}
}
