// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    internal sealed class EmberProjectValidationPanel
    {
        #region 内部参数

        private readonly EmberSetupWindowContext _context;
        private EmberProjectValidationReport _report;
        private string _error;
        private string _search = string.Empty;
        private string _category = "全部类别";
        private int _filter;
        private int _revision;
        private int _page;
        private Vector2 _scroll;
        private bool _requested;
        private static readonly string[] Filters = { "全部", "错误", "警告", "差异", "通过/说明" };

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberProjectValidationPanel(EmberSetupWindowContext context) { _context = context; }
        internal void RequestScan() { _requested = true; _context.Repaint(); }

        internal void Draw()
        {
            if (_requested && !_context.OperationsBlocked && Event.current.type == EventType.Layout)
            {
                _requested = false;
                Scan();
            }
            EditorGUILayout.LabelField("项目校验", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("检查当前项目的生成链路、资源元数据和场景配置。业务内容差异单独列出，检查不会修改文件。", MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_context.OperationsBlocked))
                    if (GUILayout.Button("检查当前项目", GUILayout.Width(125))) Scan();
                if (_report != null && GUILayout.Button("复制报告", GUILayout.Width(90)))
                    EditorGUIUtility.systemCopyBuffer = _report.ToPlainText();
                if (GUILayout.Button("项目初始化", GUILayout.Width(100))) EmberSetupWindow.ShowWindow();
                if (EmberProjectSetup.IsEmbeddedPackage() && GUILayout.Button("模板开发", GUILayout.Width(90)))
                    EmberSetupWindow.ShowTemplateDevelopment();
                if (GUILayout.Button("UI 开发中心", GUILayout.Width(110)))
                    EditorApplication.ExecuteMenuItem("Ember/UI/UI 开发中心");
            }
            if (_requested) EditorGUILayout.HelpBox("等待编辑模式且编译/导入结束后执行检查。", MessageType.Info);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (_report == null) return;
            EditorGUILayout.LabelField("比较基准：" + _report.Baseline, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"检查时间 {_report.CheckedAt:HH:mm:ss} · 错误 {_report.ErrorCount} · 警告 {_report.WarningCount} · 差异 {_report.DifferenceCount}", EditorStyles.boldLabel);
            if (_revision != _context.Revision)
                EditorGUILayout.HelpBox("项目或模板状态已变化，此报告可能过期，请重新检查。", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                _filter = GUILayout.Toolbar(_filter, Filters);
                var categories = new[] { "全部类别" }.Concat(_report.Issues.Select(item => item.Category).Distinct()).ToArray();
                int index = Math.Max(0, Array.IndexOf(categories, _category));
                _category = categories[EditorGUILayout.Popup(index, categories, GUILayout.Width(130))];
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(100));
            }
            var visible = _report.Issues.Where(Matches).OrderByDescending(item => item.Severity).ToList();
            int pages = Math.Max(1, (visible.Count + 99) / 100);
            _page = Mathf.Clamp(_page, 0, pages - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("上一页", GUILayout.Width(65))) _page = Math.Max(0, _page - 1);
                EditorGUILayout.LabelField($"{_page + 1} / {pages} 页 · {visible.Count} 项");
                if (GUILayout.Button("下一页", GUILayout.Width(65))) _page = Math.Min(pages - 1, _page + 1);
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var issue in visible.Skip(_page * 100).Take(100)) DrawIssue(issue);
            if (visible.Count == 0) EditorGUILayout.LabelField("当前筛选下没有结果。", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        internal static void DrawIssue(EmberValidationIssue issue)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var severity = issue.Severity == EmberValidationSeverity.Error ? MessageType.Error
                    : issue.Severity == EmberValidationSeverity.Warning ? MessageType.Warning : MessageType.None;
                EditorGUILayout.HelpBox($"{SeverityLabel(issue.Severity)} · {issue.Category} · {issue.Message}", severity);
                if (!string.IsNullOrEmpty(issue.AssetPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.SelectableLabel(issue.AssetPath, EditorStyles.miniLabel, GUILayout.Height(18));
                        var asset = AssetDatabase.LoadMainAssetAtPath(issue.AssetPath);
                        using (new EditorGUI.DisabledScope(asset == null))
                            if (GUILayout.Button("定位", GUILayout.Width(48)))
                            { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
                        if (GUILayout.Button("复制路径", GUILayout.Width(68)))
                            EditorGUIUtility.systemCopyBuffer = issue.AssetPath;
                    }
                }
                if (!string.IsNullOrEmpty(issue.Suggestion))
                    EditorGUILayout.LabelField(issue.Suggestion, EditorStyles.wordWrappedMiniLabel);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void Scan()
        {
            if (_context.OperationsBlocked) return;
            _context.IsBusy = true;
            _error = null;
            try
            {
                _report = EmberProjectValidationService.Scan();
                _revision = _context.Revision;
                _page = 0;
            }
            catch (Exception ex) { _report = null; _error = "检查未完成：" + ex.Message; }
            finally { _context.IsBusy = false; _context.Repaint(); }
        }

        private bool Matches(EmberValidationIssue issue)
        {
            if (_category != "全部类别" && issue.Category != _category) return false;
            if (_filter == 1 && issue.Severity != EmberValidationSeverity.Error) return false;
            if (_filter == 2 && issue.Severity != EmberValidationSeverity.Warning) return false;
            if (_filter == 3 && issue.Severity != EmberValidationSeverity.Difference) return false;
            if (_filter == 4 && issue.Severity != EmberValidationSeverity.Passed && issue.Severity != EmberValidationSeverity.Information) return false;
            return string.IsNullOrEmpty(_search)
                || (issue.Message + " " + issue.AssetPath + " " + issue.Suggestion).IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SeverityLabel(EmberValidationSeverity severity)
        {
            switch (severity)
            {
                case EmberValidationSeverity.Error: return "错误";
                case EmberValidationSeverity.Warning: return "警告";
                case EmberValidationSeverity.Difference: return "差异";
                case EmberValidationSeverity.Passed: return "通过";
                default: return "说明";
            }
        }

        #endregion
    }
}
