// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.UI
{
    /// <summary>
    /// UI 模块事件 Key 常量表。
    /// 用于 EmberEventBus 广播框架级 UI 事件（模块就绪、关闭等），
    /// 消费方不明确时使用 EmberEventBus；具体页面生命周期事件走 <see cref="EUIObserver"/>（UniRx）。
    ///
    /// <para>Key 区间：5000-5999（UI 模块专用，与 Core 的 1xxx 区间区隔）。</para>
    /// </summary>
    public static class EUIEvents
    {
        /// <summary>UI 视图引擎就绪</summary>
        public const int UIViewEngineReady = 5000;

        /// <summary>UI 视图引擎即将销毁</summary>
        public const int UIViewEngineShutdown = 5001;

        /// <summary>UI 管理器就绪</summary>
        public const int UIManagerReady = 5002;

        // 区间 5010-5099 预留给更多框架级 UI 事件

        /// <summary>Loading 页面渐入开始</summary>
        public const int LoadingFadeInStart    = 5010;

        /// <summary>Loading 页面渐入完成（进度条可见，可开始加载）</summary>
        public const int LoadingFadeInComplete = 5011;

        /// <summary>Loading 页面渐出开始（假进度到 100%，开始关闭动画）</summary>
        public const int LoadingFadeOutStart   = 5012;

        /// <summary>Loading 页面渐出完成（loading 完全关闭，可继续后续操作）</summary>
        public const int LoadingFadeOutComplete = 5013;
    }
}
