using System;
using System.Collections.Generic;

namespace Ember.Core
{
    /// <summary>
    /// 事件总线 —— 全局事件发布/订阅系统。
    ///
    /// 设计参考了 burner 项目的 EventDispatcher，改用 string-key
    /// 替代 int-key，提供更好的可读性和可调试性。
    ///
    /// 特性：
    /// - 支持 0～4 个泛型参数的事件回调
    /// - 遍历中安全增删（派发中的操作延迟到本轮结束执行）
    /// - 线程不安全，仅限主线程使用（符合 Unity 规范）
    ///
    /// 用法：
    /// <code>
    /// // 订阅
    /// EmberEventBus.Subscribe("PlayerDied", OnPlayerDied);
    /// // 发布
    /// EmberEventBus.Dispatch("PlayerDied", playerId);
    /// // 取消订阅
    /// EmberEventBus.Unsubscribe("PlayerDied", OnPlayerDied);
    /// </code>
    /// </summary>
    public static class EmberEventBus
    {
        // ---- 0 参字典（强类型 Action） ----
        private static readonly Dictionary<string, Action> _events0 = new Dictionary<string, Action>();

        // ---- 1～4 参字典（Delegate 作为通用容器） ----
        private static readonly Dictionary<string, Delegate> _events1 = new Dictionary<string, Delegate>();
        private static readonly Dictionary<string, Delegate> _events2 = new Dictionary<string, Delegate>();
        private static readonly Dictionary<string, Delegate> _events3 = new Dictionary<string, Delegate>();
        private static readonly Dictionary<string, Delegate> _events4 = new Dictionary<string, Delegate>();

        /// <summary>
        /// 派发深度计数器：> 0 表示该事件正在派发中，此时
        /// Subscribe/Unsubscribe/Clear 操作会被延迟执行。
        /// </summary>
        private static readonly Dictionary<string, int> _dispatchDepth = new Dictionary<string, int>();

        /// <summary>
        /// 延迟操作队列。每个元素是一个闭包，在派发结束后执行。
        /// 使用闭包而非存储 Delegate + 类型判断，避免了 C# 开放泛型匹配问题。
        /// </summary>
        private static readonly List<Action> _pendingOps = new List<Action>();

        // ============================================================
        // Subscribe — 订阅
        // ============================================================

        public static void Subscribe(string eventKey, Action handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return;
            }

            if (_events0.TryGetValue(eventKey, out var existing))
                _events0[eventKey] = existing + handler;
            else
                _events0[eventKey] = handler;
        }

        public static void Subscribe<T>(string eventKey, Action<T> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return;
            }

