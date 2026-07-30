//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Text;
//
//
//namespace Burner.Basic
//{
//    public static class StringExtension
//    {
//        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
//
//        public static bool HasNonASCII(this string str)
//        {
//            foreach(var c in str)
//            {
//                if(c >= 255)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }
//
//        public static bool HasNonASCII(this StringBuilder str)
//        {
//            for(int i = 0;i < str.Length;i++)
//            {
//                var c = str[i];
//                if(c >= 255)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }
//
//        [HasGC]
//        public static bool ContainsIgnoreCase(this string str, string cmp)
//        {
//            // following code would get very bad performance
//            //return str.IndexOf(cmp, System.StringComparison.OrdinalIgnoreCase) != -1;
//
//            return str.ToAlphaLower().Contains(cmp.ToAlphaLower());
//        }
//
//        public static bool EqualsIgnoreCase(this string a, string b) => string.Compare(a, b, true) == 0;
//
//        /// <summary>
//        /// check all chars if it has a char that is not char.IsWhiteSpace
//        /// </summary>
//        public static bool IsEmpty(this string str)
//        {
//            if(str.IsNullOrEmpty())
//            {
//                return true;
//            }
//
//            for(int i = 0;i < str.Length;i++)
//            {
//                if(!char.IsWhiteSpace(str[i]))
//                {
//                    return false;
//                }
//            }
//
//            return true;
//        }
//
//        /// <summary>
//        /// Convert to Int32
//        /// </summary>
//        public static int ToInt(this string s)
//        {
//            int i = 0;
//            if (!s.IsNullOrEmpty()) int.TryParse(s.Trim(), out i);
//            return i;
//        }
//
//        // Char.ToLower/IsUpper is too expensive, we just use this simple function to get alpha char's state
//        // check this wiki for detail: https://burner.feishu.cn/wiki/wikcnXgxDGEOESnxwM0fYLrIptf#oewZNx
//        internal static char ToAlphaLower(char c) => c >= 'A' && c <= 'Z' ? (char)(c - 'A' + 'a') : c;
//        internal static bool IsAlphaUpper(char c) => c >= 'A' && c <= 'Z';
//        internal static bool IsAlphaLower(char c) => c >= 'a' && c <= 'z';
//
//        /// <summary>
//        /// convert a string to alpha [A-z,a-z] lowercase string
//        /// It does NOT like string.ToLower() which will process unicode string with a expensive cost
//        /// </summary>
//        public static bool HasUpperChar(string str)
//        {
//            if(!str.IsNullOrEmpty())
//            {
//                for(int i = 0; i < str.Length; i++)
//                {
//                    if(IsAlphaUpper(str[i])) return true;
//                }
//            }
//            return false;
//        }
//
//        public static string ToAlphaLower(this string str, bool testUpperChar = true)
//        {
//            if(testUpperChar && !HasUpperChar(str)) return str;
//
//            var bytes = str.ToCharArray();
//            for(int i = 0;i < bytes.Length;i++)
//            {
//                bytes[i] = ToAlphaLower(bytes[i]);
//            }
//            return new string(bytes);
//        }
//
//        /// <summary>
//        ///  to replace "Path.GetFileName(path).ToLower()" with less GC Alloc
//        /// </summary>
//        [HasGC]
//        public static string ParseLowerCaseFilename(this string path, ref char[] parseBuffer)
//        {
//            if(path.IsNullOrEmpty())
//            {
//                return string.Empty;
//            }
//
//            if(parseBuffer == null || parseBuffer.Length < path.Length)
//            {
//                parseBuffer = new char[Math.Max(path.Length, 256)];
//            }
//
//            var start = path.LastIndexOf('/');
//            if(start == -1) start = path.LastIndexOf('\\');
//            start = start == -1 ? 0 : (start + 1);
//
//            int idx = 0;
//            while(start < path.Length)
//            {
//                parseBuffer[idx++] = ToAlphaLower(path[start++]);
//            }
//
//            return new string(parseBuffer, 0, idx);
//        }
//
//        /// <summary>
//        /// if starts with a string by the startIdx
//        /// example:
//        ///     "123_111".StartsWith("111") == false
//        ///     "123_111".StartsWithIdx("111", 4) == true
//        ///     "123_111".StartsWithIdx("111", 3) == false
//        ///     "123_111".StartsWithIdx("111", 5) == false
//        ///
//        /// </summary>
//        /// <param name="str"></param>
//        /// <param name="cmp"></param>
//        /// <param name="startIdx"> it includes the char in startIdx</param>
//        /// <param name="ignoreCase"></param>
//        /// <returns></returns>
//        [NoGC]
//        public static bool StartsWithIdx(this string str, string cmp, int startIdx, bool ignoreCase = false)
//        {
//            if(str == null || cmp == null
//            || startIdx < 0 || startIdx >= str.Length
//            || startIdx + cmp.Length > str.Length)
//            {
//                return false;
//            }
//
//            for(int i = 0; i < cmp.Length; i++)
//            {
//                char a = str[i + startIdx];
//                char b = cmp[i];
//
//                if(ignoreCase)
//                {
//                    if(ToAlphaLower(a) != ToAlphaLower(b))
//                    {
//                        return false;
//                    }
//                }
//                else
//                {
//                    if(a != b)
//                    {
//                        return false;
//                    }
//                }
//            }
//
//            return true;
//        }
//
//        /// <summary>
//        /// if EndsWith some string by the end of endIdx
//        ///
//        /// example:
//        ///     "123_111".EndsWith("123") == false
//        ///     "123_111".EndsWithIdx("123", 3) == true
//        ///     "123_111".EndsWithIdx("123", 4) == false
//        ///
//        /// </summary>
//        /// <param name="str"></param>
//        /// <param name="cmp"></param>
//        /// <param name="endIdx"> excludes the own idx</param>
//        /// <param name="ignoreCase"></param>
//        /// <returns></returns>
//        [NoGC]
//        public static bool EndsWithIdx(this string str, string cmp, int endIdx, bool ignoreCase = false)
//        {
//            if(str == null || cmp == null
//            || cmp.Length > str.Length
//            || endIdx <= 0 || endIdx > str.Length)
//            {
//                return false;
//            }
//
//            for(int i = cmp.Length - 1; i >= 0; i--)
//            {
//                char a = str[--endIdx];
//                char b = cmp[i];
//                if(ignoreCase)
//                {
//                    if(ToAlphaLower(a) != ToAlphaLower(b))
//                    {
//                        return false;
//                    }
//                }
//                else
//                {
//                    if(a != b)
//                    {
//                        return false;
//                    }
//                }
//            }
//
//            return true;
//        }
//
//        public static bool EndsWith(this string str, StringView cmp, int endIdx = -1, bool ignoreCase = false)
//        {
//            if(str == null || cmp.OriginString == null
//                           || cmp.Length > str.Length
//                           || endIdx > str.Length)
//            {
//                return false;
//            }
//
//            if(endIdx == -1) endIdx = str.Length;
//
//            for(int i = cmp.Length - 1; i >= 0; i--)
//            {
//                char a = str[--endIdx];
//                char b = cmp[i];
//                if(ignoreCase || cmp.IgnoreCaseCompare)
//                {
//                    if(ToAlphaLower(a) != ToAlphaLower(b))
//                    {
//                        return false;
//                    }
//                }
//                else
//                {
//                    if(a != b)
//                    {
//                        return false;
//                    }
//                }
//            }
//
//            return true;
//        }
//
//        public static StringView[] SplitToStringViews(this string str, char splitChar, bool ignoreCase = false)
//        {
//            if(str.IsNullOrEmpty()) throw new System.ArgumentNullException();
//
//            int splitCount = 0;
//            foreach(var c in str)
//            {
//                if(c == splitChar)
//                {
//                    splitCount++;
//                }
//            }
//
//            var stringViews = new StringView[splitCount + 1];
//            var idx = 0;
//            var startIdx = 0;
//            for(int i = 0; i < str.Length; i++)
//            {
//                if(str[i] == splitChar)
//                {
//                    stringViews[idx++] = new StringView(str, startIdx, i - startIdx, ignoreCase);
//                    startIdx = i + 1;
//                }
//            }
//
//            if(startIdx < str.Length)
//            {
//                stringViews[idx] = new StringView(str, startIdx, str.Length - startIdx, ignoreCase);
//            }
//
//            return stringViews;
//        }
//
//        public static StringView[] SplitToStringViews(this string str, char[] splitChar, bool ignoreCase = false)
//        {
//            if(str.IsNullOrEmpty()) throw new System.ArgumentNullException();
//
//            int splitCount = 0;
//            foreach(var c in str)
//            {
//                foreach(var s in splitChar)
//                {
//                    if(c == s)
//                    {
//                        splitCount++;
//                        break;
//                    }
//                }
//            }
//
//            var stringViews = new StringView[splitCount + 1];
//            var idx = 0;
//            var startIdx = 0;
//            for(int i = 0; i < str.Length; i++)
//            {
//                foreach(var s in splitChar)
//                {
//                    if(str[i] == s)
//                    {
//                        stringViews[idx++] = new StringView(str, startIdx, i - startIdx, ignoreCase);
//                        startIdx = i + 1;
//                        break;
//                    }
//                }
//            }
//
//            if(startIdx < str.Length)
//            {
//                stringViews[idx] = new StringView(str, startIdx, str.Length - startIdx, ignoreCase);
//            }
//
//            return stringViews;
//        }
//    }
//}
