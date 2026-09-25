// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="TMPEx"/> 的 Inspector：先画 TMP 自己的面板，再追加「多语言」一行 Key，
    /// 并在下方逐语言预览 Key 对应的文本（只读，不修改场景与 Prefab）。
    /// <para>继承 TMP 的 <see cref="TMP_EditorPanelUI"/> 而不是重画面板，保证字体、材质、
    /// 对齐、边距等原始设置一处不少。</para>
    /// </summary>
    [CustomEditor(typeof(TMPEx))]
    public sealed class TMPExEditor : TMP_EditorPanelUI
    {
        #region 内部参数

        private SerializedProperty _keyProperty;
        private readonly List<string> _previewLanguages = new List<string>(8);

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            _keyProperty = serializedObject.FindProperty("_key");
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var component = target as TMPEx;
            if (component == null || _keyProperty == null) return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("多语言", EditorStyles.boldLabel);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_keyProperty, new GUIContent("Key", "填写后显示多语言表里的文本；留空显示上面的原文。"));
            if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            DrawPreview(component);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void DrawPreview(TMPEx component)
        {
            string key = component.Key;
            if (string.IsNullOrEmpty(key))
            {
                EditorGUILayout.HelpBox("未填写 Key：显示上面的原文，行为与普通 TMP 相同。", MessageType.None);
                return;
            }

            var localizer = TextLocalization.Localizer;
            if (localizer == null)
            {
                EditorGUILayout.HelpBox("当前没有注入多语言解析器，运行时会回退到原文。", MessageType.Warning);
                return;
            }

            _previewLanguages.Clear();
            var languages = localizer.Languages;
            if (languages != null)
                for (int i = 0; i < languages.Count; i++)
                    if (!string.IsNullOrEmpty(languages[i])) _previewLanguages.Add(languages[i]);
            if (_previewLanguages.Count == 0) _previewLanguages.Add(localizer.CurrentLanguage);

            string current = localizer.CurrentLanguage;
            EditorGUILayout.LabelField("当前语言：" + (string.IsNullOrEmpty(current) ? "（未设置）" : current), EditorStyles.miniLabel);
            for (int i = 0; i < _previewLanguages.Count; i++)
            {
                string language = _previewLanguages[i];
                bool resolved = TextLocalization.TryResolve(key, language, out string text);
                string marker = language == current ? " ●" : string.Empty;
                EditorGUILayout.LabelField(language + marker,
                    resolved ? text : "（缺条目 → 回退原文）", EditorStyles.wordWrappedMiniLabel);
            }

            if (Application.isPlaying && GUILayout.Button("按当前语言立即刷新")) TextLocalization.RefreshAll();
            EditorGUILayout.HelpBox("编辑期不会改写文本，避免破坏正式 Prefab 的所见即所得；运行期按当前语言显示。", MessageType.None);
        }

        #endregion
    }
}
