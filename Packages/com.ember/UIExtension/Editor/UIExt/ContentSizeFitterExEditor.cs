////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEngine;
////using UnityEngine.UI;
////
////namespace UnityEditor.UI
////{
////    [CustomEditor(typeof(ContentSizeFitterEx), true)]
////    [CanEditMultipleObjects]
////    /// <summary>
////    /// Custom Editor for the ContentSizeFitter Component.
////    /// Extend this class to write a custom editor for a component derived from ContentSizeFitter.
////    /// </summary>
////    public class ContentSizeFitterExEditor : SelfControllerEditor
////    {
////        SerializedProperty m_HorizontalFit, m_maxWidth;
////        SerializedProperty m_VerticalFit, m_maxHeight;
////
////        protected virtual void OnEnable()
////        {
////            m_HorizontalFit = serializedObject.FindProperty("m_HorizontalFit");
////            m_maxWidth = serializedObject.FindProperty("m_maxWidth");
////            m_VerticalFit = serializedObject.FindProperty("m_VerticalFit");
////            m_maxHeight = serializedObject.FindProperty("m_maxHeight");
////        }
////
////        public override void OnInspectorGUI()
////        {
////            serializedObject.Update();
////            EditorGUILayout.PropertyField(m_HorizontalFit, true);
////            if(m_HorizontalFit.enumValueIndex == 2)
////            {
////                EditorGUILayout.PropertyField(m_maxWidth, true);
////            }
////            EditorGUILayout.PropertyField(m_VerticalFit, true);
////            if (m_VerticalFit.enumValueIndex == 2)
////            {
////                EditorGUILayout.PropertyField(m_maxHeight, true);
////            }
////            serializedObject.ApplyModifiedProperties();
////
////            base.OnInspectorGUI();
////        }
////    }
////}
