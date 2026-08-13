using System;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 主界面 / 大厅状态 —— 框架提供的核心状态之一。
    ///
    /// 角色：Init 完成后的默认着陆点，也是退出 Gameplay 后的归宿。
    ///
    /// <b>生命周期：</b>
    /// <code>
    /// Init ──→ Main ←── Gameplay（退出后回到这里）
    ///            │
    ///            └──→ Push: Settings / Shop / Inventory...（弹窗式覆盖）
    /// </code>
    ///
    /// <b>开屏动画（MainState 持有）：</b>
    /// OnEnter → Subscribe(OpeningAnimationEnd) → OnNext(MainSceneReady)
    ///   → EUIMainAnimationStarter 播放动画
    ///   → 动画结束 → OpeningAnimationEnd → OnOpeningAnimationEnd()
    ///
    /// <b>子类化指南：</b>
    /// - override <see cref="OnMainEnter"/>：显示主界面 UI、播放背景音乐
    /// - override <see cref="OnOpeningAnimationEnd"/>：开屏动画结束后打开首页
    /// - override <see cref="OnMainExit"/>：隐藏主界面 UI、清理资源
    ///
    /// <b>注意：</b>
    /// - 本状态的 OnEnter/OnExit 已包含日志和事件广播，子类不要 override 它们，
    ///   而是 override OnMainEnter/OnMainExit/OnOpeningAnimationEnd
    /// - 主界面上的弹出层（设置、商城等）请使用 <c>Fsm.Push</c>，不要 TransitionTo
    /// </summary>
    public class MainState : EmberGameState
    {
        private const string TAG = LogTags.CoreStateMachine;

        public override string Name => "Main";
        public override string Description
            => "主界面/大厅：选择玩法入口，退出 Gameplay 后的归宿。";
        public override bool IsRequired => true;
        public override string ScenePath => "MainScene";

        // Main ──→ Gameplay（双向，无条件）
        public override TransitionDescriptor[] GetTransitions() => new TransitionDescriptor[]
        {
            new(typeof(GameplayState), "进入玩法"),
        };

        // Main - - → Settings（覆盖式，无条件）
        public override TransitionDescriptor[] GetPushTargets() => new TransitionDescriptor[]
        {
            new(typeof(SettingsState), "设置"),
        };

        #region 生命周期（密封 —— 子类 override OnMainEnter / OnOpeningAnimationEnd）

        public sealed override void OnEnter(object args)
        {
            EmberDebug.LogInit(TAG, "MainState: entering lobby...");
            EmberEventBus.OnNext(EmberBroadcastEvent.SceneLoaded); // 语义复用：主界面已就绪
            OnMainEnter(args);

            // ── 开屏动画事件链（先订阅再广播，与 InitState 模式对称）──
            EmberEventBus.Subscribe(EmberBroadcastEvent.OpeningAnimationEnd, OnAnimEnd);
            EmberEventBus.OnNext(EmberBroadcastEvent.MainSceneReady);

            void OnAnimEnd()
            {
                EmberEventBus.Unsubscribe(EmberBroadcastEvent.OpeningAnimationEnd, OnAnimEnd);
                EmberDebug.LogEvent(TAG, "MainState: opening animation end.");
                OnOpeningAnimationEnd();
            }
        }

        public sealed override void OnExit()
        {
            OnMainExit();
            EmberDebug.LogCleanup(TAG, "MainState: leaving lobby.");
        }

        #endregion

        // ============================================================

        #region 外部方法（子类可 override）

        /// <summary>
        /// 进入主界面。子类 override 此方法来显示 UI、播放 BGM 等。
        /// </summary>
        protected virtual void OnMainEnter(object args) { }

        /// <summary>
        /// 开屏动画结束。子类 override 此方法来打开首页。
        /// 此时 MainScene 已就绪，动画已播放完毕。
        /// </summary>
        protected virtual void OnOpeningAnimationEnd() { }

        /// <summary>
        /// 离开主界面。子类 override 此方法来隐藏 UI。
        /// </summary>
        protected virtual void OnMainExit() { }

        #endregion
    }
}
