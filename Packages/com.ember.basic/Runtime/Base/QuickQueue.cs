// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

namespace Ember.Basic
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// 判断值是否满足删除条件的谓词接口。
    /// </summary>
    public interface IRemoveAllPredicate<in TValue>
    {
        bool Predicated(TValue value);
    }

    /// <summary>
    /// 基于 Dictionary + LinkedList 的快速双端队列。
    ///
    /// 特性：
    /// - 头部/尾部 Push/Pop 均为 O(1)
    /// - 任意位置 Remove 为 O(1)
    /// - 支持排序模式（提供 Comparison 委托时自动维护排序）
    /// - 通过内部节点池实现 Push/Pop 零 GC 分配
    /// </summary>
    public class QuickQueue<TValue> : IEnumerable<TValue>
    {
        private readonly Stack<LinkedListNode<TValue>> _nodePool;
        private readonly Dictionary<TValue, LinkedListNode<TValue>> _dataMap;
        private readonly LinkedList<TValue> _dataList;
        private readonly Comparison<TValue> _comparer;

        private bool _isRemoveAll;

        private DefaultRemoveAllPredicate _removeAllPredicate;

        private class DefaultRemoveAllPredicate : IRemoveAllPredicate<TValue>
        {
            public Predicate<TValue> Predicate;
            public bool Predicated(TValue value) => Predicate(value);
        }

        /// <summary>
        /// 队列中当前元素数量。
        /// </summary>
        public int Count => _dataList.Count;

        public QuickQueue(int capacity = 0, Comparison<TValue> comparer = null)
        {
            if (capacity > 0)
            {
                _nodePool = new Stack<LinkedListNode<TValue>>(capacity);
                _dataMap = new Dictionary<TValue, LinkedListNode<TValue>>(capacity);
            }
            else
            {
                _nodePool = new Stack<LinkedListNode<TValue>>();
                _dataMap = new Dictionary<TValue, LinkedListNode<TValue>>();
            }

            _comparer = comparer;
            _dataList = new LinkedList<TValue>();
        }

        public QuickQueue(Comparison<TValue> comparer) : this(0, comparer)
        {
        }

        // ======== 节点池 ========

        private LinkedListNode<TValue> PopNode(TValue key)
        {
            if (_nodePool.Count > 0)
            {
                var n = _nodePool.Pop();
                n.Value = key;
                return n;
            }

            return new LinkedListNode<TValue>(key);
        }

        private void PushNode(LinkedListNode<TValue> n)
        {
#if UNITY_EDITOR
            if (n == null)
                throw new Exception("[Ember] QuickQueue internal error: null node pushed to pool.");
#endif
            n.Value = default;
            _nodePool.Push(n);
        }

        // ======== Push ========

        /// <summary>在队列头部插入元素。</summary>
        public void PushFirst(TValue key) => Push(key, first: true);

        /// <summary>在队列尾部插入元素。</summary>
        public void PushLast(TValue key) => Push(key, first: false);

        /// <summary>在队列尾部插入元素（同 PushLast）。</summary>
        public void Add(TValue key) => Push(key, first: false);

        public void Push(TValue key, bool first = true)
        {
            if (_dataMap.TryGetValue(key, out var node))
            {
                if (_comparer == null)
                {
                    node.Value = key;
                }
                else
                {
                    Remove(key);
                    Add(key);
                }
            }
            else
            {
                var newNode = PopNode(key);
                if (_dataList.Count == 0)
                {
                    _dataList.AddFirst(newNode);
                }
                else
                {
                    if (_comparer != null)
                    {
                        InsertSorted(newNode);
                    }
                    else
                    {
                        if (first)
                            _dataList.AddBefore(_dataList.First, newNode);
                        else
                            _dataList.AddAfter(_dataList.Last, newNode);
                    }
                }

                _dataMap.Add(key, newNode);
            }
        }

        private void InsertSorted(LinkedListNode<TValue> newNode)
        {
            if (_comparer(_dataList.First.Value, newNode.Value) >= 0)
            {
                _dataList.AddBefore(_dataList.First, newNode);
            }
            else if (_comparer(_dataList.Last.Value, newNode.Value) <= 0)
            {
                _dataList.AddAfter(_dataList.Last, newNode);
            }
            else
            {
                bool added = false;
                var n = _dataList.First;
                while (n != null)
                {
                    if (_comparer(n.Value, newNode.Value) >= 0)
                    {
                        added = true;
                        _dataList.AddBefore(n, newNode);
                        break;
                    }

                    n = n.Next;
                }

                if (!added)
                    _dataList.AddAfter(_dataList.Last, newNode);
            }
        }

        // ======== Pop ========

        public TValue PopFirst() => Pop(first: true);
        public TValue PopLast() => Pop(first: false);

        public TValue Pop(bool first = false)
        {
            if (_dataList.Count > 0)
            {
                var node = first ? _dataList.First : _dataList.Last;
                var key = node.Value;

                _dataList.Remove(node);
                _dataMap.Remove(key);
                PushNode(node);

                return key;
            }

            return default;
        }

        /// <summary>查看但不移除头部/尾部元素。</summary>
        public TValue Peek(bool first = false)
        {
            if (_dataList.Count > 0)
            {
                var node = first ? _dataList.First : _dataList.Last;
                return node.Value;
            }

            return default;
        }

        // ======== Remove ========

        /// <summary>移除指定元素，O(1)。</summary>
        public bool Remove(TValue key) => TryPop(key);

        public bool TryPop(TValue key)
        {
            if (_dataMap.TryGetValue(key, out var node))
            {
                _dataList.Remove(node);
                _dataMap.Remove(key);
                PushNode(node);
                return true;
            }

            return false;
        }

        /// <summary>是否包含指定元素，O(1)。</summary>
        public bool Contains(TValue key) => _dataMap.ContainsKey(key);

        /// <summary>批量删除满足条件的元素。</summary>
        public void RemoveAll(Predicate<TValue> predicate)
        {
            _removeAllPredicate ??= new DefaultRemoveAllPredicate();
            _removeAllPredicate.Predicate = predicate;
            RemoveAll(_removeAllPredicate);
        }

        public void RemoveAll(IRemoveAllPredicate<TValue> predicate)
        {
            if (Count == 0) return;

            if (_isRemoveAll)
                throw new InvalidOperationException("[Ember] Cannot call RemoveAll recursively.");

            _isRemoveAll = true;
            try
            {
                loop_start:
                var node = _dataList.First;
                while (node != null)
                {
                    var deleting = predicate.Predicated(node.Value);

                    if (node.List != _dataList)
                        goto loop_start;

                    if (deleting)
                    {
                        var delNode = node;
                        node = delNode.Next;

                        _dataList.Remove(delNode);
                        _dataMap.Remove(delNode.Value);
                        PushNode(delNode);
                    }
                    else
                    {
                        node = node.Next;
                    }
                }
            }
            finally
            {
                _isRemoveAll = false;
            }
        }

        // ======== 批量操作 ========

        public void CopyTo(List<TValue> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            if (list.Capacity < _dataList.Count + list.Count)
                list.Capacity = _dataList.Count + list.Count;

            var node = _dataList.First;
            while (node != null)
            {
                list.Add(node.Value);
                node = node.Next;
            }
        }

        public void Clear()
        {
            if (_dataList.Count == 0) return;

            if (_isRemoveAll)
                throw new InvalidOperationException("[Ember] Cannot call Clear during RemoveAll.");

            var node = _dataList.Last;
            while (node != null)
            {
                PushNode(node);
                node = node.Previous;
            }

            _dataList.Clear();
            _dataMap.Clear();
        }

        /// <summary>释放节点池中缓存的内存，供 GC 回收。</summary>
        public void FreeCache() => _nodePool.Clear();

        public IEnumerator<TValue> GetEnumerator() => _dataList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
