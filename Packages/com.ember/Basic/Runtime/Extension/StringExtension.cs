// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.Text;

namespace Ember.Basic
{
    /// <summary>
    /// 字符串扩展方法，侧重于性能优化（避免 Unicode 开销、减少 GC 分配）。
    /// </summary>
    public static class StringExtension
    {
        // ======== 基本判断 ========

        [NoGC]
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

        [NoGC]
        public static bool IsEmpty(this string str)
        {
            if (str.IsNullOrEmpty()) return true;

            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsWhiteSpace(str[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 判断字符串是否包含非 ASCII 字符（c >= 255）。
        /// </summary>
        [NoGC]
        public static bool HasNonASCII(this string str)
        {
            foreach (var c in str)
            {
                if (c >= 255) return true;
            }

            return false;
        }

        /// <summary>
        /// 判断 StringBuilder 是否包含非 ASCII 字符（c >= 255）。
        /// </summary>
        [NoGC]
        public static bool HasNonASCII(this StringBuilder str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] >= 255) return true;
            }

            return false;
        }

        // ======== 大小写（仅 ASCII，避免 Unicode 开销）========

        /// <summary>
        /// ASCII-only 转小写。仅处理 A-Z → a-z，不支持 Unicode 大小写映射。
        /// 比 <see cref="char.ToLowerInvariant"/> 快很多，适合日志/路径/配置键等场景。
        /// </summary>
        [NoGC]
        internal static char ToAlphaLower(char c) => c is >= 'A' and <= 'Z' ? (char)(c - 'A' + 'a') : c;

        [NoGC]
        internal static bool IsAlphaUpper(char c) => c is >= 'A' and <= 'Z';

        [NoGC]
        internal static bool IsAlphaLower(char c) => c is >= 'a' and <= 'z';

        /// <summary>
        /// ASCII-only 转小写（整个字符串）。先检查是否有大写字符，无则直接返回原串避免分配。
        /// </summary>
        [HasGC]
        public static string ToAlphaLower(this string str, bool testUpperChar = true)
        {
            if (testUpperChar && !HasUpperChar(str)) return str;

            var chars = str.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = ToAlphaLower(chars[i]);
            return new string(chars);
        }

        /// <summary>
        /// 检查字符串中是否包含大写 ASCII 字符。
        /// </summary>
        [NoGC]
        public static bool HasUpperChar(string str)
        {
            if (!str.IsNullOrEmpty())
            {
                for (int i = 0; i < str.Length; i++)
                {
                    if (IsAlphaUpper(str[i])) return true;
                }
            }

            return false;
        }

        // ======== 忽略大小写比较 ========

        /// <summary>
        /// 忽略大小写判断是否包含子串。使用 ASCII-only 大小写转换，比 StringComparison.OrdinalIgnoreCase 更快。
        /// </summary>
        [HasGC]
        public static bool ContainsIgnoreCase(this string str, string cmp)
        {
            return str.ToAlphaLower().Contains(cmp.ToAlphaLower());
        }

        /// <summary>
        /// 忽略大小写相等判断。
        /// </summary>
        [NoGC]
        public static bool EqualsIgnoreCase(this string a, string b) => string.Compare(a, b, true) == 0;

        // ======== 转换 ========

        /// <summary>
        /// 安全地将字符串转为 int，失败返回 0。
        /// </summary>
        [NoGC]
        public static int ToInt(this string s)
        {
            int i = 0;
            if (!s.IsNullOrEmpty()) int.TryParse(s.Trim(), out i);
            return i;
        }

        // ======== 路径工具 ========

        /// <summary>
        /// 从路径中提取文件名并转为小写，使用可复用的 char 缓冲区减少 GC 分配。
        /// </summary>
        [HasGC]
        public static string ParseLowerCaseFilename(this string path, ref char[] parseBuffer)
        {
            if (path.IsNullOrEmpty()) return string.Empty;

            if (parseBuffer == null || parseBuffer.Length < path.Length)
                parseBuffer = new char[Math.Max(path.Length, 256)];

            var start = path.LastIndexOf('/');
            if (start == -1) start = path.LastIndexOf('\\');
            start = start == -1 ? 0 : start + 1;

            int idx = 0;
            while (start < path.Length)
                parseBuffer[idx++] = ToAlphaLower(path[start++]);

            return new string(parseBuffer, 0, idx);
        }

