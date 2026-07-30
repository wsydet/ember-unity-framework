namespace Ember.Core
{
    /// <summary>
    /// 系统初始化状态 —— 框架内置的必需状态。
    ///
    /// 游戏启动后首先进入此状态，在此阶段完成：
    /// - Manager 初始化（EmberManagerCollector.InitializeAll()）
    /// - 资源系统就绪等待
    /// - 任何启动时必须完成的准备工作
    ///
    /// 此状态为系统必需（IsRequired = true），不可从状态机中注销。
    /// 初始化完成后切换到 Login 或 MainMenu。
    /// </summary>
    public class InitState : EmberGameState
    {
        public override string Name => "Init";
        public override string Description => "系统初始化：Manager 启动、资源就绪、框架自检。此状态为必需状态，不可删除。";
        public override bool IsRequired => true;
    }
}
