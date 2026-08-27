// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

namespace Ember.Basic
{
    using System.Collections.Generic;

#if UNITY_EDITOR
    [ForDebug, ForTest]
    public static class DictionaryPoolRefCount
    {
        public static PoolRefCount RefCount = new();
    }
#endif

    /// <summary>
    /// Dictionary 的共享对象池 —— 跟 ListPool 一样的套路，只是池子装的是 Dictionary。
    ///
    /// <h3>契约：借了必须还</h3>
    ///
    /// Get 借走、Return 还回来。<b>不还就是池泄漏</b>。Return 会调用 dict.Clear()，
    /// 所以还完之后 Key/Value 全部清空，不要再持有引用。
    ///
    /// Dictionary 比 List 更重（内部有 Bucket 数组 + Entry 数组），new 一次的开销更大，
    /// 池化的收益也更明显。尤其是 Key 为 string/Enum 的场景——查表、映射、计数——非常常见。
    ///
    /// <h3>典型用法</h3>
    ///
    /// <code>
    /// // 统计每种物品的数量，只在一帧内有效
    /// var counts = DictionaryPool《string, int》.Get();
    /// try
    /// {
    ///     foreach (var item in inventory)
    ///         counts[item.Name] = counts.GetValueOrDefault(item.Name) + 1;
    ///     // ... 使用 counts ...
    /// }
    /// finally
    /// {
    ///     DictionaryPool《string, int》.Return(counts);
    /// }
    /// </code>
    ///
    /// <h3>池的独立性</h3>
    ///
    /// DictionaryPool《string, int》 和 DictionaryPool《int, string》 是两个互不干扰的池。
    /// 每个 (K, V) 组合对应自己独立的静态 Stack。
    ///
    /// <h3>常见错误</h3>
    ///
    /// <code>
    /// // ❌ 忘了 Return（跟 ListPool 一样）
    /// void Bad() {
    ///     var dict = DictionaryPool《int, string》.Get();
    ///     if (earlyExit) return;  // 泄漏！
    ///     DictionaryPool《int, string》.Return(dict);
    /// }
    ///
    /// // ❌ 存到字段
    /// class Bad {
    ///     private Dictionary《int, string》 _lookup = DictionaryPool《int, string》.Get();
    /// }
    /// </code>
    /// </summary>
    public class DictionaryPool<K, V>
    {
        private static readonly Stack<Dictionary<K, V>> s_pool = new(1024);

        /// <summary>
        /// 借一个 Dictionary。池空时 new 一个新的（此时有 GC 分配）。
        /// </summary>
        [HasGC]
        public static Dictionary<K, V> Get(int capacity = 16)
        {
            var result = s_pool.Count >= 1 ? s_pool.Pop() : new Dictionary<K, V>(capacity);
#if UNITY_EDITOR
            DictionaryPoolRefCount.RefCount.IncRef(result);
#endif
            return result;
        }

        /// <summary>
        /// 还回去。先 Clear 清空所有 Key/Value。
        /// </summary>
        [NoGC]
        public static void Return(Dictionary<K, V> dict)
        {
            dict.Clear();
            s_pool.Push(dict);
#if UNITY_EDITOR
            DictionaryPoolRefCount.RefCount.DecRef(dict);
#endif
        }

        /// <summary>
        /// 清空池子，场景切换时释放内存。
        /// </summary>
        [NoGC]
        public static void Clear() => s_pool.Clear();

        /// <summary>
        /// 池子里蹲着多少个 Dictionary。纯诊断用。
        /// </summary>
        public static int CachedCount => s_pool.Count;
    }
}
