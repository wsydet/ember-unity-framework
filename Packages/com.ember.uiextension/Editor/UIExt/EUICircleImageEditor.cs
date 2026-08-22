// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEditor;
using UnityEditor.UI;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="EUICircleImage"/> 的 Inspector。
    /// 继承 Unity 内置 <see cref="ImageEditor"/>（保证原生 Image 字段照常显示），
    /// 在 base 绘制之后补画圆形设置字段：分段数、填充百分比、未填充颜色。
    /// </summary>
    [CustomEditor(typeof(EUICircleImage), true)]
    public class EUICircleImageEditor : ImageEditor
    {
        #region 内部参数

        private SerializedProperty _segments;
        private SerializedProperty _fillPercent;
        private SerializedProperty _unfilledColor;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            _segments = serializedObject.FindProperty("_segments");
            _fillPercent = serializedObject.FindProperty("_fillPercent");
            _unfilledColor = serializedObject.FindProperty("_unfilledColor");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("圆形设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_segments, new GUIContent("分段数", "圆形由多少块三角形拼成，数值越大边缘越平滑"));
            EditorGUILayout.PropertyField(_fillPercent, new GUIContent("填充百分比", "显示部分占圆形的比例，1 = 完整圆形，0.5 = 半圆"));
            EditorGUILayout.PropertyField(_unfilledColor, new GUIContent("未填充颜色", "FillPercent 之外区域的灰度颜色"));

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
