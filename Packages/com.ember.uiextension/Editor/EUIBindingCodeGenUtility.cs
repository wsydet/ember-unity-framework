// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;
using System.IO;

using Ember.Basic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding 代码生成工具（逻辑实现选择、路径预览、生成/重新生成、剪贴板、自动收集）。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingCodeGenUtility
    {
        #region 生命周期（初始化）

        static EUIBindingCodeGenUtility()
        {
            EUIBinding.OnIsOnPrefab = HandleIsOnPrefab;
            EUIBinding.OnGetCodeRootPath = HandleGetCodeRootPath;
            EUIBinding.OnGetLogicNames = HandleGetLogicNames;
            EUIBinding.OnGetGeneratedPath = HandleGetGeneratedPath;
            EUIBinding.OnHasGeneratedFile = HandleHasGeneratedFile;
            EUIBinding.OnGetGeneratedScript = HandleGetGeneratedScript;
            EUIBinding.OnGenerateCode = HandleGenerateCode;
            EUIBinding.OnGenerateToClipboard = HandleGenerateToClipboard;
            EUIBinding.OnAutoCollectBindings = HandleAutoCollectBindings;
            EUIBinding.OnClearAndRecollect = HandleClearAndRecollect;
            EUIBinding.OnClearAllBindings = HandleClearAllBindings;
            EUIBinding.OnCopyGeneratedPath = HandleCopyGeneratedPath;
            EUIBinding.OnOpenCodeGenSettings = HandleOpenSettings;
            EUIBinding.OnShowLogicMenu = HandleShowLogicMenu;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 数据提供

        private static bool HandleIsOnPrefab(EUIBinding binding)
        {
            if (!binding) return true;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
            if (!string.IsNullOrEmpty(path)) return true;
            // 也可能直接在 Prefab Stage 中编辑
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.IsPartOfPrefabContents(binding.gameObject);
        }

        private static string HandleGetCodeRootPath(EUIBinding.CodePathMode mode)
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            return mode == EUIBinding.CodePathMode.Framework
                ? settings.FrameworkCodeRoot
                : settings.BusinessCodeRoot;
        }

        private static string[] HandleGetLogicNames()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null
                || settings.LogicImplementations.Length == 0)
                return new[] { "（未配置）" };

            var names = new string[settings.LogicImplementations.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = settings.LogicImplementations[i]
                    ? settings.LogicImplementations[i].name
                    : "（缺失）";
            return names;
        }

        private static string HandleGetGeneratedPath(EUIBinding binding)
        {
            if (!binding) return "—";

            var logic = GetCurrentLogic(binding);
            if (!logic) return "（无逻辑实现）";

            if (string.IsNullOrEmpty(binding.ClassName))
                return "（请先填写类名）";

            // 优先使用 binding 的路径模式根目录，回退到逻辑实现的 codePath
            var root = !string.IsNullOrEmpty(binding.CodePath)
                ? binding.CodePath
                : GetLogicCodePath(logic);

            if (string.IsNullOrEmpty(root))
                return "（请先在 Project Settings 中配置代码生成路径）";

            var subDir = string.IsNullOrEmpty(binding.ClassPath)
                ? binding.ClassName
                : binding.ClassPath + "/" + binding.ClassName;

            return $"{root}/{subDir}/{binding.ClassName}{logic.CodeFileExtension}";
        }

        private static string GetLogicCodePath(LogicImplementationData logic)
        {
            // logic.GetCodeFilePath("X") = "{codePath}/X.cs"
            // Strip "X.cs" to get the root
            var sample = logic.GetCodeFilePath("__ember_tmp__");
            return System.IO.Path.GetDirectoryName(sample)?.Replace("\\", "/") ?? "Assets";
        }

        private static bool HandleHasGeneratedFile(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            return !string.IsNullOrEmpty(path) && path != "—"
                && File.Exists(GetFullPath(path));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 代码生成

        private static void HandleGenerateCode(EUIBinding binding)
        {
            if (!binding) return;

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("生成代码失败",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            if (!logic.CanGenerate(binding))
            {
                EditorUtility.DisplayDialog("生成代码失败",
                    "配置不完整。请检查类名、页面名等字段。", "确定");
                return;
            }

            // 如果不在预制体上，先生成预制体
            if (!HandleIsOnPrefab(binding))
            {
                GeneratePrefab(binding);
            }

            // 生成 PageDef（如果是 Page）
            if (binding.IsPage)
            {
                var csharp = logic as CSharpLogicImplementationData;
                if (csharp)
                    csharp.GenerateOrUpdatePageDefinition(binding);
            }

            // 计算基类信息
            string baseClsName = null;
            EUIBinding.BindingEntry[] declaredFields = null;
            var baseBinding = GetBaseBinding(binding);
            if (baseBinding)
            {
                baseClsName = baseBinding.ClassName;
                declaredFields = GetDeclaredFields(binding, baseBinding);
            }

            logic.GenerateCode(binding, baseClsName, declaredFields);

            EmberDebug.Log("EmberUI", $"代码生成完成：{binding.ClassName}");
        }

        /// <summary>
        /// 生成预制体到 {根目录}/Prefabs/{类名}.prefab
        /// </summary>
        private static void GeneratePrefab(EUIBinding binding)
        {
            var root = binding.CodePath;
            if (string.IsNullOrEmpty(root))
            {
                EditorUtility.DisplayDialog("生成预制体失败",
                    "未配置代码生成路径。请先在 Project Settings 中配置。", "确定");
                return;
            }

            var prefabDir = $"{root}/Prefabs";
            if (!Directory.Exists(prefabDir))
                Directory.CreateDirectory(prefabDir);

            var prefabPath = $"{prefabDir}/{binding.ClassName}.prefab";
            var go = binding.gameObject;

            // 如果已存在同路径预制体，询问是否覆盖
            if (File.Exists(prefabPath))
            {
                var overwrite = EditorUtility.DisplayDialog("预制体已存在",
                    $"\"{prefabPath}\" 已存在，是否覆盖？", "覆盖", "取消");
                if (!overwrite) return;
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.UserAction);
            EmberDebug.Log("EmberUI", $"预制体已生成：{prefabPath}");
        }

        private static void HandleGenerateToClipboard(EUIBinding binding)
        {
            if (!binding) return;

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("错误",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            if (!logic.CanGenerateForNoGen(binding))
            {
                EditorUtility.DisplayDialog("错误",
                    "当前逻辑实现不支持剪贴板生成。", "确定");
                return;
            }

            logic.GenerateCodeForNoGen(binding,
                string.IsNullOrEmpty(binding.ClassName)
                    ? binding.gameObject.name
                    : binding.ClassName);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 自动收集

        private static void HandleAutoCollectBindings(EUIBinding binding)
        {
            if (!binding) return;

            if (!EditorUtility.DisplayDialog("自动收集子控件",
                "将扫描当前节点的所有子节点，自动添加绑定条目。\n已有绑定不会被覆盖。",
                "开始收集", "取消"))
                return;

            var logic = GetCurrentLogic(binding);
            if (!logic)
            {
                EditorUtility.DisplayDialog("自动收集失败",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            // 收集已有绑定名
            var defined = new Dictionary<GameObject, GameObject>();
            EUIBindingEditorUtility.GatherBindingDefinitions(binding, defined);
            var definedNames = new HashSet<string>();
            if (binding.Bindings != null)
                foreach (var b in binding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name)) definedNames.Add(b.Name);

            // 直接修改 C# 对象，不通过 SerializedObject（避免 Odin 属性树缓存失效）
            var list = new List<EUIBinding.BindingEntry>(
                binding.Bindings ?? System.Array.Empty<EUIBinding.BindingEntry>());
            CollectBindingsToList(binding, defined, definedNames, binding.transform, logic, list);

            // 写入 private 字段 + Undo 强制 Unity 序列化系统感知变更
            Undo.RecordObject(binding, "自动收集子控件");
            var field = typeof(EUIBinding).GetField("bindings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(binding, list.ToArray());
            EditorUtility.SetDirty(binding);

            // 强制刷新 Odin：操作 SerializedObject 让 Unity 知道数据变了
            var forceSo = new SerializedObject(binding);
            forceSo.Update();
            forceSo.ApplyModifiedPropertiesWithoutUndo();
            forceSo.Dispose();

            EmberDebug.Log("EmberUI",
                $"自动收集完成，共 {list.Count} 个绑定（field check: {(binding.Bindings?.Length ?? 0)}）");
        }

        private static void CollectBindingsToList(
            EUIBinding binding,
            Dictionary<GameObject, GameObject> defined,
            HashSet<string> definedNames,
            Transform parent,
            LogicImplementationData logic,
            List<EUIBinding.BindingEntry> list)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childGO = child.gameObject;

                // 跳过被 EUIBindingExclude 标记的节点及其子树
                if (childGO.GetComponent<EUIBindingExclude>())
                    continue;

                bool hasChildBinding = childGO.GetComponent<EUIBinding>() != null;

                if (!defined.ContainsKey(childGO) && IsNameSuitable(child.name))
                {
                    list.Add(new EUIBinding.BindingEntry
                    {
                        Name = logic.GetNameForCode(child.name, definedNames),
                        GameObject = childGO,
                        Type = DetectWidgetType(childGO),
                        ClassName = null,
                    });
                }

                if (!hasChildBinding)
                    CollectBindingsToList(binding, defined, definedNames, child, logic, list);
            }
        }

        /// <summary>检测 GameObject 上的组件类型</summary>
        private static EUIBinding.WidgetTypes DetectWidgetType(GameObject go)
        {
            if (!go) return EUIBinding.WidgetTypes.Component;

            var binding = go.GetComponent<EUIBinding>();
            if (binding) return EUIBinding.WidgetTypes.UILogic;

            foreach (var rule in EUIBindingEditorUtility.AutoSelectExactBuiltInComponentTypeRules)
            {
                if (rule.ExactMatches(go))
                    return rule.WidgetType;
            }

            foreach (var rule in EUIBindingEditorUtility.BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == EUIBinding.WidgetTypes.UILogic) continue;
                if (rule.Matches(go))
                    return rule.WidgetType;
            }

            return EUIBinding.WidgetTypes.Component;
        }

        /// <summary>判断节点名是否适合作为绑定（收集所有非子 Binding 节点的子节点）</summary>
        private static bool IsNameSuitable(string name)
        {
            // 收集所有子节点，过滤掉纯数字/特殊名称
            return !string.IsNullOrEmpty(name) && !name.StartsWith("_");
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 辅助

        private static void HandleShowLogicMenu(EUIBinding binding)
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            var impls = settings.LogicImplementations;
            if (impls == null || impls.Length == 0)
            {
                EditorUtility.DisplayDialog("提示",
                    "未配置逻辑实现。请点击 ⚙ 按钮进入 Project Settings 添加。", "确定");
                return;
            }

            var menu = new GenericMenu();
            for (int i = 0; i < impls.Length; i++)
            {
                if (!impls[i]) continue;
                int idx = i;
                menu.AddItem(new GUIContent(impls[i].name),
                    idx == EUIBinding.CodeGenLogicIndex,
                    () => EUIBinding.CodeGenLogicIndex = idx);
            }
            menu.ShowAsContext();
        }

        private static void HandleOpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/EUI Binding");
        }

        private static UnityEngine.Object HandleGetGeneratedScript(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return null;
            var fullPath = GetFullPath(path);
            return !string.IsNullOrEmpty(fullPath) && File.Exists(fullPath)
                ? AssetDatabase.LoadAssetAtPath<MonoScript>(path)
                : null;
        }

        private static void HandleClearAndRecollect(EUIBinding binding)
        {
            if (!binding) return;
            if (!EditorUtility.DisplayDialog("清除并重新收集",
                "将清除所有现有绑定并重新扫描子节点进行收集，是否继续？",
                "确认清除并收集", "取消"))
                return;

            // 清除：直接写 C# 对象
            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, System.Array.Empty<EUIBinding.BindingEntry>());
            EmberDebug.Log("EmberUI", "清除完成：→ 0");

            // 重新收集
            HandleAutoCollectBindings(binding);
        }

        private static void HandleClearAllBindings(EUIBinding binding)
        {
            if (!binding) return;
            if (!EditorUtility.DisplayDialog("清除所有绑定",
                "确认清除当前所有的控件绑定条目？此操作不可撤销。",
                "确认清除", "取消"))
                return;

            // 直接写 C# 对象
            typeof(EUIBinding)
                .GetField("bindings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(binding, System.Array.Empty<EUIBinding.BindingEntry>());
            EditorUtility.SetDirty(binding);
            EmberDebug.Log("EmberUI", "已清除所有绑定");
        }

        private static void HandleCopyGeneratedPath(EUIBinding binding)
        {
            var path = HandleGetGeneratedPath(binding);
            if (string.IsNullOrEmpty(path) || path == "—") return;
            GUIUtility.systemCopyBuffer = path;
            EmberDebug.Log("EmberUI", $"已复制路径：{path}");
        }

        private static LogicImplementationData GetCurrentLogic(EUIBinding binding)
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null
                || settings.LogicImplementations.Length == 0)
                return null;

            int index = EUIBinding.CodeGenLogicIndex;
            if (index < 0 || index >= settings.LogicImplementations.Length)
                index = 0;

            return settings.LogicImplementations[index];
        }

        private static EUIBinding GetBaseBinding(EUIBinding binding)
        {
            if (!binding) return null;

            string guid;
            using (var so = new SerializedObject(binding))
                guid = so.FindProperty("baseBindingUUID").stringValue;

            if (string.IsNullOrEmpty(guid)) return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab ? prefab.GetComponent<EUIBinding>() : null;
        }

        /// <summary>计算需要声明的字段（排除基类已有字段）</summary>
        private static EUIBinding.BindingEntry[] GetDeclaredFields(
            EUIBinding binding, EUIBinding baseBinding)
        {
            var baseNames = new HashSet<string>();
            if (baseBinding.Bindings != null)
                foreach (var b in baseBinding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name))
                        baseNames.Add(b.Name);

            var declared = new List<EUIBinding.BindingEntry>();
            if (binding.Bindings != null)
                foreach (var b in binding.Bindings)
                    if (!string.IsNullOrEmpty(b.Name) && !baseNames.Contains(b.Name))
                        declared.Add(b);

            return declared.ToArray();
        }

        private static string GetFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            return assetPath.StartsWith("Assets/")
                ? Path.Combine(Application.dataPath,
                    assetPath.Substring("Assets/".Length))
                : assetPath;
        }

        #endregion
    }
}
