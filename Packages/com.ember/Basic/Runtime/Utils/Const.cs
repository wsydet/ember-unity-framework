// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System.Text;

namespace Ember.Basic
{
    /// <summary>
    /// 预分配的常量/共享对象，用于避免频繁分配产生的 GC。
    /// 注意：<see cref="sb"/> 和 <see cref="sb2"/> 是共享的可变对象，使用前需 Clear。
    /// </summary>
    public static class SharedConst
    {
        public static readonly string[] EmptyStringArray = new string[0];
        public static readonly uint[] EmptyUintArray = new uint[0];
        public static readonly uint[] ZeroUintArray = new uint[] { 0 };
        public static readonly string[] LineSeparators = new string[] { "\r\n", "\n" };

        /// <summary>共享 StringBuilder，使用时需先 Clear。</summary>
        public static readonly StringBuilder SharedStringBuilder = new();

        /// <summary>备用 StringBuilder，使用时需先 Clear。</summary>
        public static readonly StringBuilder SharedStringBuilder2 = new();
    }
}
