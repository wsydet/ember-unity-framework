// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Console 日志导出工具 —— 抓取控制台日志，支持按 EmberDebug SO 标签过滤后导出为 .txt 文件。
    ///
    /// 使用方式：
    ///   1. Tools → Ember → 控制台日志导出
    ///   2. 窗口打开后自动开始采集日志
    ///   3. 可选加载 EmberDebugConfig 进行标签过滤
    ///   4. 点击导出保存为 .txt
    /// </summary>
    public class ConsoleLogExporter : EmberEditorWindow
    {
        protected override string MenuPath => "Ember/Tool/控制台日志导出";
        protected override string WindowTitle => "Console Log Exporter";
        protected override Vector2 WindowSize => new(600, 700);
        protected override string WindowVersion => "v1.0";

        private readonly List<LogEntry> _logs = new();
        private readonly List<LogEntry> _filteredLogs = new();
        private bool _capturing;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private string _textFilter = "";
        private string _tagFilter = "";
        private Vector2 _scrollPos;
        private bool _autoScroll = true;
        private int _maxLogs = 5000;
        private HashSet<string> _disabledTags = new();

        private struct LogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
            public DateTime Time;
        }

        [MenuItem("Ember/Tool/控制台日志导出", false, 200)]
        public static void ShowWindow()
        {
            var win = GetWindow<ConsoleLogExporter>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Ember/Tool/导出最近 100 条日志到桌面", false, 210)]
        public static void QuickExport()
        {
            var lang = EmberEditorWindow.GlobalLang;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filename = EditorToolUtility.L10n(lang, "Unity_Console_", "Unity_控制台_") + $"{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(desktop, filename);
            ExportLogFile(filePath, ReadEditorLog(tailLines: 100), null);
            EditorUtility.DisplayDialog("Ember",
                EditorToolUtility.L10n(lang,
                    $"Logs exported to:\n{filePath}",
                    $"日志已导出到:\n{filePath}"),
                "OK");
            EditorUtility.RevealInFinder(filePath);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _capturing = true;
            Application.logMessageReceived += OnLogReceived;
            LoadDisabledTagsFromSO();
        }

        protected override void OnDisable()
        {
            Application.logMessageReceived -= OnLogReceived;
            _capturing = false;
            base.OnDisable();
        }

        protected override void DrawContent()
        {
            DrawStatusBar();
            DrawSeparatorLine();
            DrawFilters();
            DrawSeparatorLine();
            DrawActionButtons();
            DrawSeparatorLine();
            DrawLogList();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(L10n($"Captured: {_logs.Count} logs", $"已采集: {_logs.Count} 条日志"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_capturing ? L10n("Pause", "暂停") : L10n("Resume", "继续"), GUILayout.Width(60)))
            {
                if (_capturing) { _capturing = false; Application.logMessageReceived -= OnLogReceived; }
                else { _capturing = true; Application.logMessageReceived += OnLogReceived; }
            }
            if (GUILayout.Button(L10n("Clear", "清空"), GUILayout.Width(60)))
            { _logs.Clear(); _filteredLogs.Clear(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Filters", "过滤条件"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _showInfo = EditorGUILayout.ToggleLeft(L10n("Info", "信息"), _showInfo, GUILayout.Width(60));
            _showWarnings = EditorGUILayout.ToggleLeft(L10n("Warning", "警告"), _showWarnings, GUILayout.Width(70));
            _showErrors = EditorGUILayout.ToggleLeft(L10n("Error", "错误"), _showErrors, GUILayout.Width(60));
            _autoScroll = EditorGUILayout.ToggleLeft(L10n("Auto Scroll", "自动滚动"), _autoScroll, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            _textFilter = EditorGUILayout.TextField(L10n("Text Filter", "文本过滤"), _textFilter);
            _tagFilter = EditorGUILayout.TextField(L10n("Tag Filter (e.g. Audio,UI)", "标签过滤 (如 Audio,UI)"), _tagFilter);
            if (GUILayout.Button(L10n("Apply Filters", "应用过滤"), GUILayout.Width(120)))
                ApplyFilters();
            if (_disabledTags.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Format(L10n(
                    "EmberDebug tag filter loaded: {0} disabled tags detected.",
                    "已加载 EmberDebug 标签过滤: {0} 个标签已禁用。"),
                    _disabledTags.Count), MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Export", "导出"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(L10n("Export All to Desktop", "导出全部到桌面"), GUILayout.Height(40)))
                QuickExportAll();
            if (GUILayout.Button(L10n("Export Filtered", "导出过滤后日志"), GUILayout.Height(40)))
                ExportFiltered();
            EditorGUILayout.EndHorizontal();
            _maxLogs = EditorGUILayout.IntField(L10n("Max Logs", "最大日志数"), _maxLogs);
            if (_maxLogs < 100) _maxLogs = 100;
            if (_maxLogs > 50000) _maxLogs = 50000;
            EditorGUILayout.EndVertical();
        }

        private void DrawLogList()
        {
            ApplyFilters();
            EditorGUILayout.LabelField(string.Format(L10n($"Showing {_filteredLogs.Count} logs:", $"显示 {_filteredLogs.Count} 条日志:")), EditorStyles.boldLabel);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box", GUILayout.ExpandHeight(true));
            int startIdx = Mathf.Max(0, _filteredLogs.Count - 500);
            for (int i = startIdx; i < _filteredLogs.Count; i++)
            {
                var entry = _filteredLogs[i];
                Color bgColor = entry.Type switch
                {
                    LogType.Error or LogType.Exception or LogType.Assert => new Color(0.4f, 0.15f, 0.15f),
                    LogType.Warning => new Color(0.35f, 0.3f, 0.05f),
                    _ => Color.clear,
                };
                if (bgColor != Color.clear)
                {
                    Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                    EditorGUI.DrawRect(r, bgColor);
                }
                EditorGUILayout.BeginHorizontal();
                string timeStr = entry.Time.ToString("HH:mm:ss");
                EditorGUILayout.LabelField(timeStr, GUILayout.Width(70));
                var style = entry.Type switch
                {
                    LogType.Error or LogType.Exception or LogType.Assert => ErrorStyle,
                    LogType.Warning => WarningStyle,
                    _ => EditorStyles.miniLabel,
                };
                EditorGUILayout.LabelField(entry.Message, style ?? EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            if (_autoScroll && _filteredLogs.Count > 0 && Event.current.type == EventType.Repaint)
            {
                _scrollPos.y = float.MaxValue;
                Repaint();
            }
        }

        private void OnLogReceived(string message, string stackTrace, LogType type)
        {
            if (!_capturing) return;
            _logs.Add(new LogEntry { Message = message, StackTrace = stackTrace, Type = type, Time = DateTime.Now });
            while (_logs.Count > _maxLogs) _logs.RemoveAt(0);
            Repaint();
        }

        private void ApplyFilters()
        {
            _filteredLogs.Clear();
            var query = _logs.AsEnumerable();
            if (!_showInfo) query = query.Where(e => e.Type != LogType.Log);
            if (!_showWarnings) query = query.Where(e => e.Type != LogType.Warning);
            if (!_showErrors) query = query.Where(e => e.Type != LogType.Error && e.Type != LogType.Exception && e.Type != LogType.Assert);
            if (!string.IsNullOrEmpty(_textFilter))
                query = query.Where(e => e.Message.IndexOf(_textFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!string.IsNullOrEmpty(_tagFilter))
            {
                var tags = _tagFilter.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
                query = query.Where(e => tags.Any(t => e.Message.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            if (_disabledTags.Count > 0)
                query = query.Where(e => !_disabledTags.Any(tag => !string.IsNullOrEmpty(tag) && e.Message.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0));
            _filteredLogs.AddRange(query);
        }

        private void LoadDisabledTagsFromSO()
        {
            _disabledTags.Clear();
            var guids = AssetDatabase.FindAssets("t:ScriptableObject EmberDebugConfig");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null || so.GetType().Name != "EmberDebugConfigSO") continue;
                using var serializedObj = new SerializedObject(so);
                var globalOpen = serializedObj.FindProperty("_globalOpen");
                if (globalOpen != null && !globalOpen.boolValue) { _disabledTags.Add("[ALL]"); break; }
                var disabledTags = serializedObj.FindProperty("_disabledTags");
                if (disabledTags != null && disabledTags.isArray)
                {
                    for (int i = 0; i < disabledTags.arraySize; i++)
                    {
                        string tag = disabledTags.GetArrayElementAtIndex(i).stringValue;
                        if (!string.IsNullOrEmpty(tag)) _disabledTags.Add(tag);
                    }
                }
                break;
            }
        }

        private void QuickExportAll()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string prefix = L10n("Unity_Console_", "Unity_控制台_");
            string filePath = Path.Combine(desktop, $"{prefix}{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            ExportLogFile(filePath, _logs, _disabledTags.Count > 0 ? _disabledTags : null);
            EditorUtility.DisplayDialog("Ember",
                L10n($"Exported {_logs.Count} logs to:\n{filePath}", $"已导出 {_logs.Count} 条日志到:\n{filePath}"),
                "OK");
            EditorUtility.RevealInFinder(filePath);
        }

        private void ExportFiltered()
        {
            ApplyFilters();
            if (_filteredLogs.Count == 0) { EditorUtility.DisplayDialog("Ember", L10n("No logs match current filters.", "没有符合过滤条件的日志。"), "OK"); return; }
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string prefix = L10n("Unity_Console_Filtered_", "Unity_控制台_过滤_");
            string filePath = Path.Combine(desktop, $"{prefix}{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            ExportLogFile(filePath, _filteredLogs, null);
            EditorUtility.DisplayDialog("Ember",
                L10n($"Exported {_filteredLogs.Count} logs to:\n{filePath}", $"已导出 {_filteredLogs.Count} 条日志到:\n{filePath}"),
                "OK");
            EditorUtility.RevealInFinder(filePath);
        }

        private static void ExportLogFile(string filePath, IEnumerable<LogEntry> entries, HashSet<string> disabledTags)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Unity Console Log Export ===");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Project: {Application.productName}");
            if (disabledTags != null && disabledTags.Count > 0) sb.AppendLine($"Filtered Tags: {string.Join(", ", disabledTags)}");
            sb.AppendLine(new string('-', 80));
            foreach (var entry in entries)
            {
                string typeLabel = entry.Type switch { LogType.Error => "[ERROR]", LogType.Exception => "[EXCEPTION]", LogType.Warning => "[WARNING]", LogType.Assert => "[ASSERT]", _ => "[INFO]" };
                sb.AppendLine($"{entry.Time:HH:mm:ss.fff} {typeLabel} {entry.Message}");
                if (!string.IsNullOrEmpty(entry.StackTrace)) sb.AppendLine($"  {entry.StackTrace}");
            }
            sb.AppendLine(new string('-', 80));
            sb.AppendLine($"Total: {entries.Count()} logs");
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static List<LogEntry> ReadEditorLog(int tailLines)
        {
            var entries = new List<LogEntry>();
            try
            {
                string logPath = Application.consoleLogPath;
                if (!File.Exists(logPath)) return entries;
                var allLines = File.ReadAllLines(logPath);
                int start = Mathf.Max(0, allLines.Length - tailLines * 2);
                for (int i = start; i < allLines.Length; i++)
                    if (!string.IsNullOrWhiteSpace(allLines[i]))
                        entries.Add(new LogEntry { Message = allLines[i], Type = LogType.Log, Time = DateTime.Now });
            }
            catch { }
            return entries;
        }

        private static GUIStyle _errorStyle, _warningStyle;
        private static GUIStyle ErrorStyle => _errorStyle ??= new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
        private static GUIStyle WarningStyle => _warningStyle ??= new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.85f, 0.3f) } };
    }
}
#endif
