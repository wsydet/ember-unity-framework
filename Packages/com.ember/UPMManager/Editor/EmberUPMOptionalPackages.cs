using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            internal State(string text, bool installed, bool canInstall)
            { Text = text; Installed = installed; CanInstall = canInstall; }
        }

        internal static readonly Definition[] All =
        {
            new("com.coplaydev.unity-mcp", "Unity MCP（AI 编辑器连接）", "MCPForUnity.Editor.Windows.MCPForUnityEditorWindow",
                "连接 AI 助手与 Unity 编辑器。安装后请在 Window > MCP for Unity 配置服务与客户端；已安装不代表已连接。",
                "10.1.2", "4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50",
                "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#4ce7dd3cc54e37e2ed6dc59cb5a047f3dccb3f50"),
            new("com.borodar.rainbow-folders", "Rainbow Folders", "Borodar.RainbowFolders.RainbowFoldersGUI",
                "Project 文件夹颜色与图标", "2.4.5", "rainbow-folders-v2.4.5"),
            new("com.borodar.rainbow-hierarchy", "Rainbow Hierarchy", "Borodar.RainbowHierarchy.RainbowHierarchyGUI",
                "Hierarchy 层级颜色与分组", "2.6.5", "rainbow-hierarchy-v2.6.5"),
            new("com.flyingworm.consolepro", "Console Pro", "FlyingWormConsole3.ConsoleProDebug",
                "Console 日志增强", "3.9.81", "consolepro-v3.9.81"),
            new("com.ryanindiedev.inputdevicedetector", "InputDeviceDetector", "InputDeviceDetection.InputDeviceDetector",
                "键鼠与手柄输入设备识别", "1.0.0", "inputdevicedetector-v1.0.0"),
            new("com.moremountains.feel", "Feel", "MoreMountains.Feedbacks.MMF_Player",
                "反馈、动效与振动工具", "5.4.0", "ember-v0.11.1")
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
                return FromEvidence(true, info.version, SourceLabel(info.source), false, false);
            bool loaded = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType(definition.TypeName, false) != null);
            return FromEvidence(false, null, null, loaded, !loaded && HasImportedScript(definition.TypeName));
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
