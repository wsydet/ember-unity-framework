// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;

namespace Ember.Core.Editor
{
    /// <summary>模板元数据（Templates~/《模板名》/template.json）。</summary>
    [Serializable]
    public class TemplateInfo
    {
        #region 编辑器面板参数

        public int schemaVersion;
        public string id;
        public string displayName;
        public string description;
        public string version;
        public string frameworkVersion;
        public string channel;
        public int order;
        public string parentId;
        public string parentVersion;
        public string parentContentHash;
        public string contentHash;
        public string versionedContentHash;

        #endregion
    }

    /// <summary>schema v1 → v2 的只读迁移计划；构建计划不会写入模板或元数据。</summary>
    public sealed class TemplateMetadataMigrationPlan
    {
        #region 内部参数

        public TemplateInfo Source { get; }
        public TemplateInfo Migrated { get; }
        public string ComputedContentHash { get; }
        public bool IsRequired { get; }
        public bool CanApply { get; }
        public string Error { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateMetadataMigrationPlan(
            TemplateInfo source,
            TemplateInfo migrated,
            string computedContentHash,
            bool isRequired,
            bool canApply,
            string error)
        {
            Source = source;
            Migrated = migrated;
            ComputedContentHash = computedContentHash;
            IsRequired = isRequired;
            CanApply = canApply;
            Error = error;
        }

        #endregion
    }

    /// <summary>消费端已部署模板记录（Assets/Editor/EmberDeployedTemplates.json）。</summary>
    [Serializable]
    public class DeployedTemplateRecord
    {
        #region 编辑器面板参数

        public string templateId;
        public string version;
        public string frameworkVersion;
        public string deployedAt;

        #endregion
    }

    /// <summary>部署记录文件反序列化结构。</summary>
    [Serializable]
    internal class DeployedTemplatesData
    {
        #region 编辑器面板参数

        public string activeTemplateId;
        public List<DeployedTemplateRecord> records = new();

        #endregion
    }

    /// <summary>dev 仓库「当前正在编辑的模板」记录（Assets/Editor/EmberEditingTemplate.json）。</summary>
    [Serializable]
    public class EditingTemplateRecord
    {
        #region 编辑器面板参数

        public string templateId;
        public string templateVersion;
        public string contentHash;
        public string loadedAt;

        #endregion
    }

    /// <summary>模板升级等级（已部署版本 → 包内版本）：major=弃用重写 / minor=结构变化 / patch=修复。</summary>
    public enum TemplateUpgradeLevel
    {
        None,
        Patch,
        Minor,
        Major
    }

    /// <summary>模板继承与编辑副本的同步状态。</summary>
    public enum TemplateSyncStatus
    {
        Root,
        Synced,
        ParentChanged,
        ParentUnversioned,
        TemplateContentDirty,
        EditingCopyStale,
        ParentMissing,
        CycleDetected,
        SnapshotMissingOrCorrupted,
        MetadataNotInitialized,
        InvalidAssetMetadata
    }

    /// <summary>父模板、旧基线与派生模板之间的文件单元分类。</summary>
    public enum TemplateChangeKind
    {
        AdoptParentAddition,
        AdoptParentModification,
        AdoptParentDeletion,
        KeepChildAddition,
        KeepChildModification,
        KeepChildDeletion,
        AlreadyMatchesParent,
        ConflictConcurrentAddition,
        ConflictConcurrentModification,
        ConflictParentDeletionChildModification,
        ConflictParentModificationChildDeletion,
        ConflictGuidMismatch,
        ConflictPathTypeChanged,
        AutoMergeScene
    }

    /// <summary>冲突文件单元的人工选择。</summary>
    public enum TemplateConflictChoice
    {
        Unresolved,
        KeepChild,
        AcceptParent
    }

    /// <summary>三方变更单元类型；资源文件与同名 .meta 始终属于同一 Asset 单元。</summary>
    public enum TemplateChangeUnitKind
    {
        Asset,
        DirectoryMetadata,
        PathTypeConflict,
        SceneSemantic
    }

    /// <summary>同步计划在事务 stage 中应采用的内容来源；人工冲突未决时为空。</summary>
    public enum TemplateChangeApplyMode
    {
        KeepChild,
        AcceptParent,
        SemanticMerge
    }

    /// <summary>场景语义 dry-run 的结果状态。</summary>
    public enum SceneSemanticMergeStatus
    {
        Succeeded,
        Fallback
    }

    /// <summary>一次显式场景语义 dry-run 的可复核指纹与安全回退原因。</summary>
    public sealed class SceneSemanticMergePreview
    {
        #region 内部参数

        public string ScenePath { get; }
        public string OldHash { get; }
        public string ParentHash { get; }
        public string ChildHash { get; }
        public string MergedHash { get; }
        public string ToolVersion { get; }
        public string RulesHash { get; }
        public string ReportDigest { get; }
        public string ReportSummary { get; }
        public SceneSemanticMergeStatus Status { get; }
        public string FailureReason { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal SceneSemanticMergePreview(
            string scenePath,
            string oldHash,
            string parentHash,
            string childHash,
            string mergedHash,
            string toolVersion,
            string rulesHash,
            string reportDigest,
            string reportSummary,
            SceneSemanticMergeStatus status,
            string failureReason)
        {
            ScenePath = scenePath;
            OldHash = oldHash;
            ParentHash = parentHash;
            ChildHash = childHash;
            MergedHash = mergedHash;
            ToolVersion = toolVersion;
            RulesHash = rulesHash;
            ReportDigest = reportDigest;
            ReportSummary = reportSummary;
            Status = status;
            FailureReason = failureReason;
        }

        #endregion
    }

