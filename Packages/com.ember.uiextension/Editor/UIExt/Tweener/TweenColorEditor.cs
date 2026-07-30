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
//    [CustomEditor(typeof(TweenColor))]
//    public class TweenColorEditor : UITweenerEditor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenColor tw = target as TweenColor;
//            GUI.changed = false;
//
//            Color from = EditorGUILayout.ColorField("From", tw.from);
//            Color to = EditorGUILayout.ColorField("To", tw.to);
//
//            if (GUI.changed)
//            {
//                RegisterUndo("Tween Change", tw);
//                tw.from = from;
//                tw.to = to;
//                EditorUtility.SetDirty(tw);
//            }
//
//            DrawCommonProperties();
//        }
//    }
//}
