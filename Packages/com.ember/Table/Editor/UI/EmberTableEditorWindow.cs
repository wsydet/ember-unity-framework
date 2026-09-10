// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>配置表声明、Schema、数据、使用代码、校验和事务烘焙的统一入口。</summary>
    public sealed class EmberTableEditorWindow : EditorWindow
    {
        #region 内部参数

        private const float TableListWidth = 270f;
        private const float DataColumnWidth = 170f;
        private const int MaxPreviewRows = 200;
        private static readonly GUIContent ExportAllButton = new(
            "导出全部表",
            "首次接入或 Row Schema、输出路径、Definition 清单变化时使用。");
        private static readonly GUIContent ExportCurrentButton = new(
            "导出当前表",
            "CSV/TSV 数据变化时只验证并替换当前表二进制；结构变化会提示先全量导出。");

        private IReadOnlyList<EmberTableDefinition> _definitions = Array.Empty<EmberTableDefinition>();
        private EmberTableValidationResult _catalogValidation;
        private EmberTableDefinition _selected;
        private EmberTableSchema _selectedSchema;
        private EmberTableValidatedData _selectedData;
        private IReadOnlyList<EmberTableDiagnostic> _selectedDiagnostics = Array.Empty<EmberTableDiagnostic>();
        private IReadOnlyList<EmberTableDiagnostic> _operationDiagnostics = Array.Empty<EmberTableDiagnostic>();
        private IReadOnlyList<EmberTableArtifactAction> _actions = Array.Empty<EmberTableArtifactAction>();
        private IReadOnlyList<string> _artifactRows = Array.Empty<string>();
        private Vector2 _tableListScroll;
        private Vector2 _detailScroll;
        private Vector2 _dataScroll;
        private Vector2 _resultScroll;
        private string _search = string.Empty;
        private string _usageCode = string.Empty;
        private string _summary = "正在扫描项目中的 Table Definition。";
        private bool _showSchema = true;
        private bool _showData = true;
        private bool _showCode = true;
        private bool _showDiagnostics = true;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnEnable()
        {
            minSize = new Vector2(900f, 560f);
            EditorApplication.projectChanged += RefreshCatalog;
            RefreshCatalog();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshCatalog;
        }

        private void OnGUI()
        {
            DrawMainToolbar();
            EditorGUILayout.HelpBox(_summary, MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTableList();
                GUILayout.Box(GUIContent.none, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
                DrawSelectedTable();
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [MenuItem("Ember/配置表中心")]
        private static void Open()
        {
            GetWindow<EmberTableEditorWindow>("Ember 配置表");
        }

        private void DrawMainToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新声明", EditorStyles.toolbarButton)) RefreshCatalog();
                if (GUILayout.Button("校验全部", EditorStyles.toolbarButton)) RunValidation();
                if (GUILayout.Button("预览全部变更", EditorStyles.toolbarButton)) RunPipeline(false);
                if (GUILayout.Button(ExportAllButton, EditorStyles.toolbarButton)) RunPipeline(true);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("创建/补齐项目 Table 接线", EditorStyles.toolbarButton))
                    CreateProjectScaffold();
            }
        }

        private void DrawTableList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(TableListWidth)))
            {
                EditorGUILayout.LabelField($"已声明的表（{_definitions.Count}）", EditorStyles.boldLabel);
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                _tableListScroll = EditorGUILayout.BeginScrollView(_tableListScroll);

                int visibleCount = 0;
                for (int i = 0; i < _definitions.Count; i++)
                {
                    EmberTableDefinition definition = _definitions[i];
                    if (!MatchesSearch(definition)) continue;
                    visibleCount++;

                    bool valid = _catalogValidation != null
                                 && _catalogValidation.Tables.Any(item => ReferenceEquals(item.Definition, definition));
                    int errorCount = CountErrors(definition);
                    string state = valid ? "✓" : errorCount > 0 ? "!" : "•";
                    string rowName = definition.RowType == null ? "未指定 Row" : definition.RowType.Name;
                    string label = $"{state}  {DisplayTableId(definition)}\n    {rowName}";
                    bool selected = GUILayout.Toggle(
                        ReferenceEquals(_selected, definition),
                        label,
                        "Button",
                        GUILayout.Height(44f));
                    if (selected && !ReferenceEquals(_selected, definition)) Select(definition);
                }

                if (_definitions.Count == 0)
                    EditorGUILayout.HelpBox(
                        "项目中还没有 EmberTableDefinition。先声明 Row，再通过 Assets/Create/Ember/Table Definition 创建表定义。",
                        MessageType.Info);
                else if (visibleCount == 0)
                    EditorGUILayout.HelpBox("没有匹配的 Table ID 或 Row 类型。", MessageType.Info);

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedTable()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                if (!_selected)
                {
                    DrawEmptyGuide();
                    return;
                }

                DrawSelectedToolbar();
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                DrawOverview();
                DrawSchema();
                DrawData();
                DrawUsageCode();
                DrawDiagnosticsAndResults();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSelectedToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EmberTableDefinition next = EditorGUILayout.ObjectField(
                    _selected,
                    typeof(EmberTableDefinition),
                    false,
                    GUILayout.MinWidth(180f)) as EmberTableDefinition;
                if (!ReferenceEquals(next, _selected)) Select(next);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("校验当前", EditorStyles.toolbarButton)) RunCurrentValidation();
                if (GUILayout.Button(ExportCurrentButton, EditorStyles.toolbarButton)) RunCurrentBake();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("定位 Definition")) Ping(_selected);
                if (GUILayout.Button("定位 Row")) Ping(_selected.RowScript);
                if (GUILayout.Button("打开源文件")) OpenSource();
                if (GUILayout.Button("定位二进制"))
                    Ping(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_selected.RuntimeOutputPath));
                if (GUILayout.Button("浏览正式产物")) BrowseArtifact();
            }
        }

        private void DrawOverview()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(DisplayTableId(_selected), EditorStyles.largeLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawValue("Definition", AssetDatabase.GetAssetPath(_selected));
                DrawValue("Row 类型", _selected.RowType?.FullName ?? "未指定");
                DrawValue("加载策略", _selected.Required ? "Required（失败阻止整批切换）" : "Optional（失败时省略）");
                DrawValue("源文件", _selected.Source ? AssetDatabase.GetAssetPath(_selected.Source) : "未指定");
                DrawValue("Runtime 产物", _selected.RuntimeOutputPath);
                DrawValue("Resources 路径", _selected.GetLogicalResourcePath() ?? "无效");
                if (_selectedSchema != null)
                {
                    DrawValue("Schema Hash", _selectedSchema.SchemaHash);
                    DrawValue("主键", $"{_selectedSchema.PrimaryKey.ColumnName} → {_selectedSchema.PrimaryKey.MemberName}");
                }
                DrawValue("解析行数", _selectedData == null ? "校验未通过" : _selectedData.Rows.Count.ToString());
            }
        }

        private void DrawSchema()
        {
            _showSchema = EditorGUILayout.Foldout(_showSchema, "字段结构（程序中的 Row Schema）", true);
            if (!_showSchema) return;
            if (_selectedSchema == null)
            {
                EditorGUILayout.HelpBox("Row Schema 无法解析，请先处理当前表诊断。", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("CSV/TSV 列", GUILayout.Width(150f));
                    GUILayout.Label("C# 成员", GUILayout.Width(150f));
                    GUILayout.Label("C# 类型", GUILayout.Width(180f));
                    GUILayout.Label("约束", GUILayout.ExpandWidth(true));
                }
                for (int i = 0; i < _selectedSchema.Columns.Count; i++)
                {
                    EmberTableColumnSchema column = _selectedSchema.Columns[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.SelectableLabel(column.ColumnName, GUILayout.Width(150f), GUILayout.Height(18f));
                        EditorGUILayout.SelectableLabel(column.MemberName, GUILayout.Width(150f), GUILayout.Height(18f));
                        EditorGUILayout.SelectableLabel(
                            EmberTableExplorerPresenter.DisplayTypeName(column.ValueType),
                            GUILayout.Width(180f),
                            GUILayout.Height(18f));
                        GUILayout.Label(ColumnConstraints(column), GUILayout.ExpandWidth(true));
                    }
                }
            }
        }

        private void DrawData()
        {
            int rowCount = _selectedData?.Rows.Count ?? 0;
            _showData = EditorGUILayout.Foldout(_showData, $"数据预览（严格类型值，{rowCount} 行）", true);
            if (!_showData) return;
            if (_selectedData == null || _selectedSchema == null)
            {
                EditorGUILayout.HelpBox("当前数据未通过严格校验，因此不会展示可能误导程序的宽松解析结果。", MessageType.Warning);
                return;
            }
            if (rowCount == 0)
            {
                EditorGUILayout.HelpBox("表已声明且校验通过，但当前没有数据行。", MessageType.Info);
                return;
            }

            int displayCount = Math.Min(rowCount, MaxPreviewRows);
            float height = Mathf.Clamp(42f + displayCount * 20f, 90f, 310f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll, GUILayout.Height(height));
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("#", GUILayout.Width(38f));
                    for (int i = 0; i < _selectedSchema.Columns.Count; i++)
                    {
                        EmberTableColumnSchema column = _selectedSchema.Columns[i];
                        GUILayout.Label(
                            column.ColumnName + "  ·  " + EmberTableExplorerPresenter.DisplayTypeName(column.ValueType),
                            GUILayout.Width(DataColumnWidth));
                    }
                }
                for (int rowIndex = 0; rowIndex < displayCount; rowIndex++)
                {
                    object[] row = _selectedData.Rows[rowIndex];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label((rowIndex + 1).ToString(), GUILayout.Width(38f));
                        for (int columnIndex = 0; columnIndex < _selectedSchema.Columns.Count; columnIndex++)
                        {
                            EmberTableColumnSchema column = _selectedSchema.Columns[columnIndex];
                            object value = columnIndex < row.Length ? row[columnIndex] : null;
                            EditorGUILayout.SelectableLabel(
                                EmberTableExplorerPresenter.FormatCSharpLiteral(value, column.ValueType),
                                GUILayout.Width(DataColumnWidth),
                                GUILayout.Height(18f));
                        }
                    }
                }
                if (rowCount > displayCount)
                    GUILayout.Label($"仅显示前 {displayCount} 行，共 {rowCount} 行。", EditorStyles.miniLabel);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawUsageCode()
        {
            _showCode = EditorGUILayout.Foldout(_showCode, "可直接复制的强类型查询代码", true);
            if (!_showCode) return;
            if (_selectedSchema == null)
            {
                EditorGUILayout.HelpBox("Schema 有效后才会生成查询代码。", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "代码按当前 Table ID、Row 类型、主键、字段和二级索引生成。GameTableModule 使用模板默认命名；项目重命名后请对应调整。",
                    MessageType.None);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("复制代码", GUILayout.Width(90f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = _usageCode;
                        _summary = $"已复制表 [{_selected.TableId}] 的强类型查询代码。";
                    }
                }
                EditorGUILayout.SelectableLabel(
                    _usageCode,
                    EditorStyles.textArea,
                    GUILayout.MinHeight(Mathf.Clamp(110f + _selectedSchema.Columns.Count * 18f, 180f, 420f)));
            }
        }

        private void DrawDiagnosticsAndResults()
        {
            _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "诊断与最近操作", true);
            if (!_showDiagnostics) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedDiagnostics.Count == 0
                    && _operationDiagnostics.Count == 0
                    && _actions.Count == 0
                    && _artifactRows.Count == 0)
                {
                    EditorGUILayout.LabelField("当前表没有诊断，尚无需要展示的操作结果。", EditorStyles.miniLabel);
                    return;
                }

                _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.MaxHeight(260f));
                for (int i = 0; i < _selectedDiagnostics.Count; i++) DrawDiagnostic(_selectedDiagnostics[i]);
                for (int i = 0; i < _operationDiagnostics.Count; i++) DrawDiagnostic(_operationDiagnostics[i]);
                for (int i = 0; i < _actions.Count; i++)
                    EditorGUILayout.LabelField($"{_actions[i].Kind}: {_actions[i].AssetPath}");
                for (int i = 0; i < _artifactRows.Count; i++)
                    EditorGUILayout.SelectableLabel(_artifactRows[i], GUILayout.Height(18f));
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawEmptyGuide()
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("配置表声明浏览器", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "左侧会列出项目中的所有 EmberTableDefinition。选择一张表即可看到 Row 字段、严格转换后的源数据，以及可直接复制的查询代码。",
                MessageType.Info);
            const string example =
                "[EmberTable(\"sample\")]\n" +
                "public sealed class SampleRow\n" +
                "{\n" +
                "    [EmberTableKey, EmberTableColumn(\"id\")]\n" +
                "    public string Id { get; }\n\n" +
                "    [EmberTableConstructor]\n" +
                "    public SampleRow(string id) { Id = id; }\n" +
                "}";
            EditorGUILayout.SelectableLabel(example, EditorStyles.textArea, GUILayout.Height(170f));
        }

        private void RefreshCatalog()
        {
            EmberTableDefinition previous = _selected;
            _definitions = EmberTablePipeline.FindAllDefinitions();
            _catalogValidation = EmberTableValidationService.ValidateAll(_definitions.ToList());
            _selected = previous && _definitions.Contains(previous)
                ? previous
                : _definitions.FirstOrDefault();
            RefreshSelectedDetails();
            _operationDiagnostics = Array.Empty<EmberTableDiagnostic>();
            _actions = Array.Empty<EmberTableArtifactAction>();
            _artifactRows = Array.Empty<string>();
            _summary = _catalogValidation.Succeeded
                ? $"发现并验证 {_definitions.Count} 张声明表。选择左侧表可查看字段、数据与使用代码。"
                : $"发现 {_definitions.Count} 张声明表，其中有 {_catalogValidation.Diagnostics.Count} 条诊断。";
            Repaint();
        }

        private void RefreshSelectedDetails()
        {
            _selectedSchema = null;
            _selectedData = null;
            _selectedDiagnostics = Array.Empty<EmberTableDiagnostic>();
            _usageCode = string.Empty;
            if (!_selected) return;

            string path = AssetDatabase.GetAssetPath(_selected);
            EmberTableSchemaAnalyzer.TryAnalyze(
                _selected.RowType,
                _selected.TableId,
                path,
                out _selectedSchema,
                out IReadOnlyList<EmberTableDiagnostic> schemaDiagnostics);

            _selectedData = _catalogValidation?.Tables.FirstOrDefault(item =>
                ReferenceEquals(item.Definition, _selected));
            var diagnostics = new List<EmberTableDiagnostic>();
            if (_catalogValidation != null)
            {
                diagnostics.AddRange(_catalogValidation.Diagnostics.Where(item =>
                    string.Equals(item.TableId, _selected.TableId, StringComparison.Ordinal)
                    || string.Equals(item.FilePath, path, StringComparison.Ordinal)));
            }
            if (_selectedSchema == null)
                for (int i = 0; i < schemaDiagnostics.Count; i++)
                    if (!diagnostics.Contains(schemaDiagnostics[i])) diagnostics.Add(schemaDiagnostics[i]);
            _selectedDiagnostics = diagnostics.AsReadOnly();

            if (_selectedSchema != null)
                _usageCode = EmberTableExplorerPresenter.BuildUsageCode(
                    _selected,
                    _selectedSchema,
                    _selectedData?.Rows ?? Array.Empty<object[]>());
        }

        private void Select(EmberTableDefinition definition)
        {
            _selected = definition;
            RefreshSelectedDetails();
            _summary = definition
                ? $"当前表：{DisplayTableId(definition)}。"
                : "请选择一张 Table Definition。";
            Repaint();
        }

        private bool MatchesSearch(EmberTableDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(_search)) return true;
            return DisplayTableId(definition).IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                   || (definition.RowType?.FullName?.IndexOf(_search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private int CountErrors(EmberTableDefinition definition)
        {
            if (_catalogValidation == null) return 0;
            string path = AssetDatabase.GetAssetPath(definition);
            return _catalogValidation.Diagnostics.Count(item =>
                item.Severity == EmberTableDiagnosticSeverity.Error
                && (string.Equals(item.TableId, definition.TableId, StringComparison.Ordinal)
                    || string.Equals(item.FilePath, path, StringComparison.Ordinal)));
        }

        private static string DisplayTableId(EmberTableDefinition definition)
        {
            return string.IsNullOrEmpty(definition?.TableId) ? "<未声明 Table ID>" : definition.TableId;
        }

        private static string ColumnConstraints(EmberTableColumnSchema column)
        {
            var values = new List<string>();
            if (column.IsPrimaryKey) values.Add("主键");
            if (column.IsNullable) values.Add("可空");
            if (!string.IsNullOrEmpty(column.ReferenceTableId)) values.Add("引用 " + column.ReferenceTableId);
            return values.Count == 0 ? "—" : string.Join("，", values);
        }

        private static void DrawValue(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(110f));
                EditorGUILayout.SelectableLabel(value ?? string.Empty, GUILayout.Height(18f));
            }
        }

        private void RunValidation()
        {
            RefreshCatalog();
            _operationDiagnostics = _catalogValidation.Diagnostics;
            _summary = _catalogValidation.Succeeded
                ? $"校验通过：{_catalogValidation.Tables.Count} 张表。"
                : $"校验失败：{_catalogValidation.Diagnostics.Count} 条诊断。";
        }

        private void RunCurrentValidation()
        {
            EmberTableValidationResult result = EmberTablePipeline.Validate(_selected);
            RefreshCatalog();
            _operationDiagnostics = result.Diagnostics;
            _summary = result.Succeeded ? "当前表校验通过。" : $"当前表校验失败：{result.Diagnostics.Count} 条诊断。";
        }

        private void RunPipeline(bool commit)
        {
            EmberTablePipelineResult result = commit
                ? EmberTablePipeline.BakeAndGenerateAll()
                : EmberTablePipeline.PreviewAll();
            RefreshCatalog();
            _operationDiagnostics = result.Diagnostics;
            _actions = result.Actions;
            _summary = result.Succeeded
                ? commit ? "全量产物已在一个事务中提交。" : "预览完成；尚未写入任何正式产物。"
                : "操作失败；正式产物未改变或已回滚。";
        }

        private void RunCurrentBake()
        {
            EmberTablePipelineResult result = EmberTablePipeline.BakeCurrent(_selected);
            RefreshCatalog();
            _operationDiagnostics = result.Diagnostics;
            _actions = result.Actions;
            _summary = result.Succeeded
                ? $"已导出当前表 [{_selected.TableId}]；只更新该表二进制。"
                : "当前表导出失败；如 Row Schema、路径或表清单有变化，请先导出全部表。";
        }

        private void OpenSource()
        {
            if (!_selected || !_selected.Source) return;
            EditorUtility.OpenWithDefaultApp(AssetDatabase.GetAssetPath(_selected.Source));
        }

        private void CreateProjectScaffold()
        {
            bool succeeded = EmberTableProjectScaffold.TryCreate(out string summary, out var diagnostics);
            RefreshCatalog();
            _operationDiagnostics = diagnostics;
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
                _operationDiagnostics = result.Diagnostics;
                _actions = Array.Empty<EmberTableArtifactAction>();
                _artifactRows = EmberTableArtifactBrowser.DescribeRows(_selected, table);
            }
            else
            {
                _summary = error;
                _operationDiagnostics = Array.Empty<EmberTableDiagnostic>();
                _actions = Array.Empty<EmberTableArtifactAction>();
                _artifactRows = Array.Empty<string>();
            }
        }

        private static void DrawDiagnostic(EmberTableDiagnostic diagnostic)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(diagnostic.ToString(), GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(diagnostic.FilePath) && GUILayout.Button("定位", GUILayout.Width(48f)))
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
