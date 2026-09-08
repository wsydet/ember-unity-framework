// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ember.Basic;

namespace Ember.Core.Editor
{
    /// <summary>模板继承的纯目录与元数据引擎；不访问 AssetDatabase，不执行模板写入。</summary>
    internal static class EmberTemplateInheritanceEngine
    {
        #region 内部参数

        internal const int CurrentSchemaVersion = 2;

        /// <summary>
        /// Git URL 包安装可能在缺少仓库根 .gitattributes 的包缓存中把文本文件
        /// 从 LF 检出为 CRLF。模板 hash 需要跨这种传输保持稳定，但二进制文件仍按
        /// 原始字节参与校验。
        /// </summary>
        private static readonly HashSet<string> LineEndingNormalizedExtensions = new(
            new[]
            {
                ".anim", ".asmdef", ".asmref", ".asset", ".bak", ".cginc", ".compute",
                ".controller", ".cs", ".hlsl", ".inputactions", ".json", ".lighting",
                ".mat", ".md", ".meta", ".mixer", ".overrideController", ".playable",
                ".prefab", ".preset", ".renderTexture", ".scenetemplate", ".shader",
                ".shadergraph", ".shadersubgraph", ".signal", ".spriteatlas",
                ".spriteatlasv2", ".terrainlayer", ".tss", ".txt", ".unity", ".uss",
                ".uxml", ".yaml", ".yml"
            },
            StringComparer.OrdinalIgnoreCase);

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 计算 Assets 完整树 hash：包含 .meta，路径统一为 / 并按 ordinal 排序，
        /// 单文件复用 CryptographyUtils.GetMD5File，最终序列复用 CryptographyUtils.GetMD5。
        /// </summary>
        internal static string ComputeTemplateContentHash(string assetsPath)
        {
            return CaptureDirectory(assetsPath).ContentHash;
        }

        /// <summary>计算模板文件内容 hash；已知文本格式先把 CRLF 归一化为 LF。</summary>
        internal static string ComputeTemplateFileContentHash(string path)
        {
            return ComputeTemplateFileContentHash(path, File.ReadAllBytes(path));
        }

        /// <summary>计算内存中的模板文件内容 hash；用于只读预览和部署后比较。</summary>
        internal static string ComputeTemplateFileContentHash(string path, byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (!LineEndingNormalizedExtensions.Contains(Path.GetExtension(path)))
                return CryptographyUtils.GetMD5(bytes);

            int crlfCount = 0;
            for (int i = 0; i + 1 < bytes.Length; i++)
            {
                if (bytes[i] == '\r' && bytes[i + 1] == '\n')
                    crlfCount++;
            }
            if (crlfCount == 0)
                return CryptographyUtils.GetMD5(bytes);

            var normalized = new byte[bytes.Length - crlfCount];
            int write = 0;
            for (int read = 0; read < bytes.Length; read++)
            {
                if (bytes[read] == '\r'
                    && read + 1 < bytes.Length
                    && bytes[read + 1] == '\n')
                {
                    continue;
                }
                normalized[write++] = bytes[read];
            }
            return CryptographyUtils.GetMD5(normalized);
        }

        /// <summary>构建 schema v1 → v2 的只读迁移计划，不修改传入对象或磁盘。</summary>
        internal static TemplateMetadataMigrationPlan BuildMetadataMigrationPlan(
            TemplateInfo source,
            string assetsPath)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sourceCopy = Clone(source);
            var snapshot = CaptureDirectory(assetsPath);
            var contentHash = snapshot.ContentHash;

            if (source.schemaVersion > CurrentSchemaVersion)
            {
                return new TemplateMetadataMigrationPlan(
                    sourceCopy,
                    null,
                    contentHash,
                    false,
                    false,
                    $"模板 [{source.id}] 使用了更高版本的 metadata schema v{source.schemaVersion}，当前仅支持 v{CurrentSchemaVersion}。");
            }

            if (source.schemaVersion == CurrentSchemaVersion)
            {
                return new TemplateMetadataMigrationPlan(
                    sourceCopy,
                    Clone(source),
                    contentHash,
                    false,
                    false,
                    null);
            }

            if (!string.IsNullOrEmpty(source.parentId))
            {
                return new TemplateMetadataMigrationPlan(
                    sourceCopy,
                    null,
                    contentHash,
                    true,
                    false,
                    $"旧模板 [{source.id}] 已带 parentId，无法安全猜测父模板基线，请人工迁移。");
            }

            if (!TryValidateAssetMetadata(snapshot, out var validationError))
            {
                return new TemplateMetadataMigrationPlan(
                    sourceCopy,
                    null,
                    contentHash,
                    true,
                    false,
                    $"模板 [{source.id}] 资产 metadata 无效：{validationError}");
            }

            var migrated = Clone(source);
            migrated.schemaVersion = CurrentSchemaVersion;
            migrated.parentId = string.Empty;
            migrated.parentVersion = string.Empty;
            migrated.parentContentHash = string.Empty;
            migrated.contentHash = contentHash;
            migrated.versionedContentHash = contentHash;

