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
//    public static class ListPoolRefCount
//    {
//        public static PoolRefCount RefCount = new PoolRefCount();
//    }
//#endif
//
//    /// <summary>
//    /// A easy pool for System.Collections.Generic.List<T>
//    /// https://burner.feishu.cn/wiki/wikcnGBpg543s50PbFnkbdjoWVc#doxcn0oAYwyQGwo4EuiflHXjgnh
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    public class ListPool<T>
//    {
//        private static readonly List<List<T>> s_lists =
//            new List<List<T>>(1024);
//
//        public const int InitCapacity = 16;
//
//
//        public static List<T> Pop()
//        {
//            return Pop(InitCapacity);
//        }
//
//        public static List<T> PopLeast(int capacity)
//        {
//            List<T> ret = null;
//            for(var i = s_lists.Count - 1; i >= 0; i--)
//            {
//                var list = s_lists[i];
//                if(list.Capacity >= capacity)
//                {
//                    s_lists.RemoveAt(i);
//                    ret = list;
//                    break;
//                }
//            }
//
//            if(ret == null)
//            {
//                ret = new List<T>(capacity);
//            }
//
//#if UNITY_EDITOR
//            ListPoolRefCount.RefCount.IncRef(ret);
//#endif
//            return ret;
//        }
//
//        public static List<T> Pop(int capacity)
//        {
//            return PopLeast(capacity);
//        }
//
//        public static void Push(List<T> list)
//        {
//            list.Clear();
//            s_lists.Add(list);
//
//#if UNITY_EDITOR
//            ListPoolRefCount.RefCount.DecRef(list);
//#endif
//        }
//
//        public static void Clear() => s_lists.Clear();
//
//        public static int CurrCached => s_lists.Count;
//    }
//}
