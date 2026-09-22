using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Editor
{
    public sealed class NarrativeStoryGraphView : GraphView
    {
        #region 内部参数
        private sealed class ChapterCard : Node
        {
            internal NarrativeChapterSO Chapter;
            internal Port Input;
        }
        private readonly Dictionary<NarrativeChapterSO, ChapterCard> _cards = new();
        private readonly Action<NarrativeChapterSO> _select;
        private readonly Action<NarrativeChapterSO> _open;
        private readonly Action<Action> _edit;
        private readonly Action _create;
        private readonly NarrativeMiniMap _miniMap;
        private NarrativeStorySO _story;
        private bool _building;
        private bool _editable;
        public int ChapterCount => _cards.Count;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private GraphViewChange Changed(GraphViewChange change)
        {
            if (_building) return change;
            if (!_editable) { change.edgesToCreate?.Clear(); change.elementsToRemove?.Clear(); return change; }
            if (change.movedElements != null)
            {
                var positions = new Dictionary<NarrativeChapterSO, Vector2>();
                foreach (var card in change.movedElements.OfType<ChapterCard>())
                {
                    var rect = card.GetPosition(); rect.position = NarrativeAutoLayout.Snap(rect.position); card.SetPosition(rect);
                    positions.Add(card.Chapter, rect.position);
                }
                if (positions.Count > 0) _edit(() => NarrativeStoryModel.Move(_story, positions));
            }
            if (change.elementsToRemove != null)
                foreach (var edge in change.elementsToRemove.OfType<Edge>())
                    if (edge.output?.userData is ValueTuple<int, int> port)
                        _edit(() => NarrativeStoryModel.Connect(_story, port.Item1, port.Item2, null));
            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                    if (edge.output.userData is ValueTuple<int, int> port && edge.input.node is ChapterCard target)
                        _edit(() => NarrativeStoryModel.Connect(_story, port.Item1, port.Item2, target.Chapter));
            return change;
        }
        private void AddOutput(ChapterCard card, int exitIndex, int routeIndex, string title, NarrativeChapterSO target)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(NarrativeChapterSO));
            port.portName = title; port.tooltip = title; port.userData = (exitIndex, routeIndex); port.SetEnabled(_editable); card.outputContainer.Add(port);
            if (target && _cards.TryGetValue(target, out var destination)) AddElement(port.ConnectTo(destination.Input));
        }
        private void OnKey(KeyDownEvent e)
        {
            if (e.target is TextElement) return;
            if (e.keyCode == KeyCode.Space && _editable) { _create(); e.StopImmediatePropagation(); }
            if (e.keyCode == KeyCode.A && !e.actionKey) { FrameAll(); e.StopPropagation(); }
            if (e.keyCode == KeyCode.F) { FrameSelection(); e.StopPropagation(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeStoryGraphView(Action<NarrativeChapterSO> select, Action<NarrativeChapterSO> open, Action<Action> edit, Action create)
        {
            _select = select; _open = open; _edit = edit; _create = create; style.flexGrow = 1; focusable = true;
            SetupZoom(.1f, 3);
            var grid = new GridBackground(); grid.StretchToParentSize(); Insert(0, grid);
            this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector()); this.AddManipulator(new ClickSelector());
            RegisterCallback<MouseDownEvent>(_ => Focus()); RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            graphViewChanged = Changed; nodeCreationRequest = _ => { if (_editable) _create(); };
            deleteSelection = (_, _) =>
            {
                if (!_editable) return;
                var edges = selection.OfType<Edge>().ToArray();
                _edit(() => { foreach (var edge in edges) if (edge.output.userData is ValueTuple<int, int> p) NarrativeStoryModel.Connect(_story, p.Item1, p.Item2, null); });
            };
            _miniMap = new NarrativeMiniMap(this); Add(_miniMap);
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
            => _editable ? ports.Where(p => p.direction != startPort.direction).ToList() : new List<Port>();
        public override void AddToSelection(ISelectable selectable)
        { base.AddToSelection(selectable); if (!_building && selectable is ChapterCard card) _select(card.Chapter); }
        public override void ClearSelection() { base.ClearSelection(); if (!_building) _select(null); }
        public void Rebuild(NarrativeStorySO story)
        {
            _building = true; _story = story;
            try
            {
                ClearSelection();
                foreach (var item in graphElements.Where(e => e is Node || e is Edge).ToArray()) RemoveElement(item);
                _cards.Clear(); _editable = NarrativeGraphModel.CanEdit(story); if (!story) return;
                var layout = NarrativeGraphModel.GetLayout(story, false); int i = 0;
                foreach (var chapter in story.Chapters)
                {
                    if (!chapter || _cards.ContainsKey(chapter)) continue;
                    var card = new ChapterCard { Chapter = chapter, title = (story.Entry == chapter ? "入口 · " : "") + chapter.DisplayName };
                    card.capabilities &= ~Capabilities.Deletable;
                    if (!_editable) card.capabilities &= ~Capabilities.Movable;
                    card.style.width = 320; card.style.minHeight = 150;
                    card.titleContainer.style.backgroundColor = new Color(.24f, .39f, .54f);
                    card.Input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(NarrativeChapterSO));
                    card.Input.portName = "进入章节"; card.Input.SetEnabled(_editable); card.inputContainer.Add(card.Input);
                    var label = new Label(chapter.Nodes.Count + " 个节点 · 双击进入\n" + chapter.ChapterId);
                    label.AddToClassList("narrative-summary"); card.extensionContainer.Add(label);
                    card.RegisterCallback<MouseDownEvent>(e => { if (e.button == 0 && e.clickCount == 2) { _open(chapter); e.StopImmediatePropagation(); } });
                    card.SetPosition(new Rect(layout ? layout.GetPosition(chapter.ChapterId, i) : new Vector2(i % 3 * 440, i / 3 * 320), new Vector2(320, 180)));
                    AddElement(card); _cards.Add(chapter, card); i++;
                }
                for (int j = 0; j < story.Exits.Count; j++)
                {
                    var link = story.Exits[j]; if (link?.Source == null || !_cards.TryGetValue(link.Source, out var card)) continue;
                    string exit = link.Exit ? link.Exit.name : "失效出口";
                    for (int k = 0; k < link.Routes.Count; k++)
                        AddOutput(card, j, k, exit + " · " + (k + 1) + " " + link.Routes[k]?.Text, link.Routes[k]?.Target);
                    AddOutput(card, j, -1, exit + " · 兜底", link.Fallback);
                }
                foreach (var card in _cards.Values) { card.RefreshExpandedState(); card.RefreshPorts(); }
            }
            finally { _building = false; _miniMap.SetNodes(_cards.Values); }
        }
        public void ShowExecution(NarrativeSnapshot snapshot)
        {
            foreach (var card in _cards.Values) card.EnableInClassList("narrative-current", snapshot != null &&
                snapshot.StoryId == _story?.StoryId && snapshot.ChapterId == card.Chapter.ChapterId);
        }
        public void SelectChapter(NarrativeChapterSO chapter, bool frame)
        {
            _building = true;
            try { ClearSelection(); if (chapter && _cards.TryGetValue(chapter, out var card)) AddToSelection(card); }
            finally { _building = false; }
            if (frame) FrameSelection();
        }
        public void AutoArrange()
        {
            if (!_editable) return;
            var positions = new Dictionary<NarrativeChapterSO, Vector2>(); int i = 0;
            foreach (var chapter in _cards.Keys) { positions.Add(chapter, new Vector2(i % 3 * 460, i / 3 * 380)); i++; }
            _edit(() => NarrativeStoryModel.Move(_story, positions)); schedule.Execute(() => FrameAll()).ExecuteLater(160);
        }
        #endregion
    }
}
