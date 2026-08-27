////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System;
////using System.Collections;
////using System.Collections.Generic;
////using System.IO;
////using UnityEngine;
////using UnityEditor;
////using UnityEngine.UI;
////using Burner;
////using System.Linq;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(GameUIBinding))]
////    public class GameUIBindingEditor : UnityEditor.Editor
////    {
////        SerializedProperty pageName, bindings, className, classPath, isPage, pageFlags, selfWidgetType, selfWidgetClassName, noCodeGen, baseBindingUUID;
////        EUIBindingSettingData settings;
////        string[] logicTypeNames;
////        static string[] typeNames;
////        static Dictionary<string, KeyValuePair<int, KeyValuePair<System.Type, System.Type>>> extensionTypeMapping;
////        static int logicIndex = 0;
////        bool needValidation = true;
////        bool hasMissingField = false;
////        bool hasInvalidField = false;
////        int templateSelector = 1;
////        static GameUIBindingTemplate savedTemplate;
////        string classNameForNoGen;
////        string searchTxt;
////        GameObject searchGO;
////        GameObject baseObj;
////        Dictionary<string, GameUIBindingTemplate.BindingEntry> baseTypeFields;
////        private void OnEnable()
////        {
////            isPage = serializedObject.FindProperty("isPage");
////            pageFlags = serializedObject.FindProperty("pageFlags");
////            pageName = serializedObject.FindProperty("pageName");
////            className = serializedObject.FindProperty("className");
////            classPath = serializedObject.FindProperty("classPath");
////            noCodeGen = serializedObject.FindProperty("noCodeGen");
////            selfWidgetType = serializedObject.FindProperty("selfWidgetType");
////            selfWidgetClassName = serializedObject.FindProperty("selfWidgetClassName");
////            bindings = serializedObject.FindProperty("bindings");
////            baseBindingUUID = serializedObject.FindProperty("baseBindingUUID");
////
////            settings = EUIBindingSettingData.GetOrCreateSettings();
////            logicTypeNames = new string[settings.LogicImplementations.Length];
////            for(int i = 0; i < settings.LogicImplementations.Length; i++)
////            {
////                logicTypeNames[i] = settings.LogicImplementations[i].name;
////            }
////            if (logicIndex >= logicTypeNames.Length)
////                logicIndex = logicTypeNames.Length - 1;
////        }
////
////        public static string[] GetComponentTypeNames()
////        {
////            if (typeNames == null)
////            {
////                extensionTypeMapping = new Dictionary<string, KeyValuePair<int, KeyValuePair<System.Type, System.Type>>>();
////                List<string> nameList = new List<string>();
////                var names = System.Enum.GetNames(typeof(GameUIBinding.WidgetTypes));
////                for (int i = 0; i < names.Length - 2; i++)
////                {
////                    nameList.Add(names[i]);
////                }
////
////                foreach(var a in System.AppDomain.CurrentDomain.GetAssemblies())
////                {
////                    foreach(var t in a.GetTypes())
////                    {
////                        if (t.IsGenericTypeDefinition || t.IsAbstract)
////                            continue;
////                        var arr = t.GetCustomAttributes(typeof(BurnerUIExtensionAttribute), false);
////                        if(arr != null && arr.Length > 0)
////                        {
////                            BurnerUIExtensionAttribute attr = (BurnerUIExtensionAttribute)arr[0];
////                            string name = string.IsNullOrEmpty(attr.Name) ? attr.ComponentType.Name : attr.Name;
////                            extensionTypeMapping[name] = new KeyValuePair<int, KeyValuePair<System.Type, System.Type>>(nameList.Count, new KeyValuePair<System.Type, System.Type>(attr.ComponentType, t));
////                            nameList.Add(name);
////                        }
////                    }
////                }
////
////                typeNames = nameList.ToArray();
////            }
////            return typeNames;
////        }
////
////        public static Dictionary<string, KeyValuePair<int, KeyValuePair<System.Type, System.Type>>> GetExtensionTypeMapping()
////        {
////            if (extensionTypeMapping == null)
////                GetComponentTypeNames();
////            return extensionTypeMapping;
////        }
////
////        public static void GatherBindingDefinitions(GameUIBinding binding, Dictionary<GameObject, GameObject> defined, bool recursive = true)
////        {
////            if (binding.Bindings == null)
////                return;
////            foreach(var i in binding.Bindings)
////            {
////                if(recursive && i.GameObject)
////                {
////                    GameUIBinding child = i.GameObject.GetComponent<GameUIBinding>();
////                    if (child == binding)
////                        continue;
////                    if (child)
////                    {
////                        GatherBindingDefinitions(child, defined);
////                    }
////                }
////                if (!defined.ContainsKey(i.GameObject))
////                    defined[i.GameObject] = binding.gameObject;
////            }
////        }
////
////        public static void AutoSelectByObject(GameObject go, SerializedProperty type, SerializedProperty cn)
////        {
////            var binding = go.GetComponent<GameUIBinding>();
////            if (binding)
////            {
////                SetBuiltInWidgetType(type, cn, GameUIBinding.WidgetTypes.UILogic);
////                SetUILogicClassName(binding, cn);
////                return;
////            }
////
////            foreach (var rule in AutoSelectExactBuiltInComponentTypeRules)
////            {
////                if (rule.ExactMatches(go))
////                {
////                    SetBuiltInWidgetType(type, cn, rule.WidgetType);
////                    return;
////                }
////            }
////
////            if (TryGetFirstMatchingExtension(go, out var extensionName))
////            {
////                type.enumValueIndex = (int)GameUIBinding.WidgetTypes.End + 1;
////                if (cn != null)
////                    cn.stringValue = extensionName;
////                return;
////            }
////
////            foreach (var rule in BuiltInComponentTypeRules)
////            {
////                if (rule.WidgetType == GameUIBinding.WidgetTypes.UILogic)
////                {
////                    continue;
////                }
////
////                if (rule.Matches(go))
////                {
////                    SetBuiltInWidgetType(type, cn, rule.WidgetType);
////                    return;
////                }
////            }
////
////            SetBuiltInWidgetType(type, cn, GameUIBinding.WidgetTypes.Component);
////        }
////
////        public static bool ValidateType(GameObject go, SerializedProperty type, SerializedProperty cn, bool canAutoDetect=true)
////        {
////            bool invalid = false;
////            if (!go)
////            {
////                EditorGUILayout.HelpBox($"绑定对象缺失", MessageType.Error);
////
////                return true;
////            }
////            int index = type.enumValueIndex;
////            if (index == (int)GameUIBinding.WidgetTypes.End + 1)
////                index = (int)GameUIBinding.WidgetTypes.Extension;
////            switch ((GameUIBinding.WidgetTypes)index)
////            {
////                case GameUIBinding.WidgetTypes.UILogic:
////                    var binding = go.GetComponent<GameUIBinding>();
////                    if (!binding)
////                    {
////                        EditorGUILayout.HelpBox($"{go} 并不包含GameUIBinding组件", MessageType.Error);
////                        invalid = true;
////                    }
////                    else
////                    {
////                        SetUILogicClassName(binding, cn);
////                    }
////                    break;
////                case GameUIBinding.WidgetTypes.Extension:
////                    var mapping = GetExtensionTypeMapping();
////                    if(mapping.TryGetValue(cn.stringValue, out var ct))
////                    {
////                        if (!go.GetComponent(ct.Value.Key))
////                        {
////                            EditorGUILayout.HelpBox($"{go} 并不包含{ct.Value.Key.Name}组件", MessageType.Error);
////                            invalid = true;
////                        }
////                    }
////                    else
////                    {
////                        EditorGUILayout.HelpBox($"找不到{go} 指定的扩展组件:{cn.stringValue}", MessageType.Error);
////                        invalid = true;
////                    }
////                    break;
////                default:
////                    var widgetType = (GameUIBinding.WidgetTypes)index;
////                    if (TryGetBuiltInComponentTypeRule(widgetType, out var rule) && !rule.Matches(go))
////                    {
////                        EditorGUILayout.HelpBox($"{go} 并不包含{rule.ComponentTypeNames}组件", MessageType.Error);
////                        invalid = true;
////                    }
////                    break;
////            }
////            if (invalid && canAutoDetect)
////            {
////                if (GUILayout.Button("自动识别类型"))
////                {
////                    AutoSelectByObject(go, type, cn);
////                }
////            }
////            return invalid;
////        }
////
////        static int GetWidgetIndexAndName(SerializedProperty type, SerializedProperty cn)
////        {
////            int index = type.enumValueIndex;
////            if (index == (int)GameUIBinding.WidgetTypes.End + 1)
////                index = (int)GameUIBinding.WidgetTypes.Extension;
////            switch ((GameUIBinding.WidgetTypes)index)
////            {
////                case GameUIBinding.WidgetTypes.UILogic:
////                    return index;
////                case GameUIBinding.WidgetTypes.Extension:
////                    var mapping = GetExtensionTypeMapping();
////                    if (mapping.TryGetValue(cn.stringValue, out var pair))
////                    {
////                        return pair.Key;
////                    }
////                    else
////                        return -1;
////                default:
////                    return index;
////            }
////        }
////
////        public static void DrawComponentType(string typeLabel, SerializedProperty type, SerializedProperty cn, ref bool needValidation, int labelSize = 55, GameObject bindingObject = null)
////        {
////            using (new EditorGUILayout.HorizontalScope())
////            {
////                GUILayout.Label(typeLabel, GUILayout.Width(labelSize));
////
////                int oldIdx = GetWidgetIndexAndName(type, cn);
////                var options = GetAvailableComponentTypeOptions(bindingObject, oldIdx, cn.stringValue);
////                int oldOptionIdx = GetComponentTypeOptionIndex(options, oldIdx);
////                int optionIdx = EditorGUILayout.Popup(oldOptionIdx, options.Select(i => i.Name).ToArray(), GUILayout.Width(100), GUILayout.MinWidth(80));
////                int index = optionIdx >= 0 && optionIdx < options.Count ? options[optionIdx].Index : oldIdx;
////                if (oldOptionIdx != optionIdx)
////                    needValidation = true;
////
////                if (index == (int)(GameUIBinding.WidgetTypes.UILogic))
////                {
////                    if (!string.IsNullOrEmpty(cn.stringValue))
////                    {
////                        EditorGUI.BeginDisabledGroup(true);
////                        cn.stringValue = EditorGUILayout.TextField(cn.stringValue, GUILayout.MinWidth(100));
////                        EditorGUI.EndDisabledGroup();
////                    }
////                }
////                if (index < 0)
////                {
////                    return;
////                }
////
////                if (index >= (int)(GameUIBinding.WidgetTypes.End))
////                {
////                    var newName = GetComponentTypeNames()[index];
////                    type.enumValueIndex = (int)GameUIBinding.WidgetTypes.End + 1;
////                    cn.stringValue = newName;
////                }
////                else
////                {
////                    SetBuiltInWidgetType(type, cn, (GameUIBinding.WidgetTypes)index);
////                }
////            }
////        }
////
////        private struct ComponentTypeOption
////        {
////            public int Index;
////            public string Name;
////        }
////
////        private struct ComponentTypeRule
////        {
////            public GameUIBinding.WidgetTypes WidgetType;
////            public Type[] ComponentTypes;
////
////            public int Index => (int)WidgetType;
////            public string ComponentTypeNames => string.Join("或", ComponentTypes.Select(i => i.Name));
////
////            public ComponentTypeRule(GameUIBinding.WidgetTypes widgetType, params Type[] componentTypes)
////            {
////                WidgetType = widgetType;
////                ComponentTypes = componentTypes;
////            }
////
////            public bool Matches(GameObject go)
////            {
////                for (int i = 0; i < ComponentTypes.Length; i++)
////                {
////                    if (go.GetComponent(ComponentTypes[i]))
////                    {
////                        return true;
////                    }
////                }
////
////                return false;
////            }
////
////            public bool ExactMatches(GameObject go)
////            {
////                var components = go.GetComponents<Component>();
////                for (int i = 0; i < components.Length; i++)
////                {
////                    var component = components[i];
////                    if (!component)
////                    {
////                        continue;
////                    }
////
////                    var componentType = component.GetType();
////                    for (int j = 0; j < ComponentTypes.Length; j++)
////                    {
////                        if (componentType == ComponentTypes[j])
////                        {
////                            return true;
////                        }
////                    }
////                }
////
////                return false;
////            }
////        }
////
////        private static readonly ComponentTypeRule[] BuiltInComponentTypeRules =
////        {
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.UILogic, typeof(GameUIBinding)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Button, typeof(Button)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Text, typeof(Text), typeof(TMPro.TextMeshProUGUI)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMPro.TMP_InputField)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.UIContainer, typeof(UIContainer)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Image, typeof(Image)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.RawImage, typeof(RawImage)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Canvas, typeof(Canvas)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.TabLoader, typeof(TabLoader)),
////        };
////
////        private static readonly ComponentTypeRule[] AutoSelectExactBuiltInComponentTypeRules =
////        {
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Button, typeof(Button)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Text, typeof(Text), typeof(TMPro.TextMeshProUGUI)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.InputField, typeof(InputField), typeof(TMPro.TMP_InputField)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.Toggle, typeof(Toggle)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ProgressBar, typeof(Slider)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.UIContainer, typeof(UIContainer)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ToggleGroup, typeof(ToggleGroup)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.ScrollRect, typeof(ScrollRect)),
////            new ComponentTypeRule(GameUIBinding.WidgetTypes.TabLoader, typeof(TabLoader)),
////        };
////
////        private static List<ComponentTypeOption> GetAvailableComponentTypeOptions(GameObject go, int currentIndex, string currentName)
////        {
////            var allNames = GetComponentTypeNames();
////            if (!go)
////            {
////                return GetAllComponentTypeOptions(allNames);
////            }
////
////            var result = new List<ComponentTypeOption>();
////            AddComponentTypeOption(result, (int)GameUIBinding.WidgetTypes.Component, allNames);
////
////            foreach (var rule in BuiltInComponentTypeRules)
////            {
////                if (rule.Matches(go))
////                {
////                    AddComponentTypeOption(result, rule.Index, allNames);
////                }
////            }
////
////            var mapping = GetExtensionTypeMapping();
////            foreach (var i in mapping)
////            {
////                if (go.GetComponent(i.Value.Value.Key))
////                {
////                    AddComponentTypeOption(result, i.Value.Key, allNames);
////                }
////            }
////
////            if (currentIndex >= 0)
////            {
////                AddComponentTypeOption(result, currentIndex, allNames);
////            }
////            else if (!string.IsNullOrEmpty(currentName))
////            {
////                result.Add(new ComponentTypeOption { Index = -1, Name = currentName });
////            }
////
////            return result;
////        }
////
////        private static bool TryGetBuiltInComponentTypeRule(GameUIBinding.WidgetTypes widgetType, out ComponentTypeRule result)
////        {
////            foreach (var rule in BuiltInComponentTypeRules)
////            {
////                if (rule.WidgetType == widgetType)
////                {
////                    result = rule;
////                    return true;
////                }
////            }
////
////            result = default;
////            return false;
////        }
////
////        private static bool TryGetFirstMatchingExtension(GameObject go, out string extensionName)
////        {
////            var mapping = GetExtensionTypeMapping();
////            foreach (var i in mapping)
////            {
////                if (go.GetComponent(i.Value.Value.Key))
////                {
////                    extensionName = i.Key;
////                    return true;
////                }
////            }
////
////            extensionName = null;
////            return false;
////        }
////
////        private static void SetBuiltInWidgetType(SerializedProperty type, SerializedProperty cn, GameUIBinding.WidgetTypes widgetType)
////        {
////            int previousType = type.enumValueIndex;
////            type.enumValueIndex = (int)widgetType;
////            if (cn != null && previousType != (int)widgetType && widgetType != GameUIBinding.WidgetTypes.UILogic)
////            {
////                cn.stringValue = null;
////            }
////        }
////
////        private static void SetUILogicClassName(GameUIBinding binding, SerializedProperty cn)
////        {
////            if (cn == null)
////            {
////                return;
////            }
////
////            cn.stringValue = binding && !binding.NoCodeGeneration ? binding.ClassName : null;
////        }
////
////        private static List<ComponentTypeOption> GetAllComponentTypeOptions(string[] allNames)
////        {
////            var result = new List<ComponentTypeOption>();
////            for (int i = 0; i < allNames.Length; i++)
////            {
////                result.Add(new ComponentTypeOption { Index = i, Name = allNames[i] });
////            }
////            return result;
////        }
////
////        private static void AddComponentTypeOption(List<ComponentTypeOption> result, int index, string[] allNames)
////        {
////            if (index < 0 || index >= allNames.Length || result.Any(i => i.Index == index))
////            {
////                return;
////            }
////
////            result.Add(new ComponentTypeOption { Index = index, Name = allNames[index] });
////        }
////
////        private static int GetComponentTypeOptionIndex(List<ComponentTypeOption> options, int index)
////        {
////            for (int i = 0; i < options.Count; i++)
////            {
////                if (options[i].Index == index)
////                    return i;
////            }
////
////            return 0;
////        }
////
////        bool IsSearchHit(SerializedProperty sp, string searchTxt, GameObject searchGO)
////        {
////            var name = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name));
////            var go = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject));
////
////            if (!string.IsNullOrEmpty(searchTxt))
////            {
////                if (name.stringValue.ToLower().Contains(searchTxt))
////                    return true;
////            }
////            if (searchGO && go.objectReferenceValue == searchGO)
////                return true;
////            return false;
////        }
////
////        bool DrawBindingEntry(GameObject curGO, Dictionary<GameObject, GameObject> defined, HashSet<GameObject> selfDefined, HashSet<string> selfDefinedName, SerializedProperty sp, out bool hasInvalid)
////        {
////            hasInvalid = false;
////            var name = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name));
////            var go = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject));
////            var type = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type));
////            var cn = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName));
////            bool isInherited = baseTypeFields != null && !string.IsNullOrEmpty(name.stringValue) && baseTypeFields.ContainsKey(name.stringValue) && (selfDefinedName==null || string.IsNullOrEmpty(name.stringValue) || !selfDefinedName.Contains(name.stringValue));
////            using (new EditorGUILayout.HorizontalScope())
////            {
////                using (new EditorGUILayout.VerticalScope())
////                {
////
////                    using (new EditorGUILayout.HorizontalScope())
////                    {
////                        GUILayout.Label("变量绑定", GUILayout.Width(55));
////                        EditorGUI.BeginDisabledGroup(isInherited);
////                        name.stringValue = EditorGUILayout.TextField(name.stringValue, GUILayout.Width(100), GUILayout.MinWidth(80));
////                        EditorGUI.EndDisabledGroup();
////                        var oldObj = go.objectReferenceValue;
////                        go.objectReferenceValue = EditorGUILayout.ObjectField(go.objectReferenceValue, typeof(GameObject), true, GUILayout.MinWidth(100));
////                        bool needAutoSelect = !oldObj;
////                        if (!needAutoSelect && oldObj != go.objectReferenceValue)
////                            needValidation = true;
////                        if (go.objectReferenceValue)
////                        {
////                            //可绑定的节点必须为当前节点的子节点（不包含自己），也不能是其他prefab
////                            if (AssetDatabase.Contains(go.objectReferenceValue))
////                                go.objectReferenceValue = oldObj;
////                            else
////                            {
////                                bool found = false;
////                                Transform curT = ((GameObject)go.objectReferenceValue).transform;
////                                Transform rootT = curGO.transform;
////                                if (curT == rootT)
////                                {
////                                    go.objectReferenceValue = oldObj;
////                                }
////                                else
////                                {
////                                    while (curT)
////                                    {
////                                        if (curT == rootT)
////                                        {
////                                            found = true;
////                                            break;
////                                        }
////                                        curT = curT.parent;
////                                    }
////                                    if (!found)
////                                    {
////                                        go.objectReferenceValue = oldObj;
////                                    }
////                                }
////                            }
////                        }
////                        if (go.objectReferenceValue)
////                        {
////                            if (needAutoSelect || oldObj != go.objectReferenceValue)
////                            {
////                                if (string.IsNullOrEmpty(name.stringValue))
////                                {
////                                    if (settings && settings.LogicImplementations.Length > logicIndex && logicIndex >= 0)
////                                    {
////                                        var logic = settings.LogicImplementations[logicIndex];
////                                        name.stringValue = logic.GetNameForCode(go.objectReferenceValue.name, selfDefinedName);
////                                    }
////                                    else
////                                        name.stringValue = go.objectReferenceValue.name.Replace(" ", "_");
////                                }
////                            }
////                            if (needAutoSelect && !isInherited)
////                            {
////                                AutoSelectByObject((GameObject)go.objectReferenceValue, type, cn);
////                            }
////                        }
////
////                    }
////                    EditorGUI.BeginDisabledGroup(isInherited);
////
////                    DrawComponentType("控件类型", type, cn, ref needValidation, 55, go.objectReferenceValue as GameObject);
////
////                    EditorGUI.EndDisabledGroup();
////                    if (isInherited)
////                    {
////                        EditorGUILayout.ObjectField("继承自", baseObj, typeof(GameObject), false);
////                    }
////                    else
////                    {
////                        if (GUILayout.Button("清空"))
////                        {
////                            name.stringValue = null;
////                            go.objectReferenceValue = null;
////                            type.enumValueIndex = 0;
////                            cn.stringValue = null;
////                        }
////                    }
////
////                    if (go.objectReferenceValue && defined.TryGetValue(go.objectReferenceValue as GameObject, out var definedBy))
////                    {
////                        if (definedBy != curGO)
////                        {
////                            hasInvalid = true;
////                            EditorGUILayout.HelpBox("当前绑定已被其他节点绑定\n请删除重复绑定", MessageType.Error);
////                        }
////                    }
////                    if (go.objectReferenceValue && selfDefined.Contains(go.objectReferenceValue as GameObject))
////                    {
////                        hasInvalid = true;
////                        EditorGUILayout.HelpBox("当前绑定已被其他节点绑定\n请删除重复绑定", MessageType.Error);
////                    }
////                    else
////                    {
////                        if (!string.IsNullOrEmpty(name.stringValue))
////                        {
////                            if (selfDefinedName.Contains(name.stringValue))
////                            {
////                                hasInvalid = true;
////                                EditorGUILayout.HelpBox($"当前变量名{name.stringValue}已被其他节点绑定\n请修改绑定变量名", MessageType.Error);
////                            }
////                            else
////                                selfDefinedName.Add(name.stringValue);
////                        }
////                        selfDefined.Add(go.objectReferenceValue as GameObject);
////                    }
////                }
////                using (new EditorGUILayout.VerticalScope())
////                {
////                    EditorGUI.BeginDisabledGroup(isInherited);
////
////                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Clear"), EditorStyles.miniButtonRight))
////                    {
////                        return true;
////                    }
////                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.miniButtonRight))
////                    {
////                        AutoSelectByObject((GameObject)go.objectReferenceValue, type, cn);
////                    }
////                    EditorGUI.EndDisabledGroup();
////                }
////            }
////
////            if (needValidation)
////            {
////                hasInvalid = hasInvalid || ValidateType((GameObject)go.objectReferenceValue, type, cn, !isInherited);
////            }
////            return false;
////        }
////
////        void ApplyBindingTemplate(GameUIBinding binding, GameUIBindingTemplate template)
////        {
////            noCodeGen.boolValue = template.NoCodeGeneration;
////            isPage.boolValue = template.IsPage;
////            pageName.stringValue = template.PageName;
////            classPath.stringValue = template.ClassPath;
////            className.stringValue = template.ClassName;
////            selfWidgetType.enumValueIndex = template.SelfWidgetType > GameUIBinding.WidgetTypes.End ? (int)(GameUIBinding.WidgetTypes.End + 1) : (int)template.SelfWidgetType;
////            selfWidgetClassName.stringValue = template.SelfWidgetClassName;
////
////            bindings.ClearArray();
////            for(int i = 0; i < template.Bindings.Length; i++)
////            {
////                var entry = template.Bindings[i];
////                bindings.InsertArrayElementAtIndex(i);
////                var sp = bindings.GetArrayElementAtIndex(i);
////                var name = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name));
////                var go = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject));
////                var type = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type));
////                var cn = sp.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName));
////
////                name.stringValue = entry.Name;
////                cn.stringValue = entry.ClassName;
////                type.enumValueIndex = entry.Type > GameUIBinding.WidgetTypes.End ? (int)(GameUIBinding.WidgetTypes.End + 1) : (int)entry.Type;
////                var t = binding.transform.Find(entry.GameObjectPath);
////                go.objectReferenceValue = t ? t.gameObject : null;
////            }
////
////            serializedObject.ApplyModifiedProperties();
////        }
////
////        void DrawInheritance(out GameUIBinding baseBinding)
////        {
////            baseBinding = null;
////            GameUIBinding binding = target as GameUIBinding;
////            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////            {
////                GUILayout.Label("继承");
////                if(!string.IsNullOrEmpty(baseBindingUUID.stringValue))
////                {
////                    if (!baseObj)
////                    {
////                        baseObj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(baseBindingUUID.stringValue));
////                    }
////
////                    if (!baseObj)
////                    {
////                        baseBindingUUID.stringValue = null;
////                    }
////                    else
////                    {
////                        var newBaseObj = EditorGUILayout.ObjectField(new GUIContent("基类Prefab"), baseObj, typeof(GameObject), false) as GameObject;
////                        if(newBaseObj != baseObj)
////                        {
////                            baseTypeFields = null;
////                            baseObj = newBaseObj;
////                        }
////                        string path = AssetDatabase.GetAssetPath(baseObj);
////                        if (string.IsNullOrEmpty(path))
////                        {
////                            baseBindingUUID.stringValue = null;
////                            baseObj = null;
////                        }
////                        else
////                        {
////                            if (LogicImplementationData.TryGetPrefabName(binding, out var curPref) && curPref == System.IO.Path.GetFileName(path))
////                            {
////                                baseBindingUUID.stringValue = null;
////                                baseObj = null;
////                            }
////                            else
////                            {
////                                string uuid = AssetDatabase.AssetPathToGUID(path);
////                                if (string.IsNullOrEmpty(uuid))
////                                {
////                                    baseBindingUUID.stringValue = null;
////                                    baseObj = null;
////                                }
////                                else
////                                {
////                                    baseBindingUUID.stringValue = uuid;
////                                }
////                            }
////                        }
////
////
////                        if (baseObj)
////                        {
////                            baseBinding = baseObj.GetComponent<GameUIBinding>();
////                            if (!baseBinding)
////                            {
////                                baseBindingUUID.stringValue = null;
////                                baseObj = null;
////                            }
////                            else
////                            {
////                                if (baseTypeFields == null)
////                                {
////                                    baseTypeFields = new Dictionary<string, GameUIBindingTemplate.BindingEntry>();
////                                    foreach (var i in baseBinding.Bindings)
////                                    {
////                                        var entry = GameUIBindingTemplate.BindingEntryToTemplate(i, baseObj);
////                                        baseTypeFields[entry.Name] = entry;
////                                    }
////
////                                }
////                                EditorGUI.BeginDisabledGroup(true);
////                                EditorGUILayout.Toggle("是否为Page", baseBinding.IsPage, GUILayout.MinWidth(100));
////                                if (baseBinding.IsPage)
////                                    EditorGUILayout.TextField("页面名", baseBinding.PageName, GUILayout.MinWidth(100));
////                                EditorGUILayout.TextField("基类路径名", baseBinding.ClassPath, GUILayout.MinWidth(100));
////                                EditorGUI.EndDisabledGroup();
////
////                                Dictionary<string, GameUIBindingTemplate.BindingEntry> verifyDic = new Dictionary<string, GameUIBindingTemplate.BindingEntry>();
////                                foreach(var i in baseTypeFields)
////                                {
////                                    verifyDic[i.Key] = i.Value;
////                                }
////                                foreach (var i in binding.Bindings)
////                                {
////                                    verifyDic.Remove(i.Name);
////                                }
////
////                                if (verifyDic.Count > 0)
////                                {
////                                    hasMissingField = true;
////                                    EditorGUILayout.HelpBox($"当前缺少{verifyDic.Count}个基类定义的绑定\n请点击下方的修复按钮进行修复", MessageType.Error);
////                                    if (GUILayout.Button("自动添加缺少的绑定", GUILayout.Width(150)))
////                                    {
////                                        //自动添加缺少的绑定字段
////                                        foreach (var i in verifyDic)
////                                        {
////                                            int idx = bindings.arraySize;
////                                            bindings.InsertArrayElementAtIndex(bindings.arraySize);
////                                            var child = bindings.GetArrayElementAtIndex(idx);
////                                            child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name)).stringValue = i.Key;
////                                            var bindGO = binding.transform.Find(i.Value.GameObjectPath);
////                                            child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject)).objectReferenceValue = bindGO ? bindGO.gameObject : null;
////                                            child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type)).enumValueIndex = (int)i.Value.Type;
////                                            child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName)).stringValue = i.Value.ClassName;
////                                        }
////                                        needValidation = true;
////                                    }
////                                }
////                                else
////                                    hasMissingField = false;
////                            }
////                        }
////                    }
////                }
////                else
////                {
////                    var newBaseObj = EditorGUILayout.ObjectField(new GUIContent("基类Prefab"), baseObj, typeof(GameObject), false) as GameObject;
////                    if (newBaseObj != baseObj)
////                    {
////                        baseTypeFields = null;
////                        baseObj = newBaseObj;
////                    }
////                    if (baseObj)
////                    {
////                        string path = AssetDatabase.GetAssetPath(baseObj);
////                        if (string.IsNullOrEmpty(path))
////                        {
////                            baseObj = null;
////                        }
////                        else
////                        {
////                            string uuid = AssetDatabase.AssetPathToGUID(path);
////                            if (string.IsNullOrEmpty(uuid))
////                            {
////                                baseObj = null;
////                            }
////                            else
////                            {
////                                baseBindingUUID.stringValue = uuid;
////                            }
////                        }
////                    }
////                    else
////                    {
////                        hasMissingField = false;
////                        baseBindingUUID.stringValue = null;
////                    }
////                }
////            }
////        }
////
////        void DrawGenNoCode(GameUIBinding binding, bool complete)
////        {
////            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////            {
////                EditorGUILayout.BeginHorizontal();
////                GUILayout.Label("代码生成");
////                if (complete)
////                {
////                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_SettingsIcon"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
////                    {
////                        SettingsService.OpenProjectSettings("Project/BurnerUIBindingSetting");
////                    }
////                }
////                EditorGUILayout.EndHorizontal();
////                if (settings.LogicImplementations.Length > 0)
////                {
////                    LogicImplementationData logic;
////                    if (complete)
////                    {
////                        logicIndex = EditorGUILayout.Popup("逻辑实现", logicIndex, logicTypeNames);
////                        logic = settings.LogicImplementations[logicIndex];
////                        logic.DrawNoCodeGenerationSettings(binding);
////                    }
////                    else
////                        logic = settings.LogicImplementations[logicIndex];
////                    if (logic.CanGenerateForNoGen(binding))
////                    {
////                        using (new EditorGUILayout.HorizontalScope())
////                        {
////                            classNameForNoGen = EditorGUILayout.TextField("类名", classNameForNoGen);
////                            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(classNameForNoGen)))
////                            {
////                                if (GUILayout.Button("生成到剪贴板", GUILayout.Width(90)))
////                                {
////                                    if (hasInvalidField)
////                                    {
////                                        EditorUtility.DisplayDialog("错误", "当前存在无效的绑定，请修正后再生成", "确认");
////                                    }
////                                    else
////                                        logic.GenerateCodeForNoGen(binding, classNameForNoGen);
////                                }
////                            }
////                        }
////                    }
////                    else
////                    {
////                        EditorGUILayout.HelpBox("代码生成相关配置有误，请点击上方齿轮按钮确认相关配置", MessageType.Warning);
////                    }
////                }
////                else
////                    EditorGUILayout.HelpBox("请先在设置界面设置项目所使用的逻辑实现\n点击上方齿轮按钮进入设置界面", MessageType.Error);
////
////            }
////        }
////
////        GameUIBinding.BindingEntry[] GetDeclaredFields(GameUIBinding binding)
////        {
////            if (baseTypeFields == null)
////                return null;
////            HashSet<string> res = new HashSet<string>();
////            foreach(var i in binding.Bindings)
////            {
////                res.Add(i.Name);
////            }
////
////            foreach(var i in baseTypeFields)
////            {
////                res.Remove(i.Key);
////            }
////            List<GameUIBinding.BindingEntry> list = new List<GameUIBinding.BindingEntry>();
////            foreach(var i in binding.Bindings)
////            {
////                if (res.Contains(i.Name))
////                    list.Add(i);
////            }
////            return list.ToArray();
////        }
////
////        void DrawGenCode(GameUIBinding binding, GameUIBinding baseBinding, bool complete)
////        {
////           using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////            {
////                EditorGUILayout.BeginHorizontal();
////                GUILayout.Label("代码生成");
////                if (complete)
////                {
////                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_SettingsIcon"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
////                    {
////                        SettingsService.OpenProjectSettings("Project/BurnerUIBindingSetting");
////                    }
////                }
////                EditorGUILayout.EndHorizontal();
////                if (settings.LogicImplementations.Length > 0)
////                {
////                    LogicImplementationData logic;
////                    if (complete)
////                    {
////                        logicIndex = EditorGUILayout.Popup("逻辑实现", logicIndex, logicTypeNames);
////                    }
////                    logic = settings.LogicImplementations[logicIndex];
////                    logic.DrawGenerateSettings(binding);
////
////                    if (logic.CanGenerate(binding))
////                    {
////                        GUILayout.Label("代码生成路径:");
////                        string codePath = logic.GetCodeFilePath(binding.ClassPath);
////                        EditorGUI.indentLevel++;
////                        var scriptObj = File.Exists(codePath) ? AssetDatabase.LoadAssetAtPath<MonoScript>(codePath) : null;
////                        EditorGUILayout.ObjectField("逻辑脚本", scriptObj, typeof(MonoScript), false);
////                        if (EditorGUILayout.LinkButton(codePath) && scriptObj)
////                        {
////                            var tempObj = Selection.activeObject;
////                            Selection.activeObject = scriptObj;
////                            EditorApplication.ExecuteMenuItem("Assets/Open");
////                            Selection.activeObject = tempObj;
////                            EditorGUIUtility.PingObject(scriptObj);
////                        }
////                        // GUILayout.Label(codePath, EditorStyles.wordWrappedLabel);
////                        EditorGUI.indentLevel--;
////                        if (System.IO.File.Exists(codePath))
////                        {
////                            EditorGUI.BeginDisabledGroup(!logic.IsNeedRegenerate(binding));
////                            if (GUILayout.Button("重新生成"))
////                            {
////                                if (hasInvalidField)
////                                {
////                                    EditorUtility.DisplayDialog("错误", "当前存在无效的绑定，请修正后再生成", "确认");
////                                }
////                                else
////                                {
////                                    GameUIBinding.BindingEntry[] declearedFields = null;
////                                    string baseClsName = null;
////                                    if (baseBinding != null)
////                                    {
////                                        declearedFields = GetDeclaredFields(binding);
////                                        baseClsName = baseBinding.ClassName;
////                                    }
////
////                                    logic.GenerateCode(binding, baseClsName, declearedFields);
////                                }
////                            }
////                            EditorGUI.EndDisabledGroup();
////                        }
////                        else
////                        {
////                            if (GUILayout.Button("生成"))
////                            {
////                                if (hasInvalidField)
////                                {
////                                    EditorUtility.DisplayDialog("错误", "当前存在无效的绑定，请修正后再生成", "确认");
////                                }
////                                else
////                                {
////                                    GameUIBinding.BindingEntry[] declearedFields = null;
////                                    string baseClsName = null;
////                                    if (baseBinding != null)
////                                    {
////                                        declearedFields = GetDeclaredFields(binding);
////                                        baseClsName = baseBinding.ClassName;
////                                    }
////
////                                    logic.GenerateCode(binding, baseClsName, declearedFields);
////                                }
////                            }
////
////                        }
////                    }
////                    else
////                    {
////                        EditorGUILayout.HelpBox("代码生成相关配置有误，请点击上方齿轮按钮确认相关配置", MessageType.Warning);
////                    }
////                    if (complete)
////                        className.stringValue = logic.GetCodeClassName(classPath.stringValue);
////                }
////                else
////                    EditorGUILayout.HelpBox("请先在设置界面设置项目所使用的逻辑实现\n点击上方齿轮按钮进入设置界面", MessageType.Error);
////            }
////        }
////
////        public override void OnInspectorGUI()
////        {
////            GameUIBinding binding = target as GameUIBinding;
////            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////            {
////                GUILayout.Label("模板");
////                using (new EditorGUILayout.HorizontalScope())
////                {
////                    if (GUILayout.Button("加载", GUILayout.Width(50)))
////                    {
////                        EditorGUIUtility.ShowObjectPicker<GameUIBindingTemplate>(null, false, null, templateSelector);
////                    }
////                    if (Event.current.commandName == "ObjectSelectorClosed")
////                    {
////                        if (EditorGUIUtility.GetObjectPickerControlID() == templateSelector)
////                        {
////                            templateSelector++;
////                            var template = EditorGUIUtility.GetObjectPickerObject() as GameUIBindingTemplate;
////                            if (template)
////                            {
////                                if (EditorUtility.DisplayDialog("确认", "加载模板会覆盖当前GameUIBinding上的所有信息，继续吗？", "是", "否"))
////                                {
////                                    ApplyBindingTemplate(binding, template);
////                                }
////                            }
////                        }
////                    }
////                    if (GUILayout.Button("保存", GUILayout.Width(50)))
////                    {
////                        string folder = settings.DefaultBindingTemplatePath ? AssetDatabase.GetAssetPath(settings.DefaultBindingTemplatePath) : "Assets";
////                        string path = EditorUtility.SaveFilePanel("请选择模板要保存的路径", folder, System.IO.Path.GetFileNameWithoutExtension(binding.ClassPath), "asset");
////                        path = "Assets" + path.Replace(Application.dataPath, "");
////                        if (!string.IsNullOrEmpty(path))
////                        {
////                            var template = ScriptableObject.CreateInstance<GameUIBindingTemplate>();
////                            template.CopyFromUIBinding(binding);
////                            AssetDatabase.CreateAsset(template, path);
////                            AssetDatabase.SaveAssets();
////                        }
////                    }
////                    if (GUILayout.Button("复制", GUILayout.Width(50)))
////                    {
////                        if (savedTemplate)
////                            DestroyImmediate(savedTemplate);
////                        var template = ScriptableObject.CreateInstance<GameUIBindingTemplate>();
////                        template.CopyFromUIBinding(binding);
////                        savedTemplate = template;
////                    }
////                    using (new EditorGUI.DisabledGroupScope(!savedTemplate))
////                    {
////                        if (GUILayout.Button("粘贴", GUILayout.Width(50)))
////                        {
////                            ApplyBindingTemplate(binding, savedTemplate);
////                        }
////                    }
////                }
////            }
////            DrawInheritance(out GameUIBinding baseBinding);
////
////            EditorGUILayout.PropertyField(noCodeGen, new GUIContent("不生成代码到文件"));
////            if (noCodeGen.boolValue)
////            {
////                DrawGenNoCode(binding, true);
////            }
////            else
////            {
////                EditorGUILayout.PropertyField(isPage, new GUIContent("是否为Page"));
////                if (baseBinding && baseBinding.IsPage != isPage.boolValue)
////                {
////                    EditorGUILayout.HelpBox("Page和非Page对象无法相互继承\n请确认基类是否为Page的设置是否一致", MessageType.Error);
////                }
////                if (isPage.boolValue)
////                {
////                    EditorGUILayout.PropertyField(pageName, new GUIContent("页面名"));
////                    if (string.IsNullOrEmpty(pageName.stringValue))
////                    {
////                        EditorGUILayout.HelpBox("请输入一个页面名", MessageType.Error, true);
////                    }
////                    else if(baseBinding && baseBinding.PageName == pageName.stringValue)
////                    {
////                        EditorGUILayout.HelpBox("页面名不能和基类的页面名一致\n请确认页面名设置", MessageType.Error);
////                    }
////
////                    // GUILayout.BeginHorizontal();
////                    // GUILayout.Label("界面类型");
////                    // GUILayout.EndHorizontal();
////                    // if(GUILayout.Button("界面类型"))
////
////
////                    pageFlags.enumValueFlag = (int)(PageFlags)EditorGUILayout.EnumPopup("页面类型PageFlags", (PageFlags)pageFlags.enumValueFlag);
////                }
////
////                EditorGUILayout.PropertyField(classPath, new GUIContent("逻辑类"));
////                if (string.IsNullOrEmpty(classPath.stringValue) || string.IsNullOrEmpty(System.IO.Path.GetFileNameWithoutExtension(classPath.stringValue)))
////                {
////                    EditorGUILayout.HelpBox("请输入一个逻辑类的路径\n例如：功能模块名字/页面类名", MessageType.Error, true);
////                }
////                else
////                {
////                    if (baseBinding && baseBinding.ClassName == className.stringValue)
////                    {
////                        EditorGUILayout.HelpBox("类名不能和基类类名一致\n请确认类名设置", MessageType.Error);
////                    }
////
////                    DrawGenCode(binding, baseBinding, true);
////                }
////            }
////            EditorGUILayout.Separator();
////            bool tmp = false;
////            DrawComponentType("自身控件类型", selfWidgetType, selfWidgetClassName, ref tmp, 85);
////            if (selfWidgetType.enumValueIndex == (int)GameUIBinding.WidgetTypes.UILogic)
////            {
////                EditorGUILayout.HelpBox("自身控件类型不能为UI Logic", MessageType.Error);
////            }
////            else
////                ValidateType(binding.gameObject, selfWidgetType, selfWidgetClassName);
////            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////            {
////                GUILayout.Label("搜索");
////                searchTxt = EditorGUILayout.TextField("按名称搜索", searchTxt);
////                searchGO = EditorGUILayout.ObjectField("按节点搜索", searchGO, typeof(GameObject), true) as GameObject;
////                if (GUILayout.Button("清除"))
////                {
////                    searchTxt = null;
////                    searchGO = null;
////                }
////            }
////
////            int newSize;
////            bool needSearch = false;
////            string searchTxtLower = null;
////            if (string.IsNullOrEmpty(searchTxt) && !searchGO)
////            {
////                newSize = EditorGUILayout.DelayedIntField("绑定数", bindings.arraySize);
////                if (newSize != bindings.arraySize)
////                {
////                    bindings.arraySize = newSize;
////                }
////            }
////            else
////            {
////                newSize = bindings.arraySize;
////                needSearch = true;
////                searchTxtLower = searchTxt != null ? searchTxt.ToLower() : null;
////            }
////
////            bool valid = true;
////            bool hasSearchRes = false;
////
////
////            Dictionary<GameObject, GameObject> defined = new Dictionary<GameObject, GameObject>();
////            HashSet<GameObject> selfDefined = new HashSet<GameObject>();
////            HashSet<string> selfDefinedName = new HashSet<string>();
////            GatherBindingDefinitions(binding, defined);
////            var bindingGO = binding.gameObject;
////            for (int i = 0; i < newSize; i++)
////            {
////                var child = bindings.GetArrayElementAtIndex(i);
////                if (needSearch)
////                {
////                    if (!IsSearchHit(child, searchTxtLower, searchGO))
////                        continue;
////                    hasSearchRes = true;
////                }
////                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
////                {
////                    EditorGUI.indentLevel++;
////                    if (DrawBindingEntry(bindingGO, defined, selfDefined, selfDefinedName, child, out bool hasInvalid))
////                    {
////                        bindings.DeleteArrayElementAtIndex(i);
////                        i--;
////                        newSize--;
////                    }
////                    if (hasInvalid)
////                        valid = false;
////                    EditorGUI.indentLevel--;
////                }
////            }
////            if (needValidation && valid)
////                needValidation = false;
////            hasInvalidField = !valid;
////            if (needSearch)
////            {
////                if (!hasSearchRes)
////                {
////                    EditorGUILayout.HelpBox("找不到符合条件的绑定", MessageType.Info);
////                }
////            }
////            else
////            {
////                if (GUILayout.Button("添加绑定"))
////                {
////                    int idx = bindings.arraySize;
////                    bindings.InsertArrayElementAtIndex(bindings.arraySize);
////                    var child = bindings.GetArrayElementAtIndex(idx);
////                    child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Name)).stringValue = null;
////                    child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.GameObject)).objectReferenceValue = null;
////                    child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.Type)).enumValueIndex = 0;
////                    child.FindPropertyRelative(nameof(GameUIBinding.BindingEntry.ClassName)).stringValue = null;
////
////                }
////            }
////
////            if (noCodeGen.boolValue)
////            {
////                DrawGenNoCode(binding, false);
////            }
////            else
////            {
////                if (!string.IsNullOrEmpty(classPath.stringValue) && !string.IsNullOrEmpty(System.IO.Path.GetFileNameWithoutExtension(classPath.stringValue)))
////                {
////                    DrawGenCode(binding, baseBinding, false);
////                }
////            }
////            serializedObject.ApplyModifiedProperties();
////        }
////    }
////}
