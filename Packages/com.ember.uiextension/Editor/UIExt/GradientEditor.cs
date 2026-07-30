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
////using UnityEngine.UI;
////using UnityEditor.UI;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(Gradient), true)]
////    public class GradientEditor : UnityEditor.Editor
////    {
////        SerializedProperty topColor, topRightColor, bottomColor, bottomRightColor, fourColors, isLeft2Right;
////
////        protected void OnEnable()
////        {
////            fourColors = serializedObject.FindProperty("fourColors");
////            isLeft2Right = serializedObject.FindProperty("isLeft2Right");
////            topColor = serializedObject.FindProperty("topColor");
////            topRightColor = serializedObject.FindProperty("topRightColor");
////            bottomColor = serializedObject.FindProperty("bottomColor");
////            bottomRightColor = serializedObject.FindProperty("bottomRightColor");
////        }
////        public override void OnInspectorGUI()
////        {
////            EditorGUILayout.PropertyField(fourColors, new GUIContent("四角颜色模式"));
////            if (fourColors.boolValue)
////            {
////                EditorGUILayout.PropertyField(topColor, new GUIContent("左上颜色"));
////                EditorGUILayout.PropertyField(topRightColor, new GUIContent("右上颜色"));
////                EditorGUILayout.PropertyField(bottomColor, new GUIContent("左下颜色"));
////                EditorGUILayout.PropertyField(bottomRightColor, new GUIContent("右下颜色"));
////
////            }
////            else
////            {
////                EditorGUILayout.PropertyField(isLeft2Right, new GUIContent("左右渐变"));
////                if (isLeft2Right.boolValue)
////                {
////                    EditorGUILayout.PropertyField(topColor, new GUIContent("左部颜色"));
////                    EditorGUILayout.PropertyField(topRightColor, new GUIContent("右部颜色"));
////                }
////                else
////                {
////                    EditorGUILayout.PropertyField(topColor, new GUIContent("上部颜色"));
////                    EditorGUILayout.PropertyField(bottomColor, new GUIContent("下部颜色"));
////                }
////            }
////
////            serializedObject.ApplyModifiedProperties();
////        }
////    }
////}
