// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Ember.Basic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>统一项目中心的 embedded 模板开发页。</summary>
    internal sealed class EmberTemplateDevelopmentPanel
    {
        #region 内部参数

        private const string TAG = LogTags.CoreEditor;
        private const string SIDEBAR_WIDTH_KEY = "Ember.TemplateDevelopment.SidebarWidth";
        private const float DEFAULT_SIDEBAR_WIDTH = 300f;
        private const float MIN_SIDEBAR_WIDTH = 220f;
        private const float MIN_DETAIL_WIDTH = 420f;
        private const float SPLITTER_WIDTH = 8f;

        private static readonly string[] ChannelNames =
        {
            "stable（稳定）",
            "preview（预览版）",
            "deprecated（弃用）"
        };

        private static readonly string[] ChannelValues =
        {
            "stable",
            "preview",
            "deprecated"
        };

        private static readonly string[] CreateTypeNames =
        {
            "派生模板",
            "独立模板"
        };

        private static readonly string[] BumpNames =
        {
            "保持版本（内容不变时）",
            "主版本 +1",
            "次版本 +1",
            "补丁 +1"
        };

        private readonly EmberSetupWindowContext _context;
        private readonly Dictionary<string, TemplateConflictChoice> _syncChoices =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, TemplateSyncStatus> _cachedStatuses =
            new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _cachedStatusErrors =
            new(StringComparer.Ordinal);

        private List<TemplateInfo> _cachedTemplates = new();
        private List<TemplateGraphIssue> _cachedGraphIssues = new();
        private EditingTemplateRecord _cachedEditing;
        private string _cachedFrameworkVersion = "0.0.0";
        private bool _snapshotDirty = true;
        private string _snapshotError;

        private string _selectedTemplateId = "base";
        private string _metaForId = string.Empty;
        private string _metaName = string.Empty;
        private string _metaDescription = string.Empty;
        private string _versionField = string.Empty;
        private int _channelIndex;

        private string _newTemplateId = string.Empty;
        private string _newDisplayName = string.Empty;
        private string _newDescription = string.Empty;
        private int _createTypeIndex;
        private string _newParentId = "base";

        private TemplateSyncPlan _syncPlan;
        private string _syncPlanTemplateId;
        private int _syncBumpIndex;
        private string _lastResult;
        private readonly EmberTemplatePreviewPanel _previewPanel;
        private EmberProjectValidationReport _comparison;
        private int _comparisonRevision;
        private int _snapshotRevision;
        private Vector2 _treeScroll;
        private Vector2 _detailScroll;
        private float _sidebarWidth;
        private float _splitterDragStartX;
        private float _splitterDragStartWidth;
        private string _search = string.Empty;
        private int _detailTab;
        private bool _showCreate;
        private bool _showAdvanced;
        private bool _conflictsOnly = true;
        private string _differenceSearch = string.Empty;
        private string _differencePath;
        private string _templateText;
        private string _projectText;
        private int _differencePage;
        private static readonly string[] DetailTabs = { "概览与效果", "内容差异", "父级更新", "版本与设置" };

        private static GUIStyle _warningStyle;
        private static GUIStyle _okStyle;
        private static GUIStyle _templateButtonStyle;

        private static GUIStyle TemplateButtonStyle => _templateButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            fixedHeight = 0f
        };

        private static GUIStyle WarningStyle
        {
            get
            {
                if (_warningStyle == null)
                    _warningStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(1f, 0.6f, 0.2f) }
                    };
                return _warningStyle;
            }
        }

        private static GUIStyle OkStyle
        {
            get
            {
                if (_okStyle == null)
                    _okStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = new Color(0.4f, 0.75f, 0.4f) }
                    };
                return _okStyle;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberTemplateDevelopmentPanel(EmberSetupWindowContext context)
        {
            _context = context;
            _previewPanel = new EmberTemplatePreviewPanel(context);
            _selectedTemplateId = SessionState.GetString("Ember.TemplateDevelopment.Selection", string.Empty);
            _sidebarWidth = SessionState.GetFloat(SIDEBAR_WIDTH_KEY, DEFAULT_SIDEBAR_WIDTH);
        }

        internal void Dispose() { _previewPanel.Dispose(); }

        internal void Draw(float availableWidth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "加载到项目  →  编辑与验证  →  保存到模板  →  封存版本",
                EditorStyles.boldLabel);
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("刷新状态", GUILayout.Width(90)))
            {
                _context.Invalidate();
                InvalidateSnapshot();
                _metaForId = string.Empty;
                _previewPanel.Refresh();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorGUILayout.HelpBox(
                    "Unity 正在编译或刷新资源，模板扫描已暂停。完成后请刷新状态。",
                    MessageType.Info);
                return;
            }

            EnsureSnapshot();
            if (!string.IsNullOrEmpty(_snapshotError))
                EditorGUILayout.HelpBox(_snapshotError, MessageType.Error);

            var templates = _cachedTemplates;
            if (_snapshotRevision != _context.Revision)
                EditorGUILayout.HelpBox("项目状态可能已变化。请刷新状态；执行保存或同步时会再次核对实际内容。", MessageType.Warning);
            float sidebarWidth = ClampSidebarWidth(_sidebarWidth, availableWidth);
            // 为侧栏边距、卡片边距和常驻的垂直滚动条预留空间，避免长文本撑出横向滚动。
            float treeWidth = sidebarWidth - 40f;
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(sidebarWidth)))
                {
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(0));
                    if (GUILayout.Button(_showCreate ? "返回模板详情" : "+ 新建 / 派生 / 另存为")) _showCreate = !_showCreate;
                    _treeScroll = EditorGUILayout.BeginScrollView(
                        _treeScroll, false, true, GUILayout.MinWidth(0));
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(treeWidth)))
                        DrawTemplateTree(templates, treeWidth);
                    EditorGUILayout.EndScrollView();
                }
                DrawSidebarSplitter(sidebarWidth, availableWidth);
                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(0), GUILayout.ExpandWidth(true)))
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    DrawGraphIssues();
                    if (_showCreate) DrawCreateTemplate(templates);
                    else DrawSelectedTemplate(templates);
                    EditorGUILayout.EndScrollView();
                }
            }
            if (!_showCreate)
            {
                var selected = templates.Find(item => item.id == _selectedTemplateId);
                if (selected != null) DrawSaveAndDeleteSection(selected);
            }

            if (_context.IsBusy)
                EditorGUILayout.LabelField("⏳ 正在执行...", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(_lastResult))
                EditorGUILayout.HelpBox(_lastResult, MessageType.Info);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private float ClampSidebarWidth(float width, float availableWidth)
        {
            float maximum = Mathf.Max(MIN_SIDEBAR_WIDTH,
                availableWidth - MIN_DETAIL_WIDTH - SPLITTER_WIDTH - 24f);
            return Mathf.Clamp(width, MIN_SIDEBAR_WIDTH, maximum);
        }

        private void DrawSidebarSplitter(float sidebarWidth, float availableWidth)
        {
            var rect = GUILayoutUtility.GetRect(
                SPLITTER_WIDTH, SPLITTER_WIDTH, 0f, float.MaxValue, GUILayout.ExpandHeight(true));
            int controlId = GUIUtility.GetControlID("Ember.TemplateDevelopment.Splitter".GetHashCode(), FocusType.Passive);
            var current = Event.current;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal, controlId);
            GUI.Label(rect, new GUIContent(string.Empty, "拖动调整左侧栏宽度；双击恢复默认宽度"), GUIStyle.none);
            if (current.type == EventType.Repaint)
            {
                bool highlighted = rect.Contains(current.mousePosition) || GUIUtility.hotControl == controlId;
                EditorGUI.DrawRect(new Rect(rect.center.x - 1f, rect.y, 2f, rect.height),
                    highlighted ? new Color(0.3f, 0.6f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button != 0 || !rect.Contains(current.mousePosition)) break;
                    if (current.clickCount == 2)
                    {
                        _sidebarWidth = DEFAULT_SIDEBAR_WIDTH;
                        SessionState.SetFloat(SIDEBAR_WIDTH_KEY, _sidebarWidth);
                    }
                    else
                    {
                        GUIUtility.hotControl = controlId;
                        _splitterDragStartX = current.mousePosition.x;
                        _splitterDragStartWidth = sidebarWidth;
                    }
                    current.Use();
                    _context.Repaint();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId) break;
                    _sidebarWidth = ClampSidebarWidth(
                        _splitterDragStartWidth + current.mousePosition.x - _splitterDragStartX, availableWidth);
                    current.Use();
                    _context.Repaint();
                    break;
                case EventType.MouseUp:
                    if (current.button != 0 || GUIUtility.hotControl != controlId) break;
                    GUIUtility.hotControl = 0;
                    SessionState.SetFloat(SIDEBAR_WIDTH_KEY, _sidebarWidth);
                    current.Use();
                    _context.Repaint();
                    break;
                case EventType.Ignore:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        SessionState.SetFloat(SIDEBAR_WIDTH_KEY, _sidebarWidth);
                    }
                    break;
            }
        }

        private void DrawGraphIssues()
        {
            foreach (var issue in _cachedGraphIssues)
            {
                EditorGUILayout.HelpBox(
                    $"[{issue.TemplateId}] {issue.Message}",
                    MessageType.Error);
            }
        }

        private void DrawTemplateTree(List<TemplateInfo> templates, float treeWidth)
        {
            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox("包内没有模板。", MessageType.Warning);
                return;
            }

            var rendered = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(_search))
            {
                foreach (var match in templates.Where(item => (item.displayName + " " + item.id)
                    .IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0))
                    DrawTemplateNode(match, templates, rendered, treeWidth, 0, false);
                if (rendered.Count == 0) EditorGUILayout.LabelField("没有匹配的模板。", EditorStyles.miniLabel);
                return;
            }
            foreach (var root in templates.Where(template => string.IsNullOrEmpty(template.parentId)))
                DrawTemplateNode(root, templates, rendered, treeWidth, 0);
            foreach (var remaining in templates.Where(template => !rendered.Contains(template.id)))
                DrawTemplateNode(remaining, templates, rendered, treeWidth, 0);
        }

        private void DrawTemplateNode(
            TemplateInfo template,
            List<TemplateInfo> templates,
            HashSet<string> rendered,
            float treeWidth, int depth, bool drawChildren = true)
        {
            if (!rendered.Add(template.id)) return;

            var status = _cachedStatuses.TryGetValue(template.id, out var stored) ? stored : GetCachedStatus(template.id);
            float contentWidth = treeWidth - EditorStyles.helpBox.padding.horizontal - EditorStyles.helpBox.margin.horizontal;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(treeWidth)))
            {
                string prefix = depth > 0 ? new string(' ', Math.Min(depth, 4) * 2) + "└ " : string.Empty;
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    bool chosen = GUILayout.Toggle(template.id == _selectedTemplateId,
                        new GUIContent(prefix + template.displayName, $"{template.displayName}\n{template.id} · v{template.version}"),
                        TemplateButtonStyle, GUILayout.Width(contentWidth));
                    if (change.changed && chosen) SelectTemplate(template.id);
                }
                GUILayout.Label(new GUIContent(template.id, template.id), EditorStyles.wordWrappedMiniLabel, GUILayout.Width(contentWidth));
                GUILayout.Label($"版本 {template.version}", EditorStyles.wordWrappedMiniLabel, GUILayout.Width(contentWidth));
                GUILayout.Label(GetStatusLabel(status), EditorStyles.wordWrappedMiniLabel, GUILayout.Width(contentWidth));
                if (_cachedEditing?.templateId == template.id)
                    GUILayout.Label("当前项目正在编辑" + (GetCachedStatus(template.id) == TemplateSyncStatus.EditingCopyStale ? "（副本过期）" : ""),
                        EditorStyles.wordWrappedMiniLabel, GUILayout.Width(contentWidth));
            }

            if (!drawChildren) return;
            foreach (var child in templates.Where(item => item.parentId == template.id))
                DrawTemplateNode(child, templates, rendered, treeWidth, depth + 1);
        }

        private void DrawSelectedTemplate(List<TemplateInfo> templates)
        {
            if (templates.Count == 0) return;
            var selected = templates.Find(template => template.id == _selectedTemplateId)
                ?? templates[0];
            _selectedTemplateId = selected.id;
            SyncMetadataFields(selected);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"{selected.displayName} ({selected.id}) · v{selected.version}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"兼容框架 {selected.frameworkVersion} · {ChannelNames[Math.Max(0, Array.IndexOf(ChannelValues, selected.channel))]}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(selected.parentId))
            {
                EditorGUILayout.LabelField(
                    $"父模板：{selected.parentId} v{selected.parentVersion}",
                    EditorStyles.miniLabel);
            }

            DrawEditingCopyState(selected, templates);
            if (_cachedEditing?.templateId == selected.id)
                EditorGUILayout.LabelField(_comparison?.TemplateId == selected.id && _comparisonRevision == _context.Revision
                    ? $"项目磁盘内容：{_comparison.DifferenceCount} 个待保存文件差异"
                    : "项目磁盘内容：尚未检查或结果已过期（可在内容差异页检查）", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(selected.contentHash == selected.versionedContentHash
                ? "模板存储：已封存到当前版本" : "模板存储：已保存，有内容尚未封存版本", EditorStyles.wordWrappedMiniLabel);
            if (_cachedStatuses.TryGetValue(selected.id, out var storedStatus))
                EditorGUILayout.LabelField("父级状态：" + GetStatusLabel(storedStatus), EditorStyles.wordWrappedMiniLabel);
            DrawInheritanceMetadataGate(selected);
            if (selected.schemaVersion == EmberTemplateInheritanceEngine.CurrentSchemaVersion)
            {
                _detailTab = GUILayout.Toolbar(_detailTab, DetailTabs);
                GUILayout.Space(6);
                switch (_detailTab)
                {
                    case 0: _previewPanel.Draw(selected, _cachedEditing); break;
                    case 1: DrawContentDifferences(selected); break;
                    case 2:
                        if (string.IsNullOrEmpty(selected.parentId))
                            EditorGUILayout.HelpBox("这是根/独立模板，不需要同步父模板。可以从左侧创建派生模板。", MessageType.Info);
                        else DrawSyncSection(selected);
                        break;
                    case 3:
                        DrawVersionSection(selected);
                        DrawFrameworkSection(selected);
                        DrawMetadataSection(selected);
                        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "高级操作", true);
                        if (_showAdvanced) DrawAdvancedSettings(selected);
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEditingCopyState(
            TemplateInfo selected,
            List<TemplateInfo> templates)
        {
            var editing = _cachedEditing;
            if (editing == null || string.IsNullOrEmpty(editing.templateId))
            {
                EditorGUILayout.LabelField(
                    "📝 尚未加载模板；保存功能已禁用。",
                    EditorStyles.miniLabel);
                return;
            }

            var editingTemplate = templates.Find(template => template.id == editing.templateId);
            bool same = string.Equals(
                editing.templateId,
                selected.id,
                StringComparison.Ordinal);
            bool stale = same
                && GetCachedStatus(selected.id) == TemplateSyncStatus.EditingCopyStale;
            string label = editingTemplate != null
                ? $"{editingTemplate.displayName} ({editingTemplate.id})"
                : editing.templateId;
            if (!same)
            {
                EditorGUILayout.LabelField(
                    $"⚠ 当前项目编辑副本来自 {label}；不能覆盖选中模板，请加载选中模板或新建分支。",
                    WarningStyle);
            }
            else if (stale)
            {
                EditorGUILayout.HelpBox(
                    $"当前项目编辑副本已落后于模板存储（加载于 {editing.loadedAt}），必须重新加载后才能保存。",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField(
                    $"📝 当前正在编辑：{label} · v{editing.templateVersion}（加载于 {editing.loadedAt}）",
                    OkStyle);
            }
        }

        private void DrawInheritanceMetadataGate(TemplateInfo selected)
        {
            if (selected.schemaVersion == EmberTemplateInheritanceEngine.CurrentSchemaVersion)
                return;

            EditorGUILayout.HelpBox(
                $"模板记录格式需要升级到 v{EmberTemplateInheritanceEngine.CurrentSchemaVersion}，之后才能加载、保存或派生。此操作保留内容和版本号。",
                MessageType.Warning);
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("升级模板记录格式", GUILayout.Width(190))
                && EditorUtility.DisplayDialog(
                    "初始化模板 metadata",
                    $"将为模板 [{selected.id}] 计算内容 hash 并升级 metadata schema。\n不会修改模板 Assets 或版本号。",
                    "初始化",
                    "取消"))
            {
                RunOperation(() =>
                {
                    EmberProjectSetup.InitializeTemplateInheritanceMetadata(selected.id);
                    _metaForId = string.Empty;
                    _lastResult = $"✅ 模板 [{selected.id}] metadata 已初始化。";
                }, "metadata 初始化失败");
            }
            GUI.enabled = true;
        }

        private void DrawSyncSection(TemplateInfo selected)
        {
            if (string.IsNullOrEmpty(selected.parentId)) return;

            GUILayout.Space(5);
            EditorGUILayout.LabelField("父模板同步", EditorStyles.boldLabel);
            var parent = _cachedTemplates.Find(item => item.id == selected.parentId);
            EditorGUILayout.LabelField($"上次同步 {selected.parentVersion} → 父模板当前 {parent?.version ?? "缺失"}", EditorStyles.wordWrappedLabel);
            var status = GetCachedStatus(selected.id);
            EditorGUILayout.LabelField("状态：" + GetStatusLabel(status),
                IsBlockingStatus(status) ? WarningStyle : OkStyle);

            if (status == TemplateSyncStatus.EditingCopyStale)
            {
                // DrawEditingCopyState 已经给出一次明确的恢复指引；这里仅停止同步区，
                // 避免同一个可恢复状态在面板中重复显示成两条错误。
                return;
            }
            if (status != TemplateSyncStatus.ParentChanged)
            {
                if (status != TemplateSyncStatus.Synced)
                {
                    var error = GetCachedStatusError(selected.id);
                    if (!string.IsNullOrEmpty(error))
                        EditorGUILayout.HelpBox(error, MessageType.Warning);
                }
                return;
            }

            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("查看差异并同步", GUILayout.Width(170)))
                PreviewSync(selected.id);
            GUI.enabled = true;
            EditorGUILayout.LabelField(
                "先预览变化并确认冲突，再应用同步。场景无法自动合并时会明确提示。",
                EditorStyles.wordWrappedMiniLabel);

            if (_syncPlan == null || _syncPlanTemplateId != selected.id) return;
            DrawSyncPlan(selected);
        }

        private void DrawSyncPlan(TemplateInfo selected)
        {
            GUILayout.Space(4);
            EditorGUILayout.LabelField(
                $"同步预览：{_syncPlan.Changes.Count} 项 · 冲突 {_syncPlan.Changes.Count(item => item.IsConflict)} 项 · 自动处理 {_syncPlan.Changes.Count(item => !item.IsConflict)} 项",
                EditorStyles.miniBoldLabel);
            _conflictsOnly = EditorGUILayout.ToggleLeft("仅显示需要确认的冲突", _conflictsOnly);
            if (_conflictsOnly && !_syncPlan.Changes.Any(item => item.IsConflict))
                EditorGUILayout.LabelField("没有待确认冲突。取消筛选可查看全部自动处理项。", EditorStyles.wordWrappedMiniLabel);
            if (_syncPlan.Changes.Count == 0)
                EditorGUILayout.LabelField("仅更新父级记录，可保持模板版本。", EditorStyles.miniLabel);

            bool allResolved = true;
            bool willChangeContent = false;
            foreach (var change in _syncPlan.Changes)
            {
                if (_conflictsOnly && !change.IsConflict)
                {
                    if (WillSyncChoiceChangeContent(change, change.RecommendedChoice)) willChangeContent = true;
                    continue;
                }
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"{change.UnitPath} · {ChangeLabel(change.Kind)} · {change.Files.Count} 文件",
                    EditorStyles.wordWrappedMiniLabel,
                    GUILayout.MinWidth(180));

                TemplateConflictChoice choice;
                if (change.IsConflict)
                {
                    _syncChoices.TryGetValue(change.UnitPath, out choice);
                    int choiceIndex = ChoiceToIndex(choice);
                    choiceIndex = EditorGUILayout.Popup(
                        choiceIndex,
                        new[] { "请选择", "保留本模板", "采用父模板" },
                        GUILayout.Width(110));
                    choice = IndexToChoice(choiceIndex);
                    _syncChoices[change.UnitPath] = choice;
                    if (choice == TemplateConflictChoice.Unresolved)
                        allResolved = false;
                }
                else
                {
                    choice = change.RecommendedChoice;
                    EditorGUILayout.LabelField(
                        change.ApplyMode == TemplateChangeApplyMode.SemanticMerge
                            ? "自动合并"
                            : choice == TemplateConflictChoice.AcceptParent
                                ? "采用父模板"
                                : "保留本模板",
                        EditorStyles.miniLabel,
                        GUILayout.Width(110));
                }

                if (WillSyncChoiceChangeContent(change, choice))
                    willChangeContent = true;
                EditorGUILayout.EndHorizontal();

                if (change.ApplyMode == TemplateChangeApplyMode.SemanticMerge)
                {
                    EditorGUILayout.LabelField(
                        "场景语义合并 · 自动 · 父/派生修改可合并",
                        OkStyle);
                }
                else if (change.SceneMergePreview?.Status
                    == SceneSemanticMergeStatus.Fallback)
                {
                    EditorGUILayout.LabelField(
                        "场景语义冲突 · 已回退整场景选择",
                        WarningStyle);
                    EditorGUILayout.LabelField(
                        "保留本模板会保留整个当前场景；采用父模板会替换为整个最新父场景。",
                        EditorStyles.wordWrappedMiniLabel);
                    if (!string.IsNullOrEmpty(change.SceneMergePreview.FailureReason))
                    {
                        EditorGUILayout.LabelField(
                            "回退原因：" + change.SceneMergePreview.FailureReason,
                            EditorStyles.wordWrappedMiniLabel);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            _syncBumpIndex = EditorGUILayout.Popup(
                "同步后的派生版本",
                _syncBumpIndex,
                new[] { BumpNames[0], "封存为 " + NextVersion(selected, 0), "封存为 " + NextVersion(selected, 1), "封存为 " + NextVersion(selected, 2) });
            if (willChangeContent && _syncBumpIndex == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前选择会改变模板内容，请选择同步后封存的版本。",
                    MessageType.Warning);
            }

            GUI.enabled = !_context.OperationsBlocked
                && allResolved
                && (!willChangeContent || _syncBumpIndex > 0);
            if (GUILayout.Button("确认并应用同步", GUILayout.Width(160)))
                ApplySync(selected, willChangeContent);
            GUI.enabled = true;
        }

        private void DrawVersionSection(TemplateInfo selected)
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("封存模板版本", GUILayout.Width(95));
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("补丁 " + NextVersion(selected, 2))) BumpVersion(selected, 2);
            if (GUILayout.Button("次版 " + NextVersion(selected, 1))) BumpVersion(selected, 1);
            if (GUILayout.Button("主版 " + NextVersion(selected, 0))) BumpVersion(selected, 0);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            bool dirty = !string.Equals(
                selected.contentHash,
                selected.versionedContentHash,
                StringComparison.Ordinal);
            EditorGUILayout.LabelField(
                dirty ? "⚠ 当前内容尚未封存到版本" : "✅ 当前内容已封存",
                dirty ? WarningStyle : OkStyle);
            EditorGUILayout.LabelField("补丁用于修复，次版本用于结构变化，主版本用于破坏性改动。封存只包含已保存到模板的内容。", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawFrameworkSection(TemplateInfo selected)
        {
            string currentFramework = _cachedFrameworkVersion;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("框架版本", GUILayout.Width(70));
            EditorGUILayout.LabelField(
                $"v{selected.frameworkVersion}（当前 v{currentFramework}）",
                EditorStyles.miniLabel);
            if (string.IsNullOrEmpty(selected.parentId))
            {
                GUI.enabled = !_context.OperationsBlocked;
                if (GUILayout.Button("声明当前框架", GUILayout.Width(120)))
                    DeclareFrameworkVersion(selected);
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.LabelField("由父同步继承", EditorStyles.miniLabel, GUILayout.Width(90));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMetadataSection(TemplateInfo selected)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("频道", GUILayout.Width(70));
            _channelIndex = EditorGUILayout.Popup(_channelIndex, ChannelNames);
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("应用频道", GUILayout.Width(90)))
                ApplyChannel(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _metaName = EditorGUILayout.TextField("显示名称", _metaName);
            EditorGUILayout.LabelField("描述");
            _metaDescription = EditorGUILayout.TextArea(_metaDescription, GUILayout.MinHeight(55));
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("应用名称与描述", GUILayout.Width(150)))
                ApplyMetadata(selected);
            GUI.enabled = true;
        }

        private void DrawSaveAndDeleteSection(TemplateInfo selected)
        {
            var editing = _cachedEditing;
            bool canSave = editing != null
                && string.Equals(editing.templateId, selected.id, StringComparison.Ordinal)
                && selected.schemaVersion == EmberTemplateInheritanceEngine.CurrentSchemaVersion
                && GetCachedStatus(selected.id) != TemplateSyncStatus.EditingCopyStale;

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_context.OperationsBlocked;
            if (GUILayout.Button("加载到项目", GUILayout.Width(110))) LoadTemplateIntoProject(selected);
            if (GUILayout.Button("检查项目", GUILayout.Width(100))) EmberSetupWindow.ShowProjectValidation(true);
            GUI.enabled = !_context.OperationsBlocked && canSave;
            if (GUILayout.Button("预览并保存到模板", GUILayout.Width(170)))
                SaveProjectAsTemplate(selected);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(canSave ? $"保存目标：{selected.displayName} · 保存后版本仍为 {selected.version}"
                : "保存不可用：请先加载选中模板，并处理编辑副本过期状态。", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawContentDifferences(TemplateInfo selected)
        {
            EditorGUILayout.HelpBox("比较当前项目磁盘内容与模板存储，采用与保存相同的目录范围和开发场景对象剥离规则。新增、修改与移除都是待保存差异，不等同于错误。", MessageType.Info);
            using (new EditorGUI.DisabledScope(_context.OperationsBlocked || _cachedEditing?.templateId != selected.id))
                if (GUILayout.Button("重新计算保存差异"))
                {
                    try
                    {
                        _comparison = EmberProjectValidationService.CompareEditingTemplate(selected.id);
                        _comparisonRevision = _context.Revision;
                        _differencePath = null;
                        _differencePage = 0;
                    }
                    catch (Exception ex) { _comparison = null; _lastResult = "计算差异失败：" + ex.Message; }
                }
            if (EmberProjectValidationService.HasUnsavedScenes())
                EditorGUILayout.HelpBox("存在未保存场景，当前差异未包含这些内存修改。执行保存时会先提示保存场景。", MessageType.Warning);
            if (_comparison == null || _comparison.TemplateId != selected.id) return;
            EditorGUILayout.LabelField($"{_comparison.CheckedAt:HH:mm:ss} · {_comparison.DifferenceCount} 个文件差异", EditorStyles.boldLabel);
            if (_comparisonRevision != _context.Revision)
                EditorGUILayout.HelpBox("此差异预览可能已过期，请重新计算。", MessageType.Warning);
            _differenceSearch = EditorGUILayout.TextField("筛选路径", _differenceSearch);
            var visible = _comparison.Issues.Where(item => item.Severity == EmberValidationSeverity.Difference
                && (string.IsNullOrEmpty(_differenceSearch) || item.AssetPath.IndexOf(_differenceSearch, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            int pages = Math.Max(1, (visible.Count + 49) / 50);
            _differencePage = Mathf.Clamp(_differencePage, 0, pages - 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("上一页", GUILayout.Width(65))) _differencePage = Math.Max(0, _differencePage - 1);
                EditorGUILayout.LabelField($"{_differencePage + 1} / {pages} 页 · {visible.Count} 项");
                if (GUILayout.Button("下一页", GUILayout.Width(65))) _differencePage = Math.Min(pages - 1, _differencePage + 1);
            }
            foreach (var issue in visible.Skip(_differencePage * 50).Take(50))
            {
                EmberProjectValidationPanel.DrawIssue(issue);
                if (GUILayout.Button("查看文件内容：" + Path.GetFileName(issue.AssetPath)))
                {
                    try
                    {
                        _differencePath = issue.AssetPath;
                        string relative = issue.AssetPath.Substring("Assets/".Length);
                        _templateText = ReadDifferenceText(Path.Combine(EmberProjectSetup.GetTemplateAssetsPath(selected.id), relative), relative, false);
                        _projectText = ReadDifferenceText(Path.Combine(Application.dataPath, relative), relative, true);
                    }
                    catch (Exception ex) { _lastResult = "读取文件失败：" + ex.Message; }
                }
                if (_differencePath != issue.AssetPath) continue;
                EditorGUILayout.LabelField("文本预览（最多显示前 16000 字符；资源请使用定位按钮查看）", EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("模板存储");
                        EditorGUILayout.TextArea(_templateText, GUILayout.Height(180), GUILayout.MinWidth(120));
                    }
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("项目保存结果");
                        EditorGUILayout.TextArea(_projectText, GUILayout.Height(180), GUILayout.MinWidth(120));
                    }
                }
            }
            if (_comparison.DifferenceCount == 0) EditorGUILayout.HelpBox("当前磁盘内容与模板一致。", MessageType.Info);
        }

        private void DrawAdvancedSettings(TemplateInfo selected)
        {
            EditorGUILayout.HelpBox("手动设置版本和删除模板属于维护操作。存在派生模板时不能删除父模板。", MessageType.Warning);
            using (new EditorGUILayout.HorizontalScope())
            {
                _versionField = EditorGUILayout.TextField("手动版本", _versionField);
                using (new EditorGUI.DisabledScope(_context.OperationsBlocked || !IsValidVersion(_versionField)))
                    if (GUILayout.Button("应用", GUILayout.Width(55))) ApplyVersion(selected);
            }
            EditorGUILayout.SelectableLabel("内容 hash：" + selected.contentHash, EditorStyles.miniLabel, GUILayout.Height(18));
            EditorGUILayout.SelectableLabel("封存 hash：" + selected.versionedContentHash, EditorStyles.miniLabel, GUILayout.Height(18));
            using (new EditorGUI.DisabledScope(_context.OperationsBlocked || _cachedTemplates.Any(item => item.parentId == selected.id)))
                if (GUILayout.Button("删除此模板")) DeleteSelectedTemplate(selected);
        }

        private static string ReadDifferenceText(string path, string relative, bool project)
        {
            if (!File.Exists(path)) return "（文件不存在）";
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (!new[] { ".cs", ".meta", ".json", ".unity", ".prefab", ".asset", ".asmdef", ".md", ".inputactions" }.Contains(extension))
                return $"资源文件 · {new FileInfo(path).Length} 字节";
            if (new FileInfo(path).Length > 2 * 1024 * 1024)
                return "文件大于 2 MB，请定位资源后在对应编辑器查看。";
            var bytes = project ? EmberProjectSetup.ReadProjectTemplateBytes(path, relative) : File.ReadAllBytes(path);
            if (bytes.Take(256).Contains((byte)0)) return "二进制资源，请定位后在对应编辑器查看。";
            string text = System.Text.Encoding.UTF8.GetString(bytes);
            return text.Length > 16000 ? text.Substring(0, 16000) + "\n…（已截断）" : text;
        }

        private static string NextVersion(TemplateInfo selected, int field)
        {
            if (!Version.TryParse(selected.version, out var version)) return "版本无效";
            return field == 0 ? $"{version.Major + 1}.0.0" : field == 1
                ? $"{version.Major}.{version.Minor + 1}.0" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build) + 1}";
        }

        private static string ChangeLabel(TemplateChangeKind kind)
        {
            switch (kind)
            {
                case TemplateChangeKind.AdoptParentAddition: return "父级新增";
                case TemplateChangeKind.AdoptParentModification: return "父级修改";
                case TemplateChangeKind.AdoptParentDeletion: return "父级删除";
                case TemplateChangeKind.KeepChildAddition: return "本模板新增";
                case TemplateChangeKind.KeepChildModification: return "本模板修改";
                case TemplateChangeKind.KeepChildDeletion: return "本模板删除";
                case TemplateChangeKind.AlreadyMatchesParent: return "双方已一致";
                case TemplateChangeKind.ConflictConcurrentAddition: return "双方新增了不同内容";
                case TemplateChangeKind.ConflictConcurrentModification: return "双方修改了同一文件";
                case TemplateChangeKind.ConflictParentDeletionChildModification: return "父级删除，本模板修改";
                case TemplateChangeKind.ConflictParentModificationChildDeletion: return "父级修改，本模板删除";
                case TemplateChangeKind.ConflictGuidMismatch: return "资源 GUID 冲突";
                case TemplateChangeKind.ConflictPathTypeChanged: return "文件/目录类型冲突";
                case TemplateChangeKind.AutoMergeScene: return "场景可自动合并";
                default: return kind.ToString();
            }
        }

        private void DrawCreateTemplate(List<TemplateInfo> templates)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("创建模板", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("派生模板继承一个已封存的父模板；独立模板从空目录开始。另存为会把当前项目修改保存为新派生模板。", MessageType.Info);
            _createTypeIndex = EditorGUILayout.Popup(
                "类型",
                _createTypeIndex,
                CreateTypeNames);
            bool derived = _createTypeIndex == 0;
            if (derived)
            {
                if (templates.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有可选父模板。", MessageType.Warning);
                }
                else
                {
                    int parentIndex = templates.FindIndex(template => template.id == _newParentId);
                    if (parentIndex < 0)
                    {
                        parentIndex = templates.FindIndex(template => template.id == "base");
                        if (parentIndex < 0) parentIndex = 0;
                    }
                    var parentOptions = templates
                        .Select(template => $"{template.displayName} ({template.id}) v{template.version}")
                        .ToArray();
                    parentIndex = EditorGUILayout.Popup("父模板", parentIndex, parentOptions);
                    _newParentId = templates[parentIndex].id;

                    var parent = templates[parentIndex];
                    var parentStatus = GetCachedStatus(parent.id);
                    bool parentReady = parent.schemaVersion == EmberTemplateInheritanceEngine.CurrentSchemaVersion
                        && string.Equals(
                            parent.contentHash,
                            parent.versionedContentHash,
                            StringComparison.Ordinal)
                        && (parentStatus == TemplateSyncStatus.Root
                            || parentStatus == TemplateSyncStatus.Synced);
                    if (!parentReady)
                    {
                        EditorGUILayout.HelpBox(
                            "所选父模板尚未同步、未初始化或有未版本化内容，当前不能创建派生模板。",
                            MessageType.Warning);
                    }
                }
            }

            _newTemplateId = EditorGUILayout.TextField("模板 id", _newTemplateId);
            _newDisplayName = EditorGUILayout.TextField("显示名称", _newDisplayName);
            _newDescription = EditorGUILayout.TextField("描述", _newDescription);

            bool idValid = IsValidTemplateId(_newTemplateId)
                && !templates.Any(template => string.Equals(
                    template.id,
                    _newTemplateId,
                    StringComparison.OrdinalIgnoreCase));
            bool canCreate = !_context.OperationsBlocked
                && idValid
                && (!derived || IsParentReady(templates, _newParentId));
            GUI.enabled = canCreate;
            if (GUILayout.Button("创建并加载到项目编辑", GUILayout.Width(200)))
                CreateNewTemplate(derived);
            GUI.enabled = true;

            var editing = _cachedEditing;
            bool canSaveAs = canCreate
                && derived
                && editing != null
                && string.Equals(editing.templateId, _newParentId, StringComparison.Ordinal)
                && GetCachedStatus(editing.templateId) != TemplateSyncStatus.EditingCopyStale;
            GUI.enabled = canSaveAs;
            if (GUILayout.Button("将当前编辑副本另存为新模板", GUILayout.Width(230)))
                SaveEditingCopyAsNewTemplate();
            GUI.enabled = true;
            if (derived && editing != null && editing.templateId != _newParentId)
            {
                EditorGUILayout.LabelField(
                    "另存为要求所选父模板就是当前编辑模板；普通创建不受此限制。",
                    EditorStyles.miniLabel);
            }
            if (!string.IsNullOrEmpty(_newTemplateId) && !idValid)
            {
                EditorGUILayout.LabelField(
                    "id 需唯一，限 40 字符，并使用小写字母、数字、点、下划线或连字符。",
                    WarningStyle);
            }
            EditorGUILayout.EndVertical();
        }

        private void SelectTemplate(string templateId)
        {
            _showCreate = false;
            if (_selectedTemplateId == templateId) return;
            _selectedTemplateId = templateId;
            _metaForId = string.Empty;
            ClearSyncPlan();
            _comparison = null;
            _detailScroll = Vector2.zero;
            SessionState.SetString("Ember.TemplateDevelopment.Selection", templateId);
        }

        private void SyncMetadataFields(TemplateInfo selected)
        {
            if (_metaForId == selected.id) return;
            _metaForId = selected.id;
            _metaName = selected.displayName;
            _metaDescription = selected.description;
            _versionField = selected.version;
            _channelIndex = Array.IndexOf(ChannelValues, selected.channel ?? "stable");
            if (_channelIndex < 0) _channelIndex = 0;
        }

        private void PreviewSync(string templateId)
        {
            try
            {
                _syncPlan = EmberProjectSetup.ComputeParentSyncPreviewPlan(templateId);
                _syncPlanTemplateId = templateId;
                _syncChoices.Clear();
                _syncBumpIndex = 0;
                foreach (var change in _syncPlan.Changes)
                {
                    if (change.IsConflict)
                        _syncChoices[change.UnitPath] = TemplateConflictChoice.Unresolved;
                }
            }
            catch (Exception ex)
            {
                ClearSyncPlan();
                _lastResult = "❌ 计算同步计划失败：" + ex.Message;
                EmberDebug.LogError(TAG, "计算模板同步计划失败：" + ex);
            }
        }

        private void ApplySync(TemplateInfo selected, bool willChangeContent)
        {
            if (_context.OperationsBlocked) return;
            if (EmberProjectSetup.GetEditingTemplate()?.templateId == selected.id)
            {
                try
                {
                    _comparison = EmberProjectValidationService.CompareEditingTemplate(selected.id);
                    _comparisonRevision = _context.Revision;
                    if (_comparison.DifferenceCount > 0 || EmberProjectValidationService.HasUnsavedProjectContent())
                    {
                        _detailTab = 1;
                        _lastResult = "请先保存项目修改到模板，再重新预览父级更新。同步完成后会重新加载此模板，当前改动需要先保留。";
                        return;
                    }
                }
                catch (Exception ex) { _lastResult = "同步前检查失败：" + ex.Message; return; }
            }
            string bumpDescription = _syncBumpIndex == 0
                ? "保持版本（仅更新父级记录）"
                : "封存为 " + NextVersion(selected, _syncBumpIndex - 1);
            if (!EditorUtility.DisplayDialog(
                    "应用父模板同步",
                    $"将更新模板 [{selected.id}] 的内容和父级同步记录。\n版本处理：{bumpDescription}\n若当前项目正在编辑此模板，同步后会重新加载。\n\n继续？",
                    "应用同步",
                    "取消"))
                return;

            RunOperation(() =>
            {
                var editingBeforeSync = EmberProjectSetup.GetEditingTemplate();
                bool autoReloadEditingCopy = ShouldAutoReloadEditingCopyAfterSync(
                    editingBeforeSync,
                    selected.id);
                int? bumpField = _syncBumpIndex == 0
                    ? null
                    : _syncBumpIndex - 1;
                var updated = EmberProjectSetup.ApplyParentSync(
                    _syncPlan,
                    _syncChoices,
                    bumpField);
                ClearSyncPlan();
                _metaForId = string.Empty;

                if (autoReloadEditingCopy)
                {
                    try
                    {
                        int count = EmberProjectSetup.LoadTemplate(updated.id);
                        _lastResult = willChangeContent
                            ? $"✅ 父模板同步完成；派生模板已更新为 v{updated.version}，项目编辑副本已自动刷新（{count} 文件）。"
                            : $"✅ 父指针同步完成；模板版本保持 v{updated.version}，项目编辑副本已自动刷新（{count} 文件）。";
                    }
                    catch (Exception ex)
                    {
                        _lastResult =
                            $"⚠ 父模板同步已完成，派生模板当前为 v{updated.version}；但项目编辑副本自动刷新失败：{ex.Message}。请手动加载该模板。";
                        EmberDebug.LogError(
                            TAG,
                            $"父模板同步已提交，但自动刷新项目编辑副本失败：{ex}");
                    }
                    return;
                }

                _lastResult = willChangeContent
                    ? $"✅ 父模板同步完成；派生模板已更新为 v{updated.version}。"
                    : $"✅ 父指针同步完成；模板版本保持 v{updated.version}。";
            }, "父模板同步失败");
        }

        internal static bool ShouldAutoReloadEditingCopyAfterSync(
            EditingTemplateRecord editing,
            string templateId)
        {
            return editing != null
                && !string.IsNullOrEmpty(templateId)
                && string.Equals(editing.templateId, templateId, StringComparison.Ordinal);
        }

        internal static bool WillSyncChoiceChangeContent(
            TemplateChange change,
            TemplateConflictChoice choice)
        {
            if (change == null) return false;
            return change.IsConflict
                ? choice == TemplateConflictChoice.AcceptParent
                : change.WillChangeChildContent;
        }

        private void LoadTemplateIntoProject(TemplateInfo template)
        {
            if (_context.OperationsBlocked) return;
            if (!EditorUtility.DisplayDialog(
                    "加载模板",
                    $"将用模板 [{template.displayName}] 完整替换项目业务层目录。\n\n这些目录中未提交的改动会丢失。继续？",
                    "加载并替换",
                    "取消"))
                return;

            RunOperation(() =>
            {
                int count = EmberProjectSetup.LoadTemplate(template.id);
                SelectTemplate(template.id);
                _lastResult = $"✅ 模板 [{template.displayName}] 已加载（{count} 文件）。";
            }, "加载模板失败");
        }

        private void SaveProjectAsTemplate(TemplateInfo selected)
        {
            if (_context.OperationsBlocked) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.SaveAssets();
            try
            {
                _comparison = EmberProjectValidationService.CompareEditingTemplate(selected.id);
                _comparisonRevision = _context.Revision;
                _detailTab = 1;
            }
            catch (Exception ex) { _lastResult = "无法预览保存：" + ex.Message; return; }
            if (!EditorUtility.DisplayDialog(
                    "保存模板",
                    $"将把当前项目业务层保存为模板 [{selected.displayName}]。\n{_comparison.DifferenceCount} 个文件差异（包含 .meta），详情见内容差异页。\n模板版本保持 v{selected.version}。\n\n可取消先检查差异，再执行保存。",
                    "保存",
                    "取消"))
                return;

            RunOperation(() =>
            {
                int count = EmberProjectSetup.SaveTemplate(
                    selected.id,
                    _metaName,
                    _metaDescription);
                ClearSyncPlan();
                _metaForId = string.Empty;
                _lastResult = $"✅ 模板 [{selected.id}] 已保存（{count} 文件）；可进入“版本与设置”封存版本。";
                AssetDatabase.Refresh();
            }, "保存模板失败");
        }

        private void CreateNewTemplate(bool derived)
        {
            string parentId = derived ? _newParentId : null;
            string typeDescription = derived
                ? $"派生自 [{parentId}]，并同时物化 Assets 与 ParentSnapshot~"
                : "独立空模板";
            if (!EditorUtility.DisplayDialog(
                    "创建新模板",
                    $"将创建模板 [{_newTemplateId}]（{typeDescription}），随后加载到项目业务层。\n\n当前业务层未提交的改动会丢失。继续？",
                    "创建并加载",
                    "取消"))
                return;

            RunOperation(() =>
            {
                EmberProjectSetup.CreateTemplate(
                    _newTemplateId,
                    _newDisplayName,
                    _newDescription,
                    parentId);
                int count = EmberProjectSetup.LoadTemplate(_newTemplateId);
                SelectTemplate(_newTemplateId);
                _lastResult = $"✅ 模板 [{_newTemplateId}] 已创建并加载（{count} 文件）。";
                _newTemplateId = string.Empty;
                _newDisplayName = string.Empty;
                _newDescription = string.Empty;
                AssetDatabase.Refresh();
            }, "创建模板失败");
        }

        private void SaveEditingCopyAsNewTemplate()
        {
            if (!EditorUtility.DisplayDialog(
                    "另存为新模板",
                    $"将把当前项目编辑副本保存为新的派生模板 [{_newTemplateId}]，父模板为 [{_newParentId}]。\n新模板会保存完整 Assets 并建立 ParentSnapshot~；不会覆盖父模板。\n\n继续？",
                    "另存为",
                    "取消"))
                return;

            RunOperation(() =>
            {
                int count = EmberProjectSetup.SaveEditingCopyAsNewTemplate(
                    _newTemplateId,
                    _newDisplayName,
                    _newDescription);
                SelectTemplate(_newTemplateId);
                _lastResult = $"✅ 当前编辑副本已另存为派生模板 [{_newTemplateId}]（{count} 文件）。";
                _newTemplateId = string.Empty;
                _newDisplayName = string.Empty;
                _newDescription = string.Empty;
                AssetDatabase.Refresh();
            }, "另存为新模板失败");
        }

        private void BumpVersion(TemplateInfo selected, int field)
        {
            if (!EditorUtility.DisplayDialog("封存模板版本", $"将已保存的模板封存为 {NextVersion(selected, field)}。\n项目中尚未保存到模板的修改不会包含在内。", "封存版本", "取消")) return;
            RunOperation(() =>
            {
                EmberProjectSetup.BumpTemplateVersion(selected.id, field);
                var fresh = EmberProjectSetup.GetTemplates().Find(template => template.id == selected.id);
                _versionField = fresh?.version ?? _versionField;
                _lastResult = $"✅ 模板 [{selected.id}] 已更新为 v{_versionField}。";
            }, "版本更新失败");
        }

        private void ApplyVersion(TemplateInfo selected)
        {
            RunOperation(() =>
            {
                EmberProjectSetup.SetTemplateVersion(selected.id, _versionField);
                var fresh = EmberProjectSetup.GetTemplates().Find(template => template.id == selected.id);
                _versionField = fresh?.version ?? _versionField;
                _lastResult = $"✅ 模板 [{selected.id}] 版本已设为 v{_versionField}。";
            }, "版本设置失败");
        }

        private void DeclareFrameworkVersion(TemplateInfo selected)
        {
            RunOperation(() =>
            {
                EmberProjectSetup.DeclareFrameworkVersion(selected.id);
                _lastResult = $"✅ 模板 [{selected.id}] 已声明当前框架版本。";
            }, "框架版本声明失败");
        }

        private void ApplyChannel(TemplateInfo selected)
        {
            RunOperation(() =>
            {
                EmberProjectSetup.SetTemplateChannel(
                    selected.id,
                    ChannelValues[_channelIndex]);
                _lastResult = $"✅ 模板 [{selected.id}] 频道已设为 {ChannelValues[_channelIndex]}。";
            }, "频道更新失败");
        }

        private void ApplyMetadata(TemplateInfo selected)
        {
            RunOperation(() =>
            {
                EmberProjectSetup.UpdateTemplateMetadata(
                    selected.id,
                    _metaName,
                    _metaDescription);
                _metaForId = string.Empty;
                _lastResult = $"✅ 模板 [{selected.id}] 名称与描述已更新。";
            }, "metadata 更新失败");
        }

        private void DeleteSelectedTemplate(TemplateInfo selected)
        {
            if (!EditorUtility.DisplayDialog(
                    "删除模板",
                    $"将删除模板 [{selected.displayName}] 及其全部内容。\n父模板存在派生模板时会被阻止。\n\n此操作不可恢复，继续？",
                    "删除",
                    "取消"))
                return;

            RunOperation(() =>
            {
                EmberProjectSetup.DeleteTemplate(selected.id);
                _selectedTemplateId = string.Empty;
                _metaForId = string.Empty;
                ClearSyncPlan();
                _lastResult = $"✅ 模板 [{selected.id}] 已删除。";
                AssetDatabase.Refresh();
            }, "删除模板失败");
        }

        private void RunOperation(Action action, string failureLabel)
        {
            if (_context.OperationsBlocked) return;
            _context.IsBusy = true;
            _lastResult = null;
            _context.Repaint();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _lastResult = $"❌ {failureLabel}：{ex.Message}";
                EmberDebug.LogError(TAG, failureLabel + "：" + ex);
            }
            finally
            {
                _context.Invalidate();
                InvalidateSnapshot();
                _context.IsBusy = false;
                _context.Repaint();
            }
        }

        private void EnsureSnapshot()
        {
            if (!_snapshotDirty) return;

            _snapshotDirty = false;
            _snapshotError = null;
            _cachedTemplates = new List<TemplateInfo>();
            _cachedGraphIssues = new List<TemplateGraphIssue>();
            _cachedEditing = null;
            _cachedFrameworkVersion = "0.0.0";
            _cachedStatuses.Clear();
            _cachedStatusErrors.Clear();

            try
            {
                _cachedTemplates = EmberProjectSetup.GetTemplates();
                _cachedGraphIssues = EmberTemplateInheritanceEngine.ValidateTemplateGraph(
                    _cachedTemplates);
                _cachedEditing = EmberProjectSetup.GetEditingTemplate();
                _cachedFrameworkVersion = EmberProjectSetup.GetFrameworkVersion();
                _snapshotRevision = _context.Revision;
                if (!_cachedTemplates.Any(item => item.id == _selectedTemplateId))
                    _selectedTemplateId = _cachedEditing?.templateId ?? _cachedTemplates.FirstOrDefault()?.id;

                foreach (var template in _cachedTemplates)
                {
                    try
                    {
                        var plan = EmberProjectSetup.ComputeParentSyncPlan(template.id);
                        var status = plan.Status;
                        _cachedStatuses[template.id] = status;
                        if (!string.IsNullOrEmpty(plan.Error))
                            _cachedStatusErrors[template.id] = plan.Error;
                    }
                    catch (Exception ex)
                    {
                        _cachedStatuses[template.id] = TemplateSyncStatus.InvalidAssetMetadata;
                        _cachedStatusErrors[template.id] = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                _snapshotError = "刷新模板状态失败：" + ex.Message;
                EmberDebug.LogError(TAG, "刷新模板开发快照失败：" + ex);
            }
        }

        private void InvalidateSnapshot()
        {
            _snapshotDirty = true;
        }

        private TemplateSyncStatus GetCachedStatus(string templateId)
        {
            if (_cachedEditing?.templateId == templateId
                && EmberTemplateInheritanceEngine.IsEditingRecordStale(_cachedEditing, _cachedTemplates.Find(item => item.id == templateId)))
                return TemplateSyncStatus.EditingCopyStale;
            return _cachedStatuses.TryGetValue(templateId, out var status)
                ? status
                : TemplateSyncStatus.InvalidAssetMetadata;
        }

        private string GetCachedStatusError(string templateId)
        {
            return _cachedStatusErrors.TryGetValue(templateId, out var error)
                ? error
                : null;
        }

        private bool IsParentReady(
            List<TemplateInfo> templates,
            string parentId)
        {
            var parent = templates.Find(template => template.id == parentId);
            if (parent == null
                || parent.schemaVersion != EmberTemplateInheritanceEngine.CurrentSchemaVersion
                || !string.Equals(
                    parent.contentHash,
                    parent.versionedContentHash,
                    StringComparison.Ordinal))
                return false;
            var status = GetCachedStatus(parent.id);
            return status == TemplateSyncStatus.Root || status == TemplateSyncStatus.Synced;
        }

        private static bool IsValidTemplateId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.Length <= 40
                && id != "."
                && id != ".."
                && Regex.IsMatch(id, @"^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant);
        }

        private static bool IsValidVersion(string version)
        {
            return Regex.IsMatch(version ?? string.Empty, @"^\d+\.\d+\.\d+$");
        }

        private static bool IsBlockingStatus(TemplateSyncStatus status)
        {
            return status != TemplateSyncStatus.Root
                && status != TemplateSyncStatus.Synced;
        }

        private static string GetStatusLabel(TemplateSyncStatus status)
        {
            switch (status)
            {
                case TemplateSyncStatus.Root: return "根模板";
                case TemplateSyncStatus.Synced: return "已同步";
                case TemplateSyncStatus.ParentChanged: return "父模板有更新";
                case TemplateSyncStatus.ParentUnversioned: return "父模板内容尚未封存";
                case TemplateSyncStatus.TemplateContentDirty: return "存储内容未登记";
                case TemplateSyncStatus.EditingCopyStale: return "编辑副本已过期";
                case TemplateSyncStatus.ParentMissing: return "父模板缺失";
                case TemplateSyncStatus.CycleDetected: return "继承循环";
                case TemplateSyncStatus.SnapshotMissingOrCorrupted: return "父基线损坏";
                case TemplateSyncStatus.MetadataNotInitialized: return "模板记录格式待升级";
                case TemplateSyncStatus.InvalidAssetMetadata: return "资源元数据无效";
                default: return status.ToString();
            }
        }

        private static int ChoiceToIndex(TemplateConflictChoice choice)
        {
            switch (choice)
            {
                case TemplateConflictChoice.KeepChild: return 1;
                case TemplateConflictChoice.AcceptParent: return 2;
                default: return 0;
            }
        }

        private static TemplateConflictChoice IndexToChoice(int index)
        {
            switch (index)
            {
                case 1: return TemplateConflictChoice.KeepChild;
                case 2: return TemplateConflictChoice.AcceptParent;
                default: return TemplateConflictChoice.Unresolved;
            }
        }

        private void ClearSyncPlan()
        {
            _syncPlan = null;
            _syncPlanTemplateId = null;
            _syncChoices.Clear();
            _syncBumpIndex = 0;
        }

        #endregion
    }
}
