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
//    public static class DictionaryPoolRefCount
//    {
//        public static PoolRefCount RefCount = new PoolRefCount();
//    }
//#endif
//
//    /// <summary>
//    /// [Not Recommend for project client team]
//    ///
//    /// A pool of System.Collections.Generic.Dictionary
//    /// https://burner.feishu.cn/wiki/wikcnGBpg543s50PbFnkbdjoWVc#doxcn0oAYwyQGwo4EuiflHXjgnh
//    /// </summary>
//    /// <typeparam name="K"></typeparam>
//    /// <typeparam name="V"></typeparam>
//    public class DictionaryPool<K, V>
//    {
//        private static readonly Stack<Dictionary<K, V>> s_dicts =
//            new Stack<Dictionary<K, V>>(1024);
//
//        public static Dictionary<K, V> Pop()
//        {
//            return Pop(16);
//        }
//
//        public static Dictionary<K, V> Pop(int capacity)
//        {
//            var ret = s_dicts.Count >= 1 ? s_dicts.Pop() : new Dictionary<K, V>(capacity);
//#if UNITY_EDITOR
//            DictionaryPoolRefCount.RefCount.IncRef(ret);
//#endif
//            return ret;
//        }
//
//        public static void Push(Dictionary<K, V> dic)
//        {
//            dic.Clear();
//            s_dicts.Push(dic);
//#if UNITY_EDITOR
//            DictionaryPoolRefCount.RefCount.DecRef(dic);
//#endif
//        }
//
//        public static void Clear() => s_dicts.Clear();
//
//        public static int CurrCached() => s_dicts.Count;
//    }
//}