            CombineInto(_events1, eventKey, handler);
        }

        public static void Subscribe<T1, T2>(string eventKey, Action<T1, T2> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return;
            }

            CombineInto(_events2, eventKey, handler);
        }

        public static void Subscribe<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return;
            }

            CombineInto(_events3, eventKey, handler);
        }

        public static void Subscribe<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Subscribe(eventKey, handler));
                return;
            }

            CombineInto(_events4, eventKey, handler);
        }

        // ============================================================
        // Unsubscribe — 取消订阅
        // ============================================================

        public static void Unsubscribe(string eventKey, Action handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveFrom(_events0, eventKey, handler);
        }

        public static void Unsubscribe<T>(string eventKey, Action<T> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events1, eventKey, handler);
        }

        public static void Unsubscribe<T1, T2>(string eventKey, Action<T1, T2> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events2, eventKey, handler);
        }

        public static void Unsubscribe<T1, T2, T3>(string eventKey, Action<T1, T2, T3> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events3, eventKey, handler);
        }

        public static void Unsubscribe<T1, T2, T3, T4>(string eventKey, Action<T1, T2, T3, T4> handler)
        {
            if (string.IsNullOrEmpty(eventKey) || handler == null) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Unsubscribe(eventKey, handler));
                return;
            }

            RemoveDelegateFrom(_events4, eventKey, handler);
        }

        // ============================================================
        // Dispatch — 派发事件
        // ============================================================

        public static void Dispatch(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            if (!_events0.TryGetValue(eventKey, out var handler) || handler == null) return;

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

        public static void Dispatch<T>(string eventKey, T arg)
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            if (!_events1.TryGetValue(eventKey, out var del)) return;
            if (!(del is Action<T> handler)) return;

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

        public static void Dispatch<T1, T2>(string eventKey, T1 arg1, T2 arg2)
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            if (!_events2.TryGetValue(eventKey, out var del)) return;
            if (!(del is Action<T1, T2> handler)) return;

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

        public static void Dispatch<T1, T2, T3>(string eventKey, T1 arg1, T2 arg2, T3 arg3)
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            if (!_events3.TryGetValue(eventKey, out var del)) return;
            if (!(del is Action<T1, T2, T3> handler)) return;

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

        public static void Dispatch<T1, T2, T3, T4>(string eventKey, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (string.IsNullOrEmpty(eventKey)) return;
            if (!_events4.TryGetValue(eventKey, out var del)) return;
            if (!(del is Action<T1, T2, T3, T4> handler)) return;

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

        // ============================================================
        // Clear — 清理
        // ============================================================

        /// <summary>
        /// 清除指定事件的所有订阅者。
        /// </summary>
        public static void Clear(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            if (InDispatch(eventKey))
            {
                _pendingOps.Add(() => Clear(eventKey));
                return;
            }

            _events0.Remove(eventKey);
            _events1.Remove(eventKey);
            _events2.Remove(eventKey);
            _events3.Remove(eventKey);
            _events4.Remove(eventKey);
        }

        /// <summary>
        /// 清除所有事件的所有订阅者。仅在彻底重置时使用。
        /// </summary>
        public static void ClearAll()
        {
            _events0.Clear();
            _events1.Clear();
            _events2.Clear();
            _events3.Clear();
            _events4.Clear();
            _dispatchDepth.Clear();
            _pendingOps.Clear();
        }

        /// <summary>
        /// 检查指定事件是否有订阅者。
        /// </summary>
        public static bool HasSubscribers(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey)) return false;
            return (_events0.TryGetValue(eventKey, out var h0) && h0 != null)
                || (_events1.TryGetValue(eventKey, out var h1) && h1 != null)
                || (_events2.TryGetValue(eventKey, out var h2) && h2 != null)
                || (_events3.TryGetValue(eventKey, out var h3) && h3 != null)
                || (_events4.TryGetValue(eventKey, out var h4) && h4 != null);
        }

        // ============================================================
        // 内部：遍历安全控制
        // ============================================================

        private static bool InDispatch(string eventKey)
        {
            return _dispatchDepth.TryGetValue(eventKey, out int depth) && depth > 0;
        }

        private static void EnterDispatch(string eventKey)
        {
            _dispatchDepth.TryGetValue(eventKey, out int depth);
            _dispatchDepth[eventKey] = depth + 1;
        }

        private static void ExitDispatch(string eventKey)
        {
            if (!_dispatchDepth.TryGetValue(eventKey, out int depth)) return;

            int newDepth = depth - 1;
            if (newDepth > 0)
            {
                _dispatchDepth[eventKey] = newDepth;
                return;
            }

            _dispatchDepth.Remove(eventKey);

            // 执行所有属于该事件的延迟操作
            ExecutePendingOps();
        }

        private static void ExecutePendingOps()
        {
            // 取出当前所有待处理操作并清空（避免嵌套派发导致的重复执行）
            var ops = new List<Action>(_pendingOps);
            _pendingOps.Clear();

            foreach (var op in ops)
            {
                op?.Invoke();
            }
        }

        // ============================================================
        // 内部：字典操作辅助
        // ============================================================

        private static void CombineInto<TDelegate>(
            Dictionary<string, Delegate> dict, string eventKey, TDelegate handler)
            where TDelegate : Delegate
        {
            if (dict.TryGetValue(eventKey, out var existing))
                dict[eventKey] = Delegate.Combine(existing, handler);
            else
                dict[eventKey] = handler;
        }

        private static void RemoveFrom(
            Dictionary<string, Action> dict, string eventKey, Action handler)
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

        private static void RemoveDelegateFrom<TDelegate>(
            Dictionary<string, Delegate> dict, string eventKey, TDelegate handler)
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
    }
}
