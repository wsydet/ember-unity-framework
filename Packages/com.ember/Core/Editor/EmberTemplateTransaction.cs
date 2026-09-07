// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;

using Ember.Basic;

using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>Editor-only 文件事务：提交预先准备好的目录/文件，并在任一步失败时恢复全部目标。</summary>
    internal static class EmberTemplateTransaction
    {
        #region 内部参数

        private const string BackupSuffix = ".ember-backup~";

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>在独立暂存区应用父模板同步计划，并原子提交 Assets、ParentSnapshot 与 metadata。</summary>
        internal static TemplateInfo ApplyParentSync(ParentSyncTransactionRequest request)
        {
            ValidateParentSyncRequest(request);
            var applyModes = ResolveApplyModes(request.Plan, request.Resolutions);
            var templatesRoot = Directory.GetParent(request.ChildTemplateRoot)?.FullName
                ?? throw new InvalidOperationException("无法解析模板根目录。");
            var stageRoot = Path.Combine(
                templatesRoot,
                $".{request.Child.id}.sync-staging-{Guid.NewGuid():N}~");
            var stagedAssets = Path.Combine(stageRoot, "Assets");
            var stagedSnapshot = Path.Combine(stageRoot, "ParentSnapshot~", "Assets");
            var stagedMetadata = Path.Combine(stageRoot, "template.json");

            try
            {
                CopyDirectory(request.ChildAssetsPath, stagedAssets);
                ApplyChangesToStage(request, applyModes, stagedAssets);
                PruneEmptyDirectories(stagedAssets);

                if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                        stagedAssets,
                        out var contentHash,
                        out var validationError))
                {
                    throw new InvalidOperationException(
                        $"同步结果未通过资源/.meta/GUID 校验：{validationError}");
                }

                bool contentChanged = !string.Equals(
                    contentHash,
                    request.Child.contentHash,
                    StringComparison.Ordinal);
                if (contentChanged && !request.VersionBumpField.HasValue)
                {
                    throw new InvalidOperationException(
                        "父模板同步改变了派生模板内容，必须同时选择主/次/补丁 bump。");
                }

                CopyDirectory(request.ParentAssetsPath, stagedSnapshot);
                ValidateLiveAssets(
                    stagedSnapshot,
                    request.Parent.contentHash,
                    "暂存父模板基线");
                var updated = EmberTemplateInheritanceEngine.Clone(request.Child);
                updated.parentVersion = request.Parent.version;
                updated.parentContentHash = request.Parent.contentHash;
                updated.frameworkVersion = request.Parent.frameworkVersion;
                updated.contentHash = contentHash;
                if (request.VersionBumpField.HasValue)
                {
                    updated = EmberTemplateInheritanceEngine.BuildBumpedMetadata(
                        updated,
                        request.VersionBumpField.Value);
                }

                WriteTemplateJson(stagedMetadata, updated);
                ValidateLiveAssets(
                    request.ChildAssetsPath,
                    request.Plan.ChildContentHash,
                    "派生模板（提交前复核）");
                ValidateLiveAssets(
                    request.ParentAssetsPath,
                    request.Plan.ParentContentHash,
                    "父模板（提交前复核）");
                ValidateLiveAssets(
                    request.ParentSnapshotAssetsPath,
                    request.Plan.SnapshotContentHash,
                    "父模板基线（提交前复核）");
                CommitPreparedTargets(
                    new[]
                    {
                        new TemplateTransactionTarget(
                            stagedAssets,
                            request.ChildAssetsPath),
                        new TemplateTransactionTarget(
                            stagedSnapshot,
                            request.ParentSnapshotAssetsPath),
                        new TemplateTransactionTarget(
                            stagedMetadata,
                            Path.Combine(request.ChildTemplateRoot, "template.json"))
                    },
                    request.FaultInjector);
                return updated;
            }
            finally
            {
                try { CleanPath(stageRoot); }
                catch { /* 暂存区清理失败不覆盖事务本身的成功/失败结果。 */ }
            }
        }

        /// <summary>递归复制完整目录树（包含空目录、.meta 与仅由 folderAsset .meta 表示的空目录）。</summary>
        internal static int CopyDirectory(string source, string destination)
        {
            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"源目录不存在：{source}");

            CleanPath(destination);
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = directory.Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relativePath));
            }

            int count = 0;
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(source.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var target = Path.Combine(destination, relativePath);
                var directory = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.Copy(file, target, true);
                count++;
            }

            MaterializeFolderMetadataDirectories(destination);
            return count;
        }

        /// <summary>
        /// 原子提交已准备目标。所有目标先改名为备份，再将 staged 内容落位；
        /// 任一步抛异常都会撤下新内容并逆序恢复全部备份。
        /// </summary>
        internal static void CommitPreparedTargets(
            IReadOnlyList<TemplateTransactionTarget> targets,
            Action<int> faultInjector = null)
        {
            if (targets == null || targets.Count == 0)
                throw new ArgumentException("事务目标不能为空。", nameof(targets));

            ValidateTargets(targets);
            var states = new List<TemplateTransactionState>();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    var state = new TemplateTransactionState(target);
                    states.Add(state);
                    EnsureParentDirectory(target.DestinationPath);
                    if (PathExists(target.DestinationPath))
                    {
                        MovePath(target.DestinationPath, state.BackupPath);
                        state.BackupCreated = true;
                    }
                }

                for (int i = 0; i < states.Count; i++)
                {
                    var state = states[i];
                    if (PathExists(state.Target.StagedPath))
                    {
                        MovePath(state.Target.StagedPath, state.Target.DestinationPath);
                        state.NewContentPlaced = true;
                    }

                    faultInjector?.Invoke(i);
                }
            }
            catch
            {
                Rollback(states);
                throw;
            }

            foreach (var state in states)
            {
                if (!state.BackupCreated) continue;
                try { CleanPath(state.BackupPath); }
                catch { /* 新内容已完整提交；残留备份保留为可恢复副本。 */ }
            }
        }

        internal static void CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            {
                File.Delete(path);
                return;
            }
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        internal static void WriteTemplateJson(string path, TemplateInfo info)
        {
            WriteJson(path, info);
        }

        internal static void WriteJson(string path, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            EnsureParentDirectory(path);
            File.WriteAllText(
                path,
                JsonUtility.ToJson(value, true),
                new System.Text.UTF8Encoding(false));
        }

        private static void ValidateParentSyncRequest(ParentSyncTransactionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Plan == null || request.Child == null || request.Parent == null)
                throw new InvalidOperationException("同步事务缺少计划或模板 metadata。");
            if (!request.Plan.IsEligible
                || request.Plan.Status != TemplateSyncStatus.ParentChanged)
                throw new InvalidOperationException("同步计划当前不可应用，请重新计算 dry-run。");
            if (!string.Equals(request.Plan.TemplateId, request.Child.id, StringComparison.Ordinal)
                || !string.Equals(request.Plan.ParentId, request.Parent.id, StringComparison.Ordinal))
                throw new InvalidOperationException("同步计划与目标模板不一致。");
            if (!string.Equals(
                    request.Plan.SnapshotContentHash,
                    request.Child.parentContentHash,
                    StringComparison.Ordinal)
                || !string.Equals(
                    request.Plan.ParentContentHash,
                    request.Parent.contentHash,
                    StringComparison.Ordinal)
                || !string.Equals(
                    request.Plan.ChildContentHash,
                    request.Child.contentHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("同步计划与当前模板 metadata hash 不一致，请重新计算 dry-run。");
            }

            if (string.IsNullOrWhiteSpace(request.ChildTemplateRoot)
                || string.IsNullOrWhiteSpace(request.ChildAssetsPath)
                || string.IsNullOrWhiteSpace(request.ParentAssetsPath)
                || string.IsNullOrWhiteSpace(request.ParentSnapshotAssetsPath))
                throw new InvalidOperationException("同步事务路径不能为空。");

            ValidateLiveAssets(
                request.ChildAssetsPath,
                request.Plan.ChildContentHash,
                "派生模板");
            ValidateLiveAssets(
                request.ParentAssetsPath,
                request.Plan.ParentContentHash,
                "父模板");
            ValidateLiveAssets(
                request.ParentSnapshotAssetsPath,
                request.Plan.SnapshotContentHash,
                "父模板基线");

            var currentFilePlan = new TemplateSyncPlan(
                request.Plan.TemplateId,
                request.Plan.ParentId,
                TemplateSyncStatus.ParentChanged,
                null,
                request.Plan.SnapshotContentHash,
                request.Plan.ParentContentHash,
                request.Plan.ChildContentHash,
                EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                    request.ParentSnapshotAssetsPath,
                    request.ParentAssetsPath,
                    request.ChildAssetsPath),
                true);
            if (!EmberTemplateSceneMergePlanner.TryValidatePreviewPlan(
                    request.Plan,
                    currentFilePlan,
                    out var planError))
            {
                throw new InvalidOperationException(
                    "同步计划已过期；事务 stage 前的文件级复核失败。"
                    + (string.IsNullOrEmpty(planError) ? string.Empty : " " + planError));
            }
        }

        private static void ValidateLiveAssets(
            string assetsPath,
            string expectedHash,
            string label)
        {
            if (!EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                    assetsPath,
                    out var liveHash,
                    out var validationError))
            {
                throw new InvalidOperationException(
                    $"{label}未通过资源/.meta/GUID 校验：{validationError}");
            }
            if (!string.Equals(liveHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{label}内容已变化，不能应用旧同步计划；请重新计算 dry-run。");
            }
        }

        private static Dictionary<string, TemplateChangeApplyMode> ResolveApplyModes(
            TemplateSyncPlan plan,
            IReadOnlyDictionary<string, TemplateConflictChoice> resolutions)
        {
            var result = new Dictionary<string, TemplateChangeApplyMode>(StringComparer.Ordinal);
            foreach (var change in plan.Changes)
            {
                if (!change.IsConflict)
                {
                    if (!change.ApplyMode.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"同步单元缺少应用模式：{change.UnitPath}");
                    }
                    result.Add(change.UnitPath, change.ApplyMode.Value);
                    continue;
                }

                if (resolutions == null
                    || !resolutions.TryGetValue(change.UnitPath, out var choice)
                    || (choice != TemplateConflictChoice.KeepChild
                        && choice != TemplateConflictChoice.AcceptParent))
                {
                    throw new InvalidOperationException(
                        $"同步仍有未解决冲突：{change.UnitPath}");
                }
                result.Add(
                    change.UnitPath,
                    choice == TemplateConflictChoice.AcceptParent
                        ? TemplateChangeApplyMode.AcceptParent
                        : TemplateChangeApplyMode.KeepChild);
            }
            return result;
        }

        private static void ApplyChangesToStage(
            ParentSyncTransactionRequest request,
            Dictionary<string, TemplateChangeApplyMode> applyModes,
            string stagedAssets)
        {
            foreach (var change in request.Plan.Changes)
            {
                var applyMode = applyModes[change.UnitPath];
                if (applyMode == TemplateChangeApplyMode.KeepChild) continue;
                if (applyMode == TemplateChangeApplyMode.SemanticMerge)
                {
                    ApplySemanticMergeToStage(request, change, stagedAssets);
                    continue;
                }

                ApplyParentChangeToStage(request, change, stagedAssets);
            }
        }

        private static void ApplyParentChangeToStage(
            ParentSyncTransactionRequest request,
            TemplateChange change,
            string stagedAssets)
        {
            bool parentUnitIsDirectory = false;
            if (change.UnitKind == TemplateChangeUnitKind.PathTypeConflict)
            {
                parentUnitIsDirectory = Directory.Exists(CombineRelativePath(
                    request.ParentAssetsPath,
                    change.UnitPath));
                PreparePathTypeForParent(
                    stagedAssets,
                    request.ParentAssetsPath,
                    change.UnitPath);
            }

            foreach (var file in change.Files)
            {
                // 目录本身不是物理文件；PathTypeConflict 单元中的同路径“文件缺失”
                // 不能把刚建立的父目录再次删除。
                if (parentUnitIsDirectory
                    && string.Equals(
                        file.RelativePath,
                        change.UnitPath,
                        StringComparison.Ordinal))
                    continue;

                var stagedPath = CombineRelativePath(stagedAssets, file.RelativePath);
                if (!file.ParentExists)
                {
                    CleanPath(stagedPath);
                    continue;
                }

                var parentPath = CombineRelativePath(
                    request.ParentAssetsPath,
                    file.RelativePath);
                EnsureParentDirectory(stagedPath);
                File.Copy(parentPath, stagedPath, true);
            }

            if (change.UnitKind == TemplateChangeUnitKind.DirectoryMetadata)
            {
                var parentDirectory = CombineRelativePath(
                    request.ParentAssetsPath,
                    change.UnitPath);
                var stagedDirectory = CombineRelativePath(stagedAssets, change.UnitPath);
                if (Directory.Exists(parentDirectory))
                    Directory.CreateDirectory(stagedDirectory);
            }
        }

        private static void ApplySemanticMergeToStage(
            ParentSyncTransactionRequest request,
            TemplateChange change,
            string stagedAssets)
        {
            var preview = change.SceneMergePreview
                ?? throw new InvalidOperationException(
                    $"场景语义同步缺少 dry-run 指纹：{change.UnitPath}");
            ValidateSemanticInputHashes(request, change);
            var mergeResult = request.YamlMerge.Merge(
                CombineRelativePath(request.ParentSnapshotAssetsPath, change.UnitPath),
                CombineRelativePath(request.ParentAssetsPath, change.UnitPath),
                CombineRelativePath(request.ChildAssetsPath, change.UnitPath));
            if (!mergeResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"场景 [{change.UnitPath}] 在事务 stage 中无法重新语义合并："
                    + (mergeResult.FailureReason ?? mergeResult.Status.ToString()));
            }
            ValidateSemanticInputHashes(request, change);

            if (!string.Equals(
                    mergeResult.ToolVersion,
                    preview.ToolVersion,
                    StringComparison.Ordinal)
                || !string.Equals(
                    mergeResult.RulesHash,
                    preview.RulesHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"场景 [{change.UnitPath}] 的 UnityYAMLMerge 工具或规则已变化，请重新预览。");
            }

            if (mergeResult.MergedBytes == null
                || !string.Equals(
                    mergeResult.MergedHash,
                    preview.MergedHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"场景 [{change.UnitPath}] 的 stage 合并结果 hash 与 dry-run 不一致，请重新预览。");
            }

            var stagedScenePath = CombineRelativePath(stagedAssets, change.UnitPath);
            EnsureParentDirectory(stagedScenePath);
            File.WriteAllBytes(stagedScenePath, mergeResult.MergedBytes);
            if (!string.Equals(
                    CryptographyUtils.GetMD5File(stagedScenePath),
                    preview.MergedHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"场景 [{change.UnitPath}] 写入 stage 后的结果 hash 校验失败。");
            }

            TemplateFileChange metaFile = null;
            foreach (var file in change.Files)
            {
                if (string.Equals(
                        file.RelativePath,
                        change.UnitPath + ".meta",
                        StringComparison.Ordinal))
                {
                    metaFile = file;
                    break;
                }
            }

            var stagedMetaPath = stagedScenePath + ".meta";
            if (metaFile == null
                || !metaFile.ChildExists
                || !File.Exists(stagedMetaPath)
                || !string.Equals(
                    CryptographyUtils.GetMD5File(stagedMetaPath),
                    metaFile.ChildHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"场景 [{change.UnitPath}] 的 .meta 在 stage 中发生变化，事务已中止。");
            }
        }

        private static void ValidateSemanticInputHashes(
            ParentSyncTransactionRequest request,
            TemplateChange change)
        {
            foreach (var file in change.Files)
            {
                ValidateSemanticInputHash(
                    request.ParentSnapshotAssetsPath,
                    file.RelativePath,
                    file.OldHash,
                    "O");
                ValidateSemanticInputHash(
                    request.ParentAssetsPath,
                    file.RelativePath,
                    file.ParentHash,
                    "N");
                ValidateSemanticInputHash(
                    request.ChildAssetsPath,
                    file.RelativePath,
                    file.ChildHash,
                    "C");
            }
        }

        private static void ValidateSemanticInputHash(
            string root,
            string relativePath,
            string expectedHash,
            string label)
        {
            var path = CombineRelativePath(root, relativePath);
            if (string.IsNullOrEmpty(expectedHash)
                || !File.Exists(path)
                || !string.Equals(
                    CryptographyUtils.GetMD5File(path),
                    expectedHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"场景语义合并输入 {label} [{relativePath}] 的 hash 已变化，请重新预览。");
            }
        }

        private static void PreparePathTypeForParent(
            string stagedAssets,
            string parentAssets,
            string unitPath)
        {
            var parentPath = CombineRelativePath(parentAssets, unitPath);
            var stagedPath = CombineRelativePath(stagedAssets, unitPath);
            if (File.Exists(parentPath) && Directory.Exists(stagedPath))
                Directory.Delete(stagedPath, true);
            else if (Directory.Exists(parentPath) && File.Exists(stagedPath))
                File.Delete(stagedPath);

            if (Directory.Exists(parentPath) && !Directory.Exists(stagedPath))
                Directory.CreateDirectory(stagedPath);
        }

        private static string CombineRelativePath(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void PruneEmptyDirectories(string root)
        {
            var directories = Directory.GetDirectories(root, "*", SearchOption.AllDirectories);
            Array.Sort(directories, (left, right) => right.Length.CompareTo(left.Length));
            foreach (var directory in directories)
            {
                if (Directory.GetFileSystemEntries(directory).Length == 0
                    && !File.Exists(directory + ".meta"))
                    Directory.Delete(directory);
            }
        }

        private static void ValidateTargets(IReadOnlyList<TemplateTransactionTarget> targets)
        {
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var target in targets)
            {
                if (target == null
                    || string.IsNullOrWhiteSpace(target.StagedPath)
                    || string.IsNullOrWhiteSpace(target.DestinationPath))
                {
                    throw new InvalidOperationException("事务目标路径不能为空。");
                }

                var destination = Path.GetFullPath(target.DestinationPath);
                if (!destinations.Add(destination))
                    throw new InvalidOperationException($"事务目标重复：{destination}");

                if (!target.AllowMissingStage && !PathExists(target.StagedPath))
                    throw new InvalidOperationException($"事务暂存内容不存在：{target.StagedPath}");

                var backup = destination + BackupSuffix;
                if (PathExists(backup))
                {
                    throw new InvalidOperationException(
                        $"事务备份已存在，请先人工确认并处理：{backup}");
                }
            }
        }

        private static void Rollback(List<TemplateTransactionState> states)
        {
            Exception rollbackError = null;
            for (int i = states.Count - 1; i >= 0; i--)
            {
                var state = states[i];
                try
                {
                    if (state.NewContentPlaced && PathExists(state.Target.DestinationPath))
                        CleanPath(state.Target.DestinationPath);
                    if (state.BackupCreated && PathExists(state.BackupPath))
                        MovePath(state.BackupPath, state.Target.DestinationPath);
                }
                catch (Exception ex)
                {
                    rollbackError ??= ex;
                }
            }

            if (rollbackError != null)
            {
                throw new IOException(
                    "模板事务失败，且自动回滚未能完整恢复；请保留 *.ember-backup~ 并人工检查。",
                    rollbackError);
            }
        }

        private static bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        private static void MovePath(string source, string destination)
        {
            if (File.Exists(source))
            {
                File.Move(source, destination);
                return;
            }
            if (Directory.Exists(source))
            {
                Directory.Move(source, destination);
                return;
            }
            throw new FileNotFoundException($"待移动路径不存在：{source}", source);
        }

        private static void EnsureParentDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static void MaterializeFolderMetadataDirectories(string root)
        {
            foreach (var metaPath in Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories))
            {
                bool isFolderMetadata = false;
                foreach (var line in File.ReadLines(metaPath))
                {
                    if (!string.Equals(
                            line.Trim(),
                            "folderAsset: yes",
                            StringComparison.Ordinal))
                        continue;
                    isFolderMetadata = true;
                    break;
                }
                if (!isFolderMetadata) continue;

                var directoryPath = metaPath.Substring(0, metaPath.Length - ".meta".Length);
                if (!File.Exists(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
            }
        }

        private sealed class TemplateTransactionState
        {
            #region 内部参数

            internal TemplateTransactionTarget Target { get; }
            internal string BackupPath { get; }
            internal bool BackupCreated { get; set; }
            internal bool NewContentPlaced { get; set; }

            #endregion

            // --------------------------------------------------------

            #region 外部方法

            internal TemplateTransactionState(TemplateTransactionTarget target)
            {
                Target = target;
                BackupPath = Path.GetFullPath(target.DestinationPath) + BackupSuffix;
            }

            #endregion
        }

        #endregion
    }

    /// <summary>父模板同步事务所需的完整、已复核输入。</summary>
    internal sealed class ParentSyncTransactionRequest
    {
        #region 内部参数

        internal TemplateSyncPlan Plan { get; }
        internal TemplateInfo Child { get; }
        internal TemplateInfo Parent { get; }
        internal string ChildTemplateRoot { get; }
        internal string ChildAssetsPath { get; }
        internal string ParentAssetsPath { get; }
        internal string ParentSnapshotAssetsPath { get; }
        internal IReadOnlyDictionary<string, TemplateConflictChoice> Resolutions { get; }
        internal int? VersionBumpField { get; }
        internal Action<int> FaultInjector { get; }
        internal EmberUnityYamlMerge YamlMerge { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal ParentSyncTransactionRequest(
            TemplateSyncPlan plan,
            TemplateInfo child,
            TemplateInfo parent,
            string childTemplateRoot,
            string childAssetsPath,
            string parentAssetsPath,
            string parentSnapshotAssetsPath,
            IReadOnlyDictionary<string, TemplateConflictChoice> resolutions,
            int? versionBumpField,
            Action<int> faultInjector = null,
            EmberUnityYamlMerge yamlMerge = null)
        {
            Plan = plan;
            Child = child;
            Parent = parent;
            ChildTemplateRoot = childTemplateRoot;
            ChildAssetsPath = childAssetsPath;
            ParentAssetsPath = parentAssetsPath;
            ParentSnapshotAssetsPath = parentSnapshotAssetsPath;
            Resolutions = resolutions;
            VersionBumpField = versionBumpField;
            FaultInjector = faultInjector;
            YamlMerge = yamlMerge ?? new EmberUnityYamlMerge();
        }

        #endregion
    }

    /// <summary>一个已准备好的事务落位目标。</summary>
    internal sealed class TemplateTransactionTarget
    {
        #region 内部参数

        internal string StagedPath { get; }
        internal string DestinationPath { get; }
        internal bool AllowMissingStage { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateTransactionTarget(
            string stagedPath,
            string destinationPath,
            bool allowMissingStage = false)
        {
            StagedPath = stagedPath;
            DestinationPath = destinationPath;
            AllowMissingStage = allowMissingStage;
        }

        #endregion
    }
}
