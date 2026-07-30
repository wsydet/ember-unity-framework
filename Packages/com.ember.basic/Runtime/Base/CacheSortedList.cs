//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//namespace Burner.Basic
//{
//    using System;
//    using System.Collections.Generic;
//
//    /// <summary>
//    /// binary search tree list (BST) which is similar with C# SortedList, but has some extra special features:
//    /// 1, cached node, avoid GC.Alloc
//    /// 2, multi-values, can add multi-values with same key
//    /// 3, O(1) time for ContainsKey, TryGetValue, better than O(lgn) in SortedList
//    /// 4, find greater or equal key, same as C++ lower_bound / upper_bound, O(lgn) time
//    ///
//    /// - multi-values example:
//    ///
//    ///     var list = new CacheSortedList<int,string>();
//    ///     list.Add(1,"1");
//    ///     list.Add(2,"2");
//    ///     list.Add(3,"3");
//    ///
//    ///     Assert.AreEqual(new List<int>(){1,2,3}, list.Keys);
//    ///     Assert.AreEqual(new List<string>(){"1","2","3"}, list.Values);
//    ///
//    ///     list.Add(3,"33"); // add to
//    ///     list.Add(3,"333");
//    ///     list.Add(4,"4");
//    ///
//    ///     Assert.AreEqual(new List<int>(){1,2,3,4}, list.Keys);
//    ///     Assert.AreEqual(new List<string>(){"1","2","3", "33", "333", "4"}, list.Values);
//    ///
//    ///     bool result = list.Remove(3,"???");
//    ///     Assert.IsFalse(result);
//    ///
//    ///     bool result = list.Remove(3,"33");
//    ///     Assert.IsTrue(result);
//    ///
//    ///     Assert.AreEqual(new List<int>(){1,2,3,4}, list.Keys);
//    ///     Assert.AreEqual(new List<string>(){"1","2","3", "333", "4"}, list.Values);
//    ///
//    ///     bool result = list.RemoveKey(3, false); // remove all in the second parameter "false"
//    ///
//    ///     Assert.AreEqual(new List<int>(){1,2,4}, list.Keys);
//    ///     Assert.AreEqual(new List<string>(){"1","2", "4"}, list.Values);
//    ///
//    /// ---- find greater or equal key example:
//    ///
//    ///     var list = new CacheSortedList<int,string>();
//    ///     list.Add(1,"1");
//    ///     list.Add(2,"2");
//    ///     list.Add(4,"4");
//    ///     list.Add(8,"8");
//    ///     list.Add(16,"16");
//    ///
//    ///     KeyValuePair<int, string> kv;
//    ///     bool ret = list.TryGetGreaterOrEqual(1, out kv);
//    ///     Assert.True(ret);
//    ///     Assert.AreEqual(1, kv.Key);
//    ///     Assert.AreEqual("1", kv.Value);
//    ///
//    ///     ret = list.TryGetGreater(1, out kv);
//    ///     Assert.True(ret);
//    ///     Assert.AreEqual(2, kv.Key);
//    ///     Assert.AreEqual("2", kv.Value);
//    ///
//    ///     ret = list.TryGetGreater(12, out kv);
//    ///     Assert.True(ret);
//    ///     Assert.AreEqual(16, kv.Key);
//    ///     Assert.AreEqual("16", kv.Value);
//    ///
//    ///     ret = list.TryGetGreater(16, out kv);
//    ///     Assert.False(ret);
//    ///
//    /// </summary>
//    public class CacheSortedList<TKey, TValue>
//    {
//        private enum NodeColor
//        {
//            BLACK, RED
//        }
//
//        class Node
//        {
//            public TKey key;
//            public TValue value;
//            public List<TValue> dups;
//
//            public Node left;
//            public Node right;
//            public Node parent;
//            public NodeColor color;
//
//            public Node Uncle()
//            {
//                if(parent == null || parent.parent == null) return null;
//
//                var grand = parent.parent;
//                return grand.left == parent ? grand.right : grand.left;
//            }
//
//
//
//        }
//
//        readonly IComparer<TKey> _comparer;
//        readonly Dictionary<TKey, Node> _dict;
//
//        readonly Stack<Node> _nodePool;
//        Node PopNodePool()
//        {
//            if(_nodePool.Count > 0)
//            {
//                return _nodePool.Pop();
//            }
//            return new Node();
//        }
//
//        void PushNodePool(Node n)
//        {
//            n.key = default;
//            n.value = default;
//            if(n.dups != null) PushDupsPool(n.dups);
//
//            n.left = null;
//            n.right = null;
//            n.parent = null;
//
//            _nodePool.Push(n);
//        }
//
//        readonly Stack<List<TValue>> _dupsPool;
//        List<TValue> PopDupsPool()
//        {
//            if(_dupsPool.Count > 0)
//            {
//                return _dupsPool.Pop();
//            }
//
//            return new List<TValue>();
//        }
//
//        void PushDupsPool(List<TValue> q)
//        {
//            q.Clear();
//            _dupsPool.Push(q);
//        }
//
//        Node _root = null;
//        public int Count { get; private set; }
//
//        public CacheSortedList(int capacity = 0) : this(null, capacity)
//        {
//
//        }
//
//        public CacheSortedList(IComparer<TKey> comparer, int capacity = 0)
//        {
//            _comparer = comparer ?? Comparer<TKey>.Default;
//
//            if(capacity != 0)
//            {
//                _nodePool = new Stack<Node>(capacity);
//                _dupsPool = new Stack<List<TValue>>(capacity);
//                _dict = new Dictionary<TKey, Node>(capacity);
//            }
//            else
//            {
//                _nodePool = new Stack<Node>();
//                _dupsPool = new Stack<List<TValue>>();
//                _dict = new Dictionary<TKey, Node>();
//            }
//        }
//
//        public void Add(TKey key, TValue value)
//        {
//            var n = AddNode(key, value);
//
//            if(!_dict.ContainsKey(key))
//            {
//                _dict.Add(key, n);
//            }
//        }
//
//        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
//
//        public bool TryGetValue(TKey key, out TValue val, List<TValue> values = null)
//        {
//            if(!_dict.TryGetValue(key, out var n))
//            {
//                val = default;
//                return false;
//            }
//
//            val = n.value;
//            if(values != null)
//            {
//                if(n.dups != null)
//                {
//                    values.AddRange(n.dups);
//                }
//                else
//                {
//                    values.Add(n.value);
//                }
//            }
//
//            return true;
//        }
//
//        public bool Remove(TKey key, TValue value)
//        {
//            if(!_dict.TryGetValue(key, out var n))
//            {
//                return false;
//            }
//
//            if(n.dups != null)
//            {
//                for(int i = 0; i < n.dups.Count; i++)
//                {
//                    if(Comparer<TValue>.Default.Compare(n.dups[i], value) == 0)
//                    {
//                        Count--;
//                        n.dups.RemoveAt(i);
//
//                        if(n.dups.Count == 1)
//                        {
//                            n.value = n.dups[0];
//                            PushDupsPool(n.dups);
//                            n.dups = null;
//                        }
//
//                        return true;
//                    }
//                }
//            }
//            else
//            {
//                if(Comparer<TValue>.Default.Compare(n.value, value) == 0)
//                {
//                    _dict.Remove(key);
//                    RemoveNode(n);
//
//                    return true;
//                }
//            }
//
//            return false;
//        }
//
//        public bool RemoveKey(TKey key, bool lastOrAll = true)
//        {
//            if(!_dict.TryGetValue(key, out var n))
//            {
//                return false;
//            }
//
//            if(!lastOrAll)
//            {
//                _dict.Remove(key);
//                RemoveNode(n);
//            }
//            else
//            {
//                if(n.dups == null)
//                {
//                    _dict.Remove(key);
//                    RemoveNode(n);
//                }
//                else
//                {
//                    Count--;
//
//                    n.dups.RemoveAt(n.dups.Count - 1); // remove last one
//
//                    if(n.dups.Count == 1)
//                    {
//                        n.value = n.dups[0];
//                        PushDupsPool(n.dups);
//                        n.dups = null;
//                    }
//                }
//            }
//
//            return true;
//        }
//
//        public void Clear()
//        {
//            _root = null;
//            Count = 0;
//
//            foreach(var kv in _dict) PushNodePool(kv.Value);
//            _dict.Clear();
//        }
//
//        /// <summary>
//        /// free all cache memory
//        /// </summary>
//        public void FreeCache()
//        {
//            _dupsPool.Clear();
//            _nodePool.Clear();
//        }
//
//        /// <summary>
//        /// load the keys list in sorted, O(n) time complex
//        /// it costs a lot, please don't it in production(release) runtime every frame
//        /// </summary>
//        [HasGC("Has a little GC.Alloc becuase of creating delegate Action<T>")]
//        public void GetKeys(List<TKey> list)
//        {
//            if(list == null) throw new ArgumentNullException();
//            MorrisTraversal(_root, n => list.Add(n.key));
//        }
//
//        /// <summary>
//        /// load the values by sorted keys order, O(n) time complex
//        /// it costs a lot, please don't it in production(release) runtime every frame
//        /// </summary>
//        /// <param name="list"></param>
//        [HasGC("Has a little GC.Alloc becuase of creating delegate Action<T>")]
//        public void GetValues(List<TValue> list)
//        {
//            if(list == null) throw new ArgumentNullException();
//            MorrisTraversal(_root, n =>
//            {
//                if(n.dups != null)
//                {
//                    list.AddRange(n.dups);
//                }
//                else
//                {
//                    list.Add(n.value);
//                }
//            });
//        }
//
//
//        /// <summary>
//        /// get the keys list in sorted, O(n) time complex
//        /// it costs a lot, please don't it in production(release) runtime every frame
//        /// </summary>
//        [HasGC]
//        public ICollection<TKey> Keys
//        {
//            get
//            {
//                var list = new List<TKey>(_dict.Count);
//                GetKeys(list);
//                return list;
//            }
//        }
//
//        /// <summary>
//        /// load the values by sorted keys order, O(n) time complex
//        /// it costs a lot, please don't it in production(release) runtime every frame
//        /// </summary>
//        [HasGC]
//        public ICollection<TValue> Values
//        {
//            get
//            {
//                var list = new List<TValue>(_dict.Count);
//                GetValues(list);
//                return list;
//            }
//        }
//
//        /// <summary>
//        /// get the min key and one of its values
//        /// if it is empty, it will return default KeyValuePair of TKey and TValue
//        /// </summary>
//        public KeyValuePair<TKey, TValue> Min
//        {
//            get
//            {
//                var n = GetMinNode();
//                if(n == null) return new KeyValuePair<TKey, TValue>();
//                return new KeyValuePair<TKey, TValue>(n.key, n.dups.IsNullOrEmpty() ? n.value : n.dups[0]);
//            }
//        }
//
//        /// <summary>
//        /// get the max key and one of its values
//        /// if it is empty, it will return default KeyValuePair of TKey and TValue
//        /// </summary>
//        public KeyValuePair<TKey, TValue> Max
//        {
//            get
//            {
//                var n = GetMaxNode();
//                if(n == null) return new KeyValuePair<TKey, TValue>();
//                return new KeyValuePair<TKey, TValue>(n.key, n.dups.IsNullOrEmpty() ? n.value : n.dups[0]);
//            }
//        }
//
//        private Node GetMinNode()
//        {
//            if(_root == null) return null;
//
//            var n = _root;
//            while(n.left != null)
//            {
//                n = n.left;
//            }
//
//            return n;
//        }
//
//        private Node GetMaxNode()
//        {
//            if(_root == null) return null;
//
//            var n = _root;
//            while(n.right != null)
//            {
//                n = n.right;
//            }
//
//            return n;
//        }
//
//        private bool LoadValues(Node n, out TKey key, List<TValue> loadList)
//        {
//            key = default;
//            if(n == null) return false;
//
//            if(loadList == null)
//            {
//                throw new ArgumentNullException("[Burner]: loadlist cannot be null");
//            }
//
//            key = n.key;
//            if(!n.dups.IsNullOrEmpty())
//            {
//                loadList.AddRange(n.dups);
//            }
//            else
//            {
//                loadList.Add(n.value);
//            }
//
//            return true;
//        }
//
//        public bool TryLoadMin(out TKey key, List<TValue> loadList) => LoadValues(GetMinNode(), out key, loadList);
//        public bool TryLoadMax(out TKey key, List<TValue> loadList) => LoadValues(GetMaxNode(), out key, loadList);
//
//        public bool TryGetGreaterOrEqual(TKey key, out KeyValuePair<TKey,TValue> kv, bool remove = false)
//        {
//            if(_dict.TryGetValue(key, out var nd))
//            {
//                kv = GetValue(nd, remove);
//                return true;
//            }
//
//            return TryBoundImpl(key, out kv, true, remove);
//        }
//
//        public bool TryGetGreater(TKey key, out KeyValuePair<TKey, TValue> kv, bool remove = false)
//        {
//            return TryBoundImpl(key, out kv, false, remove);
//        }
//
//        private bool TryBoundImpl(TKey key, out KeyValuePair<TKey, TValue> kv, bool equal, bool remove = false)
//        {
//            kv = default;
//
//            Node selected = null;
//            var n = _root;
//            while(n != null)
//            {
//                if(equal && KeyEquals(n.key, key))
//                {
//                    selected = n;
//                    break;
//                }
//
//                if(KeyGreater(n.key, key))
//                {
//                    selected = n;
//                    n = n.left;
//                }
//                else
//                {
//                    n = n.right;
//                }
//            }
//
//            if(selected != null)
//            {
//                kv = GetValue(selected, remove);
//                return true;
//            }
//
//            return false;
//        }
//
//        public bool TryPopGreaterOrEqual(TKey key, out KeyValuePair<TKey, TValue> kv) => TryGetGreaterOrEqual(key, out kv, true);
//        public bool TryPopGreater(TKey key, out KeyValuePair<TKey, TValue> kv) => TryGetGreater(key, out kv, true);
//
//        private bool KeyEquals(TKey a, TKey b) => _comparer.Compare(a, b) == 0;
//        private bool KeyGreater(TKey a, TKey b) => _comparer.Compare(a, b) > 0;
//        private bool KeyGreaterOrEquals(TKey a, TKey b) => _comparer.Compare(a, b) >= 0;
//
//        private KeyValuePair<TKey,TValue> GetValue(Node n, bool remove = true)
//        {
//            TKey key = n.key;
//            TValue value;
//            if(n.dups == null)
//            {
//                value = n.value;
//                if(remove)
//                {
//                    _dict.Remove(key);
//                    RemoveNode(n);
//                }
//            }
//            else
//            {
//                value = n.dups[n.dups.Count - 1];
//
//                if(remove)
//                {
//                    Count--;
//
//                    n.dups.RemoveAt(n.dups.Count - 1);
//
//                    if(n.dups.Count == 1)
//                    {
//                        n.value = n.dups[0];
//                        PushDupsPool(n.dups);
//                        n.dups = null;
//                    }
//                }
//            }
//
//            return new KeyValuePair<TKey, TValue>(key, value);
//        }
//
//        private Node AddNode(TKey key, TValue value)
//        {
//            Count++;
//
//            Node p = null;
//            Node n = _root;
//            while(n != null)
//            {
//                if(KeyEquals(n.key, key))
//                {
//                    if(n.dups == null)
//                    {
//                        n.dups = PopDupsPool();
//                        n.dups.Add(n.value);
//                        n.value = default;
//                    }
//
//                    n.dups.Add(value);
//
//                    return n;
//                }
//
//                if(KeyGreater(key, n.key))
//                {
//                    p = n;
//                    n = n.right;
//                }
//                else
//                {
//                    p = n;
//                    n = n.left;
//                }
//            }
//
//            if(p == null) // parent == null
//            {
//                n = PopNodePool();
//                n.key = key;
//                n.value = value;
//
//                _root = n;
//            }
//            else
//            {
//                n = PopNodePool();
//                n.parent = p;
//                n.key = key;
//                n.value = value;
//                n.color = NodeColor.RED;
//
//                if(KeyGreater(key, p.key))
//                {
//                    p.right = n;
//                }
//                else
//                {
//                    p.left = n;
//                }
//
//                RBAddFixUp(n);
//            }
//
//            _root.color = NodeColor.BLACK;
//
//            return n;
//        }
//
//        private void RBAddFixUp(Node z)
//        {
//            var p = z.parent;
//            while(p != null
//                  && p.color == NodeColor.RED
//                  && p.parent != null) // must have a grandparent
//            {
//                // case 1
//                var u = z.Uncle();
//                if(u != null && u.color == NodeColor.RED)
//                {
//                    u.color = NodeColor.BLACK;
//                    z.parent.color = NodeColor.BLACK;
//                    z = z.parent.parent;
//                    p = z.parent;
//                    z.color = NodeColor.RED;
//                    continue;
//                }
//
//                // case 2
//                if(p == p.parent.left)
//                {
//                    if(p.right == z)
//                    {
//                        RotateLeft(p, ref _root);
//                        (z, p) = (p, z);
//                    }
//
//                    // case 3
//                    p.color = NodeColor.BLACK;
//                    p.parent.color = NodeColor.RED;
//                    RotateRight(p.parent, ref _root);
//                }
//                else
//                {
//                    if(p.left == z)
//                    {
//                        RotateRight(p, ref _root);
//                        (z, p) = (p, z);
//                    }
//
//                    // case 3
//                    p.color = NodeColor.BLACK;
//                    p.parent.color = NodeColor.RED;
//                    RotateLeft(p.parent, ref _root);
//                }
//            }
//        }
//
//        private void RemoveNode(Node n)
//        {
//            Count -= (n.dups == null) ? 1 : n.dups.Count;
//
//            var deletedColor = n.color;
//            var parent = n.parent;
//
//            if(n.left == null && n.right == null)
//            {
//                // leaf node
//
//                if(n.parent != null)
//                {
//                    if(n.parent.left == n) n.parent.left = null;
//                    else n.parent.right = null;
//                }
//                else
//                {
//                    _root = null;
//                }
//
//                PushNodePool(n);
//
//                if(deletedColor == NodeColor.BLACK)
//                {
//                    RBRemoveFixUp(null, parent);
//                }
//            }
//            else
//            {
//                Node x;
//                if(n.left == null) // only right
//                {
//                    x = n.right;
//
//                    Relink(n, n.right, ref _root);
//                    PushNodePool(n);
//                }
//                else if(n.right == null) // only left
//                {
//                    x = n.left;
//
//                    Relink(n, n.left, ref _root);
//                    PushNodePool(n);
//                }
//                else // both has left and right
//                {
//                    Node sc = GetSuccessor(n);
//
//                    deletedColor = sc.color;
//                    parent = sc.parent;
//
//                    if(sc.left != null)
//                    {
//                        throw new Exception($"[Burner]: {nameof(CacheSortedList<TKey, TValue>)} internal error, please call engine guys!");
//                    }
//
//                    if(sc.right != null)
//                    {
//                        x = sc.right;
//                        Relink(sc, sc.right, ref _root);
//                    }
//                    else
//                    {
//                        if(sc.parent.left == sc) sc.parent.left = null;
//                        else sc.parent.right = null;
//                        x = null;
//                    }
//
//                    n.key = sc.key;
//                    n.value = sc.value;
//                    (sc.dups, n.dups) = (n.dups, sc.dups);
//
//                    // rebind the key and node,
//                    // successor will be returned to pool
//                    _dict[n.key] = n;
//
//                    PushNodePool(sc);
//                }
//
//                if(deletedColor == NodeColor.BLACK)
//                {
//                    RBRemoveFixUp(x, parent);
//                }
//            }
//        }
//
//        private void RBRemoveFixUp(Node x, Node p)
//        {
//            // x might be null
//            while(p != null
//                  && x != _root
//                  && (x == null || x.color == NodeColor.BLACK))
//            {
//                var w = GetSibling(x, p);
//
//                if(w == null)
//                {
//                    throw new Exception("[Burner]: Internal Error! Please call the engine guys!");
//                }
//
//                if(p.left == x)
//                {
//                    if(w.color == NodeColor.RED)
//                    {
//                        // case 1
//                        w.color = NodeColor.BLACK;
//                        p.color = NodeColor.RED;
//                        RotateLeft(p, ref _root);
//                        w = p.right;
//                    }
//
//                    if(w == null)
//                    {
//                        throw new Exception("[Burner]: Internal Error! Please call the engine guys!");
//                    }
//
//                    if((w.left == null || w.left.color == NodeColor.BLACK)
//                       && (w.right == null || w.right.color == NodeColor.BLACK))
//                    {
//                        // case 2
//                        w.color = NodeColor.RED;
//                        x = p;
//                        p = x.parent;
//                        continue;
//                    }
//
//                    if(w.left != null
//                       && w.left.color == NodeColor.RED
//                       && (w.right == null || w.right.color == NodeColor.BLACK))
//                    {
//                        // case 3
//
//                        w.left.color = NodeColor.BLACK;
//                        w.color = NodeColor.RED;
//                        RotateRight(w, ref _root);
//                        w = w.parent;
//                    }
//
//                    if(w == null)
//                    {
//                        throw new Exception("[Burner]: Internal Error! Please call the engine guys!");
//                    }
//
//                    // case 4
//                    w.color = p.color;
//                    if(w.right != null) w.right.color = NodeColor.BLACK;
//                    p.color = NodeColor.BLACK;
//                    RotateLeft(p, ref _root);
//
//                    x = _root;
//                }
//                else
//                {
//                    if(w.color == NodeColor.RED)
//                    {
//                        // case 1
//                        w.color = NodeColor.BLACK;
//                        p.color = NodeColor.RED;
//                        RotateRight(p, ref _root);
//                        w = p.left;
//                    }
//
//                    if(w == null)
//                    {
//                        throw new Exception("[Burner]: Internal Error! Please call the engine guys!");
//                    }
//
//                    if((w.left == null || w.left.color == NodeColor.BLACK)
//                       && (w.right == null || w.right.color == NodeColor.BLACK))
//                    {
//                        // case 2
//                        w.color = NodeColor.RED;
//                        x = p;
//                        p = x.parent;
//                        continue;
//                    }
//
//                    if(w.right != null
//                       && w.right.color == NodeColor.RED
//                       && (w.left == null || w.left.color == NodeColor.BLACK))
//                    {
//                        // case 3
//
//                        w.right.color = NodeColor.BLACK;
//                        w.color = NodeColor.RED;
//                        RotateLeft(w, ref _root);
//                        w = w.parent;
//                    }
//
//                    if(w == null)
//                    {
//                        throw new Exception("[Burner]: Internal Error! Please call the engine guys!");
//                    }
//
//                    // case 4
//                    w.color = p.color;
//                    if(w.left != null) w.left.color = NodeColor.BLACK;
//                    p.color = NodeColor.BLACK;
//                    RotateRight(p, ref _root);
//
//                    x = _root;
//                }
//            }
//
//            if(x != null) x.color = NodeColor.BLACK;
//        }
//
//        [ForTest]
//        public void VerifyRBTree()
//        {
//            if(_root == null) return;
//            if(_root.color == NodeColor.RED) // root must be black
//            {
//                throw new Exception();
//            }
//
//            RedNodeCheck(_root);
//
//            // each path of leaf node has same numbers of black nodes
//            var list = new Stack<Node>(Count);
//            var formerBlackNodes = -1;
//            void Traversal(Node n)
//            {
//                if(n == null)
//                {
//                    int b = 0;
//                    list.ForEach(n =>
//                    {
//                        if(n.color == NodeColor.BLACK) b++;
//                    });
//                    if(formerBlackNodes == -1) formerBlackNodes = b;
//                    if(formerBlackNodes != b)
//                    {
//                        throw new Exception();
//                    }
//
//                    return;
//                }
//
//                list.Push(n);
//                Traversal(n.left);
//                Traversal(n.right);
//                list.Pop();
//            }
//
//            Traversal(_root);
//        }
//
//        // each red node has both black children or null
//        private static void RedNodeCheck(Node n)
//        {
//            if(n == null) return;
//            if(n.color == NodeColor.RED)
//            {
//                if(n.left != null && n.left.color == NodeColor.RED)
//                {
//                    throw new Exception();
//                }
//
//                if(n.right != null && n.right.color == NodeColor.RED)
//                {
//                    throw new Exception();
//                }
//            }
//
//            RedNodeCheck(n.left);
//            RedNodeCheck(n.right);
//        }
//
//        // delete n and connect n.parent with c
//        private static void Relink(Node n, Node c, ref Node root)
//        {
//            if(n.parent != null)
//            {
//                if(n.parent.left == n)
//                {
//                    n.parent.left = c;
//                    c.parent = n.parent;
//                }
//                else
//                {
//                    n.parent.right = c;
//                    c.parent = n.parent;
//                }
//            }
//            else
//            {
//                root = c;
//                root.parent = null;
//            }
//        }
//
//        private static Node GetMin(Node n)
//        {
//            while(n.left != null) n = n.left;
//            return n;
//        }
//
//        private static Node GetSuccessor(Node n)
//        {
//            if(n.right != null) return GetMin(n.right);
//            Node p = n.parent;
//
//            while(p != null && p.right == n)
//            {
//                n = p;
//                p = p.parent;
//            }
//            return p;
//        }
//
//        private static Node GetSibling(Node n, Node parent)
//        {
//            if(parent == null) return null;
//            return parent.left == n ? parent.right : parent.left;
//        }
//
//        /// <summary>
//        ///
//        ///        n
//        ///      /   \
//        ///     m     c
//        ///   /   \
//        ///  a     b
//        ///
//        ///   it changes into
//        ///
//        ///      m
//        ///    /   \
//        ///   a     n
//        ///        / \
//        ///       b   c
//        ///
//        /// </summary>
//        private static void RotateRight(Node n, ref Node root)
//        {
//            if(n.left == null) return;
//
//            var m = n.left;
//            var a = m.left;
//            var b = m.right;
//            var c = n.right;
//            var p = n.parent;
//
//            if(p != null)
//            {
//                if(p.left == n) p.left = m;
//                else p.right = m;
//            }
//            else
//            {
//                root = m;
//            }
//
//            m.left = a;
//            m.right = n;
//            n.left = b;
//            n.right = c;
//
//            n.parent = m;
//            m.parent = p;
//
//            if(a != null) a.parent = m;
//            if(b != null) b.parent = n;
//            if(c != null) c.parent = n;
//        }
//
//        /// <summary>
//        ///      m
//        ///    /   \
//        ///   a     n
//        ///        / \
//        ///       b   c
//        ///
//        ///  it changes into
//        ///
//        ///        n
//        ///      /   \
//        ///     m     c
//        ///   /   \
//        ///  a     b
//        ///
//        /// </summary>
//        private static void RotateLeft(Node m, ref Node root)
//        {
//            if(m.right == null) return;
//
//            var n = m.right;
//            var a = m.left;
//            var b = n.left;
//            var c = n.right;
//            var p = m.parent;
//
//            if(p != null)
//            {
//                if(p.left == m) p.left = n;
//                else p.right = n;
//            }
//            else
//            {
//                root = n;
//            }
//
//            n.left = m;
//            n.right = c;
//            m.left = a;
//            m.right = b;
//
//            m.parent = n;
//            n.parent = p;
//
//            if(a != null) a.parent = m;
//            if(b != null) b.parent = m;
//            if(c != null) c.parent = n;
//        }
//
//        // to traverse binary tree without recursion and without stack
//        // reference: https://www.geeksforgeeks.org/inorder-tree-traversal-without-recursion-and-without-stack/
//        private static void MorrisTraversal(Node root, Action<Node> action)
//        {
//            Node current = root;
//            Node pre;
//
//            while(current != null)
//            {
//                if(current.left == null)
//                {
//                    action(current);
//                    current = current.right;
//                }
//                else
//                {
//                    pre = current.left;
//                    while(pre.right != null && pre.right != current)
//                    {
//                        pre = pre.right;
//                    }
//
//                    if(pre.right == null)
//                    {
//                        pre.right = current;
//                        current = current.left;
//                    }
//                    else
//                    {
//                        pre.right = null;
//                        action(current);
//                        current = current.right;
//                    }
//                }
//            }
//        }
//    }
//}
