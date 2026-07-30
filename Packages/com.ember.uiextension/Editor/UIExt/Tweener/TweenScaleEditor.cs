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
//    [CustomEditor(typeof(TweenScale))]
//    public class TweenScaleEditor : UITweenerEditor
//    {
//        public override void OnInspectorGUI()
//        {
//            GUILayout.Space(6f);
//            SetLabelWidth(120f);
//
//            TweenScale tw = target as TweenScale;
//            GUI.changed = false;
//
//            Vector3 from = EditorGUILayout.Vector3Field("From", tw.from);
//            Vector3 to = EditorGUILayout.Vector3Field("To", tw.to);
//            bool table = EditorGUILayout.Toggle("Update Table", tw.updateTable);
//
//            if (GUI.changed)
//            {
//                RegisterUndo("Tween Change", tw);
//                tw.from = from;
//                tw.to = to;
//                tw.updateTable = table;
//                EditorUtility.SetDirty(tw);
//            }
//
//            DrawCommonProperties();
//        }
//    }
//}
