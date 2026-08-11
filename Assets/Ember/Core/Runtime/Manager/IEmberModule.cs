namespace Ember.Core
{
    /// <summary>
    /// 业务模块接口 —— 生命周期由状态机驱动，与 <see cref="IEmberManager"/> 平行。
    ///
    /// <b>定位：业务层模块。</b>
    /// 与 <see cref="IEmberManager"/>（框架管道，启动即初始化）不同，
    /// IEmberModule 代表"只在某些游戏状态下才需要"的业务模块
    /// （战斗系统、网络连接、背包逻辑等）。
    ///
    /// <b>初始化由 <c>EmberModuleCollector</c>（未来实现）按阶段驱动：</b>
    /// - Phase 0：框架管道（保留，IEmberManager 覆盖）
    /// - Phase 1：全局业务（Login 后常驻，如网络、账号）
    /// - Phase 2+：场景内业务（进入具体玩法时初始化）
    ///
    /// 状态机在 TransitionTo/Exit 时自动管理对应 Phase 模块的生命周期：
    /// - 进入新状态 → Phase 匹配的模块调用 <see cref="OnInit"/>
    /// - 退出旧状态 → Phase 匹配的模块调用 <see cref="OnDestroy"/>
    ///
    /// 用法：
    /// <code>
    /// public class BattleModule : IEmberModule
    /// {
    ///     public int Phase => 2;
    ///
    ///     public void OnInit()   { /* 加载战斗资源、注册事件 */ }
    ///     public void OnDestroy() { /* 卸载战斗资源、注销事件 */ }
    ///
    ///     public void ResetModuleData()
    ///     {
    ///         // 清空运行时数据（不重建对象），用于"返回主菜单 → 重新进入"场景
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IEmberModule
    {
        /// <summary>
        /// 初始化阶段。值越大，在越晚的游戏状态下初始化。
        /// Phase 0 保留给框架管道（由 IEmberManager 覆盖），业务模块从 Phase 1 开始。
        /// </summary>
        int Phase { get; }

        /// <summary>
        /// 模块初始化。由 EmberModuleCollector 在状态机进入对应 Phase 时调用。
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
