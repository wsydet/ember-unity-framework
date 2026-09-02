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

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>删除一个 UI 前展示给用户的精确影响清单。</summary>
    public sealed class EUIDeletePlan
    {
        public EUIPrefabCatalogEntry Entry;
        public readonly List<string> AssetPaths = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> Errors = new List<string>();
        public string PageDefFile;

        public bool CanExecute => Entry != null && Errors.Count == 0;

        public string BuildSummary()
        {
            var builder = new StringBuilder();
            foreach (var path in AssetPaths) builder.AppendLine(path);
            if (!string.IsNullOrEmpty(PageDefFile))
                builder.AppendLine($"{PageDefFile}：精确删除 {Entry.PageName}");
            foreach (var warning in Warnings) builder.AppendLine($"⚠ {warning}");
            foreach (var error in Errors) builder.AppendLine($"✖ {error}");
            return builder.ToString().TrimEnd();
        }
    }

    /// <summary>维护操作结果；失败时保留已完成项目，明确报告，不伪装成事务成功。</summary>
    public sealed class EUIMaintenanceResult
    {
        public bool Success = true;
        public int ChangedCount;
        public readonly List<string> Messages = new List<string>();

        public string Message => string.Join("\n", Messages);

        public void Fail(string message)
        {
            Success = false;
            Messages.Add(message);
        }
    }

    /// <summary>由自动生成的 .Binding.cs 锚定的一组孤儿产物。</summary>
    public sealed class EUIOrphanScriptGroup
    {
        public string BindingScriptPath;
        public readonly List<string> AssetPaths = new List<string>();
    }

    /// <summary>UI 开发中心的破坏性维护服务。所有入口均不显示弹窗，由窗口统一确认。</summary>
    public static class EUIPrefabMaintenanceService
    {
        private const string TAG = LogTags.EmberUI;
        private const string GeneratedBindingMarker = "本文件为自动生成，请勿修改";

        /// <summary>构建单 UI 的精确删除计划，不执行写入。</summary>
        public static EUIDeletePlan BuildDeletePlan(EUIPrefabCatalogSnapshot snapshot,
            EUIPrefabCatalogEntry entry)
        {
            var plan = new EUIDeletePlan { Entry = entry };
            if (snapshot == null || entry == null)
            {
                plan.Errors.Add("目录快照或 UI 条目为空。");
                return plan;
            }

            if (!ValidateSnapshotForDestructiveUse(snapshot, out var snapshotError))
            {
                plan.Errors.Add(snapshotError);
                return plan;
            }

            if (!IsUnderRoot(entry.PrefabPath, snapshot.UIResourceRoot)
                || !EUIPrefabCatalogService.IsSafeAssetPath(entry.PrefabPath)
                || !entry.PrefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                plan.Errors.Add($"预制体不在允许的 UI Assets 范围：{entry.PrefabPath}");
                return plan;
            }
            plan.AssetPaths.Add(entry.PrefabPath);

            if (!entry.NoCodeGeneration)
            {
                AddScriptIfExclusive(snapshot, plan, entry.LogicScriptPath, entry);
                AddScriptIfExclusive(snapshot, plan, entry.BindingScriptPath, entry);
                if (entry.GenerateCustomSettings)
                    AddScriptIfExclusive(snapshot, plan, entry.SettingsScriptPath, entry);
            }

            if (entry.IsPage && !string.IsNullOrEmpty(entry.PageDefFile))
            {
                var isKnownFile = string.Equals(entry.PageDefFile, snapshot.UserPageDefFile,
                        StringComparison.Ordinal)
                    || string.Equals(entry.PageDefFile, snapshot.FrameworkPageDefFile,
                        StringComparison.Ordinal);
                if (!isKnownFile)
                    plan.Errors.Add($"PageDef 文件不在配置范围：{entry.PageDefFile}");
                else if (entry.PageDefMatchCount > 1)
                    plan.Errors.Add($"检测到 {entry.PageDefMatchCount} 个同名 PageDef，拒绝自动删除。");
                else if (!entry.PageDefOk)
                    plan.Errors.Add("PageDef 路径与目标预制体不一致，拒绝自动删除。");
                else if (!CanRemoveExactPageDef(entry.PageDefFile, entry.PageName,
                             entry.PrefabPath, out var removeError))
                    plan.Errors.Add(removeError);
                else
                    plan.PageDefFile = entry.PageDefFile;
            }
            return plan;
        }

        /// <summary>执行已经预览并由用户确认的删除计划。</summary>
        public static EUIMaintenanceResult ExecuteDelete(EUIDeletePlan plan,
            EUIPrefabCatalogSnapshot snapshot)
        {
            var result = new EUIMaintenanceResult();
            if (plan == null || snapshot == null || !plan.CanExecute)
            {
                result.Fail("删除计划无效，未执行任何操作。");
                return result;
            }

            if (!TryValidateDeletePlanAtExecution(plan, snapshot, out var validationError))
            {
                result.Fail(validationError);
                return result;
            }

            // 先删除目标 prefab；失败则停止，避免留下“页面仍在但脚本已删”的更坏状态。
            if (!TryDeleteAsset(plan.Entry.PrefabPath, out var deleteError))
            {
                result.Fail(deleteError);
                return result;
            }
            result.ChangedCount++;
            result.Messages.Add($"已删除：{plan.Entry.PrefabPath}");

            foreach (var path in plan.AssetPaths.Where(path => path != plan.Entry.PrefabPath))
            {
                if (!EUIPrefabCatalogService.IsSafeAssetPath(path)
                    || !IsUnderRoot(path, snapshot.BusinessCodeRoot))
                {
                    result.Fail($"脚本超出业务代码根目录，已跳过：{path}");
                    continue;
                }
                if (!TryDeleteAsset(path, out deleteError))
                {
                    result.Fail(deleteError);
                    continue;
                }
                result.ChangedCount++;
                result.Messages.Add($"已删除：{path}");
            }

            if (!string.IsNullOrEmpty(plan.PageDefFile))
            {
                if (TryRemoveExactPageDef(plan.PageDefFile, plan.Entry.PageName,
                        plan.Entry.PrefabPath, out var error))
                {
                    result.ChangedCount++;
                    result.Messages.Add($"已删除 PageDef：{plan.Entry.PageName}");
                }
                else
                {
                    result.Fail(error);
                }
            }
            return result;
        }

        /// <summary>从配置代码根中寻找以自动生成 Binding 文件为锚点、且没有任何 prefab 引用的产物组。</summary>
        public static List<EUIOrphanScriptGroup> FindOrphanScriptGroups(
            EUIPrefabCatalogSnapshot snapshot)
        {
            var result = new List<EUIOrphanScriptGroup>();
            if (snapshot == null
                || !EUIPrefabCatalogService.IsSafeAssetPath(snapshot.BusinessCodeRoot)
                || !AssetDatabase.IsValidFolder(snapshot.BusinessCodeRoot)) return result;

            var rootFullPath = EUIPrefabCatalogService.ToFullPath(snapshot.BusinessCodeRoot);
            if (string.IsNullOrEmpty(rootFullPath) || !Directory.Exists(rootFullPath)) return result;

            if (!TryCollectReferencedScriptPaths(snapshot.BusinessCodeRoot, null,
                    out var referencedScripts, out var referenceError))
            {
                EmberDebug.LogError(TAG, $"UI 孤儿脚本扫描已停止：{referenceError}");
                return result;
            }

            string[] bindingFiles;
            try
            {
                bindingFiles = Directory.GetFiles(rootFullPath, "*.Binding.cs",
                    SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                EmberDebug.LogError(TAG, $"UI 孤儿脚本扫描失败：{exception.Message}");
                return result;
            }

            foreach (var fullPath in bindingFiles)
            {
                var relative = snapshot.BusinessCodeRoot + "/"
                    + fullPath.Substring(rootFullPath.Length + 1).Replace('\\', '/');
                relative = EUIPrefabCatalogService.NormalizeAssetPath(relative);
                if (!EUIPrefabCatalogService.IsSafeAssetPath(relative)
                    || !IsUnderRoot(relative, snapshot.BusinessCodeRoot)
                    || referencedScripts.Contains(relative)) continue;

                string content;
                try
                {
                    content = File.ReadAllText(fullPath, Encoding.UTF8);
                }
                catch (Exception exception)
                {
                    EmberDebug.LogError(TAG,
                        $"无法检查自动生成标记：{relative}\n{exception.Message}");
                    continue;
                }
                if (!content.Contains(GeneratedBindingMarker)) continue;

                var group = new EUIOrphanScriptGroup { BindingScriptPath = relative };
                group.AssetPaths.Add(relative);
                var logicPath = relative.Substring(0,
                    relative.Length - ".Binding.cs".Length) + ".cs";
                var settingsPath = relative.Substring(0,
                    relative.Length - ".Binding.cs".Length) + "Settings.cs";
                if (File.Exists(EUIPrefabCatalogService.ToFullPath(logicPath)))
                    group.AssetPaths.Add(logicPath);
                if (File.Exists(EUIPrefabCatalogService.ToFullPath(settingsPath)))
                    group.AssetPaths.Add(settingsPath);
                result.Add(group);
            }

            result.Sort((a, b) => string.Compare(a.BindingScriptPath,
                b.BindingScriptPath, StringComparison.Ordinal));
            return result;
        }

        public static EUIMaintenanceResult DeleteOrphanScriptGroup(
            EUIOrphanScriptGroup group, string businessCodeRoot)
        {
            var result = new EUIMaintenanceResult();
            if (group == null)
            {
                result.Fail("孤儿脚本组为空。");
                return result;
            }

            if (!TryValidateOrphanGroupAtExecution(group, businessCodeRoot,
                    out var validationError))
            {
                result.Fail(validationError);
                return result;
            }

            foreach (var path in group.AssetPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!EUIPrefabCatalogService.IsSafeAssetPath(path)
                    || !IsUnderRoot(path, businessCodeRoot))
                {
                    result.Fail($"超出业务代码根目录，已跳过：{path}");
                    continue;
                }
                if (!TryDeleteAsset(path, out var deleteError))
                {
                    result.Fail(deleteError);
                    continue;
                }
                result.ChangedCount++;
                result.Messages.Add($"已删除：{path}");
            }
            return result;
        }

        public static int RemoveMissingScriptsInPrefab(string prefabPath)
        {
            if (!IsSafeMaintainedPrefabAsset(prefabPath, out var rangeError))
            {
                EmberDebug.LogError(TAG, rangeError);
                return 0;
            }

            GameObject contents = null;
            var removed = 0;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (!contents) return 0;
                foreach (var transform in contents.GetComponentsInChildren<Transform>(true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                if (removed > 0)
                {
                    var saved = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath,
                        out var saveSucceeded);
                    if (!saveSucceeded || !saved)
                    {
                        EmberDebug.LogError(TAG,
                            $"移除 Missing Script 后保存预制体失败：{prefabPath}");
                        return 0;
                    }
                }
            }
            catch (Exception exception)
            {
                EmberDebug.LogError(TAG,
                    $"移除 Missing Script 失败：{prefabPath}\n{exception.Message}");
                return 0;
            }
            finally
            {
                if (contents) PrefabUtility.UnloadPrefabContents(contents);
            }
            return removed;
        }

        public static int RemoveNullBindingsInPrefab(string prefabPath)
        {
            if (!IsSafeMaintainedPrefabAsset(prefabPath, out var rangeError))
            {
                EmberDebug.LogError(TAG, rangeError);
                return 0;
            }

            GameObject contents = null;
            var removed = 0;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (!contents) return 0;
                var binding = contents.GetComponent<EUIBinding>();
                if (!binding) return 0;

                using (var serializedObject = new SerializedObject(binding))
                {
                    var bindings = serializedObject.FindProperty("bindings");
                    for (var index = bindings.arraySize - 1; index >= 0; index--)
                    {
                        var target = bindings.GetArrayElementAtIndex(index)
                            .FindPropertyRelative("GameObject");
                        if (target.objectReferenceValue != null) continue;
                        bindings.DeleteArrayElementAtIndex(index);
                        removed++;
                    }
                    if (removed > 0)
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        var saved = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath,
                            out var saveSucceeded);
                        if (!saveSucceeded || !saved)
                        {
                            EmberDebug.LogError(TAG,
                                $"清理空引用绑定后保存预制体失败：{prefabPath}");
                            return 0;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                EmberDebug.LogError(TAG,
                    $"清理空引用绑定失败：{prefabPath}\n{exception.Message}");
                return 0;
            }
            finally
            {
                if (contents) PrefabUtility.UnloadPrefabContents(contents);
            }
            return removed;
        }

        /// <summary>删除单个普通空叶子；执行时再次验证，防止 dry-run 后状态变化。</summary>
        public static bool DeleteEmptyLeaf(string prefabPath, string nodePath, out string error)
        {
            error = null;
            if (!IsSafeMaintainedPrefabAsset(prefabPath, out error))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(nodePath)
                || nodePath.StartsWith("/", StringComparison.Ordinal)
                || nodePath.Split('/').Any(segment => string.IsNullOrEmpty(segment)
                    || segment == "." || segment == ".."))
            {
                error = $"节点路径无效：{nodePath}";
                return false;
            }

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                if (!contents)
                {
                    error = $"无法加载预制体：{prefabPath}";
                    return false;
                }
                var target = contents.transform.Find(nodePath);
                if (!target || !EUIPrefabCatalogService.IsDeletableEmptyLeaf(contents, target))
                {
                    error = $"节点已不存在或属于受保护结构：{nodePath}";
                    return false;
                }
                UnityEngine.Object.DestroyImmediate(target.gameObject);
                var saved = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath,
                    out var saveSucceeded);
                if (!saveSucceeded || !saved)
                {
                    error = $"删除空叶子后保存预制体失败：{prefabPath}";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"删除空叶子失败：{exception.Message}";
                return false;
            }
            finally
            {
                if (contents) PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        public static bool IsUnderRoot(string assetPath, string root)
        {
            assetPath = EUIPrefabCatalogService.NormalizeAssetPath(assetPath);
            root = EUIPrefabCatalogService.NormalizeAssetPath(root);
            return IsAssetsBoundaryPath(assetPath)
                && IsAssetsBoundaryPath(root)
                && (string.Equals(assetPath, root, StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAssetsBoundaryPath(string path)
        {
            if (string.IsNullOrEmpty(path)
                || path.StartsWith("/", StringComparison.Ordinal)
                || path.Contains(":")
                || path.Contains("//"))
                return false;

            var segments = path.Split('/');
            return segments.Length > 0
                && string.Equals(segments[0], "Assets", StringComparison.OrdinalIgnoreCase)
                && segments.All(segment => !string.IsNullOrEmpty(segment)
                    && string.Equals(segment, segment.Trim(), StringComparison.Ordinal)
                    && segment != "."
                    && segment != "..");
        }

        private static void AddScriptIfExclusive(EUIPrefabCatalogSnapshot snapshot,
            EUIDeletePlan plan, string path, EUIPrefabCatalogEntry target)
        {
            if (string.IsNullOrEmpty(path)
                || !File.Exists(EUIPrefabCatalogService.ToFullPath(path))) return;
            if (!EUIPrefabCatalogService.IsSafeAssetPath(path)
                || !IsUnderRoot(path, snapshot.BusinessCodeRoot))
            {
                plan.Errors.Add($"脚本超出业务代码根目录：{path}");
                return;
            }

            var shared = snapshot.Entries.Any(entry => entry != target
                && (string.Equals(entry.LogicScriptPath, path, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.BindingScriptPath, path, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entry.SettingsScriptPath, path, StringComparison.OrdinalIgnoreCase)));
            if (shared)
            {
                plan.Warnings.Add($"脚本仍被其他 UI 引用，保留：{path}");
                return;
            }
            plan.AssetPaths.Add(path);
        }

        private static bool TryRemoveExactPageDef(string pageDefFile, string pageName,
            string prefabPath, out string error)
        {
            error = null;
            var fullPath = EUIPrefabCatalogService.ToFullPath(pageDefFile);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(pageDefFile)
                || string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                error = $"PageDef 文件不存在：{pageDefFile}";
                return false;
            }

            List<string> lines;
            try
            {
                lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
            }
            catch (Exception exception)
            {
                error = $"读取 PageDef 失败：{exception.Message}";
                return false;
            }
            if (!TryFindExactPageDefRange(lines, pageName, prefabPath,
                    out var declarationStart, out var declarationEnd, out error))
                return false;

            var start = FindOwnedSummaryStart(lines, declarationStart);
            var count = declarationEnd - start + 1;
            if (declarationEnd + 1 < lines.Count && string.IsNullOrWhiteSpace(lines[declarationEnd + 1]))
                count++;
            lines.RemoveRange(start, count);
            try
            {
                File.WriteAllText(fullPath, string.Join("\n", lines), new UTF8Encoding(false));
                return true;
            }
            catch (Exception exception)
            {
                error = $"写入 PageDef 失败：{exception.Message}";
                return false;
            }
        }

        private static bool CanRemoveExactPageDef(string pageDefFile, string pageName,
            string prefabPath, out string error)
        {
            var fullPath = EUIPrefabCatalogService.ToFullPath(pageDefFile);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(pageDefFile)
                || string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                error = $"PageDef 文件不存在：{pageDefFile}";
                return false;
            }
            List<string> lines;
            try
            {
                lines = File.ReadAllLines(fullPath, Encoding.UTF8).ToList();
            }
            catch (Exception exception)
            {
                error = $"读取 PageDef 失败：{exception.Message}";
                return false;
            }
            return TryFindExactPageDefRange(lines, pageName, prefabPath,
                out _, out _, out error);
        }

        private static bool TryFindExactPageDefRange(IList<string> lines, string pageName,
            string prefabPath, out int declarationStart, out int declarationEnd, out string error)
        {
            declarationStart = -1;
            declarationEnd = -1;
            error = null;
            var matches = EUIPrefabCatalogService.FindPageDefinitions(string.Join("\n", lines))
                .Where(match => string.Equals(match.Name, pageName, StringComparison.Ordinal))
                .ToList();
            if (matches.Count != 1)
            {
                error = matches.Count == 0
                    ? $"未找到 PageDef：{pageName}"
                    : $"检测到多个同名 PageDef，拒绝删除：{pageName}";
                return false;
            }

            var match = matches[0];
            if (!match.IsStandardField)
            {
                error = $"PageDef 不是标准 public static readonly 字段，拒绝自动删除：{pageName}";
                return false;
            }
            declarationStart = match.DeclarationLine;
            declarationEnd = match.DeclarationEndLine;
            if (declarationEnd < declarationStart || declarationEnd - declarationStart > 12)
            {
                error = $"PageDef 格式过于复杂，拒绝自动删除：{pageName}";
                return false;
            }

            if (!string.Equals(match.PrefabPath, prefabPath, StringComparison.Ordinal))
            {
                error = $"PageDef 路径与预制体不匹配，拒绝删除：{pageName}";
                return false;
            }
            return true;
        }

        private static int FindOwnedSummaryStart(IList<string> lines, int declarationStart)
        {
            if (declarationStart >= 3
                && Regex.IsMatch(lines[declarationStart - 3], @"^\s*///\s*<summary>\s*$")
                && Regex.IsMatch(lines[declarationStart - 2], @"^\s*///(?:\s+.*)?$")
                && !Regex.IsMatch(lines[declarationStart - 2], @"<\/?summary>")
                && Regex.IsMatch(lines[declarationStart - 1], @"^\s*///\s*</summary>\s*$"))
                return declarationStart - 3;

            if (declarationStart >= 1
                && Regex.IsMatch(lines[declarationStart - 1],
                    @"^\s*///\s*<summary>.*</summary>\s*$"))
                return declarationStart - 1;
            return declarationStart;
        }

        private static bool TryValidateDeletePlanAtExecution(EUIDeletePlan plan,
            EUIPrefabCatalogSnapshot snapshot, out string error)
        {
            error = null;
            if (!ValidateSnapshotForDestructiveUse(snapshot, out error)) return false;
            if (plan.Entry == null
                || !IsSafePrefabAsset(plan.Entry.PrefabPath)
                || !IsUnderRoot(plan.Entry.PrefabPath, snapshot.UIResourceRoot))
            {
                error = $"预制体不在允许的 UI Assets 范围：{plan.Entry?.PrefabPath}";
                return false;
            }

            EUIBinding liveBinding;
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(plan.Entry.PrefabPath);
                liveBinding = prefab ? prefab.GetComponent<EUIBinding>() : null;
            }
            catch (Exception exception)
            {
                error = $"加载待删除预制体失败：{exception.Message}";
                return false;
            }
            if (!liveBinding)
            {
                error = $"待删除资产已不是根 EUIBinding 预制体：{plan.Entry.PrefabPath}";
                return false;
            }
            if (liveBinding.IsPage != plan.Entry.IsPage
                || (liveBinding.IsPage && !string.Equals(liveBinding.PageName,
                    plan.Entry.PageName, StringComparison.Ordinal))
                || liveBinding.NoCodeGeneration != plan.Entry.NoCodeGeneration)
            {
                error = "预制体页面信息已变更，请重新扫描后再删除。";
                return false;
            }

            if (!TryBuildExpectedScriptPaths(liveBinding, snapshot.BusinessCodeRoot,
                    out var liveScripts, out error)) return false;
            var allowedPaths = new HashSet<string>(liveScripts, StringComparer.OrdinalIgnoreCase)
            {
                plan.Entry.PrefabPath,
            };
            var plannedPaths = plan.AssetPaths
                .Select(EUIPrefabCatalogService.NormalizeAssetPath)
                .ToList();
            if (liveBinding.NoCodeGeneration && plannedPaths.Any(path =>
                    !string.Equals(path, plan.Entry.PrefabPath,
                        StringComparison.OrdinalIgnoreCase)))
            {
                error = "不生成代码的 EUIBinding 删除计划不能包含脚本资产。";
                return false;
            }
            if (plannedPaths.Count(path => string.Equals(path, plan.Entry.PrefabPath,
                    StringComparison.OrdinalIgnoreCase)) != 1
                || plannedPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    != plannedPaths.Count
                || plannedPaths.Any(path => !allowedPaths.Contains(path)))
            {
                error = "删除计划中包含非当前 EUIBinding 生成的资产，已拒绝执行。";
                return false;
            }
            foreach (var path in plannedPaths)
            {
                if (!EUIPrefabCatalogService.IsSafeAssetPath(path)
                    || (!string.Equals(path, plan.Entry.PrefabPath,
                            StringComparison.OrdinalIgnoreCase)
                        && !IsUnderRoot(path, snapshot.BusinessCodeRoot)))
                {
                    error = $"删除目标超出允许的 Assets 范围：{path}";
                    return false;
                }
                var fullPath = EUIPrefabCatalogService.ToFullPath(path);
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                {
                    error = $"删除目标已不存在，请重新扫描：{path}";
                    return false;
                }
            }

            if (!TryCollectReferencedScriptPaths(snapshot.BusinessCodeRoot,
                    plan.Entry.PrefabPath, out var referencedScripts, out error)) return false;
            var sharedPath = plannedPaths.FirstOrDefault(path =>
                !string.Equals(path, plan.Entry.PrefabPath, StringComparison.OrdinalIgnoreCase)
                && referencedScripts.Contains(path));
            if (!string.IsNullOrEmpty(sharedPath))
            {
                error = $"脚本当前仍被其他根 EUIBinding 预制体引用，已拒绝删除：{sharedPath}";
                return false;
            }

            if (string.IsNullOrEmpty(plan.PageDefFile)) return true;
            if (!string.Equals(plan.PageDefFile, snapshot.UserPageDefFile,
                    StringComparison.Ordinal)
                && !string.Equals(plan.PageDefFile, snapshot.FrameworkPageDefFile,
                    StringComparison.Ordinal))
            {
                error = $"PageDef 文件不在当前配置范围：{plan.PageDefFile}";
                return false;
            }
            return TryValidateExactPageDefAcrossFiles(snapshot, plan.PageDefFile,
                plan.Entry.PageName, plan.Entry.PrefabPath, out error);
        }

        private static bool TryValidateOrphanGroupAtExecution(EUIOrphanScriptGroup group,
            string businessCodeRoot, out string error)
        {
            error = null;
            businessCodeRoot = EUIPrefabCatalogService.NormalizeAssetPath(businessCodeRoot);
            var bindingPath = EUIPrefabCatalogService.NormalizeAssetPath(group.BindingScriptPath);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(businessCodeRoot)
                || !AssetDatabase.IsValidFolder(businessCodeRoot))
            {
                error = $"业务代码根目录不是 Assets/ 下的有效目录：{businessCodeRoot}";
                return false;
            }
            if (!EUIPrefabCatalogService.IsSafeAssetPath(bindingPath)
                || !IsUnderRoot(bindingPath, businessCodeRoot)
                || !bindingPath.EndsWith(".Binding.cs", StringComparison.Ordinal))
            {
                error = $"孤儿 Binding 路径超出允许范围：{bindingPath}";
                return false;
            }

            var bindingFullPath = EUIPrefabCatalogService.ToFullPath(bindingPath);
            string bindingContent;
            try
            {
                if (string.IsNullOrEmpty(bindingFullPath) || !File.Exists(bindingFullPath))
                {
                    error = $"孤儿 Binding 文件已不存在：{bindingPath}";
                    return false;
                }
                bindingContent = File.ReadAllText(bindingFullPath, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                error = $"复核自动生成 Binding 失败：{exception.Message}";
                return false;
            }
            if (!bindingContent.Contains(GeneratedBindingMarker))
            {
                error = $"Binding 已无自动生成标记，拒绝删除：{bindingPath}";
                return false;
            }

            var basePath = bindingPath.Substring(0,
                bindingPath.Length - ".Binding.cs".Length);
            var allowedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                bindingPath,
                basePath + ".cs",
                basePath + "Settings.cs",
            };
            var targets = group.AssetPaths
                .Select(EUIPrefabCatalogService.NormalizeAssetPath)
                .ToList();
            if (targets.Count == 0
                || !targets.Contains(bindingPath, StringComparer.OrdinalIgnoreCase)
                || targets.Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Count
                || targets.Any(path => !allowedPaths.Contains(path)
                    || !EUIPrefabCatalogService.IsSafeAssetPath(path)
                    || !IsUnderRoot(path, businessCodeRoot)))
            {
                error = "孤儿脚本组包含非预期或越界路径，已拒绝执行。";
                return false;
            }
            var missingTarget = targets.FirstOrDefault(path =>
            {
                var fullPath = EUIPrefabCatalogService.ToFullPath(path);
                return string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath);
            });
            if (!string.IsNullOrEmpty(missingTarget))
            {
                error = $"孤儿脚本列表已过期，目标不存在：{missingTarget}";
                return false;
            }
            if (!TryCollectReferencedScriptPaths(businessCodeRoot, null,
                    out var referencedScripts, out error)) return false;
            if (referencedScripts.Contains(bindingPath))
            {
                error = $"Binding 当前已被根 EUIBinding 预制体引用，拒绝删除：{bindingPath}";
                return false;
            }
            return true;
        }

        private static bool TryCollectReferencedScriptPaths(string businessCodeRoot,
            string excludedPrefabPath, out HashSet<string> referencedScripts, out string error)
        {
            referencedScripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            error = null;
            if (!EUIPrefabCatalogService.IsSafeAssetPath(businessCodeRoot)
                || !AssetDatabase.IsValidFolder(businessCodeRoot))
            {
                error = $"业务代码根目录无效：{businessCodeRoot}";
                return false;
            }

            string[] guids;
            try
            {
                guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            }
            catch (Exception exception)
            {
                error = $"无法扫描 Assets 预制体：{exception.Message}";
                return false;
            }
            foreach (var guid in guids)
            {
                var prefabPath = EUIPrefabCatalogService.NormalizeAssetPath(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (!IsSafePrefabAsset(prefabPath)
                    || string.Equals(prefabPath, excludedPrefabPath,
                        StringComparison.OrdinalIgnoreCase)) continue;

                EUIBinding binding;
                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (!prefab) continue;
                    binding = prefab.GetComponent<EUIBinding>();
                }
                catch (Exception exception)
                {
                    error = $"复核 EUIBinding 预制体失败：{prefabPath}\n{exception.Message}";
                    return false;
                }
                if (!binding) continue;
                if (!TryBuildExpectedScriptPaths(binding, businessCodeRoot,
                        out var paths, out error))
                {
                    error = $"{prefabPath}：{error}";
                    return false;
                }
                foreach (var path in paths) referencedScripts.Add(path);
            }
            return true;
        }

        private static bool TryBuildExpectedScriptPaths(EUIBinding binding,
            string businessCodeRoot, out List<string> paths, out string error)
        {
            paths = new List<string>();
            error = null;
            if (!binding) return true;

            var className = binding.ClassName?.Trim();
            if (string.IsNullOrEmpty(className)) return true;
            if (className.IndexOfAny(new[] { '/', '\\', ':' }) >= 0
                || className == "." || className == ".."
                || className.EndsWith("~", StringComparison.Ordinal))
            {
                error = $"EUIBinding 类名无法安全解析：{className}";
                return false;
            }

            var classPath = string.IsNullOrWhiteSpace(binding.ClassPath)
                ? string.Empty
                : binding.ClassPath.Trim().Replace('\\', '/').Trim('/');
            if (!string.IsNullOrEmpty(classPath)
                && (classPath.Contains(":") || classPath.Contains("//")
                    || classPath.Split('/').Any(segment => string.IsNullOrEmpty(segment)
                        || segment == "." || segment == ".."
                        || segment.EndsWith("~", StringComparison.Ordinal))))
            {
                error = $"EUIBinding 类路径无法安全解析：{binding.ClassPath}";
                return false;
            }

            var prefix = string.IsNullOrEmpty(classPath)
                ? businessCodeRoot + "/" + className
                : businessCodeRoot + "/" + classPath + "/" + className;
            foreach (var path in new[]
                     {
                         prefix + ".cs",
                         prefix + ".Binding.cs",
                         prefix + "Settings.cs",
                     })
            {
                if (!EUIPrefabCatalogService.IsSafeAssetPath(path)
                    || !IsUnderRoot(path, businessCodeRoot))
                {
                    error = $"EUIBinding 脚本路径超出业务代码根目录：{path}";
                    return false;
                }
                paths.Add(path);
            }
            return true;
        }

        private static bool TryValidateExactPageDefAcrossFiles(
            EUIPrefabCatalogSnapshot snapshot, string expectedFile, string pageName,
            string prefabPath, out string error)
        {
            error = null;
            var matchingFiles = new List<string>();
            var total = 0;
            foreach (var file in new[] { snapshot.UserPageDefFile, snapshot.FrameworkPageDefFile })
            {
                if (string.IsNullOrEmpty(file)) continue;
                var fullPath = EUIPrefabCatalogService.ToFullPath(file);
                try
                {
                    var content = File.ReadAllText(fullPath, Encoding.UTF8);
                    var count = EUIPrefabCatalogService.FindPageDefinitions(content)
                        .Count(match => string.Equals(match.Name, pageName,
                            StringComparison.Ordinal));
                    total += count;
                    for (var index = 0; index < count; index++) matchingFiles.Add(file);
                }
                catch (Exception exception)
                {
                    error = $"复核 PageDef 失败：{file}\n{exception.Message}";
                    return false;
                }
            }
            if (total != 1)
            {
                error = total == 0
                    ? $"当前已找不到 PageDef：{pageName}"
                    : $"当前 GamePages.cs 与 GamePages.User.cs 共有 {total} 个同名 PageDef，拒绝删除。";
                return false;
            }
            if (!string.Equals(matchingFiles[0], expectedFile, StringComparison.Ordinal))
            {
                error = $"PageDef 所在文件已变更，请重新扫描：{matchingFiles[0]}";
                return false;
            }
            return CanRemoveExactPageDef(expectedFile, pageName, prefabPath, out error);
        }

        private static bool ValidateSnapshotForDestructiveUse(
            EUIPrefabCatalogSnapshot snapshot, out string error)
        {
            error = null;
            if (snapshot == null)
            {
                error = "UI 目录快照为空。";
                return false;
            }
            if (string.IsNullOrEmpty(snapshot.UserPageDefFile))
            {
                error = "用户 PageDef 文件为空。";
                return false;
            }
            foreach (var pair in new[]
                     {
                         new KeyValuePair<string, string>("用户 PageDef", snapshot.UserPageDefFile),
                         new KeyValuePair<string, string>("框架 PageDef", snapshot.FrameworkPageDefFile),
                     }.Where(pair => !string.IsNullOrEmpty(pair.Value)))
            {
                var fullPath = EUIPrefabCatalogService.ToFullPath(pair.Value);
                if (!EUIPrefabCatalogService.IsSafeAssetPath(pair.Value)
                    || !pair.Value.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                {
                    error = $"{pair.Key} 不是 Assets/ 下已存在的规范文件：{pair.Value}";
                    return false;
                }
            }
            if (!EUIPrefabCatalogService.IsSafeAssetPath(snapshot.UIResourceRoot)
                || !AssetDatabase.IsValidFolder(snapshot.UIResourceRoot))
            {
                error = $"UI 资源根目录不是 Assets/ 下的有效目录：{snapshot.UIResourceRoot}";
                return false;
            }
            if (!EUIPrefabCatalogService.IsSafeAssetPath(snapshot.BusinessCodeRoot)
                || !AssetDatabase.IsValidFolder(snapshot.BusinessCodeRoot))
            {
                error = $"业务代码根目录不是 Assets/ 下的有效目录：{snapshot.BusinessCodeRoot}";
                return false;
            }
            return true;
        }

        private static bool IsSafePrefabAsset(string prefabPath)
        {
            prefabPath = EUIPrefabCatalogService.NormalizeAssetPath(prefabPath);
            if (!EUIPrefabCatalogService.IsSafeAssetPath(prefabPath)
                || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSafeMaintainedPrefabAsset(string prefabPath, out string error)
        {
            error = null;
            if (!IsSafePrefabAsset(prefabPath))
            {
                error = $"预制体路径不在允许的 Assets 范围：{prefabPath}";
                return false;
            }

            try
            {
                var settings = EUIBindingSettingData.LoadExistingSettings();
                var implementation = settings?.LogicImplementations?
                    .OfType<CSharpLogicImplementationData>()
                    .FirstOrDefault(item => item);
                var root = EUIPrefabCatalogService.NormalizeAssetPath(
                    implementation?.UIResourceRoot);
                if (!settings || !implementation
                    || !EUIPrefabCatalogService.IsSafeAssetPath(root)
                    || !AssetDatabase.IsValidFolder(root)
                    || !IsUnderRoot(prefabPath, root))
                {
                    error = $"预制体不在当前配置的 UI 资源根目录中：{prefabPath}";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"复核 UI 资源范围失败：{exception.Message}";
                return false;
            }
        }

        private static bool TryDeleteAsset(string assetPath, out string error)
        {
            error = null;
            if (!EUIPrefabCatalogService.IsSafeAssetPath(assetPath))
            {
                error = $"删除目标不在允许的 Assets 范围：{assetPath}";
                return false;
            }
            try
            {
                if (AssetDatabase.DeleteAsset(assetPath)) return true;
                error = $"删除失败：{assetPath}";
                return false;
            }
            catch (Exception exception)
            {
                error = $"删除失败：{assetPath}\n{exception.Message}";
                return false;
            }
        }
    }
}
