using System;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 核心玩法状态 —— 框架提供的核心状态之一。
    ///
    /// 角色：承载游戏的核心玩法循环。从 Main 进入，退出后回到 Main。
    ///
    /// <b>生命周期：</b>
    /// <code>
    /// Main ──→ Gameplay（进入玩法）
    ///            │
    ///            ├── Push: PauseMenu / Inventory...（弹窗式覆盖，暂停玩法）
    ///            └── Pop:  恢复玩法
    ///            │
    ///            └── TransitionTo&lt;MainState&gt;（退出玩法，回到大厅）
    /// </code>
    ///
    /// <b>子类化指南：</b>
    /// - 进入玩法时自动 <c>InitPhase(ModulePhase.Gameplay)</c> 初始化玩法阶段业务模块
    /// - override <see cref="OnGameplayEnter"/>：加载战斗场景、初始化战斗模块
    /// - override <see cref="OnGameplayExit"/>：卸载战斗场景、清理战斗模块
    /// - override <see cref="OnGameplayUpdate"/>：驱动玩法主循环（如不需要可留空）
    /// - override <see cref="OnGameplayPause"/>：暂停逻辑（被弹出窗口覆盖时）
    /// - override <see cref="OnGameplayResume"/>：恢复逻辑（弹出窗口关闭后）
    ///
    /// <b>注意：</b>
    /// - 本状态的 OnEnter/OnExit/OnUpdate/OnPause/OnResume 已包含日志，
    ///   子类应 override OnGameplayXxx 系列方法
    /// - 游戏内弹出层（暂停菜单、背包等）请使用 <c>Fsm.Push</c>
    /// - 退出玩法请 <c>GameLauncher.Instance.Fsm.TransitionTo&lt;MainState&gt;()</c>
    /// </summary>
    public class GameplayState : EmberGameState
    {
        private const string TAG = LogTags.CoreStateMachine;

        public override string Name => "Gameplay";
        public override string Description
            => "核心玩法：游戏主循环。退出时回到 Main。";
        public override bool IsRequired => true;
        public override string ScenePath => "GameplayScene";

        // Gameplay ──→ Main（单向，无条件）
        public override TransitionDescriptor[] GetTransitions() => new TransitionDescriptor[]
        {
            new(typeof(MainState), "返回大厅"),
        };

        // Gameplay - - → Settings（覆盖式，无条件）
        public override TransitionDescriptor[] GetPushTargets() => new TransitionDescriptor[]
        {
            new(typeof(SettingsState), "设置"),
        };

        #region 生命周期（密封 —— 子类 override OnGameplayXxx）

        public sealed override void OnEnter(object args)
        {
            EmberDebug.LogEvent(TAG, "GameplayState: entering gameplay...");
            EmberModuleCollector.Instance.InitPhase(ModulePhase.Gameplay);
            OnGameplayEnter(args);
        }

        public sealed override void OnExit()
        {
            OnGameplayExit();
            EmberModuleCollector.Instance.DestroyPhase(ModulePhase.Gameplay);
            EmberDebug.LogCleanup(TAG, "GameplayState: leaving gameplay.");
        }

        public sealed override void OnUpdate()
        {
            OnGameplayUpdate();
        }

        public sealed override void OnPause()
        {
            EmberDebug.Log(TAG, "GameplayState: paused (overlay on top).");
            OnGameplayPause();
        }

        public sealed override void OnResume()
        {
            EmberDebug.Log(TAG, "GameplayState: resumed.");
            OnGameplayResume();
        }

        #endregion

        // ============================================================

        #region 外部方法（子类可 override）

        /// <summary>进入玩法。子类 override 来加载场景、初始化游戏模块。</summary>
        protected virtual void OnGameplayEnter(object args) { }

        /// <summary>退出玩法。子类 override 来卸载场景、清理游戏模块。</summary>
        protected virtual void OnGameplayExit() { }

        /// <summary>每帧驱动。子类 override 来运行玩法主循环。</summary>
        protected virtual void OnGameplayUpdate() { }

        /// <summary>被覆盖时暂停。子类 override 来暂停游戏逻辑。</summary>
        protected virtual void OnGameplayPause() { }

        /// <summary>覆盖层关闭后恢复。子类 override 来恢复游戏逻辑。</summary>
        protected virtual void OnGameplayResume() { }

        #endregion
    }
}
