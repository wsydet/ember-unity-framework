// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.Collections.Generic;

namespace Ember.Basic
{
    /// <summary>
    /// 基于红黑树的有序列表，类似 C# <see cref="SortedList{TKey, TValue}"/>，但有以下增强：
    ///
    /// 1. 缓存节点，避免 GC 分配
    /// 2. 支持同一 Key 下存储多个 Value
    /// 3. ContainsKey / TryGetValue 为 O(1)（通过内部 Dictionary）
    /// 4. 支持 lower_bound / upper_bound 风格的查找（TryGetGreaterOrEqual / TryGetGreater）
    ///
    /// <h3>Key 类型建议</h3>
    /// 红黑树每一步都要比较 Key 的大小来决定走左边还是右边，所以 Key 必须能比大小。
    /// <b>推荐用 int 或 enum</b>——最常见、最自然、比较零开销。string 也可以但比较稍慢。
    /// 如果 Key 之间不存在大小关系（只能用 Equals 判相等），
    /// 那就不要用这个结构，用 Dictionary 或 HashSet。
    ///
    /// <h3>示例</h3>
    /// <code>
    /// // int Key —— 技能等级表: 1→火球, 5→冰箭, 10→雷击
    /// var skills = new CacheSortedList《int, string》();
    /// skills.Add(1, "火球");
    /// skills.Add(5, "冰箭");
    /// skills.Add(10, "雷击");
    ///
    /// // enum Key —— 品质倍率
    /// var config = new CacheSortedList《Quality, float》();
    /// config.Add(Quality.Common, 1f);
    /// config.Add(Quality.Rare, 1.5f);
    ///
    /// // 同 Key 多值
    /// var list = new CacheSortedList《int, string》();
    /// list.Add(3, "a");
    /// list.Add(3, "b");   // Key=3 下存了 "a" "b" 两个值
    /// </code>
    /// </summary>
    public class CacheSortedList<TKey, TValue>
    {
        private enum NodeColor { Black, Red }

        private class Node
        {
            public TKey Key;
            public TValue Value;
            public List<TValue> Duplicates;

            public Node Left;
            public Node Right;
            public Node Parent;
            public NodeColor Color;

            public Node Uncle()
            {
                if (Parent == null || Parent.Parent == null) return null;
                var grand = Parent.Parent;
                return grand.Left == Parent ? grand.Right : grand.Left;
            }
        }

        private readonly IComparer<TKey> _comparer;
        private readonly Dictionary<TKey, Node> _dict;

        private readonly Stack<Node> _nodePool;
        private readonly Stack<List<TValue>> _dupsPool;

        private Node _root;

        public int Count { get; private set; }

        // ======== 构造 ========

        public CacheSortedList(int capacity = 0) : this(null, capacity) { }

        public CacheSortedList(IComparer<TKey> comparer, int capacity = 0)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;

            if (capacity != 0)
            {
                _nodePool = new Stack<Node>(capacity);
                _dupsPool = new Stack<List<TValue>>(capacity);
                _dict = new Dictionary<TKey, Node>(capacity);
            }
            else
            {
                _nodePool = new Stack<Node>();
                _dupsPool = new Stack<List<TValue>>();
                _dict = new Dictionary<TKey, Node>();
            }
        }

        // ======== 节点池 ========

        private Node PopNodePool() => _nodePool.Count > 0 ? _nodePool.Pop() : new Node();

        private void PushNodePool(Node n)
        {
            n.Key = default;
            n.Value = default;
            if (n.Duplicates != null) PushDupsPool(n.Duplicates);
            n.Left = null;
            n.Right = null;
            n.Parent = null;
            _nodePool.Push(n);
        }

        private List<TValue> PopDupsPool() => _dupsPool.Count > 0 ? _dupsPool.Pop() : new List<TValue>();

        private void PushDupsPool(List<TValue> list)
        {
            list.Clear();
            _dupsPool.Push(list);
        }

        // ======== Add ========

