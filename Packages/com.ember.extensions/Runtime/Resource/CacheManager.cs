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
//using System.Collections.Generic;
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using Burner.Extensions;
//using UnityEngine;
//
//namespace Burner.Extensions.Resource
//{
//    public class CacheManager
//    {
//        static CacheManager instance = new CacheManager();
//        public static CacheManager Instance => instance;
//        class CacheInfo
//        {
//            public MemoryPool<IResourceHandle> Pool;
//            public int PreserveCount;
//            public DateTime TouchTime;
//            public bool IsAsset;
//            public List<KeyValuePair<IResourceHandle, Action<IResourceHandle>>> PendingCallback;
//        }
//        Dictionary<string, CacheInfo> objectPool = new Dictionary<string, CacheInfo>();
//        public float CacheTime { get; set; } = 30;
//
//        public int MaximalCacheCount { get; set; } = 15;
//        public IResourceHandle GetObject(string prefabName, Action<IResourceHandle> onLoad, GameObject parent = null)
//        {
//            var veryfiedName = VerifyResName(prefabName);
//
//            if (objectPool.TryGetValue(veryfiedName, out var info))
//            {
//                IResourceHandle res = info.Pool.Alloc();
//                if (res != null)
//                {
//                    if (info.Pool.Count > 0)
//                        info.TouchTime = DateTime.Now;
//                    if (onLoad != null)
//                    {
//                        if (res.ResObject != null)
//                            onLoad(res);
//                        else
//                        {
//                            if (info.PendingCallback == null)
//                                info.PendingCallback = new List<KeyValuePair<IResourceHandle, Action<IResourceHandle>>>();
//                            info.PendingCallback.Add(new KeyValuePair<IResourceHandle, Action<IResourceHandle>>(res, onLoad));
//                        }
//                    }
//                    return res;
//                }
//            }
//            return ResourceEngine.Proxy.LoadAssetAsync<GameObject>(veryfiedName, (go, handle) =>
//            {
//                onLoad?.Invoke(handle);
//            }, parent);
//        }
//
//        public void ReleaseObject(string prefabName, IResourceHandle obj)
//        {
//            
//            if(obj == null)
//            {
//                Debug.LogWarning("obj cannot be null, prefabName=" + prefabName);
//                return;
//            }
//            var veryfiedName = VerifyResName(prefabName);
//            if (objectPool.TryGetValue(veryfiedName, out var info))
//            {
//                if (info.Pool.Contains(obj))
//                    return;
//            }
//            else
//            {
//                info = new CacheInfo();
//                info.Pool = new MemoryPool<IResourceHandle>();
//                objectPool.Add(veryfiedName, info);
//            }
//            if (!info.Pool.Free(obj))
//            {
//                //缓存已满
//                obj.Dispose();
//            }
//            info.TouchTime = DateTime.Now;
//            CheckAndReleasePool();
//            
//            if (obj.ResObject is GameObject go)
//            {
//                var t = go.transform;
//                go.SetActive(false);
//                t.SetParent(null);
//                t.localScale = Vector3.one;
//                t.localRotation = Quaternion.identity;
//                t.localPosition = Vector3.zero;
//            }
//        }
//
//        void CheckAndReleasePool()
//        {
//            int exceedCnt = CountAliveResCount() - MaximalCacheCount;
//            if (exceedCnt > 0)
//            {
//                do
//                {
//                    DateTime oldestTime = default;
//                    CacheInfo oldestInfo = null;
//                    foreach (var i in objectPool)
//                    {
//                        var cur = i.Value;
//                        if (cur.Pool.Count < 1 || cur.PreserveCount > 0 || cur.IsAsset)
//                            continue;
//                        if (cur.TouchTime < oldestTime || oldestInfo == null)
//                        {
//                            oldestInfo = cur;
//                            oldestTime = cur.TouchTime;
//                        }
//                    }
//                    if (oldestInfo == null)
//                        break;
//                    var pool = oldestInfo.Pool;
//                    while (pool.Count > 0)
//                    {
//                        var res = pool.Alloc();
//                        res.Dispose();
//                    }
//                    exceedCnt--;
//                } while (exceedCnt > 0);
//            }
//        }
//
//        int CountAliveResCount()
//        {
//            int res = 0;
//            foreach(var i in objectPool)
//            {
//                if (i.Value.IsAsset)
//                    continue;
//                if (i.Value.Pool.Count > 0 || i.Value.PreserveCount > 0)
//                    res++;
//            }
//
//            return res;
//        }
//
//        public void Dispose()
//        {
//            foreach(var i in objectPool)
//            {
//                var pool = i.Value.Pool;
//                while (pool.Count > 0)
//                {
//                    var res = pool.Alloc();
//                    res.Dispose();
//                }
//
//                if(i.Value.PendingCallback != null)
//                {
//                    foreach(var j in i.Value.PendingCallback)
//                    {
//                        j.Key.Dispose();
//                    }
//                    i.Value.PendingCallback.Clear();
//                }               
//            }
//            objectPool.Clear();
//        }
//
//        public void ClearCache()
//        {
//            foreach (var i in objectPool)
//            {
//                var info = i.Value;
//                while (info.Pool.Count > info.PreserveCount)
//                {
//                    var handle = info.Pool.Alloc();
//                    handle.Dispose();
//                }
//            }
//        }
//
//        string VerifyResName(string name)
//        {
//            if (Utility.HasUpperChar(name))
//            {
//                name = name.ToLower();
//            }
//            return name;
//        }
//
//        public void ReleasePreserve(string prefabName)
//        {
//            var veryfiedName = VerifyResName(prefabName);
//            if (objectPool.TryGetValue(VerifyResName(veryfiedName), out var info))
//            {
//                info.PreserveCount--;
//                if (info.PreserveCount < 0)
//                    info.PreserveCount = 0;
//            }
//        }
//
//        public void PreloadObject(string prefabName, bool preserve = false, Action onDone = null, GameObject parent = null)
//        {
//            DoPreload(prefabName, true, preserve, onDone, false, parent);
//        }
//
//        public void PreloadAsset(string prefabName, bool preserve = false, Action onDone = null, GameObject parent = null)
//        {
//            DoPreload(prefabName, false, preserve, onDone, true, parent);
//        }
//
//        void DoPreload(string prefabName, bool isPrefab, bool preserve, Action onDone, bool isAsset, GameObject parent = null)
//        {
//            var veryfiedName = VerifyResName(prefabName);
//
//            if (!objectPool.TryGetValue(VerifyResName(veryfiedName), out var info))
//            {
//                info = new CacheInfo();
//                info.IsAsset = isAsset;
//                info.Pool = new MemoryPool<IResourceHandle>();
//                objectPool.Add(veryfiedName, info);
//            }
//            if (info.Pool.Count <= 0)
//            {
//                if (info.Pool.CanFree)
//                {
//                    if (isPrefab)
//                    {
//                        ResourceEngine.Proxy.LoadAssetAsync<GameObject>(veryfiedName, (go, handle) =>
//                        {
//                            info.TouchTime = DateTime.Now;
//                            info.Pool.Free(handle);
//                            if (preserve)
//                            {
//                                info.PreserveCount = Mathf.Min(info.PreserveCount + 1, info.Pool.Count);
//                            }
//                            onDone?.Invoke();
//                        }, parent);
//                    }
//                    else
//                    {
//                        ResourceEngine.Proxy.LoadAssetAsync<UnityEngine.Object>(veryfiedName, (asset, handle) =>
//                        {
//                            info.TouchTime = DateTime.Now;
//                            info.Pool.Free(handle);
//                            if (preserve)
//                            {
//                                info.PreserveCount = Mathf.Min(info.PreserveCount + 1, info.Pool.Count);
//                            }
//                            onDone?.Invoke();
//                        }, parent);
//                    }
//                }
//            }
//            else
//            {
//                info.TouchTime = DateTime.Now;
//                if (preserve)
//                {
//                    info.PreserveCount = Mathf.Min(info.PreserveCount + 1, info.Pool.Count);
//                }
//                onDone?.Invoke();
//            }
//        }
//
//        public void Update()
//        {
//            foreach (var i in objectPool)
//            {
//                var info = i.Value;
//                if (info.Pool.Count > info.PreserveCount)
//                {
//                    var elapsedTime = DateTime.Now - info.TouchTime;
//                    if (elapsedTime.TotalSeconds > CacheTime)
//                    {
//                        while (info.Pool.Count > info.PreserveCount)
//                        {
//                            var handle = info.Pool.Alloc();
//                            handle.Dispose();
//                        }
//                    }
//                }
//                if (info.PendingCallback != null)
//                {
//                    for (int j = 0; j < info.PendingCallback.Count; j++)
//                    {
//                        var kv = info.PendingCallback[j];
//                        if (kv.Key.ResObject != null)
//                        {
//                            kv.Value(kv.Key);
//                            int lastIdx = info.PendingCallback.Count - 1;
//                            info.PendingCallback[j] = info.PendingCallback[lastIdx];
//                            info.PendingCallback.RemoveAt(lastIdx);
//                            j--;
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
