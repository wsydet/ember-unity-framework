// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System.Collections.Generic;

namespace Ember.Basic
{
    /// <summary>
    /// 泛型对象池，用于复用 class 类型的实例。
    ///
    /// 与 <see cref="ListPool{T}"/> 等静态池不同，此池按实例管理，
    /// 每个 MemoryPool 有独立的最大容量限制。
    /// </summary>
    /// <typeparam name="T">池中对象的类型，必须是引用类型</typeparam>
    public class MemoryPool<T> where T : class
    {
        private readonly int _maxCapacity;
        private readonly Queue<T> _objects;

        public MemoryPool(int maxCapacity = 10)
        {
            _maxCapacity = maxCapacity;
            _objects = new Queue<T>();
        }

        /// <summary>
        /// 当前池中缓存的对象数量。
        /// </summary>
        public int Count => _objects.Count;

        /// <summary>
        /// 是否还可以向池中归还对象（未达到最大容量）。
        /// </summary>
        public bool CanReturn => _objects.Count < _maxCapacity;

        /// <summary>
        /// 从池中获取一个对象。如果池为空则返回 null。
        /// </summary>
        public T Get()
        {
            return _objects.Count > 0 ? _objects.Dequeue() : null;
        }

        /// <summary>
        /// 向池中归还一个对象。如果池已满则返回 false，对象不会被缓存。
        /// </summary>
        public bool Return(T obj)
        {
            if (_objects.Count < _maxCapacity)
            {
                _objects.Enqueue(obj);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查指定对象是否已在池中。
        /// </summary>
        public bool Contains(T obj)
        {
            return _objects.Contains(obj);
        }

        /// <summary>
        /// 清空池中所有缓存对象。
        /// </summary>
        public void Clear()
        {
            _objects.Clear();
        }
    }
}
