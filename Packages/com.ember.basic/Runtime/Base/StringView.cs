//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using System.Collections.Generic;
//
//namespace Burner.Basic
//{
//    public struct StringView 
//    {
//        private readonly string _str;
//        private readonly int _start;
//        private readonly int _length;
//
//        public string OriginString => _str;
//        public int OriginLength => _str.IsNullOrEmpty() ? 0 : _str.Length;
//        public int Start => _start;
//        public int Length => _length;
//        public bool IgnoreCaseCompare { get; set; }
//        
//        private int _hashCode;
//        
//        public StringView(string str, bool ignoreCaseCompare = false, bool calcHash = false) : this(str, 0, str.Length, ignoreCaseCompare, calcHash)
//        {
//        }
//
//        public StringView(string str, int start, int length, bool ignoreCaseCompare = false, bool calcHash = false)
//        {
//            if(str == null)
//            {
//                throw new ArgumentException("[StringView]: str is null");
//            }
//            
//            if(start < 0 || length < 0 || length > str.Length - start)
//            {
//                throw new ArgumentException($"[StringView]: {str}, {start}, {length}");
//            }
//            
//            _str = str;
//            _start = start;
//            _length = length;
//            IgnoreCaseCompare = ignoreCaseCompare;
//            _hashCode = calcHash ? CalcHashCode(_str, _start, _length, ignoreCaseCompare) : 0;
//        }
//        
//        [HasGC()]
//        public override string ToString() => _str.Substring(_start, _length);
//
//        private static int CalcHashCode(string str, int start, int length, bool ignoreCaseCompare)
//        {
//            int hash = 0;
//            for (int i = 0; i < length; i++)
//            {
//                var c = str[i + start];
//                    
//                if(ignoreCaseCompare) c = StringExtension.ToAlphaLower(c);
//                hash = 31 * hash + c;
//            }
//
//            return hash;
//        }
//        public override int GetHashCode()
//        {
//            if(_hashCode == 0)
//            {
//                _hashCode = CalcHashCode(_str, _start, _length, IgnoreCaseCompare);
//            }
//            
//            return _hashCode;
//        }
//        
//        public static bool operator == (StringView a, StringView b) => a.Equals(b);
//        public static bool operator != (StringView a, StringView b) => !a.Equals(b);
//        
//        public static bool operator == (StringView a, string b) => a.Equals(b);
//        public static bool operator != (StringView a, string b) => !a.Equals(b);
//        
//        public static bool operator == (string a, StringView b) => b.Equals(a);
//        public static bool operator != (string a, StringView b) => !b.Equals(a);
//
//        public char this[int index] => _str[index + _start];
//        
//        public bool IsNullOrEmpty() => _str == null || _length == 0;
//        
//        public override bool Equals(object obj)
//        {
//            if(obj is StringView other && Equals(other))
//            {
//                return true;
//            }
//
//            if(obj is string otherStr)
//            {
//                return Equals(otherStr);
//            }
//
//            return false;
//        }
//        
//        public bool Equals(string other)
//        {
//            if(_length != other.Length) return false;
//            for(var i = 0; i < _length; i++)
//            {
//                if(IgnoreCaseCompare)
//                {
//                    if(StringExtension.ToAlphaLower(_str[i + _start]) != StringExtension.ToAlphaLower(other[i]))
//                    {
//                        return false;
//                    }
//                }
//                else
//                {
//                    if(_str[i + _start] != other[i])
//                    {
//                        return false;
//                    }
//                }  
//            }
//
//            return true;
//        }
//        
//        public bool Equals(StringView other)
//        {
//            if(_length != other._length) return false;
//
//            for(int i = 0; i < _length; i++)
//            {
//                if(IgnoreCaseCompare || other.IgnoreCaseCompare)
//                {
//                    if(StringExtension.ToAlphaLower(_str[_start + i]) != StringExtension.ToAlphaLower(other._str[other._start + i]))
//                    {
//                        return false;
//                    }    
//                }
//                else
//                {
//                    if(_str[_start + i] != other._str[other._start + i])
//                    {
//                        return false;
//                    }
//                }
//            }
//            
//            return true;
//        }
//
//        public StringView Substring(int start, int length, bool calcHash = false) 
//            => new StringView(_str, _start + start, length, IgnoreCaseCompare, calcHash);
//    }
//}