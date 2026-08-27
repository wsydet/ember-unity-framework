// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 原生内存数据视图 —— 持有 IntPtr + 长度的轻量结构体。
    /// </summary>
    public struct NativeDataView
    {
        public IntPtr Ptr { get; private set; }
        public int Length { get; set; }
        public bool Managed { get; private set; }

        public static readonly NativeDataView Null = new(IntPtr.Zero, 0);

        public NativeDataView(IntPtr ptr, int length, bool managed = false)
        {
            Ptr = ptr;
            Length = length;
            Managed = managed;
        }

        public void Set(IntPtr ptr, int length, bool managed = false)
        {
            Ptr = ptr;
            Length = length;
            Managed = managed;
        }

        public bool IsNull() => Ptr == IntPtr.Zero;
        public bool IsEmpty() => Ptr == IntPtr.Zero || Length == 0;

        public void SetNull()
        {
            Ptr = IntPtr.Zero;
            Length = 0;
        }

        public override int GetHashCode() => Ptr.GetHashCode() + Length;
    }

    /// <summary>
    /// 原生 UDT（用户自定义类型）视图 —— 仅持有 IntPtr。
    /// </summary>
    public struct NativeUDTView
    {
        public IntPtr Ptr;

        public static readonly NativeUDTView Null = new(IntPtr.Zero);

        public NativeUDTView(IntPtr ptr) => Ptr = ptr;

        public bool IsNull() => Ptr == IntPtr.Zero;

        public void Set(IntPtr ptr) => Ptr = ptr;
    }
}