            return new TemplateMetadataMigrationPlan(
                sourceCopy,
                migrated,
                contentHash,
                true,
                true,
                null);
        }

        /// <summary>普通保存后的 metadata：只更新描述与 contentHash，不隐式变更版本或框架声明。</summary>
        internal static TemplateInfo BuildSavedMetadata(
            TemplateInfo source,
            string displayName,
            string description,
            string contentHash)
        {
            EnsureSchemaV2(source);
            if (string.IsNullOrEmpty(contentHash))
                throw new ArgumentException("模板内容 hash 不能为空。", nameof(contentHash));

            var saved = Clone(source);
            saved.displayName = string.IsNullOrEmpty(displayName) ? source.id : displayName;
            saved.description = description ?? string.Empty;
            saved.contentHash = contentHash;
            return saved;
        }

        /// <summary>模板版本字段 +1，并把当前 contentHash 封存为 versionedContentHash。</summary>
        internal static TemplateInfo BuildBumpedMetadata(TemplateInfo source, int field)
        {
            EnsureSchemaV2(source);
            var version = ParseVersion(source.version);
            switch (field)
            {
                case 0:
                    version[0]++;
                    version[1] = 0;
                    version[2] = 0;
                    break;
                case 1:
                    version[1]++;
                    version[2] = 0;
                    break;
                case 2:
                    version[2]++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field));
            }

            return BuildVersionedMetadata(
                source,
                $"{version[0]}.{version[1]}.{version[2]}");
        }

        /// <summary>设置显式模板版本，并把当前 contentHash 封存为 versionedContentHash。</summary>
        internal static TemplateInfo BuildVersionedMetadata(TemplateInfo source, string version)
        {
            EnsureSchemaV2(source);
            if (string.IsNullOrEmpty(source.contentHash))
                throw new InvalidOperationException($"模板 [{source.id}] 缺少 contentHash，请先保存模板内容。");
            if (string.Equals(source.version, version, StringComparison.Ordinal)
                && !string.Equals(source.contentHash, source.versionedContentHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"模板 [{source.id}] 有未版本化内容，不能用相同版本号封存；请选择主/次/补丁 bump。");
            }

            var versioned = Clone(source);
            versioned.version = version;
            versioned.versionedContentHash = source.contentHash;
            return versioned;
        }

        /// <summary>根/独立模板显式声明框架版本；派生模板只能在父子同步成功后继承声明。</summary>
        internal static TemplateInfo BuildFrameworkDeclaration(TemplateInfo source, string frameworkVersion)
        {
            EnsureSchemaV2(source);
            if (!string.IsNullOrEmpty(source.parentId))
                throw new InvalidOperationException(
                    $"派生模板 [{source.id}] 不能单独声明框架版本；请先同步父模板。");
            if (!string.Equals(source.contentHash, source.versionedContentHash, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"模板 [{source.id}] 有未版本化内容，请先 bump 模板版本再声明框架兼容性。");

            var declared = Clone(source);
            declared.frameworkVersion = frameworkVersion;
            return declared;
        }

        /// <summary>校验全部模板的单继承谱系，报告父缺失、id 冲突和循环。</summary>
        internal static List<TemplateGraphIssue> ValidateTemplateGraph(IEnumerable<TemplateInfo> templates)
        {
            var issues = new List<TemplateGraphIssue>();
            var map = BuildTemplateMap(templates, issues);
            foreach (var pair in map)
            {
                if (string.IsNullOrEmpty(pair.Value.parentId)) continue;
                ValidateLineage(pair.Value, map, issues);
            }
            return issues;
        }

        /// <summary>构建完整父模板同步计划；只读目录与 metadata，不执行任何写入。</summary>
        internal static TemplateSyncPlan BuildParentSyncPlan(
            TemplateInfo child,
            IEnumerable<TemplateInfo> templates,
            string parentAssetsPath,
            string parentSnapshotAssetsPath,
            string childAssetsPath)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            var templateList = new List<TemplateInfo>();
            if (templates != null)
            {
                foreach (var template in templates)
                    templateList.Add(template);
            }

            if (string.IsNullOrEmpty(child.parentId))
            {
                if (child.schemaVersion != CurrentSchemaVersion
                    || string.IsNullOrEmpty(child.contentHash)
                    || string.IsNullOrEmpty(child.versionedContentHash))
                {
                    return CreatePlan(
                        child,
                        TemplateSyncStatus.MetadataNotInitialized,
                        $"模板 [{child.id}] 尚未完成 schema v{CurrentSchemaVersion} 元数据初始化。",
                        null,
                        null,
                        null,
                        null,
                        false);
                }

                TemplateDirectorySnapshot rootSnapshot;
                try
                {
                    rootSnapshot = CaptureDirectory(childAssetsPath);
                }
                catch (Exception ex)
                {
                    return CreatePlan(
                        child,
                        TemplateSyncStatus.TemplateContentDirty,
                        $"模板 [{child.id}] 内容不可用：{ex.Message}",
                        null,
                        null,
                        null,
                        null,
                        false);
                }

                if (!TryValidateAssetMetadata(rootSnapshot, out var rootError))
                {
                    return CreatePlan(
                        child,
                        TemplateSyncStatus.InvalidAssetMetadata,
                        $"模板 [{child.id}] 资产 metadata 无效：{rootError}",
                        null,
                        null,
                        rootSnapshot.ContentHash,
                        null,
                        false);
                }
                if (!string.Equals(rootSnapshot.ContentHash, child.contentHash, StringComparison.Ordinal))
                {
                    return CreatePlan(
                        child,
                        TemplateSyncStatus.TemplateContentDirty,
                        $"模板 [{child.id}] 的磁盘内容与 contentHash 不一致。",
                        null,
                        null,
                        rootSnapshot.ContentHash,
                        null,
                        false);
                }

                return CreatePlan(
                    child,
                    TemplateSyncStatus.Root,
                    null,
                    null,
                    null,
                    rootSnapshot.ContentHash,
                    null,
                    false);
            }

            var graphIssues = ValidateTemplateGraph(templateList);
            foreach (var issue in graphIssues)
            {
                if (issue.TemplateId != child.id) continue;
                return CreatePlan(
                    child,
                    issue.Status,
                    issue.Message,
                    null,
                    null,
                    null,
                    null,
                    false);
            }

            var parent = FindTemplate(templateList, child.parentId);
            if (parent == null)
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.ParentMissing,
                    $"父模板 [{child.parentId}] 不存在。",
                    null,
                    null,
                    null,
                    null,
                    false);
            }

            if (child.schemaVersion != CurrentSchemaVersion
                || parent.schemaVersion != CurrentSchemaVersion
                || string.IsNullOrEmpty(child.parentVersion)
                || string.IsNullOrEmpty(child.parentContentHash)
                || string.IsNullOrEmpty(child.contentHash)
                || string.IsNullOrEmpty(child.versionedContentHash)
                || string.IsNullOrEmpty(parent.contentHash)
                || string.IsNullOrEmpty(parent.versionedContentHash))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.MetadataNotInitialized,
                    $"模板 [{child.id}] 或父模板 [{parent.id}] 尚未完成 schema v{CurrentSchemaVersion} 元数据初始化。",
                    null,
                    null,
                    null,
                    null,
                    false);
            }

            TemplateDirectorySnapshot oldParent;
            try
            {
                oldParent = CaptureDirectory(parentSnapshotAssetsPath);
            }
            catch (Exception ex)
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.SnapshotMissingOrCorrupted,
                    $"父模板基线快照不可用：{ex.Message}",
                    null,
                    null,
                    null,
                    null,
                    false);
            }

            TemplateDirectorySnapshot currentParent;
            try
            {
                currentParent = CaptureDirectory(parentAssetsPath);
            }
            catch (Exception ex)
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.ParentMissing,
                    $"父模板内容不可用：{ex.Message}",
                    oldParent.ContentHash,
                    null,
                    null,
                    null,
                    false);
            }

            TemplateDirectorySnapshot currentChild;
            try
            {
                currentChild = CaptureDirectory(childAssetsPath);
            }
            catch (Exception ex)
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.TemplateContentDirty,
                    $"派生模板内容不可用：{ex.Message}",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    null,
                    null,
                    false);
            }

            if (!TryValidateAssetMetadata(oldParent, out var snapshotError))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.SnapshotMissingOrCorrupted,
                    snapshotError,
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!TryValidateAssetMetadata(currentParent, out var parentError))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.InvalidAssetMetadata,
                    $"父模板资产 metadata 无效：{parentError}",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!TryValidateAssetMetadata(currentChild, out var childError))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.InvalidAssetMetadata,
                    $"派生模板资产 metadata 无效：{childError}",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!string.Equals(currentParent.ContentHash, parent.contentHash, StringComparison.Ordinal))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.TemplateContentDirty,
                    $"父模板 [{parent.id}] 的磁盘内容与 contentHash 不一致，请先通过模板开发页保存。",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!string.Equals(parent.contentHash, parent.versionedContentHash, StringComparison.Ordinal))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.ParentUnversioned,
                    $"父模板 [{parent.id}] 有未版本化内容，请先 bump 父模板版本。",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!string.Equals(currentChild.ContentHash, child.contentHash, StringComparison.Ordinal))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.TemplateContentDirty,
                    $"派生模板 [{child.id}] 的磁盘内容与 contentHash 不一致，请先保存或恢复模板内容。",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            if (!string.Equals(oldParent.ContentHash, child.parentContentHash, StringComparison.Ordinal))
            {
                return CreatePlan(
                    child,
                    TemplateSyncStatus.SnapshotMissingOrCorrupted,
                    $"模板 [{child.id}] 的 ParentSnapshot~ 与 parentContentHash 不一致。",
                    oldParent.ContentHash,
                    currentParent.ContentHash,
                    currentChild.ContentHash,
                    null,
                    false);
            }

            var changes = BuildThreeWayChanges(oldParent, currentParent, currentChild);
            bool parentChanged = !string.Equals(child.parentVersion, parent.version, StringComparison.Ordinal)
                || !string.Equals(child.parentContentHash, parent.contentHash, StringComparison.Ordinal)
                || !string.Equals(child.frameworkVersion, parent.frameworkVersion, StringComparison.Ordinal);
            return CreatePlan(
                child,
                parentChanged ? TemplateSyncStatus.ParentChanged : TemplateSyncStatus.Synced,
                null,
                oldParent.ContentHash,
                currentParent.ContentHash,
                currentChild.ContentHash,
                changes,
                parentChanged);
        }

        /// <summary>直接构建 O/N/C 目录的文件级三方差异，供纯引擎测试与后续事务复用。</summary>
        internal static List<TemplateChange> BuildThreeWayChanges(
            string oldParentAssetsPath,
            string currentParentAssetsPath,
            string currentChildAssetsPath)
        {
            return BuildThreeWayChanges(
                CaptureDirectory(oldParentAssetsPath),
                CaptureDirectory(currentParentAssetsPath),
                CaptureDirectory(currentChildAssetsPath));
        }

        /// <summary>校验模板 Assets 的资源/.meta 配对与 GUID 唯一性，并返回实时树 hash。</summary>
        internal static bool TryValidateTemplateAssets(
            string assetsPath,
            out string contentHash,
            out string error)
        {
            try
            {
                var snapshot = CaptureDirectory(assetsPath);
                contentHash = snapshot.ContentHash;
                return TryValidateAssetMetadata(snapshot, out error);
            }
            catch (Exception ex)
            {
                contentHash = null;
                error = ex.Message;
                return false;
            }
        }

        /// <summary>判断项目编辑副本记录是否落后于模板存储。</summary>
        internal static bool IsEditingRecordStale(
            EditingTemplateRecord record,
            TemplateInfo template)
        {
            if (record == null || template == null) return true;
            if (!string.Equals(record.templateId, template.id, StringComparison.Ordinal)) return true;
            if (string.IsNullOrEmpty(record.templateVersion)
                || string.IsNullOrEmpty(record.contentHash))
                return true;
            return !string.Equals(record.templateVersion, template.version, StringComparison.Ordinal)
                || !string.Equals(record.contentHash, template.contentHash, StringComparison.Ordinal);
        }

        private static List<TemplateChange> BuildThreeWayChanges(
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            var units = BuildChangeUnits(oldParent, currentParent, currentChild);
            var changes = new List<TemplateChange>();
            foreach (var unit in units)
            {
                var kind = ClassifyChange(unit, oldParent, currentParent, currentChild);
                if (!kind.HasValue) continue;

                var fileChanges = new List<TemplateFileChange>();
                unit.Paths.Sort(StringComparer.Ordinal);
                foreach (var path in unit.Paths)
                {
                    oldParent.Files.TryGetValue(path, out var oldFile);
                    currentParent.Files.TryGetValue(path, out var parentFile);
                    currentChild.Files.TryGetValue(path, out var childFile);
                    fileChanges.Add(new TemplateFileChange(
                        path,
                        oldFile?.Hash,
                        parentFile?.Hash,
                        childFile?.Hash,
                        oldFile?.Guid,
                        parentFile?.Guid,
                        childFile?.Guid));
                }

                changes.Add(new TemplateChange(
                    unit.UnitPath,
                    unit.UnitKind,
                    kind.Value,
                    GetRecommendedChoice(kind.Value),
                    fileChanges));
            }

            changes.Sort((left, right) =>
            {
                int pathCompare = StringComparer.Ordinal.Compare(left.UnitPath, right.UnitPath);
                return pathCompare != 0 ? pathCompare : left.UnitKind.CompareTo(right.UnitKind);
            });
            return changes;
        }

        private static List<ChangeUnitBuilder> BuildChangeUnits(
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            var allPaths = new HashSet<string>(StringComparer.Ordinal);
            AddFilePaths(allPaths, oldParent);
            AddFilePaths(allPaths, currentParent);
            AddFilePaths(allPaths, currentChild);

            var units = new Dictionary<string, ChangeUnitBuilder>(StringComparer.Ordinal);
            foreach (var path in allPaths)
            {
                GetChangeUnitIdentity(
                    path,
                    oldParent,
                    currentParent,
                    currentChild,
                    out var key,
                    out var unitPath,
                    out var unitKind);

                if (!units.TryGetValue(key, out var unit))
                {
                    unit = new ChangeUnitBuilder(unitPath, unitKind);
                    units.Add(key, unit);
                }
                unit.Paths.Add(path);
            }

            return new List<ChangeUnitBuilder>(units.Values);
        }

        private static void AddFilePaths(
            HashSet<string> target,
            TemplateDirectorySnapshot snapshot)
        {
            foreach (var path in snapshot.Files.Keys)
                target.Add(path);
        }

        private static void GetChangeUnitIdentity(
            string relativePath,
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild,
            out string key,
            out string unitPath,
            out TemplateChangeUnitKind unitKind)
        {
            unitPath = relativePath.EndsWith(".meta", StringComparison.Ordinal)
                ? relativePath.Substring(0, relativePath.Length - ".meta".Length)
                : relativePath;

            var pathTypeConflictRoot = FindPathTypeConflictRoot(
                unitPath,
                oldParent,
                currentParent,
                currentChild);
            if (pathTypeConflictRoot != null)
            {
                unitPath = pathTypeConflictRoot;
                unitKind = TemplateChangeUnitKind.PathTypeConflict;
                key = "P:" + unitPath;
                return;
            }

            bool appearsAsFile = oldParent.Files.ContainsKey(unitPath)
                || currentParent.Files.ContainsKey(unitPath)
                || currentChild.Files.ContainsKey(unitPath);
            bool appearsAsDirectory = oldParent.Directories.Contains(unitPath)
                || currentParent.Directories.Contains(unitPath)
                || currentChild.Directories.Contains(unitPath);

            if (appearsAsFile && appearsAsDirectory)
            {
                unitKind = TemplateChangeUnitKind.PathTypeConflict;
                key = "P:" + unitPath;
            }
            else if (appearsAsDirectory)
            {
                unitKind = TemplateChangeUnitKind.DirectoryMetadata;
                key = "D:" + unitPath;
            }
            else
            {
                unitKind = TemplateChangeUnitKind.Asset;
                key = "A:" + unitPath;
            }
        }

        private static string FindPathTypeConflictRoot(
            string path,
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            int separatorIndex = -1;
            while (true)
            {
                separatorIndex = path.IndexOf('/', separatorIndex + 1);
                var candidate = separatorIndex < 0
                    ? path
                    : path.Substring(0, separatorIndex);
                bool appearsAsFile = oldParent.Files.ContainsKey(candidate)
                    || currentParent.Files.ContainsKey(candidate)
                    || currentChild.Files.ContainsKey(candidate);
                bool appearsAsDirectory = oldParent.Directories.Contains(candidate)
                    || currentParent.Directories.Contains(candidate)
                    || currentChild.Directories.Contains(candidate);
                if (appearsAsFile && appearsAsDirectory)
                    return candidate;
                if (separatorIndex < 0) return null;
            }
        }

        private static TemplateChangeKind? ClassifyChange(
            ChangeUnitBuilder unit,
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            bool oldExists = UnitExists(unit, oldParent);
            bool parentExists = UnitExists(unit, currentParent);
            bool childExists = UnitExists(unit, currentChild);
            bool parentMatchesOld = UnitsEqual(unit, currentParent, oldParent);
            bool childMatchesOld = UnitsEqual(unit, currentChild, oldParent);
            bool parentMatchesChild = UnitsEqual(unit, currentParent, currentChild);

            if (HasCurrentPathTypeConflict(unit, currentParent, currentChild))
                return TemplateChangeKind.ConflictPathTypeChanged;
            if (!parentMatchesChild && HasGuidMismatch(unit, currentParent, currentChild))
                return TemplateChangeKind.ConflictGuidMismatch;

            if (oldExists)
            {
                if (parentExists)
                {
                    if (childExists)
                    {
                        if (parentMatchesOld && childMatchesOld) return null;
                        if (!parentMatchesOld && childMatchesOld)
                            return TemplateChangeKind.AdoptParentModification;
                        if (parentMatchesOld && !childMatchesOld)
                            return TemplateChangeKind.KeepChildModification;
                        if (parentMatchesChild)
                            return TemplateChangeKind.AlreadyMatchesParent;
                        return TemplateChangeKind.ConflictConcurrentModification;
                    }

                    return parentMatchesOld
                        ? TemplateChangeKind.KeepChildDeletion
                        : TemplateChangeKind.ConflictParentModificationChildDeletion;
                }

                if (!childExists) return null;
                if (unit.UnitKind == TemplateChangeUnitKind.DirectoryMetadata
                    && childMatchesOld
                    && ChildHasModifiedDescendant(unit.UnitPath, oldParent, currentChild))
                {
                    return TemplateChangeKind.ConflictParentDeletionChildModification;
                }
                return childMatchesOld
                    ? TemplateChangeKind.AdoptParentDeletion
                    : TemplateChangeKind.ConflictParentDeletionChildModification;
            }

            if (parentExists)
            {
                if (!childExists) return TemplateChangeKind.AdoptParentAddition;
                return parentMatchesChild
                    ? TemplateChangeKind.AlreadyMatchesParent
                    : TemplateChangeKind.ConflictConcurrentAddition;
            }

            return childExists ? TemplateChangeKind.KeepChildAddition : null;
        }

        private static bool ChildHasModifiedDescendant(
            string directoryPath,
            TemplateDirectorySnapshot oldParent,
            TemplateDirectorySnapshot currentChild)
        {
            string prefix = directoryPath + "/";
            foreach (var pair in currentChild.Files)
            {
                if (!pair.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                oldParent.Files.TryGetValue(pair.Key, out var oldFile);
                if (!string.Equals(pair.Value.Hash, oldFile?.Hash, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static TemplateConflictChoice GetRecommendedChoice(TemplateChangeKind kind)
        {
            switch (kind)
            {
                case TemplateChangeKind.AdoptParentAddition:
                case TemplateChangeKind.AdoptParentModification:
                case TemplateChangeKind.AdoptParentDeletion:
                    return TemplateConflictChoice.AcceptParent;

                case TemplateChangeKind.KeepChildAddition:
                case TemplateChangeKind.KeepChildModification:
                case TemplateChangeKind.KeepChildDeletion:
                case TemplateChangeKind.AlreadyMatchesParent:
                    return TemplateConflictChoice.KeepChild;

                default:
                    return TemplateConflictChoice.Unresolved;
            }
        }

        private static bool UnitExists(
            ChangeUnitBuilder unit,
            TemplateDirectorySnapshot snapshot)
        {
            foreach (var path in unit.Paths)
            {
                if (snapshot.Files.ContainsKey(path)) return true;
            }
            return false;
        }

        private static bool UnitsEqual(
            ChangeUnitBuilder unit,
            TemplateDirectorySnapshot left,
            TemplateDirectorySnapshot right)
        {
            foreach (var path in unit.Paths)
            {
                left.Files.TryGetValue(path, out var leftFile);
                right.Files.TryGetValue(path, out var rightFile);
                if (!string.Equals(leftFile?.Hash, rightFile?.Hash, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool HasGuidMismatch(
            ChangeUnitBuilder unit,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            foreach (var path in unit.Paths)
            {
                if (!path.EndsWith(".meta", StringComparison.Ordinal)) continue;
                currentParent.Files.TryGetValue(path, out var parentMeta);
                currentChild.Files.TryGetValue(path, out var childMeta);
                if (parentMeta == null || childMeta == null) continue;
                if (string.IsNullOrEmpty(parentMeta.Guid) || string.IsNullOrEmpty(childMeta.Guid))
                    continue;
                if (!string.Equals(parentMeta.Guid, childMeta.Guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasCurrentPathTypeConflict(
            ChangeUnitBuilder unit,
            TemplateDirectorySnapshot currentParent,
            TemplateDirectorySnapshot currentChild)
        {
            if (unit.UnitKind != TemplateChangeUnitKind.PathTypeConflict) return false;
            var parentType = GetPathType(unit.UnitPath, currentParent);
            var childType = GetPathType(unit.UnitPath, currentChild);
            return parentType != TemplatePathType.Missing
                && childType != TemplatePathType.Missing
                && parentType != childType;
        }

        private static TemplatePathType GetPathType(
            string path,
            TemplateDirectorySnapshot snapshot)
        {
            if (snapshot.Files.ContainsKey(path)) return TemplatePathType.File;
            if (snapshot.Directories.Contains(path)) return TemplatePathType.Directory;
            return TemplatePathType.Missing;
        }

        private static TemplateDirectorySnapshot CaptureDirectory(string assetsPath)
        {
            if (string.IsNullOrWhiteSpace(assetsPath))
                throw new ArgumentException("模板 Assets 路径不能为空。", nameof(assetsPath));

            var root = Path.GetFullPath(assetsPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"模板 Assets 目录不存在：{root}");

            var files = new Dictionary<string, TemplateFileFingerprint>(StringComparer.Ordinal);
            var directories = new HashSet<string>(StringComparer.Ordinal);
            var allPaths = new List<string>();

            foreach (var directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
            {
                var relativePath = ToRelativePath(root, directory);
                directories.Add(relativePath);
                allPaths.Add(relativePath);
            }

            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                var relativePath = ToRelativePath(root, file);
                string guid = null;
                bool isFolderMetadata = false;
                if (relativePath.EndsWith(".meta", StringComparison.Ordinal))
                    ReadMetaProperties(file, out guid, out isFolderMetadata);
                var fingerprint = new TemplateFileFingerprint(
                    relativePath,
                    ComputeTemplateFileContentHash(relativePath, File.ReadAllBytes(file)),
                    guid,
                    isFolderMetadata);
                files.Add(relativePath, fingerprint);
                allPaths.Add(relativePath);
            }

            // Git 不保存空目录。folderAsset .meta 因而可能是空目录在模板仓库中的
            // 唯一物化表示；把它恢复为目录路径，三方计划仍可按目录单元处理。
            foreach (var pair in files)
            {
                if (!pair.Value.IsFolderMetadata) continue;
                var targetPath = pair.Key.Substring(0, pair.Key.Length - ".meta".Length);
                if (files.ContainsKey(targetPath) || directories.Contains(targetPath)) continue;
                directories.Add(targetPath);
                allPaths.Add(targetPath);
            }

            ValidateCaseInsensitivePathUniqueness(allPaths);
            return new TemplateDirectorySnapshot(
                root,
                files,
                directories,
                ComputeTreeHash(files.Values));
        }

        private static string ComputeTreeHash(IEnumerable<TemplateFileFingerprint> fingerprints)
        {
            var files = new List<TemplateFileFingerprint>(fingerprints);
            files.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.RelativePath, right.RelativePath));

            var sequence = new StringBuilder();
            foreach (var file in files)
            {
                // 长度前缀避免路径与 hash 的简单拼接产生边界歧义。
                sequence.Append(file.RelativePath.Length.ToString(CultureInfo.InvariantCulture));
                sequence.Append(':');
                sequence.Append(file.RelativePath);
                sequence.Append(':');
                sequence.Append(file.Hash);
                sequence.Append('\n');
            }
            return CryptographyUtils.GetMD5(sequence.ToString());
        }

        private static string ToRelativePath(string root, string path)
        {
            return path.Substring(root.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');
        }

        private static void ReadMetaProperties(
            string metaPath,
            out string guid,
            out bool isFolderMetadata)
        {
            guid = null;
            isFolderMetadata = false;
            foreach (var line in File.ReadLines(metaPath))
            {
                var match = Regex.Match(line, @"^guid:\s*(\S+)\s*$");
                if (match.Success)
                    guid = match.Groups[1].Value;
                if (Regex.IsMatch(line, @"^folderAsset:\s*yes\s*$"))
                    isFolderMetadata = true;
            }
        }

        private static bool TryValidateAssetMetadata(
            TemplateDirectorySnapshot snapshot,
            out string error)
        {
            foreach (var file in snapshot.Files.Keys)
            {
                if (file.EndsWith(".meta", StringComparison.Ordinal)) continue;
                if (snapshot.Files.ContainsKey(file + ".meta")) continue;
                error = $"资源缺少配套 .meta：{file}";
                return false;
            }

            var guidPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in snapshot.Files)
            {
                if (!pair.Key.EndsWith(".meta", StringComparison.Ordinal)) continue;
                var targetPath = pair.Key.Substring(0, pair.Key.Length - ".meta".Length);
                bool targetIsFile = snapshot.Files.ContainsKey(targetPath);
                bool targetIsDirectory = snapshot.Directories.Contains(targetPath);
                if (pair.Value.IsFolderMetadata && targetIsFile)
                {
                    error = $"目录 .meta 对应到资源文件：{pair.Key}";
                    return false;
                }
                if (!pair.Value.IsFolderMetadata && targetIsDirectory)
                {
                    error = $"资源 .meta 对应到目录：{pair.Key}";
                    return false;
                }
                if (!targetIsFile && !targetIsDirectory)
                {
                    error = $"存在孤立 .meta：{pair.Key}";
                    return false;
                }

                if (string.IsNullOrEmpty(pair.Value.Guid))
                {
                    error = $".meta 缺少 GUID：{pair.Key}";
                    return false;
                }

                if (guidPaths.TryGetValue(pair.Value.Guid, out var existing))
                {
                    error = $"模板内 GUID 重复：[{existing}] 与 [{pair.Key}] 共用 {pair.Value.Guid}";
                    return false;
                }
                guidPaths.Add(pair.Value.Guid, pair.Key);
            }

            error = null;
            return true;
        }

        private static Dictionary<string, TemplateInfo> BuildTemplateMap(
            IEnumerable<TemplateInfo> templates,
            List<TemplateGraphIssue> issues)
        {
            var map = new Dictionary<string, TemplateInfo>(StringComparer.Ordinal);
            var caseInsensitiveIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (templates == null) return map;

            foreach (var template in templates)
            {
                if (template == null || string.IsNullOrEmpty(template.id)) continue;
                if (caseInsensitiveIds.TryGetValue(template.id, out var caseCollision)
                    && !string.Equals(caseCollision, template.id, StringComparison.Ordinal))
                {
                    issues.Add(new TemplateGraphIssue(
                        template.id,
                        TemplateSyncStatus.InvalidAssetMetadata,
                        $"模板 id 存在 Windows 大小写碰撞：[{caseCollision}] 与 [{template.id}]。"));
                    continue;
                }
                caseInsensitiveIds[template.id] = template.id;

                if (map.ContainsKey(template.id))
                {
                    issues.Add(new TemplateGraphIssue(
                        template.id,
                        TemplateSyncStatus.InvalidAssetMetadata,
                        $"模板 id 重复：{template.id}"));
                    continue;
                }
                map.Add(template.id, template);
            }
            return map;
        }

        private static void ValidateLineage(
            TemplateInfo source,
            Dictionary<string, TemplateInfo> templates,
            List<TemplateGraphIssue> issues)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = source;
            while (current != null && !string.IsNullOrEmpty(current.parentId))
            {
                if (!visited.Add(current.id))
                {
                    issues.Add(new TemplateGraphIssue(
                        source.id,
                        TemplateSyncStatus.CycleDetected,
                        $"模板 [{source.id}] 的父模板谱系存在循环。"));
                    return;
                }

                var parentId = current.parentId;
                if (!templates.TryGetValue(parentId, out current))
                {
                    issues.Add(new TemplateGraphIssue(
                        source.id,
                        TemplateSyncStatus.ParentMissing,
                        $"模板 [{source.id}] 的父模板 [{parentId}] 不存在。"));
                    return;
                }
            }
        }

        private static TemplateInfo FindTemplate(
            IEnumerable<TemplateInfo> templates,
            string templateId)
        {
            if (templates == null) return null;
            foreach (var template in templates)
            {
                if (template != null
                    && string.Equals(template.id, templateId, StringComparison.Ordinal))
                    return template;
            }
            return null;
        }

        private static TemplateSyncPlan CreatePlan(
            TemplateInfo child,
            TemplateSyncStatus status,
            string error,
            string snapshotHash,
            string parentHash,
            string childHash,
            List<TemplateChange> changes,
            bool canApply)
        {
            return new TemplateSyncPlan(
                child.id,
                child.parentId,
                status,
                error,
                snapshotHash,
                parentHash,
                childHash,
                changes,
                canApply);
        }

        /// <summary>检测 Windows 不可表示的大小写路径碰撞，包含目录层级碰撞。</summary>
        internal static void ValidateCaseInsensitivePathUniqueness(IEnumerable<string> relativePaths)
        {
            if (relativePaths == null)
                throw new ArgumentNullException(nameof(relativePaths));

            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in relativePaths)
            {
                if (string.IsNullOrEmpty(path))
                    throw new InvalidDataException("模板包含空相对路径。");

                var normalized = path.Replace('\\', '/');
                var segments = normalized.Split('/');
                var current = new StringBuilder();
                foreach (var segment in segments)
                {
                    if (string.IsNullOrEmpty(segment) || segment == "." || segment == "..")
                        throw new InvalidDataException($"模板包含非法相对路径：{path}");

                    if (current.Length > 0) current.Append('/');
                    current.Append(segment);
                    var candidate = current.ToString();

                    if (seen.TryGetValue(candidate, out var existing)
                        && !string.Equals(existing, candidate, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            $"模板包含 Windows 大小写路径碰撞：[{existing}] 与 [{candidate}]。");
                    }

                    if (!seen.ContainsKey(candidate))
                        seen.Add(candidate, candidate);
                }
            }
        }

        internal static TemplateInfo Clone(TemplateInfo source)
        {
            if (source == null) return null;
            return new TemplateInfo
            {
                schemaVersion = source.schemaVersion,
                id = source.id,
                displayName = source.displayName,
                description = source.description,
                version = source.version,
                frameworkVersion = source.frameworkVersion,
                channel = source.channel,
                order = source.order,
                parentId = source.parentId,
                parentVersion = source.parentVersion,
                parentContentHash = source.parentContentHash,
                contentHash = source.contentHash,
                versionedContentHash = source.versionedContentHash
            };
        }

        private static void EnsureSchemaV2(TemplateInfo source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.schemaVersion != CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"模板 [{source.id}] 尚未初始化 schema v{CurrentSchemaVersion} 继承元数据。");
            }
        }

        private static int[] ParseVersion(string version)
        {
            var parts = (version ?? string.Empty).Split('.');
            var result = new[] { 0, 0, 0 };
            for (int i = 0; i < result.Length && i < parts.Length; i++)
                int.TryParse(parts[i].Trim(), out result[i]);
            return result;
        }

        private sealed class TemplateFileFingerprint
        {
            #region 内部参数

            internal string RelativePath { get; }
            internal string Hash { get; }
            internal string Guid { get; }
            internal bool IsFolderMetadata { get; }

            #endregion

            // --------------------------------------------------------

            #region 外部方法

            internal TemplateFileFingerprint(
                string relativePath,
                string hash,
                string guid,
                bool isFolderMetadata)
            {
                RelativePath = relativePath;
                Hash = hash;
                Guid = guid;
                IsFolderMetadata = isFolderMetadata;
            }

            #endregion
        }

        private sealed class TemplateDirectorySnapshot
        {
            #region 内部参数

            internal string RootPath { get; }
            internal Dictionary<string, TemplateFileFingerprint> Files { get; }
            internal HashSet<string> Directories { get; }
            internal string ContentHash { get; }

            #endregion

            // --------------------------------------------------------

            #region 外部方法

            internal TemplateDirectorySnapshot(
                string rootPath,
                Dictionary<string, TemplateFileFingerprint> files,
                HashSet<string> directories,
                string contentHash)
            {
                RootPath = rootPath;
                Files = files;
                Directories = directories;
                ContentHash = contentHash;
            }

            #endregion
        }

        private sealed class ChangeUnitBuilder
        {
            #region 内部参数

            internal string UnitPath { get; }
            internal TemplateChangeUnitKind UnitKind { get; }
            internal List<string> Paths { get; } = new();

            #endregion

            // --------------------------------------------------------

            #region 外部方法

            internal ChangeUnitBuilder(string unitPath, TemplateChangeUnitKind unitKind)
            {
                UnitPath = unitPath;
                UnitKind = unitKind;
            }

            #endregion
        }

        private enum TemplatePathType
        {
            Missing,
            File,
            Directory
        }

        #endregion
    }
}
