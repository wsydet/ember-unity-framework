// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ember.Basic
{
    /// <summary>
    /// 加密与哈希工具 —— CRC32C、MD5、Base64、XOR 混淆等常用算法。
    ///
    /// 所有方法均为静态方法，标注了 GC 分配情况（[NoGC] / [HasGC]），
    /// 热路径上注意避开 [HasGC] 的方法。
    ///
    /// 用法：
    /// <code>
    /// int crc = CryptographyUtils.ComputeCrc32("hello");
    /// string md5 = CryptographyUtils.GetMD5("hello");
    /// string b64 = CryptographyUtils.EncodeBase64(Encoding.UTF8.GetBytes("hello"));
    /// </code>
    /// </summary>
    public static class CryptographyUtils
    {
        #region 内部参数

        private const string TAG = LogTags.BasicCrypto;

        // CRC32C Castagnoli reflected polynomial，对齐 Java java.util.zip.CRC32C 的字节更新规则
        private const uint Crc32CPolynomial = 0x82F63B78U;

        // 预生成查表可避免每次调用都逐 bit 计算 CRC32C
        private static readonly uint[] Crc32CTable = CreateCrc32CTable();

        #endregion

        // ============================================================

        #region CRC32C

        /// <summary>
        /// 计算字符串的 CRC32C（Castagnoli）哈希值。
        /// 使用 UTF-8 编码将字符串转为字节后计算。
        /// </summary>
        [HasGC]
        public static int ComputeCrc32(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            return ComputeCrc32(bytes);
        }

        /// <summary>
        /// 计算字节数组的 CRC32C（Castagnoli）哈希值。
        /// </summary>
        [NoGC]
        public static int ComputeCrc32(byte[] bytes)
        {
            return ComputeCrc32(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// 计算字节数组指定范围的 CRC32C（Castagnoli）哈希值。
        /// </summary>
        /// <param name="bytes">源字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">参与计算的长度</param>
        /// <exception cref="ArgumentOutOfRangeException">length 为负数时抛出</exception>
        [NoGC]
        public static int ComputeCrc32(byte[] bytes, int offset, int length)
        {
            if (length == 0) return 0;

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            uint crc = 0xFFFFFFFFU;
            for (int i = 0; i < length; i++)
            {
                UpdateCrc32C(ref crc, bytes[offset + i]);
            }

            return (int)~crc;
        }

        /// <summary>
        /// 更新 CRC32C 计算 —— 单字节。
        /// 实现 Castagnoli 多项式查表法，与 Java CRC32C 行为一致。
        /// </summary>
        [NoGC]
        private static void UpdateCrc32C(ref uint crc, byte value)
        {
            crc = (crc >> 8) ^ Crc32CTable[(int)((crc ^ value) & 0xFFU)];
        }

        /// <summary>
        /// 预生成 CRC32C 256 项查表。
        /// </summary>
        [HasGC]
        private static uint[] CreateCrc32CTable()
        {
            var table = new uint[256];
            for (int i = 0; i < table.Length; i++)
            {
                uint crc = (uint)i;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1U) != 0 ? (crc >> 1) ^ Crc32CPolynomial : crc >> 1;
                }
                table[i] = crc;
            }
            return table;
        }

        #endregion

        // ============================================================

        #region MD5

        /// <summary>
        /// 计算字符串的 MD5 哈希（UTF-8 编码），返回小写十六进制字符串。
        /// </summary>
        [HasGC]
        public static string GetMD5(string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);
            return GetMD5(bytes);
        }

        /// <summary>
        /// 计算字节数组的 MD5 哈希，返回小写十六进制字符串。
        /// </summary>
        [HasGC]
        public static string GetMD5(byte[] bytes)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes, 0, bytes.Length);
            return HashToHexString(hash);
        }

        /// <summary>
        /// 计算文件的 MD5 哈希，返回小写十六进制字符串。
        /// 打开文件的方式会影响 MD5 结果，请确保使用一致的 FileStream 方式。
        /// </summary>
        [HasGC]
        public static string GetMD5File(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            var hash = md5.ComputeHash(stream);
            return HashToHexString(hash);
        }

        /// <summary>
        /// 将 MD5 哈希字节数组转为小写十六进制字符串。
        /// </summary>
        [HasGC]
        private static string HashToHexString(byte[] hash)
        {
            var builder = new StringBuilder();
            foreach (var b in hash)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        #endregion

        // ============================================================

        #region Base64

        /// <summary>
        /// 将 UTF-8 字符串进行 Base64 编码。
        /// </summary>
        [HasGC]
        public static string EncodeBase64(string text)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                EmberDebug.LogException(TAG, ex);
                return null;
            }
        }

        /// <summary>
        /// 将字节数组进行 Base64 编码。
        /// </summary>
        [HasGC]
        public static string EncodeBase64(byte[] bytes)
        {
            try
            {
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                EmberDebug.LogException(TAG, ex);
                return null;
            }
        }

        /// <summary>
        /// 将 Base64 字符串解码为 UTF-8 字符串。
        /// </summary>
        [HasGC]
        public static string DecodeBase64(string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                EmberDebug.LogException(TAG, ex);
                return null;
            }
        }

        #endregion

        // ============================================================

        #region 其他工具

        /// <summary>
        /// 将字节数组转为小写十六进制字符串。
        /// 与 MD5 摘要输出格式一致（每字节两位小写 hex）。
        /// </summary>
        [HasGC]
        public static string ArrayToHexString(byte[] array)
        {
            var sb = new StringBuilder();
            foreach (var b in array)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// 对字节数组进行 XOR 混淆（原地修改）。
        ///
        /// 使用随机 XOR 掩码对数据做轻量混淆，适用于非加密场景的数据保护。
        /// 返回的 reserve 值可在后续用于还原（需要业务层自行保存）。
        ///
        /// <b>注意：这不是加密算法，不可用于安全敏感场景。</b>
        /// </summary>
        /// <param name="seed">可变种子值，每次调用递增，影响返回的 reserve</param>
        /// <param name="data">待混淆的字节数组（原地修改）</param>
        /// <param name="offset">起始偏移</param>
        /// <returns>reserve 值，可用于还原时的元数据</returns>
        [NoGC]
        public static int Obfuscate(ref int seed, byte[] data, int offset = 0)
        {
            var xorMask = UnityEngine.Random.Range(0, int.MaxValue) % 91 + 9;

            for (var i = offset; i < data.Length; ++i)
                data[i] = (byte)(data[i] ^ xorMask);

            var rand16 = (++seed) << 16;
            var rand8 = (UnityEngine.Random.Range(0, int.MaxValue) % 200 + 9) << 8;
            var reserve = (rand16 | rand8) | xorMask;

            return reserve;
        }

        #endregion
    }
}
