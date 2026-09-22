using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Editor
{
    /// <summary>保留式画布。导航只更新视图变换，不读取 SO、磁盘或重建节点。</summary>
    public sealed class NarrativeFlowGraphView : GraphView
    {
        #region 内部参数
        private readonly Dictionary<NarrativeNodeSO, FlowNode> _nodes = new();
        private readonly Action<NarrativeNodeSO> _selected;
        private readonly Action<Action> _edit;
        private readonly Action<Vector2, Vector2> _search;
        private NarrativeChapterSO _chapter;
        private bool _building;
        private bool _editable;
        private Vector2 _mouse;
        private FlowNode _current;
        private readonly NarrativeMiniMap _miniMap;
        public bool SnapToGrid { get; set; } = true;
        public int BuildCount { get; private set; }
        public int NodeCount => _nodes.Count;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class FlowNode : Node
        {
            internal NarrativeNodeSO Model { get; }
            internal Port Input { get; }
            internal Dictionary<string, Port> Outputs { get; } = new();
            internal FlowNode(NarrativeNodeSO model, NarrativeChapterSO chapter, NarrativeTableCatalog catalog, bool editable)
            {
                Model = model; viewDataKey = model.NodeId;
                title = (chapter.Entry == model ? "开始 · " : "") + (model is NarrativeChoiceSO ? "玩家选择" : model is NarrativeBranchSO ? "条件分支" : model is NarrativeEndingSO ? "结局" : model is NarrativeChapterExitSO ? "转到下一章" : "对话段") + " · " + model.name;
                tooltip = model.NodeId;
                style.width = 260;
                style.minHeight = 150;
                if (!editable) capabilities &= ~(Capabilities.Movable | Capabilities.Deletable | Capabilities.Copiable);
                Color color = model is NarrativeChoiceSO ? new Color(.58f, .35f, .72f) :
                    model is NarrativeBranchSO ? new Color(.7f, .48f, .18f) :
                    model is NarrativeEndingSO ? new Color(.3f, .55f, .38f) : new Color(.2f, .42f, .64f);
                titleContainer.style.backgroundColor = color;
                Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(NarrativeNodeSO));
                Input.portName = "入口"; Input.SetEnabled(editable); inputContainer.Add(Input);
                var summary = new Label(Summary(model, catalog)); summary.AddToClassList("narrative-summary");
                extensionContainer.Add(summary);
                var paths = NarrativeGraphModel.Ports(model);
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(NarrativeNodeSO));
                    port.portName = path == "_next" ? "后续" : path == "_fallback" ? "兜底" :
                        model is NarrativeChoiceSO choice ? (i + 1) + ". " + choice.Options[i]?.Text :
                        model is NarrativeBranchSO branch ? (i + 1) + ". " + branch.Branches[i]?.Text : path;
                    port.tooltip = port.portName; port.userData = path; port.SetEnabled(editable);
                    outputContainer.Add(port); Outputs.Add(path, port);
                }
                RefreshExpandedState(); RefreshPorts();
            }
            private static string Summary(NarrativeNodeSO node, NarrativeTableCatalog catalog)
            {
                if (node is NarrativeDialogueSO dialogue)
                {
                    var commands = dialogue.Commands;
                    var command = commands.FirstOrDefault(c => c != null && c.Kind == NovelCommandKind.Say);
                    string name = command?.CharacterId ?? "";
                    if (catalog != null && catalog.TryGetCharacter(name, out var character)) name = character.DisplayName;
                    if (string.IsNullOrEmpty(name)) name = "旁白";
                    return commands.Count + " 条指令\n" + (command == null ? "对话与演出" : name + "：" + command.Text);
                }
                if (node is NarrativeChoiceSO choice) return choice.Prompt;
                if (node is NarrativeBranchSO) return "按顺序首个命中，否则走兜底";
                if (node is NarrativeChapterExitSO) return "章节出口 · 在章节总览配置下一章";
                return "结局 · " + ((NarrativeEndingSO)node).EndingId;
            }
        }

        private GraphViewChange Changed(GraphViewChange change)
        {
            if (_building) return change;
            if (!_editable)
            {
                change.edgesToCreate?.Clear(); change.elementsToRemove?.Clear(); return change;
            }
            if (change.movedElements != null)
            {
                var moved = change.movedElements.OfType<FlowNode>().ToList();
                if (moved.Count > 0)
                {
                    Vector2 correction = SnapToGrid ? NarrativeAutoLayout.Snap(moved[0].GetPosition().position) - moved[0].GetPosition().position : Vector2.zero;
                    var positions = new Dictionary<NarrativeNodeSO, Vector2>();
                    foreach (var node in moved)
                    {
                        var rect = node.GetPosition(); rect.position += correction; node.SetPosition(rect);
                        positions[node.Model] = rect.position;
                    }
                    _edit(() => NarrativeGraphModel.MoveMany(_chapter, positions));
                }
            }
            if (change.elementsToRemove != null)
                foreach (var edge in change.elementsToRemove.OfType<Edge>())
                    if (edge.output?.node is FlowNode source && edge.input?.node is FlowNode target &&
                        NarrativeGraphModel.Target(source.Model, (string)edge.output.userData) == target.Model)
                        _edit(() => NarrativeGraphModel.Connect(_chapter, source.Model, (string)edge.output.userData, null));
            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                    if (edge.output.node is FlowNode source && edge.input.node is FlowNode target)
                        _edit(() => NarrativeGraphModel.Connect(_chapter, source.Model, (string)edge.output.userData, target.Model));
            return change;
        }
        private void DeleteSelected()
        {
            if (!_editable) return;
            var nodes = selection.OfType<FlowNode>().Select(n => n.Model).ToArray();
            var edges = selection.OfType<Edge>().ToArray();
            if (nodes.Length > 0)
            {
                var incoming = nodes.SelectMany(n => NarrativeGraphModel.Incoming(_chapter, n)).ToArray();
                if (!EditorUtility.DisplayDialog("从本章移除 " + nodes.Length + " 个节点", "保留独立资产，可撤销。以下入口/入链将断开：\n" + string.Join("\n", incoming.Take(20)), "移除并断开", "取消")) return;
            }
            _edit(() =>
            {
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                foreach (var edge in edges)
                    if (edge.output?.node is FlowNode source) NarrativeGraphModel.Connect(_chapter, source.Model, (string)edge.output.userData, null);
                foreach (var node in nodes) NarrativeGraphModel.Remove(_chapter, node, true);
                Undo.CollapseUndoOperations(group);
            });
        }
        private void OnKey(KeyDownEvent e)
        {
            if (e.target is TextElement || (e.target as VisualElement)?.GetFirstAncestorOfType<TextField>() != null) return;
            if (e.keyCode == KeyCode.Space && _editable)
            { OpenSearch(_mouse); e.StopImmediatePropagation(); }
            else if (e.keyCode == KeyCode.A && !e.actionKey) { FrameAll(); e.StopPropagation(); }
            else if (e.keyCode == KeyCode.F) { FrameSelection(); e.StopPropagation(); }
            else if (e.keyCode == KeyCode.D && e.actionKey && _editable) { DuplicateSelection(); e.StopPropagation(); }
        }
        private void OpenSearch(Vector2 point) => _search(this.LocalToWorld(point), contentViewContainer.WorldToLocal(this.LocalToWorld(point)));
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeFlowGraphView(Action<NarrativeNodeSO> selected, Action<Action> edit, Action<Vector2, Vector2> search)
        {
            _selected = selected; _edit = edit; _search = search;
            style.flexGrow = 1; focusable = true;
            SetupZoom(.1f, 3f);
            var grid = new GridBackground(); grid.StretchToParentSize(); Insert(0, grid);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger()); // Unity 自带节点对齐辅助线与吸附。
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            RegisterCallback<MouseMoveEvent>(e => _mouse = e.localMousePosition);
            RegisterCallback<MouseDownEvent>(e => { _mouse = e.localMousePosition; Focus(); });
            RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            graphViewChanged = Changed;
            nodeCreationRequest = context => OpenSearch(_mouse);
            deleteSelection = (_, _) => DeleteSelected();
            _miniMap = new NarrativeMiniMap(this); Add(_miniMap);
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
            => _editable ? ports.Where(p => p.direction != startPort.direction).ToList() : new List<Port>();

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            if (!_building && selectable is FlowNode node) _selected(node.Model);
        }
        public override void ClearSelection()
        {
            base.ClearSelection(); if (!_building) _selected?.Invoke(null);
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent e)
        {
            if (_editable)
            {
                e.menu.AppendAction("添加节点…  Space", _ => OpenSearch(_mouse));
                if (selection.OfType<FlowNode>().Any()) e.menu.AppendAction("复制所选  Ctrl+D", _ => DuplicateSelection());
                if (selection.Count > 0) e.menu.AppendAction("删除所选", _ => DeleteSelected());
            }
            e.menu.AppendAction("显示全部  A", _ => FrameAll());
            e.menu.AppendAction("聚焦所选  F", _ => FrameSelection());
        }
        public void Rebuild(NarrativeChapterSO chapter, NarrativeTableCatalog catalog)
        {
            BuildCount++; _building = true;
            var selectedModels = selection.OfType<FlowNode>().Select(n => n.Model).ToArray();
            try
            {
                ClearSelection();
                foreach (var element in graphElements.Where(e => e is Node || e is Edge).ToList()) RemoveElement(element);
                _nodes.Clear(); _current = null; _chapter = chapter;
                _editable = NarrativeGraphModel.CanEdit(chapter);
                if (!chapter) return;
                var layout = NarrativeGraphModel.GetLayout(chapter, false); int index = 0;
                var defaultPositions = NarrativeAutoLayout.Calculate(chapter);
                foreach (var model in chapter.Nodes)
                {
                    if (!model || _nodes.ContainsKey(model)) continue;
                    var node = new FlowNode(model, chapter, catalog, _editable);
                    node.SetPosition(new Rect(layout ? layout.GetPosition(model.NodeId, index) : defaultPositions[model], new Vector2(260, 160)));
                    _nodes.Add(model, node); AddElement(node); index++;
                }
                foreach (var node in _nodes.Values)
                    foreach (var output in node.Outputs)
                    {
                        var target = NarrativeGraphModel.Target(node.Model, output.Key);
                        if (!target || !_nodes.TryGetValue(target, out var destination)) continue;
                        var edge = output.Value.ConnectTo(destination.Input);
                        if (!_editable) edge.capabilities &= ~Capabilities.Deletable;
                        AddElement(edge);
                    }
                foreach (var model in selectedModels) if (model && _nodes.TryGetValue(model, out var node)) AddToSelection(node);
            }
            finally { _building = false; _miniMap.SetNodes(_nodes.Values); }
        }
        public void SelectModel(NarrativeNodeSO model, bool frame)
        {
            _building = true;
            try { ClearSelection(); if (model && _nodes.TryGetValue(model, out var node)) AddToSelection(node); }
            finally { _building = false; }
            if (frame && model) FrameSelection();
        }
        public void ShowExecution(NarrativeSnapshot snapshot)
        {
            FlowNode next = null;
            if (_chapter && snapshot?.ChapterId == _chapter.ChapterId)
                next = _nodes.Values.FirstOrDefault(n => n.Model.NodeId == snapshot.NodeId);
            if (_current == next) return;
            _current?.RemoveFromClassList("narrative-current"); _current = next; _current?.AddToClassList("narrative-current");
        }
        public void AutoArrange()
        {
            if (!_editable || !_chapter) return;
            _edit(() =>
            {
                var positions = NarrativeAutoLayout.Calculate(_chapter);
                NarrativeGraphModel.MoveMany(_chapter, positions);
                foreach (var pair in positions) if (_nodes.TryGetValue(pair.Key, out var node)) node.SetPosition(new Rect(pair.Value, node.GetPosition().size));
            });
            schedule.Execute(() => FrameAll());
        }
        public void ZoomTo(float scale)
        {
            scale = Mathf.Clamp(scale, .1f, 3);
            Vector2 center = contentViewContainer.WorldToLocal(worldBound.center);
            Vector2 local = this.WorldToLocal(worldBound.center);
            UpdateViewTransform(local - center * scale, Vector3.one * scale);
        }
        public void DuplicateSelection()
        {
            if (!_editable || !_chapter) return;
            var sources = selection.OfType<FlowNode>().ToArray();
            if (sources.Length == 0) return;
            _edit(() =>
            {
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                var copies = new Dictionary<NarrativeNodeSO, NarrativeNodeSO>();
                var positions = new Dictionary<NarrativeNodeSO, Vector2>();
                foreach (var source in sources)
                {
                    var copy = NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Dialogue, source.Model);
                    copies[source.Model] = copy; positions[copy] = source.GetPosition().position + new Vector2(60, 60);
                }
                foreach (var copy in copies.Values)
                    foreach (string port in NarrativeGraphModel.Ports(copy))
                    {
                        var target = NarrativeGraphModel.Target(copy, port);
                        if (target && copies.TryGetValue(target, out var newTarget)) NarrativeGraphModel.Connect(_chapter, copy, port, newTarget);
                    }
                NarrativeGraphModel.MoveMany(_chapter, positions); Undo.CollapseUndoOperations(group);
                _selected(copies.Values.First());
            });
        }
        #endregion
    }
}
