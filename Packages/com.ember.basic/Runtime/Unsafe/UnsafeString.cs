//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//
//public static class UnsafeString 
//{
//    /// <summary>
//    /// Decode UTF-8 bytes and write directly into the string's internal char buffer.
//    /// The caller must ensure the string has enough capacity (string.Length >= decoded char count).
//    /// </summary>
//    public static unsafe void CopyFromUTF8ByteBuffer(this string str, byte* src, int sizeInBytes)
//    {
//        fixed (char* dest = str)
//        {
//            int srcIdx = 0;
//            int destIdx = 0;
//            while (srcIdx < sizeInBytes)
//            {
//                dest[destIdx++] = ansi2unicode(src, ref srcIdx);
//                srcIdx++;
//            }
//            dest[destIdx] = '\0';
//            // Mono string layout: length is stored as int32 right before the first char
//            *((int*)dest - 1) = destIdx;
//        }
//    }
//
//    public static unsafe int utf8len(byte* ntcs)
//    {
//        if (ntcs != null)
//        {
//            int charCount = 0;
//            byte ch = 0;
//            while ((ch = * ntcs) != 0x0)
//            {
//                if (0x80 != (0xC0 & ch))
//                {
//                    ++charCount;
//                }
//
//                ++ntcs;
//            }
//
//            return charCount;
//        }
//
//        return 0;
//    }
//
//    public static unsafe char ansi2unicode(byte* src, ref int i)
//    {
//        var c = *(src + i);
//
//        if (c >= 0xfc)
//        {
//            
//        }
//        else if (c >= 0xf8)
//        {
//            
//        }
//        else if (c >= 0xf0)
//        {
//            
//        }
//        else if (c >= 0xe0)
//        {
//            var c1 = *(src + i + 1);
//            var c2 = *(src + i + 2);
//            i += 2;
//            return (char)(((c & 0x1f) << 12) + ((c1 & 0x3f) << 6) + (c2 & 0x3f));
//        }
//        else if (c >= 0xc0)
//        {
//            var c1 = *(src + i + 1);
//            i++;
//            return (char)(((c& 0x3f) << 6) + (c1 & 0x3f));
//        }
//
//        return (char)c;
//    }
//    
//}