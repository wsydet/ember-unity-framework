using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 事件组 —— 批量管理 <see cref="EmberEventBus"/> 订阅，一键清理。
    ///
    /// 典型用法：一个 UI 页面或模块在初始化时订阅多个事件，
    /// 退出时只需 Dispose 此 Group，无需逐个管理每个 IDisposable。
    ///
    /// 用法：
    /// <code>
    /// private readonly EmberEventGroup _events = new();
    ///
    /// void Start()
    /// {
    ///     _events.Add(EmberBroadcastEvent.SceneLoaded, OnSceneLoaded);
    ///     _events.Add(MyEvents.ItemAcquired, (int itemId) => UpdateInventory(itemId));
    /// }
    ///
    /// void OnDestroy()
    /// {
    ///     _events.Dispose();  // 一键清理所有订阅
    /// }
    /// </code>
    /// </summary>
    public sealed class EmberEventGroup : IDisposable
    {
        #region 内部参数

        private readonly List<IDisposable> _subs = new();
        private bool _disposed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>订阅无参事件。等效于 EmberEventBus.Subscribe。</summary>
        [NoGC]
        public void Add(int eventKey, Action handler)
        {
            if (_disposed || handler == null) return;
            _subs.Add(EmberEventBus.Subscribe(eventKey, handler));
        }

        /// <summary>订阅 1 参事件。等效于 EmberEventBus.Subscribe。</summary>
        [NoGC]
        public void Add<T>(int eventKey, Action<T> handler)
        {
            if (_disposed || handler == null) return;
            _subs.Add(EmberEventBus.Subscribe(eventKey, handler));
        }

        /// <summary>订阅 2 参事件。等效于 EmberEventBus.Subscribe。</summary>
        [NoGC]
        public void Add<T1, T2>(int eventKey, Action<T1, T2> handler)
        {
            if (_disposed || handler == null) return;
            _subs.Add(EmberEventBus.Subscribe(eventKey, handler));
        }

        /// <summary>订阅 3 参事件。等效于 EmberEventBus.Subscribe。</summary>
        [NoGC]
        public void Add<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
        {
            if (_disposed || handler == null) return;
            _subs.Add(EmberEventBus.Subscribe(eventKey, handler));
        }

        /// <summary>订阅 4 参事件。等效于 EmberEventBus.Subscribe。</summary>
        [NoGC]
        public void Add<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
        {
            if (_disposed || handler == null) return;
            _subs.Add(EmberEventBus.Subscribe(eventKey, handler));
        }

        /// <summary>
        /// 清空所有由本 Group 管理的订阅。
        /// 调用后 Group 仍可复用（继续 Add 新的订阅）。
        /// </summary>
        [NoGC]
        public void Clear()
        {
            foreach (var sub in _subs)
                sub.Dispose();
            _subs.Clear();
        }

        /// <summary>
        /// 清空所有订阅并标记为已释放。Dispose 后不应再使用。
        /// </summary>
        [NoGC]
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
        }

        #endregion
    }
}
