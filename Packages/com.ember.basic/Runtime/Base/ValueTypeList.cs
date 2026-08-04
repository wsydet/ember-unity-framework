// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using System.Collections;
using System.Collections.Generic;

namespace Ember.Basic
{
    /// <summary>
    /// 值类型专用 List，提供与 <see cref="List{T}"/> 类似的 API，
    /// 额外支持 <see cref="GetRef"/> 返回 ref 引用以进行零拷贝访问。
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    public class ValueTypeList<T> : IEnumerable<T> where T : struct
    {
        private const int DefaultCapacity = 4;
        private static readonly T[] EmptyArray = new T[0];

        private T[] _items;
        private int _size;
        private int _version;

        public ValueTypeList()
        {
            _items = EmptyArray;
        }

        public ValueTypeList(int capacity)
        {
            _items = capacity == 0 ? EmptyArray : new T[capacity];
        }

        // ======== 属性 ========

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                            Array.Copy(_items, 0, newItems, 0, _size);
                        _items = newItems;
                    }
                    else
                    {
                        _items = EmptyArray;
                    }
                }
            }
        }

        public int Count => _size;

        public T this[int index]
        {
            get => _items[index];
            set
            {
                _items[index] = value;
                _version++;
            }
        }

        /// <summary>
        /// 返回指定索引处元素的 ref 引用，支持零拷贝读写。
        /// </summary>
        public ref T GetRef(int index) => ref _items[index];

        // ======== Add ========

        public void Add(ref T item)
        {
            var array = _items;
            var size = _size;
            _version++;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        public void AddRange(IEnumerable<T> collection) => InsertRange(_size, collection);

        private void AddWithResize(T item)
        {
            var size = _size;
            EnsureCapacity(size + 1);
            _size = size + 1;
            _items[size] = item;
        }

        // ======== Insert ========

        public void Insert(int index, T item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
                Array.Copy(_items, index, _items, index + 1, _size - index);
            _items[index] = item;
            _size++;
            _version++;
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                        Array.Copy(_items, index, _items, index + count, _size - index);

                    if (this == c)
                    {
                        Array.Copy(_items, 0, _items, index, index);
                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        c.CopyTo(_items, index);
                    }

                    _size += count;
                }
            }
            else if (index < _size)
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                        Insert(index++, en.Current);
                }
            }
            else
            {
                AddEnumerable(collection);
            }

            _version++;
        }

        // ======== Remove ========

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            _size--;
            if (index < _size)
                Array.Copy(_items, index + 1, _items, index, _size - index);
            _items[_size] = default;
            _version++;
        }

        public void RemoveRange(int index, int count)
        {
            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                    Array.Copy(_items, index + count, _items, index, _size - index);
                _version++;
                Array.Clear(_items, _size, count);
            }
        }

        // ======== 查询 ========

        public int IndexOf(T item) => Array.IndexOf(_items, item, 0, _size);

        public int IndexOf(T item, int index) => Array.IndexOf(_items, item, index, _size - index);

        public int IndexOf(T item, int index, int count) => Array.IndexOf(_items, item, index, count);

        public bool Contains(T item) => _size != 0 && IndexOf(item) != -1;

        public int BinarySearch(T item) => BinarySearch(0, Count, item, null);

        public int BinarySearch(T item, IComparer<T> comparer) => BinarySearch(0, Count, item, comparer);

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0) return -1;
            return Array.BinarySearch(_items, index, count, item, comparer);
        }

        // ======== 复制 ========

        public void CopyTo(T[] array) => CopyTo(array, 0);

        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, _size);

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        // ======== 排序 ========

        public void Reverse() => Reverse(0, Count);

        public void Reverse(int index, int count)
        {
            if (count > 1)
                Array.Reverse(_items, index, count);
            _version++;
        }

        public void Sort() => Sort(0, Count, null);

        public void Sort(IComparer<T> comparer) => Sort(0, Count, comparer);

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (count > 1)
                Array.Sort(_items, index, count, comparer);
            _version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (_size > 1)
            {
                _defaultComparer.CompareFunc = comparison;
                Array.Sort(_items, 0, _size, _defaultComparer);
            }

            _version++;
        }

        // ======== 清理 ========

        public void Clear()
        {
            int size = _size;
            _size = 0;
            _version++;
            if (size > 0)
                Array.Clear(_items, 0, size);
        }

        // ======== 内部 ========

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
                if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        private void AddEnumerable(IEnumerable<T> enumerable)
        {
            using (IEnumerator<T> en = enumerable.GetEnumerator())
            {
                _version++;
                while (en.MoveNext())
                {
                    T current = en.Current;
                    if (_size == _items.Length)
                        EnsureCapacity(_size + 1);
                    _items[_size++] = current;
                }
            }
        }

        // ======== Comparer ========

        private sealed class ComparerAdapter : IComparer<T>
        {
            public Comparison<T> CompareFunc { get; set; }
            public int Compare(T x, T y) => CompareFunc(x, y);
        }

        private static readonly ComparerAdapter _defaultComparer = new();

        // ======== Enumerator ========

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ValueTypeList<T> _list;
            private int _index;
            private readonly int _version;
            private T _current;

            internal Enumerator(ValueTypeList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                if (_version == _list._version && (uint)_index < (uint)_list._size)
                {
                    _current = _list._items[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                _index = _list._size + 1;
                _current = default;
                return false;
            }

            public T Current => _current;

            object IEnumerator.Current => Current;

            void IEnumerator.Reset()
            {
                _index = 0;
                _current = default;
            }
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
    }
}
