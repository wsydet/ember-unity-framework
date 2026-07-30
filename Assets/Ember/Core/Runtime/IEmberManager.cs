namespace Ember.Core
{
    /// <summary>
    /// 管理器接口 —— 所有框架级和项目级管理器的统一契约。
    ///
    /// 实现此接口 + 继承 <see cref="EmberSingleton{T}"/> 或
    /// <see cref="EmberMonoSingleton{T}"/> 的类，会被
    /// <see cref="EmberManagerCollector"/> 自动发现并初始化。
    ///
    /// 参考 burner 的 <c>IManager</c> 模式。
    ///
    /// 用法：
    /// <code>
    /// [EmberInitOrder(100)]
    /// public class MyManager : EmberSingleton&lt;MyManager&gt;, IEmberManager
    /// {
    ///     public void Init() { ... }
    ///     public void Destroy() { ... }
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
