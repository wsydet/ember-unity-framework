// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.UI
{
    /// <summary>
    /// UI 页面生命周期接口。
    /// 所有被 EmberUIManager 管理的页面必须实现此接口。
    ///
    /// <para>两阶段生命周期设计：</para>
    /// <list type="bullet">
    ///   <item><b>数据阶段</b>：Init / Cleanup —— 纯数据操作，无动画</item>
    ///   <item><b>表现阶段</b>：PlayShow / PlayHide —— 纯动画表现</item>
    /// </list>
    ///
    /// <para>完整流程：</para>
    /// <code>
    /// Push → Init(args) → PlayShow() → [Opened]
    ///     → OnPause()                   ← 被其他页面遮挡
    ///     → OnResume()                  ← 重新回到顶层
    ///     → OnReopen(args)              ← 已加载页面被重新打开
    ///     → PlayHide() → Cleanup()      ← 关闭
    /// </code>
    /// </summary>
    public interface IUIView
    {
        // ── 数据阶段 ──

        /// <summary>
        /// 初始化页面数据。在预制体实例化之后、PlayShow 之前调用。
        /// 只做数据操作：填文字、设图片、注册事件监听。不要在此做动画。
        /// </summary>
        /// <param name="args">打开时传入的参数</param>
        void Init(object args);

        /// <summary>
        /// 清理页面。在 PlayHide 完成之后调用。
        /// 注销事件、释放引用、归还池。之后 GameObject 会被销毁/回池。
        /// </summary>
        void Cleanup();

        // ── 表现阶段 ──

        /// <summary>
        /// 播放打开动画。由 EmberUIManager 通过协程驱动，
        /// 动画结束后 EmberUIManager 将页面标记为 Opened。
        /// 如果不需要动画，yield break 即可。
        /// </summary>
        void PlayShow();

        /// <summary>
        /// 播放关闭动画。由 EmberUIManager 通过协程驱动，
        /// 动画结束后 EmberUIManager 调 Cleanup() 并销毁。
        /// </summary>
        void PlayHide();

        // ── 栈操作回调 ──

        /// <summary>
        /// 另一个页面被 Push 到上方时调用。此页面不会被销毁，只是被遮挡。
        /// </summary>
        void OnPause();

        /// <summary>
        /// 上方页面被 Pop 后，此页面重新回到栈顶时调用。
        /// </summary>
        void OnResume();

        /// <summary>
        /// 已加载但之前被关闭的页面被重新打开时调用。
        /// 与 Init 互斥 —— 如果页面已加载，调 OnReopen 而非 Init。
        /// </summary>
        void OnReopen(object args);

        // ── 输入 ──

        /// <summary>
        /// 返回键处理。返回 true 表示已处理（阻止冒泡）。
        /// 由 EmberUIPageRouter 从 TopMost → Popup → MainPage 逐层询问。
        /// </summary>
        bool TryEscapeKeyClose();

        // ── 状态 ──

        /// <summary>页面是否已完成 Init（数据就绪）</summary>
        bool IsInitialized { get; }

        /// <summary>页面是否处于 Opened 状态</summary>
        bool IsOpened { get; }

        /// <summary>页面当前的生命周期状态</summary>
        PageState State { get; }
    }
}
