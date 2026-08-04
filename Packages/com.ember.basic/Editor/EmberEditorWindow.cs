// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR

using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Ember 编辑器工具窗口基类 —— 基于 Odin Inspector。
    /// 提供语言切换、中英文 L10n、统一 Header/Footer 绘制。
    ///
    /// 子类在 <c>OnImGUI</c> 中用 Odin 属性驱动 UI，省略手写 Layout 代码。
    /// </summary>
    public abstract class EmberEditorWindow : OdinEditorWindow
    {
        [HideInInspector]
        public EditorToolLanguage Lang = EditorToolLanguage.Chinese;

        protected string L10n(string en, string cn) => Lang == EditorToolLanguage.English ? en : cn;

        protected virtual string WindowVersion => "v1.0";

        /// <summary>
        /// 子类必须返回窗口的菜单路径（如 "Tools/Ember/我的工具"）。
        /// </summary>
        protected abstract string MenuPath { get; }

        /// <summary>
        /// 子类必须返回窗口标题。
        /// </summary>
        protected abstract string WindowTitle { get; }

        protected virtual Vector2 WindowSize => new(500, 600);

        /// <summary>
        /// 绘制窗口内容。子类在此处用 Odin 属性或手写 OnGUI 实现。
        /// 基类自动处理 Header / 语言切换 / Footer。
        /// </summary>
        protected virtual void DrawContent() { }

        protected override void OnImGUI()
        {
            DrawToolbar();
            DrawSeparatorLine();
            DrawContent();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{WindowVersion} | Ember Tools", EditorStyles.centeredGreyMiniLabel);
            base.OnImGUI();
        }

        // ---- 工具条（语言切换 + 可选按钮）----

        protected void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 标题
            GUILayout.Label(WindowTitle, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 语言
            string langLabel = Lang == EditorToolLanguage.English ? "EN" : "中文";
            if (GUILayout.Button(langLabel, EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                Lang = Lang == EditorToolLanguage.English
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

        // ---- 通用 Odin 按钮样式 ----

        protected static GUIStyle BigButtonStyle =>
            new(GUIStyle.none) { fixedHeight = 40, alignment = TextAnchor.MiddleCenter };
    }
}
#endif
