// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Ember.Basic;
using Ember.UI;

using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Ember.UIExtension.Editor
{
    #region 数据结构

    /// <summary>EUIBinding 完整快照</summary>
    public sealed class EUIBindingSnapshot
    {
        public string PrefabAssetPath;
        public string PrefabName;
        public string PageName;
        public string ClassPath;
        public string ClassName;
        public bool IsPage;
        public PageType PageType;
        public bool UseUIUpdate;
        public bool GenerateAutoCreateClickableMaskOverride;
        public bool GenerateOnClickMaskOverride;
        public EUIBinding.WidgetTypes SelfWidgetType;
        public string SelfWidgetClassName;
        public bool NoCodeGen;
        public string BaseBindingPrefabPath;
        public string BaseBindingGuid;
        public List<EUIBindingEntrySnapshot> Entries = new List<EUIBindingEntrySnapshot>();
    }

    /// <summary>单个绑定条目的快照</summary>
    public sealed class EUIBindingEntrySnapshot
    {
        public string Name;
        public string GameObjectPath;
        public EUIBinding.WidgetTypes WidgetType;
        public string ClassName;
    }

    /// <summary>验证问题严重度</summary>
    public enum EUIBindingIssueSeverity { Info, Warning, Error }

    /// <summary>单个验证问题</summary>
    public sealed class EUIBindingValidationIssue
    {
        public EUIBindingIssueSeverity Severity;
        public string BindingPath;
        public string Message;
        public string Suggestion;
    }

    /// <summary>验证结果集</summary>
    public sealed class EUIBindingValidationResult
    {
        public string AssetPath;
        public List<EUIBindingValidationIssue> Issues = new List<EUIBindingValidationIssue>();

        public bool HasError
        {
            get
            {
                foreach (var issue in Issues)
                    if (issue.Severity == EUIBindingIssueSeverity.Error) return true;
                return false;
            }
        }

        public void AddIssue(EUIBindingIssueSeverity severity, string bindingPath, string message, string suggestion = null)
        {
            Issues.Add(new EUIBindingValidationIssue
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
        public EUIBinding.WidgetTypes WidgetType;
        public Type[] ComponentTypes;
        public int Index => (int)WidgetType;
        public string ComponentTypeNames => string.Join(" 或 ", ComponentTypes.Select(i => i.Name));

        public ComponentTypeRule(EUIBinding.WidgetTypes widgetType, params Type[] componentTypes)
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
    /// EUIBinding 编辑器工具类。
    /// 提供快照、验证、批量收集、EUIPageDef 更新、组件类型检测等功能。
    /// </summary>
    public static class EUIBindingEditorUtility
    {
        private const string TAG = LogTags.EmberUI;
        private const string PageNameProperty = "pageName";
        private const string ClassPathProperty = "classPath";
        private const string ClassNameProperty = "className";
        private const string CodePathModeProperty = "codePathMode";
        private const string PrefabNameProperty = "prefabName";
        private const string GenerateCustomSettingsProperty = "generateCustomSettings";
        private const string IsPageProperty = "isPage";
        private const string SelfWidgetTypeProperty = "selfWidgetType";
        private const string SelfWidgetClassNameProperty = "selfWidgetClassName";
        private const string BindingsProperty = "bindings";
        private const string NoCodeGenProperty = "noCodeGen";
        private const string BaseBindingGuidProperty = "baseBindingUUID";
        private const string PageTypeProperty = "pageType";
        private const string LegacyPageFlagsProperty = "pageFlags";
        private const string UseUIUpdateProperty = "useUIUpdate";
        private const string UseMaskProperty = "useMask";
        private const string MaskColorProperty = "maskColor";
        private const string ClickMaskToCloseProperty = "clickMaskToClose";
        private const string GenerateAutoCreateClickableMaskOverrideProperty = "generateAutoCreateClickableMaskOverride";
        private const string GenerateOnClickMaskOverrideProperty = "generateOnClickMaskOverride";
        private const string UsePresetFadeProperty = "usePresetFade";
        private const string UseTransitionBlockProperty = "useTransitionBlock";
        private const string UseAnimatorProperty = "useAnimator";
        private const string UseCustomTransitionProperty = "useCustomTransition";
        private const string FadeInTimeProperty = "fadeInTime";
        private const string FadeOutTimeProperty = "fadeOutTime";

        #region 快照

        /// <summary>获取 binding 的完整快照</summary>
        public static EUIBindingSnapshot GetBindingSnapshot(EUIBinding binding)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            var assetPath = GetPrefabAssetPath(binding);
            var snapshot = new EUIBindingSnapshot
            {
                PrefabAssetPath = assetPath,
                PrefabName = string.IsNullOrEmpty(assetPath)
                    ? binding.gameObject.name : Path.GetFileNameWithoutExtension(assetPath),
                PageName = binding.PageName,
                ClassPath = binding.ClassPath,
                ClassName = binding.ClassName,
                IsPage = binding.IsPage,
                PageType = binding.PageType,
                UseUIUpdate = binding.UseUIUpdate,
                GenerateAutoCreateClickableMaskOverride = binding.GenerateAutoCreateClickableMaskOverride,
                GenerateOnClickMaskOverride = binding.GenerateOnClickMaskOverride,
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
                    snapshot.Entries.Add(new EUIBindingEntrySnapshot
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
        public static void ApplyBindingSnapshot(EUIBinding binding, EUIBindingSnapshot snapshot)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            SetPageInfo(binding, snapshot.PageName, snapshot.ClassPath, snapshot.ClassName,
                snapshot.IsPage, snapshot.PageType, snapshot.NoCodeGen);
            SetOptionalCodeFeatures(binding, snapshot.UseUIUpdate,
                snapshot.GenerateAutoCreateClickableMaskOverride,
                snapshot.GenerateOnClickMaskOverride);
            SetSelfWidget(binding, snapshot.SelfWidgetType, snapshot.SelfWidgetClassName);
            SetBaseBinding(binding, snapshot.BaseBindingGuid);
            SetBindings(binding, snapshot.Entries);
        }

        /// <summary>设置页面信息</summary>
        public static void SetPageInfo(EUIBinding binding, string pageName, string classPath,
            string className, bool isPage, PageType pageType, bool noCodeGen = false)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(PageNameProperty).stringValue = pageName;
                so.FindProperty(ClassPathProperty).stringValue = classPath;
                so.FindProperty(ClassNameProperty).stringValue = className;
                so.FindProperty(IsPageProperty).boolValue = isPage;
                so.FindProperty(NoCodeGenProperty).boolValue = noCodeGen;
                so.FindProperty(PageTypeProperty).intValue = (int)pageType;
                so.FindProperty(LegacyPageFlagsProperty).intValue = 0;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置自身控件类型</summary>
        public static void SetSelfWidget(EUIBinding binding, EUIBinding.WidgetTypes widgetType,
            string widgetClassName = null)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(SelfWidgetTypeProperty).enumValueIndex = widgetType > EUIBinding.WidgetTypes.End
                    ? (int)EUIBinding.WidgetTypes.End + 1 : (int)widgetType;
                so.FindProperty(SelfWidgetClassNameProperty).stringValue = widgetClassName;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置页面可选代码能力。</summary>
        public static void SetOptionalCodeFeatures(EUIBinding binding, bool useUIUpdate,
            bool generateAutoCreateClickableMaskOverride, bool generateOnClickMaskOverride)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(UseUIUpdateProperty).boolValue = useUIUpdate;
                so.FindProperty(GenerateAutoCreateClickableMaskOverrideProperty).boolValue =
                    generateAutoCreateClickableMaskOverride;
                so.FindProperty(GenerateOnClickMaskOverrideProperty).boolValue = generateOnClickMaskOverride;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 将标准页面创建请求完整应用到新建的 EUIBinding。
        /// 非 Popup 页面会清空仅弹窗有效的遮罩与高级钩子配置；方块过渡不属于标准页面骨架。
        /// </summary>
        public static void ApplyCreationConfig(EUIBinding binding, EUICreationRequest request)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            if (request == null) throw new ArgumentNullException(nameof(request));

            bool isPopup = request.PageType == PageType.Popup
                || request.PageType == PageType.FullScreenPopup;
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(CodePathModeProperty).intValue = (int)request.CodePathMode;
                so.FindProperty(ClassPathProperty).stringValue = request.ClassPath;
                so.FindProperty(ClassNameProperty).stringValue = request.ClassName;
                so.FindProperty(PrefabNameProperty).stringValue = request.PrefabName;
                so.FindProperty(NoCodeGenProperty).boolValue = false;
                so.FindProperty(GenerateCustomSettingsProperty).boolValue = request.GenerateCustomSettings;

                so.FindProperty(IsPageProperty).boolValue = true;
                so.FindProperty(PageNameProperty).stringValue = request.PageName;
                so.FindProperty(PageTypeProperty).intValue = (int)request.PageType;
                so.FindProperty(LegacyPageFlagsProperty).intValue = 0;
                so.FindProperty(UseUIUpdateProperty).boolValue = request.UseUIUpdate;

                so.FindProperty(UseMaskProperty).boolValue = isPopup && request.UseMask;
                so.FindProperty(MaskColorProperty).colorValue = request.MaskColor;
                so.FindProperty(ClickMaskToCloseProperty).boolValue = isPopup && request.ClickMaskToClose;
                so.FindProperty(GenerateAutoCreateClickableMaskOverrideProperty).boolValue = isPopup
                    && request.GenerateAutoCreateClickableMaskOverride;
                so.FindProperty(GenerateOnClickMaskOverrideProperty).boolValue = isPopup
                    && request.GenerateOnClickMaskOverride;

                so.FindProperty(UsePresetFadeProperty).boolValue =
                    request.TransitionMode == EUIBinding.RegularTransitionMode.PresetFade;
                so.FindProperty(UseTransitionBlockProperty).boolValue = false;
                so.FindProperty(UseAnimatorProperty).boolValue =
                    request.TransitionMode == EUIBinding.RegularTransitionMode.Animator;
                so.FindProperty(UseCustomTransitionProperty).boolValue =
                    request.TransitionMode == EUIBinding.RegularTransitionMode.CustomCode;
                so.FindProperty(FadeInTimeProperty).floatValue = request.FadeInTime;
                so.FindProperty(FadeOutTimeProperty).floatValue = request.FadeOutTime;

                so.FindProperty(BaseBindingGuidProperty).stringValue = string.Empty;
                so.FindProperty(SelfWidgetTypeProperty).intValue = (int)EUIBinding.WidgetTypes.Component;
                so.FindProperty(SelfWidgetClassNameProperty).stringValue = string.Empty;
                so.FindProperty(BindingsProperty).ClearArray();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(binding);
        }

        /// <summary>设置绑定列表</summary>
        public static void SetBindings(EUIBinding binding, IList<EUIBindingEntrySnapshot> entries)
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
                        sp.FindPropertyRelative("Type").enumValueIndex = entry?.WidgetType > EUIBinding.WidgetTypes.End
                            ? (int)EUIBinding.WidgetTypes.End + 1 : (int)(entry?.WidgetType ?? 0);
                        sp.FindPropertyRelative("ClassName").stringValue = entry?.ClassName;
                    }
                }
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置基类绑定（通过 GUID）</summary>
        public static void SetBaseBinding(EUIBinding binding, string baseBindingGuid)
        {
            using (var so = new SerializedObject(binding))
            {
                so.FindProperty(BaseBindingGuidProperty).stringValue = baseBindingGuid;
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>设置基类绑定（通过 Prefab）</summary>
        public static void SetBaseBinding(EUIBinding binding, GameObject basePrefab)
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
        public static string GetBaseBindingGuid(EUIBinding binding)
        {
            using (var so = new SerializedObject(binding))
                return so.FindProperty(BaseBindingGuidProperty).stringValue;
        }

        #endregion

        // --------------------------------------------------------

        #region 收集与验证

        /// <summary>收集子节点绑定</summary>
        public static List<EUIBindingEntrySnapshot> CollectBindings(EUIBinding binding, bool clearExisting = false)
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

                CollectBindingsRecursive(binding, bp, definedObjects, definedNames, binding.transform, new HashSet<GameObject>());
                so.ApplyModifiedProperties();
            }
            return GetBindingSnapshot(binding).Entries;
        }

        /// <summary>
        /// 收集节点上增强组件（IEUIExposedChildProvider）通过槽位持有的子节点，
        /// 加入 ownedChildren。自动收集时会跳过这些节点，避免与槽位引用重复绑定。
        /// </summary>
        public static void CollectOwnedChildren(GameObject go, HashSet<GameObject> ownedChildren)
        {
            var providers = go.GetComponents<IEUIExposedChildProvider>();
            if (providers == null || providers.Length == 0) return;

            foreach (var provider in providers)
            {
                var owned = provider.GetOwnedChildren();
                if (owned == null) continue;
                foreach (var comp in owned)
                {
                    if (comp != null)
                        ownedChildren.Add(comp.gameObject);
                }
            }
        }

        /// <summary>验证 binding 的合法性</summary>
        public static EUIBindingValidationResult ValidateBinding(EUIBinding binding)
        {
            var result = new EUIBindingValidationResult();
            if (!binding)
            {
                result.AddIssue(EUIBindingIssueSeverity.Error, null, "EUIBinding 为空。");
                return result;
            }
            result.AssetPath = GetPrefabAssetPath(binding);
            ValidatePageInfo(binding, result);
            ValidateEntries(binding, result);
            ValidateBaseBinding(binding, result);
            return result;
        }

        /// <summary>生成或更新 EUIPageDef</summary>
        public static bool GenerateOrUpdatePageDef(EUIBinding binding,
            CSharpLogicImplementationData implementationData = null)
        {
            if (!binding) throw new ArgumentNullException(nameof(binding));
            if (!binding.IsPage) return true;
            implementationData = implementationData ?? FindCSharpLogicImplementationData();
            if (!implementationData)
            {
                EmberDebug.LogError(TAG, "未找到 CSharpLogicImplementationData。请检查 Project Settings/EUI Binding 中的逻辑实现数据。");
                return false;
            }
            bool embedded = EUIBindingCodeGenUtility.IsEmbeddedPackage();
            if (binding.PathMode == EUIBinding.CodePathMode.Framework && !embedded)
            {
                EmberDebug.LogError(TAG, "消费端项目不允许为 Framework 页面生成 EUIPageDef，请改用用户模式。");
                return false;
            }
            bool frameworkMode = binding.PathMode == EUIBinding.CodePathMode.Framework && embedded;
            return implementationData.GenerateOrUpdatePageDefinition(binding, frameworkMode);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法（编辑器辅助）

        /// <summary>
        /// 根据 PageType 设置 Canvas.sortingOrder，方便非 Play 模式下查看层级效果。
        /// 标准页面创建器与本方法共用 <see cref="GetDefaultSortingOrder"/> 映射。
        /// </summary>
        public static void ApplyCanvasSortingOrder(EUIBinding binding)
        {
            if (binding == null || !binding.IsPage) return;

            var canvas = binding.GetComponent<Canvas>();
            if (canvas == null) return;

            int order = GetDefaultSortingOrder(binding.PageType);
            if (order < 0) return;

            canvas.sortingOrder = order;
            EditorUtility.SetDirty(canvas);
            PrefabUtility.SavePrefabAsset(binding.gameObject);
        }

        /// <summary>获取页面类型在编辑态预览使用的默认 sortingOrder。</summary>
        public static int GetDefaultSortingOrder(PageType pageType)
        {
            return pageType switch
            {
                PageType.Background => (int)UILayer.Background,
                PageType.MainPage => (int)UILayer.Normal,
                PageType.Popup => (int)UILayer.Popup,
                PageType.FullScreenPopup => (int)UILayer.Popup,
                PageType.TopMost => (int)UILayer.TopMost,
                PageType.SubPage => (int)UILayer.Normal,
                PageType.FreePage => 30000,
                _ => -1,
            };
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static CSharpLogicImplementationData FindCSharpLogicImplementationData()
        {
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.LogicImplementations == null) return null;
            foreach (var implementation in settings.LogicImplementations)
            {
                if (implementation is CSharpLogicImplementationData csharp)
                    return csharp;
            }
            return null;
        }

        private static void CollectBindingsRecursive(EUIBinding binding, SerializedProperty bp,
            Dictionary<GameObject, GameObject> definedObjects, HashSet<string> definedNames, Transform transform,
            HashSet<GameObject> ownedChildren)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var childGo = child.gameObject;

                // 收集当前节点增强组件通过槽位持有的子节点（这些子节点不再单独绑定）
                CollectOwnedChildren(childGo, ownedChildren);

                // 跳过被增强组件槽位持有的子节点
                if (ownedChildren.Contains(childGo))
                    continue;

                // 跳过被 EUIBindingExclude 标记的节点及其子树
                if (childGo.GetComponent<EUIBindingExclude>())
                    continue;

                bool isSubBindingRoot = childGo.GetComponent<EUIBinding>() != null;

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
                    CollectBindingsRecursive(binding, bp, definedObjects, definedNames, child, ownedChildren);
            }
        }

        private static void ValidatePageInfo(EUIBinding binding, EUIBindingValidationResult result)
        {
            if (string.IsNullOrEmpty(binding.ClassPath))
                result.AddIssue(EUIBindingIssueSeverity.Error, null,
                    "classPath 为空。", "填写逻辑路径。");

            if (string.IsNullOrEmpty(binding.ClassName))
                result.AddIssue(EUIBindingIssueSeverity.Error, null,
                    "className 为空。", "填写页面或组件逻辑类名。");

            if (!binding.IsPage) return;

            if (string.IsNullOrEmpty(binding.PageName))
                result.AddIssue(EUIBindingIssueSeverity.Error, null,
                    "页面级 binding 的 pageName 为空。", "pageName 应作为 EUIPageDef 常量名。");

            if (!Enum.IsDefined(typeof(PageType), binding.PageType) || binding.PageType == PageType.Overlay)
                result.AddIssue(EUIBindingIssueSeverity.Error, null,
                    "页面级 binding 的 PageType 无效或暂不受代码生成支持。",
                    "选择 Background、MainPage、Popup、FullScreenPopup、TopMost、SubPage 或 FreePage。");
        }

        private static void ValidateEntries(EUIBinding binding, EUIBindingValidationResult result)
        {
            if (binding.Bindings == null) return;
            var names = new HashSet<string>();
            foreach (var entry in binding.Bindings)
            {
                var bindingPath = entry.GameObject
                    ? GetTransformPath(binding.transform, entry.GameObject.transform) : null;

                if (string.IsNullOrEmpty(entry.Name))
                    result.AddIssue(EUIBindingIssueSeverity.Error, bindingPath,
                        "绑定字段名为空。", "重新收集绑定或手动补齐字段名。");
                else if (!names.Add(entry.Name))
                    result.AddIssue(EUIBindingIssueSeverity.Error, bindingPath,
                        $"绑定字段名重复：{entry.Name}。", "重命名其中一个绑定字段。");

                if (!entry.GameObject)
                {
                    result.AddIssue(EUIBindingIssueSeverity.Error, bindingPath,
                        $"绑定 {entry.Name} 的 GameObject 丢失。", "重新指定节点或删除该绑定。");
                }
            }
        }

        private static void ValidateBaseBinding(EUIBinding binding, EUIBindingValidationResult result)
        {
            var guid = GetBaseBindingGuid(binding);
            if (string.IsNullOrEmpty(guid)) return;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                result.AddIssue(EUIBindingIssueSeverity.Error, null,
                    "baseBindingUUID 指向的 prefab 不存在。", "清空继承配置或重新选择基础 prefab。");
            }
        }

        private static GameObject ResolveGameObject(EUIBinding binding, string relativePath)
        {
            if (!binding) return null;
            if (string.IsNullOrEmpty(relativePath)) return binding.gameObject;
            var transform = binding.transform.Find(relativePath);
            return transform ? transform.gameObject : null;
        }

        private static string GetPrefabAssetPath(EUIBinding binding)
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
            new ComponentTypeRule(EUIBinding.WidgetTypes.UILogic, typeof(EUIBinding)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Button, typeof(Button)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Text, typeof(Text), typeof(TextMeshProUGUI)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMP_InputField)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Image, typeof(Image)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.RawImage, typeof(RawImage)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Canvas, typeof(Canvas)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.CanvasGroup, typeof(CanvasGroup)),
        };

        public static readonly ComponentTypeRule[] AutoSelectExactBuiltInComponentTypeRules =
        {
            new ComponentTypeRule(EUIBinding.WidgetTypes.Button, typeof(Button)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Text, typeof(Text), typeof(TextMeshProUGUI)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMP_InputField)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
            new ComponentTypeRule(EUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
        };

        /// <summary>获取所有组件类型名（内置 + 扩展）</summary>
        public static string[] GetComponentTypeNames()
        {
            if (_typeNames == null)
            {
                _extensionTypeMapping = new Dictionary<string, KeyValuePair<int, KeyValuePair<Type, Type>>>();
                List<string> nameList = new List<string>();
                var names = System.Enum.GetNames(typeof(EUIBinding.WidgetTypes));
                for (int i = 0; i < names.Length - 2; i++)
                    nameList.Add(names[i]);

                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (t.IsGenericTypeDefinition || t.IsAbstract) continue;
                        var arr = t.GetCustomAttributes(typeof(EUIExtensionAttribute), false);
                        if (arr != null && arr.Length > 0)
                        {
                            EUIExtensionAttribute attr = (EUIExtensionAttribute)arr[0];
                            string name = string.IsNullOrEmpty(attr.Name) ? t.Name : attr.Name;
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

        /// <summary>获取扩展类型的完整类型名（含命名空间，代码生成用）。</summary>
        public static string GetExtensionFullTypeName(string className)
        {
            var mapping = GetExtensionTypeMapping();
            if (string.IsNullOrEmpty(className) || !mapping.TryGetValue(className, out var kv))
                return className;
            return kv.Value.Value.FullName;
        }

        /// <summary>
        /// 检测 GameObject 上的组件类型，返回 (类型, 类名)。
        /// 与 AutoSelectByObject 逻辑一致，扩展类型会同时返回扩展显示名作为类名。
        /// </summary>
        public static (EUIBinding.WidgetTypes Type, string ClassName) DetectWidgetType(GameObject go)
        {
            var binding = go.GetComponent<EUIBinding>();
            if (binding)
                return (EUIBinding.WidgetTypes.UILogic, binding.NoCodeGeneration ? null : binding.ClassName);

            foreach (var rule in AutoSelectExactBuiltInComponentTypeRules)
                if (rule.ExactMatches(go))
                    return (rule.WidgetType, null);

            if (TryGetFirstMatchingExtension(go, out var extensionName))
                return (EUIBinding.WidgetTypes.Extension, extensionName);

            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == EUIBinding.WidgetTypes.UILogic) continue;
                if (rule.Matches(go))
                    return (rule.WidgetType, null);
            }

            return (EUIBinding.WidgetTypes.Component, null);
        }

        /// <summary>收集所有已定义的绑定（递归，含子 EUIBinding）</summary>
        public static void GatherBindingDefinitions(EUIBinding binding, Dictionary<GameObject, GameObject> defined, bool recursive = true)
        {
            if (binding.Bindings == null) return;
            foreach (var i in binding.Bindings)
            {
                if (recursive && i.GameObject)
                {
                    EUIBinding child = i.GameObject.GetComponent<EUIBinding>();
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
            var binding = go.GetComponent<EUIBinding>();
            if (binding)
            {
                SetBuiltInWidgetType(type, cn, EUIBinding.WidgetTypes.UILogic);
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
                type.enumValueIndex = (int)EUIBinding.WidgetTypes.End + 1;
                if (cn != null)
                    cn.stringValue = extensionName;
                return;
            }

            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == EUIBinding.WidgetTypes.UILogic) continue;
                if (rule.Matches(go))
                {
                    SetBuiltInWidgetType(type, cn, rule.WidgetType);
                    return;
                }
            }

            SetBuiltInWidgetType(type, cn, EUIBinding.WidgetTypes.Component);
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
            if (index == (int)EUIBinding.WidgetTypes.End + 1)
                index = (int)EUIBinding.WidgetTypes.Extension;
            switch ((EUIBinding.WidgetTypes)index)
            {
                case EUIBinding.WidgetTypes.UILogic:
                    var childBinding = go.GetComponent<EUIBinding>();
                    if (!childBinding)
                    {
                        EditorGUILayout.HelpBox($"{go} 并不包含 EUIBinding 组件", MessageType.Error);
                        invalid = true;
                    }
                    else
                    {
                        SetUILogicClassName(childBinding, cn);
                    }
                    break;
                case EUIBinding.WidgetTypes.Extension:
                    var mapping = GetExtensionTypeMapping();
                    if (mapping.TryGetValue(cn.stringValue, out var ct))
                    {
                        if (!go.GetComponent(ct.Value.Value))
                        {
                            EditorGUILayout.HelpBox($"{go} 并不包含 {ct.Value.Value.Name} 组件", MessageType.Error);
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
                    var widgetType = (EUIBinding.WidgetTypes)index;
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

                if (index == (int)EUIBinding.WidgetTypes.UILogic)
                {
                    if (!string.IsNullOrEmpty(cn.stringValue))
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        cn.stringValue = EditorGUILayout.TextField(cn.stringValue, GUILayout.MinWidth(100));
                        EditorGUI.EndDisabledGroup();
                    }
                }
                if (index < 0) return;

                if (index >= (int)EUIBinding.WidgetTypes.End)
                {
                    var newName = GetComponentTypeNames()[index];
                    type.enumValueIndex = (int)EUIBinding.WidgetTypes.End + 1;
                    cn.stringValue = newName;
                }
                else
                {
                    SetBuiltInWidgetType(type, cn, (EUIBinding.WidgetTypes)index);
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
            res.Add(new ComponentTypeOption { Index = (int)EUIBinding.WidgetTypes.Component, Name = allNames[(int)EUIBinding.WidgetTypes.Component] });

            foreach (var rule in BuiltInComponentTypeRules)
            {
                if (rule.Matches(go))
                    res.Add(new ComponentTypeOption { Index = rule.Index, Name = allNames[rule.Index] });
            }

            var mapping = GetExtensionTypeMapping();
            foreach (var i in mapping)
            {
                if (go.GetComponent(i.Value.Value.Value))
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
            if (index == (int)EUIBinding.WidgetTypes.End + 1)
                index = (int)EUIBinding.WidgetTypes.Extension;
            switch ((EUIBinding.WidgetTypes)index)
            {
                case EUIBinding.WidgetTypes.UILogic:
                    return index;
                case EUIBinding.WidgetTypes.Extension:
                    var mapping = GetExtensionTypeMapping();
                    if (mapping.TryGetValue(cn.stringValue, out var pair))
                        return pair.Key;
                    return -1;
                default:
                    return index;
            }
        }

        private static bool TryGetBuiltInComponentTypeRule(EUIBinding.WidgetTypes widgetType, out ComponentTypeRule result)
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
                // 用扩展类型本身精确匹配，避免同一原生类型（如 Image）下的多个扩展互相误判
                if (go.GetComponent(i.Value.Value.Value)) { extensionName = i.Key; return true; }
            }
            extensionName = null;
            return false;
        }

        private static void SetBuiltInWidgetType(SerializedProperty type, SerializedProperty cn, EUIBinding.WidgetTypes widgetType)
        {
            int previousType = type.enumValueIndex;
            type.enumValueIndex = (int)widgetType;
            if (cn != null && previousType != (int)widgetType && widgetType != EUIBinding.WidgetTypes.UILogic)
                cn.stringValue = null;
        }

        private static void SetUILogicClassName(EUIBinding childBinding, SerializedProperty cn)
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
