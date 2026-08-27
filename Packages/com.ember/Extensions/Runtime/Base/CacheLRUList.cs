// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember
// Migrated from Burner extensions with cleanup.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Ember.Basic;

namespace Ember.Extensions
{
    public interface ICanExpiredData : IDisposable
    {
        bool Expired();
    }

    /// <summary>
    /// High-performance LRU (Least Recently Used) data structure with O(1) push/pop operations
    /// and zero GC allocation during normal operation.
    ///
    /// Not thread-safe — external synchronization required for multi-threaded use.
    /// Can also function as a FIFO queue when MaxLength is set to zero or negative.
    ///
    /// <example>
    /// var list = new CacheLRUList&lt;int, string&gt;(5); // max 5 entries
    ///
    /// list.Push(1, "");      // head → 1
    /// list.Push(2, "");      // head → 2, 1 ← tail
    /// list.Push(3, "");      // head → 3, 2, 1 ← tail
    /// list.Push(4, "");      // head → 4, 3, 2, 1 ← tail
    /// list.Push(5, "");      // head → 5, 4, 3, 2, 1 ← tail
    /// list.Push(6, "");      // head → 6, 5, 4, 3, 2 ← tail (1 evicted)
    ///
    /// // Zero-GC manual enumeration:
    /// var first = list.First;
    /// while (first != null) { first = first.Next; }
    /// </example>
    /// </summary>
    public class CacheLRUList<TKey, TVal> : IEnumerable<KeyValuePair<TKey, TVal>> where TVal : ICanExpiredData
    {
        private const string TAG = LogTags.ExtensionCacheLRU;

        public class LRUNode
        {
            public TVal data;
            public TKey key;
        }

        readonly private Stack<LinkedListNode<LRUNode>> _nodePool;
        readonly private Dictionary<TKey, LinkedListNode<LRUNode>> _dataMap;
        readonly private LinkedList<LRUNode> _dataList;

        bool _operatingFlag = false;

        /// <summary>
        /// Max cache length. When the cache count exceeds this value, the tail node is popped and disposed.
        /// Set to zero or negative to disable auto-eviction.
        /// </summary>
        public int MaxLength
        {
            get { return _maxLength; }
            set
            {
                var former = _maxLength;
                _maxLength = value;

                if (_maxLength < former)
                {
                    CheckMaxLength();
                }
            }
        }
        int _maxLength;

        /// <summary>
        /// Current count of cached nodes.
        /// </summary>
        public int Count => _dataList.Count;

        /// <summary>
        /// The first (head) node of the linked list. Use for zero-GC manual enumeration:
        /// <code>var node = list.First; while (node != null) { ... node = node.Next; }</code>
        /// </summary>
        public LinkedListNode<LRUNode> First => _dataList.First;

        /// <summary>
        /// Creates a new CacheLRUList.
        /// </summary>
        /// <param name="maxLength">
        /// Max cache count. When exceeded, the tail node is popped and disposed.
        /// Set to zero or negative to disable auto-eviction.
        /// </param>
        public CacheLRUList(int maxLength = -1)
        {
            MaxLength = maxLength;

            if (MaxLength > 0)
            {
                _nodePool = new Stack<LinkedListNode<LRUNode>>(MaxLength + 1);
                _dataMap = new Dictionary<TKey, LinkedListNode<LRUNode>>(MaxLength + 1);
            }
            else
            {
                _nodePool = new Stack<LinkedListNode<LRUNode>>();
                _dataMap = new Dictionary<TKey, LinkedListNode<LRUNode>>();
            }

            _dataList = new LinkedList<LRUNode>();
        }

        #region 内部方法

        private LinkedListNode<LRUNode> PopNode(TKey key, TVal data)
        {
            if (_nodePool.Count > 0)
            {
                var n = _nodePool.Pop();
                n.Value.data = data;
                n.Value.key = key;
                return n;
            }

            return new LinkedListNode<LRUNode>(new LRUNode { key = key, data = data });
        }

        private void PushNode(LinkedListNode<LRUNode> n)
        {
#if UNITY_EDITOR
            if (n == null)
            {
                throw new Exception("[Ember]: CacheLRUList internal error — node is null");
            }
#endif
            _nodePool.Push(n);
        }

        [Conditional("UNITY_EDITOR")]
        private void AssetCheck()
        {
            if (_operatingFlag)
            {
                throw new Exception("[Ember]: CacheLRUList cannot be operated during TVal.Dispose — check exception stack");
            }

            if (_dataMap.Count != _dataList.Count)
            {
                throw new Exception($"[Ember]: CacheLRUList internal error — map count {_dataMap.Count} vs list count {_dataList.Count}");
            }
        }

        private void DisposeNode(TVal data)
        {
            _operatingFlag = true;
            try
            {
                data.Dispose();
            }
            catch (Exception ex)
            {
                EmberDebug.LogException(TAG, ex);
            }
            finally
            {
                _operatingFlag = false;
            }
        }

