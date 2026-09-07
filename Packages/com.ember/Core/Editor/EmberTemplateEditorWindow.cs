// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;

using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>旧窗口兼容壳；实际模板开发 UI 已迁入 Ember 项目中心。</summary>
    [Obsolete("请使用 EmberSetupWindow.ShowTemplateDevelopment() 打开统一项目中心。")]
    public class EmberTemplateEditorWindow : EditorWindow
    {
        #region 外部方法

        public static void ShowWindow()
        {
            EmberSetupWindow.ShowTemplateDevelopment();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "模板编辑器已合并到 Ember 项目中心的“模板开发”页。",
                MessageType.Info);
            if (GUILayout.Button("打开项目中心"))
                EmberSetupWindow.ShowTemplateDevelopment();
        }

        #endregion
    }
}
