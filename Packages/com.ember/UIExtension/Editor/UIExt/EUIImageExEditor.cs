// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using UnityEditor;
using UnityEditor.UI;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="EUIImageEx"/> 的 Inspector。
    /// 继承 Unity 内置 <see cref="ImageEditor"/>（保证原生 Image 字段照常显示），
    /// 在 base 绘制之后补画增强字段：精灵数组、帧动画、点击区域、布局。
    /// </summary>
    [CustomEditor(typeof(EUIImageEx), true)]
    public class EUIImageExEditor : ImageEditor
    {
        #region 内部参数

        private SerializedProperty _spriteArray;
        private SerializedProperty _spriteIndex;
        private SerializedProperty _animated;
        private SerializedProperty _fps;
        private SerializedProperty _delay;
        private SerializedProperty _playOnce;
        private SerializedProperty _playbackSpeed;
        private SerializedProperty _irregularClickArea;
        private SerializedProperty _hitMinimalAlpha;
        private SerializedProperty _keepNativeSize;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            _spriteArray = serializedObject.FindProperty("_spriteArray");
            _spriteIndex = serializedObject.FindProperty("_spriteIndex");
            _animated = serializedObject.FindProperty("_animated");
            _fps = serializedObject.FindProperty("_fps");
            _delay = serializedObject.FindProperty("_delay");
            _playOnce = serializedObject.FindProperty("_playOnce");
            _playbackSpeed = serializedObject.FindProperty("_playbackSpeed");
            _irregularClickArea = serializedObject.FindProperty("_irregularClickArea");
            _hitMinimalAlpha = serializedObject.FindProperty("_hitMinimalAlpha");
            _keepNativeSize = serializedObject.FindProperty("_keepNativeSize");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("精灵数组", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_spriteArray, new GUIContent("精灵列表", "可切换的精灵数组，按索引显示"));
            EditorGUILayout.PropertyField(_spriteIndex, new GUIContent("当前索引"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("帧动画", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animated, new GUIContent("启用帧动画"));
            if (_animated.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fps, new GUIContent("帧率"));
                EditorGUILayout.PropertyField(_delay, new GUIContent("循环间隔", "每轮动画结束后的等待时间（秒），0 = 无间隔立即循环"));
                EditorGUILayout.PropertyField(_playOnce, new GUIContent("只播放一次"));
                EditorGUILayout.PropertyField(_playbackSpeed, new GUIContent("播放速度"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("点击区域", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_irregularClickArea, new GUIContent("不规则点击", "启用后根据像素透明度判定点击"));
            if (_irregularClickArea.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_hitMinimalAlpha, new GUIContent("透明度阈值", "像素 alpha 低于此值时不响应点击"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("布局", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_keepNativeSize, new GUIContent("保持原始尺寸", "切换精灵后自动调用 SetNativeSize"));

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
