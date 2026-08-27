////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEditor;
////using UnityEditor.UI;
////using UnityEngine;
////using Burner.Extensions;
////using UnityEngine.UI;
////
////namespace Burner
////{
////    [CustomEditor(typeof(BurnerButton))]
////    [CanEditMultipleObjects]
////    public class BurnerButtonEditor : ButtonEditor
////    {
////        private SerializedProperty m_IsEnableScale;
////        private SerializedProperty clickScale;
////        private SerializedProperty blockTime;
////        private SerializedProperty useBlockTime;
////        private SerializedProperty ScaleTarget;
////        private SerializedProperty m_IsEnableLongPress;
////        private SerializedProperty m_LongPressTime;
////        private SerializedProperty m_OnLongClick;
////
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////
////            m_IsEnableScale = serializedObject.FindProperty("isEnableScale");
////            clickScale = serializedObject.FindProperty("clickScale");
////            useBlockTime = serializedObject.FindProperty("UseClickBlockTime");
////            blockTime = serializedObject.FindProperty("ClickBlockTime");
////            ScaleTarget = serializedObject.FindProperty("ScaleTarget");
////            m_IsEnableLongPress = serializedObject.FindProperty("isEnableLongPress");
////            m_LongPressTime = serializedObject.FindProperty("m_LongPressTime");
////            m_OnLongClick = serializedObject.FindProperty("m_OnLongClick");
////        }
////
////        public override void OnInspectorGUI()
////        {
////            base.OnInspectorGUI();
////            serializedObject.Update();
////
////            EditorGUILayout.PropertyField(useBlockTime, new GUIContent("Enable Alternative Block Time",
////                $"BurnerButton defaultly, has a internal time {BurnerButton.GlobalBlockTime}s to block clicked event which can be " +
////                $"triggered many times during a short period when player click it very quickly. You may know it as Cool Down time.\n" +
////                $"Enable this option to set it into another value."));
////            if(useBlockTime.boolValue)
////            {
////                EditorGUI.indentLevel++;
////                EditorGUILayout.PropertyField(blockTime, new GUIContent("Alternative Block Time", "in sceonds"));
////                EditorGUI.indentLevel--;
////            }
////
////            EditorGUILayout.PropertyField(m_IsEnableScale, new GUIContent("Enable Scale",
////                "To enable scale the transform of current BurnerButton GameObject when clicked."));
////            if (m_IsEnableScale.boolValue)
////            {
////                EditorGUI.indentLevel++;
////                EditorGUILayout.PropertyField(clickScale, new GUIContent("Scale Value"));
////                EditorGUILayout.PropertyField(ScaleTarget, new GUIContent("Non-Scale Target",
////                    "This GameObject's transform will be set into 1/scale, that means it won't be scale when clicked."));
////                EditorGUI.indentLevel--;
////            }
////
////            EditorGUILayout.PropertyField(m_IsEnableLongPress, new GUIContent("Enable Long Press",
////                "To enable Long Press triggering feature."));
////            if(m_IsEnableLongPress.boolValue)
////            {
////                EditorGUI.indentLevel++;
////                EditorGUILayout.PropertyField(m_LongPressTime, new GUIContent("Long Press Time",
////                    "How much time has passed to trigger OnLongClick event. In seconds"));
////                EditorGUILayout.PropertyField(m_OnLongClick);
////                EditorGUI.indentLevel--;
////            }
////            serializedObject.ApplyModifiedProperties();
////        }
////
////        [MenuItem("GameObject/Burner UI/BurnerButton", false, 10)]
////        static void CreateCustomGameObject(MenuCommand menuCommand)
////        {
////            // Create a custom game object
////            GameObject go = new GameObject("BurnerButton");
////            // Ensure it gets reparented if this was a context click (otherwise does nothing)
////            var parent = menuCommand.context as GameObject;
////            if (parent == null)
////            {
////                parent = Selection.activeGameObject;
////            }
////            GameObjectUtility.SetParentAndAlign(go, parent);
////            var img = go.AddComponent<Image>();
////            go.AddComponent<BurnerButton>();
////            var rct = go.GetComponent<RectTransform>();
////            if (rct.IsNull()) rct = go.AddComponent<RectTransform>();
////            rct.sizeDelta = new Vector2(160.0f, 30.0f);
////
////            var child = new GameObject("Text(TMP)");
////            child.AddComponent<RectTransform>();
////            var tmp = child.AddComponent<TMPro.TextMeshProUGUI>();
////            tmp.text = "TextMeshPro";
////            child.transform.SetParent(go.transform, false);
////            (child.transform as RectTransform).sizeDelta = Vector2.zero;
////            (child.transform as RectTransform).anchorMin = Vector2.zero;
////            (child.transform as RectTransform).anchorMax = Vector2.one;
////
////            go.GetComponent<BurnerButton>().AutoAddScaleTarget();
////
////            // Register the creation in the undo system
////            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
////            Selection.activeObject = go;
////        }
////    }
////}