        private void CheckMaxLength()
        {
            if (MaxLength > 0)
            {
                AssetCheck();

                while (_dataList.Count > MaxLength)
                {
                    var last = _dataList.Last;
                    var data = last.Value.data;

                    _dataMap.Remove(last.Value.key);
                    _dataList.RemoveLast();
                    PushNode(last);

                    DisposeNode(data);
                }
            }
        }

        #endregion

        #region 外部方法

        /// <summary>
        /// Pushes a value with the given key. If the key already exists, updates the value.
        /// Otherwise inserts at the head of the queue. If max length is exceeded, the tail is evicted and disposed.
        /// </summary>
        public void Push(TKey key, TVal data)
        {
            AssetCheck();

            if (_dataMap.TryGetValue(key, out var node))
            {
                // Update existing
                node.Value.data = data;
            }
            else
            {
                var newNode = PopNode(key, data);
                if (_dataList.Count == 0)
                {
                    _dataList.AddFirst(newNode);
                }
                else
                {
                    _dataList.AddBefore(_dataList.First, newNode);
                }

                _dataMap.Add(key, newNode);
                CheckMaxLength();
            }

            AssetCheck();
        }

        /// <summary>
        /// Tries to remove and return the value associated with the given key.
        /// Returns true if found, false otherwise.
        /// </summary>
        public bool TryPop(TKey key, out TVal val)
        {
            if (_dataMap.TryGetValue(key, out var node))
            {
                AssetCheck();

                val = node.Value.data;

                _dataList.Remove(node);
                _dataMap.Remove(key);
                PushNode(node);

                AssetCheck();

                return true;
            }
            else
            {
                val = default;
                return false;
            }
        }

        /// <summary>
        /// Removes and returns the value associated with the given key, or default if not found.
        /// </summary>
        public TVal Pop(TKey key)
        {
            if (_dataMap.TryGetValue(key, out var node))
            {
                AssetCheck();

                _dataList.Remove(node);
                _dataMap.Remove(key);

                PushNode(node);

                AssetCheck();

                return node.Value.data;
            }

            return default;
        }

        /// <summary>
        /// Tries to pop the last (tail) value of the queue.
        /// </summary>
        public bool TryPopLast(out TVal val)
        {
            if (_dataList.Count > 0)
            {
                AssetCheck();

                var node = _dataList.Last;
                val = node.Value.data;

                _dataMap.Remove(node.Value.key);
                _dataList.RemoveLast();

                PushNode(node);

                AssetCheck();

                return true;
            }
            else
            {
                val = default;
                return false;
            }
        }

        /// <summary>
        /// Pops the last (tail) value of the queue, or default if empty.
        /// </summary>
        public TVal PopLast()
        {
            if (_dataList.Count > 0)
            {
                AssetCheck();

                var node = _dataList.Last;
                var val = node.Value.data;

                _dataMap.Remove(node.Value.key);
                _dataList.RemoveLast();

                PushNode(node);

                AssetCheck();

                return val;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Clears all nodes from the LRU list.
        /// </summary>
        /// <param name="disposeNode">If true, Dispose() is called on each node's data before removal.</param>
        public void Clear(bool disposeNode = true)
        {
            if (_dataList.Count > 0)
            {
                AssetCheck();

                var node = _dataList.Last;
                while (node != null)
                {
                    if (disposeNode)
                    {
                        DisposeNode(node.Value.data);
                    }

                    PushNode(node);
                    node = node.Previous;
                }

                _dataList.Clear();
                _dataMap.Clear();
            }
        }

        /// <summary>
        /// Frees all cached LinkedListNode memory in the internal node pool.
        /// </summary>
        public void FreeCache() => _nodePool.Clear();

        /// <summary>
        /// Iterates from the tail of the list and disposes expired nodes.
        /// </summary>
        /// <param name="breakIfFalse">
        /// If true, stops iterating at the first non-expired node (useful when older entries
        /// are more likely to be expired and you want to avoid scanning the entire list).
        /// </param>
        public void DisposeFromLast(bool breakIfFalse = false)
        {
            if (_dataList.Count == 0)
            {
                return;
            }

            AssetCheck();

            var node = _dataList.Last;
            while (node != null && _dataList.Count > 0)
            {
                if (node.Value.data.Expired())
                {
                    var del = node;
                    node = node.Previous;
                    var data = del.Value.data;

                    _dataList.Remove(del);
                    _dataMap.Remove(del.Value.key);
                    PushNode(del);

                    DisposeNode(data);
                }
                else
                {
                    if (breakIfFalse)
                    {
                        break;
                    }
                    node = node.Previous;
                }
            }

            AssetCheck();
        }

        public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator() => new ListEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct ListEnumerator : IEnumerator<KeyValuePair<TKey, TVal>>
        {
            LinkedList<LRUNode>.Enumerator _enumerator;
            object IEnumerator.Current => Current;

            internal ListEnumerator(CacheLRUList<TKey, TVal> list)
            {
                _enumerator = list._dataList.GetEnumerator();
            }

            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() { }

            public KeyValuePair<TKey, TVal> Current
            {
                get
                {
                    var curr = _enumerator.Current;
                    return new KeyValuePair<TKey, TVal>(curr.key, curr.data);
                }
            }

            public void Dispose() => _enumerator.Dispose();
        }

        #endregion
    }
}
