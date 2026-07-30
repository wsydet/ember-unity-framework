//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//
//namespace Burner.Basic
//{
//	public struct NativeDataView
//	{
//		public IntPtr ptr { get; private set; }
//		public int len { get; set; }
//		public bool managed { get; private set; }
//
//		public NativeDataView(IntPtr p, int l, bool managed = false)
//		{
//			ptr = p;
//			len = l;
//			this.managed = managed;
//		}
//
//		public void Set(IntPtr p, int l, bool managed = false)
//		{
//			ptr = p;
//			len = l;
//			this.managed = managed;
//		}
//
//		public bool IsNull()
//		{
//			return ptr == IntPtr.Zero;
//		}
//		
//		public void SetNull()
//		{
//			ptr = IntPtr.Zero;
//			len = 0;
//		}
//
//		public bool IsEmpty()
//		{
//			return ptr == IntPtr.Zero || len == 0;
//		}
//
//		public static readonly NativeDataView NullValue = new NativeDataView(IntPtr.Zero, 0);
//
//		public override int GetHashCode()
//		{
//			return ptr.GetHashCode() + len;
//		}
//	}
//
//	public struct NativeUDTView
//	{
//		public IntPtr ptr;
//		public static readonly NativeUDTView NullValue = new NativeUDTView(IntPtr.Zero);
//		public NativeUDTView(IntPtr p)
//		{
//			ptr = p;
//		}
//		public bool IsNull()
//		{
//			return ptr == IntPtr.Zero;
//		}
//		public void Set(IntPtr p)
//		{
//			ptr = p;
//		}
//	}
//}