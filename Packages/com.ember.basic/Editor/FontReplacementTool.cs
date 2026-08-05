// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// UI 综合管理工具 —— 批量替换 TMP 字体、旧版 Text 转 TMP、自动适配文本框尺寸。
    /// 搜索范围：所有已加载的场景（包括 Additive 叠加的）。
    /// </summary>
    public class FontReplacementTool : EmberEditorWindow
    {
        protected override string MenuPath => "Ember/Tool/UI 综合管理工具";
        protected override string WindowTitle => "UI Toolkit";
        protected override Vector2 WindowSize => new(400, 550);
        protected override string WindowVersion => "v2.1";

        [BoxGroup("配置"), LabelText("目标 TMP 字体")]
        public TMP_FontAsset SelectedFont;

        [BoxGroup("配置"), LabelText("排除关键字")]
        public string ExcludeFilter = "";

        private static TMP_FontAsset s_lastFont;
        private static string s_lastExcludeFilter = "";

        // ======== 菜单入口 ========

        [MenuItem("Ember/Tool/UI 综合管理工具", false, 110)]
        public static void ShowWindow()
        {
            var win = GetWindow<FontReplacementTool>();
            win.minSize = win.WindowSize;
            if (s_lastFont) win.SelectedFont = s_lastFont;
            win.ExcludeFilter = s_lastExcludeFilter;
            win.Show();
        }

        [MenuItem("GameObject/Ember/字体替换/打开面板", false, 1400)]
        public static void ShowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/字体替换/替换所有 TMP 字体 (使用上次字体)", false, 1470)]
        public static void QuickReplaceFont()
        {
            if (!s_lastFont) { EditorUtility.DisplayDialog("Ember", "请先打开面板选择一次字体。", "确定"); return; }
            int count = ReplaceAllTMPFonts(s_lastFont, s_lastExcludeFilter);
            MarkAllDirty();
            EditorUtility.DisplayDialog("Ember", $"已替换 {count} 处 TMP 字体。", "确定");
        }

        [MenuItem("GameObject/Ember/字体替换/替换所有 TMP 字体 (使用上次字体)", true, 1470)]
        public static bool QuickReplaceFontValidate() => s_lastFont;

        [MenuItem("GameObject/Ember/字体替换/转换 Legacy Text → TMP", false, 1471)]
        public static void QuickConvertLegacy()
        {
            int count = ConvertAllLegacyText(s_lastFont);
            MarkAllDirty();
            EditorUtility.DisplayDialog("Ember", $"已转换 {count} 处 Text → TMP。", "确定");
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.LabelField(L10n("2. Batch Font Replacement", "2. 批量字体替换"), EditorStyles.boldLabel);
            if (GUILayout.Button(L10n("Replace TMP Fonts in Scene", "替换已加载场景中的 TMP 字体"), BigButtonStyle))
            {
                if (!SelectedFont) { EditorUtility.DisplayDialog(L10n("Error", "错误"), L10n("Please select a target TMP font first!", "请先选择目标 TMP 字体！"), L10n("OK", "确定")); return; }
                s_lastFont = SelectedFont;
                s_lastExcludeFilter = ExcludeFilter;
                int count = ReplaceAllTMPFonts(SelectedFont, ExcludeFilter);
                MarkAllDirty();
                EditorUtility.DisplayDialog(L10n("Success", "成功"), L10n($"Modified {count} fonts", $"已修改 {count} 处字体"), L10n("OK", "确定"));
            }
            if (GUILayout.Button(L10n("Replace Fonts in Prefabs", "替换工程预制体字体"), BigButtonStyle))
            {
                if (!SelectedFont) { EditorUtility.DisplayDialog(L10n("Error", "错误"), L10n("Please select a target TMP font first!", "请先选择目标 TMP 字体！"), L10n("OK", "确定")); return; }
                ReplaceInPrefabs();
            }

            DrawSeparatorLine();

            EditorGUILayout.LabelField(L10n("3. Component Conversion (Legacy → TMP)", "3. 组件转换 (Legacy → TMP)"), EditorStyles.boldLabel);
            if (GUILayout.Button(L10n("Convert Legacy Text to TMP in Scene", "将已加载场景中的 Text 转换为 TMP"), BigButtonStyle))
            {
                int count = ConvertAllLegacyText(SelectedFont);
                MarkAllDirty();
                EditorUtility.DisplayDialog(L10n("Done", "完成"), L10n($"Converted {count} Text → TMP", $"已转换 {count} 处 Text → TMP"), L10n("OK", "确定"));
            }

            DrawSeparatorLine();

            EditorGUILayout.LabelField(L10n("4. Layout Fit (RectTransform Fit)", "4. 布局适配 (RectTransform Fit)"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n("Auto-adjust text box size based on current text content.", "根据当前文字内容自动调整文本框大小。"), MessageType.Info);
            if (GUILayout.Button(L10n("Auto-fit All TMP Sizes in Scene", "自动调整场景中所有 TMP 尺寸"), BigButtonStyle))
            {
                int count = FitAllTMP(ExcludeFilter);
                MarkAllDirty();
                EditorUtility.DisplayDialog(L10n("Done", "完成"), L10n($"Adjusted {count} text box sizes", $"已调整 {count} 个文本框尺寸"), L10n("OK", "确定"));
            }
        }

        // ======== 核心逻辑 ========

        private static int ReplaceAllTMPFonts(TMP_FontAsset font, string exclude)
        {
            var texts = GameObject.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            Undo.RecordObjects(texts, "Batch Replace Font");
            int count = 0;
            foreach (var t in texts)
            {
                if (!string.IsNullOrEmpty(exclude) && t.name.Contains(exclude)) continue;
                t.font = font;
                t.SetAllDirty();
                count++;
            }
            return count;
        }

        private void ReplaceInPrefabs()
        {
            string[] ids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < ids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(ids[i]);
                EditorUtility.DisplayProgressBar("处理预制体", path, (float)i / ids.Length);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length == 0) continue;
                Undo.RecordObjects(texts, "Replace Prefab Font");
                foreach (var t in texts) t.font = SelectedFont;
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private static int ConvertAllLegacyText(TMP_FontAsset font)
        {
            var legacy = GameObject.FindObjectsByType<Text>(FindObjectsInactive.Include);
            int count = 0;
            foreach (var old in legacy)
            {
                if (old.GetComponent<TextMeshProUGUI>() != null) continue;
                var go = old.gameObject;
                string content = old.text;
                Color color = old.color;
                float size = old.fontSize;
                var anchor = old.alignment;

                Undo.DestroyObjectImmediate(old);
                var tmp = Undo.AddComponent<TextMeshProUGUI>(go);
                tmp.text = content; tmp.color = color; tmp.fontSize = size;
                if (font) tmp.font = font;
                tmp.alignment = anchor switch
                {
                    TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                    TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                    TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                    TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                    TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                    TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                    TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                    TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                    TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                    _ => TextAlignmentOptions.Center,
                };
                count++;
            }
            return count;
        }

        private static int FitAllTMP(string exclude)
        {
            var texts = GameObject.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            Undo.RecordObjects(System.Array.ConvertAll(texts, t => t.rectTransform), "Fit TMP Size");
            int count = 0;
            foreach (var t in texts)
            {
                if (!string.IsNullOrEmpty(exclude) && t.name.Contains(exclude)) continue;
                t.ForceMeshUpdate();
                t.rectTransform.sizeDelta = t.GetPreferredValues();
                count++;
            }
            return count;
        }

        /// <summary>
        /// 将所有已加载的场景标脏。FindObjectsByType 会搜索所有已加载场景，
        /// 修改后需要标脏每一个被触及的场景。
        /// </summary>
        private static void MarkAllDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif
