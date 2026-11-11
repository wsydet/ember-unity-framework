// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ember.Core.Editor
{
    /// <summary>
    /// 模板文件系统边界。Windows 扩展路径仅传给 System.IO；枚举结果恢复调用方的普通路径，
    /// 不改变相对路径、AssetDatabase 路径、hash 或计划中的路径。仍受卷的组件长度及权限限制。
    /// </summary>
    internal static class EmberTemplateFileSystem
    {
        #region 内部方法
        internal static string NativePath(string path)
        {
            if (Path.DirectorySeparatorChar != '\\' || string.IsNullOrEmpty(path)) return path;
            string full = Path.GetFullPath(path).Replace('/', '\\');
            if (full.StartsWith(@"\\?\", StringComparison.Ordinal)) return full;
            return full.StartsWith(@"\\", StringComparison.Ordinal)
                ? @"\\?\UNC\" + full.Substring(2) : @"\\?\" + full;
        }

        private static string[] LogicalEntries(string root, string nativeRoot, string[] entries)
        {
            if (Path.DirectorySeparatorChar != '\\') return entries;
            int length = nativeRoot.TrimEnd('\\', '/').Length;
            for (int i = 0; i < entries.Length; i++)
                entries[i] = Path.Combine(root, entries[i].Substring(length).TrimStart('\\', '/'));
            return entries;
        }
        #endregion

        // --------------------------------------------------------

        #region 外部方法
        internal static class File
        {
            internal static bool Exists(string path) => System.IO.File.Exists(NativePath(path));
            internal static byte[] ReadAllBytes(string path) => System.IO.File.ReadAllBytes(NativePath(path));
            internal static string ReadAllText(string path) => System.IO.File.ReadAllText(NativePath(path));
            internal static string ReadAllText(string path, Encoding encoding) => System.IO.File.ReadAllText(NativePath(path), encoding);
            internal static string[] ReadAllLines(string path) => System.IO.File.ReadAllLines(NativePath(path));
            internal static IEnumerable<string> ReadLines(string path) => System.IO.File.ReadLines(NativePath(path));
            internal static void WriteAllBytes(string path, byte[] bytes) => System.IO.File.WriteAllBytes(NativePath(path), bytes);
            internal static void WriteAllLines(string path, IEnumerable<string> lines) => System.IO.File.WriteAllLines(NativePath(path), lines);
            internal static void WriteAllText(string path, string text, Encoding encoding) => System.IO.File.WriteAllText(NativePath(path), text, encoding);
            internal static void WriteAllText(string path, string text) => System.IO.File.WriteAllText(NativePath(path), text);
            internal static void Copy(string source, string destination, bool overwrite) => System.IO.File.Copy(NativePath(source), NativePath(destination), overwrite);
            internal static void Move(string source, string destination) => System.IO.File.Move(NativePath(source), NativePath(destination));
            internal static void Delete(string path) => System.IO.File.Delete(NativePath(path));
            internal static FileAttributes GetAttributes(string path) => System.IO.File.GetAttributes(NativePath(path));
        }

        internal static class Directory
        {
            internal static bool Exists(string path) => System.IO.Directory.Exists(NativePath(path));
            internal static void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(NativePath(path));
            internal static DirectoryInfo GetParent(string path) => System.IO.Directory.GetParent(path);
            internal static void Delete(string path, bool recursive = false) => System.IO.Directory.Delete(NativePath(path), recursive);
            internal static void Move(string source, string destination) => System.IO.Directory.Move(NativePath(source), NativePath(destination));
            internal static string[] GetFiles(string path, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetFiles(native, pattern, option));
            }
            internal static string[] GetDirectories(string path, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetDirectories(native, pattern, option));
            }
            internal static string[] GetFileSystemEntries(string path)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetFileSystemEntries(native));
            }
            internal static IEnumerable<string> EnumerateFileSystemEntries(string path) => GetFileSystemEntries(path);
        }
        #endregion
    }
}
