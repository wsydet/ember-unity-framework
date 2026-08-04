// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System.Collections.Generic;

namespace Ember.Basic
{
    /// <summary>
    /// 自然排序比较器 —— 把数字当数值比，而不是当字符比。
    ///
    /// C# 默认的字符串排序是字典序： "Frame_10" 排在 "Frame_2" 前面（因为 '1' &lt; '2'）。
    /// 自然排序把 "10" 当整数 10 比 "2" 当整数 2： "Frame_2" 排在 "Frame_10" 前面。
    ///
    /// 使用方式：
    /// <code>
    /// var files = Directory.GetFiles(path);
    /// Array.Sort(files, NaturalStringComparer.Instance);
    /// </code>
    /// </summary>
    public sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new();

        /// <summary>
        /// 自然排序比较。内部有 Substring 分配，但排序算法调用它的次数是 O(n log n)，
        /// 只要不每帧对大量字符串排序就没问题。
        /// </summary>
        [HasGC]
        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int xi = 0, yi = 0;
            while (xi < x.Length && yi < y.Length)
            {
                char xc = x[xi], yc = y[yi];

                // 两个都是数字 → 当成整数比
                if (char.IsDigit(xc) && char.IsDigit(yc))
                {
                    int cmp = CompareNumber(x, ref xi, y, ref yi);
                    if (cmp != 0) return cmp;
                    continue;
                }

                // 至少一个不是数字 → 当成字符比（忽略大小写）
                int cc = char.ToUpperInvariant(xc).CompareTo(char.ToUpperInvariant(yc));
                if (cc != 0) return cc;

                xi++; yi++;
            }

            // 走到这里说明公共前缀完全相同，谁长谁排后面
            return (x.Length - xi).CompareTo(y.Length - yi);
        }

        private static int CompareNumber(string x, ref int xi, string y, ref int yi)
        {
            int xs = xi, ys = yi;
            while (xi < x.Length && char.IsDigit(x[xi])) xi++;
            while (yi < y.Length && char.IsDigit(y[yi])) yi++;

            string xn = TrimLeadingZeros(x.Substring(xs, xi - xs));
            string yn = TrimLeadingZeros(y.Substring(ys, yi - ys));

            // 先比位数
            int lc = xn.Length.CompareTo(yn.Length);
            if (lc != 0) return lc;

            // 位数相同，逐位比
            int vc = string.CompareOrdinal(xn, yn);
            if (vc != 0) return vc;

            // 数值完全相同（如 "01" vs "1"）→ 原始长度短的排前面
            return (xi - xs).CompareTo(yi - ys);
        }

        private static string TrimLeadingZeros(string s)
        {
            string t = s.TrimStart('0');
            return t.Length == 0 ? "0" : t;
        }
    }
}
