using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    /// <summary>编辑器专用正交路由。障碍是节点正文/标题，路径没有运行语义。</summary>
    internal static class EmberFsmBlueprintRouting
    {
        internal static List<Vector2> Route(Vector2 start, Vector2 end, List<Rect> obstacles, float margin)
        {
            var a = start + Vector2.right * margin;
            var b = end + Vector2.left * margin;
            var rects = obstacles.Select(r => new Rect(r.xMin - 5, r.yMin - 5, r.width + 10, r.height + 10)).ToArray();
            var xs = new SortedSet<float> { a.x, b.x }; var ys = new SortedSet<float> { a.y, b.y };
            foreach (var r in rects) { xs.Add(r.xMin - margin); xs.Add(r.xMax + margin); ys.Add(r.yMin - margin); ys.Add(r.yMax + margin); }
            var xx = xs.ToArray(); var yy = ys.ToArray(); int width = xx.Length, count = width * yy.Length;
            int first = Array.IndexOf(yy, a.y) * width + Array.IndexOf(xx, a.x);
            int last = Array.IndexOf(yy, b.y) * width + Array.IndexOf(xx, b.x);
            var distance = Enumerable.Repeat(float.PositiveInfinity, count).ToArray();
            var previous = Enumerable.Repeat(-1, count).ToArray();
            var queue = new SortedSet<(float distance, int id)>(); distance[first] = 0; queue.Add((0, first));
            Vector2 Point(int id) => new(xx[id % width], yy[id / width]);
            while (queue.Count > 0)
            {
                var item = queue.Min; queue.Remove(item); int id = item.id;
                if (id == last) break;
                int x = id % width, y = id / width;
                var next = new[] { x > 0 ? id - 1 : -1, x + 1 < width ? id + 1 : -1,
                    y > 0 ? id - width : -1, y + 1 < yy.Length ? id + width : -1 };
                foreach (int n in next)
                {
                    if (n < 0 || rects.Any(r => Intersects(Point(id), Point(n), r))) continue;
                    float d = distance[id] + Vector2.Distance(Point(id), Point(n));
                    if (d >= distance[n]) continue;
                    queue.Remove((distance[n], n)); distance[n] = d; previous[n] = id; queue.Add((d, n));
                }
            }
            var result = new List<Vector2> { end, b };
            if (previous[last] >= 0)
            {
                int cursor = previous[last];
                while (cursor >= 0) { result.Add(Point(cursor)); cursor = previous[cursor]; }
            }
            else
            {
                // 重叠的用户布局可能阻塞出口；保留端点并绕到画布外沿，不能改真实绑定。
                float top = rects.Length == 0 ? Math.Min(a.y, b.y) - margin : rects.Min(r => r.yMin) - margin;
                result.Add(new Vector2(b.x, top)); result.Add(new Vector2(a.x, top)); result.Add(a);
            }
            result.Add(start); result.Reverse();
            for (int i = result.Count - 2; i > 0; i--)
                if (result[i] == result[i - 1] || result[i] == result[i + 1]) result.RemoveAt(i);
            return result;
        }
        internal static bool Intersects(Vector2 a, Vector2 b, Rect r)
        {
            if (Mathf.Approximately(a.x, b.x)) return a.x > r.xMin && a.x < r.xMax && Math.Max(a.y,b.y) > r.yMin && Math.Min(a.y,b.y) < r.yMax;
            return a.y > r.yMin && a.y < r.yMax && Math.Max(a.x,b.x) > r.xMin && Math.Min(a.x,b.x) < r.xMax;
        }
    }

    internal sealed class EmberFsmBlueprintPath : GraphElement
    {
        private readonly List<Vector2> _points;
        private readonly bool _dashed;
        private readonly Color _color;
        internal bool PathSelected { get; private set; }
        private bool _runtimeRecent;
        internal void SetRuntimeRecent(bool recent) { _runtimeRecent=recent; MarkDirtyRepaint(); }
        internal EmberFsmBlueprintPath(List<Vector2> points, bool dashed, Color color, Action selected)
        {
            _dashed = dashed; _color = color; capabilities = Capabilities.Selectable; layer = 2;
            // GraphElement 默认 margin 会把绘制原点平移，路径必须使用纯画布坐标。
            style.marginLeft=style.marginRight=style.marginTop=style.marginBottom=0;
            style.paddingLeft=style.paddingRight=style.paddingTop=style.paddingBottom=0;
            style.borderLeftWidth=style.borderRightWidth=style.borderTopWidth=style.borderBottomWidth=0;
            float x = points.Min(p => p.x) - 12, y = points.Min(p => p.y) - 12;
            _points = points.Select(p => p - new Vector2(x,y)).ToList();
            style.position = Position.Absolute; style.left = x; style.top = y;
            style.width = points.Max(p => p.x) - x + 12; style.height = points.Max(p => p.y) - y + 12;
            RegisterCallback<MouseDownEvent>(e => { selected(); e.StopPropagation(); });
            generateVisualContent += Draw;
        }
        public override bool ContainsPoint(Vector2 localPoint)
        {
            for (int i = 1; i < _points.Count; i++)
            {
                var d = _points[i] - _points[i-1]; float t = d.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(localPoint-_points[i-1],d)/d.sqrMagnitude);
                if (Vector2.Distance(localPoint, _points[i-1]+d*t) < 7) return true;
            }
            return false;
        }
        internal void SetSelected(bool selected) { PathSelected = selected; MarkDirtyRepaint(); }
        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D; painter.strokeColor = PathSelected ? Color.yellow : _runtimeRecent ? new Color(.35f,1,.65f) : _color;
            painter.lineWidth = PathSelected ? 3 : 1.5f;
            float distance = 0;
            for (int i = 1; i < _points.Count; i++)
            {
                var a = _points[i-1]; var b = _points[i]; var direction = (b-a).normalized; float length = Vector2.Distance(a,b);
                if (_dashed)
                {
                    float cursor = 0;
                    while (cursor < length)
                    {
                        float phase = (distance + cursor) % 12;
                        float step = Math.Min(length - cursor, (phase < 7 ? 7 : 12) - phase);
                        if (step < .001f) step = Math.Min(length - cursor, .001f);
                        if (phase < 7) { painter.BeginPath(); painter.MoveTo(a + direction * cursor); painter.LineTo(a + direction * (cursor + step)); painter.Stroke(); }
                        cursor += step;
                    }
                }
                else { painter.BeginPath(); painter.MoveTo(a); painter.LineTo(b); painter.Stroke(); }
                distance += length;
            }
            if (_points.Count < 2) return;
            var end = _points[^1]; var dir = (end - _points[^2]).normalized; var side = new Vector2(-dir.y,dir.x);
            painter.fillColor = PathSelected ? Color.yellow : _runtimeRecent ? new Color(.35f,1,.65f) : _color; painter.BeginPath(); painter.MoveTo(end);
            painter.LineTo(end-dir*10+side*5); painter.LineTo(end-dir*10-side*5); painter.ClosePath(); painter.Fill();
        }
    }
}
