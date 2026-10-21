using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>共享的源表位置策略。只保存相对 Unity 根目录的路径。</summary>
    public static class EmberTableSourceFiles
    {
        #region 内部参数
        public const string CONFIG_PATH = "ProjectSettings/EmberTableSources.json";
        [Serializable] public sealed class Configuration
        {
            public string[] allowedRoots = Array.Empty<string>();
            public string defaultRoot = "Assets/GameResource/TableSources";
            public Binding[] bindings = Array.Empty<Binding>();
        }
        [Serializable] public sealed class Binding { public string tableId; public string path; }
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static StringComparison Comparison => Application.platform == RuntimePlatform.WindowsEditor
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static Configuration ReadConfiguration()
        {
            string path = System.IO.Path.Combine(ProjectRoot, CONFIG_PATH);
            return File.Exists(path) ? JsonUtility.FromJson<Configuration>(File.ReadAllText(path))
                ?? throw new InvalidOperationException("源表配置无效：" + CONFIG_PATH) : new Configuration();
        }

        public static string BindingPath(TextAsset legacy, string externalPath, string tableId = null)
        {
            var matches = (ReadConfiguration().bindings ?? Array.Empty<Binding>()).Where(b => b != null && b.tableId == tableId && tableId != null).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("重复源表映射：" + tableId);
            string path = matches.Length == 1 ? matches[0].path :
                !string.IsNullOrWhiteSpace(externalPath) ? externalPath : AssetDatabase.GetAssetPath(legacy);
            if (matches.Length == 1 && !string.IsNullOrWhiteSpace(externalPath) && !SameFile(path, externalPath))
                throw new InvalidOperationException("源表配置映射与资产外部绑定冲突：" + tableId);
            Resolve(path);
            return path.Replace('\\', '/');
        }

        public static bool HasBinding(string tableId) => (ReadConfiguration().bindings ?? Array.Empty<Binding>())
            .Any(b => b != null && b.tableId == tableId);

        public static string PathForTable(string tableId, string fileName)
        {
            var definitions = EmberTablePipeline.FindAllDefinitions().Where(d => d.TableId == tableId).ToArray();
            if (definitions.Length > 1) throw new InvalidOperationException("重复 Definition：" + tableId);
            return definitions.Length == 1 ? definitions[0].SourcePath : HasBinding(tableId)
                ? BindingPath(null, null, tableId) : DefaultPath(fileName);
        }

        public static string DefaultPath(string fileName)
        {
            if (System.IO.Path.GetFileName(fileName) != fileName || fileName.Contains("\\"))
                throw new InvalidOperationException("源表文件名不能包含目录。");
            string path = ReadConfiguration().defaultRoot.TrimEnd('/', '\\') + "/" + fileName;
            Resolve(path);
            return path;
        }

        public static string Resolve(string path)
        {
            string normalized = Relative(path);
            string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectRoot, normalized));
            var config = ReadConfiguration();
            bool allowed = Within(full, System.IO.Path.Combine(ProjectRoot, "Assets"))
                || Within(full, System.IO.Path.Combine(ProjectRoot, "Packages"));
            foreach (string root in config.allowedRoots ?? Array.Empty<string>())
            {
                string candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectRoot, Relative(root)));
                if (candidate == System.IO.Path.GetPathRoot(candidate) || candidate.TrimEnd('/', '\\') == ProjectRoot.TrimEnd('/', '\\'))
                    throw new InvalidOperationException("源表根目录不能是磁盘或 Unity 工程根目录。");
                if (Within(full, candidate)) allowed = true;
            }
            if (!allowed) throw new InvalidOperationException("源表越出允许目录：" + path);
            string extension = System.IO.Path.GetExtension(full);
            if (!extension.Equals(".csv", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("源表必须为 CSV/TSV：" + path);
            RejectLinks(full);
            return full;
        }

        public static byte[] ReadBytes(string path) => File.ReadAllBytes(Resolve(path));
        public static string ReadText(string path) => File.ReadAllText(Resolve(path));
        public static bool SameFile(string left, string right) => string.Equals(Resolve(left), Resolve(right), Comparison);

        public static void WriteBytes(string path, byte[] bytes)
        {
            string full = Resolve(path);
            if (Within(full, System.IO.Path.Combine(ProjectRoot, "Packages")))
                throw new InvalidOperationException("不能编辑包内源表：" + path);
            string temporary = full + ".ember-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                Resolve(path);
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        if (File.Exists(full)) File.Replace(temporary, full, null);
                        else File.Move(temporary, full);
                        break;
                    }
                    catch (IOException) when (attempt < 3)
                    {
                        // Unity/virus scanners can briefly hold an imported source on Windows.
                        System.Threading.Thread.Sleep(25);
                        Resolve(path);
                    }
                }
                ImportIfAsset(path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void ImportIfAsset(string path)
        {
            string full = Resolve(path);
            if (Within(full, System.IO.Path.Combine(ProjectRoot, "Assets")))
            {
                string assetPath = "Assets/" + full.Substring(System.IO.Path.Combine(ProjectRoot, "Assets").Length + 1).Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string Relative(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || System.IO.Path.IsPathRooted(path) || path.Contains(":"))
                throw new InvalidOperationException("源表路径必须相对 Unity 工程根目录：" + path);
            return path.Replace('\\', '/');
        }
        private static bool Within(string file, string root) => file.StartsWith(
            System.IO.Path.GetFullPath(root).TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar, Comparison);
        private static void RejectLinks(string path)
        {
            for (string current = path; !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("源表路径不允许符号链接或目录联接：" + current);
        }
        #endregion
    }
}
