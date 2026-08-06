// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EmberUIBinding 的自定义 Inspector。
    /// 提供绑定列表管理、类型自动检测、代码生成入口。
    /// </summary>
    [CustomEditor(typeof(EmberUIBinding))]
    public class EmberUIBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _bindingsProp;
        private SerializedProperty _namespaceProp;
        private SerializedProperty _classNameProp;
        private SerializedProperty _outputDirProp;
        private SerializedProperty _baseClassProp;

        private string _searchFilter = "";
        private GameObject _searchObject;

        private void OnEnable()
        {
            _bindingsProp  = serializedObject.FindProperty("_bindings");
            _namespaceProp = serializedObject.FindProperty("_namespaceName");
            _classNameProp = serializedObject.FindProperty("_className");
            _outputDirProp = serializedObject.FindProperty("_outputDirectory");
            _baseClassProp = serializedObject.FindProperty("_baseClassName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── 代码生成设置 ──
            EditorGUILayout.PropertyField(_namespaceProp);
            EditorGUILayout.PropertyField(_classNameProp);

            if (string.IsNullOrEmpty(_classNameProp.stringValue))
            {
                // 自动填充类名 = prefab 名
                var binding = (EmberUIBinding)target;
                var prefabName = binding.gameObject.name;
                _classNameProp.stringValue = "UI" + prefabName.Replace(" ", "_");
            }

            EditorGUILayout.PropertyField(_outputDirProp);
            EditorGUILayout.PropertyField(_baseClassProp);

            EditorGUILayout.Space();

            // ── 生成按钮 ──
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成代码", GUILayout.Height(30)))
            {
                var b = (EmberUIBinding)target;
                if (string.IsNullOrEmpty(b.ClassName))
                {
                    EditorUtility.DisplayDialog("错误", "请先设置类名（ClassName）", "确认");
                }
                else
                {
                    EmberUIBindingGenerator.GenerateSingle(b);
                }
            }

            if (GUILayout.Button("生成并刷新", GUILayout.Height(30)))
            {
                var b = (EmberUIBinding)target;
                if (!string.IsNullOrEmpty(b.ClassName))
                {
                    EmberUIBindingGenerator.GenerateSingle(b);
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ── 搜索 ──
            EditorGUILayout.LabelField("搜索过滤", EditorStyles.boldLabel);
            _searchFilter = EditorGUILayout.TextField("名称", _searchFilter);
            _searchObject = (GameObject)EditorGUILayout.ObjectField("节点", _searchObject, typeof(GameObject), true);

            if (!string.IsNullOrEmpty(_searchFilter) || _searchObject != null)
            {
                if (GUILayout.Button("清除搜索"))
                {
                    _searchFilter = "";
                    _searchObject = null;
                }
            }

            EditorGUILayout.Space();

            // ── 绑定列表 ──
            EditorGUILayout.LabelField($"控件绑定 ({_bindingsProp.arraySize})", EditorStyles.boldLabel);

            int toDelete = -1;
            for (int i = 0; i < _bindingsProp.arraySize; i++)
            {
                var entry = _bindingsProp.GetArrayElementAtIndex(i);
                var nameProp = entry.FindPropertyRelative("Name");
                var targetProp = entry.FindPropertyRelative("Target");
                var typeProp = entry.FindPropertyRelative("Type");
                var classNameProp = entry.FindPropertyRelative("ClassName");

                // 搜索过滤
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(nameProp.stringValue?.ToLower().Contains(_searchFilter.ToLower()) ?? false))
                    continue;
                if (_searchObject != null && targetProp.objectReferenceValue != _searchObject)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // 变量名
                nameProp.stringValue = EditorGUILayout.TextField(
                    nameProp.stringValue, GUILayout.Width(100));

                // 节点引用
                var oldObj = targetProp.objectReferenceValue;
                targetProp.objectReferenceValue = EditorGUILayout.ObjectField(
                    targetProp.objectReferenceValue, typeof(GameObject), true);

                // 自动命名 + 自动检测类型
                if (targetProp.objectReferenceValue != null)
                {
                    var go = (GameObject)targetProp.objectReferenceValue;
                    if (string.IsNullOrEmpty(nameProp.stringValue))
                        nameProp.stringValue = SanitizeName(go.name);

                    if (oldObj != targetProp.objectReferenceValue)
                        AutoDetectType(go, typeProp, classNameProp);
                }

                // 类型下拉
                typeProp.enumValueIndex = (int)(EmberUIBinding.WidgetType)EditorGUILayout.EnumPopup(
                    (EmberUIBinding.WidgetType)typeProp.enumValueIndex, GUILayout.Width(90));

                // Extension 类名
                if ((EmberUIBinding.WidgetType)typeProp.enumValueIndex == EmberUIBinding.WidgetType.Extension)
                {
                    classNameProp.stringValue = EditorGUILayout.TextField(
                        classNameProp.stringValue, GUILayout.MinWidth(100));
                }

                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(25)))
                    toDelete = i;

                EditorGUILayout.EndHorizontal();

                // 验证
                if (targetProp.objectReferenceValue != null)
                {
                    var go = (GameObject)targetProp.objectReferenceValue;
                    var widgetType = (EmberUIBinding.WidgetType)typeProp.enumValueIndex;
                    if (!ValidateWidgetType(go, widgetType))
                    {
                        EditorGUILayout.HelpBox(
                            $"节点缺少 {widgetType} 组件，请检查类型", MessageType.Warning);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (toDelete >= 0)
            {
                _bindingsProp.DeleteArrayElementAtIndex(toDelete);
            }

            // ── 添加按钮 ──
            EditorGUILayout.Space();
            if (GUILayout.Button("+ 添加绑定"))
            {
                var idx = _bindingsProp.arraySize;
                _bindingsProp.InsertArrayElementAtIndex(idx);
                var entry = _bindingsProp.GetArrayElementAtIndex(idx);
                entry.FindPropertyRelative("Name").stringValue = "";
                entry.FindPropertyRelative("Target").objectReferenceValue = null;
                entry.FindPropertyRelative("Type").enumValueIndex = 0;
                entry.FindPropertyRelative("ClassName").stringValue = "";
            }

            // ── 自动收集 ──
            if (GUILayout.Button("自动收集所有子节点"))
            {
                AutoCollectChildren();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region 内部方法

        /// <summary>根据 GameObject 上的组件自动检测 WidgetType</summary>
        private static void AutoDetectType(GameObject go, SerializedProperty typeProp, SerializedProperty classNameProp)
        {
            if (go == null) return;

            // 精确匹配优先级
            if (go.GetComponent<TMP_Text>() != null || go.GetComponent<Text>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Text;
            else if (go.GetComponent<Button>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Button;
            else if (go.GetComponent<Toggle>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Toggle;
            else if (go.GetComponent<ToggleGroup>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.ToggleGroup;
            else if (go.GetComponent<ScrollRect>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.ScrollRect;
            else if (go.GetComponent<Slider>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Slider;
            else if (go.GetComponent<TMP_InputField>() != null || go.GetComponent<InputField>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.InputField;
            else if (go.GetComponent<RawImage>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.RawImage;
            else if (go.GetComponent<Image>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Image;
            else if (go.GetComponent<TMP_Dropdown>() != null || go.GetComponent<Dropdown>() != null)
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Dropdown;
            else
                typeProp.enumValueIndex = (int)EmberUIBinding.WidgetType.Component;
        }

        /// <summary>验证节点是否包含对应组件</summary>
        private static bool ValidateWidgetType(GameObject go, EmberUIBinding.WidgetType type)
        {
            return type switch
            {
                EmberUIBinding.WidgetType.Component   => true,
                EmberUIBinding.WidgetType.Text        => go.GetComponent<TMP_Text>() != null || go.GetComponent<Text>() != null,
                EmberUIBinding.WidgetType.Image       => go.GetComponent<Image>() != null,
                EmberUIBinding.WidgetType.RawImage    => go.GetComponent<RawImage>() != null,
                EmberUIBinding.WidgetType.Button      => go.GetComponent<Button>() != null,
                EmberUIBinding.WidgetType.Toggle      => go.GetComponent<Toggle>() != null,
                EmberUIBinding.WidgetType.ToggleGroup => go.GetComponent<ToggleGroup>() != null,
                EmberUIBinding.WidgetType.InputField  => go.GetComponent<TMP_InputField>() != null || go.GetComponent<InputField>() != null,
                EmberUIBinding.WidgetType.ScrollRect  => go.GetComponent<ScrollRect>() != null,
                EmberUIBinding.WidgetType.Slider      => go.GetComponent<Slider>() != null,
                EmberUIBinding.WidgetType.Dropdown    => go.GetComponent<TMP_Dropdown>() != null || go.GetComponent<Dropdown>() != null,
                EmberUIBinding.WidgetType.Extension   => true,
                _ => false,
            };
        }

        private static string SanitizeName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_");
        }

        /// <summary>自动收集所有直接子节点</summary>
        private void AutoCollectChildren()
        {
            var binding = (EmberUIBinding)target;
            var existingNames = new System.Collections.Generic.HashSet<string>();
            foreach (var b in binding.Bindings)
                if (!string.IsNullOrEmpty(b.Name))
                    existingNames.Add(b.Name);

            _bindingsProp.ClearArray();
            CollectChildrenRecursive(binding.transform, binding.transform, existingNames);
        }

        private void CollectChildrenRecursive(Transform root, Transform current,
            System.Collections.Generic.HashSet<string> existingNames)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                var go = child.gameObject;

                // 跳过纯结构节点（空 GameObject）
                var comps = go.GetComponents<Component>();
                if (comps.Length <= 2) // Transform + maybe RectTransform
                {
                    CollectChildrenRecursive(root, child, existingNames);
                    continue;
                }

                // 跳过已经有 EmberUIBinding 的子节点（它自己管理）
                if (go.GetComponent<EmberUIBinding>() != null)
                    continue;

                var name = SanitizeName(go.name);
                if (existingNames.Contains(name))
                    name = name + "_" + i;

                var idx = _bindingsProp.arraySize;
                _bindingsProp.InsertArrayElementAtIndex(idx);
                var entry = _bindingsProp.GetArrayElementAtIndex(idx);
                entry.FindPropertyRelative("Name").stringValue = name;
                entry.FindPropertyRelative("Target").objectReferenceValue = go;

                var typeProp = entry.FindPropertyRelative("Type");
                var cnProp = entry.FindPropertyRelative("ClassName");
                AutoDetectType(go, typeProp, cnProp);

                existingNames.Add(name);

                // 递归
                CollectChildrenRecursive(root, child, existingNames);
            }
        }

        #endregion
    }
}
