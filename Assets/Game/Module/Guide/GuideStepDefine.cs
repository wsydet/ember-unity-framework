using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Guide
{
    /// <summary>
    /// 单个引导步骤 —— 描述「开始阶段」与「结束阶段」各自的事件 / 条件 / 执行器。
    ///
    /// <b>开始阶段（NotStart → Doing）：</b>
    /// 先按顺序检查跳过 / 成功条件，满足后执行 <see cref="startExecutors"/> 并注册 <see cref="endEvents"/>。
    /// <b>结束阶段（Doing → Finished）：</b>
    /// 由 <see cref="endEvents"/>（或 <see cref="needUpdate"/> 每帧轮询）驱动，检查完成 / 取消条件，
    /// 满足后执行 <see cref="endExecutors"/> 并进入下一步或完成。
    /// </summary>
    [Serializable]
    public class GuideStepDefine
    {
        #region 编辑器面板参数

        /// <summary>步骤名（仅调试 / 日志用）。</summary>
        public string name;

        /// <summary>是否每帧轮询条件。默认 false = 由事件驱动；true = 每帧重新检查条件。</summary>
        public bool needUpdate;

        // ---- 开始阶段 ----

        /// <summary>开始阶段监听的事件（条件失败时注册，等待这些事件触发重试）。</summary>
        [SerializeReference]
        public List<GuideEvent> startEvents = new();

        /// <summary>开始条件：满足则跳过整条引导。</summary>
        [SerializeReference]
        public GuideConditionBase startConditionsToSkipAll;

        /// <summary>开始条件：满足则跳过当前步。</summary>
        [SerializeReference]
        public GuideConditionBase startConditionsToSkip;

        /// <summary>开始条件：满足则进入执行（null = 无条件，直接执行）。</summary>
        [SerializeReference]
        public GuideConditionBase startConditionsToSuccess;

        /// <summary>开始执行器（进入 Doing 时执行）。</summary>
        [SerializeReference]
        public List<GuideExecutor> startExecutors = new();

        // ---- 结束阶段 ----

        /// <summary>结束阶段监听的事件（进入 Doing 后注册，等待这些事件触发完成 / 取消判断）。</summary>
        [SerializeReference]
        public List<GuideEvent> endEvents = new();

        /// <summary>结束条件：满足则完成所有剩余步骤。</summary>
        [SerializeReference]
        public GuideConditionBase endConditionsToFinishAll;

        /// <summary>结束条件：满足则取消整条引导（回到第 0 步）。</summary>
        [SerializeReference]
        public GuideConditionBase endConditionsToCancelAll;

        /// <summary>结束条件：满足则取消当前步（重新执行当前步）。</summary>
        [SerializeReference]
        public GuideConditionBase endConditionsToCancel;

        /// <summary>结束条件：满足则完成当前步进入下一步（null = 无条件，直接完成）。</summary>
        [SerializeReference]
        public GuideConditionBase endConditionsToSuccess;

        /// <summary>结束执行器（完成当前步时执行）。</summary>
        [SerializeReference]
        public List<GuideExecutor> endExecutors = new();

        #endregion
    }
}
