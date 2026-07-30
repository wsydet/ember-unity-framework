//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//
//namespace Burner.Basic
//{
//    /// <summary>
//    /// 值类型List，用来高效方便存储值类型对象
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    public class ValueTypeList<T> : IEnumerable<T> where T : struct
//    {
//        private const int _defaultCapacity = 4;
//
//        private T[] _items;
//        private int _size;
//        private int _version;
//
//        private static readonly T[] _emptyArray = new T[0];
//
//        // Constructs a UncheckedList. The list is initially empty and has a capacity
//        // of zero. Upon adding the first element to the list the capacity is
//        // increased to _defaultCapacity, and then increased in multiples of two
//        // as required.
//        public ValueTypeList()
//        {
//            _items = _emptyArray;
//        }
//
//        // Constructs a UncheckedList with a given initial capacity. The list is
//        // initially empty, but will have room for the given number of elements
//        // before any reallocations are required.
//        // 
//        public ValueTypeList(int capacity)
//        {
//            if (capacity == 0)
//                _items = _emptyArray;
//            else
//                _items = new T[capacity];
//        }
//
//        // Gets and sets the capacity of this list.  The capacity is the size of
//        // the internal array used to hold items.  When set, the internal 
//        // array of the list is reallocated to the given capacity.
//        // 
//        public int Capacity
//        {
//            get
//            {
//                return _items.Length;
//            }
//            set
//            {
//                if (value != _items.Length)
//                {
//                    if (value > 0)
//                    {
//                        T[] newItems = new T[value];
//                        if (_size > 0)
//                        {
//                            Array.Copy(_items, 0, newItems, 0, _size);
//                        }
//                        _items = newItems;
//                    }
//                    else
//                    {
//                        _items = _emptyArray;
//                    }
//                }
//            }
//        }
//
//        // Read-only property describing how many elements are in the UncheckedList.
//        public int Count
//        {
//            get
//            {
//                return _size;
//            }
//        }
//
//        // Sets or Gets the element at the given index.
//        // 
//        public T this[int index]
//        {
//            get
//            {
//                return _items[index];
//            }
//
//            set
//            {
//                _items[index] = value;
//                _version++;
//            }
//        }
//
//        public ref T GetRef(int index)
//        {
//            return ref _items[index];
//        }
//
//        // Adds the given object to the end of this list. The size of the list is
//        // increased by one. If required, the capacity of the list is doubled
//        // before adding the new element.
//        public void Add(ref T item)
//        {
//            var array = _items;
//            var size = _size;
//            _version++;
//            if ((uint)size < (uint)array.Length)
//            {
//                _size = size + 1;
//                array[size] = item;
//            }
//            else
//            {
//                AddWithResize(item);
//            }
//        }
//        // Adds the elements of the given collection to the end of this list. If
//        // required, the capacity of the list is increased to twice the previous
//        // capacity or the new size, whichever is larger.
//        //
//        public void AddRange(IEnumerable<T> collection)
//        {
//            InsertRange(_size, collection);
//        }
//
//        // Non-inline from UncheckedList.Add to improve its code quality as uncommon path
//        private void AddWithResize(T item)
//        {
//            var size = _size;
//            EnsureCapacity(size + 1);
//            _size = size + 1;
//            _items[size] = item;
//        }
//
//        // Searches a section of the list for a given element using a binary search
//        // algorithm. Elements of the list are compared to the search value using
//        // the given IComparer interface. If comparer is null, elements of
//        // the list are compared to the search value using the IComparable
//        // interface, which in that case must be implemented by all elements of the
//        // list and the given search value. This method assumes that the given
//        // section of the list is already sorted; if this is not the case, the
//        // result will be incorrect.
//        //
//        // The method returns the index of the given value in the list. If the
//        // list does not contain the given value, the method returns a negative
//        // integer. The bitwise complement operator (~) can be applied to a
//        // negative result to produce the index of the first element (if any) that
//        // is larger than the given search value. This is also the index at which
//        // the search value should be inserted into the list in order for the list
//        // to remain sorted.
//        // 
//        // The method uses the Array.BinarySearch method to perform the
//        // search.
//        // 
//        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
//        {
//            if (index < 0) return -1;
//            return Array.BinarySearch<T>(_items, index, count, item, comparer);
//        }
//
//        public int BinarySearch(T item)
//        {
//            return BinarySearch(0, Count, item, null);
//        }
//
//        public int BinarySearch(T item, IComparer<T> comparer)
//        {
//            return BinarySearch(0, Count, item, comparer);
//        }
//
//
//        // Clears the contents of UncheckedList.
//        public void Clear()
//        {
//            int size = _size;
//            _size = 0;
//            _version++;
//            if (size > 0)
//            {
//                Array.Clear(_items, 0, size); // Clear the elements so that the gc can reclaim the references.
//            }
//        }
//
//        // Contains returns true if the specified element is in the UncheckedList.
//        // It does a linear, O(n) search.  Equality is determined by calling
//        // EqualityComparer<T>.Default.Equals().
//
//        public bool Contains(T item)
//        {
//            // PERF: IndexOf calls Array.IndexOf, which internally
//            // calls EqualityComparer<T>.Default.IndexOf, which
//            // is specialized for different types. This
//            // boosts performance since instead of making a
//            // virtual method call each iteration of the loop,
//            // via EqualityComparer<T>.Default.Equals, we
//            // only make one virtual call to EqualityComparer.IndexOf.
//
//            return _size != 0 && IndexOf(item) != -1;
//        }
//
//        // Copies this UncheckedList into array, which must be of a 
//        // compatible array type.  
//        //
//        public void CopyTo(T[] array)
//        {
//            CopyTo(array, 0);
//        }
//
//        // Copies a section of this list to the given array at the given index.
//        // 
//        // The method uses the Array.Copy method to copy the elements.
//        // 
//        public void CopyTo(int index, T[] array, int arrayIndex, int count)
//        {
//            // Delegate rest of error checking to Array.Copy.
//            Array.Copy(_items, index, array, arrayIndex, count);
//        }
//
//        public void CopyTo(T[] array, int arrayIndex)
//        {
//            // Delegate rest of error checking to Array.Copy.
//            Array.Copy(_items, 0, array, arrayIndex, _size);
//        }
//
//        // Ensures that the capacity of this list is at least the given minimum
//        // value. If the current capacity of the list is less than min, the
//        // capacity is increased to twice the current capacity or to min,
//        // whichever is larger.
//        private void EnsureCapacity(int min)
//        {
//            if (_items.Length < min)
//            {
//                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
//                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
//                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
//                if ((uint)newCapacity > Int32.MaxValue) newCapacity = Int32.MaxValue;
//                if (newCapacity < min) newCapacity = min;
//                Capacity = newCapacity;
//            }
//        }
//        // Returns an enumerator for this list with the given
//        // permission for removal of elements. If modifications made to the list 
//        // while an enumeration is in progress, the MoveNext and 
//        // GetObject methods of the enumerator will throw an exception.
//        //
//        public Enumerator GetEnumerator()
//        {
//            return new Enumerator(this);
//        }
//
//        IEnumerator<T> IEnumerable<T>.GetEnumerator()
//        {
//            return new Enumerator(this);
//        }
//
//        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
//        {
//            return new Enumerator(this);
//        }
//        // Returns the index of the first occurrence of a given value in a range of
//        // this list. The list is searched forwards from beginning to end.
//        // The elements of the list are compared to the given value using the
//        // Object.Equals method.
//        // 
//        // This method uses the Array.IndexOf method to perform the
//        // search.
//        // 
//        public int IndexOf(T item)
//        {
//            return Array.IndexOf(_items, item, 0, _size);
//        }
//
//        // Returns the index of the first occurrence of a given value in a range of
//        // this list. The list is searched forwards, starting at index
//        // index and ending at count number of elements. The
//        // elements of the list are compared to the given value using the
//        // Object.Equals method.
//        // 
//        // This method uses the Array.IndexOf method to perform the
//        // search.
//        // 
//        public int IndexOf(T item, int index)
//        {
//            return Array.IndexOf(_items, item, index, _size - index);
//        }
//
//        // Returns the index of the first occurrence of a given value in a range of
//        // this list. The list is searched forwards, starting at index
//        // index and upto count number of elements. The
//        // elements of the list are compared to the given value using the
//        // Object.Equals method.
//        // 
//        // This method uses the Array.IndexOf method to perform the
//        // search.
//        // 
//        public int IndexOf(T item, int index, int count)
//        {
//            return Array.IndexOf(_items, item, index, count);
//        }
//
//        // Inserts an element into this list at a given index. The size of the list
//        // is increased by one. If required, the capacity of the list is doubled
//        // before inserting the new element.
//        // 
//        public void Insert(int index, T item)
//        {
//            if (_size == _items.Length) EnsureCapacity(_size + 1);
//            if (index < _size)
//            {
//                Array.Copy(_items, index, _items, index + 1, _size - index);
//            }
//            _items[index] = item;
//            _size++;
//            _version++;
//        }
//
//        // Removes the element at the given index. The size of the list is
//        // decreased by one.
//        // 
//        public bool Remove(T item)
//        {
//            int index = IndexOf(item);
//            if (index >= 0)
//            {
//                RemoveAt(index);
//                return true;
//            }
//
//            return false;
//        }
//
//        // Removes the element at the given index. The size of the list is
//        // decreased by one.
//        // 
//        public void RemoveAt(int index)
//        {
//            _size--;
//            if (index < _size)
//            {
//                Array.Copy(_items, index + 1, _items, index, _size - index);
//            }
//            if (!typeof(T).IsValueType)
//            {
//                _items[_size] = default(T);
//            }
//            _version++;
//        }
//
//        // Inserts the elements of the given collection at a given index. If
//        // required, the capacity of the list is increased to twice the previous
//        // capacity or the new size, whichever is larger.  Ranges may be added
//        // to the end of the list by setting index to the UncheckedList's size.
//        //
//        public void InsertRange(int index, IEnumerable<T> collection)
//        {
//            ICollection<T> c = collection as ICollection<T>;
//            if (c != null)
//            {    // if collection is ICollection<T>
//                int count = c.Count;
//                if (count > 0)
//                {
//                    EnsureCapacity(_size + count);
//                    if (index < _size)
//                    {
//                        Array.Copy(_items, index, _items, index + count, _size - index);
//                    }
//
//                    // If we're inserting a UncheckedList into itself, we want to be able to deal with that.
//                    if (this == c)
//                    {
//                        // Copy first part of _items to insert location
//                        Array.Copy(_items, 0, _items, index, index);
//                        // Copy last part of _items back to inserted location
//                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
//                    }
//                    else
//                    {
//                        c.CopyTo(_items, index);
//                    }
//                    _size += count;
//                }
//            }
//            else if (index < _size)
//            {
//                // We're inserting a lazy enumerable. Call Insert on each of the constituent items.
//                using (IEnumerator<T> en = collection.GetEnumerator())
//                {
//                    while (en.MoveNext())
//                    {
//                        Insert(index++, en.Current);
//                    }
//                }
//            }
//            else
//            {
//                // We're adding a lazy enumerable because the index is at the end of this list.
//                AddEnumerable(collection);
//            }
//            _version++;
//        }
//
//        // Removes a range of elements from this list.
//        // 
//        public void RemoveRange(int index, int count)
//        {
//            if (count > 0)
//            {
//                int i = _size;
//                _size -= count;
//                if (index < _size)
//                {
//                    Array.Copy(_items, index + count, _items, index, _size - index);
//                }
//
//                _version++;
//                if (!typeof(T).IsValueType)
//                {
//                    Array.Clear(_items, _size, count);
//                }
//            }
//        }
//        // Reverses the elements in this list.
//        public void Reverse()
//        {
//            Reverse(0, Count);
//        }
//
//        // Reverses the elements in a range of this list. Following a call to this
//        // method, an element in the range given by index and count
//        // which was previously located at index i will now be located at
//        // index index + (index + count - i - 1).
//        // 
//        public void Reverse(int index, int count)
//        {
//            if (count > 1)
//            {
//                Array.Reverse(_items, index, count);
//            }
//            _version++;
//        }
//
//        // Sorts the elements in this list.  Uses the default comparer and 
//        // Array.Sort.
//        public void Sort()
//        {
//            Sort(0, Count, null);
//        }
//
//        // Sorts the elements in this list.  Uses Array.Sort with the
//        // provided comparer.
//        public void Sort(IComparer<T> comparer)
//        {
//            Sort(0, Count, comparer);
//        }
//
//        // Sorts the elements in a section of this list. The sort compares the
//        // elements to each other using the given IComparer interface. If
//        // comparer is null, the elements are compared to each other using
//        // the IComparable interface, which in that case must be implemented by all
//        // elements of the list.
//        // 
//        // This method uses the Array.Sort method to sort the elements.
//        // 
//        public void Sort(int index, int count, IComparer<T> comparer)
//        {
//            if (count > 1)
//            {
//                Array.Sort<T>(_items, index, count, comparer);
//            }
//            _version++;
//        }
//
//        class DefaultComparer<CT> : IComparer<CT>
//        {
//            public Comparison<CT> CompareFunc { get; set; }
//            public int Compare(CT x, CT y)
//            {
//                return CompareFunc(x, y);
//            }
//        }
//
//        static readonly DefaultComparer<T> defaultComparer = new DefaultComparer<T>();
//
//        public void Sort(Comparison<T> comparison)
//        {
//            if (_size > 1)
//            {
//                defaultComparer.CompareFunc = comparison;
//                Array.Sort<T>(_items, 0, _size, defaultComparer);
//            }
//            _version++;
//        }
//
//        private void AddEnumerable(IEnumerable<T> enumerable)
//        {
//            //Debug.Assert(enumerable != null);
//            //Debug.Assert(!(enumerable is ICollection<T>), "We should have optimized for this beforehand.");
//
//            using (IEnumerator<T> en = enumerable.GetEnumerator())
//            {
//                _version++; // Even if the enumerable has no items, we can update _version.
//
//                while (en.MoveNext())
//                {
//                    // Capture Current before doing anything else. If this throws
//                    // an exception, we want to make a clean break.
//                    T current = en.Current;
//
//                    if (_size == _items.Length)
//                    {
//                        EnsureCapacity(_size + 1);
//                    }
//
//                    _items[_size++] = current;
//                }
//            }
//        }
//
//        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
//        {
//            private ValueTypeList<T> list;
//            private int index;
//            private int version;
//            private T current;
//
//            internal Enumerator(ValueTypeList<T> list)
//            {
//                this.list = list;
//                index = 0;
//                version = list._version;
//                current = default(T);
//            }
//
//            public void Dispose()
//            {
//            }
//
//            public bool MoveNext()
//            {
//                ValueTypeList<T> localUncheckedList = list;
//
//                if (version == localUncheckedList._version && ((uint)index < (uint)localUncheckedList._size))
//                {
//                    current = localUncheckedList._items[index];
//                    index++;
//                    return true;
//                }
//                return MoveNextRare();
//            }
//
//            private bool MoveNextRare()
//            {
//                index = list._size + 1;
//                current = default(T);
//                return false;
//            }
//
//            public T Current
//            {
//                get
//                {
//                    return current;
//                }
//            }
//
//            Object System.Collections.IEnumerator.Current
//            {
//                get
//                {
//                    return Current;
//                }
//            }
//
//            void System.Collections.IEnumerator.Reset()
//            {
//                index = 0;
//                current = default(T);
//            }
//        }
//    }
//}
