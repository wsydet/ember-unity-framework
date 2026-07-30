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
//using System.IO;
//
//namespace Burner.UIExtension
//{
//    [CustomEditor(typeof(TabLoader))]
//    public class TabLoaderEditor : UnityEditor.Editor
//    {
//        SerializedProperty tabPages, tabPageInfos, prefabMode;
//        bool needValidation = false;
//        private void OnEnable()
//        {
//            tabPages = serializedObject.FindProperty("tabPages");
//            tabPageInfos = serializedObject.FindProperty("tabPageInfos");
//            prefabMode = serializedObject.FindProperty("prefabMode");
//        }
//        public override void OnInspectorGUI()
//        {
//            var loader = target as TabLoader;
//            if (!prefabMode.boolValue)
//            {
//                EditorGUILayout.PropertyField(tabPages, new GUIContent("子页签节点"));
//                if (GUILayout.Button("转换成Prefab模式"))
//                {
//                    tabPageInfos.ClearArray();
//                    int cnt = tabPages.arraySize;
//                    for (int i = 0; i < cnt; i++)
//                    {
//                        var child = tabPages.GetArrayElementAtIndex(i);
//                        var go = child.objectReferenceValue as GameObject;
//                        if (go)
//                        {
//                            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
//                            {
//                                EditorUtility.DisplayDialog("错误",$"{go.name} 不是一个Prefab，无法转换成Prefab模式", "确定");
//                                return;
//                            }
//                            var name = Path.GetFileName(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)).ToLower();
//                            var rt = go.transform as RectTransform;
//                            var anchorMin = rt.anchorMin;
//                            var anchorMax = rt.anchorMax;
//                            var anchoredPos = rt.anchoredPosition;
//                            var pivot = rt.pivot;
//                            var sizeDelta = rt.sizeDelta;
//
//                            var idx = tabPageInfos.arraySize;
//                            tabPageInfos.InsertArrayElementAtIndex(idx);
//                            var info = tabPageInfos.GetArrayElementAtIndex(idx);
//                            var nameSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.PrefabName));
//                            var anchorMinSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.AnchorMin));
//                            var anchorMaxSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.AnchorMax));
//                            var anchoredPositionSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.AnchoredPosition));
//                            var pivotSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.Pivot));
//                            var sizeDeltaSp = info.FindPropertyRelative(nameof(TabLoader.TabInfo.SizeDelta));
//
//                            nameSp.stringValue = name;
//                            anchorMinSp.vector2Value= anchorMin;
//                            anchorMaxSp.vector2Value= anchorMax;
//                            anchoredPositionSp.vector2Value= anchoredPos;
//                            pivotSp.vector2Value = pivot;
//                            sizeDeltaSp.vector2Value= sizeDelta;
//
//
//                            DestroyImmediate(go);
//                        }
//                    }
//                    tabPages.ClearArray();
//                    prefabMode.boolValue = true;
//                }
//            }
//            else
//            {
//                EditorGUILayout.PropertyField(tabPageInfos, new GUIContent("子页签信息"));
//                if (GUILayout.Button("还原成节点模式"))
//                {
//                    tabPages.ClearArray();
//                    foreach (var i in loader.TabPageInfos)
//                    {
//                        var go = LoadPrefab(i.PrefabName, loader.transform);
//                        if (go)
//                        {
//                            go.SetActive(false);
//                            var idx = tabPages.arraySize;
//                            tabPages.InsertArrayElementAtIndex(idx);
//                            var child = tabPages.GetArrayElementAtIndex(idx);
//                            child.objectReferenceValue = go;
//                            var t = go.transform as RectTransform;
//                            if (t)
//                            {
//                                t.anchorMin = i.AnchorMin;
//                                t.anchorMax = i.AnchorMax;
//                                t.pivot = i.Pivot;
//                                t.anchoredPosition = i.AnchoredPosition;
//                                t.sizeDelta = i.SizeDelta;
//                            }
//                        }
//                    }
//                    prefabMode.boolValue = false;
//                    tabPageInfos.ClearArray();
//                }
//            }
//            serializedObject.ApplyModifiedProperties();
//        }
//
//        GameObject LoadPrefab(string path, Transform parent)
//        {
//            var guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(path)} t:prefab");
//            foreach(var i in guids)
//            {
//                string p = AssetDatabase.GUIDToAssetPath(i);
//                if(Path.GetFileName(p).ToLower() == path.ToLower())
//                {
//                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(p);
//                    if (asset)
//                    {
//                        if (asset.GetComponent<PagePreloader>())
//                            continue;
//                        return PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
//                    }
//                }
//            }
//            return null;
//        }
//    }
//}