    /// <summary>单个物理文件在 O/N/C 三方中的 hash 与 GUID 状态。</summary>
    public sealed class TemplateFileChange
    {
        #region 内部参数

        public string RelativePath { get; }
        public string OldHash { get; }
        public string ParentHash { get; }
        public string ChildHash { get; }
        public string OldGuid { get; }
        public string ParentGuid { get; }
        public string ChildGuid { get; }
        public bool OldExists => OldHash != null;
        public bool ParentExists => ParentHash != null;
        public bool ChildExists => ChildHash != null;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateFileChange(
            string relativePath,
            string oldHash,
            string parentHash,
            string childHash,
            string oldGuid,
            string parentGuid,
            string childGuid)
        {
            RelativePath = relativePath;
            OldHash = oldHash;
            ParentHash = parentHash;
            ChildHash = childHash;
            OldGuid = oldGuid;
            ParentGuid = parentGuid;
            ChildGuid = childGuid;
        }

        #endregion
    }

    /// <summary>一个原子同步单元的三方分类结果。</summary>
    public sealed class TemplateChange
    {
        #region 内部参数

        public string UnitPath { get; }
        public TemplateChangeUnitKind UnitKind { get; }
        public TemplateChangeKind Kind { get; }
        public TemplateConflictChoice RecommendedChoice { get; }
        public IReadOnlyList<TemplateFileChange> Files { get; }
        public TemplateChangeApplyMode? ApplyMode { get; }
        public SceneSemanticMergePreview SceneMergePreview { get; }
        public bool WillChangeChildContent { get; }
        public bool IsConflict => RecommendedChoice == TemplateConflictChoice.Unresolved;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateChange(
            string unitPath,
            TemplateChangeUnitKind unitKind,
            TemplateChangeKind kind,
            TemplateConflictChoice recommendedChoice,
            List<TemplateFileChange> files)
            : this(
                unitPath,
                unitKind,
                kind,
                recommendedChoice,
                files,
                GetDefaultApplyMode(recommendedChoice),
                null,
                recommendedChoice == TemplateConflictChoice.AcceptParent)
        {
        }

        internal TemplateChange(
            string unitPath,
            TemplateChangeUnitKind unitKind,
            TemplateChangeKind kind,
            TemplateConflictChoice recommendedChoice,
            List<TemplateFileChange> files,
            TemplateChangeApplyMode? applyMode,
            SceneSemanticMergePreview sceneMergePreview,
            bool willChangeChildContent)
        {
            UnitPath = unitPath;
            UnitKind = unitKind;
            Kind = kind;
            RecommendedChoice = recommendedChoice;
            Files = files.AsReadOnly();
            ApplyMode = applyMode;
            SceneMergePreview = sceneMergePreview;
            WillChangeChildContent = willChangeChildContent;
        }

        private static TemplateChangeApplyMode? GetDefaultApplyMode(
            TemplateConflictChoice recommendedChoice)
        {
            switch (recommendedChoice)
            {
                case TemplateConflictChoice.KeepChild:
                    return TemplateChangeApplyMode.KeepChild;
                case TemplateConflictChoice.AcceptParent:
                    return TemplateChangeApplyMode.AcceptParent;
                default:
                    return null;
            }
        }

        #endregion
    }

    /// <summary>父模板 → 派生模板的只读三方同步计划。</summary>
    public sealed class TemplateSyncPlan
    {
        #region 内部参数

        public string TemplateId { get; }
        public string ParentId { get; }
        public TemplateSyncStatus Status { get; }
        public string Error { get; }
        public string SnapshotContentHash { get; }
        public string ParentContentHash { get; }
        public string ChildContentHash { get; }
        public IReadOnlyList<TemplateChange> Changes { get; }
        public bool IsEligible { get; }
        public bool HasConflicts { get; }
        public bool HasUnresolvedConflicts => HasConflicts;
        public bool CanApply => IsEligible && !HasUnresolvedConflicts;
        public bool HasSemanticMerges { get; }
        public bool WillChangeChildContent { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateSyncPlan(
            string templateId,
            string parentId,
            TemplateSyncStatus status,
            string error,
            string snapshotContentHash,
            string parentContentHash,
            string childContentHash,
            List<TemplateChange> changes,
            bool canApply)
        {
            TemplateId = templateId;
            ParentId = parentId;
            Status = status;
            Error = error;
            SnapshotContentHash = snapshotContentHash;
            ParentContentHash = parentContentHash;
            ChildContentHash = childContentHash;
            Changes = (changes ?? new List<TemplateChange>()).AsReadOnly();
            IsEligible = canApply;

            foreach (var change in Changes)
            {
                if (change.IsConflict)
                    HasConflicts = true;
                if (change.ApplyMode == TemplateChangeApplyMode.SemanticMerge)
                    HasSemanticMerges = true;
                if (change.WillChangeChildContent)
                    WillChangeChildContent = true;
            }
        }

        #endregion
    }

    /// <summary>模板谱系校验问题。</summary>
    public sealed class TemplateGraphIssue
    {
        #region 内部参数

        public string TemplateId { get; }
        public TemplateSyncStatus Status { get; }
        public string Message { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal TemplateGraphIssue(string templateId, TemplateSyncStatus status, string message)
        {
            TemplateId = templateId;
            Status = status;
            Message = message;
        }

        #endregion
    }
}
