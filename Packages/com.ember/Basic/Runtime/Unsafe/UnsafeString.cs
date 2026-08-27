// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember
//
// WARNING: This file uses unsafe code to directly manipulate string internals.
// Only use in performance-critical paths where GC allocation must be avoided.

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 字符串的 unsafe 操作扩展。直接操作字符串内部 char 缓冲区，避免分配。
    /// 调用者必须确保字符串有足够容量。
    /// </summary>
    public static class UnsafeStringExtensions
    {
        /// <summary>
        /// 将 UTF-8 字节流直接解码写入字符串的内部 char 缓冲区。
        /// 调用者必须确保 string.Length >= 解码后的字符数。
        /// </summary>
        public static unsafe void CopyFromUTF8ByteBuffer(this string str, byte* src, int sizeInBytes)
        {
            fixed (char* dest = str)
            {
                int srcIdx = 0;
                int destIdx = 0;
                while (srcIdx < sizeInBytes)
                {
                    dest[destIdx++] = AnsiToUnicode(src, ref srcIdx);
                    srcIdx++;
                }

                dest[destIdx] = '\0';
                // Mono/Unity string layout: length is stored as int32 right before the first char
                *((int*)dest - 1) = destIdx;
            }
        }

        /// <summary>
        /// 计算以 null 结尾的 UTF-8 字符串的字符数。
        /// </summary>
        public static unsafe int Utf8Length(byte* ntcs)
        {
            if (ntcs == null) return 0;

            int charCount = 0;
            byte ch;
            while ((ch = *ntcs) != 0x0)
            {
                if (0x80 != (0xC0 & ch))
                    ++charCount;
                ++ntcs;
            }

            return charCount;
        }

        /// <summary>
        /// 将 UTF-8 字节序列解码为单个 Unicode 字符，并推进索引。
        /// </summary>
        public static unsafe char AnsiToUnicode(byte* src, ref int i)
        {
            var c = *(src + i);

            if (c >= 0xe0)
            {
                var c1 = *(src + i + 1);
                var c2 = *(src + i + 2);
                i += 2;
                return (char)(((c & 0x1f) << 12) + ((c1 & 0x3f) << 6) + (c2 & 0x3f));
            }

            if (c >= 0xc0)
            {
                var c1 = *(src + i + 1);
                i++;
                return (char)(((c & 0x3f) << 6) + (c1 & 0x3f));
            }

            return (char)c;
        }
    }
}
