// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEditor;
using UnityEditor.UI;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="EUIToggleEx"/> 的 Inspector。
    /// 继承 Unity 内置 <see cref="ToggleEditor"/>（保证原生 Toggle 字段照常显示），
    /// 在 base 绘制之后补画增强字段：状态节点、Label 文本槽位。
    /// </summary>
    [CustomEditor(typeof(EUIToggleEx), true)]
    public class EUIToggleExEditor : ToggleEditor
    {
        #region 内部参数

        private SerializedProperty _onNode;
        private SerializedProperty _offNode;
        private SerializedProperty _disableNode;
        private SerializedProperty _label;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            _onNode = serializedObject.FindProperty("_onNode");
            _offNode = serializedObject.FindProperty("_offNode");
            _disableNode = serializedObject.FindProperty("_disableNode");
            _label = serializedObject.FindProperty("_label");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("状态节点", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onNode, new GUIContent("On 节点", "isOn = true 时显示的 GameObject"));
            EditorGUILayout.PropertyField(_offNode, new GUIContent("Off 节点", "isOn = false 时显示的 GameObject"));
            EditorGUILayout.PropertyField(_disableNode, new GUIContent("禁用节点", "interactable = false 时显示的 GameObject"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("引用", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_label, new GUIContent("文本", "开关的 label 文本槽位，Inspector 拖入子文本后可直接通过 Label 属性访问。"));

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
