//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using System;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using System.Text;
//using UnityEngine;
//
//namespace Burner.Extensions
//{
//    /// <summary>
//    /// a global constant string pool instead of string.Intern.
//    /// it's more flexible and can be GC gathered comparing to string.Intern
//    ///   https://burner.feishu.cn/wiki/wikcnbxMRcCtKBPO2bPcFstHF6f
//    /// </summary>
//    public class CachedIntPtrStrings : Singleton<CachedIntPtrStrings>
//    {
//        /// <summary>
//        /// LinkedList has much more GC.Alloc, so we use this simple struct to string if it conflicts
//        /// </summary>
//        private struct CachedStringData
//        {
//            private string str0;
//            private string str1;
//            private string str2;
//            private string str3;
//
//            public CachedStringData(string str)
//            {
//                str0 = str;
//                str1 = null;
//                str2 = null;
//                str3 = null;
//            }
//
//            public string IsIn(string s)
//            {
//                if(str0 == s) return str0;
//                if(str1 == s) return str1;
//                if(str2 == s) return str2;
//                if(str3 == s) return str3;
//
//                return null;
//            }
//
//            public string IsIn(IntPtr ptr, int len)
//            {
//                if(AreEquals(ptr, len, str0)) return str0;
//                if(str1 != null && AreEquals(ptr, len, str1)) return str1;
//                if(str2 != null && AreEquals(ptr, len, str2)) return str2;
//                if(str3 != null && AreEquals(ptr, len, str3)) return str3;
//
//                return null;
//            }
//
//            public void Add(string str)
//            {
//                if(str1 == null) str1 = str;
//                else if(str2 == null) str2 = str;
//                else if(str3 == null) str3 = str;
//                else throw new Exception($"[Burner]: Internal Error! Reached the limit of {nameof(CachedIntPtrStrings)} when hash conflict! " +
//                                         "Please contact engine guys!");
//            }
//        }
//        private Dictionary<int, CachedStringData> _dict = new Dictionary<int, CachedStringData>();
//
//        public int CachedCount { get; private set; }
//        public int CachedHashCount => _dict.Count;
//
//        public int MaxCachedStringLength { get; private set; }
//
//        public void SetCapacity(int capacity)
//        {
//            var newDict = new Dictionary<int, CachedStringData>(capacity);
//
//            lock(_dict)
//            {
//                foreach(var kv in _dict)
//                {
//                    newDict.Add(kv.Key, kv.Value);
//                }
//
//                _dict = newDict;
//            }
//        }
//
//        public void ClearAll()
//        {
//            lock(_dict)
//            {
//                _dict.Clear();
//                CachedCount = 0;
//                MaxCachedStringLength = 0;
//            }
//        }
//
//        public bool IsCached(string ASCIIStr)
//        {
//#if UNITY_EDITOR
//            if(ASCIIStr.IsNullOrEmpty())
//            {
//                throw new ArgumentNullException("[Burner]: Cannot make a null/empty string as cache");
//            }
//
//            if(ASCIIStr.HasNonASCII())
//            {
//                throw new ArgumentNullException("[Burner]: Cannot make a NON-ASCII string as cache: " + ASCIIStr);
//            }
//#endif
//            var hash = GetHash(ASCIIStr);
//
//            lock(_dict)
//            {
//                if(_dict.TryGetValue(hash, out var list))
//                {
//                    return list.IsIn(ASCIIStr) != null;
//                }
//            }
//
//            return false;
//        }
//
//        /// <summary>
//        /// only support ASCII string
//        /// </summary>
//        public string MakeCached(string ASCIIStr)
//        {
//#if UNITY_EDITOR
//            if(ASCIIStr.IsNullOrEmpty())
//            {
//                throw new ArgumentNullException("[Burner]: Cannot make a null/empty string as cache");
//            }
//
//            if(ASCIIStr.HasNonASCII())
//            {
//                throw new ArgumentNullException("[Burner]: Cannot make a NON-ASCII string as cache: " + ASCIIStr);
//            }
//#endif
//
//            var hash = GetHash(ASCIIStr);
//
//            lock(_dict)
//            {
//                if(!_dict.TryGetValue(hash, out var cached))
//                {
//                    _dict.Add(hash, new CachedStringData(ASCIIStr));
//                }
//                else
//                {
//                    var ret = cached.IsIn(ASCIIStr);
//                    if(ret != null) return ret;
//
//                    cached.Add(ASCIIStr);
//                    _dict[hash] = cached;
//                }
//
//                CachedCount++;
//                MaxCachedStringLength = Mathf.Max(MaxCachedStringLength, ASCIIStr.Length);
//            }
//
//            return ASCIIStr;
//        }
//
//        public bool IsCached(IntPtr ptr, int len)
//        {
//            return TryGetCachedStr(ptr, len) != null;
//        }
//
//        public string TryGetCachedStr(IntPtr ptr, int len, bool putCacheIfNotFound = false)
//        {
//#if UNITY_EDITOR
//            if(ptr == IntPtr.Zero || len <= 0)
//            {
//                throw new ArgumentNullException("[Burner]: Cannot make a null/empty IntPtr as cache");
//            }
//#endif
//            var hash = GetHash(ptr, len);
//
//            lock(_dict)
//            {
//                string ret;
//
//                if(!_dict.TryGetValue(hash, out var cached))
//                {
//                    if(!putCacheIfNotFound) return null;
//
//                    ret = CreateStr(ptr, len);
//                    _dict.Add(hash, new CachedStringData(ret));
//                }
//                else
//                {
//                    ret = cached.IsIn(ptr, len);
//                    if(ret != null) return ret;
//
//                    if(!putCacheIfNotFound) return null;
//
//                    ret = CreateStr(ptr, len);
//                    cached.Add(ret);
//                    _dict[hash] = cached;
//                }
//
//                CachedCount++;
//                MaxCachedStringLength = Mathf.Max(MaxCachedStringLength, ret.Length);
//
//                return ret;
//            }
//        }
//
//        public string MakeCached(IntPtr ptr, int len)
//        {
//            return TryGetCachedStr(ptr, len, true);
//        }
//
//        public static string CreateStr(IntPtr ptr, int len)
//        {
//            var ret = Marshal.PtrToStringAnsi(ptr, len);
//            if(ret == null) ret = CreateUTF8Str(ptr, len);
//            return ret;
//        }
//
//        public static string CreateUTF8Str(IntPtr str, int len)
//        {
//            byte[] buffer = new byte[len];
//            Marshal.Copy(str, buffer, 0, len);
//            return Encoding.UTF8.GetString(buffer);
//        }
//
//#if UNITY_EDITOR && LOVENGINE_TESTS
//        // test hash algorithm for test result while hash has some conflict
//        public static bool IsTestHash;
//#endif
//
//        public static int GetHash(string ASCIIStr)
//        {
//#if UNITY_EDITOR && LOVENGINE_TESTS
//            if(IsTestHash)
//            {
//                int h1 = 0;
//                foreach(var c in ASCIIStr)
//                {
//                    h1 += c;
//                }
//
//                return h1;
//            }
//#endif
//            int h = 31;
//            foreach(var c in ASCIIStr)
//            {
//                h = 31 * h + c;
//            }
//            return h;
//        }
//
//        public static unsafe int GetHash(IntPtr ptr, int len)
//        {
//            var p = (byte*) ptr.ToPointer();
//
//#if UNITY_EDITOR && LOVENGINE_TESTS
//            if(IsTestHash)
//            {
//                int h1 = 0;
//                for(var i = 0;i < len;i++)
//                {
//                    h1 += p[i];
//                }
//                return h1;
//            }
//#endif
//
//            int h = 31;
//            for(int i = 0;i < len;i++)
//            {
//                h = 31 * h + p[i];
//            }
//            return h;
//        }
//
//        // comparing for ASCII string
//        private static unsafe bool AreEquals(IntPtr ptr, int len, string str)
//        {
//            if(str.Length != len)
//            {
//                return false;
//            }
//
//            var p = (byte*)ptr.ToPointer();
//            for(int i = 0;i < len;i++)
//            {
//                if(p[i] != (byte)str[i])
//                {
//                    return false;
//                }
//            }
//
//            return true;
//        }
//    }
//}
