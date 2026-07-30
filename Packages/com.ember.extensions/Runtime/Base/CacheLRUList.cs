//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using System.Collections;
//
//namespace Burner.Extensions
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Diagnostics;
//
//    public interface ICanExpiredData : IDisposable
//    {
//        bool Expired();
//    }
//
//    /// <summary>
//    /// high performance LRU data structure with O(1) push/pop operation without any GC.Alloc
//    /// https://burner.feishu.cn/wiki/wikcnGBpg543s50PbFnkbdjoWVc#doxcnWEsaQiAow2QiS6sL3aPRff
//    ///
//    /// It's not multi-thread safe, you need to deal with those multi-thread context with a lock.
//    /// Of course, it can be used as FIFO queue, if you set MaxLength as zero or negative value
//    ///
//    /// pseudo example:
//    ///
//    /// var list = new CacheLRUList<int,string>(5); // create a list with max length 5
//    ///
//    /// list.Push(1,"");      // head-> 1
//    /// list.Push(2,"");      // head-> 2 1 <- tail
//    /// list.Push(3,"");      // head-> 3 2 1 <- tail
//    /// list.Push(4,"");      // head-> 4 3 2 1 <- tail
//    /// list.Push(5,"");      // head-> 5 4 3 2 1 <- tail
//    ///
//    /// list.Push(6,"");      // head-> 6 5 4 3 2 <- tail,  popup the tail
//    ///
//    /// Assert.False(list.TryPop(1));
//    ///
//    /// list.Pop(3);         // head-> 6 5 4 2 <- tail
//    /// list.Push(3);         // head-> 3 6 5 4 2 <- tail
//    ///
//    /// </summary>
//    public class CacheLRUList<TKey, TVal> : IEnumerable<KeyValuePair<TKey, TVal>> where TVal : ICanExpiredData
//    {
//        public class LRUNode
//        {
//            public TVal data;
//            public TKey key;
//        }
//
//        readonly private Stack<LinkedListNode<LRUNode>> _nodePool;
//        readonly private Dictionary<TKey, LinkedListNode<LRUNode>> _dataMap;
//        readonly private LinkedList<LRUNode> _dataList;
//
//        bool _operatingFlag = false;
//
//        /// <summary>
//        /// max cache length
//        /// </summary>
//        public int MaxLength
//        {
//            get { return _maxLength; }
//            set
//            {
//                var former = _maxLength;
//                _maxLength = value;
//
//                if(_maxLength < former)
//                {
//                    CheckMaxLength();
//                }
//            }
//        }
//        int _maxLength;
//
//        /// <summary>
//        /// get current count of cached nodes
//        /// </summary>
//        public int Count => _dataList.Count;
//
//        /// <summary>
//        /// there is some GC.Alloc while 'foreach(var n in CacheLRUList)'
//        /// so that can use following enumeration to avoid GC.Alloc:
//        ///   var first = CacheLRUList.First;
//        ///   while(first != null){
//        ///      first = first.Next;
//        ///   }
//        /// </summary>
//        public LinkedListNode<LRUNode> First => _dataList.First;
//
//        /// <summary>
//        /// constructor
//        /// </summary>
//        /// <param name="maxLength">
//        /// max cache count in this LRU.
//        /// when its cache count is greater than this max length, the tail node will be popped / disposed.
//        /// If set it as value less or equal than 0, tail's value won't never be disposed when push a new one
//        /// </param>
//        public CacheLRUList(int maxLength = -1)
//        {
//            MaxLength = maxLength;
//
//            if(MaxLength > 0)
//            {
//                _nodePool = new Stack<LinkedListNode<LRUNode>>(MaxLength + 1);
//                _dataMap = new Dictionary<TKey, LinkedListNode<LRUNode>>(MaxLength + 1);
//            }
//            else
//            {
//                _nodePool = new Stack<LinkedListNode<LRUNode>>();
//                _dataMap = new Dictionary<TKey, LinkedListNode<LRUNode>>();
//            }
//
//            _dataList = new LinkedList<LRUNode>();
//        }
//
//
//
//        private LinkedListNode<LRUNode> PopNode(TKey key, TVal data)
//        {
//            if(_nodePool.Count > 0)
//            {
//                var n = _nodePool.Pop();
//                n.Value.data = data;
//                n.Value.key = key;
//                return n;
//            }
//
//            return new LinkedListNode<LRUNode>(new LRUNode { key = key, data = data });
//        }
//
//        private void PushNode(LinkedListNode<LRUNode> n)
//        {
//#if UNITY_EDITOR
//            if(n == null)
//            {
//                throw new Exception($"[Burner]: CacheLRUList Internal Error, Please call Burner developers");
//            }
//#endif
//            _nodePool.Push(n);
//        }
//
//        [Conditional("UNITY_EDITOR")]
//        private void AssetCheck()
//        {
//            if(_operatingFlag)
//            {
//                throw new Exception($"[Burner]: CacheLRUList cannot been operated in TVal.Dispose, please check exception stack information");
//            }
//
//            if(_dataMap.Count != _dataList.Count)
//            {
//                throw new Exception($"[Burner]: CacheLRUList Internal Error {_dataMap.Count} vs {_dataList.Count}, Please call Burner developers");
//            }
//        }
//
//        private void DisposeNode(TVal data)
//        {
//            _operatingFlag = true;
//            try
//            {
//                data.Dispose();
//            }
//            catch(Exception ex)
//            {
//                UnityEngine.Debug.LogException(ex);
//            }
//            finally
//            {
//                _operatingFlag = false;
//            }
//        }
//
//        private void CheckMaxLength()
//        {
//            if(MaxLength > 0)
//            {
//                AssetCheck();
//
//                while(_dataList.Count > MaxLength)
//                {
//                    var last = _dataList.Last;
//                    var data = last.Value.data;
//
//                    _dataMap.Remove(last.Value.key);
//                    _dataList.RemoveLast();
//                    PushNode(last);
//
//                    DisposeNode(data);
//                }
//            }
//        }
//
//        /// <summary>
//        /// if you push a value with key exists, it will update value, or insert in head of this queue
//        /// if it reaches max length of cache, node in last will be popped and disposed
//        /// </summary>
//        public void Push(TKey key, TVal data)
//        {
//            AssetCheck();
//
//            if(_dataMap.TryGetValue(key, out var node))
//            {
//                // update data
//                node.Value.data = data;
//            }
//            else
//            {
//                var newNode = PopNode(key, data);
//                if(_dataList.Count == 0)
//                {
//                    _dataList.AddFirst(newNode);
//                }
//                else
//                {
//                    _dataList.AddBefore(_dataList.First, newNode);
//                }
//
//                _dataMap.Add(key, newNode);
//                CheckMaxLength();
//            }
//
//            AssetCheck();
//        }
//
//        public bool TryPop(TKey key, out TVal val)
//        {
//            if(_dataMap.TryGetValue(key, out var node))
//            {
//                AssetCheck();
//
//                val = node.Value.data;
//
//                _dataList.Remove(node);
//                _dataMap.Remove(key);
//                PushNode(node);
//
//                AssetCheck();
//
//                return true;
//            }
//            else
//            {
//                val = default;
//                return false;
//            }
//        }
//
//        /// <summary>
//        /// return null(default) if cannot find the key, it won't throw exception
//        /// </summary>
//        public TVal Pop(TKey key)
//        {
//            if(_dataMap.TryGetValue(key, out var node))
//            {
//                AssetCheck();
//
//                _dataList.Remove(node);
//                _dataMap.Remove(key);
//
//                PushNode(node);
//
//                AssetCheck();
//
//                return node.Value.data;
//            }
//
//            return default;
//        }
//
//        /// <summary>
//        /// try to pop last value of this queue
//        /// </summary>
//        public bool TryPopLast(out TVal val)
//        {
//            if(_dataList.Count > 0)
//            {
//                AssetCheck();
//
//                var node = _dataList.Last;
//                val = node.Value.data;
//
//                _dataMap.Remove(node.Value.key);
//                _dataList.RemoveLast();
//
//                PushNode(node);
//
//                AssetCheck();
//
//                return true;
//            }
//            else
//            {
//                val = default;
//                return false;
//            }
//        }
//
//        /// <summary>
//        /// pop last value of this queue
//        /// </summary>
//        public TVal PopLast()
//        {
//            if(_dataList.Count > 0)
//            {
//                AssetCheck();
//
//                var node = _dataList.Last;
//                var val = node.Value.data;
//
//                _dataMap.Remove(node.Value.key);
//                _dataList.RemoveLast();
//
//                PushNode(node);
//
//                AssetCheck();
//
//                return val;
//            }
//            else
//            {
//                return default;
//            }
//        }
//
//        /// <summary>
//        /// load (append) all values from Frist to Last
//        /// recommand only used for debug/test
//        /// </summary>
//        [ForDebug, ForTest]
//        public void LoadListFristToLast(List<TVal> list)
//        {
//            var n = _dataList.First;
//            while(n != null)
//            {
//                list.Add(n.Value.data);
//                n = n.Next;
//            }
//        }
//
//        /// <summary>
//        /// clear all node of this LRU list
//        /// </summary>
//        /// <param name="disposeNode">
//        /// dispose node or not
//        /// </param>
//        public void Clear(bool disposeNode = true)
//        {
//            if(_dataList.Count > 0)
//            {
//                AssetCheck();
//
//                var node = _dataList.Last;
//                while(node != null)
//                {
//                    if(disposeNode)
//                    {
//                        DisposeNode(node.Value.data);
//                    }
//
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
//        /// free all cache of LinkedNode memory
//        /// </summary>
//        public void FreeCache() => _nodePool.Clear();
//
//        /// <summary>
//        /// to iterate from tail of list and dispose data when predicate succ
//        /// </summary>
//        /// <param name="breakIfFalse">
//        /// whether breaking iteration when predicate failed,
//        /// comparing to iterating whole list, breaking is more effective in some cases
//        /// </param>
//        public void DisposeFromLast(bool breakIfFalse = false)
//        {
//            if(_dataList.Count == 0)
//            {
//                return;
//            }
//
//            AssetCheck();
//
//            var node = _dataList.Last;
//            while(node != null && _dataList.Count > 0)
//            {
//                if(node.Value.data.Expired())
//                {
//                    var del = node;
//                    node = node.Previous;
//                    var data = del.Value.data;
//
//                    _dataList.Remove(del);
//                    _dataMap.Remove(del.Value.key);
//                    PushNode(del);
//
//                    DisposeNode(data);
//                }
//                else
//                {
//                    if(breakIfFalse)
//                    {
//                        break;
//                    }
//                    node = node.Previous;
//                }
//            }
//
//            AssetCheck();
//        }
//
//        public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator() => new ListEnumerator(this);
//        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//
//        public struct ListEnumerator : IEnumerator<KeyValuePair<TKey, TVal>>
//        {
//            LinkedList<LRUNode>.Enumerator _enumerator;
//            object IEnumerator.Current => Current;
//
//            internal ListEnumerator(CacheLRUList<TKey, TVal> list)
//            {
//                _enumerator = list._dataList.GetEnumerator();
//            }
//
//            public bool MoveNext() => _enumerator.MoveNext();
//            public void Reset(){}
//
//            public KeyValuePair<TKey, TVal> Current
//            {
//                get
//                {
//                    var curr = _enumerator.Current;
//                    return new KeyValuePair<TKey, TVal>(curr.key, curr.data);
//                }
//            }
//
//            public void Dispose() => _enumerator.Dispose();
//        }
//    }
//}
