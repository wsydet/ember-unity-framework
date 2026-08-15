using Cysharp.Threading.Tasks;

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
    /// BootSplash 在 FrameworkScene 启动时自动激活。
    /// MainScene 加载完成后先 await 黑幕淡出（见 EmberBootSplashBridge），
    /// 黑幕完全消失后才 TransitionTo《MainState》。
    ///
    /// <b>开屏动画归 MainState 持有</b>：
    /// 进入 MainState.OnEnter 后播报 MainSceneReady，
    /// EUIMainAnimationStarter 响应并播放动画。
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
                fsm.LoadSceneAsync?.Invoke("MainScene", async () =>
                {
                    EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: MainScene loaded. Waiting for BootSplash to fade out...");

                    // 串行时序：等 BootSplash 完全淡出（黑幕消失）后才 TransitionTo，
                    // 保证开屏动画 / MainUI 进入动画在 BootSplash 消失之后才开始。
                    // 若业务层未挂 BootSplash（桥接为 null），则跳过等待立即 TransitionTo。
                    if (EmberBootSplashBridge.WaitForFadeOut != null)
                        await EmberBootSplashBridge.WaitForFadeOut();

                    EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: transitioning to Main...");
                    fsm.TransitionTo<MainState>(skipSceneLoad: true);
                });
            }
        }
    }
}
