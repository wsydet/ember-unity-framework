using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core
{
    /// <summary>
    /// 事件总线 —— 全局事件发布/订阅系统。
    ///
    /// 设计参考了 burner 项目的 EventDispatcher。使用 int-key 配合
    /// <see cref="EmberBroadcastEvent"/> 常量表，通过区间分配避免 Key 冲突。
    ///
    /// 定位：广播型生命周期事件（模块 Ready / Shutdown、场景加载等）。
    /// 具体游戏数据事件（血量变化、物品变更）推荐使用 UniRx Subject。
    ///
    /// 特性：
    /// - 支持 0～4 个泛型参数的事件回调
    /// - int-key + 常量表，编译期避免冲突，IDE 可跳转
    /// - 遍历中安全增删（派发中的操作延迟到本轮结束执行）
    /// - 线程不安全，仅限主线程使用（符合 Unity 规范）
    ///
    /// API 对齐 UniRx：<c>Subscribe</c> 返回 <see cref="IDisposable"/>，
    /// <c>OnNext</c> 广播事件。
    ///
    /// 用法：
    /// <code>
    /// // 订阅（返回 IDisposable，与 UniRx 一致）
    /// var sub = EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, OnResourceReady);
    /// // 发布（类似 UniRx Subject.OnNext / MessageBroker.Publish）
    /// EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);
    /// // 取消订阅（Dispose 即可）
    /// sub.Dispose();
    /// </code>
    /// </summary>
    public static class EmberEventBus
    {
        private const string TAG = LogTags.CoreEventBus;

        #region 参数

        /// <summary>
        /// 0 参事件字典（强类型 Action，支持 += / -= 运算符）。
        /// </summary>
        private static readonly Dictionary<int, Action> _events0 = new();

        /// <summary>
        /// 1～4 参事件字典（Delegate 作为通用容器，Dispatch 时做类型转换）。
        /// </summary>
        private static readonly Dictionary<int, Delegate> _events1 = new();
        private static readonly Dictionary<int, Delegate> _events2 = new();
        private static readonly Dictionary<int, Delegate> _events3 = new();
        private static readonly Dictionary<int, Delegate> _events4 = new();

        /// <summary>
        /// 播报深度计数器，> 0 表示该事件正在派发中，
        /// 此时 Subscribe / Unsubscribe / Clear 操作会被延迟执行。
        /// </summary>
        private static readonly Dictionary<int, int> _dispatchDepth = new();

        /// <summary>
        /// 延迟操作队列。每个元素是一个闭包，在派发结束后执行。
        /// 使用闭包而非存储 Delegate + 类型判断，避免了 C# 开放泛型匹配问题。
        /// </summary>
        private static readonly List<Action> _pendingOps = new List<Action>();

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 订阅 ========

        /// <summary>
        /// 订阅无参事件，返回 <see cref="IDisposable"/>（对齐 UniRx）。
        /// Dispose 返回值即可取消订阅。
        /// </summary>
        public static IDisposable Subscribe(int eventKey, Action handler)
        {
            if (handler == null) return Subscription.Empty;
            EmberDebug.LogEvent(TAG, $"Subscribe: key={eventKey}, handler={handler.Method.Name}");

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return new Subscription(() => Unsubscribe(eventKey, handler));
            }

            if (_events0.TryGetValue(eventKey, out var existing))
                _events0[eventKey] = existing + handler;
            else
                _events0[eventKey] = handler;

            return new Subscription(() => Unsubscribe(eventKey, handler));
        }

        /// <summary>
        /// 订阅 1 参事件，返回 <see cref="IDisposable"/>。
        /// </summary>
        public static IDisposable Subscribe<T>(int eventKey, Action<T> handler)
        {
            if (handler == null) return Subscription.Empty;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return new Subscription(() => Unsubscribe(eventKey, handler));
            }

            CombineInto(_events1, eventKey, handler);
            return new Subscription(() => Unsubscribe(eventKey, handler));
        }

        /// <summary>
        /// 订阅 2 参事件，返回 <see cref="IDisposable"/>。
        /// </summary>
        public static IDisposable Subscribe<T1, T2>(int eventKey, Action<T1, T2> handler)
        {
            if (handler == null) return Subscription.Empty;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return new Subscription(() => Unsubscribe(eventKey, handler));
            }

            CombineInto(_events2, eventKey, handler);
            return new Subscription(() => Unsubscribe(eventKey, handler));
        }

        /// <summary>
        /// 订阅 3 参事件，返回 <see cref="IDisposable"/>。
        /// </summary>
        public static IDisposable Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
        {
            if (handler == null) return Subscription.Empty;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return new Subscription(() => Unsubscribe(eventKey, handler));
            }

            CombineInto(_events3, eventKey, handler);
            return new Subscription(() => Unsubscribe(eventKey, handler));
        }

        /// <summary>
        /// 订阅 4 参事件，返回 <see cref="IDisposable"/>。
        /// </summary>
        public static IDisposable Subscribe<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
        {
            if (handler == null) return Subscription.Empty;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return new Subscription(() => Unsubscribe(eventKey, handler));
            }

            CombineInto(_events4, eventKey, handler);
            return new Subscription(() => Unsubscribe(eventKey, handler));
        }

        // ======== 取消订阅 ========

        /// <summary>
        /// 取消订阅无参事件。
        /// </summary>
        public static void Unsubscribe(int eventKey, Action handler)
        {
            if (handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveFrom(_events0, eventKey, handler);
        }

        /// <summary>
        /// 取消订阅 1 参事件。
        /// </summary>
        public static void Unsubscribe<T>(int eventKey, Action<T> handler)
        {
            if (handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events1, eventKey, handler);
        }

        /// <summary>
        /// 取消订阅 2 参事件。
        /// </summary>
        public static void Unsubscribe<T1, T2>(int eventKey, Action<T1, T2> handler)
        {
            if (handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events2, eventKey, handler);
        }

        /// <summary>
        /// 取消订阅 3 参事件。
        /// </summary>
        public static void Unsubscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)
        {
            if (handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events3, eventKey, handler);
        }

        /// <summary>
        /// 取消订阅 4 参事件。
        /// </summary>
        public static void Unsubscribe<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)
        {
            if (handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events4, eventKey, handler);
        }

        // ======== 播报 ========

        /// <summary>
        /// 播报无参事件。
        /// </summary>
        public static void OnNext(int eventKey)
        {
            if (!_events0.TryGetValue(eventKey, out var handler) || handler == null) return;
            EmberDebug.LogEvent(TAG, $"Dispatch: key={eventKey}");

            EnterDispatch(eventKey);
            try
            {
                handler.Invoke();
            }
            finally
            {
                ExitDispatch(eventKey);
            }
        }

        /// <summary>
        /// 播报 1 参事件。
        /// </summary>
        public static void OnNext<T>(int eventKey, T arg)
        {
            if (!_events1.TryGetValue(eventKey, out var del)) return;
            if (del is not Action<T> handler) return;

            EnterDispatch(eventKey);
            try
            {
                handler.Invoke(arg);
            }
            finally
            {
                ExitDispatch(eventKey);
            }
        }

        /// <summary>
        /// 播报 2 参事件。
        /// </summary>
        public static void OnNext<T1, T2>(int eventKey, T1 arg1, T2 arg2)
        {
            if (!_events2.TryGetValue(eventKey, out var del)) return;
            if (del is not Action<T1, T2> handler) return;

            EnterDispatch(eventKey);
            try
            {
                handler.Invoke(arg1, arg2);
            }
            finally
            {
                ExitDispatch(eventKey);
            }
        }

        /// <summary>
        /// 播报 3 参事件。
        /// </summary>
        public static void OnNext<T1, T2, T3>(int eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!_events3.TryGetValue(eventKey, out var del)) return;
            if (del is not Action<T1, T2, T3> handler) return;

            EnterDispatch(eventKey);
            try
            {
                handler.Invoke(arg1, arg2, arg3);
            }
            finally
            {
                ExitDispatch(eventKey);
            }
        }

        /// <summary>
        /// 播报 4 参事件。
        /// </summary>
        public static void OnNext<T1, T2, T3, T4>(int eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!_events4.TryGetValue(eventKey, out var del)) return;
            if (del is not Action<T1, T2, T3, T4> handler) return;

            EnterDispatch(eventKey);
            try
            {
                handler.Invoke(arg1, arg2, arg3, arg4);
            }
            finally
            {
                ExitDispatch(eventKey);
            }
        }

        // ======== 诊断与清理 ========

        /// <summary>
        /// 检查指定事件是否有订阅者。
        /// </summary>
        public static bool HasSubscribers(int eventKey)
        {
            return (_events0.TryGetValue(eventKey, out var h0) && h0 != null)
                || (_events1.TryGetValue(eventKey, out var h1) && h1 != null)
                || (_events2.TryGetValue(eventKey, out var h2) && h2 != null)
                || (_events3.TryGetValue(eventKey, out var h3) && h3 != null)
                || (_events4.TryGetValue(eventKey, out var h4) && h4 != null);
        }

        /// <summary>
        /// 清除指定事件的所有订阅者。通常在模块退出时调用，
        /// 避免残留订阅导致野指针回调（类似于 UniRx 中 Dispose 所有相关 subscription）。
        /// 与逐个 <c>sub.Dispose()</c> 效果相同，但更高效。
        /// </summary>
        public static void ClearSubscribers(int eventKey)
        {
            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => ClearSubscribers(eventKey));
                return;
            }

            _events0.Remove(eventKey);
            _events1.Remove(eventKey);
            _events2.Remove(eventKey);
            _events3.Remove(eventKey);
            _events4.Remove(eventKey);
        }

        /// <summary>
        /// 清除所有事件的所有订阅者。仅在程序退出或彻底重置时使用。
        /// 类似于 UniRx 中 <c>CompositeDisposable.Clear()</c> 的语义。
        /// </summary>
        public static void ClearAllSubscribers()
        {
            EmberDebug.LogCleanup(TAG, "ClearAllSubscribers");
            _events0.Clear();
            _events1.Clear();
            _events2.Clear();
            _events3.Clear();
            _events4.Clear();
            _dispatchDepth.Clear();
            _pendingOps.Clear();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// 判断指定事件是否正在派发中。
        /// </summary>
        private static bool InDispatch(int eventKey)
        {
            return _dispatchDepth.TryGetValue(eventKey, out int depth) && depth > 0;
        }

        /// <summary>
        /// 进入派发，递增嵌套深度。
        /// </summary>
        private static void EnterDispatch(int eventKey)
        {
            _dispatchDepth.TryGetValue(eventKey, out int depth);
            _dispatchDepth[eventKey] = depth + 1;
        }

        /// <summary>
        /// 退出派发，递减嵌套深度；当深度归零时执行所有延迟操作。
        /// </summary>
        private static void ExitDispatch(int eventKey)
        {
            if (!_dispatchDepth.TryGetValue(eventKey, out int depth)) return;

            int newDepth = depth - 1;
            if (newDepth > 0)
            {
                _dispatchDepth[eventKey] = newDepth;
                return;
            }

            _dispatchDepth.Remove(eventKey);
            ExecutePendingOps();
        }

        /// <summary>
        /// 执行延迟操作队列中的所有操作（取出后清空，避免嵌套派发重复执行）。
        /// </summary>
        private static void ExecutePendingOps()
        {
            var ops = new List<Action>(_pendingOps);
            _pendingOps.Clear();

            foreach (var op in ops)
            {
                op?.Invoke();
            }
        }

        /// <summary>
        /// 将 handler 合并到 Delegate 字典中（泛型版本，支持 Delegate.Combine）。
        /// </summary>
        private static void CombineInto<TDelegate>(
            Dictionary<int, Delegate> dict, int eventKey, TDelegate handler)
            where TDelegate : Delegate
        {
            if (dict.TryGetValue(eventKey, out var existing))
                dict[eventKey] = Delegate.Combine(existing, handler);
            else
                dict[eventKey] = handler;
        }

        /// <summary>
        /// 从强类型 Action 字典中移除 handler（支持 -= 运算符）。
        /// </summary>
        private static void RemoveFrom(
            Dictionary<int, Action> dict, int eventKey, Action handler)
        {
            if (dict.TryGetValue(eventKey, out var existing))
            {
                var result = existing - handler;
                if (result == null)
                    dict.Remove(eventKey);
                else
                    dict[eventKey] = result;
            }
        }

        /// <summary>
        /// 从 Delegate 字典中移除 handler（泛型版本，支持 Delegate.Remove）。
        /// </summary>
        private static void RemoveDelegateFrom<TDelegate>(
            Dictionary<int, Delegate> dict, int eventKey, TDelegate handler)
            where TDelegate : Delegate
        {
            if (dict.TryGetValue(eventKey, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                    dict.Remove(eventKey);
                else
                    dict[eventKey] = result;
            }
        }

        #endregion

        // ============================================================

        /// <summary>
        /// 订阅句柄，调用 <see cref="Dispose"/> 即可取消订阅（对齐 UniRx IDisposable 模式）。
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }

            /// <summary>空订阅句柄，用于 handler 为 null 等无需取消的场景。</summary>
            public static readonly IDisposable Empty = new Subscription(null);
        }
    }
}
