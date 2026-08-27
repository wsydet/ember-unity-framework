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
//    [CustomEditor(typeof(UIParticleOrder))]
//    public class UIParticleOrderEditor : UnityEditor.Editor
//    {
//        SerializedProperty important;
//        SerializedProperty orderOffset;
//        SerializedProperty maskable;
//
//        private void OnEnable()
//        {
//            important = serializedObject.FindProperty("important");
//            orderOffset = serializedObject.FindProperty("orderOffset");
//            maskable = serializedObject.FindProperty("maskable");
//        }
//
//        public override void OnInspectorGUI()
//        {
//            EditorGUI.BeginChangeCheck();
//
//            EditorGUILayout.PropertyField(important, new GUIContent("Important"));
//            EditorGUILayout.PropertyField(orderOffset, new GUIContent("渲染Order偏移"));
//            EditorGUILayout.PropertyField(maskable, new GUIContent("Maskable"));
//
//            serializedObject.ApplyModifiedProperties();
//
//            if (EditorGUI.EndChangeCheck())
//            {
//                foreach (UIParticleOrder p in targets)
//                {
//                    p.Refresh();
//                }
//            }
//        }
//    }
//}
