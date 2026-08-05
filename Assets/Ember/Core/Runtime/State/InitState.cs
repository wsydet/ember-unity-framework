using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 系统初始化状态 —— 框架内置的必需状态。
    ///
    /// 启动流程：
    /// 1. 初始化所有 Manager
    /// 2. 广播 CoreReady
    /// 3. 预加载 MainScene
    /// 4. MainScene 就绪 → 广播 <see cref="EmberBroadcastEvent.InitSceneReady"/>
    ///    → MainScene 上的动画脚本接管（默认立即完成）
    /// 5. 收到 <see cref="EmberBroadcastEvent.InitAnimationDone"/>
    ///    → TransitionTo&lt;MainState&gt;
    ///
    /// <b>自定义启动动画：</b>
    /// 继承 EmberInitAnimationStarter，override PlayStartupAnimation，完成后调用 onComplete。
    ///
    /// 此状态为系统必需（IsRequired = true），不可从状态机中注销。
    /// </summary>
    public class InitState : EmberGameState
    {
        public override string Name => "Init";
        public override string Description
            => "系统初始化：Manager 启动、资源就绪、预加载 MainScene。";
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

            EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: framework ready. Preloading MainScene...");

            if (args is EmberStateMachine fsm)
            {
                fsm.LoadSceneAsync?.Invoke("MainScene", () =>
                {
                    EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: MainScene loaded. Firing InitSceneReady...");
                    EmberEventBus.OnNext(EmberBroadcastEvent.InitSceneReady);

                    EmberEventBus.Subscribe(EmberBroadcastEvent.InitAnimationDone, OnAnimationDone);

                    void OnAnimationDone()
                    {
                        EmberEventBus.Unsubscribe(EmberBroadcastEvent.InitAnimationDone, OnAnimationDone);
                        EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: animation done. Transitioning to Main...");
                        fsm.TransitionTo<MainState>(skipSceneLoad: true);
                    }
                });
            }
        }
    }
}
