//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//
//namespace Burner.UIExtension
//{
//    [CustomEditor(typeof(UIContainer))]
//    public class UIContainerEditor : UnityEditor.Editor
//    {
//        private SerializedProperty templateNode,
//            templateType,
//            templateClassName,
//            isPredefined,
//            predefinedChildren,
//            multiTemplateEnabled,
//            templateNodes,
//            disableParentChange,
//            isPredefinedRecycle,
//            predefinedChildrenInfo,
//            disableLayoutGroup,
//            affectingLayoutGroups,
//            ignoreRecycleCheck;
//        bool needValidation = false;
//        private void OnEnable()
//        {
//            templateNode = serializedObject.FindProperty("templateNode");
//            templateType = serializedObject.FindProperty("templateType");
//            templateClassName = serializedObject.FindProperty("templateClassName");
//            isPredefined = serializedObject.FindProperty("isPredefined");
//            predefinedChildren = serializedObject.FindProperty("predefinedChildren");
//            multiTemplateEnabled = serializedObject.FindProperty("multiTemplateEnabled");
//            templateNodes = serializedObject.FindProperty("templateNodes");
//            disableParentChange = serializedObject.FindProperty("disableParentChange");
//            isPredefinedRecycle = serializedObject.FindProperty("isPredefinedRecycle");
//            predefinedChildrenInfo = serializedObject.FindProperty("predefinedChildrenInfo");
//            disableLayoutGroup = serializedObject.FindProperty("disableLayoutGroup");
//            affectingLayoutGroups = serializedObject.FindProperty("affectingLayoutGroups");
//            ignoreRecycleCheck = serializedObject.FindProperty("ignoreRecycleCheck");
//        }
//
//        void AddChildInfo(GameObject go)
//        {
//            var rt = go.transform as RectTransform;
//            int idx = predefinedChildrenInfo.arraySize;
//            predefinedChildrenInfo.InsertArrayElementAtIndex(idx);
//            var sp = predefinedChildrenInfo.GetArrayElementAtIndex(idx);
//            var name = sp.FindPropertyRelative("Name");
//            var pos = sp.FindPropertyRelative("Position");
//            var size = sp.FindPropertyRelative("Size");
//            name.stringValue = go.name;
//            pos.vector2Value = rt.anchoredPosition;
//            size.vector2Value = rt.sizeDelta;
//        }
//
//        void ConvertToPredifinedRecycle()
//        {
//            if (isPredefinedRecycle.boolValue)
//                return;
//            if (predefinedChildren.arraySize > 0)
//            {
//                string prefabPath = null;
//                for (int i = 0; i < predefinedChildren.arraySize; i++)
//                {
//                    var child = predefinedChildren.GetArrayElementAtIndex(i);
//                    if (!child.objectReferenceValue)
//                    {
//                        EditorUtility.DisplayDialog("错误", $"{i}号子节点的对象为空", "确定");
//                        return;
//                    }
//                    else
//                    {
//                        var go = child.objectReferenceValue as GameObject;
//                        var rt = go.transform as RectTransform;
//                        if(!rt)
//                        {
//                            EditorUtility.DisplayDialog("错误", $"{i}号子节点的Transform不是RectTransform", "确定");
//                            return;
//                        }
//                        var status = PrefabUtility.GetPrefabInstanceStatus(go);
//                        if (status != PrefabInstanceStatus.Connected)
//                        {
//                            EditorUtility.DisplayDialog("错误", $"{i}号子节点不是一个Prefab", "确定");
//                            return;
//                        }
//
//                        var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(go);
//                        if (prefab != go)
//                        {
//                            EditorUtility.DisplayDialog("错误", $"{i}号子节点不是一个Prefab", "确定");
//                            return;
//                        }
//                        var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
//                        if (string.IsNullOrEmpty(prefabPath))
//                            prefabPath = path;
//                        else if (prefabPath != path)
//                        {
//                            EditorUtility.DisplayDialog("错误", "所有子节点都必须使用同一个Prefab", "确定");
//                            return;
//                        }
//                    }
//                }
//
//                if (EditorUtility.DisplayDialog("请确认", "转换成循环模式会删除所有子节点，并且该操作无法被撤销，继续吗？", "是", "否"))
//                {
//                    predefinedChildrenInfo.ClearArray();
//                    for (int i = 0; i < predefinedChildren.arraySize; i++)
//                    {
//                        var child = predefinedChildren.GetArrayElementAtIndex(i);
//                        var go = child.objectReferenceValue as GameObject;
//                        bool needDestory = true;
//                        if (i == 0)
//                        {
//                            templateNode.objectReferenceValue = go;
//                            multiTemplateEnabled.boolValue = false;
//                            needDestory = false;
//                        }
//
//                        AddChildInfo(go);
//
//                        if (needDestory)
//                            DestroyImmediate(go);
//                        else
//                            go.name = "Template";
//                    }
//                    predefinedChildren.ClearArray();
//                    isPredefinedRecycle.boolValue = true;
//                }
//            }
//            else
//            {
//                EditorUtility.DisplayDialog("错误", "请先添加子节点", "确定");
//            }
//        }
//
//        void ConvertToNormal()
//        {
//            if (!isPredefinedRecycle.boolValue)
//                return;
//            if (!templateNode.objectReferenceValue)
//            {
//                EditorUtility.DisplayDialog("错误", "模板节点为空", "确定");
//                return;
//            }
//
//            var parent = ((UIContainer)target).transform;
//            predefinedChildren.ClearArray();
//            var template = templateNode.objectReferenceValue as GameObject;
//            var templateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(template));
//            for (int i = 0; i < predefinedChildrenInfo.arraySize; i++)
//            {
//                var child = predefinedChildrenInfo.GetArrayElementAtIndex(i);
//                var name = child.FindPropertyRelative("Name").stringValue;
//                var pos = child.FindPropertyRelative("Position").vector2Value;
//                var size = child.FindPropertyRelative("Size").vector2Value;
//                GameObject go;
//                if (i != 0)
//                {
//                    go = PrefabUtility.InstantiatePrefab(templateAsset, parent) as GameObject;
//                    go.transform.SetParent(parent, true);
//                }
//                else
//                    go = template;
//                predefinedChildren.InsertArrayElementAtIndex(i);
//                child = predefinedChildren.GetArrayElementAtIndex(i);
//                child.objectReferenceValue = go;
//                go.name = name;
//                var rt = go.transform as RectTransform;
//                rt.anchorMin = Vector3.zero;
//                rt.anchorMax = Vector3.zero;
//                rt.anchoredPosition = pos;
//                rt.sizeDelta = size;
//            }
//            predefinedChildrenInfo.ClearArray();
//            isPredefinedRecycle.boolValue = false;
//        }
//
//        public override void OnInspectorGUI()
//        {
//            UIContainer container = target as UIContainer;
//            var transform = container.transform;
//            EditorGUILayout.PropertyField(isPredefined, new GUIContent("预定义子节点"));
//            if (isPredefined.boolValue)
//            {
//                if (isPredefinedRecycle.boolValue)
//                {
//                    EditorGUILayout.HelpBox("当前处于预定义子节点的循环模式，\n如果需要修改节点位置，请点击下面的还原按钮，\n修改完毕后再重新转换成循环模式", MessageType.Info);
//                    if(GUILayout.Button("还原成普通模式"))
//                    {
//                        ConvertToNormal();
//                    }
//                    EditorGUI.BeginDisabledGroup(true);
//                    EditorGUILayout.PropertyField(templateNode, new GUIContent("模板节点"));
//                    EditorGUILayout.PropertyField(predefinedChildrenInfo, new GUIContent("子节点"));
//                    using (new EditorGUILayout.HorizontalScope())
//                    {
//                        GameUIBindingEditor.DrawComponentType("子节点类型", templateType, templateClassName, ref needValidation, 85);
//                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
//                        {
//                            GameUIBindingEditor.AutoSelectByObject(predefinedChildren.GetArrayElementAtIndex(0).objectReferenceValue as GameObject, templateType, templateClassName);
//                        }
//                    }
//                    EditorGUI.EndDisabledGroup();
//                }
//                else
//                {
//                    if (GUILayout.Button("转换成循环模式"))
//                        ConvertToPredifinedRecycle();
//                    int oldCnt = predefinedChildren.arraySize;
//                    UnityEngine.Object oldObj = oldCnt > 0 ? predefinedChildren.GetArrayElementAtIndex(0).objectReferenceValue : null;
//                    EditorGUILayout.PropertyField(predefinedChildren, new GUIContent("子节点"));
//
//                    if (GUILayout.Button("收集所有子节点"))
//                    {
//                        HashSet<Object> curChildren = new HashSet<Object>();
//                        for (int i = 0; i < predefinedChildren.arraySize; i++)
//                        {
//                            var child = predefinedChildren.GetArrayElementAtIndex(i);
//                            if (child.objectReferenceValue)
//                                curChildren.Add(child.objectReferenceValue);
//                        }
//
//                        for (int i = 0; i < transform.childCount; i++)
//                        {
//                            var child = transform.GetChild(i).gameObject;
//                            if (!curChildren.Contains(child))
//                            {
//                                var newIdx = predefinedChildren.arraySize;
//                                predefinedChildren.InsertArrayElementAtIndex(newIdx);
//                                var sp = predefinedChildren.GetArrayElementAtIndex(newIdx);
//                                sp.objectReferenceValue = child;
//                            }
//                        }
//                    }
//                    bool needAutoDetect = false;
//                    if (predefinedChildren.arraySize != oldCnt)
//                    {
//                        needAutoDetect = true;
//                        needValidation = true;
//                    }
//                    if (predefinedChildren.arraySize > 0)
//                    {
//                        var newObj = predefinedChildren.GetArrayElementAtIndex(0).objectReferenceValue;
//
//                        if (newObj != oldObj)
//                        {
//                            needAutoDetect = true;
//                        }
//                    }
//                    if (needAutoDetect && predefinedChildren.arraySize > 0)
//                    {
//                        GameUIBindingEditor.AutoSelectByObject(predefinedChildren.GetArrayElementAtIndex(0).objectReferenceValue as GameObject, templateType, templateClassName);
//                    }
//
//                    using (new EditorGUILayout.HorizontalScope())
//                    {
//                        GameUIBindingEditor.DrawComponentType("子节点类型", templateType, templateClassName, ref needValidation, 85);
//                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
//                        {
//                            GameUIBindingEditor.AutoSelectByObject(predefinedChildren.GetArrayElementAtIndex(0).objectReferenceValue as GameObject, templateType, templateClassName);
//                        }
//                    }
//                    if (needValidation)
//                    {
//                        oldCnt = predefinedChildren.arraySize;
//                        bool allValid = true;
//                        for (int i = 0; i < oldCnt; i++)
//                        {
//                            if (GameUIBindingEditor.ValidateType(predefinedChildren.GetArrayElementAtIndex(i).objectReferenceValue as GameObject, templateType, templateClassName, false))
//                            {
//                                allValid = false;
//                            }
//
//                        }
//                        if (allValid)
//                            needValidation = false;
//                    }
//                }
//            }
//            else
//            {
//                EditorGUILayout.PropertyField(multiTemplateEnabled, new GUIContent("多模板"));
//                SerializedProperty tNode = templateNode;
//                if (multiTemplateEnabled.boolValue)
//                {
//                    Object old = null;
//                    if (templateNodes.arraySize > 0)
//                    {
//                        tNode = templateNodes.GetArrayElementAtIndex(0);
//                        old = tNode.objectReferenceValue;
//                    }
//                    EditorGUILayout.PropertyField(templateNodes, new GUIContent("模板节点"));
//                    if (tNode.objectReferenceValue != old)
//                    {
//                        if (tNode.objectReferenceValue)
//                        {
//                            if (AssetDatabase.Contains(tNode.objectReferenceValue))
//                                tNode.objectReferenceValue = old;
//                            else
//                                GameUIBindingEditor.AutoSelectByObject(tNode.objectReferenceValue as GameObject, templateType, templateClassName);
//                        }
//                    }
//                    templateNode.objectReferenceValue = null;
//                }
//                else
//                {
//                    var old = templateNode.objectReferenceValue;
//                    EditorGUILayout.PropertyField(templateNode, new GUIContent("模板节点"));
//                    if (templateNode.objectReferenceValue != old)
//                    {
//                        if (templateNode.objectReferenceValue)
//                        {
//                            if (AssetDatabase.Contains(templateNode.objectReferenceValue))
//                                templateNode.objectReferenceValue = old;
//                            else
//                                GameUIBindingEditor.AutoSelectByObject(templateNode.objectReferenceValue as GameObject, templateType, templateClassName);
//                        }
//                    }
//                    templateNodes.ClearArray();
//                }
//
//                using (new EditorGUILayout.HorizontalScope())
//                {
//                    GameUIBindingEditor.DrawComponentType("模板类型", templateType, templateClassName, ref needValidation);
//                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.miniButtonRight, GUILayout.Width(20)))
//                    {
//                        GameUIBindingEditor.AutoSelectByObject(tNode.objectReferenceValue as GameObject, templateType, templateClassName);
//                    }
//                }
//                if (needValidation)
//                {
//                    if (!GameUIBindingEditor.ValidateType(tNode.objectReferenceValue as GameObject, templateType, templateClassName))
//                        needValidation = false;
//                }
//                EditorGUILayout.PropertyField(disableParentChange, new GUIContent("低消耗循环模式","开启循环模式后，如果勾选该选项，当子对象划出视野时，该对象不会从容器Transform中移除，而是移动到看不到的位置"));
//                if (disableParentChange.boolValue)
//                {
//                    EditorGUILayout.HelpBox("在循环模式中，不可见的元素不会被隐藏，而是移动到不可见的位置\n再次显示时由于没有SetActive，因此自动播放的动效可能工作不正常，需要自行处理", MessageType.Warning);
//                }
//                EditorGUILayout.PropertyField(disableLayoutGroup, new GUIContent("禁用LayoutGroup模式"));
//                if (disableLayoutGroup.boolValue)
//                {
//                    EditorGUILayout.HelpBox("在循环模式中，禁用LayoutGroup有利于运行时性能\n但是有可能造成布局错误，需要在下面的列表中指定可能影响到的LayoutGroup", MessageType.Warning);
//                    EditorGUILayout.PropertyField(affectingLayoutGroups, new GUIContent("影响到的LayoutGroup"));
//                }                
//                EditorGUILayout.PropertyField(ignoreRecycleCheck, new GUIContent("不进行循环列表检查(慎选)"));
//                
//            }
//            serializedObject.ApplyModifiedProperties();
//        }
//    }
//}
