// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using UniRx;

namespace Ember.UI
{
    /// <summary>
    /// UI 生命周期事件的可观测门面。
    ///
    /// 底层为 UniRx Subject《T》，EUIViewEngine 在页面生命周期各节点推事件，
    /// 业务模块通过静态属性订阅感兴趣的数据流。
    ///
    /// <para>与 EmberEventBus 的分工：</para>
    /// <list type="bullet">
    ///   <item><b>EmberEventBus</b>：框架级广播（UIReady / UIShutdown），消费方不确定</item>
    ///   <item><b>EUIObserver</b>：页面生命周期事件，类型安全 + 操作符（Where / Throttle）</item>
    /// </list>
    ///
    /// <para>使用示例：</para>
    /// <code>
    /// EUIObserver.OnPageOpened
    ///     .Where(e =&gt; e.Page.PageType == PageType.Popup
    ///         || e.Page.PageType == PageType.FullScreenPopup)
    ///     .Subscribe(e =&gt; audioMgr.PlaySFX("popup_open"))
    ///     .AddTo(this);
    /// </code>
    /// </summary>
    public static class EUIObserver
    {
        #region 内部参数

        private static readonly Subject<PageLifecycleEvent> _onPageOpened   = new Subject<PageLifecycleEvent>();
        private static readonly Subject<PageLifecycleEvent> _onPageClosed   = new Subject<PageLifecycleEvent>();
        private static readonly Subject<PageLifecycleEvent> _onPagePaused   = new Subject<PageLifecycleEvent>();
        private static readonly Subject<PageLifecycleEvent> _onPageResumed  = new Subject<PageLifecycleEvent>();
        private static readonly Subject<PageLifecycleEvent> _onPageReopened = new Subject<PageLifecycleEvent>();
        private static readonly Subject<PageLifecycleEvent> _onPageLoadStarted = new Subject<PageLifecycleEvent>();
        private static readonly Subject<Unit>              _onAllClosed     = new Subject<Unit>();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>页面完成 Init + PlayShow，进入 Opened 状态</summary>
        public static IObservable<PageLifecycleEvent> OnPageOpened   => _onPageOpened;

        /// <summary>页面完成 PlayHide + Cleanup</summary>
        public static IObservable<PageLifecycleEvent> OnPageClosed   => _onPageClosed;

        /// <summary>页面被上方页面遮挡（OnPause）</summary>
        public static IObservable<PageLifecycleEvent> OnPagePaused   => _onPagePaused;

        /// <summary>页面重新回到栈顶（OnResume）</summary>
        public static IObservable<PageLifecycleEvent> OnPageResumed  => _onPageResumed;

        /// <summary>已加载页面被重新打开（OnReopen）</summary>
        public static IObservable<PageLifecycleEvent> OnPageReopened => _onPageReopened;

        /// <summary>页面开始加载（Prefab 异步加载发起时，先于 OnBeginLoad）</summary>
        public static IObservable<PageLifecycleEvent> OnPageLoadStarted => _onPageLoadStarted;

        /// <summary>所有页面被关闭</summary>
        public static IObservable<Unit>              OnAllClosed     => _onAllClosed;

        #endregion

        // --------------------------------------------------------

        #region 内部方法（由 EUIViewEngine 调用）

        internal static void NotifyOpened(EUIPageDef pageDef, object args)
        {
            _onPageOpened.OnNext(new PageLifecycleEvent(pageDef, args));
        }

        internal static void NotifyClosed(EUIPageDef pageDef, object args)
        {
            _onPageClosed.OnNext(new PageLifecycleEvent(pageDef, args));
        }

        internal static void NotifyPaused(EUIPageDef pageDef)
        {
            _onPagePaused.OnNext(new PageLifecycleEvent(pageDef, null));
        }

        internal static void NotifyResumed(EUIPageDef pageDef)
        {
            _onPageResumed.OnNext(new PageLifecycleEvent(pageDef, null));
        }

        internal static void NotifyReopened(EUIPageDef pageDef, object args)
        {
            _onPageReopened.OnNext(new PageLifecycleEvent(pageDef, args));
        }

        internal static void NotifyLoadStarted(EUIPageDef pageDef)
        {
            _onPageLoadStarted.OnNext(new PageLifecycleEvent(pageDef, null));
        }

        internal static void NotifyAllClosed()
        {
            _onAllClosed.OnNext(Unit.Default);
        }

        #endregion
    }

    /// <summary>
    /// 页面生命周期事件。
    /// </summary>
    public struct PageLifecycleEvent
    {
        /// <summary>触发事件的页面定义</summary>
        public EUIPageDef Page;

        /// <summary>携带的参数（OnPause/OnResume 时为 null）</summary>
        public object Args;

        public PageLifecycleEvent(EUIPageDef page, object args)
        {
            Page = page;
            Args = args;
        }

        public override string ToString()
        {
            return $"PageEvent({Page})";
        }
    }
}
