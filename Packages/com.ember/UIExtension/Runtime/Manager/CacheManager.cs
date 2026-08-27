//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Reflection;
//
//using UnityEngine;
//using Burner.Basic;
//using Burner.Extensions;
//namespace Burner.UIExtension
//{
//    //[Obsolete("Please use Burner.Extensions.Resource.CacheManager instead")]
//    public class CacheManager
//    {
//        static CacheManager instance = new CacheManager();
//        static Burner.Extensions.Resource.CacheManager InstanceInternal => Burner.Extensions.Resource.CacheManager.Instance;
//
//        public static CacheManager Instance => instance;
//
//        public float CacheTime
//        {
//            get => InstanceInternal.CacheTime;
//            set
//            {
//                InstanceInternal.CacheTime = value;
//            }
//        }
//
//        public int MaximalCacheCount
//        {
//            get => InstanceInternal.MaximalCacheCount;
//            set
//            {
//                InstanceInternal.MaximalCacheCount = value;
//            }
//        }
//
//        public IResourceHandle GetObject(string prefabName, Action<IResourceHandle> onLoad, GameObject parent = null)
//        {
//            return InstanceInternal.GetObject(prefabName, onLoad, parent);
//        }
//
//        public void ReleaseObject(string prefabName, IResourceHandle obj)
//        {
//            InstanceInternal.ReleaseObject(prefabName, obj);
//        }
//        public void Dispose()
//        {
//            InstanceInternal.Dispose();
//        }
//
//        public void ReleasePreserve(string prefabName)
//        {
//            InstanceInternal.ReleasePreserve(prefabName);
//        }
//
//        public void PreloadObject(string prefabName, bool preserve = false, Action onDone = null)
//        {
//            InstanceInternal.PreloadObject(prefabName, preserve, onDone);
//        }
//
//        public void PreloadAsset(string prefabName, bool preserve = false, Action onDone = null)
//        {
//            InstanceInternal.PreloadAsset(prefabName, preserve, onDone);
//        }
//        public void Update()
//        {
//            InstanceInternal.Update();
//        }
//    }
//}
