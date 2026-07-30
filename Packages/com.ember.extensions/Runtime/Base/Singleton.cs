//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using System;
//using UnityEngine;
//
//namespace Burner.Extensions
//{
//    public class Singleton<T>: IDisposable where T : class, IDisposable, new()
//    {
//        private static T _instance;
//
//        public static T Instance
//        {
//            get
//            {
//                if (_instance == null)
//                {
//                    _instance = new T();
//                }
//                return _instance;
//            }
//        }
//
//        public virtual void Dispose()
//        {
//            _instance = null;
//            GC.SuppressFinalize(this);
//        }
//
//        public static bool HasInstance()
//        {
//            return _instance != null;
//        }
//
//        public static void DestroyInstance()
//        {
//            if (_instance == null) return;
//
//            Debug.Log($"Destroying instance of {_instance.GetType().Name}");
//
//            _instance.Dispose();
//            _instance = null;
//        }
//
//        ~Singleton()
//        {
//            Dispose();
//        }
//    }
//}
