// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.UPMManager.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>消费端补丁预览；指纹绑定三方内容与部署记录，应用前必须重新核对。</summary>
    public sealed class TemplatePatchPlan
    {
        #region 内部参数
        public string TemplateId { get; internal set; }
        public string FromVersion { get; internal set; }
        public string ToVersion { get; internal set; }
        public IReadOnlyList<TemplateChange> Changes { get; internal set; }
        internal string SourceHash, ProjectHash, BaselineHash, RecordJson, TemplateJson;
        internal string ProjectRoot, SourceAssets;
        #endregion
    }

    public static partial class EmberProjectSetup
    {
        #region 内部参数
        // 持久、可提交版本控制，不放在 Library/Temp，也不进入 Unity 资源导入或模板覆盖范围。
        internal const string DeploymentBaselinePath = "ProjectSettings/EmberTemplateBaseline~";
        #endregion

        // --------------------------------------------------------

        #region 内部方法
        private static DeployedTemplatesData ReadPatchDeploymentData(string projectRoot)
        {
            string path = Path.Combine(projectRoot, DeployedRecordsPath);
            var data = File.Exists(path)
                ? JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(path)) : null;
            if (data?.records == null || ResolveActiveDeployment(data) == null)
                throw new InvalidDataException("缺少明确的活动部署记录，请先确认模板身份。");
            return data;
        }

        private static void ValidatePatchVersion(DeployedTemplateRecord active, TemplateInfo template)
        {
            if (active.templateId != template.id)
                throw new InvalidOperationException("增量更新只能用于当前活动模板。");
            if (!IsForwardTemplatePatch(active.version, template.version))
                throw new InvalidOperationException("增量更新要求 major.minor 相同且 patch 递增；前两位变化请先保护本地改动，再完整部署。");
            if (string.IsNullOrEmpty(template.contentHash) || template.contentHash != template.versionedContentHash)
                throw new InvalidOperationException("增量更新只能使用已 Bump 封存的模板。");
        }

        private static string ValidateDeploymentBaseline(string projectRoot, DeployedTemplateRecord active)
        {
            string root = Path.Combine(projectRoot, DeploymentBaselinePath);
            EmberTemplateTransaction.ValidateDirectorySafety(root);
            string metadata = Path.Combine(root, "template.json");
            var info = File.Exists(metadata) ? JsonUtility.FromJson<TemplateInfo>(File.ReadAllText(metadata)) : null;
            if (info == null || info.id != active.templateId || info.version != active.version
                || string.IsNullOrEmpty(active.contentHash) || info.contentHash != active.contentHash)
                throw new InvalidDataException("部署基线缺失或身份不匹配。请导入部署时旧版本模板的 Assets，不能用当前业务文件代替原稿。");
            string assets = Path.Combine(root, "Assets");
            EnsureStoredContentMatchesMetadata(info, assets, "部署基线校验");
            return assets;
        }

        private static void AddDeploymentBaseline(List<TemplateTransactionTarget> targets, string stageRoot,
            string projectRoot, string sourceAssets, TemplateInfo template)
        {
            string staged = Path.Combine(stageRoot, "Baseline");
            EmberTemplateTransaction.CopyDirectory(sourceAssets, Path.Combine(staged, "Assets"));
            EnsureStoredContentMatchesMetadata(template, Path.Combine(staged, "Assets"), "基线暂存");
            EmberTemplateTransaction.WriteTemplateJson(Path.Combine(staged, "template.json"), template);
            targets.Add(new TemplateTransactionTarget(staged, Path.Combine(projectRoot, DeploymentBaselinePath)));
        }

        // 只比较实际由部署器管理的内容；头标记不是业务变化，统一到固定值后分类。
        private static void CopyPatchAssets(string sourceAssets, string destination, bool normalizeMarkers)
        {
            Directory.CreateDirectory(destination);
            foreach (string relative in TemplateDirNames)
            {
                string source = Path.Combine(sourceAssets, relative);
                if (Directory.Exists(source))
                    EmberTemplateTransaction.CopyDirectory(source, Path.Combine(destination, relative));
                else if (File.Exists(source))
                    throw new InvalidDataException("模板管理目录被文件占用：" + relative);
            }
            if (!normalizeMarkers) return;
            foreach (string file in Directory.GetFiles(destination, "*", SearchOption.AllDirectories))
                if (!IsSkillSourcePath(RelativePath(destination, file), EmberAISkillInstaller.TemplateSourceDirectory))
                    RewriteVersionMarker(file, "0.0.0", "0.0.0");
        }

        private static string PatchProjectHash(string projectRoot)
        {
            var hashes = new List<string>();
            foreach (string relative in TemplateDirNames)
            {
                string path = Path.Combine(projectRoot, "Assets", relative);
                EmberTemplateTransaction.ValidateDirectorySafety(path);
                if (File.Exists(path)) throw new InvalidDataException("模板管理目录被文件占用：" + relative);
                hashes.Add(relative + ":" + (Directory.Exists(path) ? ComputeTemplateContentHash(path) : "<missing>"));
            }
            return string.Join("\n", hashes);
        }

        private static void ValidatePatchPlan(TemplatePatchPlan plan, TemplateInfo template)
        {
            var data = ReadPatchDeploymentData(plan.ProjectRoot);
            var active = ResolveActiveDeployment(data);
            ValidatePatchVersion(active, template);
            string baseline = ValidateDeploymentBaseline(plan.ProjectRoot, active);
            EnsureStoredContentMatchesMetadata(template, plan.SourceAssets, "增量更新");
            if (plan.TemplateId != template.id || plan.ToVersion != template.version
                || plan.FromVersion != active.version || plan.SourceHash != template.contentHash
                || plan.TemplateJson != JsonUtility.ToJson(template)
                || plan.RecordJson != JsonUtility.ToJson(data)
                || plan.ProjectHash != PatchProjectHash(plan.ProjectRoot)
                || plan.BaselineHash != ComputeTemplateContentHash(baseline))
                throw new IOException("项目、模板或部署基线在预览后变化，请重新预览增量更新。");
        }

        private static void ApplyPatchChanges(TemplatePatchPlan plan, string stagedAssets,
            IDictionary<string, TemplateConflictChoice> resolutions, TemplateInfo template)
        {
            foreach (var change in plan.Changes)
            {
                var choice = change.RecommendedChoice;
                if (change.IsConflict && (resolutions == null || !resolutions.TryGetValue(change.UnitPath, out choice)))
                    choice = TemplateConflictChoice.Unresolved;
                if (choice != TemplateConflictChoice.AcceptParent && choice != TemplateConflictChoice.KeepChild)
                    throw new InvalidOperationException("仍有未解决的增量冲突：" + change.UnitPath);
                if (choice == TemplateConflictChoice.KeepChild) continue;
                // 目录/文件类型变化不属于补丁兼容契约；作者应升级 minor。
                if (change.UnitKind == TemplateChangeUnitKind.PathTypeConflict)
                    throw new InvalidOperationException("补丁不能改变路径类型：" + change.UnitPath);
                foreach (var file in change.Files)
                {
                    string target = Path.Combine(stagedAssets, file.RelativePath);
                    if (!file.ParentExists)
                    {
                        if (File.Exists(target)) File.Delete(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(Path.Combine(plan.SourceAssets, file.RelativePath), target, true);
                    if (!IsSkillSourcePath(file.RelativePath, EmberAISkillInstaller.TemplateSourceDirectory))
                        RewriteVersionMarker(target, template.version, template.frameworkVersion);
                }
                if (change.UnitKind == TemplateChangeUnitKind.DirectoryMetadata
                    && Directory.Exists(Path.Combine(plan.SourceAssets, change.UnitPath)))
                    Directory.CreateDirectory(Path.Combine(stagedAssets, change.UnitPath));
            }
            // 仅删除已无 .meta 且为空的目录，避免 Unity 重新为已删除目录生成 GUID。
            foreach (string directory in Directory.GetDirectories(stagedAssets, "*", SearchOption.AllDirectories)
                         .OrderByDescending(p => p.Length))
                if (!File.Exists(directory + ".meta") && !Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            foreach (var change in plan.Changes.Where(c => c.UnitKind == TemplateChangeUnitKind.DirectoryMetadata))
            {
                string directory = Path.Combine(stagedAssets, change.UnitPath);
                if (Directory.Exists(directory) && !File.Exists(directory + ".meta"))
                    throw new InvalidDataException("仍保留目录内容时不能移除其 .meta，请保留本地目录元数据：" + change.UnitPath);
            }
        }

        private static void EnsurePatchEditorReady()
        {
            if (IsEmbeddedPackage())
                throw new InvalidOperationException("框架开发项目请使用模板加载/保存流程；增量更新用于消费项目。");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请在编辑器空闲且退出播放模式后更新模板。");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("已取消模板更新。");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("仍有未保存场景；请保存或关闭后再预览/应用补丁。");
            AssetDatabase.SaveAssets();
        }
        #endregion

        // --------------------------------------------------------

        #region 外部方法
        /// <summary>严格三段版本，只接受同 major.minor 内向前升级；允许跨越多个 patch。</summary>
        public static bool IsForwardTemplatePatch(string from, string to)
        {
            return Version.TryParse(from, out var oldVersion) && Version.TryParse(to, out var newVersion)
                && oldVersion.Build >= 0 && newVersion.Build >= 0 && oldVersion.Revision == -1 && newVersion.Revision == -1
                && oldVersion.Major == newVersion.Major && oldVersion.Minor == newVersion.Minor
                && newVersion.Build > oldVersion.Build;
        }

        /// <summary>预览同模板补丁升级；只建立临时比较副本，不修改业务文件。</summary>
        public static TemplatePatchPlan PreviewTemplatePatch(string templateId)
        {
            EnsurePatchEditorReady();
            var template = GetCompatibleTemplates().Find(t => t.id == templateId)
                ?? throw new InvalidOperationException("模板不存在或与当前框架不兼容。");
            return BuildTemplatePatchPlan(Directory.GetParent(Application.dataPath).FullName,
                Path.Combine(GetResolvedPath(PACKAGE), "Templates~", templateId, "Assets"), template);
        }

        internal static TemplatePatchPlan BuildTemplatePatchPlan(string projectRoot, string sourceAssets, TemplateInfo template)
        {
            var data = ReadPatchDeploymentData(projectRoot);
            var active = ResolveActiveDeployment(data);
            ValidatePatchVersion(active, template);
            EnsureStoredContentMatchesMetadata(template, sourceAssets, "增量预览");
            string baseline = ValidateDeploymentBaseline(projectRoot, active);
            var plan = new TemplatePatchPlan
            {
                TemplateId = template.id, FromVersion = active.version, ToVersion = template.version,
                ProjectRoot = projectRoot, SourceAssets = sourceAssets, SourceHash = template.contentHash,
                BaselineHash = ComputeTemplateContentHash(baseline), ProjectHash = PatchProjectHash(projectRoot),
                RecordJson = JsonUtility.ToJson(data), TemplateJson = JsonUtility.ToJson(template)
            };
            string stage = Path.Combine(projectRoot, "Temp", "EmberPatchPreview-" + Guid.NewGuid().ToString("N") + "~");
            try
            {
                string old = Path.Combine(stage, "Old"), incoming = Path.Combine(stage, "Incoming"), local = Path.Combine(stage, "Local");
                CopyPatchAssets(baseline, old, true);
                CopyPatchAssets(sourceAssets, incoming, true);
                CopyPatchAssets(Path.Combine(projectRoot, "Assets"), local, true);
                EmberTemplateInheritanceEngine.ValidateCaseInsensitivePathUniqueness(
                    new[] { old, incoming, local }.SelectMany(root =>
                        Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(file => RelativePath(root, file)))
                        .Distinct(StringComparer.Ordinal));
                plan.Changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(old, incoming, local).AsReadOnly();
                ValidatePatchPlan(plan, template);
                return plan;
            }
            finally { TryCleanTransactionPath(stage); }
        }

        /// <summary>应用已审阅补丁；AcceptParent=采用新版，KeepChild=保留本地。所有冲突必须明确解决。</summary>
        public static int ApplyTemplatePatch(TemplatePatchPlan plan, IDictionary<string, TemplateConflictChoice> resolutions)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            EnsurePatchEditorReady();
            var template = GetCompatibleTemplates().Find(t => t.id == plan.TemplateId)
                ?? throw new InvalidOperationException("模板不存在或与当前框架不兼容。");
            string root = Directory.GetParent(Application.dataPath).FullName;
            string source = Path.Combine(GetResolvedPath(PACKAGE), "Templates~", template.id, "Assets");
            if (Path.GetFullPath(root) != Path.GetFullPath(plan.ProjectRoot)
                || Path.GetFullPath(source) != Path.GetFullPath(plan.SourceAssets))
                throw new InvalidOperationException("补丁预览不属于当前项目或包。");
            int count = CommitTemplatePatch(plan, template, resolutions, true);
            AssetDatabase.Refresh();
            RegisterBuildSettings(true);
            EmberSceneMappingCreator.EnsureAndRescan();
            return count;
        }

        internal static int CommitTemplatePatch(TemplatePatchPlan plan, TemplateInfo template,
            IDictionary<string, TemplateConflictChoice> resolutions, bool confirmSkills = false, Action<int> faultInjector = null)
        {
            ValidatePatchPlan(plan, template);
            string stage = Path.Combine(plan.ProjectRoot, "Temp", "EmberPatchApply-" + Guid.NewGuid().ToString("N") + "~");
            string assets = Path.Combine(stage, "Assets");
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                CopyPatchAssets(Path.Combine(plan.ProjectRoot, "Assets"), assets, false);
                ApplyPatchChanges(plan, assets, resolutions, template);
                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(assets, out _, out string error))
                    throw new InvalidDataException("增量结果资源校验失败：" + error);
                EnsureNoTemplateGuidCollisions(plan.ProjectRoot, assets, true);
                var skills = confirmSkills ? ConfirmLifecycleSkills(plan.ProjectRoot, assets, template, false)
                    : PreviewSkills(plan.ProjectRoot, assets, template, template.id);
                EmberAISkillInstaller.ValidateTemplatePreview(skills);
                var targets = new List<TemplateTransactionTarget>();
                int count = 0;
                // 逐文件提交，仅写发生变化的文件；本地独有内容与顶层目录 GUID 保持原位。
                foreach (string relative in TemplateDirNames)
                {
                    string destination = Path.Combine(plan.ProjectRoot, "Assets", relative);
                    string prepared = Path.Combine(assets, relative);
                    var files = new HashSet<string>(StringComparer.Ordinal);
                    if (Directory.Exists(destination))
                        foreach (string file in Directory.GetFiles(destination, "*", SearchOption.AllDirectories))
                            files.Add(relative + "/" + RelativePath(destination, file));
                    if (Directory.Exists(prepared))
                        foreach (string file in Directory.GetFiles(prepared, "*", SearchOption.AllDirectories))
                            files.Add(relative + "/" + RelativePath(prepared, file));
                    // 新增/删除目录作为事务单元，保证空目录及其 GUID 不会在导入后被重新生成。
                    var directoryTargets = new List<string>();
                    var candidates = new List<string>();
                    if (!Directory.Exists(destination) && Directory.Exists(prepared)) candidates.Add(relative);
                    if (Directory.Exists(destination))
                        candidates.AddRange(Directory.GetDirectories(destination, "*", SearchOption.AllDirectories)
                            .Select(p => relative + "/" + RelativePath(destination, p))
                            .Where(p => !Directory.Exists(Path.Combine(assets, p))));
                    if (Directory.Exists(prepared))
                        candidates.AddRange(Directory.GetDirectories(prepared, "*", SearchOption.AllDirectories)
                            .Select(p => relative + "/" + RelativePath(prepared, p))
                            .Where(p => !Directory.Exists(Path.Combine(plan.ProjectRoot, "Assets", p))));
                    foreach (string directory in candidates.OrderBy(p => p.Length))
                    {
                        if (directoryTargets.Any(p => directory.StartsWith(p + "/", StringComparison.Ordinal))) continue;
                        directoryTargets.Add(directory);
                        string stagedDirectory = Path.Combine(assets, directory);
                        targets.Add(new TemplateTransactionTarget(stagedDirectory,
                            Path.Combine(plan.ProjectRoot, "Assets", directory), !Directory.Exists(stagedDirectory)));
                    }
                    foreach (string file in files.OrderBy(p => p, StringComparer.Ordinal))
                    {
                        string stagedFile = Path.Combine(assets, file), liveFile = Path.Combine(plan.ProjectRoot, "Assets", file);
                        if (File.Exists(stagedFile) && File.Exists(liveFile)
                            && File.ReadAllBytes(stagedFile).SequenceEqual(File.ReadAllBytes(liveFile))) continue;
                        if (!directoryTargets.Any(p => file.StartsWith(p + "/", StringComparison.Ordinal)))
                            targets.Add(new TemplateTransactionTarget(stagedFile, liveFile, !File.Exists(stagedFile)));
                        count++;
                    }
                }
                AddDeploymentBaseline(targets, stage, plan.ProjectRoot, plan.SourceAssets, template);
                AddDeploymentRecord(targets, stage, plan.ProjectRoot, template);
                AddPreparedSkills(targets, skills, true);
                ValidatePatchPlan(plan, template);
                EmberAISkillInstaller.ValidateTemplatePreview(skills);
                EnsureNoTemplateGuidCollisions(plan.ProjectRoot, assets, true);
                EmberTemplateTransaction.CommitPreparedTargets(targets, faultInjector);
                return count;
            }
            finally
            {
                TryCleanTransactionPath(stage);
                AssetDatabase.AllowAutoRefresh();
            }
        }

        /// <summary>可复制给本地恢复 Skill 的冲突交接日志；不包含文件正文，不是覆盖授权。</summary>
        public static string BuildTemplatePatchConflictLog(TemplatePatchPlan plan,
            IDictionary<string, TemplateConflictChoice> resolutions = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var lines = new List<string>
            {
                "Ember 模板增量冲突日志 v1",
                "生成时间 UTC: " + DateTime.UtcNow.ToString("o"),
                "项目: " + Path.GetFullPath(plan.ProjectRoot),
                "模板: " + plan.TemplateId,
                "版本: " + plan.FromVersion + " -> " + plan.ToVersion,
                "旧模板原稿: " + Path.Combine(plan.ProjectRoot, DeploymentBaselinePath, "Assets"),
                "新版模板: " + Path.GetFullPath(plan.SourceAssets),
                "本地内容: " + Path.Combine(plan.ProjectRoot, "Assets"),
                "部署记录: " + Path.Combine(plan.ProjectRoot, DeployedRecordsPath),
                "旧基线树 hash: " + plan.BaselineHash,
                "新版树 hash: " + plan.SourceHash,
                "本地受管目录指纹:",
                plan.ProjectHash,
                "",
                "日志是诊断快照，不代表冲突已解决或授权覆盖。先核对项目身份、版本及指纹。",
                "单文件 hash 使用模板算法（文本 CRLF 归一化；生成版本头统一为 0.0.0）；原稿路径下保留真实版本头。",
                "Skill 只修改项目本地文件；不得修改包内模板或旧基线，不得自行推进部署版本。",
                "合并完成后回到项目中心重新预览；同文件仍会显示双方修改，核对合并结果后选择保留本地，再应用。",
                ""
            };
            foreach (var change in plan.Changes.Where(c => c.IsConflict))
            {
                var choice = TemplateConflictChoice.Unresolved;
                if (resolutions != null && resolutions.TryGetValue(change.UnitPath, out var selected)) choice = selected;
                lines.Add("冲突: " + change.UnitPath);
                lines.Add("类型: " + change.Kind + " / " + change.UnitKind);
                lines.Add("当前选择: " + choice + "（尚未提交）");
                foreach (var file in change.Files)
                {
                    lines.Add("  文件: " + file.RelativePath);
                    lines.Add("  旧版: " + (file.OldHash ?? "<不存在>") + " | GUID: " + (file.OldGuid ?? "-"));
                    lines.Add("  新版: " + (file.ParentHash ?? "<不存在>") + " | GUID: " + (file.ParentGuid ?? "-"));
                    lines.Add("  本地: " + (file.ChildHash ?? "<不存在>") + " | GUID: " + (file.ChildGuid ?? "-"));
                }
                lines.Add("");
            }
            if (!plan.Changes.Any(c => c.IsConflict)) lines.Add("没有文件级冲突。");
            return string.Join("\n", lines);
        }

        /// <summary>旧记录恢复基线：仅接受与部署 hash 完全匹配的旧模板原稿，项目 Assets 不变。</summary>
        public static void RestoreTemplateDeploymentBaseline(string oldTemplateAssets)
        {
            EnsurePatchEditorReady();
            RestoreTemplateDeploymentBaseline(Directory.GetParent(Application.dataPath).FullName, oldTemplateAssets);
        }

        internal static void RestoreTemplateDeploymentBaseline(string projectRoot, string oldTemplateAssets)
        {
            var data = ReadPatchDeploymentData(projectRoot);
            var active = ResolveActiveDeployment(data);
            string hash = ComputeTemplateContentHash(oldTemplateAssets);
            if (string.IsNullOrEmpty(active.contentHash) || hash != active.contentHash)
                throw new InvalidDataException("所选旧模板与部署记录 hash 不匹配，不能作为升级基线。");
            var template = new TemplateInfo { schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                id = active.templateId, version = active.version,
                frameworkVersion = active.frameworkVersion, contentHash = hash, versionedContentHash = hash };
            string stage = Path.Combine(projectRoot, "Temp", "EmberBaseline-" + Guid.NewGuid().ToString("N") + "~");
            try
            {
                var targets = new List<TemplateTransactionTarget>();
                AddDeploymentBaseline(targets, stage, projectRoot, oldTemplateAssets, template);
                if (JsonUtility.ToJson(data) != JsonUtility.ToJson(ReadPatchDeploymentData(projectRoot)))
                    throw new IOException("部署记录已变化，请重试。");
                EmberTemplateTransaction.CommitPreparedTargets(targets);
            }
            finally { TryCleanTransactionPath(stage); }
        }
        #endregion
    }
}
