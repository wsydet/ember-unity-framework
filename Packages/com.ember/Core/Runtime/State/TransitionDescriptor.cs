using System;

namespace Ember.Core
{
    /// <summary>
    /// 流转描述符 —— 状态间连接线（可视化数据包）。
    ///
    /// 一条边包含：目标状态、加载模式（<see cref="QuickSceneLoad"/>）、标签、条件与准入 Guard。
    /// 起点状态 = 声明此边的状态（From 由声明者隐含，遍历时可知）。
    ///
    /// <b>「切换 / 叠加」不由边声明，由状态机运行时判定：</b>
    /// 目标状态无场景（<c>ScenePath</c> 为空）或与当前同场景 → 叠加（Push 语义）；
    /// 场景不同 → 切换（替换 + 场景加载）。可视化编辑器按两端场景推断并在连线上标注。
    ///
    /// 双重用途：
    /// <b>1. 可视化编辑器</b> —— 遍历各状态的 <c>GetEdges()</c> 生成完整连线图：
    ///     <c>TargetState</c> 是箭头指向的节点，<c>QuickSceneLoad</c> 在线上标注加载模式（快速/假进度），
    ///     <c>Label</c> 是箭头上的文字，<c>Condition</c> 不为空时边显示为不同颜色（受限边），
    ///     <c>ReadOnly</c> 边显示锁图标（框架内置边不可编辑结构）。
    ///
    /// <b>2. 运行时校验与流转</b> —— <see cref="EmberStateMachine.TransitionTo"/> 查找对应边，
    ///     如果有 <c>Guard</c> 且返回 false，则拒绝切换；随后按场景路径判定切换/叠加。
    ///
    /// <b>用法：</b>
    /// <code>
    /// public override TransitionDescriptor[] GetEdges() => new[]
    /// {
    ///     new TransitionDescriptor(typeof(MainState), "返回大厅")
    ///     {
    ///         QuickSceneLoad = true,   // 快速加载（切回 Main 时场景不同才生效）
    ///         ReadOnly = true,         // 框架内置边：仅可切换 QuickSceneLoad
    ///     },
    ///     new TransitionDescriptor(typeof(SettingsState), "设置") { ReadOnly = true }, // 无场景 → 自动叠加
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

        /// <summary>
        /// 该连接线的场景转场模式：true = 快速加载（Loading 跳过假进度，真实加载完成即就绪）。
        /// 由 <see cref="EmberStateMachine.TransitionTo"/> 在走此边且发生场景切换时读取生效；
        /// 可视化编辑器同步读取此字段，在连接线上标注模式（如「快速」/「进度」）。
        /// 叠加路径（目标无场景/同场景）不加载场景，此字段不生效。
        /// 可修改：即使用户不可编辑边本身（见 <see cref="ReadOnly"/>），也可以切换此开关。
        /// </summary>
        public bool QuickSceneLoad { get; set; }

        /// <summary>
        /// 只读标记：true = 框架内置边，用户不可增删此边、不可改 TargetState/Label/Condition，
        /// 仅可切换 <see cref="QuickSceneLoad"/>（快速/假进度）。
        /// 可视化编辑器对只读边显示锁图标并禁用结构编辑；运行时状态机不受影响。
        /// </summary>
        public bool ReadOnly { get; init; }

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
