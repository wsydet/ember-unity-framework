// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEditor;
using UnityEngine;

using Ember.SceneUI.Integration;

namespace Ember.SceneUI.Editor
{
    /// <summary>SceneUI 运行时只读诊断面板。</summary>
    public sealed class SceneUIDiagnosticsWindow : EditorWindow
    {
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
            EditorGUILayout.LabelField("SceneUI Runtime Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后显示运行时统计。", MessageType.Info);
                return;
            }

            SceneUIPageHost[] hosts = FindObjectsByType<SceneUIPageHost>(
                FindObjectsInactive.Include);
            bool drewDiagnostics = false;
            for (int i = 0; i < hosts.Length; i++)
            {
                SceneUIPageHost host = hosts[i];
                if (!host || !host.TryGetDiagnostics(out SceneUIDiagnostics diagnostics))
                    continue;

                EditorGUILayout.LabelField($"Host: {host.name}", EditorStyles.boldLabel);
                DrawDiagnostics(diagnostics);
                drewDiagnostics = true;
            }

            if (!drewDiagnostics)
            {
                EditorGUILayout.HelpBox(
                    "当前没有已绑定的 SceneUIPageHost。SceneUI Engine 由业务模块按 Phase 创建。",
                    MessageType.Info);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void DrawDiagnostics(in SceneUIDiagnostics diagnostics)
        {
            DrawSection("Active");
            DrawValue("Contexts", diagnostics.ActiveContextCount);
            DrawValue("Entries", diagnostics.ActiveEntryCount);
            DrawValue("Views", diagnostics.ActiveViewCount);
            DrawValue("Pending delayed recycle", diagnostics.PendingDelayedRecycleCount);

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
