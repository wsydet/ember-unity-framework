using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Guide
{
    /// <summary>
    /// 单条引导定义 —— 描述一个引导流程的完整步骤。
    ///
    /// <b>创建方式：</b>Assets → Create → Ember/Guide/GuideDefine，
    /// 然后在上方 <see cref="GuideConfig"/> 里引用它。
    ///
    /// <b>结构：</b>
    /// - <see cref="baseSkipAll"/>：全局跳过条件（满足则整条引导跳过）。
    /// - <see cref="guideSteps"/>：顺序执行的步骤列表。
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/Guide/GuideDefine", fileName = "GuideDefine")]
    public class GuideDefine : ScriptableObject
    {
        #region 编辑器面板参数

        /// <summary>全局跳过条件：满足则跳过整条引导（不进入任何步骤）。</summary>
        [SerializeReference]
        public GuideConditionBase baseSkipAll;

        /// <summary>步骤列表，按顺序执行。</summary>
        public List<GuideStepDefine> guideSteps = new();

        #endregion
    }
}
