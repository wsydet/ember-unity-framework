////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System;
////using System.Collections.Generic;
////using System.IO;
////using UnityEditor;
////using UnityEngine;
////using UnityEngine.UI;
////
////namespace Burner.UIExtension
////{
////    public sealed class GameUIBindingSnapshot
////    {
////        public string PrefabAssetPath;
////        public string PrefabName;
////        public string PageName;
////        public string ClassPath;
////        public string ClassName;
////        public bool IsPage;
////        public PageFlags PageFlags;
////        public GameUIBinding.WidgetTypes SelfWidgetType;
////        public string SelfWidgetClassName;
////        public bool NoCodeGen;
////        public string BaseBindingPrefabPath;
////        public string BaseBindingGuid;
////        public List<GameUIBindingEntrySnapshot> Entries = new List<GameUIBindingEntrySnapshot>();
////    }
////
////    public sealed class GameUIBindingEntrySnapshot
////    {
////        public string Name;
////        public string GameObjectPath;
////        public GameUIBinding.WidgetTypes WidgetType;
////        public string ClassName;
////    }
////
////    public enum GameUIBindingIssueSeverity
////    {
////        Info,
////        Warning,
////        Error,
////    }
////
////    public sealed class GameUIBindingValidationIssue
////    {
////        public GameUIBindingIssueSeverity Severity;
////        public string BindingPath;
////        public string Message;
////        public string Suggestion;
////        public string EnglishMessageFormat;
////        public object[] EnglishMessageArguments;
////    }
////
////    public sealed class GameUIBindingValidationResult
////    {
////        public string AssetPath;
////        public List<GameUIBindingValidationIssue> Issues = new List<GameUIBindingValidationIssue>();
////
////        public bool HasError
////        {
////            get
////            {
////                foreach (var issue in Issues)
////                {
////                    if (issue.Severity == GameUIBindingIssueSeverity.Error)
////                    {
////                        return true;
////                    }
////                }
////
////                return false;
////            }
////        }
////
////        public void AddIssue(GameUIBindingIssueSeverity severity, string bindingPath, string message, string suggestion = null)
////        {
////            Issues.Add(new GameUIBindingValidationIssue
////            {
////                Severity = severity,
////                BindingPath = bindingPath,
////                Message = message,
////                Suggestion = suggestion,
////                EnglishMessageFormat = message,
////            });
////        }
////
////        public void AddIssueWithEnglishMessage(
////            GameUIBindingIssueSeverity severity,
////            string bindingPath,
////            string englishMessageFormat,
////            object[] englishMessageArguments,
////            string message,
////            string suggestion = null)
////        {
////            Issues.Add(new GameUIBindingValidationIssue
////            {
////                Severity = severity,
////                BindingPath = bindingPath,
////                Message = message,
////                Suggestion = suggestion,
////                EnglishMessageFormat = englishMessageFormat,
////                EnglishMessageArguments = englishMessageArguments,
////            });
////        }
////    }
////
////    public static class GameUIBindingEditorUtility
////    {
////        private const string PageNameProperty = "pageName";
////        private const string ClassPathProperty = "classPath";
////        private const string ClassNameProperty = "className";
////        private const string IsPageProperty = "isPage";
////        private const string SelfWidgetTypeProperty = "selfWidgetType";
////        private const string SelfWidgetClassNameProperty = "selfWidgetClassName";
////        private const string BindingsProperty = "bindings";
////        private const string NoCodeGenProperty = "noCodeGen";
////        private const string BaseBindingGuidProperty = "baseBindingUUID";
////        private const string PageFlagsProperty = "pageFlags";
////
////        public static GameUIBindingSnapshot GetBindingSnapshot(GameUIBinding binding)
////        {
////            if (!binding)
////            {
////                throw new ArgumentNullException(nameof(binding));
////            }
////
////            var assetPath = GetPrefabAssetPath(binding);
////            var snapshot = new GameUIBindingSnapshot
////            {
////                PrefabAssetPath = assetPath,
////                PrefabName = string.IsNullOrEmpty(assetPath) ? binding.gameObject.name : Path.GetFileNameWithoutExtension(assetPath),
////                PageName = binding.PageName,
////                ClassPath = binding.ClassPath,
////                ClassName = binding.ClassName,
////                IsPage = binding.IsPage,
////                PageFlags = binding.PageFlags,
////                SelfWidgetType = binding.SelfWidgetType,
////                SelfWidgetClassName = binding.SelfWidgetClassName,
////                NoCodeGen = binding.NoCodeGeneration,
////                BaseBindingGuid = GetBaseBindingGuid(binding),
////            };
////
////            if (!string.IsNullOrEmpty(snapshot.BaseBindingGuid))
////            {
////                snapshot.BaseBindingPrefabPath = AssetDatabase.GUIDToAssetPath(snapshot.BaseBindingGuid);
////            }
////
////            if (binding.Bindings != null)
////            {
////                foreach (var entry in binding.Bindings)
////                {
////                    snapshot.Entries.Add(new GameUIBindingEntrySnapshot
////                    {
////                        Name = entry.Name,
////                        GameObjectPath = GetTransformPath(binding.transform, entry.GameObject ? entry.GameObject.transform : null),
////                        WidgetType = entry.Type,
////                        ClassName = entry.ClassName,
////                    });
////                }
////            }
////
////            return snapshot;
////        }
////
////        public static void ApplyBindingSnapshot(GameUIBinding binding, GameUIBindingSnapshot snapshot)
////        {
////            if (!binding)
////            {
////                throw new ArgumentNullException(nameof(binding));
////            }
////
////            if (snapshot == null)
////            {
////                throw new ArgumentNullException(nameof(snapshot));
////            }
////
////            SetPageInfo(binding, snapshot.PageName, snapshot.ClassPath, snapshot.ClassName, snapshot.IsPage, snapshot.PageFlags, snapshot.NoCodeGen);
////            SetSelfWidget(binding, snapshot.SelfWidgetType, snapshot.SelfWidgetClassName);
////            SetBaseBinding(binding, snapshot.BaseBindingGuid);
////            SetBindings(binding, snapshot.Entries);
////        }
////
////        public static void SetPageInfo(
////            GameUIBinding binding,
////            string pageName,
////            string classPath,
////            string className,
////            bool isPage,
////            PageFlags pageFlags,
////            bool noCodeGen = false)
////        {
////            using (var so = CreateSerializedObject(binding))
////            {
////                so.FindProperty(PageNameProperty).stringValue = pageName;
////                so.FindProperty(ClassPathProperty).stringValue = classPath;
////                so.FindProperty(ClassNameProperty).stringValue = className;
////                so.FindProperty(IsPageProperty).boolValue = isPage;
////                so.FindProperty(NoCodeGenProperty).boolValue = noCodeGen;
////                so.FindProperty(PageFlagsProperty).enumValueFlag = (int)pageFlags;
////                so.ApplyModifiedProperties();
////            }
////        }
////
////        public static void SetSelfWidget(GameUIBinding binding, GameUIBinding.WidgetTypes widgetType, string widgetClassName = null)
////        {
////            using (var so = CreateSerializedObject(binding))
////            {
////                SetWidgetType(so.FindProperty(SelfWidgetTypeProperty), widgetType);
////                so.FindProperty(SelfWidgetClassNameProperty).stringValue = widgetClassName;
////                so.ApplyModifiedProperties();
////            }
////        }
////
////        public static void SetBindings(GameUIBinding binding, IList<GameUIBindingEntrySnapshot> entries)
////        {
////            using (var so = CreateSerializedObject(binding))
////            {
////                var bindings = so.FindProperty(BindingsProperty);
////                bindings.ClearArray();
////
////                if (entries != null)
////                {
////                    for (var i = 0; i < entries.Count; i++)
////                    {
////                        var entry = entries[i];
////                        bindings.InsertArrayElementAtIndex(i);
////                        WriteBindingEntry(binding, bindings.GetArrayElementAtIndex(i), entry);
////                    }
////                }
////
////                so.ApplyModifiedProperties();
////            }
////        }
////
////        public static void SetBaseBinding(GameUIBinding binding, GameObject basePrefab)
////        {
////            var guid = string.Empty;
////            if (basePrefab)
////            {
////                var path = AssetDatabase.GetAssetPath(basePrefab);
////                guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
////            }
////
////            SetBaseBinding(binding, guid);
////        }
////
////        public static void SetBaseBinding(GameUIBinding binding, string baseBindingGuid)
////        {
////            using (var so = CreateSerializedObject(binding))
////            {
////                so.FindProperty(BaseBindingGuidProperty).stringValue = baseBindingGuid;
////                so.ApplyModifiedProperties();
////            }
////        }
////
////        public static string GetBaseBindingGuid(GameUIBinding binding)
////        {
////            using (var so = CreateSerializedObject(binding))
////            {
////                return so.FindProperty(BaseBindingGuidProperty).stringValue;
////            }
////        }
////
////        public static List<GameUIBindingEntrySnapshot> CollectBindings(GameUIBinding binding, bool clearExisting = false)
////        {
////            if (!binding)
////            {
////                throw new ArgumentNullException(nameof(binding));
////            }
////
////            using (var so = CreateSerializedObject(binding))
////            {
////                var bindings = so.FindProperty(BindingsProperty);
////                var definedObjects = new Dictionary<GameObject, GameObject>();
////                var definedNames = new HashSet<string>();
////
////                if (clearExisting)
////                {
////                    bindings.ClearArray();
////                }
////                else
////                {
////                    GameUIBindingEditor.GatherBindingDefinitions(binding, definedObjects);
////                    if (binding.Bindings != null)
////                    {
////                        foreach (var entry in binding.Bindings)
////                        {
////                            if (!string.IsNullOrEmpty(entry.Name))
////                            {
////                                definedNames.Add(entry.Name);
////                            }
////                        }
////                    }
////                }
////
////                CollectBindingsRecursive(binding, bindings, definedObjects, definedNames, binding.transform);
////                so.ApplyModifiedProperties();
////            }
////
////            return GetBindingSnapshot(binding).Entries;
////        }
////
////        public static GameUIBindingValidationResult ValidateBinding(GameUIBinding binding)
////        {
////            var result = new GameUIBindingValidationResult();
////            if (!binding)
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "GameUIBinding is null.",
////                    null,
////                    "GameUIBinding 为空。");
////                return result;
////            }
////
////            result.AssetPath = GetPrefabAssetPath(binding);
////            ValidatePageInfo(binding, result);
////            ValidateEntries(binding, result);
////            ValidateBaseBinding(binding, result);
////            return result;
////        }
////
////        public static bool GenerateOrUpdatePageDef(GameUIBinding binding, CSharpLogicImplementationData implementationData = null)
////        {
////            if (!binding)
////            {
////                throw new ArgumentNullException(nameof(binding));
////            }
////
////            if (!binding.IsPage)
////            {
////                return true;
////            }
////
////            implementationData = implementationData ? implementationData : FindCSharpLogicImplementationData();
////            if (!implementationData)
////            {
////                Debug.LogError("未找到 CSharpLogicImplementationData，无法更新 EUIPageDef。请检查 Project Settings/Burner UI 中的逻辑实现数据。", binding);
////                return false;
////            }
////
////            return implementationData.GenerateOrUpdatePageDefinition(binding);
////        }
////
////        private static CSharpLogicImplementationData FindCSharpLogicImplementationData()
////        {
////            var settings = EUIBindingSettingData.GetOrCreateSettings();
////            if (settings.LogicImplementations == null)
////            {
////                return null;
////            }
////
////            foreach (var implementation in settings.LogicImplementations)
////            {
////                if (implementation is CSharpLogicImplementationData csharpImplementation)
////                {
////                    return csharpImplementation;
////                }
////            }
////
////            return null;
////        }
////
////        private static SerializedObject CreateSerializedObject(GameUIBinding binding)
////        {
////            if (!binding)
////            {
////                throw new ArgumentNullException(nameof(binding));
////            }
////
////            return new SerializedObject(binding);
////        }
////
////        private static void WriteBindingEntry(GameUIBinding binding, SerializedProperty property, GameUIBindingEntrySnapshot entry)
////        {
////            property.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name)).stringValue = entry?.Name;
////            property.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject)).objectReferenceValue =
////                ResolveGameObject(binding, entry?.GameObjectPath);
////            SetWidgetType(property.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type)), entry?.WidgetType ?? GameUIBinding.WidgetTypes.Component);
////            property.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName)).stringValue = entry?.ClassName;
////        }
////
////        private static void CollectBindingsRecursive(
////            GameUIBinding binding,
////            SerializedProperty bindings,
////            Dictionary<GameObject, GameObject> definedObjects,
////            HashSet<string> definedNames,
////            Transform transform)
////        {
////            for (var i = 0; i < transform.childCount; i++)
////            {
////                var child = transform.GetChild(i);
////                var childGo = child.gameObject;
////                var isSubBindingRoot = childGo.GetComponent<GameUIBinding>() || childGo.GetComponent<UIContainer>();
////
////                if (!definedObjects.ContainsKey(childGo) && IsBindingNameCandidate(child.name))
////                {
////                    var index = bindings.arraySize;
////                    bindings.InsertArrayElementAtIndex(index);
////                    var childProperty = bindings.GetArrayElementAtIndex(index);
////                    childProperty.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name)).stringValue =
////                        GetUniqueBindingName(child.name, definedNames);
////                    childProperty.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject)).objectReferenceValue = childGo;
////                    childProperty.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName)).stringValue = null;
////                    GameUIBindingEditor.AutoSelectByObject(
////                        childGo,
////                        childProperty.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type)),
////                        childProperty.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName)));
////                    definedObjects[childGo] = binding.gameObject;
////                }
////
////                if (!isSubBindingRoot)
////                {
////                    CollectBindingsRecursive(binding, bindings, definedObjects, definedNames, child);
////                }
////            }
////        }
////
////        private static void ValidatePageInfo(GameUIBinding binding, GameUIBindingValidationResult result)
////        {
////            if (string.IsNullOrEmpty(binding.ClassPath))
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "classPath is empty.",
////                    null,
////                    "classPath 为空。",
////                    "填写相对 Assets/Game/GameLogic/GameModule 的逻辑路径。");
////            }
////
////            if (string.IsNullOrEmpty(binding.ClassName))
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "className is empty.",
////                    null,
////                    "className 为空。",
////                    "填写页面或组件逻辑类名。");
////            }
////
////            if (!binding.IsPage)
////            {
////                return;
////            }
////
////            if (string.IsNullOrEmpty(binding.PageName))
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "The page binding pageName is empty.",
////                    null,
////                    "页面级 binding 的 pageName 为空。",
////                    "pageName 应作为 EUIPageDef 常量名。");
////            }
////
////            if (binding.PageFlags == PageFlags.None)
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "The page binding pageFlags is None.",
////                    null,
////                    "页面级 binding 的 pageFlags 为 None。",
////                    "选择 MainPage、Popup、TopMost、SubPage 或 FreePage。");
////            }
////
////            if (!LogicImplementationData.TryGetPrefabName(binding, out var prefabName) || string.IsNullOrEmpty(prefabName))
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "The page binding is not part of a Prefab asset.",
////                    null,
////                    "页面级 binding 不在 prefab 资产中。",
////                    "只有 prefab 根或 prefab stage 中的对象可以生成 EUIPageDef。");
////                return;
////            }
////
////            if (Path.HasExtension(prefabName))
////            {
////                prefabName = Path.GetFileNameWithoutExtension(prefabName);
////            }
////
////            if (!string.IsNullOrEmpty(prefabName) && prefabName != binding.gameObject.name)
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Warning,
////                    null,
////                    "Prefab file name {0} does not match root node name {1}.",
////                    new object[] { prefabName, binding.gameObject.name },
////                    $"prefab 文件名 {prefabName} 与根节点名 {binding.gameObject.name} 不一致。",
////                    "确认 EUIPageDef 值应来自 prefab 文件名的小写形式。");
////            }
////        }
////
////        private static void ValidateEntries(GameUIBinding binding, GameUIBindingValidationResult result)
////        {
////            var names = new HashSet<string>();
////            if (binding.Bindings == null)
////            {
////                return;
////            }
////
////            foreach (var entry in binding.Bindings)
////            {
////                var bindingPath = entry.GameObject ? GetTransformPath(binding.transform, entry.GameObject.transform) : null;
////                if (string.IsNullOrEmpty(entry.Name))
////                {
////                    result.AddIssueWithEnglishMessage(
////                        GameUIBindingIssueSeverity.Error,
////                        bindingPath,
////                        "The binding field name is empty.",
////                        null,
////                        "绑定字段名为空。",
////                        "重新收集绑定或手动补齐字段名。");
////                }
////                else if (!names.Add(entry.Name))
////                {
////                    result.AddIssueWithEnglishMessage(
////                        GameUIBindingIssueSeverity.Error,
////                        bindingPath,
////                        "Binding field name {0} is duplicated.",
////                        new object[] { entry.Name },
////                        $"绑定字段名重复：{entry.Name}。",
////                        "重命名其中一个绑定字段。");
////                }
////
////                if (!entry.GameObject)
////                {
////                    result.AddIssueWithEnglishMessage(
////                        GameUIBindingIssueSeverity.Error,
////                        bindingPath,
////                        "GameObject for binding {0} is missing.",
////                        new object[] { entry.Name },
////                        $"绑定 {entry.Name} 的 GameObject 丢失。",
////                        "重新指定节点或删除该绑定。");
////                    continue;
////                }
////
////                ValidateEntryType(entry, bindingPath, result);
////            }
////        }
////
////        private static void ValidateEntryType(GameUIBinding.BindingEntry entry, string bindingPath, GameUIBindingValidationResult result)
////        {
////            switch (entry.Type)
////            {
////                case GameUIBinding.WidgetTypes.Text:
////                    if (!entry.GameObject.GetComponent<Text>() && !entry.GameObject.GetComponent<TMPro.TextMeshProUGUI>())
////                    {
////                        result.AddIssueWithEnglishMessage(
////                            GameUIBindingIssueSeverity.Error,
////                            bindingPath,
////                            "Binding {0} is missing a Text component.",
////                            new object[] { entry.Name },
////                            $"绑定 {entry.Name} 缺少 Text 组件。");
////                    }
////                    break;
////                case GameUIBinding.WidgetTypes.Toggle:
////                    RequireComponent<Toggle>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.Button:
////                    RequireComponent<Button>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.Image:
////                    RequireComponent<Image>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.InputField:
////                    if (!entry.GameObject.GetComponent<InputField>() && !entry.GameObject.GetComponent<TMPro.TMP_InputField>())
////                    {
////                        result.AddIssueWithEnglishMessage(
////                            GameUIBindingIssueSeverity.Error,
////                            bindingPath,
////                            "Binding {0} is missing an InputField component.",
////                            new object[] { entry.Name },
////                            $"绑定 {entry.Name} 缺少 InputField 组件。");
////                    }
////                    break;
////                case GameUIBinding.WidgetTypes.ToggleGroup:
////                    RequireComponent<ToggleGroup>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.ScrollRect:
////                    RequireComponent<ScrollRect>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.RawImage:
////                    RequireComponent<RawImage>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.Canvas:
////                    RequireComponent<Canvas>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.TabLoader:
////                    RequireComponent<TabLoader>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.UIContainer:
////                    RequireComponent<UIContainer>(entry, bindingPath, result);
////                    break;
////                case GameUIBinding.WidgetTypes.UILogic:
////                    var childBinding = entry.GameObject.GetComponent<GameUIBinding>();
////                    if (!childBinding)
////                    {
////                        result.AddIssueWithEnglishMessage(
////                            GameUIBindingIssueSeverity.Error,
////                            bindingPath,
////                            "UILogic binding {0} is missing a child GameUIBinding.",
////                            new object[] { entry.Name },
////                            $"UILogic 绑定 {entry.Name} 缺少子 GameUIBinding。");
////                    }
////                    else if (!childBinding.NoCodeGeneration && string.IsNullOrEmpty(childBinding.ClassName))
////                    {
////                        result.AddIssueWithEnglishMessage(
////                            GameUIBindingIssueSeverity.Error,
////                            bindingPath,
////                            "The child GameUIBinding.className for UILogic binding {0} is empty.",
////                            new object[] { entry.Name },
////                            $"UILogic 绑定 {entry.Name} 的子 GameUIBinding.className 为空。");
////                    }
////                    break;
////            }
////        }
////
////        private static void RequireComponent<T>(GameUIBinding.BindingEntry entry, string bindingPath, GameUIBindingValidationResult result)
////            where T : Component
////        {
////            if (!entry.GameObject.GetComponent<T>())
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    bindingPath,
////                    "Binding {0} is missing a {1} component.",
////                    new object[] { entry.Name, typeof(T).Name },
////                    $"绑定 {entry.Name} 缺少 {typeof(T).Name} 组件。");
////            }
////        }
////
////        private static void ValidateBaseBinding(GameUIBinding binding, GameUIBindingValidationResult result)
////        {
////            var guid = GetBaseBindingGuid(binding);
////            if (string.IsNullOrEmpty(guid))
////            {
////                return;
////            }
////
////            var path = AssetDatabase.GUIDToAssetPath(guid);
////            if (string.IsNullOrEmpty(path))
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "The Prefab referenced by baseBindingUUID does not exist.",
////                    null,
////                    "baseBindingUUID 指向的 prefab 不存在。",
////                    "清空继承配置或重新选择基础 prefab。");
////                return;
////            }
////
////            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
////            var baseBinding = prefab ? prefab.GetComponent<GameUIBinding>() : null;
////            if (!baseBinding)
////            {
////                result.AddIssueWithEnglishMessage(
////                    GameUIBindingIssueSeverity.Error,
////                    null,
////                    "The Prefab referenced by baseBindingUUID has no GameUIBinding: {0}.",
////                    new object[] { path },
////                    $"baseBindingUUID 指向的 prefab 没有 GameUIBinding：{path}。");
////                return;
////            }
////
////            var currentNames = new HashSet<string>();
////            if (binding.Bindings != null)
////            {
////                foreach (var entry in binding.Bindings)
////                {
////                    if (!string.IsNullOrEmpty(entry.Name))
////                    {
////                        currentNames.Add(entry.Name);
////                    }
////                }
////            }
////
////            if (baseBinding.Bindings == null)
////            {
////                return;
////            }
////
////            foreach (var entry in baseBinding.Bindings)
////            {
////                if (!string.IsNullOrEmpty(entry.Name) && !currentNames.Contains(entry.Name))
////                {
////                    result.AddIssueWithEnglishMessage(
////                        GameUIBindingIssueSeverity.Warning,
////                        null,
////                        "Base binding field {0} is missing.",
////                        new object[] { entry.Name },
////                        $"缺少基础 binding 字段：{entry.Name}。",
////                        "在 Inspector 继承区点击自动添加，或通过 GameUIBindingEditorUtility 同步。");
////                }
////            }
////        }
////
////        private static void SetWidgetType(SerializedProperty property, GameUIBinding.WidgetTypes widgetType)
////        {
////            property.enumValueIndex = widgetType > GameUIBinding.WidgetTypes.End
////                ? (int)GameUIBinding.WidgetTypes.End + 1
////                : (int)widgetType;
////        }
////
////        private static GameObject ResolveGameObject(GameUIBinding binding, string relativePath)
////        {
////            if (!binding)
////            {
////                return null;
////            }
////
////            if (string.IsNullOrEmpty(relativePath))
////            {
////                return binding.gameObject;
////            }
////
////            var transform = binding.transform.Find(relativePath);
////            return transform ? transform.gameObject : null;
////        }
////
////        private static string GetPrefabAssetPath(GameUIBinding binding)
////        {
////            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
////            if (string.IsNullOrEmpty(path))
////            {
////                path = AssetDatabase.GetAssetPath(binding.gameObject);
////            }
////
////            return path;
////        }
////
////        private static string GetTransformPath(Transform root, Transform target)
////        {
////            if (!root || !target)
////            {
////                return null;
////            }
////
////            if (root == target)
////            {
////                return string.Empty;
////            }
////
////            var names = new Stack<string>();
////            var current = target;
////            while (current && current != root)
////            {
////                names.Push(current.name);
////                current = current.parent;
////            }
////
////            return current == root ? string.Join("/", names.ToArray()) : null;
////        }
////
////        private static bool IsBindingNameCandidate(string name)
////        {
////            return name.StartsWith("m_", StringComparison.Ordinal) ||
////                   (name.StartsWith("m", StringComparison.Ordinal) && name.Length > 1 && char.IsUpper(name[1]));
////        }
////
////        private static string GetUniqueBindingName(string nodeName, HashSet<string> definedNames)
////        {
////            var baseName = GetBindingNameForCode(nodeName);
////            var current = baseName;
////            var index = 1;
////            while (definedNames.Contains(current))
////            {
////                current = GetBindingNameForCode($"{nodeName}_{index++}");
////            }
////
////            definedNames.Add(current);
////            return current;
////        }
////
////        private static string GetBindingNameForCode(string nodeName)
////        {
////            nodeName = nodeName.Replace(" ", "_");
////            return nodeName.StartsWith("m_", StringComparison.Ordinal) ? nodeName.Substring(2) : nodeName;
////        }
////    }
////}