        // ======== 索引比较（零分配）========

        /// <summary>
        /// 判断 str 从 startIdx 开始是否以 cmp 开头。零 GC 分配。
        /// </summary>
        [NoGC]
        public static bool StartsWithIdx(this string str, string cmp, int startIdx, bool ignoreCase = false)
        {
            if (str == null || cmp == null
                || startIdx < 0 || startIdx >= str.Length
                || startIdx + cmp.Length > str.Length)
                return false;

            for (int i = 0; i < cmp.Length; i++)
            {
                char a = str[i + startIdx];
                char b = cmp[i];

                if (ignoreCase)
                {
                    if (ToAlphaLower(a) != ToAlphaLower(b)) return false;
                }
                else
                {
                    if (a != b) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断 str 在 endIdx 之前是否以 cmp 结尾。零 GC 分配。
        /// </summary>
        [NoGC]
        public static bool EndsWithIdx(this string str, string cmp, int endIdx, bool ignoreCase = false)
        {
            if (str == null || cmp == null
                || cmp.Length > str.Length
                || endIdx <= 0 || endIdx > str.Length)
                return false;

            for (int i = cmp.Length - 1; i >= 0; i--)
            {
                char a = str[--endIdx];
                char b = cmp[i];
                if (ignoreCase)
                {
                    if (ToAlphaLower(a) != ToAlphaLower(b)) return false;
                }
                else
                {
                    if (a != b) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断 str 是否以指定的 StringView 结尾。
        /// </summary>
        [NoGC]
        public static bool EndsWith(this string str, StringView cmp, int endIdx = -1, bool ignoreCase = false)
        {
            if (str == null || cmp.OriginString == null
                || cmp.Length > str.Length
                || endIdx > str.Length)
                return false;

            if (endIdx == -1) endIdx = str.Length;

            for (int i = cmp.Length - 1; i >= 0; i--)
            {
                char a = str[--endIdx];
                char b = cmp[i];
                if (ignoreCase || cmp.IgnoreCaseCompare)
                {
                    if (ToAlphaLower(a) != ToAlphaLower(b)) return false;
                }
                else
                {
                    if (a != b) return false;
                }
            }

            return true;
        }

        // ======== 零分配 Split ========

        /// <summary>
        /// 按单个字符分割字符串为零分配的 StringView 数组。
        /// </summary>
        [HasGC]
        public static StringView[] SplitToStringViews(this string str, char splitChar, bool ignoreCase = false)
        {
            if (str.IsNullOrEmpty()) throw new ArgumentNullException(nameof(str));

            int splitCount = 0;
            foreach (var c in str)
            {
                if (c == splitChar) splitCount++;
            }

            var stringViews = new StringView[splitCount + 1];
            int idx = 0;
            int startIdx = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == splitChar)
                {
                    stringViews[idx++] = new StringView(str, startIdx, i - startIdx, ignoreCase);
                    startIdx = i + 1;
                }
            }

            if (startIdx < str.Length)
                stringViews[idx] = new StringView(str, startIdx, str.Length - startIdx, ignoreCase);

            return stringViews;
        }

        /// <summary>
        /// 按多个字符分割字符串为零分配的 StringView 数组。
        /// </summary>
        [HasGC]
        public static StringView[] SplitToStringViews(this string str, char[] splitChars, bool ignoreCase = false)
        {
            if (str.IsNullOrEmpty()) throw new ArgumentNullException(nameof(str));

            int splitCount = 0;
            foreach (var c in str)
            {
                foreach (var s in splitChars)
                {
                    if (c == s)
                    {
                        splitCount++;
                        break;
                    }
                }
            }

            var stringViews = new StringView[splitCount + 1];
            int idx = 0;
            int startIdx = 0;
            for (int i = 0; i < str.Length; i++)
            {
                foreach (var s in splitChars)
                {
                    if (str[i] == s)
                    {
                        stringViews[idx++] = new StringView(str, startIdx, i - startIdx, ignoreCase);
                        startIdx = i + 1;
                        break;
                    }
                }
            }

            if (startIdx < str.Length)
                stringViews[idx] = new StringView(str, startIdx, str.Length - startIdx, ignoreCase);

            return stringViews;
        }
    }
}
