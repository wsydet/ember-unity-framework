//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using System.IO;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public sealed class UIPrefabValidationReport
//    {
//        public string AssetPath;
//        public List<GameUIBindingValidationIssue> Issues = new List<GameUIBindingValidationIssue>();
//
//        public bool HasError
//        {
//            get
//            {
//                foreach (var issue in Issues)
//                {
//                    if (issue.Severity == GameUIBindingIssueSeverity.Error)
//                    {
//                        return true;
//                    }
//                }
//
//                return false;
//            }
//        }
//
//        public void AddIssue(GameUIBindingIssueSeverity severity, string bindingPath, string message, string suggestion = null)
//        {
//            Issues.Add(new GameUIBindingValidationIssue
//            {
//                Severity = severity,
//                BindingPath = bindingPath,
//                Message = message,
//                Suggestion = suggestion,
//            });
//        }
//
//        public void AddBindingResult(GameUIBindingValidationResult bindingResult)
//        {
//            if (bindingResult == null)
//            {
//                return;
//            }
//
//            Issues.AddRange(bindingResult.Issues);
//        }
//    }
//
//    public static class UIPrefabValidationUtility
//    {
//        private static readonly Vector2 StandardReferenceResolution = new Vector2(720, 1560);
//
//        public static UIPrefabValidationReport ValidatePrefabAsset(string assetPath)
//        {
//            var report = new UIPrefabValidationReport { AssetPath = assetPath };
//            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
//            if (!prefab)
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, $"无法加载 prefab：{assetPath}");
//                return report;
//            }
//
//            ValidatePrefab(prefab, report);
//            return report;
//        }
//
//        public static UIPrefabValidationReport ValidatePrefab(GameObject prefab)
//        {
//            var report = new UIPrefabValidationReport { AssetPath = AssetDatabase.GetAssetPath(prefab) };
//            ValidatePrefab(prefab, report);
//            return report;
//        }
//
//        public static List<UIPrefabValidationReport> ValidateDirectory(string directory)
//        {
//            var reports = new List<UIPrefabValidationReport>();
//            if (string.IsNullOrEmpty(directory) || !AssetDatabase.IsValidFolder(directory))
//            {
//                return reports;
//            }
//
//            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { directory });
//            foreach (var guid in guids)
//            {
//                var path = AssetDatabase.GUIDToAssetPath(guid);
//                reports.Add(ValidatePrefabAsset(path));
//            }
//
//            return reports;
//        }
//
//        [MenuItem("Burner/Burner UI/Validation/检查选中 UI Prefab")]
//        private static void ValidateSelection()
//        {
//            var reports = new List<UIPrefabValidationReport>();
//            foreach (var obj in Selection.objects)
//            {
//                var path = AssetDatabase.GetAssetPath(obj);
//                if (string.IsNullOrEmpty(path))
//                {
//                    continue;
//                }
//
//                if (AssetDatabase.IsValidFolder(path))
//                {
//                    reports.AddRange(ValidateDirectory(path));
//                }
//                else if (Path.GetExtension(path) == ".prefab")
//                {
//                    reports.Add(ValidatePrefabAsset(path));
//                }
//            }
//
//            LogReports(reports);
//            EditorUtility.DisplayDialog("UI Prefab 校验", $"检查完成：{reports.Count} 个 prefab，错误 {CountIssues(reports, GameUIBindingIssueSeverity.Error)} 个，警告 {CountIssues(reports, GameUIBindingIssueSeverity.Warning)} 个。", "确定");
//        }
//
//        private static void ValidatePrefab(GameObject prefab, UIPrefabValidationReport report)
//        {
//            if (!prefab)
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, "Prefab 为空。");
//                return;
//            }
//
//            ValidateRoot(prefab, report);
//            var binding = prefab.GetComponent<GameUIBinding>();
//            if (binding)
//            {
//                report.AddBindingResult(GameUIBindingEditorUtility.ValidateBinding(binding));
//                ValidatePageDef(binding, report);
//            }
//
//            ValidateSafeArea(prefab, report);
//            ValidateMissingReferences(prefab, report);
//            ValidateRendererMaterials(prefab, report);
//            ValidateEmptyLeafNodes(prefab, report);
//        }
//
//        private static void ValidateRoot(GameObject prefab, UIPrefabValidationReport report)
//        {
//            var canvas = prefab.GetComponent<Canvas>();
//            if (!canvas)
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, "UI prefab 根节点缺少 Canvas。");
//            }
//
//            var scaler = prefab.GetComponent<CanvasScaler>();
//            if (!scaler)
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, "UI prefab 根节点缺少 CanvasScaler。");
//            }
//            else
//            {
//                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Error, null, "CanvasScaler.uiScaleMode 不是 ScaleWithScreenSize。");
//                }
//
//                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Warning, null, "CanvasScaler.screenMatchMode 不是 Expand。", "新页面默认使用 Expand；确有特殊适配需求时需在评审中说明。");
//                }
//
//                if (scaler.referenceResolution != StandardReferenceResolution)
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Warning, null, $"CanvasScaler.referenceResolution 是 {scaler.referenceResolution}，标准值为 {StandardReferenceResolution}。");
//                }
//            }
//
//            if (!prefab.GetComponent<GraphicRaycaster>())
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, "UI prefab 根节点缺少 GraphicRaycaster。");
//            }
//
//            if (!prefab.GetComponent<GameUIBinding>())
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, "UI prefab 根节点缺少 GameUIBinding。");
//            }
//        }
//
//        private static void ValidatePageDef(GameUIBinding binding, UIPrefabValidationReport report)
//        {
//            if (!binding.IsPage || string.IsNullOrEmpty(binding.PageName))
//            {
//                return;
//            }
//
//            var implementation = FindCSharpLogicImplementationData();
//            if (!implementation || string.IsNullOrEmpty(implementation.PageDefFile))
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Warning, null, "未配置 CSharpLogicImplementationData.pageDefFile，无法校验 PageDef。");
//                return;
//            }
//
//            if (!File.Exists(implementation.PageDefFile))
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, $"PageDef 文件不存在：{implementation.PageDefFile}");
//                return;
//            }
//
//            if (!LogicImplementationData.TryGetPrefabName(binding, out var prefabName) || string.IsNullOrEmpty(prefabName))
//            {
//                return;
//            }
//
//            prefabName = Path.GetFileNameWithoutExtension(prefabName).ToLowerInvariant();
//            var content = File.ReadAllText(implementation.PageDefFile, new System.Text.UTF8Encoding(false));
//            if (!content.Contains($"public const string {binding.PageName}"))
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, $"PageDef 缺少常量：{binding.PageName}。", "通过 GameUIBindingEditorUtility.GenerateOrUpdatePageDef 或代码生成工具重新生成。");
//            }
//
//            if (!content.Contains($"\"{prefabName}\""))
//            {
//                report.AddIssue(GameUIBindingIssueSeverity.Error, null, $"PageDef 缺少 prefab 地址：{prefabName}。", "确认 prefab 文件名、pageName 和 PageDef 生成结果一致。");
//            }
//        }
//
//        private static void ValidateSafeArea(GameObject prefab, UIPrefabValidationReport report)
//        {
//            var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
//            foreach (var behaviour in behaviours)
//            {
//                if (!behaviour)
//                {
//                    continue;
//                }
//
//                var typeName = behaviour.GetType().FullName;
//                if (typeName == "E7.NotchSolution.SafePadding")
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Warning, GetTransformPath(prefab.transform, behaviour.transform), "仍使用旧 SafePadding 组件。", "迁移到 BurnerSafeArea 后删除第三方 SafeArea 依赖。");
//                }
//                else if (typeName == "Jagapippi.AutoScreen.SafeArea")
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Error, GetTransformPath(prefab.transform, behaviour.transform), "仍使用 AutoScreen SafeArea 组件。", "项目 UI 不再接入 com.jagapippi.auto-screen。");
//                }
//            }
//        }
//
//        private static void ValidateMissingReferences(GameObject prefab, UIPrefabValidationReport report)
//        {
//            var components = prefab.GetComponentsInChildren<Component>(true);
//            foreach (var component in components)
//            {
//                if (!component)
//                {
//                    report.AddIssue(GameUIBindingIssueSeverity.Error, null, "Prefab 中存在 Missing Script。");
//                    continue;
//                }
//
//                SerializedObject serializedObject;
//                try
//                {
//                    serializedObject = new SerializedObject(component);
//                }
//                catch (System.Exception)
//                {
//                    continue;
//                }
//
//                var property = serializedObject.GetIterator();
//                while (property.NextVisible(true))
//                {
//                    if (property.propertyType != SerializedPropertyType.ObjectReference)
//                    {
//                        continue;
//                    }
//
//                    if (property.objectReferenceInstanceIDValue != 0 && property.objectReferenceValue == null)
//                    {
//                        report.AddIssue(
//                            GameUIBindingIssueSeverity.Error,
//                            GetTransformPath(prefab.transform, component.transform),
//                            $"{component.GetType().Name}.{property.displayName} 存在丢失引用。");
//                        break;
//                    }
//                }
//            }
//        }
//
//        private static void ValidateRendererMaterials(GameObject prefab, UIPrefabValidationReport report)
//        {
//            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
//            foreach (var renderer in renderers)
//            {
//                var materials = renderer.sharedMaterials;
//                for (var i = 0; i < materials.Length; i++)
//                {
//                    if (materials[i] != null)
//                    {
//                        continue;
//                    }
//
//                    report.AddIssue(
//                        GameUIBindingIssueSeverity.Warning,
//                        GetTransformPath(prefab.transform, renderer.transform),
//                        $"{renderer.GetType().Name} 的第 {i} 个材质为空。");
//                    break;
//                }
//            }
//        }
//
//        private static void ValidateEmptyLeafNodes(GameObject prefab, UIPrefabValidationReport report)
//        {
//            var transforms = prefab.GetComponentsInChildren<Transform>(true);
//            foreach (var transform in transforms)
//            {
//                if (transform == prefab.transform || transform.childCount > 0)
//                {
//                    continue;
//                }
//
//                var components = transform.GetComponents<Component>();
//                if (components.Length == 1 && components[0] is Transform)
//                {
//                    report.AddIssue(
//                        GameUIBindingIssueSeverity.Warning,
//                        GetTransformPath(prefab.transform, transform),
//                        "存在只有 Transform 的空叶子节点。",
//                        "确认是否为占位节点；如无用途建议删除。");
//                }
//            }
//        }
//
//        private static CSharpLogicImplementationData FindCSharpLogicImplementationData()
//        {
//            var settings = UIBindingSettingData.GetOrCreateSettings();
//            if (settings.LogicImplementations == null)
//            {
//                return null;
//            }
//
//            foreach (var implementation in settings.LogicImplementations)
//            {
//                if (implementation is CSharpLogicImplementationData csharpImplementation)
//                {
//                    return csharpImplementation;
//                }
//            }
//
//            return null;
//        }
//
//        private static string GetTransformPath(Transform root, Transform target)
//        {
//            if (!root || !target)
//            {
//                return null;
//            }
//
//            if (root == target)
//            {
//                return string.Empty;
//            }
//
//            var names = new Stack<string>();
//            var current = target;
//            while (current && current != root)
//            {
//                names.Push(current.name);
//                current = current.parent;
//            }
//
//            return current == root ? string.Join("/", names.ToArray()) : null;
//        }
//
//        private static void LogReports(IList<UIPrefabValidationReport> reports)
//        {
//            foreach (var report in reports)
//            {
//                foreach (var issue in report.Issues)
//                {
//                    var message = $"[{issue.Severity}] {report.AssetPath} {issue.BindingPath} {issue.Message}";
//                    if (issue.Severity == GameUIBindingIssueSeverity.Error)
//                    {
//                        Debug.LogError(message);
//                    }
//                    else if (issue.Severity == GameUIBindingIssueSeverity.Warning)
//                    {
//                        Debug.LogWarning(message);
//                    }
//                    else
//                    {
//                        Debug.Log(message);
//                    }
//                }
//            }
//        }
//
//        private static int CountIssues(IList<UIPrefabValidationReport> reports, GameUIBindingIssueSeverity severity)
//        {
//            var count = 0;
//            foreach (var report in reports)
//            {
//                foreach (var issue in report.Issues)
//                {
//                    if (issue.Severity == severity)
//                    {
//                        count++;
//                    }
//                }
//            }
//
//            return count;
//        }
//    }
//}
