// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ember.Core.Editor
{
    /// <summary>
    /// 显式 dry-run 的场景语义计划增强层；文件级 O/N/C 计划保持为唯一安全底座。
    /// 本类只替换可自动合并的 .unity 并发修改，不执行模板写入。
    /// </summary>
    internal static class EmberTemplateSceneMergePlanner
    {
        #region 内部参数

        private const int MaxCacheEntries = 256;

        private static readonly Dictionary<string, SceneSemanticMergePreview> PreviewCache =
            new(StringComparer.Ordinal);

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 用 UnityYAMLMerge 增强已通过同步闸门的文件级计划。
        /// 仅显式预览入口可以调用；普通状态刷新不得调用本方法。
        /// </summary>
        internal static TemplateSyncPlan EnhancePlan(
            TemplateSyncPlan filePlan,
            string oldParentAssetsPath,
            string currentParentAssetsPath,
            string currentChildAssetsPath,
            EmberUnityYamlMerge yamlMerge)
        {
            if (filePlan == null) throw new ArgumentNullException(nameof(filePlan));
            if (yamlMerge == null) throw new ArgumentNullException(nameof(yamlMerge));
            if (filePlan.Status != TemplateSyncStatus.ParentChanged
                || !filePlan.IsEligible
                || filePlan.Changes.Count == 0)
                return filePlan;

            var changes = new List<TemplateChange>(filePlan.Changes.Count);
            bool enhanced = false;
            foreach (var change in filePlan.Changes)
            {
                if (!TryGetSceneCandidate(change, out var sceneFile, out var sceneMeta))
                {
                    changes.Add(change);
                    continue;
                }

                var preview = GetOrCreatePreview(
                    filePlan.TemplateId,
                    change.UnitPath,
                    sceneFile,
                    oldParentAssetsPath,
                    currentParentAssetsPath,
                    currentChildAssetsPath,
                    yamlMerge);
                enhanced = true;
                if (preview.Status == SceneSemanticMergeStatus.Succeeded)
                {
                    changes.Add(new TemplateChange(
                        change.UnitPath,
                        TemplateChangeUnitKind.SceneSemantic,
                        TemplateChangeKind.AutoMergeScene,
                        TemplateConflictChoice.KeepChild,
                        new List<TemplateFileChange> { sceneFile, sceneMeta },
                        TemplateChangeApplyMode.SemanticMerge,
                        preview,
                        !string.Equals(
                            preview.MergedHash,
                            preview.ChildHash,
                            StringComparison.Ordinal)));
                }
                else
                {
                    changes.Add(new TemplateChange(
                        change.UnitPath,
                        change.UnitKind,
                        change.Kind,
                        change.RecommendedChoice,
                        new List<TemplateFileChange>(change.Files),
                        change.ApplyMode,
                        preview,
                        change.WillChangeChildContent));
                }
            }

            if (!enhanced) return filePlan;
            return new TemplateSyncPlan(
                filePlan.TemplateId,
                filePlan.ParentId,
                filePlan.Status,
                filePlan.Error,
                filePlan.SnapshotContentHash,
                filePlan.ParentContentHash,
                filePlan.ChildContentHash,
                changes,
                filePlan.IsEligible);
        }

        internal static void ClearPreviewCache()
        {
            PreviewCache.Clear();
        }

        /// <summary>
        /// 校验显式 dry-run 计划仍然严格建立在当前文件级计划之上。
        /// 场景语义单元只允许替换同路径的 .unity 并发修改冲突，其余单元必须逐项一致。
        /// </summary>
        internal static bool TryValidatePreviewPlan(
            TemplateSyncPlan previewPlan,
            TemplateSyncPlan filePlan,
            out string error)
        {
            error = null;
            if (previewPlan == null || filePlan == null)
            {
                error = "同步计划为空。";
                return false;
            }

            if (filePlan.Status != TemplateSyncStatus.ParentChanged || !filePlan.IsEligible)
            {
                error = filePlan.Error ?? "当前文件级同步计划不可应用。";
                return false;
            }

            if (previewPlan.Status != filePlan.Status
                || !previewPlan.IsEligible
                || !string.Equals(previewPlan.TemplateId, filePlan.TemplateId, StringComparison.Ordinal)
                || !string.Equals(previewPlan.ParentId, filePlan.ParentId, StringComparison.Ordinal)
                || !string.Equals(
                    previewPlan.SnapshotContentHash,
                    filePlan.SnapshotContentHash,
                    StringComparison.Ordinal)
                || !string.Equals(
                    previewPlan.ParentContentHash,
                    filePlan.ParentContentHash,
                    StringComparison.Ordinal)
                || !string.Equals(
                    previewPlan.ChildContentHash,
                    filePlan.ChildContentHash,
                    StringComparison.Ordinal)
                || previewPlan.Changes.Count != filePlan.Changes.Count)
            {
                error = "同步计划的模板、tree hash 或变更数量已变化。";
                return false;
            }

            for (int i = 0; i < previewPlan.Changes.Count; i++)
            {
                var previewChange = previewPlan.Changes[i];
                var fileChange = filePlan.Changes[i];
                if (!string.Equals(
                        previewChange.UnitPath,
                        fileChange.UnitPath,
                        StringComparison.Ordinal)
                    || !AreSameFiles(previewChange.Files, fileChange.Files))
                {
                    error = $"同步单元 [{previewChange.UnitPath}] 的文件指纹已变化。";
                    return false;
                }

                if (previewChange.ApplyMode == TemplateChangeApplyMode.SemanticMerge)
                {
                    if (!IsValidSemanticReplacement(previewChange, fileChange, out error))
                        return false;
                    continue;
                }

                if (previewChange.UnitKind != fileChange.UnitKind
                    || previewChange.Kind != fileChange.Kind
                    || previewChange.RecommendedChoice != fileChange.RecommendedChoice
                    || previewChange.ApplyMode != fileChange.ApplyMode
                    || previewChange.WillChangeChildContent
                        != fileChange.WillChangeChildContent)
                {
                    error = $"同步单元 [{previewChange.UnitPath}] 的文件级决策已变化。";
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidSemanticReplacement(
            TemplateChange previewChange,
            TemplateChange fileChange,
            out string error)
        {
            error = null;
            if (previewChange.UnitKind != TemplateChangeUnitKind.SceneSemantic
                || previewChange.Kind != TemplateChangeKind.AutoMergeScene
                || previewChange.RecommendedChoice != TemplateConflictChoice.KeepChild
                || fileChange.UnitKind != TemplateChangeUnitKind.Asset
                || fileChange.Kind != TemplateChangeKind.ConflictConcurrentModification
                || !fileChange.IsConflict
                || !string.Equals(
                    Path.GetExtension(previewChange.UnitPath),
                    ".unity",
                    StringComparison.OrdinalIgnoreCase))
            {
                error = $"同步单元 [{previewChange.UnitPath}] 不是有效的场景语义替换。";
                return false;
            }

            if (!TryGetSceneCandidate(fileChange, out var sceneFile, out _))
            {
                error = $"同步单元 [{previewChange.UnitPath}] 已不再满足场景语义合并条件。";
                return false;
            }

            var preview = previewChange.SceneMergePreview;
            if (preview == null
                || preview.Status != SceneSemanticMergeStatus.Succeeded
                || !string.Equals(preview.ScenePath, previewChange.UnitPath, StringComparison.Ordinal)
                || !string.Equals(preview.OldHash, sceneFile.OldHash, StringComparison.Ordinal)
                || !string.Equals(preview.ParentHash, sceneFile.ParentHash, StringComparison.Ordinal)
                || !string.Equals(preview.ChildHash, sceneFile.ChildHash, StringComparison.Ordinal)
                || string.IsNullOrEmpty(preview.MergedHash)
                || string.IsNullOrEmpty(preview.ToolVersion)
                || string.IsNullOrEmpty(preview.RulesHash)
                || previewChange.WillChangeChildContent
                    != !string.Equals(
                        preview.MergedHash,
                        preview.ChildHash,
                        StringComparison.Ordinal))
            {
                error = $"同步单元 [{previewChange.UnitPath}] 的语义 dry-run 指纹无效。";
                return false;
            }

            return true;
        }

        private static bool AreSameFiles(
            IReadOnlyList<TemplateFileChange> left,
            IReadOnlyList<TemplateFileChange> right)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                var leftFile = left[i];
                var rightFile = right[i];
                if (!string.Equals(leftFile.RelativePath, rightFile.RelativePath, StringComparison.Ordinal)
                    || !string.Equals(leftFile.OldHash, rightFile.OldHash, StringComparison.Ordinal)
                    || !string.Equals(leftFile.ParentHash, rightFile.ParentHash, StringComparison.Ordinal)
                    || !string.Equals(leftFile.ChildHash, rightFile.ChildHash, StringComparison.Ordinal)
                    || !string.Equals(leftFile.OldGuid, rightFile.OldGuid, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(leftFile.ParentGuid, rightFile.ParentGuid, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(leftFile.ChildGuid, rightFile.ChildGuid, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static bool TryGetSceneCandidate(
            TemplateChange change,
            out TemplateFileChange sceneFile,
            out TemplateFileChange sceneMeta)
        {
            sceneFile = null;
            sceneMeta = null;
            if (change == null
                || change.UnitKind != TemplateChangeUnitKind.Asset
                || change.Kind != TemplateChangeKind.ConflictConcurrentModification
                || !string.Equals(
                    Path.GetExtension(change.UnitPath),
                    ".unity",
                    StringComparison.OrdinalIgnoreCase))
                return false;

            foreach (var file in change.Files)
            {
                if (string.Equals(file.RelativePath, change.UnitPath, StringComparison.Ordinal))
                    sceneFile = file;
                else if (string.Equals(
                    file.RelativePath,
                    change.UnitPath + ".meta",
                    StringComparison.Ordinal))
                    sceneMeta = file;
            }

            if (sceneFile == null
                || sceneMeta == null
                || !sceneFile.OldExists
                || !sceneFile.ParentExists
                || !sceneFile.ChildExists
                || !sceneMeta.OldExists
                || !sceneMeta.ParentExists
                || !sceneMeta.ChildExists
                || string.IsNullOrEmpty(sceneMeta.OldGuid)
                || string.IsNullOrEmpty(sceneMeta.ParentGuid)
                || string.IsNullOrEmpty(sceneMeta.ChildGuid))
                return false;

            return string.Equals(
                    sceneMeta.OldGuid,
                    sceneMeta.ParentGuid,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    sceneMeta.OldGuid,
                    sceneMeta.ChildGuid,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static SceneSemanticMergePreview GetOrCreatePreview(
            string templateId,
            string scenePath,
            TemplateFileChange sceneFile,
            string oldParentAssetsPath,
            string currentParentAssetsPath,
            string currentChildAssetsPath,
            EmberUnityYamlMerge yamlMerge)
        {
            UnityYamlMergeToolInfo toolInfo = null;
            string cacheKey = null;
            if (yamlMerge.TryGetToolInfo(out toolInfo, out _))
            {
                cacheKey = BuildCacheKey(
                    templateId,
                    scenePath,
                    sceneFile,
                    toolInfo.ToolVersion,
                    toolInfo.RulesHash);
                if (PreviewCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            var mergeResult = yamlMerge.Merge(
                CombineRelativePath(oldParentAssetsPath, scenePath),
                CombineRelativePath(currentParentAssetsPath, scenePath),
                CombineRelativePath(currentChildAssetsPath, scenePath));
            var preview = new SceneSemanticMergePreview(
                scenePath,
                sceneFile.OldHash,
                sceneFile.ParentHash,
                sceneFile.ChildHash,
                mergeResult.MergedHash,
                mergeResult.ToolVersion ?? toolInfo?.ToolVersion,
                mergeResult.RulesHash ?? toolInfo?.RulesHash,
                mergeResult.ReportDigest,
                mergeResult.ReportSummary,
                mergeResult.IsSuccess
                    ? SceneSemanticMergeStatus.Succeeded
                    : SceneSemanticMergeStatus.Fallback,
                mergeResult.FailureReason);
            // 只缓存成功结果。Force Text、工具可用性、超时等失败可能是瞬态状态，
            // 再次显式 dry-run 时应允许恢复，而不是被旧失败结果永久挡住。
            if (cacheKey != null && preview.Status == SceneSemanticMergeStatus.Succeeded)
            {
                if (PreviewCache.Count >= MaxCacheEntries)
                    PreviewCache.Clear();
                PreviewCache[cacheKey] = preview;
            }
            return preview;
        }

        private static string BuildCacheKey(
            string templateId,
            string scenePath,
            TemplateFileChange sceneFile,
            string toolVersion,
            string rulesHash)
        {
            var key = new StringBuilder();
            AppendCachePart(key, templateId);
            AppendCachePart(key, scenePath);
            AppendCachePart(key, sceneFile.OldHash);
            AppendCachePart(key, sceneFile.ParentHash);
            AppendCachePart(key, sceneFile.ChildHash);
            AppendCachePart(key, toolVersion);
            AppendCachePart(key, rulesHash);
            return key.ToString();
        }

        private static void AppendCachePart(StringBuilder builder, string value)
        {
            value ??= string.Empty;
            builder.Append(value.Length);
            builder.Append(':');
            builder.Append(value);
            builder.Append('|');
        }

        private static string CombineRelativePath(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        #endregion
    }
}
