// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

#if UNITY_EDITOR

using System.IO;
using System.Text;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 文件编码工具 —— 检测和转换 UTF-8 BOM。
    ///
    /// 场景：Unity 脚本文件要求 UTF-8 BOM 编码。从外部导入或旧项目迁移的脚本可能是
    /// ANSI/GBK 编码，导致中文注释乱码。用这个工具批量检测和转换。
    /// </summary>
    public static class FileEncodingUtility
    {
        /// <summary>
        /// 检查文件是否包含 UTF-8 BOM 头（EF BB BF）。
        /// </summary>
        public static bool HasBOM(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                if (fs.Length < 3) return false;
                byte[] bom = new byte[3];
                fs.Read(bom, 0, 3);
                return bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将文件转换为 UTF-8 BOM 编码。
        ///
        /// 自动检测原编码：
        /// 1. 已经是 UTF-8 BOM → 跳过
        /// 2. 是 UTF-8 无 BOM → 加 BOM 保存
        /// 3. 是 ANSI/GBK → 用 GB2312 解码 → 转 UTF-8 BOM 保存
        /// </summary>
        public static void ConvertToUTF8BOM(string path)
        {
            if (HasBOM(path)) return;

            string text = "";
            bool readSuccess = false;

            // 先尝试用 UTF-8 读取（遇到非法字节会抛异常）
            try
            {
                using var reader = new StreamReader(path, new UTF8Encoding(false, true));
                text = reader.ReadToEnd();
                readSuccess = true;
            }
            catch
            {
                readSuccess = false;
            }

            // UTF-8 读失败 → 尝试 GB2312（中文编码）
            if (!readSuccess)
            {
                Encoding gbk = Encoding.GetEncoding("GB2312");
                text = File.ReadAllText(path, gbk);
            }

            // 写入 UTF-8 BOM
            File.WriteAllText(path, text, new UTF8Encoding(true));
        }
    }
}

#endif
