using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 系统初始化状态 —— 框架内置的必需状态。
    ///
    /// 启动流程：
    /// 1. 初始化所有 Manager
    /// 2. 广播 CoreReady
    /// 3. 加载 MainScene → TransitionTo&lt;MainState&gt;
    ///
    /// <b>InitState 持有 BootSplash（黑幕）</b>：
    /// BootSplash 在 FrameworkScene 启动时自动激活，
    /// Init 退出时（SceneLoadDone 后）自动关闭销毁。
    ///
    /// <b>开屏动画归 MainState 持有</b>：
    /// 进入 MainState.OnEnter 后播报 MainSceneReady，
    /// EmberMainAnimationStarter 响应并播放动画。
    ///
    /// 此状态为系统必需（IsRequired = true），不可从状态机中注销。
    /// </summary>
    public class InitState : EmberGameState
    {
        public override string Name => "Init";
        public override string Description
            => "系统初始化：Manager 启动、资源就绪、加载 MainScene。";
        public override bool IsRequired => true;
        public override string ScenePath => "";

        public override TransitionDescriptor[] GetTransitions() => new TransitionDescriptor[]
        {
            new(typeof(MainState), "初始化完成"),
        };

        public override void OnEnter(object args)
        {
            EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: bootstrapping framework...");

            EmberManagerCollector.Instance.InitializeAll();
            EmberEventBus.OnNext(EmberBroadcastEvent.CoreReady);

            EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: framework ready. Loading MainScene...");

            if (args is EmberStateMachine fsm)
            {
                fsm.LoadSceneAsync?.Invoke("MainScene", () =>
                {
                    EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: MainScene loaded, transitioning to Main...");
                    fsm.TransitionTo<MainState>(skipSceneLoad: true);
                });
            }
        }
    }
}
