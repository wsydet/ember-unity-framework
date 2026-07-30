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
//    public class MemoryPool<T> where T : class
//    {
//        public MemoryPool(int maxSize = 10)
//        {
//            _maxSize = maxSize;
//        }
//        public T Alloc()
//        {
//            T t = null;
//            if (_objs.Count > 0)
//                t = _objs.Dequeue();
//            return t;
//        }
//
//        public bool CanFree
//        {
//            get
//            {
//                return _objs.Count < _maxSize;
//            }
//        }
//        public bool Free(T t)
//        {
//            if (_objs.Count < _maxSize)
//            {
//                _objs.Enqueue(t);
//                return true;
//            }
//            return false;
//        }
//
//        public bool Contains(T t)
//        {
//            return _objs.Contains(t);
//        }
//        public void Dispose()
//        {
//            _objs.Clear();
//        }
//        public int Count { get { return _objs.Count; } }
//        private Queue<T> _objs = new Queue<T>();
//        private int _maxSize = 10;
//    }
//}
