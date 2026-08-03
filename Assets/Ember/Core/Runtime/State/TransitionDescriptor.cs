using System;

namespace Ember.Core
{
    /// <summary>
    /// 流转描述符 —— 声明一个状态可以流转到哪个目标状态，以及进入条件。
    ///
    /// 双重用途：
    /// <b>1. 可视化编辑器</b> —— 扫描 <see cref="EmberGameState.GetTransitions"/> 构建连线图。
    ///     <c>TargetState</c> 是箭头指向的节点，<c>Label</c> 是箭头上的文字，
    ///     <c>Condition</c> 不为空时边显示为不同颜色（受限边）。
    ///
    /// <b>2. 运行时校验</b> —— <see cref="EmberStateMachine.TransitionTo"/> 在切换前
    ///     查找对应描述符，如果有 <c>Guard</c> 且返回 false，则拒绝切换。
    ///
    /// <b>用法：</b>
    /// <code>
    /// public override TransitionDescriptor[] GetTransitions() => new[]
    /// {
    ///     new TransitionDescriptor(typeof(MainState), "返回大厅"),
    ///     new TransitionDescriptor(typeof(RaidState), "突袭副本")
    ///     {
    ///         Condition = "需要登录",
    ///         Guard = () => IsLoggedIn,
    ///     },
    /// };
    /// </code>
    /// </summary>
    public class TransitionDescriptor
    {
        /// <summary>目标状态类型（必须继承 EmberGameState）。</summary>
        public Type TargetState { get; init; }

        /// <summary>可视化编辑器中连线上的标签文字。留空则只显示箭头。</summary>
        public string Label { get; init; } = "";

        /// <summary>
        /// 条件的文字描述，可视化编辑器中展示用。
        /// 如 "需要登录"、"等级 ≥ 10"、"VIP 玩家"。
        /// 为空表示无条件边。
        /// </summary>
        public string Condition { get; init; } = "";

        /// <summary>
        /// 运行时准入条件。null = 无条件，始终允许。
        /// 返回 false 时 <see cref="EmberStateMachine.TransitionTo"/> 会拒绝切换并打印 Warning。
        /// </summary>
        public Func<bool> Guard { get; init; }

        public TransitionDescriptor() { }

        /// <summary>快捷构造：无条件边。</summary>
        public TransitionDescriptor(Type targetState, string label = "")
        {
            TargetState = targetState;
            Label = label;
        }

        /// <summary>条件边快捷构造。</summary>
        public TransitionDescriptor(Type targetState, string label, string condition, Func<bool> guard)
        {
            TargetState = targetState;
            Label = label;
            Condition = condition;
            Guard = guard;
        }

        /// <summary>用于 Dictionary / HashSet 的去重键。</summary>
        public override int GetHashCode() => TargetState?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
        {
            return obj is TransitionDescriptor other
                && other.TargetState == TargetState;
        }
    }
}
