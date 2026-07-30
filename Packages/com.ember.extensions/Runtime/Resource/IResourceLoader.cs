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
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using Burner.Extensions;
//using UnityEngine;
//
//namespace Burner.Extensions
//{
//    public interface IResourceLoader : IUpdater, IDisposable
//    {
//        string Name { get; }
//        
//        void CheckFinish();
//        void OnFinish(System.Action action);
//        void BeginRecord(bool order);
//        void EndRecord();
//
//        void ListenHandle(ILoaderHandle handle);
//    }
//}