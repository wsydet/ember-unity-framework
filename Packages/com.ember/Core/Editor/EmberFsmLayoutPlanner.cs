using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>纯布局候选计算；不访问资产、EditorPrefs 或画布。</summary>
    internal sealed class EmberFsmLayoutPlanner
    {
        #region 内部参数
        internal sealed class Item
        {
            internal string Id, Parent;
            internal Vector2 Position;
            internal float Header, Left, Right;
            internal bool Collapsed;
        }
        internal readonly Dictionary<string, Vector2> Positions;
        internal readonly Dictionary<string, Vector2> Sizes = new();
        internal readonly List<string> Conflicts = new();
        private readonly Dictionary<string, Item> _items;
        private readonly HashSet<string> _scope, _locked;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private IEnumerable<string> Children(string id) => _items.Values.Where(n => n.Parent == id).Select(n => n.Id);
        private bool Descendant(string id, string parent)
        {
            while (_items.TryGetValue(id, out var item) && item.Parent != null)
            { id = item.Parent; if (id == parent) return true; }
            return false;
        }
        private int Depth(string id)
        { int depth = 0; while (_items[id].Parent != null) { id = _items[id].Parent; depth++; } return depth; }
        private IEnumerable<string> Branch(string id) => _items.Keys.Where(k => k == id || Descendant(k, id));
        private bool CanMove(string id) => Branch(id).All(k => _scope.Contains(k) && !_locked.Contains(k));
        private void Move(string id, Vector2 delta)
        { foreach (var child in Branch(id)) Positions[child] += delta; }
        private void Fit(string id)
        {
            var item = _items[id];
            Sizes[id] = new Vector2(320, item.Header);
            var children = Children(id).ToArray();
            if (children.Length == 0 || item.Collapsed) return;
            var bounds = children.Select(Bounds).ToArray();
            float left = bounds.Min(r => r.xMin) - 30;
            float top = bounds.Min(r => r.yMin) - item.Header - 30;
            Positions[id] = new Vector2(left, top);
            Sizes[id] = new Vector2(Math.Max(320, bounds.Max(r => r.xMax) + 30 - left), bounds.Max(r => r.yMax) + 30 - top);
        }
        private bool Visible(string id)
        {
            while (_items[id].Parent != null)
            { id = _items[id].Parent; if (_items[id].Collapsed) return false; }
            return true;
        }
        private void Separate(string[] siblings, bool arrange, Vector2 origin)
        {
            // 固定分支先作为障碍；其余保留当前顺序，稳定 ID 只用于消除同坐标歧义。
            var placed = siblings.Where(id => !CanMove(id)).ToList();
            bool vertical = siblings.All(id => !Children(id).Any());
            var cursor = origin;
            // 混合同级先放复合分支，短叶子集中竖排在最后一列，避免一整行短卡片下方的大块空白。
            foreach (var id in siblings.Where(CanMove).OrderBy(id => arrange && !vertical && !Children(id).Any() ? 1 : 0)
                .ThenBy(id => Positions[id].y)
                .ThenBy(id => Positions[id].x).ThenBy(id => id, StringComparer.Ordinal))
            {
                if (arrange) Move(id, cursor - Bounds(id).position);
                for (int pass = 0; pass <= placed.Count; pass++)
                {
                    var bounds = Bounds(id);
                    var hits = placed.Where(other => Bounds(other).Overlaps(bounds)).ToArray();
                    if (hits.Length == 0) break;
                    Move(id, new Vector2(0, hits.Max(other => Bounds(other).yMax) + 30 - bounds.yMin));
                }
                placed.Add(id);
                if (arrange)
                {
                    if (vertical || !Children(id).Any()) cursor.y = Bounds(id).yMax + 30;
                    else { cursor.x = Bounds(id).xMax + 50; cursor.y = origin.y; }
                }
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmLayoutPlanner(IEnumerable<Item> items, HashSet<string> scope, HashSet<string> pinned)
        {
            _items = items.ToDictionary(n => n.Id);
            _scope = scope;
            _locked = new HashSet<string>(_items.Keys.Where(id => pinned.Any(p => p == id || Descendant(id, p))));
            Positions = _items.ToDictionary(p => p.Key, p => p.Value.Position);
            foreach (var id in _items.Keys.OrderByDescending(Depth)) Fit(id);
        }
        internal Rect Bounds(string id)
        {
            var item = _items[id]; var size = Sizes[id];
            return new Rect(Positions[id].x - item.Left, Positions[id].y,
                size.x + item.Left + item.Right, Math.Max(size.y, item.Header));
        }
        internal void Calculate(bool arrange, bool branch)
        {
            foreach (var id in _items.Keys.OrderByDescending(Depth).ThenBy(k => k, StringComparer.Ordinal))
            {
                if (!Visible(id) || _items[id].Collapsed) continue;
                var children = Children(id).ToArray();
                if (children.Length == 0) continue;
                var anchor = Positions[id];
                if (!branch || _scope.Contains(id))
                    Separate(children, arrange && _scope.Contains(id), anchor + new Vector2(30, _items[id].Header + 30));
                Fit(id);
                // 局部分支保留其根锚点，避免父框拟合把范围外祖先一起带走。
                if (branch && _scope.Contains(id) && (_items[id].Parent == null || !_scope.Contains(_items[id].Parent))
                    && CanMove(id)) Move(id, _items[id].Position - Positions[id]);
            }
            var roots = _items.Keys.Where(id => _items[id].Parent == null).ToArray();
            if (roots.Length > 0 && !branch)
                Separate(roots, arrange, new Vector2(roots.Min(id => Bounds(id).xMin), roots.Min(id => Bounds(id).yMin)));
            foreach (var id in _items.Keys)
                if ((!_scope.Contains(id) || _locked.Contains(id)) && Positions[id] != _items[id].Position)
                    Conflicts.Add(id + "：拟合父框会移动固定节点或范围外节点。");
            foreach (var group in _items.Keys.Where(Visible).GroupBy(id => _items[id].Parent ?? ""))
            {
                var ids = group.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < ids.Length; i++)
                    for (int j = i + 1; j < ids.Length; j++)
                        if (Bounds(ids[i]).Overlaps(Bounds(ids[j])) && (!branch || _scope.Contains(ids[i]) || _scope.Contains(ids[j])))
                            Conflicts.Add(ids[i] + " / " + ids[j] + "：完整显示范围仍重叠；请调整范围或固定约束。");
            }
        }
        #endregion
    }
}
