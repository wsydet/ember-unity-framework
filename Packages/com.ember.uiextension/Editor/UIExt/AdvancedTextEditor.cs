////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEngine;
////using UnityEditor;
////
////namespace Burner.UIExtension
////{
////    [CustomEditor(typeof(AdvancedText), true)]
////    public class AdvancedTextEditor : UnityEditor.UI.GraphicEditor
////    {
////        SerializedProperty m_Text;
////        SerializedProperty m_ImageFont;
////        SerializedProperty m_IsPureEmoji;
////        SerializedProperty m_ImageSize;
////        SerializedProperty m_OverlapPixels;
////        SerializedProperty m_FontData;
////
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////
////            m_Text = serializedObject.FindProperty("originalText");
////            m_ImageSize = serializedObject.FindProperty("imageSize");
////            m_OverlapPixels = serializedObject.FindProperty("overlapPixels");
////            m_ImageFont = serializedObject.FindProperty("m_ImageFont");
////            m_FontData = serializedObject.FindProperty("m_FontData");
////            m_IsPureEmoji = serializedObject.FindProperty("isPureEmoji");
////        }
////        public override void OnInspectorGUI()
////        {
////            serializedObject.Update();
////            AdvancedText img = target as AdvancedText;
////            EditorGUILayout.PropertyField(m_Text);
////            EditorGUILayout.PropertyField(m_ImageSize);
////            EditorGUILayout.PropertyField(m_OverlapPixels);
////            EditorGUILayout.PropertyField(m_IsPureEmoji);
////            img.RayCastTarget = EditorGUILayout.Toggle("Raycast Target", img.RayCastTarget);
////            EditorGUILayout.PropertyField(m_ImageFont);
////            EditorGUILayout.PropertyField(m_FontData);
////            AppearanceControlsGUI();
////            serializedObject.ApplyModifiedProperties();
////        }
////    }
////}
