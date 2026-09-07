namespace Ember.Core
{
    /// <summary>
    /// 业务模块接口 —— 生命周期由状态机驱动，与 <see cref="IEmberManager"/> 平行。
    ///
    /// <b>定位：可选、可组合的业务积木。</b>
    /// 与 <see cref="IEmberManager"/>（框架管道，启动即初始化）不同，
    /// IEmberModule 代表“只在部分游戏或部分状态下才需要”的业务模块
    /// （玩家控制、战斗系统、场景 UI 等）。模板可以不包含某个 Module，
    /// 也可以用 <see cref="EmberModuleAttribute.Enabled"/> 关闭装配；不同游戏通过
    /// 在同一套框架和 Managers 上组合不同 Modules 形成不同玩法。
    ///
    /// 模块必须通过 <see cref="EmberModuleAttribute"/> 声明阶段和启用状态。
    /// Enabled 决定启动扫描时是否装配；Phase 决定已装配模块何时激活。
    /// <b>发现与初始化由 <c>EmberModuleCollector</c> 分两阶段驱动：</b>
    /// - 框架启动：发现并构造全部启用模块，但不调用 OnInit
    /// - Phase 0：框架管道（保留，IEmberManager 覆盖）
    /// - Phase 1：全局业务（Login 后常驻，如网络、账号）
    /// - Phase 2+：场景内业务（进入具体玩法时初始化）
    ///
    /// 状态机在 TransitionTo/Exit 时自动管理对应 Phase 模块的生命周期：
    /// - 进入新状态 → Phase 匹配的模块调用 <see cref="OnInit"/>
    /// - 退出旧状态 → Phase 匹配的模块调用 <see cref="OnDestroy"/>
    ///
    /// 用法（模块以单例形式存在，继承 EmberSingleton&lt;T&gt;，生命周期方法显式实现接口）：
    /// <code>
    /// [EmberModule(ModulePhase.Gameplay)]
    /// public class BattleModule : EmberSingleton&lt;BattleModule&gt;, IEmberModule
    /// {
    ///     void IEmberModule.OnInit()   { /* 加载战斗资源、注册事件 */ }
    ///     void IEmberModule.OnDestroy() { /* 卸载战斗资源、注销事件 */ }
    ///
    ///     void IEmberModule.ResetModuleData()
    ///     {
    ///         // 清空运行时数据（不重建对象），用于"返回主菜单 → 重新进入"场景
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IEmberModule
    {
        /// <summary>
        /// 模块初始化。实例已在框架启动时被发现；状态机进入对应 Phase 后才调用本方法。
        /// </summary>
        void OnInit();

        /// <summary>
        /// 模块销毁。由 EmberModuleCollector 在状态机退出对应 Phase 时调用。
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// 热重启数据：清空运行时状态，保留对象引用。
        ///
        /// 场景：玩家从 Battle 返回 MainMenu，再进入 Battle。
        /// 在这种情况下，Module 对象本身不销毁重建，
        /// 只调用 ResetModuleData() 清空内部数据（部队位置、技能冷却等），
        /// 下次 OnInit 时就像第一次初始化一样。
        ///
        /// 如果业务模块不需要热重启能力，保持空实现即可。
        /// </summary>
        void ResetModuleData();
    }
}
