// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ember.Basic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>项目中心的只读校验；不初始化设置、不修复资源、不运行场景合并。</summary>
    internal static class EmberProjectValidationService
    {
        #region 内部参数

        internal const string MappingPath = "Assets/Ember/Editor/SOs/EmberSceneMapping.asset";
        private static readonly Regex GuidPattern = new(@"(?m)^guid: ([0-9a-fA-F]{32})\s*$");

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal static EmberProjectValidationReport Scan()
        {
            var report = new EmberProjectValidationReport();
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请在编辑模式、Unity 编译和导入结束后检查。");

            bool canEditTemplates = EmberProjectSetup.IsEmbeddedPackage();
            var editing = canEditTemplates ? EmberProjectSetup.GetEditingTemplate() : null;
            var active = EmberProjectSetup.GetActiveDeployedTemplate();
            report.TemplateId = editing?.templateId ?? active?.templateId;
            report.IsEditingTemplate = editing != null;
            var template = EmberProjectSetup.GetTemplates().Find(item => item.id == report.TemplateId);
            if (template == null)
            {
                report.Baseline = string.IsNullOrEmpty(report.TemplateId)
                    ? "未确定模板；仅检查当前项目资源与生成链路"
                    : $"模板 [{report.TemplateId}] 不在当前包中；缺少比较基准";
                report.Add(EmberValidationSeverity.Warning, "比较基准", report.Baseline,
                    suggestion: canEditTemplates
                        ? "在项目初始化页确认活动模板，或在模板开发页加载目标模板。"
                        : "在项目初始化页部署包内模板；消费项目不能创建、加载或保存框架模板。");
            }
            else
            {
                report.Baseline = DescribeBaseline(template, editing, active);
                RunCheck(report, "模板存储", () => ValidateTemplate(
                    template,
                    report,
                    canEditTemplates));
                if (editing != null && EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, template))
                    report.Add(EmberValidationSeverity.Error, "编辑副本", "编辑记录已过期，不能用旧副本覆盖模板。",
                        "Assets/Editor/EmberEditingTemplate.json", "先保留项目改动，再重新加载或手工迁移。");
                if (editing == null)
                    report.Add(EmberValidationSeverity.Information, "比较基准",
                        "部署记录未保存历史文件 hash；下方差异仅与当前包模板比较，不证明部署时的原始内容。",
                        suggestion: "业务修改可保留；版本记录和生成头标记不能证明内容已经升级。");
                RunCheck(report, "内容差异", () => CompareFiles(Application.dataPath,
                    EmberProjectSetup.GetTemplateAssetsPath(template.id), report, editing != null,
                    editing == null ? template : null));
            }

            RunCheck(report, "资源元数据", () => ValidateProjectMetadata(Application.dataPath, report));
            RunCheck(report, "场景配置", () => ValidateScenes(report));
            RunCheck(report, "代码管理区", () => ValidateManagedRegions(Application.dataPath, report));
            if (HasUnsavedScenes())
                report.Add(EmberValidationSeverity.Warning, "检查范围", "场景有未保存改动；文件差异只反映磁盘内容。",
                    suggestion: "保存场景后重新检查。");
            foreach (var type in TypeCache.GetTypesDerivedFrom<IEmberProjectValidator>()
                .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                try { ((IEmberProjectValidator)Activator.CreateInstance(type)).Validate(report); }
                catch (Exception ex)
                {
                    report.Add(EmberValidationSeverity.Error, "扩展检查", $"{type.Name} 检查未完成：{ex.Message}");
                }
            }
            return report;
        }

        internal static EmberProjectValidationReport CompareEditingTemplate(string templateId)
        {
            var template = EmberProjectSetup.GetTemplates().Find(item => item.id == templateId)
                ?? throw new InvalidOperationException("模板不存在。");
            var editing = EmberProjectSetup.GetEditingTemplate();
            if (editing?.templateId != template.id)
                throw new InvalidOperationException("请先加载此模板；当前项目正在编辑其他模板或尚未加载模板。");
            if (EmberTemplateInheritanceEngine.IsEditingRecordStale(editing, template))
                throw new InvalidOperationException("编辑副本已过期，请先保留项目改动并处理过期状态。");
            string templateAssets = EmberProjectSetup.GetTemplateAssetsPath(templateId);
            if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(templateAssets, out var hash, out var error)
                || hash != template.contentHash)
                throw new InvalidOperationException("模板存储与记录不一致，或资源元数据无效：" + error);
            var report = new EmberProjectValidationReport
            {
                TemplateId = templateId, IsEditingTemplate = true,
                Baseline = $"当前项目 → {template.displayName} v{template.version}（按模板保存规则）"
            };
            CompareFiles(Application.dataPath, templateAssets, report, true);
            return report;
        }

        internal static string DescribeBaseline(TemplateInfo template, EditingTemplateRecord editing,
            DeployedTemplateRecord active)
        {
            if (editing != null)
                return $"编辑副本 {editing.templateId} v{editing.templateVersion} → 当前模板 v{template.version}";
            return $"活动部署 {active?.templateId} v{active?.version} → 当前包模板 v{template.version}（参考比较，无历史快照）";
        }

        internal static void CompareFiles(string projectAssets, string templateAssets,
            EmberProjectValidationReport report, bool normalizeForSave,
            TemplateInfo deployedTemplate = null)
        {
            if (!Directory.Exists(templateAssets))
                throw new DirectoryNotFoundException("模板 Assets 目录缺失。");
            var actual = EnumerateBusinessFiles(projectAssets);
            var expected = Directory.GetFiles(templateAssets, "*", SearchOption.AllDirectories)
                .ToDictionary(path => Relative(templateAssets, path), path => path, StringComparer.Ordinal);
            var fingerprint = new StringBuilder();
            foreach (var path in actual.Keys.Union(expected.Keys).OrderBy(path => path, StringComparer.Ordinal))
            {
                bool hasActual = actual.TryGetValue(path, out var actualPath);
                bool hasExpected = expected.TryGetValue(path, out var expectedPath);
                bool normalizeScene = normalizeForSave && (path == "Game/Scenes/FrameworkScene.unity" || path == "Game/Scenes/MainScene.unity");
                string actualHash = hasActual
                    ? normalizeScene
                        ? EmberTemplateInheritanceEngine.ComputeTemplateFileContentHash(
                            path,
                            EmberProjectSetup.ReadProjectTemplateBytes(actualPath, path))
                        : EmberTemplateInheritanceEngine.ComputeTemplateFileContentHash(path, File.ReadAllBytes(actualPath))
                    : null;
                if (hasActual)
                    fingerprint.Append(path.Length).Append(':').Append(path).Append(':')
                        .Append(actualHash).Append('\n');
                string expectedHash = !hasExpected
                    ? null
                    : deployedTemplate == null
                        ? EmberTemplateInheritanceEngine.ComputeTemplateFileContentHash(
                            path,
                            File.ReadAllBytes(expectedPath))
                        : EmberTemplateInheritanceEngine.ComputeTemplateFileContentHash(
                            path,
                            EmberProjectSetup.ReadDeploymentTemplateBytes(
                                expectedPath,
                                deployedTemplate.version,
                                deployedTemplate.frameworkVersion));
                string difference = !hasActual ? "项目中缺少" : !hasExpected ? "项目新增" :
                    actualHash == expectedHash ? null : "项目已修改";
                if (difference == null) continue;
                string ownership = GetOwnership(path, hasActual ? actualPath : expectedPath);
                report.Add(EmberValidationSeverity.Difference, "内容差异", difference + " · " + ownership,
                    "Assets/" + path, normalizeForSave
                        ? !hasActual ? "保存模板时将移除此文件。" : "保存模板时将写入此文件。"
                        : "这是参考差异，请结合业务修改判断；不会自动覆盖或删除。");
            }
            report.ProjectFingerprint = CryptographyUtils.GetMD5(fingerprint.ToString());
            report.ComparisonCompleted = true;
            if (report.DifferenceCount == 0)
                report.Add(EmberValidationSeverity.Passed, "内容差异", "磁盘文件与所列基准一致。");
        }

        internal static void ValidateProjectMetadata(string projectAssets, EmberProjectValidationReport report)
        {
            var files = EnumerateBusinessFiles(projectAssets);
            var guids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int errorsBefore = report.ErrorCount;
            foreach (var pair in files)
            {
                if (!paths.Add(pair.Key))
                    report.Add(EmberValidationSeverity.Error, "资源元数据", "路径仅大小写不同。", "Assets/" + pair.Key);
                if (!pair.Key.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    if (!File.Exists(pair.Value + ".meta"))
                        report.Add(EmberValidationSeverity.Error, "资源元数据", "资源缺少 .meta。", "Assets/" + pair.Key,
                            "先确认原始 GUID，避免新建 metadata 导致已有引用断开。");
                    continue;
                }
                string owner = pair.Value.Substring(0, pair.Value.Length - 5);
                if (!File.Exists(owner) && !Directory.Exists(owner))
                    report.Add(EmberValidationSeverity.Warning, "资源元数据", "孤立的 .meta 文件。", "Assets/" + pair.Key);
                var match = GuidPattern.Match(File.ReadAllText(pair.Value));
                if (!match.Success)
                    report.Add(EmberValidationSeverity.Error, "资源元数据", "无法读取有效 GUID。", "Assets/" + pair.Key);
                else if (guids.TryGetValue(match.Groups[1].Value, out var previous))
                    report.Add(EmberValidationSeverity.Error, "资源元数据", "GUID 重复，另一处为 " + previous, "Assets/" + pair.Key);
                else guids.Add(match.Groups[1].Value, pair.Key);
            }
            foreach (var root in EmberProjectSetup.BusinessDirectories)
            {
                string directory = Path.Combine(projectAssets, root);
                if (!Directory.Exists(directory)) continue;
                foreach (var child in Directory.GetDirectories(directory, "*", SearchOption.AllDirectories))
                    if (!File.Exists(child + ".meta"))
                        report.Add(EmberValidationSeverity.Error, "资源元数据", "目录缺少 .meta。", "Assets/" + Relative(projectAssets, child));
            }
            if (errorsBefore == report.ErrorCount && files.Count > 0)
                report.Add(EmberValidationSeverity.Passed, "资源元数据", "业务目录资源/.meta 配对与 GUID 检查完成。");
        }

        internal static bool HasUnsavedScenes()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty) return true;
            return false;
        }

        internal static bool HasUnsavedProjectContent()
        {
            if (HasUnsavedScenes()) return true;
            return Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Any(asset => EditorUtility.IsPersistent(asset)
                && EditorUtility.IsDirty(asset)
                && EmberProjectSetup.BusinessDirectories.Any(directory => AssetDatabase.GetAssetPath(asset)
                    .StartsWith("Assets/" + directory + "/", StringComparison.Ordinal)));
        }

        internal static void ValidateManagedRegions(string assetsRoot, EmberProjectValidationReport report)
        {
            var markers = new Regex(@"(?m)^\s*//\s*\[EmberManaged:(begin\s+([^\]\r\n]+)|end)\]");
            foreach (var pair in EnumerateBusinessFiles(assetsRoot).Where(pair => pair.Key.EndsWith(".cs", StringComparison.Ordinal)))
            {
                bool open = false;
                bool invalid = false;
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match match in markers.Matches(File.ReadAllText(pair.Value)))
                {
                    if (match.Groups[2].Success)
                    {
                        if (open || !names.Add(match.Groups[2].Value.Trim())) invalid = true;
                        open = true;
                    }
                    else
                    {
                        if (!open) invalid = true;
                        open = false;
                    }
                }
                if (invalid || open)
                    report.Add(EmberValidationSeverity.Error, "代码管理区", "框架管理区标记未配对、嵌套或重复。",
                        "Assets/" + pair.Key, "人工核对 begin/end 标记，保留用户代码；检查不会猜测或补写区块。");
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void RunCheck(EmberProjectValidationReport report, string category, Action action)
        {
            try { action(); }
            catch (Exception ex) { report.Add(EmberValidationSeverity.Error, category, "此项检查未完成：" + ex.Message); }
        }

        private static Dictionary<string, string> EnumerateBusinessFiles(string assetsRoot)
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var root in EmberProjectSetup.BusinessDirectories)
            {
                string directory = Path.Combine(assetsRoot, root);
                if (!Directory.Exists(directory)) continue;
                foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                    files.Add(Relative(assetsRoot, file), file);
            }
            return files;
        }

        private static string Relative(string root, string path) => path.Substring(root.Length + 1).Replace('\\', '/');

        private static string GetOwnership(string path, string fullPath)
        {
            if (path.EndsWith(".Binding.cs", StringComparison.Ordinal)) return "UI 生成器管理";
            if (!path.EndsWith(".cs", StringComparison.Ordinal)) return "资源/配置";
            string text = File.ReadAllText(fullPath);
            if (text.Contains("[EmberManaged:begin")) return "框架区 + 用户区";
            return text.Contains("Generated by Ember Setup") ? "框架骨架" : "用户代码";
        }

        private static void ValidateTemplate(
            TemplateInfo template,
            EmberProjectValidationReport report,
            bool canEditTemplates)
        {
            if (!EmberProjectSetup.IsFrameworkCompatible(template.frameworkVersion))
                report.Add(EmberValidationSeverity.Warning, "框架兼容", $"模板声明 {template.frameworkVersion}，当前框架 {EmberProjectSetup.GetFrameworkVersion()}。",
                    suggestion: canEditTemplates
                        ? "根模板验收封存后声明框架版本；派生模板通过父级同步继承声明。"
                        : "更新框架包，或选择与当前框架版本兼容的包内模板。");
            if (template.contentHash != template.versionedContentHash)
                report.Add(EmberValidationSeverity.Warning, "版本封存", "模板有已保存但未封存到版本的内容。",
                    suggestion: canEditTemplates
                        ? "在模板开发的版本与设置页选择版本封存。"
                        : "消费项目不能封存模板版本，请更新框架包或联系框架维护者。");
            var plan = EmberProjectSetup.ComputeParentSyncPlan(template.id);
            if (plan.Status == TemplateSyncStatus.Root || plan.Status == TemplateSyncStatus.Synced)
                report.Add(EmberValidationSeverity.Passed, "模板存储", "模板内容、元数据和父级状态正常。");
            else
                report.Add(plan.Status == TemplateSyncStatus.ParentChanged || plan.Status == TemplateSyncStatus.ParentUnversioned
                        ? EmberValidationSeverity.Warning : EmberValidationSeverity.Error,
                    "模板存储", plan.Error ?? plan.Status.ToString(),
                    suggestion: canEditTemplates
                        ? "进入模板开发页查看父级更新和状态说明。"
                        : "消费项目不能修改包内模板；请重新安装框架包或联系框架维护者。");
        }

        private static void ValidateScenes(EmberProjectValidationReport report)
        {
            const string framework = "Assets/Game/Scenes/FrameworkScene.unity";
            var scenes = EditorBuildSettings.scenes;
            if (File.Exists(framework) && (scenes.Length == 0 || scenes[0].path != framework || !scenes[0].enabled))
                report.Add(EmberValidationSeverity.Error, "场景配置", "框架场景应位于构建列表首位且启用。", framework);
            foreach (var scene in scenes.Where(scene => scene.enabled))
                if (!File.Exists(scene.path))
                    report.Add(EmberValidationSeverity.Error, "场景配置", "构建列表引用的场景不存在。", scene.path);
            var mapping = AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(MappingPath);
            if (mapping == null)
            {
                report.Add(EmberValidationSeverity.Error, "场景配置", "场景映射配置缺失或无法加载。", MappingPath,
                    "在项目初始化页核对部署，或打开场景映射配置。");
                return;
            }
            if (mapping.frameworkScene == null)
                report.Add(EmberValidationSeverity.Error, "场景配置", "场景映射未指定框架场景。", MappingPath);
            foreach (var entry in mapping.entries ?? new List<StateSceneEntry>())
            {
                string sceneName = entry?.sceneField.SceneName;
                if (string.IsNullOrEmpty(sceneName))
                    report.Add(EmberValidationSeverity.Warning, "场景配置", $"{entry?.stateName} 未关联场景。", MappingPath,
                        "若该状态不需要场景，可保留为空。");
                else
                {
                    var matches = scenes.Where(scene => Path.GetFileNameWithoutExtension(scene.path) == sceneName && scene.enabled).ToArray();
                    if (matches.Length != 1 || !File.Exists(matches[0].path))
                        report.Add(EmberValidationSeverity.Error, "场景配置", $"{entry.stateName} 的场景不存在、未启用或名称重复。", MappingPath);
                }
            }
        }

        #endregion
    }
}
