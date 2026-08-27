// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

namespace Ember.Basic
{
    using System.Collections.Generic;

#if UNITY_EDITOR
    [ForDebug, ForTest]
    public static class ListPoolRefCount
    {
        public static PoolRefCount RefCount = new();
    }
#endif

    /// <summary>
    /// List 的共享对象池 —— 把临时 List 还给池子而不是丢掉，下次要用直接拿，避免 GC。
    ///
    /// <h3>核心契约：借了必须还</h3>
    ///
    /// Get 借走一个 List，用完必须 Return 还回来。<b>不还就是池泄漏</b>——池子被掏空，
    /// 后续请求只能 new，失去池的意义。Return 后池子会把 List 清空（调用 Clear），
    /// 所以归还之后就不要再碰它了。
    ///
    /// <h3>典型用法</h3>
    ///
    /// 最常见的场景是方法内部需要一个临时 List 做收集，用完就丢：
    ///
    /// <code>
    /// var hits = ListPool《RaycastHit》.Get();
    /// try
    /// {
    ///     Physics.RaycastNonAlloc(ray, hits);
    ///     for (int i = 0; i 《 hits.Count; i++) ProcessHit(hits[i]);
    /// }
    /// finally
    /// {
    ///     ListPool《RaycastHit》.Return(hits);
    /// }
    /// </code>
    ///
    /// 用 try/finally 包裹可以保证即使中间抛异常也不会泄漏。
    ///
    /// <h3>池的独立性</h3>
    ///
    /// 每个类型 T 有自己独立的池。ListPool《int》 和 ListPool《string》 互不干扰，
    /// int 池子里装的都是 List《int》，string 池子里装的都是 List《string》。
    ///
    /// <h3>容量策略</h3>
    ///
    /// Get 不保证返回的 List 是空的——只保证它的 Capacity 够用。
    /// 归还时池子不会缩小 List 的 Capacity（已经分配的内存留着下次直接用）。
    ///
    /// <h3>什么时候用？</h3>
    ///
    /// 适合：每帧都会临时创建、用完即弃的 List（碰撞检测、查询结果、临时排序等）。
    /// 不适合：需要长期持有、存到字段里、作为返回值交给外部管理的 List。
    ///
    /// <h3>调试泄漏</h3>
    ///
    /// Editor 下怀疑某处没有 Return，可以打开 PoolRefCount 的泄漏追踪，
    /// 打印出所有借了没还的调用堆栈。
    ///
    /// <h3>常见错误</h3>
    ///
    /// <code>
    /// // ❌ 忘记 Return —— 最常见也最隐蔽的泄漏
    /// void Bad1() {
    ///     var list = ListPool《int》.Get();
    ///     if (skip) return;  // 泄漏！
    ///     ListPool《int》.Return(list);
    /// }
    ///
    /// // ❌ 存到字段 —— 所有权混乱，没人知道什么时候该还
    /// class Bad2 {
    ///     private List《int》 _cache = ListPool《int》.Get();
    /// }
    ///
    /// // ❌ Return 之后继续用 —— 池子可能已经把它给了别人
    /// var list = ListPool《int》.Get();
    /// ListPool《int》.Return(list);
    /// list.Add(1);  // 危险！
    /// </code>
    /// </summary>
    public class ListPool<T>
    {
        private static readonly List<List<T>> s_pool = new(1024);

        /// <summary>Get 不指定容量时的默认值。</summary>
        public const int DefaultCapacity = 16;

        /// <summary>
        /// 从池中借一个 List。保证返回的 List 容量至少为 <paramref name="capacity"/>。
        /// 池空或池中所有 List 都不够大时，new 一个新的（此时有 GC 分配）。
        ///
        /// 内部从池尾向前找第一个满足容量要求的——优先复用大的，避免使用中小 List 导致频繁扩容。
        /// </summary>
        [HasGC]
        public static List<T> Get(int capacity = DefaultCapacity)
        {
            List<T> result = null;
            for (int i = s_pool.Count - 1; i >= 0; i--)
            {
                var list = s_pool[i];
                if (list.Capacity >= capacity)
                {
                    s_pool.RemoveAt(i);
                    result = list;
                    break;
                }
            }

            result ??= new List<T>(capacity);

#if UNITY_EDITOR
            ListPoolRefCount.RefCount.IncRef(result);
#endif
            return result;
        }

        /// <summary>
        /// 把借走的 List 还回来。归还前会调用 <c>list.Clear()</c> 清空所有元素。
        /// 还完之后不要再持有这个 List 的引用。
        /// </summary>
        [NoGC]
        public static void Return(List<T> list)
        {
            list.Clear();
            s_pool.Add(list);

#if UNITY_EDITOR
            ListPoolRefCount.RefCount.DecRef(list);
#endif
        }

        /// <summary>
        /// 清空池子里的所有缓存。场景切换或需要立即释放内存时用，平时不需要调。
        /// </summary>
        [NoGC]
        public static void Clear() => s_pool.Clear();

        /// <summary>
        /// 池子里蹲着多少个 List 等着被借。纯诊断用，不要拿这个值做业务判断。
        /// </summary>
        public static int CachedCount => s_pool.Count;
    }
}
