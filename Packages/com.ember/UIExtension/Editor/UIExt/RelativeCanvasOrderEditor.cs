////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(RelativeCanvasOrder))]
////    public class RelativeCanvasOrderEditor : UnityEditor.Editor
////    {
////        SerializedProperty orderOffset;
////        bool needValidation = false;
////        private void OnEnable()
////        {
////            orderOffset = serializedObject.FindProperty("orderOffset");
////        }
////        public override void OnInspectorGUI()
////        {
////            EditorGUILayout.PropertyField(orderOffset, new GUIContent("渲染Order偏移"));
////            serializedObject.ApplyModifiedProperties();
////            if (EditorGUI.EndChangeCheck())
////            {
////                foreach (RelativeCanvasOrder r in targets)
////                {
////                    r.UpdateSortingOrder();
////                }
////            }
////        }
////    }
////}