        public void Add(TKey key, TValue value)
        {
            var n = AddNode(key, value);
            if (!_dict.ContainsKey(key))
                _dict.Add(key, n);
        }

        private Node AddNode(TKey key, TValue value)
        {
            Count++;
            Node p = null;
            Node n = _root;

            while (n != null)
            {
                if (KeyEquals(n.Key, key))
                {
                    if (n.Duplicates == null)
                    {
                        n.Duplicates = PopDupsPool();
                        n.Duplicates.Add(n.Value);
                        n.Value = default;
                    }

                    n.Duplicates.Add(value);
                    return n;
                }

                if (KeyGreater(key, n.Key))
                {
                    p = n;
                    n = n.Right;
                }
                else
                {
                    p = n;
                    n = n.Left;
                }
            }

            if (p == null)
            {
                n = PopNodePool();
                n.Key = key;
                n.Value = value;
                _root = n;
            }
            else
            {
                n = PopNodePool();
                n.Parent = p;
                n.Key = key;
                n.Value = value;
                n.Color = NodeColor.Red;

                if (KeyGreater(key, p.Key))
                    p.Right = n;
                else
                    p.Left = n;

                FixInsert(n);
            }

            _root.Color = NodeColor.Black;
            return n;
        }

        // ======== 查询 ========

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue val, List<TValue> values = null)
        {
            if (!_dict.TryGetValue(key, out var n))
            {
                val = default;
                return false;
            }

            val = n.Value;
            if (values != null)
            {
                if (n.Duplicates != null)
                    values.AddRange(n.Duplicates);
                else
                    values.Add(n.Value);
            }

            return true;
        }

        // ======== Remove ========

        public bool Remove(TKey key, TValue value)
        {
            if (!_dict.TryGetValue(key, out var n))
                return false;

            if (n.Duplicates != null)
            {
                for (int i = 0; i < n.Duplicates.Count; i++)
                {
                    if (Comparer<TValue>.Default.Compare(n.Duplicates[i], value) == 0)
                    {
                        Count--;
                        n.Duplicates.RemoveAt(i);

                        if (n.Duplicates.Count == 1)
                        {
                            n.Value = n.Duplicates[0];
                            PushDupsPool(n.Duplicates);
                            n.Duplicates = null;
                        }

                        return true;
                    }
                }
            }
            else
            {
                if (Comparer<TValue>.Default.Compare(n.Value, value) == 0)
                {
                    _dict.Remove(key);
                    RemoveNode(n);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 移除指定 Key。
        /// </summary>
        /// <param name="key">要移除的 Key</param>
        /// <param name="lastOrAll">true = 仅移除最后一个值；false = 移除该 Key 下所有值</param>
        public bool RemoveKey(TKey key, bool lastOrAll = true)
        {
            if (!_dict.TryGetValue(key, out var n))
                return false;

            if (!lastOrAll)
            {
                _dict.Remove(key);
                RemoveNode(n);
            }
            else
            {
                if (n.Duplicates == null)
                {
                    _dict.Remove(key);
                    RemoveNode(n);
                }
                else
                {
                    Count--;
                    n.Duplicates.RemoveAt(n.Duplicates.Count - 1);

                    if (n.Duplicates.Count == 1)
                    {
                        n.Value = n.Duplicates[0];
                        PushDupsPool(n.Duplicates);
                        n.Duplicates = null;
                    }
                }
            }

            return true;
        }

        // ======== 边界查询（lower_bound / upper_bound）=======

        public bool TryGetGreaterOrEqual(TKey key, out KeyValuePair<TKey, TValue> kv, bool remove = false)
        {
            if (_dict.TryGetValue(key, out var nd))
            {
                kv = GetValue(nd, remove);
                return true;
            }

            return TryBoundImpl(key, out kv, equal: true, remove);
        }

        public bool TryGetGreater(TKey key, out KeyValuePair<TKey, TValue> kv, bool remove = false)
        {
            return TryBoundImpl(key, out kv, equal: false, remove);
        }

        public bool TryPopGreaterOrEqual(TKey key, out KeyValuePair<TKey, TValue> kv)
            => TryGetGreaterOrEqual(key, out kv, remove: true);

        public bool TryPopGreater(TKey key, out KeyValuePair<TKey, TValue> kv)
            => TryGetGreater(key, out kv, remove: true);

        private bool TryBoundImpl(TKey key, out KeyValuePair<TKey, TValue> kv, bool equal, bool remove = false)
        {
            kv = default;
            Node selected = null;
            var n = _root;

            while (n != null)
            {
                if (equal && KeyEquals(n.Key, key))
                {
                    selected = n;
                    break;
                }

                if (KeyGreater(n.Key, key))
                {
                    selected = n;
                    n = n.Left;
                }
                else
                {
                    n = n.Right;
                }
            }

            if (selected != null)
            {
                kv = GetValue(selected, remove);
                return true;
            }

            return false;
        }

        private KeyValuePair<TKey, TValue> GetValue(Node n, bool remove)
        {
            TKey key = n.Key;
            TValue value;

            if (n.Duplicates == null)
            {
                value = n.Value;
                if (remove)
                {
                    _dict.Remove(key);
                    RemoveNode(n);
                }
            }
            else
            {
                value = n.Duplicates[n.Duplicates.Count - 1];

                if (remove)
                {
                    Count--;
                    n.Duplicates.RemoveAt(n.Duplicates.Count - 1);

                    if (n.Duplicates.Count == 1)
                    {
                        n.Value = n.Duplicates[0];
                        PushDupsPool(n.Duplicates);
                        n.Duplicates = null;
                    }
                }
            }

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        // ======== 极值 ========

        public KeyValuePair<TKey, TValue> Min
        {
            get
            {
                var n = GetMinNode();
                if (n == null) return new KeyValuePair<TKey, TValue>();
                return new KeyValuePair<TKey, TValue>(n.Key, n.Duplicates != null ? n.Duplicates[0] : n.Value);
            }
        }

        public KeyValuePair<TKey, TValue> Max
        {
            get
            {
                var n = GetMaxNode();
                if (n == null) return new KeyValuePair<TKey, TValue>();
                return new KeyValuePair<TKey, TValue>(n.Key, n.Duplicates != null ? n.Duplicates[0] : n.Value);
            }
        }

        private Node GetMinNode()
        {
            if (_root == null) return null;
            var n = _root;
            while (n.Left != null) n = n.Left;
            return n;
        }

        private Node GetMaxNode()
        {
            if (_root == null) return null;
            var n = _root;
            while (n.Right != null) n = n.Right;
            return n;
        }

        public bool TryLoadMin(out TKey key, List<TValue> loadList)
            => LoadValues(GetMinNode(), out key, loadList);

        public bool TryLoadMax(out TKey key, List<TValue> loadList)
            => LoadValues(GetMaxNode(), out key, loadList);

        private bool LoadValues(Node n, out TKey key, List<TValue> loadList)
        {
            key = default;
            if (n == null) return false;
            if (loadList == null)
                throw new ArgumentNullException(nameof(loadList));

            key = n.Key;
            if (n.Duplicates != null)
                loadList.AddRange(n.Duplicates);
            else
                loadList.Add(n.Value);

            return true;
        }

        // ======== 遍历 ========

        /// <summary>获取按 Key 排序的 Key 列表，O(n)。会产生 GC 分配，不要每帧调用。</summary>
        [HasGC]
        public void GetKeys(List<TKey> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            MorrisTraversal(_root, n => list.Add(n.Key));
        }

        /// <summary>获取按 Key 排序的 Value 列表，O(n)。会产生 GC 分配，不要每帧调用。</summary>
        [HasGC]
        public void GetValues(List<TValue> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            MorrisTraversal(_root, n =>
            {
                if (n.Duplicates != null)
                    list.AddRange(n.Duplicates);
                else
                    list.Add(n.Value);
            });
        }

        [HasGC]
        public ICollection<TKey> Keys
        {
            get
            {
                var list = new List<TKey>(_dict.Count);
                GetKeys(list);
                return list;
            }
        }

        [HasGC]
        public ICollection<TValue> Values
        {
            get
            {
                var list = new List<TValue>(_dict.Count);
                GetValues(list);
                return list;
            }
        }

        // ======== 清理 ========

        public void Clear()
        {
            _root = null;
            Count = 0;

            foreach (var kv in _dict)
                PushNodePool(kv.Value);
            _dict.Clear();
        }

        /// <summary>释放所有缓存的内存。</summary>
        public void FreeCache()
        {
            _dupsPool.Clear();
            _nodePool.Clear();
        }

        // ======== 红黑树操作 ========

        private void FixInsert(Node z)
        {
            var p = z.Parent;
            while (p != null && p.Color == NodeColor.Red && p.Parent != null)
            {
                // case 1: uncle is red
                var u = z.Uncle();
                if (u != null && u.Color == NodeColor.Red)
                {
                    u.Color = NodeColor.Black;
                    z.Parent.Color = NodeColor.Black;
                    z = z.Parent.Parent;
                    p = z.Parent;
                    z.Color = NodeColor.Red;
                    continue;
                }

                // case 2 & 3
                if (p == p.Parent.Left)
                {
                    if (p.Right == z)
                    {
                        RotateLeft(p, ref _root);
                        (z, p) = (p, z);
                    }

                    p.Color = NodeColor.Black;
                    p.Parent.Color = NodeColor.Red;
                    RotateRight(p.Parent, ref _root);
                }
                else
                {
                    if (p.Left == z)
                    {
                        RotateRight(p, ref _root);
                        (z, p) = (p, z);
                    }

                    p.Color = NodeColor.Black;
                    p.Parent.Color = NodeColor.Red;
                    RotateLeft(p.Parent, ref _root);
                }
            }
        }

        private void RemoveNode(Node n)
        {
            Count -= n.Duplicates?.Count ?? 1;

            var deletedColor = n.Color;
            var parent = n.Parent;

            if (n.Left == null && n.Right == null)
            {
                if (n.Parent != null)
                {
                    if (n.Parent.Left == n) n.Parent.Left = null;
                    else n.Parent.Right = null;
                }
                else
                    _root = null;

                PushNodePool(n);
                if (deletedColor == NodeColor.Black)
                    FixRemove(null, parent);
            }
            else
            {
                Node x;
                if (n.Left == null)
                {
                    x = n.Right;
                    Relink(n, n.Right, ref _root);
                    PushNodePool(n);
                }
                else if (n.Right == null)
                {
                    x = n.Left;
                    Relink(n, n.Left, ref _root);
                    PushNodePool(n);
                }
                else
                {
                    Node sc = GetSuccessor(n);
                    deletedColor = sc.Color;
                    parent = sc.Parent;

                    if (sc.Left != null)
                        throw new InvalidOperationException(
                            $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: successor has left child.");

                    if (sc.Right != null)
                    {
                        x = sc.Right;
                        Relink(sc, sc.Right, ref _root);
                    }
                    else
                    {
                        if (sc.Parent.Left == sc) sc.Parent.Left = null;
                        else sc.Parent.Right = null;
                        x = null;
                    }

                    n.Key = sc.Key;
                    n.Value = sc.Value;
                    (sc.Duplicates, n.Duplicates) = (n.Duplicates, sc.Duplicates);
                    _dict[n.Key] = n;
                    PushNodePool(sc);
                }

                if (deletedColor == NodeColor.Black)
                    FixRemove(x, parent);
            }
        }

        private void FixRemove(Node x, Node p)
        {
            while (p != null && x != _root && (x == null || x.Color == NodeColor.Black))
            {
                var w = GetSibling(x, p);
                if (w == null)
                    throw new InvalidOperationException(
                        $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: null sibling in FixRemove.");

                if (p.Left == x)
                {
                    FixRemoveLeft(ref x, ref p, w);
                }
                else
                {
                    FixRemoveRight(ref x, ref p, w);
                }
            }

            if (x != null) x.Color = NodeColor.Black;
        }

        private void FixRemoveLeft(ref Node x, ref Node p, Node w)
        {
            if (w.Color == NodeColor.Red)
            {
                w.Color = NodeColor.Black;
                p.Color = NodeColor.Red;
                RotateLeft(p, ref _root);
                w = p.Right;
            }

            if (w == null)
                throw new InvalidOperationException(
                    $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: null sibling.");

            if ((w.Left == null || w.Left.Color == NodeColor.Black)
                && (w.Right == null || w.Right.Color == NodeColor.Black))
            {
                w.Color = NodeColor.Red;
                x = p;
                p = x.Parent;
                return;
            }

            if (w.Left != null && w.Left.Color == NodeColor.Red
                && (w.Right == null || w.Right.Color == NodeColor.Black))
            {
                w.Left.Color = NodeColor.Black;
                w.Color = NodeColor.Red;
                RotateRight(w, ref _root);
                w = w.Parent;
            }

            if (w == null)
                throw new InvalidOperationException(
                    $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: null sibling after rotation.");

            w.Color = p.Color;
            if (w.Right != null) w.Right.Color = NodeColor.Black;
            p.Color = NodeColor.Black;
            RotateLeft(p, ref _root);
            x = _root;
        }

        private void FixRemoveRight(ref Node x, ref Node p, Node w)
        {
            if (w.Color == NodeColor.Red)
            {
                w.Color = NodeColor.Black;
                p.Color = NodeColor.Red;
                RotateRight(p, ref _root);
                w = p.Left;
            }

            if (w == null)
                throw new InvalidOperationException(
                    $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: null sibling.");

            if ((w.Left == null || w.Left.Color == NodeColor.Black)
                && (w.Right == null || w.Right.Color == NodeColor.Black))
            {
                w.Color = NodeColor.Red;
                x = p;
                p = x.Parent;
                return;
            }

            if (w.Right != null && w.Right.Color == NodeColor.Red
                && (w.Left == null || w.Left.Color == NodeColor.Black))
            {
                w.Right.Color = NodeColor.Black;
                w.Color = NodeColor.Red;
                RotateLeft(w, ref _root);
                w = w.Parent;
            }

            if (w == null)
                throw new InvalidOperationException(
                    $"[Ember] {nameof(CacheSortedList<TKey, TValue>)} internal error: null sibling after rotation.");

            w.Color = p.Color;
            if (w.Left != null) w.Left.Color = NodeColor.Black;
            p.Color = NodeColor.Black;
            RotateRight(p, ref _root);
            x = _root;
        }

        // ======== 红黑树旋转 ========

        /// <summary>
        ///        n               m
        ///      /   \           /   \
        ///     m     c   →     a     n
        ///   /   \                  / \
        ///  a     b                b   c
        /// </summary>
        private static void RotateRight(Node n, ref Node root)
        {
            if (n.Left == null) return;

            var m = n.Left;
            var a = m.Left;
            var b = m.Right;
            var c = n.Right;
            var p = n.Parent;

            if (p != null)
            {
                if (p.Left == n) p.Left = m;
                else p.Right = m;
            }
            else
            {
                root = m;
            }

            m.Left = a;
            m.Right = n;
            n.Left = b;
            n.Right = c;

            n.Parent = m;
            m.Parent = p;

            if (a != null) a.Parent = m;
            if (b != null) b.Parent = n;
            if (c != null) c.Parent = n;
        }

        /// <summary>
        ///      m                 n
        ///    /   \             /   \
        ///   a     n     →     m     c
        ///        / \         / \
        ///       b   c       a   b
        /// </summary>
        private static void RotateLeft(Node m, ref Node root)
        {
            if (m.Right == null) return;

            var n = m.Right;
            var a = m.Left;
            var b = n.Left;
            var c = n.Right;
            var p = m.Parent;

            if (p != null)
            {
                if (p.Left == m) p.Left = n;
                else p.Right = n;
            }
            else
            {
                root = n;
            }

            n.Left = m;
            n.Right = c;
            m.Left = a;
            m.Right = b;

            m.Parent = n;
            n.Parent = p;

            if (a != null) a.Parent = m;
            if (b != null) b.Parent = m;
            if (c != null) c.Parent = n;
        }

        // ======== 辅助 ========

        private static void Relink(Node n, Node c, ref Node root)
        {
            if (n.Parent != null)
            {
                if (n.Parent.Left == n)
                {
                    n.Parent.Left = c;
                    c.Parent = n.Parent;
                }
                else
                {
                    n.Parent.Right = c;
                    c.Parent = n.Parent;
                }
            }
            else
            {
                root = c;
                root.Parent = null;
            }
        }

        private static Node GetMin(Node n)
        {
            while (n.Left != null) n = n.Left;
            return n;
        }

        private static Node GetSuccessor(Node n)
        {
            if (n.Right != null) return GetMin(n.Right);
            Node p = n.Parent;
            while (p != null && p.Right == n)
            {
                n = p;
                p = p.Parent;
            }

            return p;
        }

        private static Node GetSibling(Node n, Node parent)
        {
            if (parent == null) return null;
            return parent.Left == n ? parent.Right : parent.Left;
        }

        private bool KeyEquals(TKey a, TKey b) => _comparer.Compare(a, b) == 0;
        private bool KeyGreater(TKey a, TKey b) => _comparer.Compare(a, b) > 0;

        // ======== Morris 遍历（无栈中序遍历）=======

        private static void MorrisTraversal(Node root, Action<Node> action)
        {
            Node current = root;
            while (current != null)
            {
                if (current.Left == null)
                {
                    action(current);
                    current = current.Right;
                }
                else
                {
                    var pre = current.Left;
                    while (pre.Right != null && pre.Right != current)
                        pre = pre.Right;

                    if (pre.Right == null)
                    {
                        pre.Right = current;
                        current = current.Left;
                    }
                    else
                    {
                        pre.Right = null;
                        action(current);
                        current = current.Right;
                    }
                }
            }
        }

        // ======== 红黑树自检（仅测试用）=======

        [ForTest]
        public void VerifyRBTree()
        {
            if (_root == null) return;
            if (_root.Color == NodeColor.Red)
                throw new InvalidOperationException("[Ember] RBTree root must be black.");

            VerifyRedNodes(_root);

            // Each leaf path must have the same number of black nodes
            var stack = new Stack<Node>(Count);
            int formerBlackNodes = -1;

            void Traversal(Node n)
            {
                if (n == null)
                {
                    int b = 0;
                    foreach (var node in stack)
                    {
                        if (node.Color == NodeColor.Black) b++;
                    }

                    if (formerBlackNodes == -1) formerBlackNodes = b;
                    if (formerBlackNodes != b)
                        throw new InvalidOperationException("[Ember] RBTree black node count mismatch.");

                    return;
                }

                stack.Push(n);
                Traversal(n.Left);
                Traversal(n.Right);
                stack.Pop();
            }

            Traversal(_root);
        }

        private static void VerifyRedNodes(Node n)
        {
            if (n == null) return;
            if (n.Color == NodeColor.Red)
            {
                if (n.Left != null && n.Left.Color == NodeColor.Red)
                    throw new InvalidOperationException("[Ember] RBTree red node has red left child.");
                if (n.Right != null && n.Right.Color == NodeColor.Red)
                    throw new InvalidOperationException("[Ember] RBTree red node has red right child.");
            }

            VerifyRedNodes(n.Left);
            VerifyRedNodes(n.Right);
        }
    }
}
