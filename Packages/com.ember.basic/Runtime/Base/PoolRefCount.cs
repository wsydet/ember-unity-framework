//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//#if UNITY_EDITOR
//
//namespace Burner.Basic
//{
//    using System.Collections.Generic;
//    using System.Text;
//
//    [ForDebug]
//    public class PoolRefCount
//    {
//        // default value is false otherwise it will cause a lot lot lot of GC.Alloc
//        // by new System.Diagnostics.StackTrace().ToString() (2.7GB) when parse Manifest(abfiles).
//        //
//        // so it can only be enabled in test unit context
//        public static bool EnableCheck = false;
//
//        public int Count => _refStacks.Count;
//
//        Dictionary<object, string> _refStacks = new Dictionary<object, string>();
//
//        public void IncRef(object obj)
//        {
//            if(EnableCheck)
//            {
//                _refStacks.Add(obj, new System.Diagnostics.StackTrace(true).ToString());
//            }
//        }
//
//        public void DecRef(object obj)
//        {
//            if(EnableCheck)
//            {
//                _refStacks.Remove(obj);
//            }
//        }
//
//        public string AllLeakedObjStacks()
//        {
//            var stack = new Dictionary<string, int>();
//            _refStacks.Values.ForEach(s =>
//            {
//                if(stack.ContainsKey(s))
//                {
//                    stack[s]++;
//                }
//                else
//                {
//                    stack.Add(s, 1);
//                }
//            });
//
//            var sb = new StringBuilder();
//            foreach(var kv in stack)
//            {
//                sb.Append($"{kv.Value} leaked objects with following stacktrace:\n").Append(kv.Key).Append("\n\n");
//            }
//
//            return sb.ToString();
//        }
//
//        public void ClearAllStacks()
//        {
//            _refStacks.Clear();
//        }
//    }
//}
//
//#endif
