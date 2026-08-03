using System;

namespace Ember.Core
{
    /// <summary>
    /// 设置状态 —— 框架提供的通用覆盖式状态。
    ///
    /// 角色：暂停当前上下文（Main 或 Gameplay），以 Push 模式弹出设置面板，
    /// 关闭后自动恢复原状态。
    ///
    /// <b>使用方式：</b>
    /// <code>
    /// // 主界面打开设置
    /// Fsm.Push&lt;SettingsState&gt;(args: SettingsContext.Main);
    ///
    /// // 战斗中打开设置
    /// Fsm.Push&lt;SettingsState&gt;(args: SettingsContext.Gameplay);
    /// </code>
    ///
    /// <b>子类化指南：</b>
    /// - override <see cref="OnSettingsEnter"/>：根据 args 中的上下文展示不同 UI
    ///   （Main 上下文：音频/画质/操作/退出游戏；
    ///     Gameplay 上下文：音频/画质/操作/返回大厅/重新开始）
    /// - override <see cref="OnSettingsExit"/>：隐藏 UI
    ///
    /// <b>设计决策：</b>
    /// - 不是 Required 状态，用户可以 Unregister 后用自定义设置状态替代
    /// - 通过 <see cref="SettingsContext"/> 枚举传入上下文，而非创建多个 Settings 子类
    /// - 业务差异（Main vs Gameplay 的选项不同）由 UI 层根据 args 处理
    /// </summary>
    public class SettingsState : EmberGameState
    {
        private const string TAG = LogTags.CoreStateMachine;

        public override string Name => "Settings";
        public override string Description
            => "设置界面：暂停当前上下文并以覆盖模式打开设置面板。";
        public override bool IsRequired => false;

        #region 生命周期

        public sealed override void OnEnter(object args)
        {
            var context = args is SettingsContext ctx ? ctx : SettingsContext.Main;
            EmberDebug.Log(TAG, $"SettingsState: opened (context={context}).");
            OnSettingsEnter(args);
        }

        public sealed override void OnExit()
        {
            OnSettingsExit();
            EmberDebug.Log(TAG, "SettingsState: closed.");
        }

        #endregion

        // ============================================================

        #region 外部方法（子类可 override）

        /// <summary>
        /// 进入设置界面。args 为 <see cref="SettingsContext"/> 枚举值，
        /// 指示设置是从哪个上下文打开的（Main / Gameplay）。
        /// 子类根据上下文展示不同的设置选项。
        /// </summary>
        /// <param name="args"><see cref="SettingsContext"/> 值</param>
        protected virtual void OnSettingsEnter(object args) { }

        /// <summary>离开设置界面。子类在此隐藏 UI。</summary>
        protected virtual void OnSettingsExit() { }

        #endregion
    }

    /// <summary>
    /// Settings 打开时的上下文 —— 决定显示哪些设置选项。
    ///
    /// <b>典型差异：</b>
    /// - Main：音频 / 画质 / 操作 / 账号 / 退出游戏
    /// - Gameplay：音频 / 画质 / 操作 / 返回大厅 / 重新开始
    /// </summary>
    public enum SettingsContext
    {
        /// <summary>从主界面打开</summary>
        Main,

        /// <summary>从玩法中打开</summary>
        Gameplay,
    }
}
