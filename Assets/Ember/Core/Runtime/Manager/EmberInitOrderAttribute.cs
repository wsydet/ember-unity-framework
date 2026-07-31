using System;

namespace Ember.Core
{
    /// <summary>
    /// 管理器初始化顺序特性。
    ///
    /// 标注在实现 <see cref="IEmberManager"/> 的类上，指定初始化优先级。
    /// 值越小越先初始化，销毁时逆序（值大的先销毁）。
    ///
    /// 参考 burner 的 <c>InitOrderAttribute</c>。
    ///
    /// 预定义常量供参考：
    /// <code>
    /// [EmberInitOrder(EmberInitOrder.Core)]        // Core 层最先（EventBus、ServiceLocator）
    /// [EmberInitOrder(EmberInitOrder.Resource)]    // 资源系统
    /// [EmberInitOrder(EmberInitOrder.Audio)]       // 音频
    /// [EmberInitOrder(EmberInitOrder.Input)]       // 输入
    /// [EmberInitOrder(EmberInitOrder.UI)]          // UI
    /// [EmberInitOrder(EmberInitOrder.Scene)]       // 场景
    /// [EmberInitOrder(EmberInitOrder.Default)]     // 默认值（未标注时的 fallback）
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EmberInitOrderAttribute : Attribute
    {
        // ============================================================
        // 预定义优先级常量
        // ============================================================

        /// <summary>Core 基础设施（EventBus、ServiceLocator），最早初始化</summary>
        public const int Core     = 100;

        /// <summary>资源系统</summary>
        public const int Resource = 200;

        /// <summary>音频系统</summary>
        public const int Audio    = 300;

        /// <summary>输入系统</summary>
        public const int Input    = 400;

        /// <summary>UI 系统</summary>
        public const int UI       = 500;

        /// <summary>场景系统</summary>
        public const int Scene    = 600;

        /// <summary>业务层最低优先级</summary>
        public const int Game     = 700;

        /// <summary>默认值（未标注时使用）</summary>
        public const int Default  = 1000;

        // ============================================================

        /// <summary>初始化顺序值，越小越先初始化</summary>
        public int Order { get; }

        /// <summary>
        /// 指定初始化顺序。
        /// </summary>
        /// <param name="order">越小越先初始化，建议使用预定义常量</param>
        public EmberInitOrderAttribute(int order = Default)
        {
            Order = order;
        }
    }
}
