// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// <see cref="TMPEx"/> 的 Inspector：优先展示多语言 Key、文本和字体皮肤，
    /// 并在下方逐语言预览 Key 对应的文本（只读，不修改场景与 Prefab）。
    /// <para>继承 TMP 的 <see cref="TMP_EditorPanelUI"/> 而不是重画面板，保证字体、材质、
    /// 对齐、边距等原始设置一处不少。</para>
    /// </summary>
    [CustomEditor(typeof(TMPEx)), CanEditMultipleObjects]
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
            var component = target as TMPEx;
            if (component == null || _keyProperty == null || IsMixSelectionTypes()) return;
            serializedObject.Update();
            SirenixEditorGUI.BeginBox("多语言 · 可选");
            EditorGUILayout.PropertyField(_keyProperty, new GUIContent("Key", "填写后显示多语言表里的文本；无需多语言时留空。"));
            SirenixEditorGUI.EndBox();

            // 复用原生文本绘制，保留 RTL、样式、链接文本及多选行为，只绘制一次。
            DrawTextInput();
            bool textChanged = serializedObject.ApplyModifiedProperties();
            if (_keyProperty.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox("所选文本使用不同 Key。修改 Key 会应用到全部所选文本。", MessageType.Info);
            else
                DrawPreview(component);

            SirenixEditorGUI.BeginBox("字体皮肤");
            try { DrawFontSkins(component); }
            finally { SirenixEditorGUI.EndBox(); }

            // 字体 API 会修改真实对象，刷新序列化缓存后再绘制 TMP 原生设置。
            serializedObject.Update();
            DrawMainSettings();
            DrawExtraSettings();
            if (serializedObject.ApplyModifiedProperties() || textChanged || m_HavePropertiesChanged)
            {
                foreach (var obj in targets) ((TMPEx)obj).havePropertiesChanged = true;
                m_HavePropertiesChanged = false;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void DrawFontSkins(TMPEx component)
        {
            var catalog = EmberFontSkins.Catalog;
            var ids = new List<int> { -1, 0 };
            var names = new List<string> { "固定字体（不跟随皮肤）", "跟随全局皮肤" };
            int old = ids.IndexOf(component.FontSkinId);
            if (old < 0) { ids.Add(component.FontSkinId); names.Add("指定皮肤：" + (catalog ? catalog.FindSkin(component.FontSkinId)?.Name : null) + " #" + component.FontSkinId); old = ids.Count - 1; }
            bool mixedMode = false;
            foreach (var obj in targets) mixedMode |= ((TMPEx)obj).FontSkinId != component.FontSkinId;
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixedMode;
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("切换方式", old, names.ToArray());
            bool modeChanged = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = previousMixed;
            if (modeChanged)
                foreach (var obj in targets)
                {
                    var text = (TMPEx)obj;
                    Undo.RecordObject(text, "选择字体皮肤");
                    text.SetFontSkin(ids[selected], text.FontSlotId);
                    EditorUtility.SetDirty(text);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                }
            if (!catalog)
            {
                EditorGUILayout.HelpBox("尚未配置字体皮肤库。固定模式仍可使用 TMP Font Asset。", MessageType.Info);
                if (GUILayout.Button("打开字体皮肤工具")) EmberFontSkinWindow.ShowWindow();
                return;
            }
            if (!mixedMode && component.FontSkinId < 0)
            {
                EditorGUILayout.HelpBox("固定字体：在下方 TMP 的 Font Asset 选择字体，不受全局换肤影响。已有皮肤映射保留。", MessageType.Info);
                return;
            }
            EditorGUILayout.HelpBox(mixedMode ? "所选文本的切换方式不同；下方映射仅在使用皮肤时生效。" : "每套皮肤独立选择此文本使用的字体；切换全局皮肤时自动应用对应项。", MessageType.Info);
            foreach (var skin in catalog.Skins)
            {
                var slotIds = new List<int>();
                var labels = new List<string>();
                foreach (var entry in skin.Fonts)
                {
                    var slot = catalog.Slots.Find(s => s.Id == entry.SlotId);
                    slotIds.Add(entry.SlotId);
                    labels.Add((slot?.Name ?? "已删除槽位") + " / " + (entry.Font ? entry.Font.name : "缺少字体"));
                }
                int index = slotIds.IndexOf(component.GetFontSlotForSkin(skin.Id));
                if (index < 0) { slotIds.Add(component.GetFontSlotForSkin(skin.Id)); labels.Add("未配置 / 已删除字体槽位"); index = labels.Count - 1; }
                EditorGUI.BeginChangeCheck();
                bool mixedSlot = false;
                foreach (var obj in targets) mixedSlot |= ((TMPEx)obj).GetFontSlotForSkin(skin.Id) != component.GetFontSlotForSkin(skin.Id);
                EditorGUI.showMixedValue = mixedSlot;
                int next = EditorGUILayout.Popup(skin.Name, index, labels.ToArray());
                bool slotChanged = EditorGUI.EndChangeCheck();
                EditorGUI.showMixedValue = previousMixed;
                if (slotChanged)
                    foreach (var obj in targets)
                    {
                        var text = (TMPEx)obj;
                        Undo.RecordObject(text, "配置皮肤字体");
                        text.SetFontForSkin(skin.Id, slotIds[next]);
                        EditorUtility.SetDirty(text);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    }
            }
            if (GUILayout.Button("管理皮肤 / 一键换字体")) EmberFontSkinWindow.ShowWindow();
        }

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
