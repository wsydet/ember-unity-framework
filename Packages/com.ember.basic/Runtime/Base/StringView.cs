// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 字符串视图 —— 在不分配新字符串的前提下引用原字符串的一个子串。
    ///
    /// 用于解析、比较等场景中避免 <see cref="string.Substring(int, int)"/> 产生的 GC 分配。
    /// 支持与 string 和 StringView 的相等比较（含忽略大小写模式）。
    /// </summary>
    public struct StringView : IEquatable<StringView>, IEquatable<string>
    {
        private readonly string _str;
        private readonly int _start;
        private readonly int _length;
        private int _hashCode;

        /// <summary>原始字符串。</summary>
        public string OriginString => _str;

        /// <summary>原始字符串总长度。</summary>
        public int OriginLength => _str?.Length ?? 0;

        /// <summary>视图在原始字符串中的起始位置。</summary>
        public int Start => _start;

        /// <summary>视图长度。</summary>
        public int Length => _length;

        /// <summary>比较时是否忽略大小写。</summary>
        public bool IgnoreCaseCompare { get; set; }

        public StringView(string str, bool ignoreCaseCompare = false, bool calcHash = false)
            : this(str, 0, str.Length, ignoreCaseCompare, calcHash)
        {
        }

        public StringView(string str, int start, int length, bool ignoreCaseCompare = false, bool calcHash = false)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (start < 0 || length < 0 || length > str.Length - start)
                throw new ArgumentOutOfRangeException(
                    $"[StringView] Invalid range: str.Length={str.Length}, start={start}, length={length}");

            _str = str;
            _start = start;
            _length = length;
            IgnoreCaseCompare = ignoreCaseCompare;
            _hashCode = calcHash ? CalcHashCode(_str, _start, _length, ignoreCaseCompare) : 0;
        }

        public char this[int index] => _str[index + _start];

        public bool IsNullOrEmpty() => _str == null || _length == 0;

        // ======== Substring ========

        /// <summary>
        /// 基于当前视图创建新的子视图，不产生字符串分配。
        /// </summary>
        [HasGC]
        public StringView Substring(int start, int length, bool calcHash = false)
            => new(_str, _start + start, length, IgnoreCaseCompare, calcHash);

        // ======== ToString ========

        [HasGC]
        public override string ToString() => _str.Substring(_start, _length);

        // ======== HashCode ========

        public override int GetHashCode()
        {
            if (_hashCode == 0)
                _hashCode = CalcHashCode(_str, _start, _length, IgnoreCaseCompare);
            return _hashCode;
        }

        private static int CalcHashCode(string str, int start, int length, bool ignoreCaseCompare)
        {
            int hash = 0;
            for (int i = 0; i < length; i++)
            {
                var c = str[i + start];
                if (ignoreCaseCompare)
                    c = char.ToLowerInvariant(c);
                hash = 31 * hash + c;
            }

            return hash;
        }

        // ======== Equality ========

        public override bool Equals(object obj)
        {
            if (obj is StringView other && Equals(other))
                return true;
            if (obj is string otherStr)
                return Equals(otherStr);
            return false;
        }

        public bool Equals(string other)
        {
            if (other == null) return false;
            if (_length != other.Length) return false;

            for (int i = 0; i < _length; i++)
            {
                var a = _str[i + _start];
                var b = other[i];

                if (IgnoreCaseCompare)
                {
                    if (char.ToLowerInvariant(a) != char.ToLowerInvariant(b))
                        return false;
                }
                else
                {
                    if (a != b) return false;
                }
            }

            return true;
        }

        public bool Equals(StringView other)
        {
            if (_length != other._length) return false;

            for (int i = 0; i < _length; i++)
            {
                var a = _str[_start + i];
                var b = other._str[other._start + i];

                if (IgnoreCaseCompare || other.IgnoreCaseCompare)
                {
                    if (char.ToLowerInvariant(a) != char.ToLowerInvariant(b))
                        return false;
                }
                else
                {
                    if (a != b) return false;
                }
            }

            return true;
        }

        // ======== Operators ========

        public static bool operator ==(StringView a, StringView b) => a.Equals(b);
        public static bool operator !=(StringView a, StringView b) => !a.Equals(b);
        public static bool operator ==(StringView a, string b) => a.Equals(b);
        public static bool operator !=(StringView a, string b) => !a.Equals(b);
        public static bool operator ==(string a, StringView b) => b.Equals(a);
        public static bool operator !=(string a, StringView b) => !b.Equals(a);
    }
}
