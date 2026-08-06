// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.UI
{
    /// <summary>
    /// UI 模块事件 Key 常量表。
    /// 用于 EmberEventBus 广播框架级 UI 事件（模块就绪、关闭等），
    /// 消费方不明确时使用 EmberEventBus；具体页面生命周期事件走 <see cref="EmberUIObserver"/>（UniRx）。
    ///
    /// <para>Key 区间：5000-5999（UI 模块专用，与 Core 的 1xxx 区间区隔）。</para>
    /// </summary>
    public static class EmberUIEvents
    {
        /// <summary>UI 框架层初始化就绪</summary>
        public const int UIManagerReady = 5000;

        /// <summary>UI 框架层即将销毁</summary>
        public const int UIManagerShutdown = 5001;

        /// <summary>UI 页面路由器就绪</summary>
        public const int UIPageRouterReady = 5002;

        // 区间 5010-5099 预留给更多框架级 UI 事件
    }
}
