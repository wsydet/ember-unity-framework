using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>固定1:1画布，缩略图保持宽高比。图拖拽只写窗口草稿。</summary>
    internal sealed class EmberRoomGraphBoard : GraphView
    {
        #region 内部参数
        private readonly EmberRoomGraphWindow _window;
        private readonly Dictionary<(string room, string port, Direction direction), Port> _ports = new();
        private bool _loading, _restoringScale;
        private sealed class Endpoint { internal string Room, Port; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Connector : IEdgeConnectorListener
        {
            private readonly EmberRoomGraphBoard _board;
            internal Connector(EmberRoomGraphBoard board) => _board = board;
            public void OnDropOutsidePort(Edge edge, Vector2 position) { }
            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (edge.output?.userData is not Endpoint a || edge.input?.userData is not Endpoint b) return;
                edge.RemoveFromHierarchy();
                _board.schedule.Execute(() => _board._window.Connect(new EmberRoomGraphConnection(a.Room, a.Port, b.Room, b.Port, _board._window.Bidirectional)));
            }
        }
        private sealed class RoomPort : Port
        {
            internal RoomPort(EmberRoomGraphBoard board, Direction direction, Vector2 normal)
                : base(Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? Orientation.Vertical : Orientation.Horizontal, direction, Capacity.Single, typeof(EmberRoomGraphPort))
            {
                m_EdgeConnector = new EdgeConnector<Edge>(new Connector(board)); this.AddManipulator(m_EdgeConnector);
                portName = ""; style.position = Position.Absolute; style.width = 20; style.height = 20;
                m_ConnectorBox.style.width = 18; m_ConnectorBox.style.height = 18;
                m_ConnectorBox.style.flexGrow = 0; m_ConnectorBox.style.alignItems = Align.Center; m_ConnectorBox.style.justifyContent = Justify.Center;
                m_ConnectorBoxCap.style.width = 10; m_ConnectorBoxCap.style.height = 10; m_ConnectorBoxCap.style.flexGrow = 0; m_ConnectorBoxCap.style.flexShrink = 0;
                portColor = direction == Direction.Output ? new Color(.25f, .86f, .73f) : new Color(.44f, .69f, 1);
            }
        }
        private sealed class RoomNode : Node
        {
            internal string Id;
            private readonly EmberRoomGraphBoard _board;
            public override void OnSelected() { base.OnSelected(); _board._window.SelectRoom(Id); }
            internal RoomNode(EmberRoomGraphBoard board, EmberRoomGraphNode source, Vector2 position)
            {
                _board = board; Id = source.Id; title = source.Title;
                capabilities &= ~(Capabilities.Deletable | Capabilities.Collapsible | Capabilities.Resizable);
                style.width = source.GraphSize.x; style.height = source.GraphSize.y;
                SetPosition(new Rect(position, source.GraphSize));
                inputContainer.style.display = outputContainer.style.display = DisplayStyle.None;
                mainContainer.style.flexGrow = 1;
                var preview = new Image { image = source.Preview, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                preview.style.flexGrow = 1; preview.style.minHeight = 0; preview.style.marginLeft = preview.style.marginRight = 22;
                preview.style.marginTop = preview.style.marginBottom = 20; mainContainer.Add(preview);
                if (source.Preview == null) preview.Add(new Label("暂无项目提供的静态预览"));
                foreach (var port in source.Ports)
                {
                    var point = port.Anchor * source.GraphSize;
                    if (!port.Enabled)
                    {
                        var dot = new VisualElement { tooltip = port.Id + " · 未启用" };
                        dot.style.position = Position.Absolute; dot.style.left = point.x - 3; dot.style.top = point.y - 3;
                        dot.style.width = dot.style.height = 6; dot.style.backgroundColor = Color.gray; mainContainer.Add(dot); continue;
                    }
                    if (port.Entrance) AddPort(Direction.Input);
                    if (port.Exit) AddPort(Direction.Output);
                    void AddPort(Direction direction)
                    {
                        var view = new RoomPort(board, direction, port.Normal) { userData = new Endpoint { Room = source.Id, Port = port.Id }, tooltip = source.Id + "/" + port.Id };
                        float inset = port.Entrance && port.Exit && direction == Direction.Input ? 30 : 12;
                        var at = point - port.Normal.normalized * inset;
                        view.style.left = at.x - 10; view.style.top = at.y - 10;
                        mainContainer.Add(view); board._ports.Add((source.Id, port.Id, direction), view);
                    }
                }
                RefreshExpandedState(); RefreshPorts();
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberRoomGraphBoard(EmberRoomGraphWindow window)
        {
            _window = window;
            viewTransformChanged = view =>
            {
                if (_restoringScale || view.contentViewContainer.resolvedStyle.scale.value == Vector3.one) return;
                _restoringScale = true;
                try { view.UpdateViewTransform(view.contentViewContainer.resolvedStyle.translate, Vector3.one); }
                finally { _restoringScale = false; }
            };
            this.AddManipulator(new ContentDragger()); this.AddManipulator(new SelectionDragger()); this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground(); Insert(0, grid); grid.StretchToParentSize();
            graphViewChanged = change =>
            {
                if (_loading) return change;
                if (!_window.CanEdit) { change.movedElements?.Clear(); change.elementsToRemove?.Clear(); change.edgesToCreate?.Clear(); return change; }
                if (change.movedElements != null) foreach (var node in change.movedElements.OfType<RoomNode>()) _window.Move(node.Id, node.GetPosition().position);
                if (change.elementsToRemove != null)
                {
                    foreach (var edge in change.elementsToRemove.OfType<Edge>().ToArray())
                        if (edge.userData is EmberRoomGraphConnection connection) schedule.Execute(() => _window.Disconnect(connection));
                    change.elementsToRemove.Clear();
                }
                change.edgesToCreate?.Clear(); return change;
            };
        }
        internal void ClearGraph()
        {
            _loading = true;
            try
            {
                ClearSelection();
                foreach (var edge in edges.ToList()) { edge.input?.Disconnect(edge); edge.output?.Disconnect(edge); RemoveElement(edge); }
                foreach (var node in nodes.ToList()) RemoveElement(node);
                _ports.Clear();
            }
            finally { _loading = false; }
        }
        internal void Load(EmberRoomGraphSnapshot graph, IReadOnlyDictionary<string, Vector2> positions)
        {
            ClearGraph(); _loading = true;
            try
            {
                foreach (var node in graph.Nodes) AddElement(new RoomNode(this, node, positions.TryGetValue(node.Id, out var at) ? at : node.GraphPosition));
                foreach (var connection in graph.Connections)
                    if (_ports.TryGetValue((connection.FromRoom, connection.FromPort, Direction.Output), out var from) && _ports.TryGetValue((connection.ToRoom, connection.ToPort, Direction.Input), out var to))
                    { var edge = from.ConnectTo(to); edge.userData = connection; edge.tooltip = connection.Bidirectional ? "双向通行" : "单向通行"; AddElement(edge); }
            }
            finally { _loading = false; }
        }
        internal void Center()
        {
            var all = nodes.ToArray(); if (all.Length == 0) return;
            var min = new Vector2(all.Min(n => n.GetPosition().xMin), all.Min(n => n.GetPosition().yMin));
            UpdateViewTransform(new Vector3(50 - min.x, 50 - min.y, 0), Vector3.one);
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
        {
            if (startPort.userData is not Endpoint start) return new List<Port>();
            return ports.Where(p => p != startPort && p.direction != startPort.direction && p.userData is Endpoint end &&
                _window.Compatible(startPort.direction == Direction.Output ? new EmberRoomGraphConnection(start.Room, start.Port, end.Room, end.Port, _window.Bidirectional)
                    : new EmberRoomGraphConnection(end.Room, end.Port, start.Room, start.Port, _window.Bidirectional))).ToList();
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // 不安装可删除节点/缩放画布的默认快捷菜单。
            evt.menu.AppendAction("定位 · 100%", _ => Center());
        }
        #endregion
    }
}
