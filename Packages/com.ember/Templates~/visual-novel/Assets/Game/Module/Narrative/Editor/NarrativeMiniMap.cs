using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Editor
{
    /// <summary>固定屏幕尺寸的概览；节点与视口使用同一个等比变换，极小缩放也不会越界。</summary>
    public sealed class NarrativeMiniMap : VisualElement
    {
        #region 内部参数
        private readonly GraphView _graph;
        private readonly IMGUIContainer _canvas;
        private readonly List<Node> _nodes = new();
        public readonly struct Mapping
        {
            public readonly Rect World;
            public readonly Rect Area;
            public readonly float Scale;
            public Mapping(Rect world, Rect area)
            {
                World = world; Scale = Mathf.Min(area.width / Mathf.Max(1, world.width), area.height / Mathf.Max(1, world.height));
                Area = new Rect(area.center - world.size * Scale * .5f, world.size * Scale);
            }
            public Rect Project(Rect rect) => new(Area.position + (rect.position - World.position) * Scale, rect.size * Scale);
            public Vector2 Unproject(Vector2 point) => World.position + (point - Area.position) / Scale;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Rect Viewport() => Rect.MinMaxRect(
            _graph.contentViewContainer.WorldToLocal(_graph.worldBound.min).x,
            _graph.contentViewContainer.WorldToLocal(_graph.worldBound.min).y,
            _graph.contentViewContainer.WorldToLocal(_graph.worldBound.max).x,
            _graph.contentViewContainer.WorldToLocal(_graph.worldBound.max).y);
        private void Draw()
        {
            Rect area = new(8, 5, _canvas.contentRect.width - 16, _canvas.contentRect.height - 13);
            if (area.width <= 0 || area.height <= 0 || _graph.panel == null) return;
            var mapping = GetMapping(area);
            if (Event.current.type == EventType.Repaint)
            {
                foreach (var node in _nodes)
                    if (node.parent != null) EditorGUI.DrawRect(mapping.Project(node.GetPosition()), node.selected ? new Color(.35f, .65f, 1) : new Color(.55f, .58f, .62f));
                Rect view = mapping.Project(Viewport());
                EditorGUI.DrawRect(view, new Color(.8f, .85f, .25f, .12f));
                Color border = new(.8f, .85f, .25f, .8f);
                EditorGUI.DrawRect(new Rect(view.x, view.y, view.width, 1), border);
                EditorGUI.DrawRect(new Rect(view.x, view.yMax - 1, view.width, 1), border);
                EditorGUI.DrawRect(new Rect(view.x, view.y, 1, view.height), border);
                EditorGUI.DrawRect(new Rect(view.xMax - 1, view.y, 1, view.height), border);
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && area.Contains(Event.current.mousePosition))
            {
                Vector2 point = mapping.Unproject(Event.current.mousePosition);
                Vector3 scale = _graph.contentViewContainer.resolvedStyle.scale.value;
                _graph.UpdateViewTransform(_graph.contentRect.center - point * scale.x, scale);
                Event.current.Use();
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeMiniMap(GraphView graph)
        {
            _graph = graph; name = "narrative-mini-map";
            style.position = Position.Absolute; style.left = 16; style.top = 16;
            style.width = 200; style.height = 144; style.flexShrink = 0; style.overflow = Overflow.Hidden;
            style.backgroundColor = new Color(.15f, .16f, .18f, .96f);
            var label = new Label("概览 · 点击定位"); label.style.marginLeft = 8; label.style.marginTop = 5; Add(label);
            _canvas = new IMGUIContainer(Draw); _canvas.style.flexGrow = 1; Add(_canvas);
            graph.viewTransformChanged += _ => _canvas.MarkDirtyRepaint();
            RegisterCallback<MouseDownEvent>(e => e.StopPropagation());
            RegisterCallback<WheelEvent>(e => e.StopPropagation());
        }
        public void SetNodes(IEnumerable<Node> nodes) { _nodes.Clear(); _nodes.AddRange(nodes); _canvas.MarkDirtyRepaint(); }
        public Mapping GetMapping(Rect area)
        {
            Rect world = Viewport();
            foreach (var node in _nodes)
            {
                if (node.parent == null) continue;
                var rect = node.GetPosition();
                world = Rect.MinMaxRect(Mathf.Min(world.xMin, rect.xMin), Mathf.Min(world.yMin, rect.yMin), Mathf.Max(world.xMax, rect.xMax), Mathf.Max(world.yMax, rect.yMax));
            }
            return new Mapping(world, area);
        }
        #endregion
    }
}
