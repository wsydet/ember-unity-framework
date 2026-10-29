using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>扩展操作文档的正式发现/读取入口。公共 Wiki 与迁移说明仍可直接阅读。</summary>
    public static class EmberExtensionDocuments
    {
        #region 内部参数
        [Serializable] public sealed class Entry
        {
            public string id, title, path, ownerTemplateId, ownerTemplateVersion, moduleScope;
            public string contentHash;
            public string[] requiredExtensions;
        }
        [Serializable] private sealed class Catalog { public int schemaVersion; public Entry[] documents; }
        private sealed class Scope : IDisposable
        {
            internal string Id, Owner;
            internal Func<bool> Enabled;
            public void Dispose() => Scopes.Remove(this);
        }
        private static readonly List<Scope> Scopes = new();
        public const string CatalogPath = "Assets/Game/Documentation/ExtensionDocuments.json";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static IEnumerable<Entry> Entries()
        {
            yield return new Entry { id = "ember.fsm.blueprint.guide", title = "FSM 蓝图操作与接线", path = "manual/fsm-blueprint-access.md",
                requiredExtensions = new[] { EmberExtensionProject.FsmBlueprint } };
            if (!File.Exists(CatalogPath)) yield break;
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(CatalogPath));
            if (catalog?.schemaVersion != 1 || catalog.documents == null) throw new InvalidDataException("扩展文档清单无效。");
            foreach (var entry in catalog.documents)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || string.IsNullOrWhiteSpace(entry.ownerTemplateId)
                    || string.IsNullOrWhiteSpace(entry.ownerTemplateVersion) || string.IsNullOrWhiteSpace(entry.contentHash)
                    || entry.requiredExtensions == null || entry.requiredExtensions.Length == 0)
                    throw new InvalidDataException("模板扩展文档必须声明 ID、模板版本和扩展依赖。");
                yield return entry;
            }
        }
        private static bool Allowed(Entry entry) => entry.requiredExtensions.All(EmberExtensionProject.IsEnabled)
            && (string.IsNullOrEmpty(entry.ownerTemplateId) || entry.ownerTemplateId == EmberFsmBlueprintAccess.CurrentTemplateId
                && entry.ownerTemplateVersion == EmberExtensionProject.CurrentTemplateVersion)
            && (string.IsNullOrEmpty(entry.moduleScope) || IsScopeEnabled(entry.moduleScope))
            && (string.IsNullOrEmpty(entry.ownerTemplateId) || Hash(entry) == entry.contentHash);
        private static string Hash(Entry entry)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(FullPath(entry)))).Replace("-", "").ToLowerInvariant();
        }
        private static string FullPath(Entry entry)
        {
            string root = string.IsNullOrEmpty(entry.ownerTemplateId)
                ? Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EmberExtensionDocuments).Assembly).resolvedPath, "Documentation~")
                : "Assets/Game/Documentation";
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(fullRoot, entry.path ?? ""));
            if (!full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !full.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("扩展文档路径超出所属目录或不是 Markdown。");
            EmberTemplateTransaction.ValidateDirectorySafety(Path.GetDirectoryName(full));
            return full;
        }
        internal static bool IsScopeEnabled(string id) => Scopes.Any(scope => scope.Id == id
            && EmberProjectSetup.IsTemplateActive(scope.Owner) && SafeEnabled(scope.Enabled));
        private static bool SafeEnabled(Func<bool> check) { try { return check(); } catch (Exception) { return false; } }
        internal static string[] EnabledScopes => Scopes.Where(scope => IsScopeEnabled(scope.Id)).Select(scope => scope.Id).Distinct().OrderBy(x => x).ToArray();
        internal static void DrawIndex()
        {
            try
            {
                foreach (var entry in Available())
                    if (GUILayout.Button("扩展文档 · " + entry.title)) Open(entry.id);
            }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static IDisposable RegisterModuleScope(string id, string ownerTemplateId, Func<bool> enabled)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ownerTemplateId) || enabled == null) throw new ArgumentException("模块范围声明无效。");
            var scope = new Scope { Id = id, Owner = ownerTemplateId, Enabled = enabled }; Scopes.Add(scope); return scope;
        }
        public static Entry[] Available() => Entries().Where(Allowed).ToArray();
        /// <summary>维护源改动后显式生成待审阅的文档指纹，不写清单。</summary>
        public static string ComputeContentHash(Entry entry) => Hash(entry);
        /// <summary>每次读取/打开都重新核对正式身份和启用绑定，旧索引不能授予访问。</summary>
        public static string Read(string id)
        {
            var entry = Entries().SingleOrDefault(e => e.id == id && Allowed(e))
                ?? throw new InvalidOperationException("文档所属扩展/模板版本/模块当前不可用：" + id);
            return File.ReadAllText(FullPath(entry));
        }
        public static void Open(string id)
        {
            Read(id);
            var entry = Entries().Single(e => e.id == id && Allowed(e));
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(FullPath(entry), 1);
        }
        #endregion
    }
}
