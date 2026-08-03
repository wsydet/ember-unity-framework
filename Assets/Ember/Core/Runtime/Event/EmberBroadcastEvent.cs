namespace Ember.Core
{
    /// <summary>
    /// 框架广播事件的 int-key 常量表。
    ///
    /// 每个模块分配一个基址（间隔 1000），模块内事件 = 基址 + 偏移，
    /// 偏移从 1 开始，最多 999 个事件。
    ///
    /// 设计参考了 burner 项目的 <c>ModuleType</c> + <c>XxxEventDefine</c> 模式。
    ///
    /// 用法：
    /// <code>
    /// EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, OnResourceReady);
    /// EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);
    /// </code>
    /// </summary>
    public static class EmberBroadcastEvent
    {
        // ============================================================
        // 模块基址（间隔 1000）
        // ============================================================

        /// <summary>Core 模块基址，预留偏移 1～999</summary>
        public const int Core     = 1000;

        /// <summary>Resource 模块基址，预留偏移 1～999</summary>
        public const int Resource = 2000;

        /// <summary>UI 模块基址，预留偏移 1～999</summary>
        public const int UI       = 3000;

        /// <summary>Scene 模块基址，预留偏移 1～999</summary>
        public const int Scene    = 4000;

        /// <summary>Audio 模块基址，预留偏移 1～999</summary>
        public const int Audio    = 5000;

        /// <summary>Input 模块基址，预留偏移 1～999</summary>
        public const int Input    = 6000;

        /// <summary>业务层预留基址，起始 ID = 10000</summary>
        public const int Game     = 10000;

        // ============================================================
        // Core 模块事件（Core + 1 ~ Core + 999）
        // ============================================================

        /// <summary>Core 模块初始化完成</summary>
        public const int CoreReady    = Core + 1;

        /// <summary>Core 模块即将销毁</summary>
        public const int CoreShutdown = Core + 2;

        /// <summary>游戏状态发生切换（oldState → newState）</summary>
        public const int GameStateChanged = Core + 3;

        /// <summary>MainScene 预加载完成，启动动画脚本可以开始</summary>
        public const int InitSceneReady    = Core + 4;

        /// <summary>启动动画完成，InitState 可过渡到 MainState</summary>
        public const int InitAnimationDone = Core + 5;

        // ============================================================
        // Resource 模块事件（Resource + 1 ~ Resource + 999）
        // ============================================================

        /// <summary>Resource 模块初始化完成，资源系统可用</summary>
        public const int ResourceReady    = Resource + 1;

        /// <summary>Resource 模块即将销毁</summary>
        public const int ResourceShutdown = Resource + 2;

        // ============================================================
        // UI 模块事件（UI + 1 ~ UI + 999）
        // ============================================================

        /// <summary>UI 模块初始化完成</summary>
        public const int UIReady    = UI + 1;

        /// <summary>UI 模块即将销毁</summary>
        public const int UIShutdown = UI + 2;

        // ============================================================
        // Scene 模块事件（Scene + 1 ~ Scene + 999）
        // ============================================================

        /// <summary>场景加载完成</summary>
        public const int SceneLoaded     = Scene + 1;

        /// <summary>场景即将卸载</summary>
        public const int SceneUnloading  = Scene + 2;

        /// <summary>场景加载开始</summary>
        public const int SceneLoadStart  = Scene + 3;

        /// <summary>场景加载完成</summary>
        public const int SceneLoadDone   = Scene + 4;

        // ============================================================
        // Audio 模块事件（Audio + 1 ~ Audio + 999）
        // ============================================================

        /// <summary>Audio 模块初始化完成</summary>
        public const int AudioReady    = Audio + 1;

        /// <summary>Audio 模块即将销毁</summary>
        public const int AudioShutdown = Audio + 2;

        // ============================================================
        // Input 模块事件（Input + 1 ~ Input + 999）
        // ============================================================

        /// <summary>Input 模块初始化完成</summary>
        public const int InputReady    = Input + 1;

        /// <summary>Input 模块即将销毁</summary>
        public const int InputShutdown = Input + 2;
    }
}
