namespace Ember.Core
{
    /// <summary>
    /// 框架基础设施管理器接口 —— 应用启动即初始化的"管道"。
    ///
    /// <b>定位：框架管道，不是业务模块。</b>
    /// 实现此接口的类代表"无论什么游戏状态都需要在线"的基础服务
    /// （事件总线、资源加载、Update 驱动等）。它们在应用启动时由
    /// <see cref="EmberManagerCollector.InitializeAll"/> 统一初始化，
    /// 应用退出时逆序销毁。
    ///
    /// <b>业务模块请用 <see cref="IEmberModule"/>。</b>
    /// 业务模块的生命周期由 <see cref="EmberStateMachine"/> 驱动——
    /// 进入 BattleState 才初始化战斗模块，退出时销毁。
    /// 不要在业务模块上实现 IEmberManager，否则会随框架启动被误扫。
    ///
    /// 参考 burner 的 <c>IManager</c> 模式。
    ///
    /// 用法（仅框架层管道）：
    /// <code>
    /// [EmberInitOrder(EmberInitOrder.Resource)]
    /// public class EmberResourceManager : EmberSingleton&lt;EmberResourceManager&gt;, IEmberManager
    /// {
    ///     void IEmberManager.Init()   { /* 框架启动时调用 */ }
    ///     void IEmberManager.Destroy() { /* 框架退出时调用 */ }
    /// }
    /// </code>
    /// </summary>
    public interface IEmberManager
    {
        /// <summary>
        /// 初始化。由 EmberManagerCollector 在启动时按 InitOrder 顺序调用。
        /// </summary>
        void Init();

        /// <summary>
        /// 销毁。由 EmberManagerCollector 在程序退出时按 InitOrder 逆序调用。
        /// </summary>
        void Destroy();
    }
}
