// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>配置表的校验、预览、全量事务烘焙和诊断定位入口。</summary>
    public sealed class EmberTableEditorWindow : EditorWindow
    {
        #region 内部参数

        private Vector2 _scroll;
        private EmberTableDefinition _selected;
        private IReadOnlyList<EmberTableDiagnostic> _diagnostics = Array.Empty<EmberTableDiagnostic>();
        private IReadOnlyList<EmberTableArtifactAction> _actions = Array.Empty<EmberTableArtifactAction>();
        private IReadOnlyList<string> _rows = Array.Empty<string>();
        private string _summary = "选择 Definition 后可定位源文件或浏览正式产物。";

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnGUI()
        {
            _selected = EditorGUILayout.ObjectField("当前 Definition", _selected, typeof(EmberTableDefinition), false)
                as EmberTableDefinition;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("校验全部")) RunValidation();
                if (GUILayout.Button("预览全部变更")) RunPipeline(false);
                if (GUILayout.Button("烘焙并生成全部")) RunPipeline(true);
            }
            if (GUILayout.Button("创建/补齐项目 Table 接线")) CreateProjectScaffold();
            using (new EditorGUI.DisabledScope(!_selected))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("校验当前")) RunCurrentValidation();
                if (GUILayout.Button("烘焙当前（整批）")) RunCurrentBake();
                if (GUILayout.Button("定位 Definition")) Ping(_selected);
                if (GUILayout.Button("定位 Row")) Ping(_selected.RowScript);
            }
            using (new EditorGUI.DisabledScope(!_selected))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开源文件")) OpenSource();
                if (GUILayout.Button("定位二进制")) Ping(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_selected.RuntimeOutputPath));
                if (GUILayout.Button("浏览正式产物")) BrowseArtifact();
            }

            EditorGUILayout.HelpBox(_summary, MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _diagnostics.Count; i++) DrawDiagnostic(_diagnostics[i]);
            for (int i = 0; i < _actions.Count; i++)
                EditorGUILayout.LabelField($"{_actions[i].Kind}: {_actions[i].AssetPath}");
            for (int i = 0; i < _rows.Count; i++) EditorGUILayout.SelectableLabel(_rows[i]);
            EditorGUILayout.EndScrollView();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [MenuItem("Ember/配置表中心")]
        private static void Open()
        {
            GetWindow<EmberTableEditorWindow>("Ember 配置表");
        }

        private void RunValidation()
        {
            EmberTableValidationResult result = EmberTablePipeline.ValidateAll();
            _diagnostics = result.Diagnostics;
            _actions = Array.Empty<EmberTableArtifactAction>();
            _rows = Array.Empty<string>();
            _summary = result.Succeeded
                ? $"校验通过：{result.Tables.Count} 张表。"
                : $"校验失败：{result.Diagnostics.Count} 条诊断。";
        }

        private void RunCurrentValidation()
        {
            EmberTableValidationResult result = EmberTablePipeline.Validate(_selected);
            _diagnostics = result.Diagnostics;
            _actions = Array.Empty<EmberTableArtifactAction>();
            _rows = Array.Empty<string>();
            _summary = result.Succeeded ? "当前表校验通过。" : $"当前表校验失败：{result.Diagnostics.Count} 条诊断。";
        }

        private void RunPipeline(bool commit)
        {
            EmberTablePipelineResult result = commit
                ? EmberTablePipeline.BakeAndGenerateAll()
                : EmberTablePipeline.PreviewAll();
            _diagnostics = result.Diagnostics;
            _actions = result.Actions;
            _rows = Array.Empty<string>();
            _summary = result.Succeeded
                ? commit ? "全量产物已在一个事务中提交。" : "预览完成；尚未写入任何正式产物。"
                : "操作失败；正式产物未改变或已回滚。";
        }

        private void RunCurrentBake()
        {
            RunPipeline(true);
            if (_diagnostics.Count == 0)
                _summary = "当前表已烘焙；为保持 Catalog 与所有权清单一致，本次按整批事务核对并提交全部表。";
        }

        private void OpenSource()
        {
            if (!_selected || !_selected.Source) return;
            EditorUtility.OpenWithDefaultApp(AssetDatabase.GetAssetPath(_selected.Source));
        }

        private void CreateProjectScaffold()
        {
            bool succeeded = EmberTableProjectScaffold.TryCreate(out string summary, out var diagnostics);
            _diagnostics = diagnostics;
            _actions = Array.Empty<EmberTableArtifactAction>();
            _rows = Array.Empty<string>();
            _summary = summary ?? (succeeded ? "项目接线已创建。" : "项目接线创建失败。");
        }

        private void BrowseArtifact()
        {
            if (EmberTableArtifactBrowser.TryLoad(
                    _selected,
                    out IEmberTableData table,
                    out EmberTableLoadResult result,
                    out string error))
            {
                string sourceHash = result.Tables.Count > 0 ? result.Tables[0].SourceHash : "unknown";
                _summary = $"Runtime Binding 解码成功：{table.TableId}，{table.Count} 行，Source Hash {sourceHash}。";
                _diagnostics = result.Diagnostics;
                _actions = Array.Empty<EmberTableArtifactAction>();
                _rows = EmberTableArtifactBrowser.DescribeRows(_selected, table);
            }
            else
            {
                _summary = error;
                _diagnostics = Array.Empty<EmberTableDiagnostic>();
                _actions = Array.Empty<EmberTableArtifactAction>();
                _rows = Array.Empty<string>();
            }
        }

        private static void DrawDiagnostic(EmberTableDiagnostic diagnostic)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(diagnostic.ToString(), GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(diagnostic.FilePath) && GUILayout.Button("定位", GUILayout.Width(48)))
                {
                    UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(diagnostic.FilePath);
                    Ping(asset);
                }
            }
        }

        private static void Ping(UnityEngine.Object asset)
        {
            if (!asset) return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        #endregion
    }
}
