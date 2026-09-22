using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Table;
using Game.Table.Generated;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Editor
{
    public sealed partial class NarrativeGraphWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeChapterSO _chapter;
        [SerializeField] private NarrativeNodeSO _selected;
        [SerializeField] private bool _follow = true;
        [SerializeField] private Vector2 _pan = new(35, 35);
        [SerializeField] private float _zoom = .85f;
        [SerializeField] private bool _snap = true;
        [SerializeField] private NarrativeStorySO _story;
        [SerializeField] private bool _overview = true;
        [SerializeField] private NarrativeChapterSO _selectedChapter;
        [SerializeField] private int _contentTab;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private INarrativeDiagnostics _source;
        private NarrativeSnapshot _snapshot;
        private EmberTableEngine _tableEngine;
        private NarrativeTableCatalog _catalog;
        private NarrativeFlowGraphView _graph;
        private NarrativeStoryGraphView _storyGraph;
        private ObjectField _storyField;
        private Toolbar _chapterToolbar;
        private SerializedObject _storyContent;
        private SerializedObject _overviewContent;
        private NarrativeChapterSO _existingChapter;
        private bool _showDiagnostics;
        private Vector3 _overviewPan;
        private Vector3 _overviewScale = Vector3.one;
        public NarrativeStorySO Story => _story;
        public bool IsOverview => _overview;
        public NarrativeStoryGraphView StoryGraph => _storyGraph;
        private ObjectField _chapterField;
        private IMGUIContainer _inspector;
        private SerializedObject _content;
        private NarrativeNodeSearch _search;
        private IVisualElementScheduledItem _refresh;
        private Label _zoomLabel;
        private readonly List<VisualElement> _writeControls = new();
        private HashSet<NarrativeNodeSO> _members = new();
        private Vector2 _scroll;
        private NarrativeNodeSO _existing;
        private string _message;
        private IReadOnlyList<NarrativeError> _errors = Array.Empty<NarrativeError>();
        public NarrativeSnapshot ObservedSnapshot => _snapshot;
        public NarrativeChapterSO Chapter => _chapter;
        public NarrativeNodeSO SelectedNode => _selected;
        public NarrativeFlowGraphView Graph => _graph;
        public bool FollowExecution { get => _follow; set => _follow = value; }
        private NarrativeSnapshot ChapterSnapshot => _snapshot != null &&
            (string.IsNullOrEmpty(_snapshot.StoryId) || _story && _story.StoryId == _snapshot.StoryId)
                ? _snapshot : null;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void OnEnable()
        {
            if (!NarrativeEditorAvailability.Visible)
            {
                // 一次性 editor update 在测试运行器/布局恢复期间也会执行；不依赖 Inspector delayCall。
                EditorApplication.update -= CloseIfDisabled;
                EditorApplication.update += CloseIfDisabled;
                return;
            }
            titleContent = new GUIContent("小说流程"); minSize = new Vector2(980, 580);
            NarrativeObservation.SourceChanged += BindSource;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.projectChanged += OnProjectChanged;
            Undo.undoRedoPerformed += OnUndo;
            Undo.postprocessModifications += OnModified;
            LoadTables(); BindSource();
        }
        private void OnDisable()
        {
            EditorApplication.delayCall -= CloseIfDisabled;
            EditorApplication.update -= CloseIfDisabled;
            NarrativeObservation.SourceChanged -= BindSource;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.projectChanged -= OnProjectChanged;
            Undo.undoRedoPerformed -= OnUndo; Undo.postprocessModifications -= OnModified;
            _refresh?.Pause(); _refresh = null;
            Unbind(); _content?.Dispose(); _content = null;
            _storyContent?.Dispose(); _storyContent = null;
            _overviewContent?.Dispose(); _overviewContent = null;
            _tableEngine?.Dispose(); _tableEngine = null; _catalog = null;
            if (_search) DestroyImmediate(_search);
        }
        private void CreateGUI()
        {
            rootVisualElement.Clear(); _writeControls.Clear();
            if (!NarrativeEditorAvailability.Visible) return;
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Game/Module/Narrative/Editor/NarrativeGraph.uss");
            if (sheet) rootVisualElement.styleSheets.Add(sheet);
            if (!_story) _story = Resources.Load<NarrativeStorySO>(new NovelNewGameRequest().StoryPath);
            if (!_story) _story = AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets/GameResource" })
                .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();
            var global = new Toolbar();
            ConfigureToolbar(global, "剧情");
            _storyField = new ObjectField { objectType = typeof(NarrativeStorySO), allowSceneObjects = false, value = _story };
            _storyField.style.width = 210; _storyField.RegisterValueChangedCallback(e => ShowStory(e.newValue as NarrativeStorySO)); global.Add(_storyField);
            Button(global, "章节总览", () => ShowStory(_story));
            Button(global, "保存剧情", () => TryEdit(() => NarrativeStoryModel.Save(_story)));
            Button(global, "校验剧情", ValidateStory);
            var create = new ToolbarMenu { text = "新建 ▾" };
            create.menu.AppendAction("新剧情", _ => CreateStory(), _ => WriteStatus(_story, true));
            create.menu.AppendAction("新章节", _ => CreateChapter(), _ => WriteStatus(_story, true));
            global.Add(create);
            var maintain = new ToolbarMenu { text = "剧情工具 ▾" };
            maintain.menu.AppendAction("同步章节出口", _ => TryEdit(() => NarrativeStoryModel.SyncExits(_story)), _ => WriteStatus(_story));
            maintain.menu.AppendAction("自动排列章节总览", _ => _storyGraph.AutoArrange(), _ => WriteStatus(_story));
            global.Add(maintain);
            Button(global, "显示全部", () => { if (_overview) _storyGraph.FrameAll(); else _graph.FrameAll(); });
            rootVisualElement.Add(global);
            var toolbar = _chapterToolbar = new Toolbar();
            ConfigureToolbar(toolbar, "章节");
            _chapterField = new ObjectField { objectType = typeof(NarrativeChapterSO), allowSceneObjects = false, value = _chapter };
            _chapterField.style.width = 210;
            _chapterField.RegisterValueChangedCallback(e => ShowChapter(e.newValue as NarrativeChapterSO)); toolbar.Add(_chapterField);
            Button(toolbar, "+ 节点", () => OpenSearch(_graph.worldBound.center, _graph.contentViewContainer.WorldToLocal(_graph.worldBound.center)), true);
            Button(toolbar, "保存", () => TryEdit(() => NarrativeGraphModel.Save(_chapter)), true);
            Button(toolbar, "校验", Validate);
            var view = new ToolbarMenu { text = "视图 ▾" };
            view.menu.AppendAction("按连接自动排布", _ => _graph.AutoArrange(), _ => WriteStatus(_chapter));
            view.menu.AppendAction("显示全部节点（A）", _ => _graph.FrameAll());
            view.menu.AppendAction("聚焦所选节点（F）", _ => _graph.FrameSelection());
            view.menu.AppendAction("恢复 100% 缩放", _ => _graph.ZoomTo(1));
            toolbar.Add(new ToolbarSpacer()); toolbar.Add(view);
            var snap = new ToolbarToggle { text = "网格吸附", value = _snap };
            snap.RegisterValueChangedCallback(e => { _snap = e.newValue; _graph.SnapToGrid = _snap; }); toolbar.Add(snap);
            Button(toolbar, "−", () => _graph.ZoomTo(_zoom / 1.2f));
            _zoomLabel = new Label(); _zoomLabel.style.minWidth = 42; toolbar.Add(_zoomLabel);
            Button(toolbar, "+", () => _graph.ZoomTo(_zoom * 1.2f));
            Button(toolbar, "1:1", () => _graph.ZoomTo(1));
            rootVisualElement.Add(toolbar);
            var split = new TwoPaneSplitView(1, 380, TwoPaneSplitViewOrientation.Horizontal); split.style.flexGrow = 1;
            _graph = new NarrativeFlowGraphView(SetInspectedNode, TryEdit, OpenSearch) { SnapToGrid = _snap };
            _graph.viewTransformChanged += view =>
            {
                _pan = view.contentViewContainer.resolvedStyle.translate;
                _zoom = view.contentViewContainer.resolvedStyle.scale.value.x;
                _zoomLabel.text = Mathf.RoundToInt(_zoom * 100) + "%";
            };
            var graphs = new VisualElement(); graphs.style.flexGrow = 1; graphs.style.minWidth = 200;
            graphs.Add(_graph);
            _storyGraph = new NarrativeStoryGraphView(c => { _selectedChapter = c; _inspector?.MarkDirtyRepaint(); }, ShowChapter, TryEdit, CreateChapter);
            _storyGraph.viewTransformChanged += g =>
            {
                _overviewPan = g.contentViewContainer.resolvedStyle.translate;
                _overviewScale = g.contentViewContainer.resolvedStyle.scale.value;
            };
            graphs.Add(_storyGraph); split.Add(graphs);
            var sidebar = new VisualElement(); sidebar.style.minWidth = 280;
            var observe = new Toolbar();
            ConfigureToolbar(observe);
            var follow = new ToolbarToggle { text = "跟随执行", value = _follow };
            follow.RegisterValueChangedCallback(e => { _follow = e.newValue; if (_follow) LocateCurrent(true); }); observe.Add(follow);
            Button(observe, "定位当前", () => LocateCurrent(true)); Button(observe, "章节内容", () => SelectNode(null)); sidebar.Add(observe);
            Button(observe, "预览节点", () => Game.UI.Editor.NovelGameplayLayoutWindow.OpenNodePreview(_selected));
            Button(observe, "播放节点", () =>
            {
                if (_selected is not NarrativeDialogueSO dialogue)
                { _message = "演出试播需要选中一个对话节点。"; _inspector?.MarkDirtyRepaint(); return; }
                Game.UI.Editor.NovelNodePlaybackWindow.Play(dialogue, _chapter, _story, error =>
                { if (this) { LocateError(error); Focus(); } });
            });
            _inspector = new IMGUIContainer(DrawInspector); _inspector.style.flexGrow = 1; sidebar.Add(_inspector);
            split.Add(sidebar); rootVisualElement.Add(split);
            var help = new Label("Space 添加节点 · 滚轮缩放 · 中键平移 · 拖拽端口连线 · 框选/Shift 多选 · A 全部 · F 聚焦 · Ctrl+D 复制 · Delete 移除");
            help.AddToClassList("narrative-help"); rootVisualElement.Add(help);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnShortcut, TrickleDown.TrickleDown);
            RefreshGraph(); _graph.SelectModel(_selected, false); _graph.UpdateViewTransform(_pan, Vector3.one * Mathf.Clamp(_zoom, .1f, 3));
            _storyGraph.UpdateViewTransform(_overviewPan, _overviewScale);
            _graph.schedule.Execute(() => { if (_follow && _snapshot != null && !_overview) LocateCurrent(true); });
        }
        private static void ConfigureToolbar(Toolbar toolbar, string label = null)
        {
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.height = StyleKeyword.Auto;
            toolbar.style.minHeight = 28;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            if (label != null)
            {
                var heading = new Label(label);
                heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                heading.style.alignSelf = Align.Center;
                heading.style.marginRight = 8;
                toolbar.Add(heading);
            }
        }
        private static DropdownMenuAction.Status WriteStatus(UnityEngine.Object asset, bool create = false)
            => NarrativeEditorAvailability.Visible && !EditorApplication.isPlayingOrWillChangePlaymode &&
                (create || NarrativeGraphModel.CanEdit(asset)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        private void Button(VisualElement parent, string text, Action action, bool write = false)
        {
            var button = new ToolbarButton(action) { text = text };
            button.style.minHeight = 26; button.style.marginLeft = 2; button.style.marginRight = 2;
            button.style.paddingLeft = 8; button.style.paddingRight = 8;
            parent.Add(button);
            if (write) _writeControls.Add(button);
        }
        private void OnShortcut(KeyDownEvent e)
        {
            if (!e.actionKey) return;
            if (e.keyCode == KeyCode.S) { TryEdit(() => { if (_overview) NarrativeStoryModel.Save(_story); else NarrativeGraphModel.Save(_chapter); }); e.StopImmediatePropagation(); }
            else if (!(e.target is TextElement) && e.keyCode == KeyCode.Z && !EditorApplication.isPlayingOrWillChangePlaymode)
            { if (e.shiftKey) Undo.PerformRedo(); else Undo.PerformUndo(); e.StopImmediatePropagation(); }
        }
        private void QueueRefresh()
        {
            if (_graph == null) return;
            if (_refresh == null) _refresh = rootVisualElement.schedule.Execute(RefreshGraph);
            _refresh.ExecuteLater(120);
        }
        private void RefreshGraph()
        {
            if (_graph == null) return;
            _members = _chapter ? new HashSet<NarrativeNodeSO>(_chapter.Nodes) : new HashSet<NarrativeNodeSO>();
            if (_selected && !_members.Contains(_selected)) _selected = null;
            _graph.Rebuild(_chapter, _catalog); _graph.ShowExecution(ChapterSnapshot);
            _storyGraph.Rebuild(_story); _storyGraph.ShowExecution(_snapshot); _storyGraph.SelectChapter(_selectedChapter, false);
            _graph.style.display = _overview ? DisplayStyle.None : DisplayStyle.Flex;
            _storyGraph.style.display = _overview ? DisplayStyle.Flex : DisplayStyle.None;
            _chapterToolbar.style.display = _overview ? DisplayStyle.None : DisplayStyle.Flex;
            _storyField.SetValueWithoutNotify(_story);
            _chapterField.SetValueWithoutNotify(_chapter);
            foreach (var control in _writeControls) control.SetEnabled(NarrativeGraphModel.CanEdit(_chapter));
            BindContent(); _inspector.MarkDirtyRepaint();
        }
        private UndoPropertyModification[] OnModified(UndoPropertyModification[] modifications)
        {
            foreach (var modification in modifications)
                if (modification.currentValue.target == _story || modification.currentValue.target == _selectedChapter || modification.currentValue.target == _chapter || modification.currentValue.target is NarrativeNodeSO node && _members.Contains(node))
                { QueueRefresh(); break; }
            return modifications;
        }
        private void OnProjectChanged() { NarrativeGraphModel.InvalidateTemplateCache(); QueueRefresh(); }
        private void OnUndo() { QueueRefresh(); _inspector?.MarkDirtyRepaint(); }
        private void TryEdit(Action action)
        {
            try { action(); if (_story && NarrativeGraphModel.CanEdit(_story)) NarrativeStoryModel.SyncExits(_story); _message = null; }
            catch (Exception ex) { _message = ex.Message; }
            QueueRefresh(); // 失败的交互也从 SO 恢复画布，避免留下未写入的临时连线。
            _inspector?.MarkDirtyRepaint();
        }
        private void LoadTables()
        {
            _tableEngine?.Dispose(); _tableEngine = new EmberTableEngine(); _catalog = null;
            var catalog = GameTables.CreateCatalog(); var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var asset = Resources.Load<TextAsset>(entry.ResourcePath);
                if (!asset) { _message = "缺少导表产物：" + entry.ResourcePath; return; }
                bytes.Add(entry.TableId, asset.bytes);
            }
            if (_tableEngine.Load(catalog, bytes).Succeeded) _catalog = new NarrativeTableCatalog(_tableEngine.Database);
            else _message = "导表产物加载失败，请在配置表中心检查。";
        }
        private void Validate()
        {
            if (!_chapter) return;
            LoadTables();
            _errors = NarrativeAssetValidation.Validate(_story, _chapter, _catalog);
            _message = _errors.Count == 0 ? "章节校验通过（已重新读取导出配表）。" : null; QueueRefresh();
        }
        private void BindContent()
        {
            UnityEngine.Object asset = _selected ? _selected : _chapter;
            if (_content != null && _content.targetObject == asset) return;
            _content?.Dispose(); _content = asset ? new SerializedObject(asset) : null;
        }
        private void SetInspectedNode(NarrativeNodeSO node)
        {
            Game.UI.Editor.NovelGameplayLayoutWindow.FollowNodeSelection(node);
            if (_selected == node && _content != null) return;
            _selected = node; _contentTab = 0; _scroll = Vector2.zero; BindContent(); _inspector?.MarkDirtyRepaint();
        }
        private void DrawInspector()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            if (_snapshot != null)
            {
                _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "运行观察 · " + _snapshot.State + " / " + _snapshot.Wait, true);
                if (_showDiagnostics) Diagnostics();
            }
            else EditorGUILayout.HelpBox(EditorApplication.isPlaying ? "Play Mode · 无已发布的小说会话" : "编辑模式 · 无运行会话", MessageType.None);
            if (_overview) { DrawStoryInspector(); EditorGUILayout.EndScrollView(); return; }
            EditorGUILayout.LabelField(_selected ? "内容 · " + _selected.name : "章节内容", EditorStyles.boldLabel);
            if (!_chapter) EditorGUILayout.HelpBox("选择或新建章节；画布按空格添加节点。", MessageType.Info);
            else
            {
                if (_selected) _contentTab = GUILayout.Toolbar(_contentTab, new[] { "对话内容", "SO 设置" });
                BindContent(); if (_content != null) NarrativeContentGUI.Draw(_content, ChapterSnapshot, _catalog, _selected && _contentTab == 1);
                if (!_selected || _contentTab == 1) NodeActions();
                foreach (var error in _errors)
                {
                    EditorGUILayout.HelpBox(error.ToString(), MessageType.Error);
                    if (GUILayout.Button("定位此错误")) LocateError(error);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private void CreateChapter()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !NarrativeGraphModel.IsTemplateActive(true)) return;
            if (_story)
            {
                TryEdit(() => ShowChapter(NarrativeStoryModel.CreateChapter(_story, "CH" + (_story.Chapters.Count + 1).ToString("D2")))); return;
            }
            string path = EditorUtility.SaveFilePanelInProject("新章节", "Chapter", "asset", "节点保存在章节旁边。", "Assets/GameResource/Resources/Config/Narrative");
            if (string.IsNullOrEmpty(path)) return;
            TryEdit(() => { var chapter = CreateInstance<NarrativeChapterSO>(); AssetDatabase.CreateAsset(chapter, AssetDatabase.GenerateUniqueAssetPath(path)); ShowChapter(chapter); });
        }
        private void OpenSearch(Vector2 panelPoint, Vector2 graphPoint)
        {
            if (!NarrativeGraphModel.CanEdit(_chapter)) return;
            if (!_search) _search = CreateInstance<NarrativeNodeSearch>();
            _search.Configure(kind => CreateNodeAt(kind, graphPoint));
            SearchWindow.Open(new SearchWindowContext(position.position + panelPoint), _search);
        }
        private void Unbind() { if (_source != null) _source.Changed -= OnSnapshot; _source = null; _snapshot = null; _graph?.ShowExecution(null); _storyGraph?.ShowExecution(null); }
        private void BindSource()
        {
            Unbind(); if (EditorApplication.isPlaying) _source = NarrativeObservation.Current;
            if (_source != null) { _source.Changed += OnSnapshot; OnSnapshot(); }
            _inspector?.MarkDirtyRepaint();
        }
        private void OnSnapshot()
        {
            if (_source == null || !ReferenceEquals(_source, NarrativeObservation.Current) || !EditorApplication.isPlaying) return;
            var previous = _snapshot; _snapshot = _source.Snapshot; _graph?.ShowExecution(ChapterSnapshot); _storyGraph?.ShowExecution(_snapshot);
            if (_follow && (previous == null || previous.SessionGeneration != _snapshot.SessionGeneration || previous.ChapterId != _snapshot.ChapterId || previous.NodeId != _snapshot.NodeId))
            {
                if (_overview && _story && _snapshot.StoryId == _story.StoryId)
                    _storyGraph?.SelectChapter(_story.Chapters.FirstOrDefault(c => c && c.ChapterId == _snapshot.ChapterId), true);
                else LocateCurrent(true);
            }
            _inspector?.MarkDirtyRepaint();
        }
        private void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) BindSource();
            else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) Unbind();
            RefreshGraph();
        }
        private void Locate(string nodeId, bool select)
        {
            if (!_chapter) return;
            var node = _chapter.Nodes.FirstOrDefault(n => n && n.NodeId == nodeId);
            if (!node) return;
            if (select) SetInspectedNode(node);
            _graph?.SelectModel(node, true);
        }
        private void NodeActions()
        {
            using (new EditorGUI.DisabledScope(!NarrativeGraphModel.CanEdit(_chapter)))
            {
                if (_selected)
                {
                    if (GUILayout.Button("设为章节入口")) TryEdit(() => NarrativeGraphModel.SetEntry(_chapter, _selected));
                    if (GUILayout.Button("复制节点（新 ID）")) TryEdit(() => SelectNode(NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Dialogue, _selected)));
                    if (GUILayout.Button("从本章移除…"))
                    {
                        var incoming = NarrativeGraphModel.Incoming(_chapter, _selected);
                        if (EditorUtility.DisplayDialog("移除节点", "将断开以下入链，保留原 .asset。可撤销。\n" + string.Join("\n", incoming), "移除并断开", "取消"))
                            TryEdit(() => { NarrativeGraphModel.Remove(_chapter, _selected, true); _selected = null; });
                    }
                }
                _existing = (NarrativeNodeSO)EditorGUILayout.ObjectField("已有节点", _existing, typeof(NarrativeNodeSO), false);
                if (_existing && GUILayout.Button("加入本章（仅同章节）")) TryEdit(() => NarrativeGraphModel.AddExisting(_chapter, _existing));
            }
        }
        private void Diagnostics()
        {
            if (!EditorApplication.isPlaying) { EditorGUILayout.HelpBox("编辑模式 · 无运行会话", MessageType.None); return; }
            if (_snapshot == null) { EditorGUILayout.HelpBox("Play Mode · 无已发布的小说会话", MessageType.Info); return; }
            var s = _snapshot;
            EditorGUILayout.LabelField("运行观察（只读）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("会话 / 位置", s.SessionGeneration + " / " + s.PositionVersion);
            EditorGUILayout.LabelField("状态 / 模式", s.State + " / " + s.ReadMode);
            EditorGUILayout.LabelField("章节", s.ChapterId ?? "—");
            EditorGUILayout.LabelField("节点 / 指令", (s.NodeId ?? "—") + " / " + (s.CommandId ?? "—"));
            EditorGUILayout.LabelField("等待", s.Wait.ToString());
            if (!string.IsNullOrEmpty(s.WaitingActions)) EditorGUILayout.LabelField("等待动作", s.WaitingActions);
            foreach (var action in s.Actions) EditorGUILayout.LabelField(action, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("暂停原因", s.PauseReasons.Count == 0 ? "无" : string.Join(", ", s.PauseReasons));
            foreach (var pair in s.Variables) EditorGUILayout.LabelField(pair.Key, pair.Value.Type + " · " + pair.Value);
            foreach (var pair in s.GlobalVariables) EditorGUILayout.LabelField("全局 / " + pair.Key, pair.Value.Type + " · " + pair.Value);
            if (s.State == NarrativeState.AwaitingChoice)
                EditorGUILayout.LabelField("合法选项", string.Join(", ", s.Options.Select(o => o.Id + ": " + o.Text)));
            if (s.EndingId != null) EditorGUILayout.LabelField("结局", s.EndingId);
            if (s.Error != null)
            { EditorGUILayout.HelpBox(s.Error.ToString(), MessageType.Error); if (GUILayout.Button("定位运行错误")) LocateObservedError(); }
        }

        private NarrativeChapterSO FindObservedChapter()
        {
            if (_snapshot == null) return null;
            IEnumerable<NarrativeChapterSO> chapters;
            if (!string.IsNullOrEmpty(_snapshot.StoryId))
            {
                // 章节 ID 只在所属剧情中解析；浏览另一个剧情不能改变运行定位的范围。
                var story = _story;
                if (!story || story.StoryId != _snapshot.StoryId)
                {
                    var stories = AssetDatabase.FindAssets("t:NarrativeStorySO", new[] { "Assets" })
                        .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(AssetDatabase.GUIDToAssetPath(g)))
                        .Where(s => s && s.StoryId == _snapshot.StoryId).ToArray();
                    if (stories.Length != 1) { _message = "运行剧情无法唯一定位：" + _snapshot.StoryId; return null; }
                    story = stories[0];
                }
                chapters = story.Chapters;
            }
            else
            {
                // 兼容独立章节测试宿主，但不在多个同 ID 资产间猜测。
                chapters = AssetDatabase.FindAssets("t:NarrativeChapterSO", new[] { "Assets" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>(AssetDatabase.GUIDToAssetPath(g)));
            }
            var matches = chapters.Where(c => c && c.ChapterId == _snapshot.ChapterId).ToArray();
            if (matches.Length == 1) return matches[0];
            _message = "运行章节无法唯一定位：" + _snapshot.ChapterId;
            return null;
        }

        private void LocateObservedError()
        {
            if (_snapshot?.Error == null) return;
            var chapter = FindObservedChapter();
            if (!chapter) return;
            if (_chapter != chapter || _overview) ShowChapter(chapter);
            LocateError(_snapshot.Error);
        }

        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void LocateError(NarrativeError error)
        {
            if (error == null) return;
            var matches = (_story ? _story.Chapters : _chapter ? new[] { _chapter } : Array.Empty<NarrativeChapterSO>())
                .Where(c => c && c.ChapterId == error.ChapterId).ToArray();
            if (matches.Length != 1) { _message = "错误章节无法唯一定位：" + error.ChapterId; return; }
            var errors = _errors;
            ShowChapter(matches[0]); _errors = errors;
            var nodes = _chapter.Nodes.Where(n => n && n.NodeId == error.NodeId).ToArray();
            if (nodes.Length != 1) { _message = error.ToString(); return; }
            SelectNode(nodes[0]); _graph?.SelectModel(nodes[0], true);
            var list = _content?.FindProperty("_commands") ?? _content?.FindProperty("_options") ?? _content?.FindProperty("_branches");
            if (list != null)
                for (int i = 0; i < list.arraySize; i++)
                {
                    var item = list.GetArrayElementAtIndex(i);
                    var id = item.FindPropertyRelative(list.name == "_commands" ? "_commandId" : "_optionId");
                    if (id.stringValue == (error.CommandId ?? error.OptionId)) item.isExpanded = true;
                }
            _message = error.ToString(); _inspector?.MarkDirtyRepaint();
        }
        private void CloseIfDisabled()
        {
            EditorApplication.update -= CloseIfDisabled;
            if (this && !NarrativeEditorAvailability.Visible) Close();
        }
        public static void Open()
        {
            if (!NarrativeEditorAvailability.Visible) return;
            var window = GetWindow<NarrativeGraphWindow>(); window.ShowStory(window._story);
        }
        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId id, int line)
        {
            if (!NarrativeEditorAvailability.Visible) return false;
            var asset = EditorUtility.EntityIdToObject(id);
            if (asset is not NarrativeChapterSO && asset is not NarrativeNodeSO && asset is not NarrativeStorySO) return false;
            OpenFor(asset); return true;
        }
        public static void OpenFor(UnityEngine.Object asset)
        {
            if (!NarrativeEditorAvailability.Visible) return;
            var window = GetWindow<NarrativeGraphWindow>();
            if (asset is NarrativeStorySO story) window.ShowStory(story);
            else if (asset is NarrativeChapterSO chapter) window.ShowChapter(chapter);
            else if (asset is NarrativeNodeSO node)
            {
                var chapters = AssetDatabase.FindAssets("t:NarrativeChapterSO", new[] { "Assets" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c && c.Nodes.Contains(node)).ToArray();
                if (chapters.Length == 1) { window.ShowChapter(chapters[0]); window.SelectNode(node); }
                else window._message = "节点未登记到唯一章节，请检查节点目录。";
            }
        }
        public void ShowChapter(NarrativeChapterSO chapter)
        {
            _overview = false;
            if (chapter && (!_story || !_story.Chapters.Contains(chapter))) _story = NarrativeStoryModel.FindStory(chapter);
            _chapter = chapter; _selected = null; _errors = Array.Empty<NarrativeError>();
            BindContent(); RefreshGraph(); _graph?.schedule.Execute(() => _graph.FrameAll());
        }
        public void SelectNode(NarrativeNodeSO node) { SetInspectedNode(node); _graph?.SelectModel(node, false); }
        public void CreateNodeAt(NovelNodeKind kind, Vector2 position)
        {
            TryEdit(() =>
            {
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                var node = NarrativeGraphModel.CreateNode(_chapter, kind);
                if (_story && kind == NovelNodeKind.ChapterExit) NarrativeStoryModel.SyncExits(_story);
                NarrativeGraphModel.Move(_chapter, node, _snap ? NarrativeAutoLayout.Snap(position) : position);
                Undo.CollapseUndoOperations(group); RefreshGraph(); SelectNode(node);
            });
        }
        public void LocateCurrent(bool select)
        {
            var chapter = FindObservedChapter();
            if (!chapter) return;
            if (_chapter != chapter || _overview) ShowChapter(chapter);
            Locate(_snapshot.NodeId, select);
        }
        #endregion
    }
}
