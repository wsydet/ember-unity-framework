using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.LevelAuthoring.Editor
{
    public sealed class EmberRoomGraphWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private UnityEngine.Object _graph;
        [SerializeField] private string _adapterId, _owner, _selectedRoom, _revision;
        [SerializeField] private bool _capturedOwner;
        [SerializeField] private string _extensionScope;
        [SerializeField] private List<LayoutDraft> _draft = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable] private sealed class LayoutDraft { public string id; public Vector2 position; }
        private EmberRoomGraphBoard _board;
        private VisualElement _tools;
        private Label _status;
        private IMGUIContainer _details;
        private ObjectField _graphPicker;
        private EmberRoomGraphSnapshot _snapshot;
        public bool HasAccess => EmberRoomGraphService.Available && _owner == (EmberFsmBlueprintAccess.CurrentTemplateId ?? "")
            && EmberExtensionProject.AllowsScope(EmberExtensionProject.RoomGraph, _extensionScope);
        internal bool Bidirectional { get; private set; } = true;
        internal bool CanEdit => HasAccess && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            if (!_capturedOwner) { _owner = EmberFsmBlueprintAccess.CurrentTemplateId ?? ""; _capturedOwner = true; }
            if (string.IsNullOrEmpty(_extensionScope)) _extensionScope = EmberExtensionProject.CaptureScope(EmberExtensionProject.RoomGraph);
            Undo.undoRedoPerformed += OnUndo;
        }
        private void OnDisable() => Undo.undoRedoPerformed -= OnUndo;
        private void OnUndo() { if (HasAccess) Run(() => Reload(false)); }
        private void CreateGUI()
        {
            rootVisualElement.Clear();
            var picker = new ObjectField("原图资产") { objectType = typeof(ScriptableObject), allowSceneObjects = false, value = _graph };
            _graphPicker = picker;
            picker.RegisterValueChangedCallback(e => Run(() => SetGraph(e.newValue))); rootVisualElement.Add(picker);
            _tools = new VisualElement(); _tools.style.flexGrow = 1; rootVisualElement.Add(_tools);
            var toolbar = new Toolbar(); _tools.Add(toolbar);
            toolbar.Add(new ToolbarButton(() => Run(() => { _revision = EmberRoomGraphService.Revision(_adapterId, _graph); Reload(false); })) { text = "刷新基线（保留草稿）" });
            toolbar.Add(new ToolbarButton(() => Run(Arrange)) { text = "预览对接排版" });
            toolbar.Add(new ToolbarButton(() => Run(ApplyLayout)) { text = "应用排版" });
            toolbar.Add(new ToolbarButton(() => Run(() => Reload(true))) { text = "放弃排版草稿" });
            toolbar.Add(new ToolbarButton(() => _board?.Center()) { text = "定位 · 100%" });
            toolbar.Add(new ToolbarButton(() => Run(() => Report(string.Join("\n", EmberRoomGraphService.Validate(_adapterId, _graph)) is var text && text.Length > 0 ? text : "通用与项目校验通过。"))) { text = "校验" });
            var mode = new ToolbarToggle { text = "双向", value = true }; mode.RegisterValueChangedCallback(e => Bidirectional = e.newValue); toolbar.Add(mode);
            var split = new TwoPaneSplitView(1, 280, TwoPaneSplitViewOrientation.Horizontal); split.style.flexGrow = 1; _tools.Add(split);
            _board = new EmberRoomGraphBoard(this); _board.style.flexGrow = 1; split.Add(_board);
            var side = new ScrollView(); side.style.minWidth = 250; _details = new IMGUIContainer(DrawDetails); side.Add(_details); split.Add(side);
            _status = new Label("图排版与实际世界坐标独立。拖出口到入口连线；标题拖动仅产生排版草稿。资产保存仍由项目完成。");
            _status.style.whiteSpace = WhiteSpace.Normal; rootVisualElement.Add(_status);
            rootVisualElement.schedule.Execute(RefreshAccess).Every(250);
            if (_graph != null) Run(() => Reload(false)); RefreshAccess();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void RefreshAccess()
        {
            bool adapterAvailable;
            try { adapterAvailable = _graph == null || EmberRoomGraphService.FindAdapters(_graph).Contains(_adapterId); }
            catch (Exception error) { _tools?.SetEnabled(false); Report(error.Message); return; }
            _tools?.SetEnabled(CanEdit && adapterAvailable);
            if (!HasAccess || !adapterAvailable) Report("扩展、模板或业务适配器已关闭。原图与排版草稿保留；恢复条件后可刷新或重新打开。");
        }
        private void RequireAccess()
        { if (!HasAccess) throw new InvalidOperationException("窗口属于另一活动模板或房间图扩展已关闭；草稿保留。"); }
        private void Run(Action action) { try { RequireAccess(); action(); } catch (Exception error) { Report(error.Message); } }
        private void Report(string text) { if (_status != null) _status.text = text; }
        private void Reload(bool discard)
        {
            RequireAccess();
            var adapters = EmberRoomGraphService.FindAdapters(_graph);
            if (!adapters.Contains(_adapterId))
            {
                if (adapters.Length != 1) throw new InvalidOperationException("请选择有唯一项目适配器的原图；多个适配器时用 SetGraph 显式传入 ID。");
                _adapterId = adapters[0];
            }
            _snapshot = EmberRoomGraphService.Read(_adapterId, _graph);
            var errors = EmberRoomGraphRules.Validate(_snapshot);
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
            if (discard) _draft.Clear();
            if (_draft.Count == 0) _revision = EmberRoomGraphService.Revision(_adapterId, _graph);
            _board?.Load(_snapshot, _draft.ToDictionary(p => p.id, p => p.position)); _details?.MarkDirtyRepaint();
        }
        private void DrawDetails()
        {
            if (!HasAccess || _graph == null) return;
            var node = _snapshot?.Nodes.FirstOrDefault(n => n.Id == _selectedRoom);
            if (node == null) { EditorGUILayout.HelpBox("选择房间查看原资产和项目提供的正式试玩配置。", MessageType.Info); return; }
            EditorGUILayout.LabelField(node.Title, EditorStyles.boldLabel); EditorGUILayout.SelectableLabel(node.Id);
            if (node.Source != null && GUILayout.Button("定位既有房间资产")) { Selection.activeObject = node.Source; EditorGUIUtility.PingObject(node.Source); }
            EditorGUILayout.HelpBox("端口图锚点是卡片上的归一化坐标。世界锚点由项目显式提供；排版不会写入内容原点。", MessageType.Info);
            foreach (var port in node.Ports) EditorGUILayout.LabelField(port.Id + (port.Enabled ? "" : " · 关闭"), "图 " + port.Anchor + " / 世界 " + (port.WorldAnchor?.ToString() ?? "未声明"));
            if (EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelTrial))
                try
                {
                    foreach (var trial in EmberRoomGraphService.Trials(_adapterId, _graph, node.Id))
                        if (GUILayout.Button("试玩：" + trial.displayName)) Run(() => EmberRoomGraphService.StartTrial(_adapterId, _graph, node.Id, trial));
                }
                catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
            if (EmberLevelTrialSession.Active && GUILayout.Button("停止并恢复编辑现场")) EmberLevelTrialSession.Stop();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open()
        {
            EmberExtensionProject.Require(EmberExtensionProject.RoomGraph);
            string owner = EmberFsmBlueprintAccess.CurrentTemplateId ?? "";
            var window = Resources.FindObjectsOfTypeAll<EmberRoomGraphWindow>().FirstOrDefault(w => w._owner == owner && w.HasAccess) ?? CreateInstance<EmberRoomGraphWindow>();
            window.titleContent = new GUIContent("房间地图组合"); window.minSize = new Vector2(950, 550); window.Show();
        }
        public void SetGraph(UnityEngine.Object graph, string adapterId = null)
        {
            RequireAccess();
            if (_graph != graph && _draft.Count > 0) throw new InvalidOperationException("当前仍有排版草稿。请应用或明确放弃，再切换原图。");
            _graph = graph; _adapterId = adapterId; _selectedRoom = null;
            _graphPicker?.SetValueWithoutNotify(graph);
            if (graph != null) { Reload(false); _board?.schedule.Execute(_board.Center); }
            else { _snapshot = null; _board?.ClearGraph(); }
        }
        public void Arrange()
        {
            RequireAccess();
            var positions = EmberRoomGraphRules.Arrange(EmberRoomGraphService.Read(_adapterId, _graph));
            _draft = positions.Select(p => new LayoutDraft { id = p.Key, position = p.Value }).ToList();
            Reload(false); Report("排版预览已生成。应用前不会修改原图；实际世界原点保持。");
        }
        public void ApplyLayout()
        {
            RequireAccess();
            if (_draft.Count == 0) return;
            EmberRoomGraphService.ApplyLayout(_adapterId, _graph, _draft.ToDictionary(p => p.id, p => p.position), _revision);
            Reload(true); Report("图位置已应用，可 Undo；尚未隐式保存资产。");
        }
        internal void Move(string id, Vector2 position)
        {
            RequireAccess(); if (!CanEdit) return;
            if (_draft.Count == 0) _draft = _snapshot.Nodes.Select(n => new LayoutDraft { id = n.Id, position = n.GraphPosition }).ToList();
            _draft.Single(p => p.id == id).position = position;
        }
        internal void SelectRoom(string id) { _selectedRoom = id; _details?.MarkDirtyRepaint(); }
        internal bool Compatible(EmberRoomGraphConnection edge)
        {
            if (!CanEdit || _snapshot == null) return false;
            return EmberRoomGraphRules.Validate(new EmberRoomGraphSnapshot(_snapshot.Nodes, _snapshot.Connections.Append(edge), _snapshot.RequireOpposedNormals, _snapshot.RequireWorldAlignment, _snapshot.WorldTolerance)).Length == 0;
        }
        internal void Connect(EmberRoomGraphConnection edge) => Run(() =>
        {
            EmberRoomGraphService.Connect(_adapterId, _graph, edge, _revision);
            _revision = EmberRoomGraphService.Revision(_adapterId, _graph); Reload(false);
        });
        internal void Disconnect(EmberRoomGraphConnection edge) => Run(() =>
        {
            EmberRoomGraphService.Disconnect(_adapterId, _graph, edge, _revision);
            _revision = EmberRoomGraphService.Revision(_adapterId, _graph); Reload(false);
        });
        #endregion
    }
}
