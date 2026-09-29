// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEditor;
using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using UnityEngine;

using Ember.SceneUI.Integration;

namespace Ember.SceneUI.Editor
{
    /// <summary>SceneUI 运行时只读诊断面板。</summary>
    public sealed class SceneUIDiagnosticsWindow : EditorWindow
    {
        private readonly List<(SceneUIPageHost Host, SceneUIDiagnostics Data)> _snapshots = new();
        private Vector2 _scroll;
        private string _filter = "";
        private bool _freeze, _showDetails;
        #region 生命周期

        [MenuItem("Ember/Diagnostics/Scene UI")]
        private static void Open()
        {
            SceneUIDiagnosticsWindow window = GetWindow<SceneUIDiagnosticsWindow>();
            window.titleContent = new GUIContent("Scene UI");
            window.minSize = new Vector2(360f, 420f);
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            SirenixEditorGUI.Title("Scene UI 诊断", _freeze ? "快照已冻结" : "实时只读统计", TextAlignment.Left, true);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后显示运行时统计。", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
                _freeze = GUILayout.Toggle(_freeze, "冻结快照", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            _showDetails = SirenixEditorGUI.Foldout(_showDetails, "显示池、预热、投影与排序明细");
            // 在 Layout 固定同一轮绘制的数据，避免运行时 Host 数量变化破坏控件结构。
            if (Event.current.type == EventType.Layout && !_freeze)
            {
                _snapshots.Clear();
                foreach (var host in FindObjectsByType<SceneUIPageHost>(FindObjectsInactive.Include))
                    if (host && host.TryGetDiagnostics(out SceneUIDiagnostics data)) _snapshots.Add((host, data));
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            bool drewDiagnostics = false;
            for (int i = 0; i < _snapshots.Count; i++)
            {
                SceneUIPageHost host = _snapshots[i].Host;
                if (!host || (!string.IsNullOrEmpty(_filter) && host.name.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) < 0))
                    continue;
                SirenixEditorGUI.BeginBox(host.name);
                if (GUILayout.Button("定位 Host", GUILayout.Width(90))) EditorGUIUtility.PingObject(host);
                DrawDiagnostics(_snapshots[i].Data, _showDetails);
                SirenixEditorGUI.EndBox();
                drewDiagnostics = true;
            }

            if (!drewDiagnostics)
            {
                EditorGUILayout.HelpBox(
                    _snapshots.Count > 0 ? "没有匹配的 Host。" : "当前没有已绑定的 SceneUIPageHost。SceneUI Engine 由业务模块按 Phase 创建。",
                    MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void DrawDiagnostics(in SceneUIDiagnostics diagnostics, bool details)
        {
            DrawSection("Active");
            DrawValue("Contexts", diagnostics.ActiveContextCount);
            DrawValue("Entries", diagnostics.ActiveEntryCount);
            DrawValue("Views", diagnostics.ActiveViewCount);
            DrawValue("Pending delayed recycle", diagnostics.PendingDelayedRecycleCount);
            if (!details) return;

            DrawSection("Pool");
            DrawValue("Pooled views", diagnostics.PooledViewCount);
            DrawValue("Hits", diagnostics.PoolHitCount);
            DrawValue("Misses", diagnostics.PoolMissCount);
            DrawValue("Recycled after delay", diagnostics.RecycledAfterDelayCount);

            DrawSection("Prewarm");
            DrawValue("Pending", diagnostics.PendingPrewarmCount);
            DrawValue("Completed", diagnostics.PrewarmedViewCount);
            DrawValue("Failures", diagnostics.PrewarmFailureCount);

            DrawSection("Projection / Visibility");
            DrawValue("Projected", diagnostics.ProjectedCount);
            DrawValue("Culled", diagnostics.CulledCount);
            DrawValue("Invalid anchor", diagnostics.InvalidAnchorCount);
            DrawValue("Projection invalid", diagnostics.ProjectionInvalidCount);
            DrawValue("Behind camera", diagnostics.BehindCameraCount);
            DrawValue("Before near clip", diagnostics.BeforeNearClipCount);
            DrawValue("Beyond far clip", diagnostics.BeyondFarClipCount);
            DrawValue("Out of bounds", diagnostics.OutOfBoundsCount);
            DrawValue("View unavailable", diagnostics.ViewUnavailableCount);

            DrawSection("Occlusion");
            DrawValue("Tests", diagnostics.OcclusionTestCount);
            DrawValue("Occluded", diagnostics.OcclusionHitCount);
            DrawValue("Failures", diagnostics.OcclusionFailureCount);

            DrawSection("Flush / Sorting");
            DrawValue("Last frame", diagnostics.LastFlushFrame);
            DrawValue("Last duration", $"{diagnostics.LastFlushDurationMs:F3} ms");
            DrawValue("Last GC allocation", $"{diagnostics.LastFlushAllocatedBytes} B");
            DrawValue("Sorting passes", diagnostics.SortingPassCount);
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawValue(string label, object value)
        {
            EditorGUILayout.LabelField(label, value?.ToString() ?? "-");
        }

        #endregion
    }
}
