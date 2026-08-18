namespace Ember.Core
{
    /// <summary>
    /// 业务模块初始化阶段常量。
    ///
    /// Phase 0 保留给框架管道（由 <see cref="IEmberManager"/> 覆盖），业务模块从 Phase 1 开始。
    /// 值越大，在越晚的游戏状态下初始化。阶段与顶层状态机的对应关系：
    ///
    /// <list type="bullet">
    ///   <item><see cref="Framework"/> (0) — 框架管道，IEmberManager 专用</item>
    ///   <item><see cref="Global"/> (1) — Init 状态启动，常驻到游戏退出（玩家偏好、账号、网络）</item>
    ///   <item><see cref="Main"/> (2) — Main 状态（大厅业务）</item>
    ///   <item><see cref="Gameplay"/> (3) — Gameplay 状态（玩法业务）</item>
    /// </list>
    ///
    /// 业务模块通过 <c>public int Phase => ModulePhase.Global;</c> 声明所属阶段，
    /// 由 <see cref="EmberModuleCollector"/> 在状态机进入对应状态时自动初始化。
    /// </summary>
    public static class ModulePhase
    {
        /// <summary>框架管道（保留给 IEmberManager，业务模块不得使用）</summary>
        public const int Framework = 0;

        /// <summary>全局业务：Init 状态启动，常驻到游戏退出</summary>
        public const int Global = 1;

        /// <summary>大厅业务：Main 状态</summary>
        public const int Main = 2;

        /// <summary>玩法业务：Gameplay 状态</summary>
        public const int Gameplay = 3;
    }
}
