using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    public static class EmberRoomGraphRules
    {
        #region 内部方法
        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string[] Validate(EmberRoomGraphSnapshot graph)
        {
            var errors = new List<string>();
            if (graph == null) return new[] { "缺少房间图。" };
            if (!float.IsFinite(graph.WorldTolerance) || graph.WorldTolerance < 0) errors.Add("世界锚点容差无效。");
            var nodes = new Dictionary<string, EmberRoomGraphNode>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !nodes.TryAdd(node.Id, node)) { errors.Add("房间身份缺失或重复。"); continue; }
                if (!Finite(node.GraphPosition) || !Finite(node.GraphSize) || node.GraphSize.x <= 0 || node.GraphSize.y <= 0) errors.Add("图尺寸/位置非法：" + node.Id);
                var ports = new HashSet<string>(StringComparer.Ordinal);
                foreach (var port in node.Ports)
                {
                    if (port == null || string.IsNullOrWhiteSpace(port.Id) || !ports.Add(port.Id)) { errors.Add("端口身份缺失或重复：" + node.Id); continue; }
                    if (!Finite(port.Anchor) || port.Anchor.x < 0 || port.Anchor.y < 0 || port.Anchor.x > 1 || port.Anchor.y > 1 ||
                        !Finite(port.Normal) || port.Normal.sqrMagnitude < .001f || port.WorldAnchor.HasValue && !Finite(port.WorldAnchor.Value)) errors.Add("端口坐标非法：" + node.Id + "/" + port.Id);
                }
            }
            if (errors.Count > 0) return errors.ToArray();
            var used = new HashSet<(string room, string port)>();
            foreach (var edge in graph.Connections)
            {
                if (edge == null || !nodes.TryGetValue(edge.FromRoom ?? "", out var a) || !nodes.TryGetValue(edge.ToRoom ?? "", out var b)) { errors.Add("连线引用缺失房间。"); continue; }
                var ap = a.Ports.FirstOrDefault(p => p.Id == edge.FromPort); var bp = b.Ports.FirstOrDefault(p => p.Id == edge.ToPort);
                if (a == b || ap == null || bp == null || !ap.Enabled || !bp.Enabled || !ap.Exit || !bp.Entrance || edge.Bidirectional && (!ap.Entrance || !bp.Exit)) { errors.Add("连线方向、启用状态或端口引用非法。"); continue; }
                if (!used.Add((a.Id, ap.Id)) || !used.Add((b.Id, bp.Id))) errors.Add("端口已占用；不会自动替换已有连线。");
                if (graph.RequireOpposedNormals && Vector2.Dot(ap.Normal.normalized, bp.Normal.normalized) > -.999f) errors.Add("需要相对边端口。");
                if (graph.RequireWorldAlignment && (!ap.WorldAnchor.HasValue || !bp.WorldAnchor.HasValue || Vector2.Distance(ap.WorldAnchor.Value, bp.WorldAnchor.Value) > graph.WorldTolerance))
                    errors.Add("实际世界锚点未对齐；图上排版不会移动地图。");
            }
            return errors.ToArray();
        }

        /// <summary>仅返回图位置草稿；回环不强行闭合，不写世界坐标，不执行产品加载。</summary>
        public static IReadOnlyDictionary<string, Vector2> Arrange(EmberRoomGraphSnapshot graph, float gap = 72)
        {
            var errors = Validate(graph);
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
            if (!float.IsFinite(gap) || gap < 0) throw new ArgumentException("间距必须为非负有限值。");
            var nodes = graph.Nodes.ToDictionary(n => n.Id); var result = new Dictionary<string, Vector2>(); float componentX = 60;
            foreach (var seed in graph.Nodes)
            {
                if (result.ContainsKey(seed.Id)) continue;
                result[seed.Id] = new Vector2(componentX, 80); var queue = new Queue<EmberRoomGraphNode>(); queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    var source = queue.Dequeue();
                    foreach (var edge in graph.Connections.Where(e => e.FromRoom == source.Id || e.ToRoom == source.Id))
                    {
                        bool forward = edge.FromRoom == source.Id; var target = nodes[forward ? edge.ToRoom : edge.FromRoom];
                        if (result.ContainsKey(target.Id)) continue;
                        var a = source.Ports.Single(p => p.Id == (forward ? edge.FromPort : edge.ToPort));
                        var b = target.Ports.Single(p => p.Id == (forward ? edge.ToPort : edge.FromPort));
                        result[target.Id] = result[source.Id] + a.Anchor * source.GraphSize - b.Anchor * target.GraphSize + a.Normal.normalized * gap;
                        queue.Enqueue(target);
                    }
                }
                componentX = result.Max(p => p.Value.x + nodes[p.Key].GraphSize.x) + 120;
            }
            if (result.Count > 0)
            { var offset = new Vector2(60 - result.Min(p => p.Value.x), 70 - result.Min(p => p.Value.y)); foreach (string id in result.Keys.ToArray()) result[id] += offset; }
            return result;
        }

        /// <summary>可选十二槽预设。只给出图锚点，实际世界锚点由适配器明确提供。</summary>
        public static EmberRoomGraphPort[] TwelveSlotPreset()
        {
            var result = new List<EmberRoomGraphPort>();
            for (int slot = 0; slot < 3; slot++)
            {
                float t = (slot + .5f) / 3;
                result.Add(new EmberRoomGraphPort("Top/" + slot, new Vector2(t, 0), Vector2.down, false));
                result.Add(new EmberRoomGraphPort("Bottom/" + slot, new Vector2(t, 1), Vector2.up, false));
                result.Add(new EmberRoomGraphPort("Left/" + slot, new Vector2(0, t), Vector2.left, false));
                result.Add(new EmberRoomGraphPort("Right/" + slot, new Vector2(1, t), Vector2.right, false));
            }
            return result.ToArray();
        }
        #endregion
    }
}
