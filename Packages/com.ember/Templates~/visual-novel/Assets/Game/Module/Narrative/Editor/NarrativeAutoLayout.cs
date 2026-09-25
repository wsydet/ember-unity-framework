using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>按强连通分量分层，环路不会使排布无限迭代。只返回坐标，不修改剧情。</summary>
    public static class NarrativeAutoLayout
    {
        #region 内部参数
        public const float GRID = 20;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static Vector2 Snap(Vector2 position) => new(Mathf.Round(position.x / GRID) * GRID, Mathf.Round(position.y / GRID) * GRID);

        public static Dictionary<NarrativeNodeSO, Vector2> Calculate(NarrativeChapterSO chapter)
        {
            var nodes = chapter.Nodes.Where(n => n).Distinct().ToList();
            var members = new HashSet<NarrativeNodeSO>(nodes);
            var links = new Dictionary<NarrativeNodeSO, List<NarrativeNodeSO>>();
            foreach (var node in nodes)
                links[node] = NarrativeGraphModel.Ports(node).Select(p => NarrativeGraphModel.Target(node, p))
                    .Where(n => n && members.Contains(n)).Distinct().ToList();
            var indices = new Dictionary<NarrativeNodeSO, int>();
            var low = new Dictionary<NarrativeNodeSO, int>();
            var active = new HashSet<NarrativeNodeSO>();
            var stack = new Stack<NarrativeNodeSO>();
            var component = new Dictionary<NarrativeNodeSO, int>();
            int index = 0, count = 0;
            void Visit(NarrativeNodeSO node)
            {
                indices[node] = low[node] = index++; stack.Push(node); active.Add(node);
                foreach (var target in links[node])
                {
                    if (!indices.ContainsKey(target)) { Visit(target); low[node] = Math.Min(low[node], low[target]); }
                    else if (active.Contains(target)) low[node] = Math.Min(low[node], indices[target]);
                }
                if (low[node] != indices[node]) return;
                NarrativeNodeSO item;
                do { item = stack.Pop(); active.Remove(item); component[item] = count; } while (item != node);
                count++;
            }
            if (chapter.Entry && members.Contains(chapter.Entry)) Visit(chapter.Entry);
            foreach (var node in nodes) if (!indices.ContainsKey(node)) Visit(node);
            var outgoing = new HashSet<int>[count]; var incoming = new int[count]; var ranks = new int[count];
            for (int i = 0; i < count; i++) outgoing[i] = new HashSet<int>();
            foreach (var node in nodes)
                foreach (var target in links[node])
                    if (component[node] != component[target] && outgoing[component[node]].Add(component[target])) incoming[component[target]]++;
            var queue = new Queue<int>();
            for (int i = 0; i < count; i++) if (incoming[i] == 0) queue.Enqueue(i);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in outgoing[current])
                {
                    ranks[next] = Math.Max(ranks[next], ranks[current] + 1);
                    if (--incoming[next] == 0) queue.Enqueue(next);
                }
            }
            var heights = new Dictionary<int, float>();
            var result = new Dictionary<NarrativeNodeSO, Vector2>();
            foreach (var node in nodes)
            {
                int rank = ranks[component[node]]; heights.TryGetValue(rank, out float y);
                result[node] = new Vector2(rank * 360, y);
                heights[rank] = y + Mathf.Ceil(Mathf.Max(160, 110 + NarrativeGraphModel.Ports(node).Count * 26) / GRID) * GRID + 80;
            }
            return result;
        }
        #endregion
    }
}
