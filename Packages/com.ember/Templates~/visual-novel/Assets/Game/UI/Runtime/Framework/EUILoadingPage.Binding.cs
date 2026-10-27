/*=============================================================
 * author       : Bingo
 * prefab name  : EUILoadingPage
 * page name    : EUILoadingPage
 * update time  : 2026/10/27 12:14:31
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
        /// Animator/m_TransitionBlock
        /// </summary>
        private Component TransitionBlock;

        /// <summary>
        /// Animator/EUISafeArea/BottomCenter/m_Cg_Progress
        /// </summary>
        private EUILoadingProgressItem LoadingProgress;



    public override void OnBind()
    {
        base.OnBind();
            Cg_Progress = ControlMap["Cg_Progress"] as CanvasGroup;
            TransitionBlock = ControlMap["TransitionBlock"] as Component;
            LoadingProgress = GetChildLogic<EUILoadingProgressItem>("LoadingProgress");

    }
}
}
