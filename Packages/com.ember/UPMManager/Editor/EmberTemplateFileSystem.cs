// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ember.UPMManager.Editor
{
    /// <summary>
    /// 模板文件系统边界。Windows 扩展路径仅传给 System.IO；枚举结果恢复调用方的普通路径，
    /// 不改变相对路径、AssetDatabase 路径、hash 或计划中的路径。仍受卷的组件长度及权限限制。
    /// </summary>
    public static class EmberTemplateFileSystem
    {
        #region 内部方法
        internal static string NativePath(string path)
        {
            if (Path.DirectorySeparatorChar != '\\' || string.IsNullOrEmpty(path)) return path;
            string logical = path.Replace('/', '\\');
            if (logical.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) logical = @"\\" + logical.Substring(8);
            else if (logical.StartsWith(@"\\?\", StringComparison.Ordinal)
                && logical.Length >= 7 && char.IsLetter(logical[4]) && logical[5] == ':' && logical[6] == '\\') logical = logical.Substring(4);
            else if (logical.StartsWith(@"\\?\", StringComparison.Ordinal) || logical.StartsWith(@"\\.\", StringComparison.Ordinal))
                throw new ArgumentException("模板 IO 拒绝设备路径：" + path);
            foreach (string component in logical.Split('\\'))
                if (component != "." && component != ".." && (component.EndsWith(" ", StringComparison.Ordinal) || component.EndsWith(".", StringComparison.Ordinal)))
                    throw new ArgumentException("模板 IO 拒绝尾空格/尾点路径：" + path);
            string full = Path.GetFullPath(logical).Replace('/', '\\');
            return full.StartsWith(@"\\", StringComparison.Ordinal)
                ? @"\\?\UNC\" + full.Substring(2) : @"\\?\" + full;
        }

        private static string[] LogicalEntries(string root, string nativeRoot, string[] entries)
        {
            if (Path.DirectorySeparatorChar != '\\') return entries;
            int length = nativeRoot.TrimEnd('\\', '/').Length;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].StartsWith(nativeRoot.TrimEnd('\\', '/') + "\\", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("文件枚举超出请求根目录：" + root);
                entries[i] = Path.Combine(root, entries[i].Substring(length).TrimStart('\\', '/'));
            }
            return entries;
        }

        private static T Access<T>(string operation, string path, Func<T> action)
        {
            try { return action(); }
            catch (Exception error) { error.Data["EmberIOOperation"] = operation; error.Data["EmberIOPath"] = path; throw; }
        }

        private static void Access(string operation, string path, Action action)
        {
            Access(operation, path, () => { action(); return true; });
        }

        private static IEnumerable<string> ReadLines(string path)
        {
            using var iterator = Access("ReadLines", path, () => System.IO.File.ReadLines(NativePath(path)).GetEnumerator());
            while (Access("ReadLines", path, () => iterator.MoveNext())) yield return iterator.Current;
        }

        private static bool Exists(string path, bool directory)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try { return (System.IO.File.GetAttributes(NativePath(path)) & FileAttributes.Directory) != 0 ? directory : !directory; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }

        /// <summary>原始文件 MD5；仅最终流边界转换路径，不作文本规范化。</summary>
        public static string RawMD5(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = System.Security.Cryptography.MD5.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        #endregion

        // --------------------------------------------------------

        #region 外部方法
        public static class File
        {
            public static bool Exists(string path) => Access("Exists", path, () => EmberTemplateFileSystem.Exists(path, false));
            public static byte[] ReadAllBytes(string path) => Access("ReadAllBytes", path, () => System.IO.File.ReadAllBytes(NativePath(path)));
            public static string ReadAllText(string path) => Access("ReadAllText", path, () => System.IO.File.ReadAllText(NativePath(path)));
            public static string ReadAllText(string path, Encoding encoding) => Access("ReadAllText", path, () => System.IO.File.ReadAllText(NativePath(path), encoding));
            public static string[] ReadAllLines(string path) => Access("ReadAllLines", path, () => System.IO.File.ReadAllLines(NativePath(path)));
            public static IEnumerable<string> ReadLines(string path) => Access("ReadLines", path, () => EmberTemplateFileSystem.ReadLines(path));
            public static void WriteAllBytes(string path, byte[] bytes) => Access("WriteAllBytes", path, () => System.IO.File.WriteAllBytes(NativePath(path), bytes));
            public static void WriteAllLines(string path, IEnumerable<string> lines, Encoding encoding) => Access("WriteAllLines", path, () => System.IO.File.WriteAllLines(NativePath(path), lines, encoding));
            public static void WriteAllLines(string path, IEnumerable<string> lines) => Access("WriteAllLines", path, () => System.IO.File.WriteAllLines(NativePath(path), lines));
            public static void WriteAllText(string path, string text, Encoding encoding) => Access("WriteAllText", path, () => System.IO.File.WriteAllText(NativePath(path), text, encoding));
            public static void WriteAllText(string path, string text) => Access("WriteAllText", path, () => System.IO.File.WriteAllText(NativePath(path), text));
            public static void Copy(string source, string destination, bool overwrite = false) => Access("Copy", source + " → " + destination, () => System.IO.File.Copy(NativePath(source), NativePath(destination), overwrite));
            public static void Move(string source, string destination) => Access("Move", source + " → " + destination, () => System.IO.File.Move(NativePath(source), NativePath(destination)));
            public static void Delete(string path) => Access("Delete", path, () => System.IO.File.Delete(NativePath(path)));
            public static FileStream OpenRead(string path, FileShare share = FileShare.Read) => Access("OpenRead", path, () => new FileStream(NativePath(path), FileMode.Open, FileAccess.Read, share));
            public static FileStream CreateNewLease(string path) => Access("CreateNewLease", path, () => new FileStream(NativePath(path), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read));
            public static long Length(string path) => Access("Length", path, () => new FileInfo(NativePath(path)).Length);
            public static DateTime GetLastWriteTimeUtc(string path) => Access("GetLastWriteTimeUtc", path, () => System.IO.File.GetLastWriteTimeUtc(NativePath(path)));
            public static FileAttributes GetAttributes(string path) => Access("GetAttributes", path, () => System.IO.File.GetAttributes(NativePath(path)));
        }

        public static class Directory
        {
            public static bool Exists(string path) => Access("Exists", path, () => EmberTemplateFileSystem.Exists(path, true));
            public static void CreateDirectory(string path) => Access("CreateDirectory", path, () => System.IO.Directory.CreateDirectory(NativePath(path)));
            public static DirectoryInfo GetParent(string path) => System.IO.Directory.GetParent(path);
            public static void Delete(string path, bool recursive = false) => Access("Delete", path, () => System.IO.Directory.Delete(NativePath(path), recursive));
            public static void Move(string source, string destination) => Access("Move", source + " → " + destination, () => System.IO.Directory.Move(NativePath(source), NativePath(destination)));
            public static string[] GetFiles(string path, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetFiles(native, pattern, option));
            }
            public static string[] GetDirectories(string path, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetDirectories(native, pattern, option));
            }
            public static string[] GetFileSystemEntries(string path)
            {
                string native = NativePath(path);
                return LogicalEntries(path, native, System.IO.Directory.GetFileSystemEntries(native));
            }
            public static IEnumerable<string> EnumerateFileSystemEntries(string path) => Access("EnumerateFileSystemEntries", path, () => GetFileSystemEntries(path));
        }
        #endregion
    }
}
