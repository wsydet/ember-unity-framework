//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//namespace Burner.Basic
//{
//    using System;
//    using System.Collections;
//    using System.Collections.Generic;
//
//    public interface IRemoveAllPredicate<in TValue>
//    {
//        bool Predicated(TValue value);
//    }
//
//    /// <summary>
//    /// A queue (Dictionary + LinkedList) that you can add element in head or rear of this collection in O(1)
//    ///   and you can remove any element in any position of this list in 0(1)
//    /// And more, this queue can pop/push without any GC.Alloc
//    /// https://burner.feishu.cn/wiki/wikcnGBpg543s50PbFnkbdjoWVc#doxcnkMaGKKgoc2oY2jXvg5o6dh
//    /// </summary>
//    public class QuickQueue<TValue> : IEnumerable<TValue>
//    {
//        private readonly Stack<LinkedListNode<TValue>> _nodePool;
//        private readonly Dictionary<TValue, LinkedListNode<TValue>> _dataMap;
//        private readonly LinkedList<TValue> _dataList;
//        private readonly Comparison<TValue> _comparer;
//
//        /// <summary>
//        /// get current count of cached nodes
//        /// </summary>
//        public int Count => _dataList.Count;
//
//        private bool _isRemoveAll;
//
//        private class DefaultRemoveAllPredicate : IRemoveAllPredicate<TValue>
//        {
//            public Predicate<TValue> predicate;
//            public bool Predicated(TValue value) => predicate(value);
//        }
//
//        private DefaultRemoveAllPredicate _removeAllPredicate;
//
//        public QuickQueue(int capacity = 0, Comparison<TValue> comparer = null)
//        {
//            if(capacity > 0)
//            {
//                _nodePool = new Stack<LinkedListNode<TValue>>(capacity);
//                _dataMap = new Dictionary<TValue, LinkedListNode<TValue>>(capacity);
//            }
//            else
//            {
//                _nodePool = new Stack<LinkedListNode<TValue>>();
//                _dataMap = new Dictionary<TValue, LinkedListNode<TValue>>();
//            }
//
//            _comparer = comparer;
//            _dataList = new LinkedList<TValue>();
//        }
//
//        public QuickQueue(Comparison<TValue> comparer) : this(0, comparer)
//        {
//        }
//
//        private LinkedListNode<TValue> PopNode(TValue key)
//        {
//            if(_nodePool.Count > 0)
//            {
//                var n = _nodePool.Pop();
//                n.Value = key;
//                return n;
//            }
//
//            return new LinkedListNode<TValue>(key);
//        }
//
//        private void PushNode(LinkedListNode<TValue> n)
//        {
//#if UNITY_EDITOR
//            if(n == null)
//            {
//                throw new Exception($"[Burner]: QuickLinkedList Internal Error, Please call Burner developers");
//            }
//#endif
//            // clear the value to let the garbage collector do its job
//            n.Value = default;
//            _nodePool.Push(n);
//        }
//
//        public void PushFirst(TValue key) => Push(key, true);
//        public void PushLast(TValue key) => Push(key, false);
//
//        public TValue PopFirst() => Pop(true);
//        public TValue PopLast() => Pop(false);
//
//        public void Add(TValue key) => Push(key, false);
//
//        public void Push(TValue key, bool firstOrLast = true)
//        {
//            if(_dataMap.TryGetValue(key, out var node))
//            {
//                if(_comparer == null)
//                {
//                    // just update data
//                    node.Value = key;
//                }
//                else
//                {
//                    // remove and add it again in order to sort
//                    Remove(key);
//                    Add(key);
//                }
//            }
//            else
//            {
//                var newNode = PopNode(key);
//                if(_dataList.Count == 0)
//                {
//                    _dataList.AddFirst(newNode);
//                }
//                else
//                {
//                    if(_comparer != null)
//                    {
//                        if(_comparer(_dataList.First.Value, key) >= 0)
//                        {
//                            _dataList.AddBefore(_dataList.First, newNode);
//                        }
//                        else if(_comparer(_dataList.Last.Value, key) <= 0)
//                        {
//                            _dataList.AddAfter(_dataList.Last, newNode);
//                        }
//                        else
//                        {
//                            bool added = false;
//                            var n = _dataList.First;
//                            while(n != null)
//                            {
//                                if(_comparer(n.Value, key) >= 0)
//                                {
//                                    added = true;
//                                    _dataList.AddBefore(n, newNode);
//                                    break;
//                                }
//                                n = n.Next;
//                            }
//
//                            if(!added)
//                            {
//                                _dataList.AddAfter(_dataList.Last, newNode);
//                            }
//
//                        }
//                    }
//                    else
//                    {
//                        if(firstOrLast)
//                        {
//                            _dataList.AddBefore(_dataList.First, newNode);
//                        }
//                        else
//                        {
//                            _dataList.AddAfter(_dataList.Last, newNode);
//                        }
//                    }
//                }
//
//                _dataMap.Add(key, newNode);
//            }
//
//        }
//
//        public bool Remove(TValue key) => TryPop(key);
//        public bool TryPop(TValue key)
//        {
//            if(_dataMap.TryGetValue(key, out var node))
//            {
//                key = node.Value;
//
//                _dataList.Remove(node);
//                _dataMap.Remove(key);
//
//                PushNode(node);
//
//                return true;
//            }
//
//            return false;
//        }
//
//        public TValue Pop(bool firstOrLast = false)
//        {
//            if(_dataList.Count > 0)
//            {
//                var node = firstOrLast ? _dataList.First : _dataList.Last;
//                var key = node.Value;
//
//                _dataList.Remove(node);
//                _dataMap.Remove(node.Value);
//
//                PushNode(node);
//
//                return key;
//            }
//
//            return default;
//        }
//
//        public TValue Peek(bool firstOrLast = false)
//        {
//            if(_dataList.Count > 0)
//            {
//                var node = firstOrLast ? _dataList.First : _dataList.Last;
//                var key = node.Value;
//
//                return key;
//            }
//
//            return default;
//        }
//
//        public bool Contains(TValue key)
//        {
//            return _dataMap.ContainsKey(key);
//        }
//
//
//        public void RemoveAll(Predicate<TValue> predicate)
//        {
//            _removeAllPredicate ??= new DefaultRemoveAllPredicate();
//
//            _removeAllPredicate.predicate = predicate;
//            RemoveAll(_removeAllPredicate);
//        }
//
//        public void RemoveAll(IRemoveAllPredicate<TValue> predicate)
//        {
//            if(Count == 0) return;
//
//            if(_isRemoveAll)
//            {
//                throw new Exception("[Burner]: cannot call RemoveAll when it's in the RemoveAll status");
//            }
//
//            _isRemoveAll = true;
//            try
//            {
//                loop_start:
//
//                var node = _dataList.First;
//                while(node != null)
//                {
//                    var deleting = predicate.Predicated(node.Value);
//
//                    if(node.List != _dataList)
//                    {
//                        // current node has been deleted by 'pred' lambda
//                        goto loop_start;
//                    }
//
//                    if(deleting)
//                    {
//                        var delNode = node;
//                        node = delNode.Next;
//
//                        _dataList.Remove(delNode);
//                        _dataMap.Remove(delNode.Value);
//
//                        PushNode(delNode);
//                    }
//                    else
//                    {
//                        node = node.Next;
//                    }
//                }
//            }
//            finally
//            {
//                _isRemoveAll = false;
//            }
//        }
//
//        public void CopyTo(List<TValue> list)
//        {
//            if(list == null)
//            {
//                throw new ArgumentNullException();
//            }
//
//            if(list.Capacity < _dataList.Count + list.Count)
//            {
//                list.Capacity = _dataList.Count + list.Count;
//            }
//
//            var node = _dataList.First;
//            while(node != null)
//            {
//                list.Add(node.Value);
//                node = node.Next;
//            }
//        }
//
//        public void Clear()
//        {
//            if(_dataList.Count > 0)
//            {
//                if(_isRemoveAll)
//                {
//                    throw new Exception("[Burner]: cannot call RemoveAll when it's in the RemoveAll status");
//                }
//
//                var node = _dataList.Last;
//                while(node != null)
//                {
//                    PushNode(node);
//                    node = node.Previous;
//                }
//
//                _dataList.Clear();
//                _dataMap.Clear();
//            }
//        }
//
//        /// <summary>
//        /// Free all cached node to let GC.Collect free this memory
//        /// </summary>
//        public void FreeCache() => _nodePool.Clear();
//
//        public IEnumerator<TValue> GetEnumerator() => _dataList.GetEnumerator();
//        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//    }
//}
