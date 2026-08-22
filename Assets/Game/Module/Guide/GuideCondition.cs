using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Module.Guide
{
    /// <summary>
    /// 数值比较操作符（用于 CompareInt 等条件）。
    /// </summary>
    public enum GuideOperator
    {
        Equal,
        Greater,
        Less,
        GreaterAndEqual,
        LessAndEqual,
    }

    /// <summary>
    /// 引导条件类型 —— 具体条件的枚举（对应 <see cref="GuideCondition"/> 的派生逻辑）。
    /// 组合条件请用 <see cref="GuideConditionGroup"/>。
    /// </summary>
    public enum GuideConditionType
    {
        /// <summary>恒真。</summary>
        True = 1,

        /// <summary>恒假。</summary>
        False = 2,

        /// <summary>是否由指定事件触发（param: <see cref="GuideCondParamIsTriggerByEvent"/>）。</summary>
        IsTriggerByEvent = 3,

        /// <summary>指定页面是否展示在顶层（param: <see cref="GuideCondParamIsUIShowing"/>）。</summary>
        IsUIShowing = 4,

        /// <summary>指定引导是否已完成（param: <see cref="GuideCondParamGuideFinished"/>）。</summary>
        IsGuideFinished = 5,

        /// <summary>黑板整型参数比较（param: <see cref="GuideCondParamCompareInt"/>）。</summary>
        CompareInt = 6,
    }

    /// <summary>
    /// 引导条件基类 —— 判断是否满足执行 / 跳过 / 完成条件。
    /// 判定过程会追加到 <paramref name="reason"/>，供诊断「引导为什么没走」。
    /// </summary>
    [Serializable]
    public abstract class GuideConditionBase
    {
        /// <summary>判断条件是否满足。</summary>
        /// <param name="blackboard">引导组黑板。</param>
        /// <param name="triggerEvent">触发本次检查的事件（无事件时为 None）。</param>
        /// <param name="reason">判定过程输出（追加式）。</param>
        public abstract bool IsMet(GuideGroupBlackboard blackboard, GuideEventType triggerEvent, StringBuilder reason);
    }

    /// <summary>逻辑组合操作符。</summary>
    public enum LogicOperator
    {
        And,
        Or,
    }

    /// <summary>
    /// 条件组合 —— 用 AND / OR 组合多个子条件。
    /// </summary>
    [Serializable]
    public class GuideConditionGroup : GuideConditionBase
    {
        #region 编辑器面板参数

        /// <summary>组合逻辑：And = 全部满足；Or = 任一满足。</summary>
        public LogicOperator logicOperator = LogicOperator.And;

        /// <summary>子条件列表。</summary>
        [SerializeReference]
        public List<GuideConditionBase> conditions = new();

        #endregion

        public override bool IsMet(GuideGroupBlackboard blackboard, GuideEventType triggerEvent, StringBuilder reason)
        {
            reason.AppendLine($"\t条件组({logicOperator})：");
            if (logicOperator == LogicOperator.And)
            {
                foreach (var c in conditions)
                {
                    if (c == null) continue;
                    if (!c.IsMet(blackboard, triggerEvent, reason)) return false;
                }
                return true;
            }

            foreach (var c in conditions)
            {
                if (c != null && c.IsMet(blackboard, triggerEvent, reason)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 具体条件 —— 一个 <see cref="GuideConditionType"/> + 对应的参数对象，
    /// 由 <see cref="IsMet"/> 分发到静态判定函数。
    /// </summary>
    [Serializable]
    public class GuideCondition : GuideConditionBase
    {
        #region 编辑器面板参数

        /// <summary>条件类型。</summary>
        public GuideConditionType conditionType;

        /// <summary>条件参数（具体类型见 <see cref="GuideConditionType"/> 各枚举说明）。</summary>
        [SerializeReference]
        public object conditionParams;

        #endregion

        /// <summary>条件类型 → 判定函数的映射。</summary>
        private static readonly Dictionary<GuideConditionType, Func<object, GuideGroupBlackboard, GuideEventType, bool>> Funcs = new()
        {
            [GuideConditionType.True]             = ReturnTrue,
            [GuideConditionType.False]            = ReturnFalse,
            [GuideConditionType.IsTriggerByEvent] = IsTriggerByEvent,
            [GuideConditionType.IsUIShowing]      = IsUIShowing,
            [GuideConditionType.IsGuideFinished]  = IsGuideFinished,
            [GuideConditionType.CompareInt]       = CompareInt,
        };

        public override bool IsMet(GuideGroupBlackboard blackboard, GuideEventType triggerEvent, StringBuilder reason)
        {
            bool success = Funcs.TryGetValue(conditionType, out var f)
                           && f(conditionParams, blackboard, triggerEvent);
            reason.AppendLine($"\t条件:{conditionType} 触发事件:{triggerEvent} 结果:{success}");
            return success;
        }

        #region 判定函数

        private static bool ReturnTrue(object p, GuideGroupBlackboard b, GuideEventType e) => true;

        private static bool ReturnFalse(object p, GuideGroupBlackboard b, GuideEventType e) => false;

        private static bool IsTriggerByEvent(object p, GuideGroupBlackboard b, GuideEventType e)
        {
            if (e == GuideEventType.None) return false;
            return p is GuideCondParamIsTriggerByEvent param && param.eventType == e;
        }

        private static bool IsUIShowing(object p, GuideGroupBlackboard b, GuideEventType e)
            => p is GuideCondParamIsUIShowing param
               && GuideUtils.IsPageShowing(param.pagePath) == param.isShowing;

        private static bool IsGuideFinished(object p, GuideGroupBlackboard b, GuideEventType e)
            => p is GuideCondParamGuideFinished param
               && GuideModule.Instance.IsGuideFinished(param.guideId);

        private static bool CompareInt(object p, GuideGroupBlackboard b, GuideEventType e)
            => p is GuideCondParamCompareInt param && b != null
               && CompareOperator(param.op, b.GetInt(param.intParamIndex), param.value);

        private static bool CompareOperator(GuideOperator op, int cur, int target)
        {
            return op switch
            {
                GuideOperator.Equal            => cur == target,
                GuideOperator.Greater          => cur > target,
                GuideOperator.Less             => cur < target,
                GuideOperator.GreaterAndEqual  => cur >= target,
                GuideOperator.LessAndEqual     => cur <= target,
                _                              => false,
            };
        }

        #endregion
    }

    // ============================================================
    // 条件参数类
    // ============================================================

    /// <summary>「是否由事件触发」条件参数。</summary>
    [Serializable]
    public class GuideCondParamIsTriggerByEvent
    {
        public GuideEventType eventType;
    }

    /// <summary>「页面是否展示」条件参数。</summary>
    [Serializable]
    public class GuideCondParamIsUIShowing
    {
        public string pagePath;
        public bool isShowing;
    }

    /// <summary>「引导是否完成」条件参数。</summary>
    [Serializable]
    public class GuideCondParamGuideFinished
    {
        public int guideId;
    }

    /// <summary>「黑板整型参数比较」条件参数。</summary>
    [Serializable]
    public class GuideCondParamCompareInt
    {
        public int intParamIndex;
        public GuideOperator op;
        public int value;
    }
}
