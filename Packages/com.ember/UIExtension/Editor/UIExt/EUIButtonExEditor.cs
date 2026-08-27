// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using UnityEditor;
using UnityEditor.UI;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="EUIButtonEx"/> 的 Inspector。
    /// 继承 Unity 内置 <see cref="ButtonEditor"/>（保证原生 Button 字段照常显示），
    /// 在 base 绘制之后补画增强字段：状态节点、附加图形、Label 文本槽位。
    ///
    /// <para>背景：Unity 内置 ButtonEditor 以 <c>[CustomEditor(typeof(Button), true)]</c>
    /// 注册（editorForChildClasses = true），会接管所有 Button 子类的 Inspector，
    /// 但只绘制基类字段。子类新增的序列化字段（如 Label 槽位）必须自定义 Editor 补画。</para>
    /// </summary>
    [CustomEditor(typeof(EUIButtonEx), true)]
    public class EUIButtonExEditor : ButtonEditor
    {
        #region 内部参数

        private SerializedProperty _enableNode;
        private SerializedProperty _disableNode;
        private SerializedProperty _additionalGraphics;
        private SerializedProperty _enableState;
        private SerializedProperty _label;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            _enableNode = serializedObject.FindProperty("_enableNode");
            _disableNode = serializedObject.FindProperty("_disableNode");
            _additionalGraphics = serializedObject.FindProperty("_additionalGraphics");
            _enableState = serializedObject.FindProperty("_enableState");
            _label = serializedObject.FindProperty("_label");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("状态节点", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableState, new GUIContent("启用状态"));
            EditorGUILayout.PropertyField(_enableNode, new GUIContent("启用节点"));
            EditorGUILayout.PropertyField(_disableNode, new GUIContent("禁用节点"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("附加图形", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_additionalGraphics, new GUIContent("附加目标图形"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("引用", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_label, new GUIContent("文本", "按钮的 label 文本槽位，Inspector 拖入子文本后可直接通过 Label 属性访问。"));

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
