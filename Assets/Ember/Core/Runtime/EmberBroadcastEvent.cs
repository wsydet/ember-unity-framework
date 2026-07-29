namespace Ember.Core
{
    /// <summary>
    /// 框架广播事件的 int-key 常量表。
    ///
    /// 每个模块分配一个基址（间隔 100），模块内事件 = 基址 + 偏移，
    /// 偏移从 1 开始，最多 99 个事件。
    ///
    /// 设计参考了 burner 项目的 <c>ModuleType</c> + <c>XxxEventDefine</c> 模式。
    ///
    /// 用法：
    /// <code>
    /// EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, OnResourceReady);
    /// EmberEventBus.Dispatch(EmberBroadcastEvent.ResourceReady);
    /// </code>
    /// </summary>
    public static class EmberBroadcastEvent
    {
        // ============================================================
        // 模块基址
        // ============================================================

        /// <summary>Core 模块基址，预留偏移 1～99</summary>
        public const int Core     = 100;

        /// <summary>Resource 模块基址，预留偏移 1～99</summary>
        public const int Resource = 200;

        /// <summary>UI 模块基址，预留偏移 1～99</summary>
        public const int UI       = 300;

        /// <summary>Scene 模块基址，预留偏移 1～99</summary>
        public const int Scene    = 400;

        /// <summary>Audio 模块基址，预留偏移 1～99</summary>
        public const int Audio    = 500;

        /// <summary>Input 模块基址，预留偏移 1～99</summary>
        public const int Input    = 600;

        /// <summary>
        /// 业务层预留基址，起始 ID = 1000。
        /// 业务层从 Game + 1 开始定义自己的广播事件。
        /// </summary>
        public const int Game     = 1000;

        // ============================================================
        // Core 模块事件（Core + 1 ~ Core + 99）
        // ============================================================

        /// <summary>Core 模块初始化完成</summary>
        public const int CoreReady    = Core + 1;

        /// <summary>Core 模块即将销毁，所有模块应在此事件中清理订阅</summary>
        public const int CoreShutdown = Core + 2;

        // ============================================================
        // Resource 模块事件（Resource + 1 ~ Resource + 99）
        // ============================================================

        /// <summary>Resource 模块初始化完成，资源系统可用</summary>
        public const int ResourceReady    = Resource + 1;

        /// <summary>Resource 模块即将销毁</summary>
        public const int ResourceShutdown = Resource + 2;

        // ============================================================
        // UI 模块事件（UI + 1 ~ UI + 99）
        // ============================================================

        /// <summary>UI 模块初始化完成</summary>
        public const int UIReady    = UI + 1;

        /// <summary>UI 模块即将销毁</summary>
        public const int UIShutdown = UI + 2;

        // ============================================================
        // Scene 模块事件（Scene + 1 ~ Scene + 99）
        // ============================================================

        /// <summary>场景加载完成</summary>
        public const int SceneLoaded     = Scene + 1;

        /// <summary>场景即将卸载</summary>
        public const int SceneUnloading  = Scene + 2;

        // ============================================================
        // Audio 模块事件（Audio + 1 ~ Audio + 99）
        // ============================================================

        /// <summary>Audio 模块初始化完成</summary>
        public const int AudioReady    = Audio + 1;

        /// <summary>Audio 模块即将销毁</summary>
        public const int AudioShutdown = Audio + 2;

        // ============================================================
        // Input 模块事件（Input + 1 ~ Input + 99）
        // ============================================================

        /// <summary>Input 模块初始化完成</summary>
        public const int InputReady    = Input + 1;

        /// <summary>Input 模块即将销毁</summary>
        public const int InputShutdown = Input + 2;
    }
}
