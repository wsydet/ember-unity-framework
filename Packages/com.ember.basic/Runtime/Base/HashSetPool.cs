//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//namespace Burner.Basic
//{
//    using System.Collections.Generic;
//
//#if UNITY_EDITOR
//    [ForDebug, ForTest]
//    public static class HashSetPoolRefCount
//    {
//        public static PoolRefCount RefCount = new PoolRefCount();
//    }
//#endif
//
//    /// <summary>
//    /// [Not Recommend for project client team]
//    ///
//    /// A pool of System.Collections.Generic.HashSet
//    /// https://burner.feishu.cn/wiki/wikcnGBpg543s50PbFnkbdjoWVc#doxcn0oAYwyQGwo4EuiflHXjgnh
//    /// </summary>
//    /// <typeparam name="V"></typeparam>
//    public class HashSetPool<V>
//    {
//        private static readonly Stack<HashSet<V>> s_dicts =
//            new Stack<HashSet<V>>(1024);
//
//        public static HashSet<V> Pop()
//        {
//            var ret = s_dicts.Count >= 1 ? s_dicts.Pop() : new HashSet<V>();
//#if UNITY_EDITOR
//            HashSetPoolRefCount.RefCount.IncRef(ret);
//#endif
//            return ret;
//        }
//
//        public static void Push(HashSet<V> set)
//        {
//            set.Clear();
//            s_dicts.Push(set);
//#if UNITY_EDITOR
//            HashSetPoolRefCount.RefCount.DecRef(set);
//#endif
//        }
//
//        public static void Clear() => s_dicts.Clear();
//
//        public static int CurrCached() => s_dicts.Count;
//    }
//}
