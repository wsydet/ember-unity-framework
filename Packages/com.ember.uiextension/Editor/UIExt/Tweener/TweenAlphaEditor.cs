//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEditor;
//
//namespace Burner.UIExtension
//{
//    [CustomEditor(typeof(TweenAlpha))]
//    public class TweenAlphaEditor : UITweenerEditor
//    {
//
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenAlpha tw = target as TweenAlpha;
//            GUI.changed = false;
//
//            float from = EditorGUILayout.Slider("From", tw.from, 0f, 1f);
//            float to = EditorGUILayout.Slider("To", tw.to, 0f, 1f);
//
//            if (GUI.changed)
//            {
//                RegisterUndo("Tween Change", tw);
//                tw.from = from;
//                tw.to = to;
//                UnityEditor.EditorUtility.SetDirty(tw);
//            }
//
//            DrawCommonProperties();
//        }
//
//    }
//}
