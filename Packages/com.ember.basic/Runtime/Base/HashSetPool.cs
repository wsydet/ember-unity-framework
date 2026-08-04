// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

namespace Ember.Basic
{
    using System.Collections.Generic;

#if UNITY_EDITOR
    [ForDebug, ForTest]
    public static class HashSetPoolRefCount
    {
        public static PoolRefCount RefCount = new();
    }
#endif

    /// <summary>
    /// HashSet 的共享对象池 —— 套路跟 ListPool / DictionaryPool 完全一样。
    ///
    /// <h3>契约：借了必须还</h3>
    ///
    /// Get 借走、Return 还回来。<b>不还就是池泄漏</b>。Return 会调用 set.Clear()。
    ///
    /// HashSet 最常见的场景是<b>去重</b>和<b>快速 Contains</b>。临时需要一个去重集合时
    /// new HashSet 会分配，用池子就免了。
    ///
    /// <h3>典型用法</h3>
    ///
    /// <code>
    /// // 收集不重复的 ID
    /// var seen = HashSetPool《int》.Get();
    /// try
    /// {
    ///     foreach (var entity in candidates)
    ///     {
    ///         if (seen.Add(entity.Id))
    ///             ProcessUnique(entity);
    ///     }
    /// }
    /// finally
    /// {
    ///     HashSetPool《int》.Return(seen);
    /// }
    /// </code>
    ///
    /// <h3>池的独立性</h3>
    ///
    /// HashSetPool《int》 和 HashSetPool《string》 互不干扰。
    ///
    /// <h3>常见错误</h3>
    ///
    /// <code>
    /// // ❌ 忘了 Return
    /// void Bad() {
    ///     var set = HashSetPool《string》.Get();
    ///     if (someCondition) return;  // 泄漏！
    ///     HashSetPool《string》.Return(set);
    /// }
    /// </code>
    /// </summary>
    public class HashSetPool<V>
    {
        private static readonly Stack<HashSet<V>> s_pool = new(1024);

        /// <summary>
        /// 借一个 HashSet。池空时 new 一个新的（此时有 GC 分配）。
        /// </summary>
        [HasGC]
        public static HashSet<V> Get()
        {
            var result = s_pool.Count >= 1 ? s_pool.Pop() : new HashSet<V>();
#if UNITY_EDITOR
            HashSetPoolRefCount.RefCount.IncRef(result);
#endif
            return result;
        }

        /// <summary>
        /// 还回去。先 Clear 清空所有元素。
        /// </summary>
        [NoGC]
        public static void Return(HashSet<V> set)
        {
            set.Clear();
            s_pool.Push(set);
#if UNITY_EDITOR
            HashSetPoolRefCount.RefCount.DecRef(set);
#endif
        }

        /// <summary>
        /// 清空池子，场景切换时释放内存。
        /// </summary>
        [NoGC]
        public static void Clear() => s_pool.Clear();

        /// <summary>
        /// 池子里蹲着多少个 HashSet。纯诊断用。
        /// </summary>
        public static int CachedCount => s_pool.Count;
    }
}
