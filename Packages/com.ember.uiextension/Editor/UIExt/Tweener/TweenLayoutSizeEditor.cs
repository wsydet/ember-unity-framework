//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
////----------------------------------------------
//using UnityEngine;
//using UnityEditor;
//
//namespace Burner.UIExtension
//{
//
//    [CustomEditor(typeof(TweenLayoutSize))]
//    public class TweenLayoutSizeEditor : UITweenerEditor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenLayoutSize tw = target as TweenLayoutSize;
//            GUI.changed = false;
//
//            Vector2 from = EditorGUILayout.Vector2Field("From", tw.from);
//            Vector2 to = EditorGUILayout.Vector2Field("To", tw.to);
//
//            if (from.x < 0) from.x = 0;
//            if (from.y < 0) from.y = 0;
//            if (to.x < 0) to.x = 0;
//            if (to.y < 0) to.y = 0;
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
