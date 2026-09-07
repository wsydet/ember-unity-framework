// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;

namespace Ember.UIExtension.Editor
{
    /// <summary>UI 模块目录模板及现有模块的一次只读快照。</summary>
    public sealed class EUIModuleTemplateSnapshot
    {
        public string UIResourceRoot;
        public string ModulesRoot;
        public string TemplateRoot;
        public readonly List<string> TemplateRelativeDirectories = new List<string>();
        public readonly List<string> ModuleDirectories = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public string Error;

        public bool IsValid => string.IsNullOrEmpty(Error);
    }

    /// <summary>创建新模块时需要创建的目录。</summary>
    public sealed class EUIModuleInitializationPlan
    {
        public EUIModuleTemplateSnapshot Snapshot;
        public string ModuleDirectory;
        public readonly List<string> DirectoriesToCreate = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool CanExecute => Snapshot?.IsValid == true && Errors.Count == 0;
    }

    /// <summary>把模板目录增量补齐到既有模块的计划。</summary>
    public sealed class EUIModuleTemplateSyncPlan
    {
        public EUIModuleTemplateSnapshot Snapshot;
        public readonly List<string> DirectoriesToCreate = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool CanExecute => Snapshot?.IsValid == true && Errors.Count == 0;

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            foreach (var path in DirectoriesToCreate) builder.AppendLine($"＋ {path}");
            foreach (var error in Errors) builder.AppendLine($"✖ {error}");
            return builder.Length == 0 ? "所有模块目录均已与模板对齐。" : builder.ToString().TrimEnd();
        }
    }

    /// <summary>单个模板目录重命名并同步现有模块的计划。</summary>
    public sealed class EUIModuleTemplateRenamePlan
    {
        public EUIModuleTemplateSnapshot Snapshot;
        public string TemplateSourcePath;
        public string TemplateTargetPath;
        public string NewName;
        public readonly List<KeyValuePair<string, string>> ModuleMoves =
            new List<KeyValuePair<string, string>>();
        public readonly List<string> ModuleDirectoriesToCreate = new List<string>();
        public readonly List<string> AlreadyAlignedModules = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool CanExecute => Snapshot?.IsValid == true && Errors.Count == 0;

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"模板：{TemplateSourcePath} → {TemplateTargetPath}");
            foreach (var move in ModuleMoves)
                builder.AppendLine($"移动：{move.Key} → {move.Value}");
            foreach (var path in ModuleDirectoriesToCreate)
                builder.AppendLine($"补齐：{path}");
            foreach (var module in AlreadyAlignedModules)
                builder.AppendLine($"已对齐：{module}");
            foreach (var error in Errors) builder.AppendLine($"✖ {error}");
            return builder.ToString().TrimEnd();
        }
    }

    /// <summary>模板目录安全删除计划；已有模块目录只报告、不删除。</summary>
    public sealed class EUIModuleTemplateDeletePlan
    {
        public EUIModuleTemplateSnapshot Snapshot;
        public string TemplateFolderPath;
        public string ConfirmationName;
        public readonly List<string> TemplateDirectories = new List<string>();
        public readonly List<string> AssetFiles = new List<string>();
        public readonly List<string> PreservedModulePaths = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool CanExecute => Snapshot?.IsValid == true
                                  && Errors.Count == 0
                                  && AssetFiles.Count == 0;

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"移入系统回收站：{TemplateFolderPath}");
            foreach (var directory in TemplateDirectories.Skip(1))
                builder.AppendLine($"  └ {directory}");
            if (PreservedModulePaths.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("以下既有模块目录会保留：");
                foreach (var path in PreservedModulePaths) builder.AppendLine($"＝ {path}");
            }
            foreach (var asset in AssetFiles) builder.AppendLine($"✖ 包含资源：{asset}");
            foreach (var error in Errors) builder.AppendLine($"✖ {error}");
            return builder.ToString().TrimEnd();
        }
    }

    /// <summary>模块目录模板操作结果。</summary>
    public sealed class EUIModuleTemplateResult
    {
        public bool Success = true;
        public readonly List<string> CreatedPaths = new List<string>();
        public readonly List<string> Messages = new List<string>();

        public string Message => string.Join("\n", Messages);

        public void Fail(string message)
        {
            Success = false;
            Messages.Add(message);
        }
    }

    /// <summary>
    /// 管理 UI 模块的空目录模板。只复制目录名称，不复制模板 .meta，确保各模块目录 GUID 独立。
    /// </summary>
    public static class EUIModuleTemplateService
    {
        public const string TemplateDirectoryName = "模板";
        public const string RequiredPrefabDirectoryName = "Prefabs";

        private static readonly HashSet<string> ReservedWindowsNames = new HashSet<string>(
            new[]
            {
                "CON", "PRN", "AUX", "NUL",
                "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
            }, StringComparer.OrdinalIgnoreCase);

        #region 快照与计划

        public static EUIModuleTemplateSnapshot Scan(string uiResourceRoot = null)
        {
            var snapshot = new EUIModuleTemplateSnapshot();
            snapshot.UIResourceRoot = string.IsNullOrWhiteSpace(uiResourceRoot)
                ? ResolveConfiguredUIResourceRoot()
                : EUIPrefabCatalogService.NormalizeAssetPath(uiResourceRoot);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(snapshot.UIResourceRoot)
                || !AssetDatabase.IsValidFolder(snapshot.UIResourceRoot))
            {
                snapshot.Error = $"UI 资源根目录无效：{snapshot.UIResourceRoot ?? "—"}";
                return snapshot;
            }

            snapshot.ModulesRoot = $"{snapshot.UIResourceRoot}/Module";
            snapshot.TemplateRoot = $"{snapshot.ModulesRoot}/{TemplateDirectoryName}";
            if (!AssetDatabase.IsValidFolder(snapshot.ModulesRoot))
            {
                snapshot.Error = $"模块资源根目录不存在：{snapshot.ModulesRoot}";
                return snapshot;
            }
            if (!AssetDatabase.IsValidFolder(snapshot.TemplateRoot))
            {
                snapshot.Error = $"模块目录模板不存在：{snapshot.TemplateRoot}";
                return snapshot;
            }

            try
            {
                CollectTemplateDirectories(snapshot.TemplateRoot, snapshot.TemplateRoot,
                    snapshot.TemplateRelativeDirectories);
                snapshot.TemplateRelativeDirectories.Sort(ComparePathByDepthThenOrdinal);

                foreach (var modulePath in AssetDatabase.GetSubFolders(snapshot.ModulesRoot)
                             .Select(EUIPrefabCatalogService.NormalizeAssetPath)
                             .Where(path => !string.Equals(path, snapshot.TemplateRoot,
                                 StringComparison.OrdinalIgnoreCase))
                             .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    snapshot.ModuleDirectories.Add(modulePath);
                }

                var templateFullPath = EUIPrefabCatalogService.ToFullPath(snapshot.TemplateRoot);
                if (!string.IsNullOrEmpty(templateFullPath))
                {
                    foreach (var file in Directory.GetFiles(templateFullPath, "*",
                                 SearchOption.AllDirectories)
                                 .Where(path => !path.EndsWith(".meta",
                                     StringComparison.OrdinalIgnoreCase)))
                    {
                        snapshot.Warnings.Add("模板目录包含非目录资源，将不会同步："
                                              + ToAssetPath(snapshot.TemplateRoot,
                                                  templateFullPath, file));
                    }
                }
            }
            catch (Exception exception)
            {
                snapshot.Error = $"扫描模块目录模板失败：{exception.Message}";
            }

            return snapshot;
        }

        public static EUIModuleInitializationPlan BuildInitializationPlan(
            EUIModuleTemplateSnapshot snapshot, string moduleDirectory)
        {
            var plan = new EUIModuleInitializationPlan
            {
                Snapshot = snapshot,
                ModuleDirectory = EUIPrefabCatalogService.NormalizeAssetPath(moduleDirectory),
            };
            if (!ValidateSnapshot(snapshot, plan.Errors)) return plan;
            if (!IsDirectModuleDirectory(snapshot, plan.ModuleDirectory))
            {
                plan.Errors.Add($"新模块目录必须是 Module 的直接子目录：{plan.ModuleDirectory}");
                return plan;
            }
            if (string.Equals(plan.ModuleDirectory, snapshot.TemplateRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                plan.Errors.Add("模板目录不能作为业务模块初始化。");
                return plan;
            }

            if (AssetDatabase.IsValidFolder(plan.ModuleDirectory)) return plan;
            if (PathExistsButIsNotFolder(plan.ModuleDirectory))
            {
                plan.Errors.Add($"模块目录目标已被文件占用：{plan.ModuleDirectory}");
                return plan;
            }

            plan.DirectoriesToCreate.Add(plan.ModuleDirectory);
            foreach (var relative in snapshot.TemplateRelativeDirectories)
                plan.DirectoriesToCreate.Add($"{plan.ModuleDirectory}/{relative}");
            return plan;
        }

        public static EUIModuleTemplateSyncPlan BuildSyncPlan(EUIModuleTemplateSnapshot snapshot)
        {
            var plan = new EUIModuleTemplateSyncPlan { Snapshot = snapshot };
            if (!ValidateSnapshot(snapshot, plan.Errors)) return plan;

            foreach (var moduleDirectory in snapshot.ModuleDirectories)
            foreach (var relative in snapshot.TemplateRelativeDirectories)
            {
                var target = $"{moduleDirectory}/{relative}";
                if (AssetDatabase.IsValidFolder(target)) continue;
                if (PathExistsButIsNotFolder(target))
                {
                    plan.Errors.Add($"目录目标已被文件占用：{target}");
                    continue;
                }
                plan.DirectoriesToCreate.Add(target);
            }

            plan.DirectoriesToCreate.Sort(ComparePathByDepthThenOrdinal);
            return plan;
        }

        public static EUIModuleTemplateRenamePlan BuildRenamePlan(
            EUIModuleTemplateSnapshot snapshot, string templateFolderPath, string newName)
        {
            var plan = new EUIModuleTemplateRenamePlan
            {
                Snapshot = snapshot,
                TemplateSourcePath = EUIPrefabCatalogService.NormalizeAssetPath(templateFolderPath),
                NewName = newName?.Trim(),
            };
            if (!ValidateSnapshot(snapshot, plan.Errors)) return plan;
            if (!TryGetTemplateRelativePath(snapshot, plan.TemplateSourcePath,
                    out var oldRelativePath))
            {
                plan.Errors.Add($"只能重命名模板内的子目录：{plan.TemplateSourcePath}");
                return plan;
            }
            if (IsProtectedTemplatePath(oldRelativePath))
            {
                plan.Errors.Add($"{RequiredPrefabDirectoryName} 是创建 UI 必需目录，禁止重命名。");
                return plan;
            }
            if (!TryValidateFolderName(plan.NewName, out var nameError))
            {
                plan.Errors.Add(nameError);
                return plan;
            }

            var oldName = GetLastSegment(plan.TemplateSourcePath);
            if (string.Equals(oldName, plan.NewName, StringComparison.Ordinal))
            {
                plan.Errors.Add("新目录名与当前名称相同。");
                return plan;
            }
            if (string.Equals(oldName, plan.NewName, StringComparison.OrdinalIgnoreCase))
            {
                plan.Errors.Add("暂不支持仅改变大小写的目录重命名。");
                return plan;
            }

            var relativeParent = GetParentRelativePath(oldRelativePath);
            var newRelativePath = string.IsNullOrEmpty(relativeParent)
                ? plan.NewName
                : $"{relativeParent}/{plan.NewName}";
            plan.TemplateTargetPath = $"{snapshot.TemplateRoot}/{newRelativePath}";
            if (AssetDatabase.IsValidFolder(plan.TemplateTargetPath)
                || PathExistsButIsNotFolder(plan.TemplateTargetPath))
            {
                plan.Errors.Add($"模板中的目标名称已存在：{plan.TemplateTargetPath}");
                return plan;
            }

            foreach (var moduleDirectory in snapshot.ModuleDirectories)
            {
                var source = $"{moduleDirectory}/{oldRelativePath}";
                var target = $"{moduleDirectory}/{newRelativePath}";
                var sourceIsFolder = AssetDatabase.IsValidFolder(source);
                var targetIsFolder = AssetDatabase.IsValidFolder(target);
                if (PathExistsButIsNotFolder(source) || PathExistsButIsNotFolder(target))
                {
                    plan.Errors.Add($"模块中存在同名文件冲突：{moduleDirectory}");
                    continue;
                }
                if (sourceIsFolder && targetIsFolder)
                {
                    plan.Errors.Add($"模块同时存在旧目录和新目录，拒绝自动合并：{source} / {target}");
                    continue;
                }
                if (sourceIsFolder)
                {
                    plan.ModuleMoves.Add(new KeyValuePair<string, string>(source, target));
                    continue;
                }
                if (targetIsFolder)
                {
                    plan.AlreadyAlignedModules.Add(moduleDirectory);
                    continue;
                }
                plan.ModuleDirectoriesToCreate.Add(target);
            }

            plan.ModuleMoves.Sort((a, b) => string.Compare(a.Key, b.Key,
                StringComparison.OrdinalIgnoreCase));
            plan.ModuleDirectoriesToCreate.Sort(ComparePathByDepthThenOrdinal);
            return plan;
        }

        public static EUIModuleTemplateDeletePlan BuildDeletePlan(
            EUIModuleTemplateSnapshot snapshot, string templateFolderPath)
        {
            var plan = new EUIModuleTemplateDeletePlan
            {
                Snapshot = snapshot,
                TemplateFolderPath = EUIPrefabCatalogService.NormalizeAssetPath(templateFolderPath),
            };
            if (!ValidateSnapshot(snapshot, plan.Errors)) return plan;
            if (!TryGetTemplateRelativePath(snapshot, plan.TemplateFolderPath,
                    out var relativePath))
            {
                plan.Errors.Add($"只能删除模板内的子目录：{plan.TemplateFolderPath}");
                return plan;
            }
            plan.ConfirmationName = GetLastSegment(plan.TemplateFolderPath);
            if (IsProtectedTemplatePath(relativePath))
            {
                plan.Errors.Add($"{RequiredPrefabDirectoryName} 是创建 UI 必需目录，禁止删除。");
                return plan;
            }

            try
            {
                plan.TemplateDirectories.Add(plan.TemplateFolderPath);
                CollectAssetSubdirectories(plan.TemplateFolderPath, plan.TemplateDirectories);
                plan.TemplateDirectories.Sort(ComparePathByDepthThenOrdinal);

                var fullPath = EUIPrefabCatalogService.ToFullPath(plan.TemplateFolderPath);
                if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath))
                {
                    plan.Errors.Add($"模板目录不存在：{plan.TemplateFolderPath}");
                    return plan;
                }
                foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
                             .Where(path => !path.EndsWith(".meta",
                                 StringComparison.OrdinalIgnoreCase)))
                {
                    plan.AssetFiles.Add(ToAssetPath(plan.TemplateFolderPath, fullPath, file));
                }

                foreach (var moduleDirectory in snapshot.ModuleDirectories)
                {
                    var modulePath = $"{moduleDirectory}/{relativePath}";
                    if (AssetDatabase.IsValidFolder(modulePath))
                        plan.PreservedModulePaths.Add(modulePath);
                }
            }
            catch (Exception exception)
            {
                plan.Errors.Add($"检查待删除模板目录失败：{exception.Message}");
            }
            return plan;
        }

        #endregion

        #region 执行

        public static EUIModuleTemplateResult ExecuteInitialization(
            EUIModuleInitializationPlan plan)
        {
            var result = new EUIModuleTemplateResult();
            if (plan?.Snapshot == null)
            {
                result.Fail("新模块初始化计划为空。");
                return result;
            }

            var currentSnapshot = Scan(plan.Snapshot.UIResourceRoot);
            var currentPlan = BuildInitializationPlan(currentSnapshot, plan.ModuleDirectory);
            if (!currentPlan.CanExecute)
            {
                result.Fail(string.Join("\n", currentPlan.Errors));
                return result;
            }
            if (AssetDatabase.IsValidFolder(currentPlan.ModuleDirectory))
            {
                result.Messages.Add($"模块目录已存在，未自动修改：{currentPlan.ModuleDirectory}");
                return result;
            }

            foreach (var path in currentPlan.DirectoriesToCreate)
            {
                if (!TryEnsureFolder(path, currentSnapshot.ModulesRoot,
                        result.CreatedPaths, out var error))
                {
                    result.Fail(error);
                    return result;
                }
            }
            result.Messages.Add($"已按模板初始化模块：{currentPlan.ModuleDirectory}");
            return result;
        }

        public static EUIModuleTemplateResult ExecuteSync(EUIModuleTemplateSyncPlan plan)
        {
            var result = new EUIModuleTemplateResult();
            if (plan?.Snapshot == null)
            {
                result.Fail("批量同步计划为空。");
                return result;
            }

            var currentSnapshot = Scan(plan.Snapshot.UIResourceRoot);
            var currentPlan = BuildSyncPlan(currentSnapshot);
            if (!currentPlan.CanExecute)
            {
                result.Fail(string.Join("\n", currentPlan.Errors));
                return result;
            }
            if (!HaveSamePaths(plan.DirectoriesToCreate, currentPlan.DirectoriesToCreate))
            {
                result.Fail("模板或模块目录在预览后发生变化，请重新预览同步计划。");
                return result;
            }

            foreach (var path in currentPlan.DirectoriesToCreate)
            {
                if (!TryEnsureFolder(path, currentSnapshot.ModulesRoot,
                        result.CreatedPaths, out var error))
                {
                    result.Fail(error);
                    return result;
                }
            }
            result.Messages.Add(result.CreatedPaths.Count == 0
                ? "所有模块目录均已与模板对齐。"
                : $"已创建 {result.CreatedPaths.Count} 个缺失目录。");
            return result;
        }

        public static EUIModuleTemplateResult CreateTemplateFolder(
            EUIModuleTemplateSnapshot snapshot, string parentRelativePath, string folderName)
        {
            var result = new EUIModuleTemplateResult();
            var errors = new List<string>();
            if (!ValidateSnapshot(snapshot, errors))
            {
                result.Fail(string.Join("\n", errors));
                return result;
            }
            if (!TryValidateFolderName(folderName?.Trim(), out var nameError))
            {
                result.Fail(nameError);
                return result;
            }

            parentRelativePath = NormalizeRelativePath(parentRelativePath);
            var parent = string.IsNullOrEmpty(parentRelativePath)
                ? snapshot.TemplateRoot
                : $"{snapshot.TemplateRoot}/{parentRelativePath}";
            if (!AssetDatabase.IsValidFolder(parent)
                || (!string.Equals(parent, snapshot.TemplateRoot, StringComparison.OrdinalIgnoreCase)
                    && !EUIPrefabMaintenanceService.IsUnderRoot(parent, snapshot.TemplateRoot)))
            {
                result.Fail($"模板父目录无效：{parent}");
                return result;
            }

            var name = folderName.Trim();
            var target = $"{parent}/{name}";
            if (AssetDatabase.IsValidFolder(target) || PathExistsButIsNotFolder(target))
            {
                result.Fail($"模板目录已存在：{target}");
                return result;
            }

            var guid = AssetDatabase.CreateFolder(parent, name);
            if (string.IsNullOrEmpty(guid))
            {
                result.Fail($"创建模板目录失败：{target}");
                return result;
            }
            result.CreatedPaths.Add(target);
            result.Messages.Add($"已创建模板目录：{target}");
            return result;
        }

        public static EUIModuleTemplateResult ExecuteRename(EUIModuleTemplateRenamePlan plan)
        {
            var result = new EUIModuleTemplateResult();
            if (plan?.Snapshot == null)
            {
                result.Fail("重命名计划为空。");
                return result;
            }

            var currentSnapshot = Scan(plan.Snapshot.UIResourceRoot);
            var currentPlan = BuildRenamePlan(currentSnapshot, plan.TemplateSourcePath,
                plan.NewName);
            if (!currentPlan.CanExecute)
            {
                result.Fail(string.Join("\n", currentPlan.Errors));
                return result;
            }
            if (!HaveSameMoves(plan.ModuleMoves, currentPlan.ModuleMoves)
                || !HaveSamePaths(plan.ModuleDirectoriesToCreate,
                    currentPlan.ModuleDirectoriesToCreate)
                || !string.Equals(plan.TemplateTargetPath, currentPlan.TemplateTargetPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Fail("模板或模块目录在预览后发生变化，请重新预览重命名计划。");
                return result;
            }

            var completedMoves = new List<KeyValuePair<string, string>>();
            var createdPaths = new List<string>();
            foreach (var move in currentPlan.ModuleMoves)
            {
                var moveError = AssetDatabase.MoveAsset(move.Key, move.Value);
                if (!string.IsNullOrEmpty(moveError))
                {
                    result.Fail($"重命名模块目录失败：{move.Key} → {move.Value}\n{moveError}");
                    RollbackRename(completedMoves, createdPaths, result);
                    return result;
                }
                completedMoves.Add(move);
            }

            foreach (var path in currentPlan.ModuleDirectoriesToCreate)
            {
                if (!TryEnsureFolder(path, currentSnapshot.ModulesRoot,
                        createdPaths, out var error))
                {
                    result.Fail(error);
                    RollbackRename(completedMoves, createdPaths, result);
                    return result;
                }
            }

            var templateMoveError = AssetDatabase.MoveAsset(
                currentPlan.TemplateSourcePath, currentPlan.TemplateTargetPath);
            if (!string.IsNullOrEmpty(templateMoveError))
            {
                result.Fail($"重命名模板目录失败：{templateMoveError}");
                RollbackRename(completedMoves, createdPaths, result);
                return result;
            }

            result.CreatedPaths.AddRange(createdPaths);
            result.Messages.Add($"已重命名模板目录，并同步 {currentSnapshot.ModuleDirectories.Count} 个模块。");
            return result;
        }

        public static EUIModuleTemplateResult ExecuteDelete(EUIModuleTemplateDeletePlan plan)
        {
            var result = new EUIModuleTemplateResult();
            if (plan?.Snapshot == null)
            {
                result.Fail("删除计划为空。");
                return result;
            }

            var currentSnapshot = Scan(plan.Snapshot.UIResourceRoot);
            var currentPlan = BuildDeletePlan(currentSnapshot, plan.TemplateFolderPath);
            if (!currentPlan.CanExecute)
            {
                result.Fail(currentPlan.BuildSummary());
                return result;
            }
            if (!HaveSamePaths(plan.TemplateDirectories, currentPlan.TemplateDirectories)
                || !HaveSamePaths(plan.AssetFiles, currentPlan.AssetFiles))
            {
                result.Fail("模板目录在确认后发生变化，请重新发起删除。");
                return result;
            }

            if (!AssetDatabase.MoveAssetToTrash(currentPlan.TemplateFolderPath))
            {
                result.Fail($"无法将模板目录移入系统回收站：{currentPlan.TemplateFolderPath}");
                return result;
            }
            result.Messages.Add($"已移入系统回收站：{currentPlan.TemplateFolderPath}");
            if (currentPlan.PreservedModulePaths.Count > 0)
                result.Messages.Add($"已有模块中的 {currentPlan.PreservedModulePaths.Count} 个对应目录保持不变。");
            return result;
        }

        #endregion

        #region 内部方法

        private static string ResolveConfiguredUIResourceRoot()
        {
            var settings = EUIBindingSettingData.LoadExistingSettings();
            var implementation = settings?.LogicImplementations?
                .OfType<CSharpLogicImplementationData>()
                .FirstOrDefault(item => item);
            return EUIPrefabCatalogService.NormalizeAssetPath(implementation?.UIResourceRoot);
        }

        private static bool ValidateSnapshot(EUIModuleTemplateSnapshot snapshot,
            ICollection<string> errors)
        {
            if (snapshot == null)
            {
                errors.Add("模块目录模板快照为空。");
                return false;
            }
            if (!snapshot.IsValid)
            {
                errors.Add(snapshot.Error);
                return false;
            }
            if (!EUIPrefabCatalogService.IsSafeAssetPath(snapshot.TemplateRoot)
                || !EUIPrefabMaintenanceService.IsUnderRoot(
                    snapshot.TemplateRoot, snapshot.ModulesRoot)
                || !AssetDatabase.IsValidFolder(snapshot.TemplateRoot))
            {
                errors.Add($"模板目录已失效：{snapshot.TemplateRoot}");
                return false;
            }
            return true;
        }

        private static void CollectTemplateDirectories(string templateRoot, string parent,
            ICollection<string> relativePaths)
        {
            foreach (var child in AssetDatabase.GetSubFolders(parent)
                         .Select(EUIPrefabCatalogService.NormalizeAssetPath)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                relativePaths.Add(child.Substring(templateRoot.Length + 1));
                CollectTemplateDirectories(templateRoot, child, relativePaths);
            }
        }

        private static void CollectAssetSubdirectories(string parent, ICollection<string> paths)
        {
            foreach (var child in AssetDatabase.GetSubFolders(parent)
                         .Select(EUIPrefabCatalogService.NormalizeAssetPath))
            {
                paths.Add(child);
                CollectAssetSubdirectories(child, paths);
            }
        }

        private static bool TryGetTemplateRelativePath(EUIModuleTemplateSnapshot snapshot,
            string templatePath, out string relativePath)
        {
            relativePath = null;
            if (!EUIPrefabCatalogService.IsSafeAssetPath(templatePath)
                || !AssetDatabase.IsValidFolder(templatePath)
                || string.Equals(templatePath, snapshot.TemplateRoot,
                    StringComparison.OrdinalIgnoreCase)
                || !EUIPrefabMaintenanceService.IsUnderRoot(templatePath,
                    snapshot.TemplateRoot))
                return false;
            relativePath = templatePath.Substring(snapshot.TemplateRoot.Length + 1);
            return !string.IsNullOrEmpty(relativePath);
        }

        private static bool IsProtectedTemplatePath(string relativePath)
        {
            return string.Equals(relativePath, RequiredPrefabDirectoryName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectModuleDirectory(EUIModuleTemplateSnapshot snapshot,
            string moduleDirectory)
        {
            if (!EUIPrefabCatalogService.IsSafeAssetPath(moduleDirectory)
                || !EUIPrefabMaintenanceService.IsUnderRoot(moduleDirectory,
                    snapshot.ModulesRoot)) return false;
            var parent = moduleDirectory.LastIndexOf('/') >= 0
                ? moduleDirectory.Substring(0, moduleDirectory.LastIndexOf('/'))
                : string.Empty;
            return string.Equals(parent, snapshot.ModulesRoot,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryEnsureFolder(string target, string allowedRoot,
            ICollection<string> createdPaths, out string error)
        {
            error = null;
            target = EUIPrefabCatalogService.NormalizeAssetPath(target);
            allowedRoot = EUIPrefabCatalogService.NormalizeAssetPath(allowedRoot);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(target)
                || !EUIPrefabMaintenanceService.IsUnderRoot(target, allowedRoot))
            {
                error = $"目录目标超出允许范围：{target}";
                return false;
            }

            var segments = target.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    if (PathExistsButIsNotFolder(next))
                    {
                        error = $"目录目标已被文件占用：{next}";
                        return false;
                    }
                    var guid = AssetDatabase.CreateFolder(current, segments[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        error = $"创建目录失败：{next}";
                        return false;
                    }
                    createdPaths.Add(next);
                }
                current = next;
            }
            return true;
        }

        private static void RollbackRename(
            IList<KeyValuePair<string, string>> completedMoves,
            IList<string> createdPaths, EUIModuleTemplateResult result)
        {
            for (var i = createdPaths.Count - 1; i >= 0; i--)
            {
                var path = createdPaths[i];
                if (!IsEmptyDirectoryTree(path))
                {
                    result.Fail($"回滚时目录已非空，为避免误删而保留：{path}");
                    continue;
                }
                if (!AssetDatabase.DeleteAsset(path))
                    result.Fail($"回滚新建目录失败：{path}");
            }
            for (var i = completedMoves.Count - 1; i >= 0; i--)
            {
                var move = completedMoves[i];
                var rollbackError = AssetDatabase.MoveAsset(move.Value, move.Key);
                if (!string.IsNullOrEmpty(rollbackError))
                    result.Fail($"回滚目录重命名失败：{move.Value} → {move.Key}\n{rollbackError}");
            }
        }

        private static bool IsEmptyDirectoryTree(string assetPath)
        {
            var fullPath = EUIPrefabCatalogService.ToFullPath(assetPath);
            if (string.IsNullOrEmpty(fullPath) || !Directory.Exists(fullPath)) return false;
            return Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
                .All(path => path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
        }

        private static bool PathExistsButIsNotFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return false;
            var fullPath = EUIPrefabCatalogService.ToFullPath(assetPath);
            return !string.IsNullOrEmpty(fullPath)
                   && (File.Exists(fullPath) || Directory.Exists(fullPath));
        }

        private static bool TryValidateFolderName(string name, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name)
                || !string.Equals(name, name.Trim(), StringComparison.Ordinal)
                || name == "." || name == ".."
                || name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name.EndsWith(".", StringComparison.Ordinal)
                || name.EndsWith("~", StringComparison.Ordinal)
                || ReservedWindowsNames.Contains(name))
            {
                error = $"文件夹名称无效：{name ?? "—"}";
                return false;
            }
            return true;
        }

        private static bool HaveSamePaths(IEnumerable<string> first, IEnumerable<string> second)
        {
            return new HashSet<string>(first ?? Enumerable.Empty<string>(),
                       StringComparer.OrdinalIgnoreCase)
                .SetEquals(second ?? Enumerable.Empty<string>());
        }

        private static bool HaveSameMoves(
            IEnumerable<KeyValuePair<string, string>> first,
            IEnumerable<KeyValuePair<string, string>> second)
        {
            var firstValues = (first ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .Select(item => $"{item.Key}\n{item.Value}");
            var secondValues = (second ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .Select(item => $"{item.Key}\n{item.Value}");
            return HaveSamePaths(firstValues, secondValues);
        }

        private static int ComparePathByDepthThenOrdinal(string first, string second)
        {
            var depth = CountSegments(first).CompareTo(CountSegments(second));
            return depth != 0
                ? depth
                : string.Compare(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountSegments(string path)
        {
            return string.IsNullOrEmpty(path) ? 0 : path.Count(character => character == '/') + 1;
        }

        private static string NormalizeRelativePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Trim().Replace('\\', '/').Trim('/');
        }

        private static string GetParentRelativePath(string relativePath)
        {
            var index = relativePath.LastIndexOf('/');
            return index < 0 ? string.Empty : relativePath.Substring(0, index);
        }

        private static string GetLastSegment(string path)
        {
            var index = path.LastIndexOf('/');
            return index < 0 ? path : path.Substring(index + 1);
        }

        private static string ToAssetPath(string assetRoot, string fullRoot, string fullPath)
        {
            return assetRoot + "/" + fullPath.Substring(fullRoot.Length + 1)
                .Replace('\\', '/');
        }

        #endregion
    }
}
