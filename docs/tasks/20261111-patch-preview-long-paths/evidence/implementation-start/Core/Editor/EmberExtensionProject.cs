using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>项目中心的扩展配置入口。运行能力只声明依赖，编辑器入口执行启用门禁。</summary>
    [InitializeOnLoad]
    public static partial class EmberExtensionProject
    {
        #region 内部参数
        public const string Core = "ember.core";
        public const string FsmRuntime = "ember.fsm.runtime";
        public const string FsmBlueprint = "ember.fsm.blueprint";
        public const string LevelRuntime = "ember.level.runtime";
        public const string LevelAuthoring = "ember.level.authoring";
        public const string LevelTrial = "ember.level.trial";
        public const string RoomGraph = "ember.level.room-graph";
        public const string DefaultsPath = "Assets/Game/Config/EmberExtensionDefaults.json";
        private static readonly Dictionary<string, EmberExtensionDefinition> Definitions = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, (long stamp, string text)> Files = new();
        [Serializable] public sealed class Configuration
        {
            public int schemaVersion = 2;
            public string ownerTemplateId;
            public string ownerTemplateVersion;
            public string[] enabled = Array.Empty<string>();
            public Ember.UPMManager.Editor.EmberAISkillInstaller.ExtensionIdentity[] bindings;
        }
        public static IReadOnlyList<EmberExtensionDefinition> All => Definitions.Values.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
        public static string ConfigurationPath => EmberProjectSetup.IsEmbeddedPackage()
            ? "ProjectSettings/EmberExtensions." + (EmberFsmBlueprintAccess.CurrentTemplateId ?? "unassigned") + ".json"
            : "ProjectSettings/EmberExtensions.json";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static EmberExtensionProject()
        {
            Register(new EmberExtensionDefinition(Core, "框架基础设施", EmberExtensionKind.Infrastructure,
                checkCompatibility: () => Application.unityVersion.StartsWith("6000.", StringComparison.Ordinal) ? null : "首版支持 Unity 6（6000.x）。"));
            Register(new EmberExtensionDefinition(FsmRuntime, "层级 FSM", EmberExtensionKind.RuntimeCapability, new[] { Core }));
            Register(new EmberExtensionDefinition(FsmBlueprint, "FSM 蓝图", EmberExtensionKind.EditorExtension, new[] { FsmRuntime }));
            Register(new EmberExtensionDefinition(LevelRuntime, "地形与视差运行能力", EmberExtensionKind.RuntimeCapability, new[] { Core }));
            Register(new EmberExtensionDefinition(LevelAuthoring, "关卡制作", EmberExtensionKind.EditorExtension, new[] { LevelRuntime }));
            Register(new EmberExtensionDefinition(LevelTrial, "关卡试玩会话", EmberExtensionKind.EditorExtension, new[] { LevelAuthoring }));
            Register(new EmberExtensionDefinition(RoomGraph, "房间地图组合", EmberExtensionKind.EditorExtension, new[] { LevelAuthoring }));
        }
        private static Configuration Read(string path)
        {
            long stamp = File.GetLastWriteTimeUtc(path).Ticks;
            if (!Files.TryGetValue(path, out var cached) || cached.stamp != stamp)
            { cached = (stamp, File.ReadAllText(path)); Files[path] = cached; }
            var config = JsonUtility.FromJson<Configuration>(cached.text);
            if (config == null || config.schemaVersion < 1 || config.schemaVersion > 2 || config.enabled == null || config.enabled.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("扩展配置格式无效：" + path);
            return config;
        }
        private static Configuration LoadDefaults()
        {
            if (File.Exists(DefaultsPath))
            {
                var config = Read(DefaultsPath);
                if (string.IsNullOrEmpty(config.ownerTemplateId) || !EmberProjectSetup.IsTemplateActive(config.ownerTemplateId))
                    throw new InvalidDataException("扩展默认组合不属于正式活动模板。");
                return config;
            }
            // 无声明时只保留基础设施，不能从模板名或残留业务脚本推断授权。
            return CreateConfiguration(Array.Empty<string>());
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Register(EmberExtensionDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (Definitions.ContainsKey(definition.Id)) throw new InvalidOperationException("扩展 ID 重复：" + definition.Id);
            Definitions.Add(definition.Id, definition);
        }
        public static Configuration Load()
        {
            if (!File.Exists(ConfigurationPath)) return LoadDefaults();
            var config = Read(ConfigurationPath);
            if (!string.IsNullOrEmpty(config.ownerTemplateId) && config.ownerTemplateId != EmberFsmBlueprintAccess.CurrentTemplateId)
                throw new InvalidDataException("项目扩展配置属于另一活动模板：" + config.ownerTemplateId);
            return config;
        }
        public static EmberExtensionResolution Resolve() => ResolveConfiguration(Load(), EmberFsmBlueprintAccess.CurrentTemplateId, CurrentTemplateVersion);
        public static bool IsEnabled(string id)
        {
            try { return Resolve().IsEnabled(id); }
            catch (Exception) { return false; }
        }
        public static bool Allows(string id, string ownerTemplateId, Func<bool> enabled)
        {
            if (!IsEnabled(id) || ownerTemplateId == null || ownerTemplateId != "" && !EmberProjectSetup.IsTemplateActive(ownerTemplateId)) return false;
            try { return enabled?.Invoke() ?? false; }
            catch (Exception) { return false; }
        }
        public static void Require(string id, bool writing = false)
        {
            if (!IsEnabled(id)) throw new InvalidOperationException("扩展未启用或依赖/兼容条件不满足：" + id + "。请检查 Ember 项目中心的扩展组合。");
            if (writing && (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
                throw new InvalidOperationException("请等待播放、编译或导入结束再编辑。");
        }
        public static void Save(Configuration config)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("请在空闲编辑状态保存扩展组合。");
            if (config == null || config.enabled == null) throw new ArgumentException("配置无效。");
            var result = EmberExtensionCatalog.Resolve(All, config.enabled);
            if (result.Problems.Count > 0) throw new InvalidOperationException(string.Join("\n", result.Problems.Values));
            var copy = CreateConfiguration(config.enabled);
            EmberProjectSetup.CommitExtensionConfiguration(copy);
            Files.Remove(ConfigurationPath); EmberConditionalMenu.Refresh();
        }
        #endregion
    }
}
