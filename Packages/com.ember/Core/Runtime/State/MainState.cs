using System;

using Cysharp.Threading.Tasks;

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
    /// - 主界面上的弹出层（设置、商城等）直接使用 <c>Fsm.TransitionTo</c>（目标无场景，自动判定为叠加）
    /// </summary>
    public class MainState : EmberGameState
    {
        private const string TAG = LogTags.CoreStateMachine;

        public override string Name => "Main";
        public override string Description
            => "主界面/大厅：选择玩法入口，退出 Gameplay 后的归宿。";
        public override bool IsRequired => true;
        public override string ScenePath => "MainScene";

        // 统一边声明（数据包，框架内置边只读）：→ Gameplay（场景切换，Loading 带假进度）；→ Settings（无场景，自动叠加）
        public override TransitionDescriptor[] GetEdges() => new TransitionDescriptor[]
        {
            new(typeof(GameplayState), "进入玩法") { QuickSceneLoad = false, ReadOnly = true },
            new(typeof(SettingsState), "设置") { ReadOnly = true },
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

        public sealed override UniTask PrepareEnterAsync()
        {
            // 简单模式：进入 Main 前先加载兜底背景页。
            // 由 SceneCoordinator 在场景加载完成后、OnEnter 前 await，
            // 保证 Loading 页渐出时背景已在底层（启动路径由 InitState 提前调用 LoadBackgroundAsync）。
            return UseUIBg ? LoadBackgroundAsync() : UniTask.CompletedTask;
        }

        #endregion

        // ============================================================

        #region 外部方法（子类可 override）

        /// <summary>
        /// 是否使用兜底背景页（UIBg）。默认 true —— 简单模式：主界面 = MainUI + UIBg。
        /// 复杂模式（如奥日式带场景的开屏效果）在子类 override 为 false，
        /// 由场景 / 开屏动画本身提供视觉，不额外加载 UIBg。
        /// 纯代码级开关，不暴露在 Inspector，仅供子类 override。
        /// </summary>
        protected internal virtual bool UseUIBg => true;

        /// <summary>
        /// 加载兜底背景页。仅当 <see cref="UseUIBg"/> 为 true 时被调用：
        /// - 启动路径：由 InitState 在 BootSplash 渐出前调用
        /// - 转场路径：由 <see cref="PrepareEnterAsync"/>（SceneCoordinator）在 Loading 页渐出前调用
        /// 框架默认不加载（返回 CompletedTask）；业务层 override 此方法加载自己的背景页。
        /// </summary>
        protected internal virtual UniTask LoadBackgroundAsync() => UniTask.CompletedTask;

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
