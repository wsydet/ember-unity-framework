// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    public enum EditorToolLanguage { English, Chinese }

    /// <summary>
    /// 编辑器工具共享辅助方法。
    /// </summary>
    public static class EditorToolUtility
    {
        public static string L10n(EditorToolLanguage lang, string en, string cn)
            => lang == EditorToolLanguage.English ? en : cn;

        public static void DrawHeader(string title, Color? bgColor = null)
        {
            Color c = bgColor ?? new Color(0.15f, 0.15f, 0.15f, 1f);
            Rect rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, c);
            var style = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            EditorGUI.LabelField(rect, title, style);
        }

        public static void DrawSeparator()
        {
            EditorGUILayout.Space(10);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(10);
        }

        public static void DrawLanguageToolbar(ref EditorToolLanguage lang)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            string label = lang == EditorToolLanguage.English ? "Language: English" : "语言: 中文";
            if (GUILayout.Button(label, EditorStyles.toolbarDropDown, GUILayout.Width(120)))
            {
                var selectedLang = lang;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("English"), lang == EditorToolLanguage.English,
                    () => selectedLang = EditorToolLanguage.English);
                menu.AddItem(new GUIContent("中文"), lang == EditorToolLanguage.Chinese,
                    () => selectedLang = EditorToolLanguage.Chinese);
                menu.ShowAsContext();
                lang = selectedLang;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
