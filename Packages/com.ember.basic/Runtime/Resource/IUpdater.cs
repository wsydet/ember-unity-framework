//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.basic
//// Primary author: qinho
//
//using System;
//
//namespace Burner.Basic
//{
//    public interface IUpdater
//    {
//        /// <summary>
//        /// update every game rendering frame
//        /// </summary>
//        /// <returns>
//        /// whether this updater was invalid to need update
//        /// true: it's invalid, should be removed from updaters list, don't need to update any more
//        /// </returns>
//        bool Update();
//
//        /// <summary>
//        /// this updater is called before or after ResourceManager.UpdateAllAsyncLoadResHandle
//        /// </summary>
//        /// <returns></returns>
//        bool PreOrPostAsyncList { get; }
//
//        /// <summary>
//        /// get the priority of resources load for sort
//        /// </summary>
//        int Priority { get; }
//    }
//
//    public interface IDelayDisposable : IDisposable
//    {
//        /// <summary>
//        /// returning true means it cannot be disposed yet
//        /// </summary>
//        /// <returns></returns>
//        bool NotYet();
//    }
//}