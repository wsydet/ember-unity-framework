using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ember.UPMManager.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    public static partial class EmberExtensionProject
    {
        #region 内部参数
        private static readonly Dictionary<string, string> ContentHashes = new(StringComparer.Ordinal);
        public static string CurrentTemplateVersion => EmberProjectSetup.IsEmbeddedPackage()
            ? EmberProjectSetup.GetEditingTemplate()?.templateVersion : EmberProjectSetup.GetActiveDeployedTemplate()?.version;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string[] BuiltinRoots(string id) => id switch
        {
            Core => new[] { "Core/Editor", "UPMManager/Editor" },
            FsmRuntime => new[] { "Core/Runtime" },
            FsmBlueprint => new[] { "Core/Editor", "Documentation~/manual/fsm-blueprint-access.md" },
            LevelRuntime => new[] { "LevelAuthoring/Runtime" },
            LevelAuthoring or LevelTrial or RoomGraph => new[] { "LevelAuthoring/Editor" },
            _ => Array.Empty<string>()
        };
        private static string ContentHash(EmberExtensionDefinition definition)
        {
            if (ContentHashes.TryGetValue(definition.Id, out var hash)) return hash;
            string root = PackageInfoRoot();
            var paths = definition.SourceRoots.Count > 0 ? definition.SourceRoots : BuiltinRoots(definition.Id);
            var text = new StringBuilder(definition.Id + "\n" + definition.ContractVersion + "\n" + definition.Compatibility
                + "\n" + string.Join("\n", definition.Dependencies));
            foreach (string relative in paths)
            {
                string path = Path.GetFullPath(Path.Combine(root, relative));
                if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("扩展内容根必须位于 com.ember 包内。");
                var files = Directory.Exists(path) ? Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    : File.Exists(path) ? new[] { path } : throw new FileNotFoundException("扩展内容根不存在：" + relative);
                foreach (string file in files.Where(f => !f.EndsWith(".meta", StringComparison.Ordinal)).OrderBy(f => f, StringComparer.Ordinal))
                {
                    using var algorithm = SHA256.Create();
                    byte[] bytes = File.ReadAllBytes(file);
                    if (new[] { ".cs", ".asmdef", ".asmref", ".json", ".md", ".uss", ".uxml", ".txt" }.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        bytes = CanonicalSource(bytes);
                    text.Append('\n').Append(file.Substring(root.Length).Replace('\\', '/')).Append(':')
                        .Append(Convert.ToBase64String(algorithm.ComputeHash(bytes)));
                }
            }
            using var sha = SHA256.Create();
            hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "").ToLowerInvariant();
            ContentHashes[definition.Id] = hash; return hash;
        }
        private static string PackageInfoRoot() => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EmberExtensionProject).Assembly)?.resolvedPath
            ?? throw new InvalidOperationException("无法定位 com.ember 包。");
        internal static byte[] CanonicalSource(byte[] bytes) => Encoding.UTF8.GetBytes(new UTF8Encoding(false, true).GetString(bytes)
            .TrimStart('\uFEFF').Replace("\r\n", "\n").Replace("\r", "\n"));
        private static string BindingProblem(string id, Configuration config)
        {
            var pending = new Stack<string>(); pending.Push(id);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                if (!visited.Add(current) || !Definitions.TryGetValue(current, out var definition)) continue;
                var binding = config.bindings?.SingleOrDefault(x => x != null && x.id == current);
                if (binding == null || binding.contractVersion != definition.ContractVersion || binding.contentHash != ContentHash(definition))
                    return "扩展或传递依赖版本/hash 未签收或已变化：" + current;
                foreach (string dependency in definition.Dependencies) pending.Push(dependency);
            }
            return null;
        }
        internal static EmberExtensionResolution ResolveConfiguration(Configuration config, string templateId, string version)
        {
            string reason = config.schemaVersion != 2 ? "旧扩展配置尚未签收版本/hash，请在项目中心检查并保存组合。"
                : (config.ownerTemplateId ?? "") != (templateId ?? "") || (config.ownerTemplateVersion ?? "") != (version ?? "")
                    ? "扩展配置与正式活动模板 ID/版本不一致。" : null;
            var definitions = All.Select(d => new EmberExtensionDefinition(d.Id, d.DisplayName, d.Kind, d.Dependencies,
                d.Compatibility, () =>
                {
                    if (d.Kind == EmberExtensionKind.Infrastructure) return d.CheckCompatibility?.Invoke();
                    if (reason != null) return reason;
                    string bindingProblem = BindingProblem(d.Id, config);
                    if (bindingProblem != null) return bindingProblem;
                    return d.CheckCompatibility?.Invoke();
                })).ToArray();
            return EmberExtensionCatalog.Resolve(definitions, config.enabled);
        }
        internal static EmberAISkillInstaller.ExtensionIdentity[] Identities(EmberExtensionResolution resolution) =>
            All.Where(d => resolution.IsEnabled(d.Id)).Select(d => new EmberAISkillInstaller.ExtensionIdentity
                { id = d.Id, contractVersion = d.ContractVersion, contentHash = ContentHash(d) }).ToArray();
        internal static EmberAISkillInstaller.ExtensionIdentity[] SourceIdentities(string assets, string projectRoot, string id, string version)
        {
            string relative = EmberProjectSetup.IsEmbeddedPackage() ? "ProjectSettings/EmberExtensions." + id + ".json" : "ProjectSettings/EmberExtensions.json";
            string config = Path.Combine(projectRoot, relative);
            // A different consumer template never borrows the previous template's override.
            Configuration value = File.Exists(config) ? Read(config) : null;
            if (value != null && value.ownerTemplateId != id) value = null;
            string defaults = Path.Combine(assets, "Game/Config/EmberExtensionDefaults.json");
            value ??= File.Exists(defaults) ? Read(defaults) : new Configuration { ownerTemplateId = id, ownerTemplateVersion = version };
            return Identities(ResolveConfiguration(value, id, version));
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>窗口持有的只读身份；重新启用同一版本可恢复，升级后旧窗口草稿不会自动接管新版本。</summary>
        public static string CaptureScope(string id)
        {
            var resolution = EmberExtensionCatalog.Resolve(All, new[] { id });
            return (EmberFsmBlueprintAccess.CurrentTemplateId ?? "") + "@" + (CurrentTemplateVersion ?? "") + ":"
                + string.Join(";", Identities(resolution).Select(x => x.id + "@" + x.contractVersion + ":" + x.contentHash));
        }
        public static bool AllowsScope(string id, string scope) => IsEnabled(id) && scope == CaptureScope(id);
        /// <summary>创建待审阅的当前版本绑定；不写文件、不启用任何工具。</summary>
        public static Configuration CreateConfiguration(IEnumerable<string> enabled)
        {
            var roots = (enabled ?? Array.Empty<string>()).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var resolution = EmberExtensionCatalog.Resolve(All, roots);
            if (resolution.Problems.Count > 0) throw new InvalidOperationException(string.Join("\n", resolution.Problems.Values));
            return new Configuration { ownerTemplateId = EmberFsmBlueprintAccess.CurrentTemplateId, ownerTemplateVersion = CurrentTemplateVersion,
                enabled = roots, bindings = Identities(resolution) };
        }
        public static EmberAISkillInstaller.ExtensionIdentity[] EnabledIdentities()
        { try { return Identities(Resolve()); } catch (Exception) { return Array.Empty<EmberAISkillInstaller.ExtensionIdentity>(); } }
        #endregion
    }
}
