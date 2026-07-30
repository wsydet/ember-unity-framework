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
////    [CustomEditor(typeof(ButtonEx), true)]
////    public class ButtonExEditor : ButtonEditor
////    {
////        SerializedProperty enableNode, disableNode, enableState, additionalGraphics;
////
////        [MenuItem("CONTEXT/Button/替换成ButtonEx")]
////        static void SwitchToButtonEx(MenuCommand command)
////        {
////            Button body = (Button)command.context;
////            if (body is ButtonEx)
////                return;
////            var sprite = body.targetGraphic;
////            var color = body.colors;
////            var state = body.spriteState;
////            var transition = body.transition;
////            var interactable = body.interactable;
////            var animationTrigger = body.animationTriggers;
////
////            var go = body.gameObject;
////            Object.DestroyImmediate(body);
////            var imgEx = go.AddComponent<ButtonEx>();
////            imgEx.targetGraphic = sprite;
////            imgEx.colors = color;
////            imgEx.spriteState = state;
////            imgEx.transition = transition;
////            imgEx.interactable = interactable;
////            imgEx.animationTriggers = animationTrigger;
////            EditorUtility.SetDirty(go);
////        }
////
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////            enableNode = serializedObject.FindProperty("enableNode");
////            disableNode = serializedObject.FindProperty("disableNode");
////            enableState = serializedObject.FindProperty("enableState");
////            additionalGraphics = serializedObject.FindProperty("additionalGraphics");
////
////        }
////        public override void OnInspectorGUI()
////        {
////            base.OnInspectorGUI();
////            bool oldState = enableState.boolValue;
////            ButtonEx btn = target as ButtonEx;
////            if(btn.transition == Selectable.Transition.ColorTint)
////            {
////                EditorGUILayout.PropertyField(additionalGraphics, new GUIContent("额外颜色变化目标"));
////            }
////            EditorGUILayout.PropertyField(enableState, new GUIContent("激活状态"));
////            EditorGUILayout.PropertyField(enableNode, new GUIContent("激活状态节点"));
////            EditorGUILayout.PropertyField(disableNode, new GUIContent("未激活状态节点"));
////            serializedObject.ApplyModifiedProperties();
////            if (oldState != enableState.boolValue)
////            {
////                btn.RefreshEnableState();
////            }
////        }
////    }
////}
