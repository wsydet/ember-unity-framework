// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ember.Basic
{
    /// <summary>
    /// 集合类型的扩展方法，提供遍历、批量操作等便利方法。
    /// 每个方法标注了 GC 分配情况，热路径上注意避开 [HasGC] 的方法。
    /// </summary>
    public static class CollectionExtension
    {
        // ======== ForEach ========
        // 注意：参数是接口（IDictionary / IEnumerable），调用 GetEnumerator() 返回的是接口类型，
        // 底层 struct enumerator 会被装箱。所以标 [HasGC] 而不是 [NoGC]。
        // 避免装箱的唯一方式是 C# 编译器对具体类型的 pattern-based foreach，
        // 即 foreach (var x in concreteList) 而非调用这些扩展方法。

        [HasGC]
        public static void ForEach<K, V>(this IDictionary<K, V> dict, Action<K, V> act)
        {
            using var it = dict.GetEnumerator();
            while (it.MoveNext())
            {
                var pair = it.Current;
                act(pair.Key, pair.Value);
            }
        }

        [HasGC]
        public static void ForEach<T>(this IEnumerable<T> e, Action<T> act)
        {
            using IEnumerator<T> it = e.GetEnumerator();
            while (it.MoveNext())
                act(it.Current);
        }

        [HasGC]
        public static void ForEach<T>(this IEnumerable<T> e, Action<T, int> act)
        {
            int idx = 0;
            using IEnumerator<T> it = e.GetEnumerator();
            while (it.MoveNext())
                act(it.Current, idx++);
        }

        // ======== 并行遍历 ========

        private static readonly object s_parallelLocker = new();

        /// <summary>
        /// 并行遍历集合。内部有闭包分配和 StringBuilder，异常统一抛出。
        /// </summary>
        [HasGC]
        public static void ParallelForEach<T>(this IEnumerable<T> list, Action<T> processor)
        {
            StringBuilder sb = null;
            Parallel.ForEach(list, f =>
            {
                try
                {
                    processor(f);
                }
                catch (Exception ex)
                {
                    lock (s_parallelLocker)
                    {
                        sb ??= new StringBuilder();
                        sb.Append(ex);
                    }
                }
            });

            if (sb != null)
                throw new AggregateException(sb.ToString());
        }

        // ======== 转换 ========

        [HasGC]
        public static string JoinToString<T>(this IEnumerable<T> e, string separator = ",")
        {
            var sb = new StringBuilder();
            foreach (var item in e)
            {
                if (sb.Length > 0)
                    sb.Append(separator);
                sb.Append(item?.ToString());
            }

            return sb.ToString();
        }

        [HasGC]
        public static T ConvertTo<T>(this object src)
        {
            try
            {
                return (T)Convert.ChangeType(src, typeof(T));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Ember] ConvertTo failed: {src} → {typeof(T)}, {e.Message}");
            }

            return default;
        }

        // ======== Dictionary 便利方法 ========

        /// <summary>
        /// 向 Dictionary《K, List《V》》中添加元素。Key 不存在时 new List（有分配）。
        /// </summary>
        [HasGC]
        public static void Add<K, V>(this Dictionary<K, List<V>> dict, K k, V v)
        {
            if (!dict.TryGetValue(k, out var list))
            {
                list = new List<V>();
                dict.Add(k, list);
            }

            list.Add(v);
        }

        // ======== 判空 ========

        [NoGC]
        public static bool IsNullOrEmpty<T>(this ICollection<T> c) => c == null || c.Count == 0;

        // ======== HashSet 扩展 ========

        [HasGC]
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            if (items == null || hashSet == null) return;
            foreach (var item in items)
                hashSet.Add(item);
        }

        [NoGC]
        public static int RemoveAll<T>(this HashSet<T> set, Predicate<T> pred)
        {
            int count = 0;
            while (set.Count > 0)
            {
                bool found = false;
                foreach (var s in set)
                {
                    if (pred(s))
                    {
                        set.Remove(s);
                        found = true;
                        count++;
                        break;
                    }
                }

                if (!found) break;
            }

            return count;
        }

        [NoGC]
        public static int RemoveAll<T, T1>(this Dictionary<T, T1> dict, Predicate<T> pred)
        {
            int count = 0;
            while (dict.Count > 0)
            {
                bool found = false;
                foreach (var s in dict.Keys)
                {
                    if (pred(s))
                    {
                        dict.Remove(s);
                        found = true;
                        count++;
                        break;
                    }
                }

                if (!found) break;
            }

            return count;
        }

        [NoGC]
        public static int RemoveAll<T>(this LinkedList<T> list, Predicate<T> match)
        {
            int count = 0;
            if (list.Count > 0)
            {
                var node = list.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (match(node.Value))
                    {
                        list.Remove(node);
                        count++;
                    }

                    node = next;
                }
            }

            return count;
        }
    }
}
