// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Ember 编辑器工具窗口基类。
    /// 提供全局语言切换、中英文 L10n、统一 Header/Footer 绘制。
    /// 子类覆盖 <c>DrawContent()</c> 实现自定义 UI。
    /// </summary>
    public abstract class EmberEditorWindow : EditorWindow
    {
        /// <summary>
        /// 全局语言设置（EditorPrefs 持久化，所有面板共享）。
        /// </summary>
        public static EditorToolLanguage GlobalLang
        {
            get => (EditorToolLanguage)EditorPrefs.GetInt("Ember_EditorLanguage", 1);
            private set
            {
                EditorPrefs.SetInt("Ember_EditorLanguage", (int)value);
                RepaintAllOpenWindows();
            }
        }

        /// <summary>
        /// 实例级 Lang 委托给全局静态属性，保证所有窗口同步切换。
        /// </summary>
        public EditorToolLanguage Lang
        {
            get => GlobalLang;
            set => GlobalLang = value;
        }

        protected string L10n(string en, string cn) => Lang == EditorToolLanguage.English ? en : cn;

        protected virtual string WindowVersion => "v1.0";

        /// <summary>
        /// 子类必须返回窗口的菜单路径（如 "Ember/Tool/我的工具"）。
        /// </summary>
        protected abstract string MenuPath { get; }

        /// <summary>
        /// 子类必须返回窗口中文标题。
        /// </summary>
        protected abstract string WindowTitle { get; }

        /// <summary>
        /// 子类可选返回窗口英文标题。默认回退到中文标题。
        /// </summary>
        protected virtual string WindowTitleEN => WindowTitle;

        protected virtual Vector2 WindowSize => new(500, 600);

        /// <summary>
        /// 绘制窗口内容。子类在此处用 IMGUI 实现全部 UI。
        /// </summary>
        protected virtual void DrawContent() { }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSeparatorLine();

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            DrawContent();
            EditorGUILayout.EndVertical();

            DrawFooter();
        }

        protected virtual void OnEnable()
        {
            _openWindows.Add(this);
        }

        protected virtual void OnDisable()
        {
            _openWindows.Remove(this);
        }

        private static readonly HashSet<EmberEditorWindow> _openWindows = new();

        private static void RepaintAllOpenWindows()
        {
            foreach (var window in _openWindows)
            {
                if (window) window.Repaint();
            }
        }

        #region Header / Footer / Toolbar

        protected void DrawFooter()
        {
            EditorGUILayout.Space(5);
            Rect footerRect = EditorGUILayout.GetControlRect(false, 22);
            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f, 1f)
                : new Color(0.75f, 0.75f, 0.75f, 1f);
            EditorGUI.DrawRect(footerRect, bgColor);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f) }
            };
            EditorGUI.LabelField(footerRect, $"{WindowVersion} | Ember Tools", style);
        }

        protected void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string titleText = Lang == EditorToolLanguage.English ? WindowTitleEN : WindowTitle;
            GUILayout.Label(titleText, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            string langLabel = Lang == EditorToolLanguage.English ? "EN" : "中文";
            if (GUILayout.Button(langLabel, EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                GlobalLang = Lang == EditorToolLanguage.English
                    ? EditorToolLanguage.Chinese
                    : EditorToolLanguage.English;
            }

            EditorGUILayout.EndHorizontal();
        }

        protected static void DrawSeparatorLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        #endregion

        protected static GUIStyle BigButtonStyle
        {
            get
            {
                var s = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 40,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
                return s;
            }
        }
    }
}
#endif
