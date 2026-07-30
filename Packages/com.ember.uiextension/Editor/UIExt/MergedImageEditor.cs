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
////    [CustomEditor(typeof(MergedImage), true)]
////    public class MergedImageEditor : ImageEditor
////    {
////        SerializedProperty imageInfo;
////
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////            imageInfo = serializedObject.FindProperty("mImageInfo");
////        }
////        public override void OnInspectorGUI()
////        {
////            base.OnInspectorGUI();
////
////            EditorGUILayout.LabelField("��ʶ���б�");
////            EditorGUI.indentLevel++;
////            for (int i = 0; i != imageInfo.arraySize; ++i)
////            {
////                EditorGUILayout.PropertyField(imageInfo.GetArrayElementAtIndex(i).FindPropertyRelative("mTexName"), new GUIContent(string.Format($"{i + 1}")));
////            }
////            serializedObject.ApplyModifiedProperties();
////
////            EditorGUI.indentLevel--;
////        }
////    }
////}
