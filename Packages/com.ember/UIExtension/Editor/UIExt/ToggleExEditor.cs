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
//using UnityEngine.UI;
//using UnityEditor.UI;
//
//namespace Burner.UIExtension
//{
//    [CustomEditor(typeof(ToggleEx), true)]
//    public class ToggleExEditor : ToggleEditor
//    {
//        SerializedProperty onNode, offNode, disableNode;
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            onNode = serializedObject.FindProperty("onNode");
//            offNode = serializedObject.FindProperty("offNode");
//            disableNode = serializedObject.FindProperty("disableNode");
//        }
//        public override void OnInspectorGUI()
//        {
//            base.OnInspectorGUI();
//            EditorGUILayout.PropertyField(onNode, new GUIContent("勾选状态节点"));
//            EditorGUILayout.PropertyField(offNode, new GUIContent("未勾选状态节点"));
//            EditorGUILayout.PropertyField(disableNode, new GUIContent("禁用状态节点"));
//            serializedObject.ApplyModifiedProperties();
//        }
//    }
//}
