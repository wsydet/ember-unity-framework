using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Ember.UPMManager.Editor
{
    /// <summary>可选包安装地址和只读检测，不引用任何第三方程序集。</summary>
    internal static class EmberUPMOptionalPackages
    {
        internal sealed class Definition
        {
            internal readonly string Name, Label, TypeName, Description, Version, Tag;
            private readonly string _installUrl;
            internal string InstallUrl => _installUrl ?? "https://github.com/wsydet/ember-thirdparty-upm.git?path=/" + Name + "#" + Tag;

            internal Definition(string name, string label, string typeName, string description, string version, string tag,
                string installUrl = null)
            {
                Name = name; Label = label; TypeName = typeName; Description = description; Version = version; Tag = tag;
                _installUrl = installUrl;
            }
        }

        internal readonly struct State
        {
            internal readonly string Text;
            internal readonly bool Installed;
            internal readonly bool CanInstall;
            internal readonly bool CanUpgrade;
            internal State(string text, bool installed, bool canInstall, bool canUpgrade = false)
            { Text = text; Installed = installed; CanInstall = canInstall; CanUpgrade = canUpgrade; }
        }

        internal static readonly Definition[] All =
        {
            new("com.coplaydev.unity-mcp", "Unity MCP（AI 编辑器连接）", "MCPForUnity.Editor.Windows.MCPForUnityEditorWindow",
                "连接 AI 助手与 Unity 编辑器。安装后请在 Window > MCP for Unity 配置服务与客户端；已安装不代表已连接。",
                "10.3.0", "v10.3.0",
                "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0"),
            new("com.borodar.rainbow-folders", "Rainbow Folders", "Borodar.RainbowFolders.RainbowFoldersGUI",
                "Project 文件夹颜色与图标", "2.4.5", "rainbow-folders-v2.4.5"),
            new("com.borodar.rainbow-hierarchy", "Rainbow Hierarchy", "Borodar.RainbowHierarchy.RainbowHierarchyGUI",
                "Hierarchy 层级颜色与分组", "2.6.5", "rainbow-hierarchy-v2.6.5"),
            new("com.flyingworm.consolepro", "Console Pro", "FlyingWormConsole3.ConsoleProDebug",
                "Console 日志增强", "3.9.81", "consolepro-v3.9.81"),
            new("com.ryanindiedev.inputdevicedetector", "InputDeviceDetector", "InputDeviceDetection.InputDeviceDetector",
                "键鼠与手柄输入设备识别", "1.0.0", "inputdevicedetector-v1.0.0"),
            new("com.moremountains.feel", "Feel", "MoreMountains.Feedbacks.MMF_Player",
                "反馈、动效与振动工具", "5.4.1", "feel-v5.4.1")
        };

        internal static Dictionary<string, State> Capture()
        {
            var packages = PackageInfo.GetAllRegisteredPackages();
            if (packages == null) throw new InvalidOperationException("Unity 尚未提供已注册包信息，请等待解析完成后刷新。");
            var states = new Dictionary<string, State>();
            foreach (var definition in All) states.Add(definition.Name, Inspect(definition, packages));
            return states;
        }

        internal static State Inspect(Definition definition, PackageInfo[] packages)
        {
            var info = packages?.FirstOrDefault(p => p.name == definition.Name);
            if (info != null)
                return InspectRegistered(definition, info.version, info.source, info.packageId, info.isDirectDependency);
            bool loaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(definition.TypeName, false) != null);
            return FromEvidence(false, null, null, loaded, !loaded && HasImportedScript(definition.TypeName));
        }

        internal static State InspectRegistered(Definition definition, string version, PackageSource source,
            string packageId, bool directDependency)
        {
            var installed = FromEvidence(true, version, SourceLabel(source), false, false);
            if (definition.Name != "com.coplaydev.unity-mcp") return installed;

            string text = installed.Text + "；框架推荐 v" + definition.Version;
            bool knownVersion = TryStableVersion(version, out var current)
                && TryStableVersion(definition.Version, out _);
            if (knownVersion)
            {
                var target = Version.Parse(definition.Version);
                if (current == target) text += "（已达推荐版本）";
                else if (current > target) text += "（高于推荐版本，不降级）";
                else text += "（有推荐更新）";
            }
            else text += "（版本无法安全比较，请人工确认）";

            bool supported = source == PackageSource.Git && directDependency
                && IsSupportedMcpUrl(GetPackageUrl(definition, packageId));
            if (!supported)
                text += "。保留本地、自定义或间接依赖；请在 Unity Package Manager 人工管理来源。";
            bool upgrade = supported && knownVersion && current < Version.Parse(definition.Version);
            return new State(text, true, false, upgrade);
        }

        // Only the official package directory, default branch and stable release tags are managed here.
        // Custom branches, commit pins, forks, SSH/local/embedded/registry packages remain user-owned.
        internal static bool IsSupportedMcpUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != "https" || uri.Host != "github.com" || !uri.IsDefaultPort
                || uri.UserInfo.Length != 0
                || !string.Equals(uri.AbsolutePath, "/CoplayDev/unity-mcp.git", StringComparison.OrdinalIgnoreCase)
                || uri.Query != "?path=/MCPForUnity") return false;
            return uri.Fragment.Length == 0 || uri.Fragment == "#main"
                || Regex.IsMatch(uri.Fragment, @"^#v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$");
        }

        private static bool TryStableVersion(string value, out Version version)
        {
            version = null;
            return value != null && Regex.IsMatch(value, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
                && Version.TryParse(value, out version);
        }

        private static string GetPackageUrl(Definition definition, string packageId)
        {
            string prefix = definition.Name + "@";
            return packageId != null && packageId.StartsWith(prefix, StringComparison.Ordinal)
                ? packageId.Substring(prefix.Length) : null;
        }

        /// <summary>Revalidate resolved evidence and the on-disk declaration immediately before dispatch.</summary>
        internal static bool TryUpgrade(Definition definition, PackageInfo[] packages, string manifestJson,
            Action<string> add, out string error)
        {
            var info = packages?.FirstOrDefault(p => p.name == definition.Name);
            if (info == null || !InspectRegistered(definition, info.version, info.source,
                    info.packageId, info.isDirectDependency).CanUpgrade)
            {
                error = "当前包已不满足升级条件，请刷新安装状态。";
                return false;
            }
            // UPM dependencies is a flat string map. Reject ambiguous/escaped declarations instead of guessing.
            if (!TryReadManifestDependency(definition, manifestJson, out var declared)
                || declared != GetPackageUrl(definition, info.packageId))
            {
                error = "manifest 的 MCP 来源与已解析包不一致；请等待 UPM 解析完成后刷新。未执行替换。";
                return false;
            }
            add(definition.InstallUrl);
            error = null;
            return true;
        }

        internal static bool TryReadManifestDependency(Definition definition, string manifestJson, out string declared)
        {
            declared = null;
            var maps = Regex.Matches(manifestJson ?? "", "\"dependencies\"\\s*:\\s*\\{([^{}]*)\\}");
            if (maps.Count != 1) return false;
            const string pair = @"""([^""\\\x00-\x1f]+)""\s*:\s*""([^""\\\x00-\x1f]*)""";
            string entries = maps[0].Groups[1].Value;
            if (!Regex.IsMatch(entries, @"^\s*(?:" + pair + @"(?:\s*,\s*" + pair + @")*)?\s*$")) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match entry in Regex.Matches(entries, pair))
            {
                string name = entry.Groups[1].Value;
                if (!names.Add(name)) return false;
                if (name == definition.Name) declared = entry.Groups[2].Value;
            }
            return true;
        }

        internal static State FromEvidence(bool registered, string version, string source, bool typeLoaded, bool scriptFound)
        {
            if (registered)
                return new State("已安装" + (string.IsNullOrEmpty(version) ? "" : " v" + version) + "（" + source + "）", true, false);
            if (typeLoaded) return new State("已安装（直接导入 / 自定义包，版本未知）", true, false);
            if (scriptFound) return new State("已发现 Assets 插件文件（待编译确认）", false, false);
            return new State("未安装（可选）", false, true);
        }

        private static string SourceLabel(PackageSource source)
        {
            switch (source)
            {
                case PackageSource.Git: return "UPM · Git";
                case PackageSource.Local: return "UPM · 本地包";
                case PackageSource.Embedded: return "UPM · 内嵌包";
                case PackageSource.Registry: return "UPM · Registry";
                default: return "UPM · " + source;
            }
        }

        private static bool HasImportedScript(string typeName)
        {
            string name = typeName.Substring(typeName.LastIndexOf('.') + 1);
            foreach (string guid in AssetDatabase.FindAssets(name + " t:MonoScript", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != name) continue;
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script != null ? script.GetClass() : null;
                if (type == null || type.FullName == typeName) return true;
            }
            return false;
        }
    }
}
