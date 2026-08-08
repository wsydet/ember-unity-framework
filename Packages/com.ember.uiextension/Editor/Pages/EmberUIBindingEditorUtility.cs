// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ember.Basic;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Ember.UIExtension.Editor
{
    #region 数据结构

    /// <summary>EmberUIBinding 完整快照</summary>
    public sealed class EmberUIBindingSnapshot
    {
        public string PrefabAssetPath;
        public string PrefabName;
        public string PageName;
        public string ClassPath;
        public string ClassName;
        public bool IsPage;
        public PageFlags PageFlags;
        public EmberUIBinding.WidgetTypes SelfWidgetType;
        public string SelfWidgetClassName;
        public bool NoCodeGen;
        public string BaseBindingPrefabPath;
        public string BaseBindingGuid;
        public List<EmberUIBindingEntrySnapshot> Entries = new List<EmberUIBindingEntrySnapshot>();
    }

    /// <summary>单个绑定条目的快照</summary>
    public sealed class EmberUIBindingEntrySnapshot
    {
        public string Name;
        public string GameObjectPath;
        public EmberUIBinding.WidgetTypes WidgetType;
        public string ClassName;
    }

    /// <summary>验证问题严重度</summary>
    public enum EmberUIBindingIssueSeverity { Info, Warning, Error }

    /// <summary>单个验证问题</summary>
    public sealed class EmberUIBindingValidationIssue
    {
        public EmberUIBindingIssueSeverity Severity;
        public string BindingPath;
        public string Message;
        public string Suggestion;
    }

    /// <summary>验证结果集</summary>
    public sealed class EmberUIBindingValidationResult
    {
        public string AssetPath;
        public List<EmberUIBindingValidationIssue> Issues = new List<EmberUIBindingValidationIssue>();

        public bool HasError
        {
            get
            {
                foreach (var issue in Issues)
                    if (issue.Severity == EmberUIBindingIssueSeverity.Error) return true;
                return false;
            }
        }

        public void AddIssue(EmberUIBindingIssueSeverity severity, string bindingPath, string message, string suggestion = null)
        {
            Issues.Add(new EmberUIBindingValidationIssue
            {
                Severity = severity,
                BindingPath = bindingPath,
                Message = message,
                Suggestion = suggestion,
            });
        }
    }

    #endregion

    // --------------------------------------------------------

    #region 组件类型数据结构

    public struct ComponentTypeOption { public int Index; public string Name; }

    public struct ComponentTypeRule
    {
        public EmberUIBinding.WidgetTypes WidgetType;
        public Type[] ComponentTypes;
        public int Index => (int)WidgetType;
        public string ComponentTypeNames => string.Join(" 或 ", ComponentTypes.Select(i => i.Name));

        public ComponentTypeRule(EmberUIBinding.WidgetTypes widgetType, params Type[] componentTypes)
        {
            WidgetType = widgetType;
            ComponentTypes = componentTypes;
        }

        public bool Matches(GameObject go)
        {
            for (int i = 0; i < ComponentTypes.Length; i++)
                if (go.GetComponent(ComponentTypes[i])) return true;
            return false;
        }

        public bool ExactMatches(GameObject go)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (!component) continue;
                var componentType = component.GetType();
                for (int j = 0; j < ComponentTypes.Length; j++)
                    if (componentType == ComponentTypes[j]) return true;
            }
            return false;
        }
    }

    #endregion

    // --------------------------------------------------------

    /// <summary>
    /// EmberUIBinding 编辑器工具类。
    /// 提供快照、验证、批量收集、PageDef 更新、组件类型检测等功能。
    /// </summary>
    public static class EmberUIBindingEditorUtility
    {
        private const string PageNameProperty = "pageName";
        private const string ClassPathProperty = "classPath";
        private const string ClassNameProperty = "className";
        private const string IsPageProperty = "isPage";
        private const string SelfWidgetTypeProperty = "selfWidgetType";
        private const string SelfWidgetClassNameProperty = "selfWidgetClassName";
        private const string BindingsProperty = "bindings";
        private const string NoCodeGenProperty = "noCodeGen";
        private const string BaseBindingGuidProperty = "baseBindingUUID";
        private const string PageFlagsProperty = "pageFlags";

        #region 快照

        /// <summary>获取 binding 的完整快照</summary>
        public static EmberUIBindingSnapshot GetBindingSnapshot(EmberUIBinding binding)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            var assetPath = GetPrefabAssetPath(binding);
            var snapshot = new EmberUIBindingSnapshot
            {
                PrefabAssetPath = assetPath,
                PrefabName = string.IsNullOrEmpty(assetPath)
                    ? binding.gameObject.name : Path.GetFileNameWithoutExtension(assetPath),
                PageName = binding.PageName,
                ClassPath = binding.ClassPath,
                ClassName = binding.ClassName,
                IsPage = binding.IsPage,
                PageFlags = binding.PageFlags,
                SelfWidgetType = binding.SelfWidgetType,
                SelfWidgetClassName = binding.SelfWidgetClassName,
                NoCodeGen = binding.NoCodeGeneration,
                BaseBindingGuid = GetBaseBindingGuid(binding),
            };

            if (!string.IsNullOrEmpty(snapshot.BaseBindingGuid))
                snapshot.BaseBindingPrefabPath = AssetDatabase.GUIDToAssetPath(snapshot.BaseBindingGuid);

            if (binding.Bindings != null)
            {
                foreach (var entry in binding.Bindings)
                {
                    snapshot.Entries.Add(new EmberUIBindingEntrySnapshot
                    {
                        Name = entry.Name,
                        GameObjectPath = GetTransformPath(binding.transform,
                            entry.GameObject ? entry.GameObject.transform : null),
                        WidgetType = entry.Type,
                        ClassName = entry.ClassName,
                    });
                }
            }
            return snapshot;
        }

        /// <summary>将快照应用到 binding</summary>
        public static void ApplyBindingSnapshot(EmberUIBinding binding, EmberUIBindingSnapshot snapshot)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SetPageInfo(binding, snapshot.PageName, snapshot.ClassPath, snapshot.ClassName,
                snapshot.IsPage, snapshot.PageFlags, snapshot.NoCodeGen);
            SetSelfWidget(binding, snapshot.SelfWidgetType, snapshot.SelfWidgetClassName);
            SetBaseBinding(binding, snapshot.BaseBindingGuid);
            SetBindings(binding, snapshot.Entries);
        }

        /// <summary>设置页面信息</summary>
        public static void SetPageInfo(EmberUIBinding binding, string pageName, string classPath,
            string className, bool isPage, PageFlags pageFlags, bool noCodeGen = false)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(PageNameProperty).stringValue = pageName;
                so.FindProperty(ClassPathProperty).stringValue = classPath;
                so.FindProperty(ClassNameProperty).stringValue = className;
                so.FindProperty(IsPageProperty).boolValue = isPage;
                so.FindProperty(NoCodeGenProperty).boolValue = noCodeGen;
                so.FindProperty(PageFlagsProperty).enumValueFlag = (int)pageFlags;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置自身控件类型</summary>
        public static void SetSelfWidget(EmberUIBinding binding, EmberUIBinding.WidgetTypes widgetType,
            string widgetClassName = null)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(SelfWidgetTypeProperty).enumValueIndex = widgetType > EmberUIBinding.WidgetTypes.End
                    ? (int)EmberUIBinding.WidgetTypes.End + 1 : (int)widgetType;
                so.FindProperty(SelfWidgetClassNameProperty).stringValue = widgetClassName;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置绑定列表</summary>
        public static void SetBindings(EmberUIBinding binding, IList<EmberUIBindingEntrySnapshot> entries)
        {
            using (var so = new SerializedObject(binding))
            {
                var bp = so.FindProperty(BindingsProperty);
                bp.ClearArray();
                if (entries != null)
                {
                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        bp.InsertArrayElementAtIndex(i);
                        var sp = bp.GetArrayElementAtIndex(i);
                        sp.FindPropertyRelative("Name").stringValue = entry?.Name;
                        sp.FindPropertyRelative("GameObject").objectReferenceValue =
                            ResolveGameObject(binding, entry?.GameObjectPath);
                        sp.FindPropertyRelative("Type").enumValueIndex = entry?.WidgetType > EmberUIBinding.WidgetTypes.End
                            ? (int)EmberUIBinding.WidgetTypes.End + 1 : (int)(entry?.WidgetType ?? 0);
                        sp.FindPropertyRelative("ClassName").stringValue = entry?.ClassName;
                    }
                }
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置基类绑定（通过 GUID）</summary>
        public static void SetBaseBinding(EmberUIBinding binding, string baseBindingGuid)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(BaseBindingGuidProperty).stringValue = baseBindingGuid;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置基类绑定（通过 Prefab）</summary>
        public static void SetBaseBinding(EmberUIBinding binding, GameObject basePrefab)
        {
            var guid = string.Empty;
            if (basePrefab)
            {
                var path = AssetDatabase.GetAssetPath(basePrefab);
                guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            }
            SetBaseBinding(binding, guid);
        }

        /// <summary>获取基类 GUID</summary>
        public static string GetBaseBindingGuid(EmberUIBinding binding)
        {
            using (var so = new SerializedObject(binding))
                return so.FindProperty(BaseBindingGuidProperty).stringValue;
        }

        #endregion

        // --------------------------------------------------------

        #region 收集与验证

        /// <summary>收集子节点绑定</summary>
        public static List<EmberUIBindingEntrySnapshot> CollectBindings(EmberUIBinding binding, bool clearExisting = false)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            using (var so = new SerializedObject(binding))
            {
                var bp = so.FindProperty(BindingsProperty);
                var definedObjects = new Dictionary<GameObject, GameObject>();
                var definedNames = new HashSet<string>();

                if (clearExisting)
                {
                    bp.ClearArray();
                }
                else
                {
                    GatherBindingDefinitions(binding, definedObjects);
                    if (binding.Bindings != null)
                    {
                        foreach (var entry in binding.Bindings)
                        {
                            if (!string.IsNullOrEmpty(entry.Name))
                                definedNames.Add(entry.Name);
                        }
                    }
                }

                CollectBindingsRecursive(binding, bp, definedObjects, definedNames, binding.transform);
                so.ApplyModifiedProperties();
            }
            return GetBindingSnapshot(binding).Entries;
        }

        /// <summary>验证 binding 的合法性</summary>
        public static EmberUIBindingValidationResult ValidateBinding(EmberUIBinding binding)
        {
            var result = new EmberUIBindingValidationResult();
            if (!binding)
            {
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null, "EmberUIBinding 为空。");
                return result;
            }
            result.AssetPath = GetPrefabAssetPath(binding);
            ValidatePageInfo(binding, result);
            ValidateEntries(binding, result);
            ValidateBaseBinding(binding, result);
            return result;
        }

        /// <summary>生成或更新 PageDef</summary>
        public static bool GenerateOrUpdatePageDef(EmberUIBinding binding,
            CSharpLogicImplementationData implementationData = null)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            if (!binding.IsPage) return true;
            implementationData = implementationData ?? FindCSharpLogicImplementationData();
            if (!implementationData)
            {
                EmberDebug.LogError("EmberUI", "未找到 CSharpLogicImplementationData。请检查 Project Settings/Ember UI Binding 中的逻辑实现数据。");
                return false;
            }
            return implementationData.GenerateOrUpdatePageDefinition(binding);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法（编辑器辅助）

        /// <summary>
        /// 根据 PageFlags 设置 Canvas.sortingOrder，方便非 Play 模式下查看层级效果。
        /// 生成代码时自动调用。
        /// </summary>
        public static void ApplyCanvasSortingOrder(EmberUIBinding binding)
        {
            if (binding == null || !binding.IsPage) return;

            var canvas = binding.GetComponent<Canvas>();
            if (canvas == null) return;

            int order = PageFlagsToSortingOrder(binding.PageFlags);
            if (order < 0) return;

            canvas.sortingOrder = order;
            EditorUtility.SetDirty(canvas);
            PrefabUtility.SavePrefabAsset(binding.gameObject);
        }

        private static int PageFlagsToSortingOrder(PageFlags flags)
        {
            if ((flags & PageFlags.TopMost) != 0) return 300;
            if ((flags & PageFlags.Popup) != 0) return 200;
            if ((flags & PageFlags.MainPage) != 0) return 100;
            if ((flags & PageFlags.SubPage) != 0) return 100;
            return -1; // None，不设置
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static CSharpLogicImplementationData FindCSharpLogicImplementationData()
        {
            var settings = UIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null) return null;
            foreach (var implementation in settings.LogicImplementations)
            {
                if (implementation is CSharpLogicImplementationData csharp)
                    return csharp;
            }
            return null;
        }

        private static void CollectBindingsRecursive(EmberUIBinding binding, SerializedProperty bp,
            Dictionary<GameObject, GameObject> definedObjects, HashSet<string> definedNames, Transform transform)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childGo = child.gameObject;
                bool isSubBindingRoot = childGo.GetComponent<EmberUIBinding>() != null;

                if (!definedObjects.ContainsKey(childGo) && IsBindingNameCandidate(child.name))
                {
                    var index = bp.arraySize;
                    bp.InsertArrayElementAtIndex(index);
                    var childProperty = bp.GetArrayElementAtIndex(index);
                    childProperty.FindPropertyRelative("Name").stringValue =
                        GetUniqueBindingName(child.name, definedNames);
                    childProperty.FindPropertyRelative("GameObject").objectReferenceValue = childGo;
                    childProperty.FindPropertyRelative("ClassName").stringValue = null;
                    AutoSelectByObject(childGo,
                        childProperty.FindPropertyRelative("Type"),
                        childProperty.FindPropertyRelative("ClassName"));
                    definedObjects[childGo] = binding.gameObject;
                }

                if (!isSubBindingRoot)
                    CollectBindingsRecursive(binding, bp, definedObjects, definedNames, child);
            }
        }

        private static void ValidatePageInfo(EmberUIBinding binding, EmberUIBindingValidationResult result)
        {
            if (string.IsNullOrEmpty(binding.ClassPath))
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null,
                    "classPath 为空。", "填写逻辑路径。");

            if (string.IsNullOrEmpty(binding.ClassName))
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null,
                    "className 为空。", "填写页面或组件逻辑类名。");

            if (!binding.IsPage) return;

            if (string.IsNullOrEmpty(binding.PageName))
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null,
                    "页面级 binding 的 pageName 为空。", "pageName 应作为 PageDef 常量名。");

            if (binding.PageFlags == PageFlags.None)
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null,
                    "页面级 binding 的 pageFlags 为 None。", "选择 MainPage、Popup、TopMost、SubPage 或 FreePage。");
        }

        private static void ValidateEntries(EmberUIBinding binding, EmberUIBindingValidationResult result)
        {
            if (binding.Bindings == null) return;
            var names = new HashSet<string>();
            foreach (var entry in binding.Bindings)
            {
                var bindingPath = entry.GameObject
                    ? GetTransformPath(binding.transform, entry.GameObject.transform) : null;

                if (string.IsNullOrEmpty(entry.Name))
                    result.AddIssue(EmberUIBindingIssueSeverity.Error, bindingPath,
                        "绑定字段名为空。", "重新收集绑定或手动补齐字段名。");
                else if (!names.Add(entry.Name))
                    result.AddIssue(EmberUIBindingIssueSeverity.Error, bindingPath,
                        $"绑定字段名重复：{entry.Name}。", "重命名其中一个绑定字段。");

                if (!entry.GameObject)
                {
                    result.AddIssue(EmberUIBindingIssueSeverity.Error, bindingPath,
                        $"绑定 {entry.Name} 的 GameObject 丢失。", "重新指定节点或删除该绑定。");
                }
            }
        }

        private static void ValidateBaseBinding(EmberUIBinding binding, EmberUIBindingValidationResult result)
        {
            var guid = GetBaseBindingGuid(binding);
            if (string.IsNullOrEmpty(guid)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                result.AddIssue(EmberUIBindingIssueSeverity.Error, null,
                    "baseBindingUUID 指向的 prefab 不存在。", "清空继承配置或重新选择基础 prefab。");
            }
        }

        private static GameObject ResolveGameObject(EmberUIBinding binding, string relativePath)
        {
            if (!binding) return null;
            if (string.IsNullOrEmpty(relativePath)) return binding.gameObject;
            var transform = binding.transform.Find(relativePath);
            return transform ? transform.gameObject : null;
        }

        private static string GetPrefabAssetPath(EmberUIBinding binding)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding);
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(binding.gameObject);
            return path;
        }

        private static string GetTransformPath(Transform root, Transform target)
        {
            if (!root || !target) return null;
            if (root == target) return string.Empty;
            var names = new Stack<string>();
            var current = target;
            while (current && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return current == root ? string.Join("/", names.ToArray()) : null;
        }

        private static bool IsBindingNameCandidate(string name)
        {
            return name.StartsWith("m_", StringComparison.Ordinal)
                || (name.StartsWith("m", StringComparison.Ordinal) && name.Length > 1 && char.IsUpper(name[1]));
        }

        private static string GetUniqueBindingName(string nodeName, HashSet<string> definedNames)
        {
            var baseName = GetBindingNameForCode(nodeName);
            var current = baseName;
            var index = 1;
            while (definedNames.Contains(current))
                current = GetBindingNameForCode($"{nodeName}_{index++}");
            definedNames.Add(current);
            return current;
        }

        private static string GetBindingNameForCode(string nodeName)
        {
            nodeName = nodeName.Replace(" ", "_");
            return nodeName.StartsWith("m_", StringComparison.Ordinal) ? nodeName.Substring(2) : nodeName;
        }

        #endregion

        // --------------------------------------------------------

        #region 组件类型检测

        private static string[] _typeNames;
        private static Dictionary<string, KeyValuePair<int, KeyValuePair<Type, Type>>> _extensionTypeMapping;

        public static readonly ComponentTypeRule[] BuiltInComponentTypeRules =
        {
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.UILogic, typeof(EmberUIBinding)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Button, typeof(Button)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Text, typeof(Text), typeof(TextMeshProUGUI)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMP_InputField)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Image, typeof(Image)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.RawImage, typeof(RawImage)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Canvas, typeof(Canvas)),
        };

        public static readonly ComponentTypeRule[] AutoSelectExactBuiltInComponentTypeRules =
        {
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Button, typeof(Button)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Text, typeof(Text), typeof(TextMeshProUGUI)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMP_InputField)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
            new ComponentTypeRule(EmberUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
        };

        /// <summary>获取所有组件类型名（内置 + 扩展）</summary>
        public static string[] GetComponentTypeNames()
        {
            if (_typeNames == null)
            {
                _extensionTypeMapping = new Dictionary<string, KeyValuePair<int, KeyValuePair<Type, Type>>>();
                List<string> nameList = new List<string>();
                var names = System.Enum.GetNames(typeof(EmberUIBinding.WidgetTypes));
                for (int i = 0; i < names.Length - 2; i++)
                    nameList.Add(names[i]);

                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (t.IsGenericTypeDefinition || t.IsAbstract) continue;
                        var arr = t.GetCustomAttributes(typeof(EmberUIExtensionAttribute), false);
                        if (arr != null && arr.Length > 0)
                        {
                            EmberUIExtensionAttribute attr = (EmberUIExtensionAttribute)arr[0];
                            string name = string.IsNullOrEmpty(attr.Name) ? attr.ComponentType.Name : attr.Name;
                            _extensionTypeMapping[name] = new KeyValuePair<int, KeyValuePair<Type, Type>>(
                                nameList.Count, new KeyValuePair<Type, Type>(attr.ComponentType, t));
                            nameList.Add(name);
                        }
                    }
                }
                _typeNames = nameList.ToArray();
            }
            return _typeNames;
        }

        /// <summary>获取扩展类型映射表</summary>
        public static Dictionary<string, KeyValuePair<int, KeyValuePair<Type, Type>>> GetExtensionTypeMapping()
        {
            if (_extensionTypeMapping == null)
                GetComponentTypeNames();
            return _extensionTypeMapping;
        }

        /// <summary>收集所有已定义的绑定（递归，含子 EmberUIBinding）</summary>
        public static void GatherBindingDefinitions(EmberUIBinding binding, Dictionary<GameObject, GameObject> defined, bool recursive = true)
        {
            if (binding.Bindings == null) return;
            foreach (var i in binding.Bindings)
            {
                if (recursive && i.GameObject)
                {
                    EmberUIBinding child = i.GameObject.GetComponent<EmberUIBinding>();
                    if (child == binding) continue;
                    if (child)
                        GatherBindingDefinitions(child, defined);
                }
                if (!defined.ContainsKey(i.GameObject))
                    defined[i.GameObject] = binding.gameObject;
            }
        }

        /// <summary>根据 GameObject 上的组件自动选择 WidgetType</summary>
        public static void AutoSelectByObject(GameObject go, SerializedProperty type, SerializedProperty cn)
        {
            var binding = go.GetComponent<EmberUIBinding>();
            if (binding)
            {
                SetBuiltInWidgetType(type, cn, EmberUIBinding.WidgetTypes.UILogic);
                SetUILogicClassName(binding, cn);
                return;
            }

            foreach (var rule in AutoSelectExactBuiltInComponentTypeRules)
            {
                if (rule.ExactMatches(go))
                {
                    SetBuiltInWidgetType(type, cn, rule.WidgetType);
                    return;
                }
            }

            if (TryGetFirstMatchingExtension(go, out var extensionName))
            {
                type.enumValueIndex = (int)EmberUIBinding.WidgetTypes.End + 1;
                if (cn != null)
                    cn.stringValue = extensionName;
                return;
            }

            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == EmberUIBinding.WidgetTypes.UILogic) continue;
                if (rule.Matches(go))
                {
                    SetBuiltInWidgetType(type, cn, rule.WidgetType);
                    return;
                }
            }

            SetBuiltInWidgetType(type, cn, EmberUIBinding.WidgetTypes.Component);
        }

        /// <summary>验证绑定类型是否匹配</summary>
        public static bool ValidateType(GameObject go, SerializedProperty type, SerializedProperty cn, bool canAutoDetect = true)
        {
            bool invalid = false;
            if (!go)
            {
                EditorGUILayout.HelpBox("绑定对象缺失", MessageType.Error);
                return true;
            }
            int index = type.enumValueIndex;
            if (index == (int)EmberUIBinding.WidgetTypes.End + 1)
                index = (int)EmberUIBinding.WidgetTypes.Extension;
            switch ((EmberUIBinding.WidgetTypes)index)
            {
                case EmberUIBinding.WidgetTypes.UILogic:
                    var childBinding = go.GetComponent<EmberUIBinding>();
                    if (!childBinding)
                    {
                        EditorGUILayout.HelpBox($"{go} 并不包含 EmberUIBinding 组件", MessageType.Error);
                        invalid = true;
                    }
                    else
                    {
                        SetUILogicClassName(childBinding, cn);
                    }
                    break;
                case EmberUIBinding.WidgetTypes.Extension:
                    var mapping = GetExtensionTypeMapping();
                    if (mapping.TryGetValue(cn.stringValue, out var ct))
                    {
                        if (!go.GetComponent(ct.Value.Key))
                        {
                            EditorGUILayout.HelpBox($"{go} 并不包含 {ct.Value.Key.Name} 组件", MessageType.Error);
                            invalid = true;
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"找不到 {go} 指定的扩展组件: {cn.stringValue}", MessageType.Error);
                        invalid = true;
                    }
                    break;
                default:
                    var widgetType = (EmberUIBinding.WidgetTypes)index;
                    if (TryGetBuiltInComponentTypeRule(widgetType, out var rule) && !rule.Matches(go))
                    {
                        EditorGUILayout.HelpBox($"{go} 并不包含 {rule.ComponentTypeNames} 组件", MessageType.Error);
                        invalid = true;
                    }
                    break;
            }
            if (invalid && canAutoDetect)
            {
                if (GUILayout.Button("自动识别类型"))
                {
                    AutoSelectByObject(go, type, cn);
                }
            }
            return invalid;
        }

        /// <summary>绘制组件类型下拉框</summary>
        public static void DrawComponentType(string typeLabel, SerializedProperty type, SerializedProperty cn, ref bool needValidation, int labelSize = 55, GameObject bindingObject = null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(typeLabel, GUILayout.Width(labelSize));
                int oldIdx = GetWidgetIndexAndName(type, cn);
                var options = GetAvailableComponentTypeOptions(bindingObject, oldIdx, cn.stringValue);
                int oldOptionIdx = GetComponentTypeOptionIndex(options, oldIdx);
                int optionIdx = EditorGUILayout.Popup(oldOptionIdx, options.Select(i => i.Name).ToArray(), GUILayout.Width(100), GUILayout.MinWidth(80));
                int index = optionIdx >= 0 && optionIdx < options.Count ? options[optionIdx].Index : oldIdx;
                if (oldOptionIdx != optionIdx)
                    needValidation = true;

                if (index == (int)EmberUIBinding.WidgetTypes.UILogic)
                {
                    if (!string.IsNullOrEmpty(cn.stringValue))
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        cn.stringValue = EditorGUILayout.TextField(cn.stringValue, GUILayout.MinWidth(100));
                        EditorGUI.EndDisabledGroup();
                    }
                }
                if (index < 0) return;

                if (index >= (int)EmberUIBinding.WidgetTypes.End)
                {
                    var newName = GetComponentTypeNames()[index];
                    type.enumValueIndex = (int)EmberUIBinding.WidgetTypes.End + 1;
                    cn.stringValue = newName;
                }
                else
                {
                    SetBuiltInWidgetType(type, cn, (EmberUIBinding.WidgetTypes)index);
                }
            }
        }

        public static List<ComponentTypeOption> GetAvailableComponentTypeOptions(GameObject go, int currentIndex, string currentName)
        {
            var allNames = GetComponentTypeNames();
            if (!go)
            {
                var result = new List<ComponentTypeOption>();
                for (int i = 0; i < allNames.Length; i++)
                    result.Add(new ComponentTypeOption { Index = i, Name = allNames[i] });
                return result;
            }

            var res = new List<ComponentTypeOption>();
            res.Add(new ComponentTypeOption { Index = (int)EmberUIBinding.WidgetTypes.Component, Name = allNames[(int)EmberUIBinding.WidgetTypes.Component] });

            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.Matches(go))
                    res.Add(new ComponentTypeOption { Index = rule.Index, Name = allNames[rule.Index] });
            }

            var mapping = GetExtensionTypeMapping();
            foreach (var i in mapping)
            {
                if (go.GetComponent(i.Value.Value.Key))
                    res.Add(new ComponentTypeOption { Index = i.Value.Key, Name = allNames[i.Value.Key] });
            }

            if (currentIndex >= 0)
                res.Add(new ComponentTypeOption { Index = currentIndex, Name = allNames[currentIndex] });
            else if (!string.IsNullOrEmpty(currentName))
                res.Add(new ComponentTypeOption { Index = -1, Name = currentName });

            return res;
        }

        private static int GetWidgetIndexAndName(SerializedProperty type, SerializedProperty cn)
        {
            int index = type.enumValueIndex;
            if (index == (int)EmberUIBinding.WidgetTypes.End + 1)
                index = (int)EmberUIBinding.WidgetTypes.Extension;
            switch ((EmberUIBinding.WidgetTypes)index)
            {
                case EmberUIBinding.WidgetTypes.UILogic:
                    return index;
                case EmberUIBinding.WidgetTypes.Extension:
                    var mapping = GetExtensionTypeMapping();
                    if (mapping.TryGetValue(cn.stringValue, out var pair))
                        return pair.Key;
                    return -1;
                default:
                    return index;
            }
        }

        private static bool TryGetBuiltInComponentTypeRule(EmberUIBinding.WidgetTypes widgetType, out ComponentTypeRule result)
        {
            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == widgetType) { result = rule; return true; }
            }
            result = default;
            return false;
        }

        private static bool TryGetFirstMatchingExtension(GameObject go, out string extensionName)
        {
            var mapping = GetExtensionTypeMapping();
            foreach (var i in mapping)
            {
                if (go.GetComponent(i.Value.Value.Key)) { extensionName = i.Key; return true; }
            }
            extensionName = null;
            return false;
        }

        private static void SetBuiltInWidgetType(SerializedProperty type, SerializedProperty cn, EmberUIBinding.WidgetTypes widgetType)
        {
            int previousType = type.enumValueIndex;
            type.enumValueIndex = (int)widgetType;
            if (cn != null && previousType != (int)widgetType && widgetType != EmberUIBinding.WidgetTypes.UILogic)
                cn.stringValue = null;
        }

        private static void SetUILogicClassName(EmberUIBinding childBinding, SerializedProperty cn)
        {
            if (cn == null) return;
            cn.stringValue = childBinding && !childBinding.NoCodeGeneration ? childBinding.ClassName : null;
        }

        private static int GetComponentTypeOptionIndex(List<ComponentTypeOption> options, int index)
        {
            for (int i = 0; i < options.Count; i++)
                if (options[i].Index == index) return i;
            return 0;
        }

        #endregion
    }
}
