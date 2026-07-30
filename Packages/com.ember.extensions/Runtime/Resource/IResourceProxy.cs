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
//    /// <summary>
//    /// 资源句柄，有利于外部进行自定义控制。其次
//    /// </summary>
//    public interface IResourceHandle : ILoaderHandle
//    {
//        public string ResName { get; }
//        public string ResFullName { get; }
//        public object ResObject { get; }
//        public T GetResObject<T>() where T : UnityEngine.Object;
//
//        // public bool IsDisposed { get; }
//        public void Dispose();
//    }
//
//    /// <summary>
//    /// 定义跨Package使用的资源管理代理类，实现隔离Burner资源管理和BurnerUI、BohUGUIExt等package的强引用，以便不同的项目组可新入新的资源管理器。
//    /// </summary>
//    public interface IResourceProxy
//    {
//        string GetPath(string resName);
//        bool Contains(string resName);
//        IResourceHandle LoadAssetAsync(string resName, Action<IResourceHandle> callback, object args = null);
//        
//        /// <summary>
//        /// args，一般不使用，不过Burner的资源管理在不同类型有不同用途，比如GameObject的时候可以传入parent的变量
//        /// </summary>
//        /// <param name="resName"></param>
//        /// <param name="callback"></param>
//        /// <param name="args"></param>
//        /// <typeparam name="T"></typeparam>
//        /// <returns></returns>
//        IResourceHandle LoadAssetAsync<T>(string resName, Action<T, IResourceHandle> callback, object args = null) where T : UnityEngine.Object;
//        void UnloadAsset(IResourceHandle resHandle);
//        void UnloadUnusedAssets();
//        void Dispose();
//
//        // 创建批量监控的Loader
//        IResourceLoader CreateLoader(string name);
//        void DeleteLoader(IResourceLoader loader);
//    }
//    
//    public static class ResourceEngine
//    {
//        public static bool EnableWarning = false;
//
//        // 日志标签
//        private static readonly string LogTag = $"[{nameof(ResourceEngine)}]:";
//        
//        private static IResourceProxy _proxy;
//        public static IResourceProxy Proxy 
//        {
//            get
//            {
//#if UNITY_EDITOR
//                if (EnableWarning && null == _proxy)
//                {
//                    UnityEngine.Debug.LogWarning($"{LogTag} Not initialize IResourceProxy, but has call it");
//                }
//#endif
//
//                return _proxy;
//            }
//            private set
//            {
//                _proxy = value;
//            }
//        }
//
//        public static void InitProxy(IResourceProxy impl)
//        {
//#if UNITY_EDITOR
//            if (EnableWarning && null != Proxy && Proxy.GetType().Name.Equals(impl.GetType().Name))
//            {
//                UnityEngine.Debug.LogWarning($"{LogTag} InitProxy same {impl.GetType().Name}");
//            }
//#endif
//            
//            Proxy = impl;
//        }
//    
//        public static void DisposeProxy()
//        {
//            Proxy?.Dispose();
//            Proxy = null;
//        }
//    }
//}
//
