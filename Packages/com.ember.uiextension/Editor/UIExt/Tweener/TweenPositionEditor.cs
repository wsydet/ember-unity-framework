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
//    [CustomEditor(typeof(TweenPosition))]
//    public class TweenPositionEditor : UITweenerEditor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenPosition tw = target as TweenPosition;
//            GUI.changed = false;
//
//            Vector3 from = EditorGUILayout.Vector3Field("From", tw.from);
//            Vector3 to = EditorGUILayout.Vector3Field("To", tw.to);
//            bool hasThrough = EditorGUILayout.Toggle("Has through point", tw.hasThroughPoint);
//            Vector3 through;
//            if (hasThrough)
//                through = EditorGUILayout.Vector3Field("Through Point", tw.through);
//            else
//                through = tw.through;
//            if (GUI.changed)
//            {
//                RegisterUndo("Tween Change", tw);
//                tw.from = from;
//                tw.to = to;
//                tw.hasThroughPoint = hasThrough;
//                tw.through = through;
//                EditorUtility.SetDirty(tw);
//            }
//
//            DrawCommonProperties();
//        }
//    }
//}
