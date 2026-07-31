namespace Ember.Core
{
    /// <summary>
    /// 系统初始化状态 —— 框架内置的必需状态。
    ///
    /// 游戏启动后首先进入此状态，在此阶段完成：
    /// - Manager 初始化（按 EmberInitOrder 排序依次 Init）
    /// - 资源系统就绪等待
    /// - 广播 CoreReady 事件
    ///
    /// 此状态为系统必需（IsRequired = true），不可从状态机中注销。
    /// 初始化完成后应 TransitionTo 到 Login 或 MainMenu。
    ///
    /// 参考 burner 的 InitProcedure：Manager 和 Module 初始化都在 Init 状态内完成，
    /// 而非在状态机启动之前。
    /// </summary>
    public class InitState : EmberGameState
    {
        public override string Name => "Init";
        public override string Description => "系统初始化：Manager 启动、资源就绪、框架自检。此状态为必需状态，不可删除。";
        public override bool IsRequired => true;

        public override void OnEnter(object args)
        {
            EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: bootstrapping framework...");

            // 反射扫描并初始化所有 IEmberManager（按 EmberInitOrder 排序）
            EmberManagerCollector.Instance.InitializeAll();

            // 框架管道已全部就绪，广播 CoreReady
            EmberEventBus.OnNext(EmberBroadcastEvent.CoreReady);

            EmberDebug.LogInit(LogTags.CoreStateMachine, "InitState: framework ready.");
        }
    }
}
