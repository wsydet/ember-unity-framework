////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using Burner.Basic;
////using UnityEngine;
////using UnityEditor;
////using UnityEngine.UI;
////using UnityEditor.UI;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(DragEventTriggerListener), true)]
////    public class DragEventTriggerListnerEditor : UnityEditor.Editor
////    {
////        SerializedProperty isHaveCoverParentScrollRect, isHaveCoverDragEventListener;
////        private void OnEnable()
////        {
////            isHaveCoverParentScrollRect = serializedObject.FindProperty("isHaveCoverParentScrollRect");
////            isHaveCoverDragEventListener = serializedObject.FindProperty("isHaveCoverDragEventListener");
////
////        }
////
////        public override void OnInspectorGUI()
////        {
////            EditorGUILayout.PropertyField(isHaveCoverParentScrollRect, new GUIContent("是否透传到上一层ScrollRect"));
////            EditorGUILayout.PropertyField(isHaveCoverDragEventListener, new GUIContent("是否透传到上一层DragEventHandler"));
////
////            serializedObject.ApplyModifiedProperties();
////        }
////    }
////}
