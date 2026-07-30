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
//    [CustomEditor(typeof(TweenRotation))]
//    public class TweenRotationEditor : UITweenerEditor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenRotation tw = target as TweenRotation;
//            GUI.changed = false;
//
//            float from = EditorGUILayout.FloatField("From", tw.from);
//            float to = EditorGUILayout.FloatField("To", tw.to);
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
