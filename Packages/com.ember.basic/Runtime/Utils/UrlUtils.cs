// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.IO;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// URL 工具集 —— URL 编解码、路径提取、URL 拼接等常用操作。
    ///
    /// 编解码使用 .NET 内置的 <see cref="Uri.EscapeDataString"/> /
    /// <see cref="Uri.UnescapeDataString"/>，符合 RFC 3986 标准。
    ///
    /// 用法：
    /// <code>
    /// var encoded = UrlUtils.UrlEncode("hello world");        // "hello%20world"
    /// var decoded = UrlUtils.UrlDecode("hello%20world");      // "hello world"
    /// var name    = UrlUtils.GetFileName("path/to/file.png"); // "file"
    /// var url     = UrlUtils.CheckURLString("http://host");   // "http://host/"
    /// </code>
    /// </summary>
    public static class UrlUtils
    {
        #region 内部参数

        private static readonly char DirectorySeparator = Path.DirectorySeparatorChar;

        #endregion

        // ============================================================

        #region 编解码

        /// <summary>
        /// URL 编码（percent-encoding），符合 RFC 3986。
        ///
        /// 使用 <see cref="Uri.EscapeDataString"/>，会对所有非 ASCII 及保留字符进行编码。
        /// 适用于 query string 参数值编码。
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>编码后的字符串</returns>
        [HasGC]
        public static string UrlEncode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return Uri.EscapeDataString(str);
        }

        /// <summary>
        /// URL 解码（percent-decoding）。
        ///
        /// 使用 <see cref="Uri.UnescapeDataString"/>，将 %XX 格式的编码还原为原始字符。
        /// </summary>
        /// <param name="str">编码后的字符串</param>
        /// <returns>解码后的字符串</returns>
        [HasGC]
        public static string UrlDecode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return Uri.UnescapeDataString(str);
        }

        #endregion

        // ============================================================

        #region 文件名提取

        /// <summary>
        /// 从 URL / 路径中提取文件名（不含扩展名）。
        ///
        /// 同时支持正斜杠和反斜杠作为路径分隔符。
        /// </summary>
        /// <param name="url">URL 或文件路径</param>
        /// <returns>不含扩展名的文件名</returns>
        [HasGC]
        public static string GetFileName(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            // Path.GetFileNameWithoutExtension 在 Windows 上同时支持 / 和 \ 分隔符
            return Path.GetFileNameWithoutExtension(url);
        }

        #endregion

        // ============================================================

        #region 路径处理

        /// <summary>
        /// 计算文件路径相对于文件夹路径的相对路径。
        /// </summary>
        /// <param name="filespec">文件完整路径</param>
        /// <param name="folder">文件夹路径</param>
        /// <returns>相对路径；如果两个路径不在同一根下，返回原始 filespec</returns>
        [HasGC]
        public static string GetRelativePath(string filespec, string folder)
        {
            var pathUri = new Uri(filespec);

            if (!folder.EndsWith(DirectorySeparator.ToString()))
                folder += DirectorySeparator;

            var folderUri = new Uri(folder);

            if (pathUri.AbsolutePath[0] != folderUri.AbsolutePath[0])
                return filespec;

            return Uri.UnescapeDataString(
                folderUri.MakeRelativeUri(pathUri).ToString().Replace("/", DirectorySeparator.ToString()));
        }

        #endregion

        // ============================================================

        #region URL 拼接

        /// <summary>
        /// 检查并确保 URL 以 "/" 结尾。
        /// </summary>
        [HasGC]
        public static string EnsureTrailingSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "/";

            if (!url.EndsWith("/"))
                return url + "/";

            return url;
        }

        /// <summary>
        /// 安全拼接两个 URL 片段，确保中间有且仅有一个 "/"。
        /// </summary>
        [HasGC]
        public static string CombineUrl(string baseUrl, string relativeUrl)
        {
            baseUrl = EnsureTrailingSlash(baseUrl);
            return string.Concat(baseUrl, relativeUrl.TrimStart('/'));
        }

        #endregion

        // ============================================================

        #region 缓存破坏

        /// <summary>
        /// 给 URL 追加随机参数用于破坏浏览器/CDN 缓存。
        ///
        /// 检测 URL 中是否已有 query string（"?"），自动选择 "?" 或 "&" 连接。
        /// 返回的 URL 末尾追加参数如 "?v=0.314159" 或 "&v=0.314159"。
        /// </summary>
        /// <param name="url">原始 URL</param>
        /// <returns>带随机版本参数的 URL</returns>
        [HasGC]
        public static string AppendRandomVersion(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var randomValue = UnityEngine.Random.Range(0f, 1f).ToString("F6");
            var connector = url.LastIndexOf('?') == -1 ? "?v=" : "&v=";
            return url + connector + randomValue;
        }

        #endregion
    }
}
