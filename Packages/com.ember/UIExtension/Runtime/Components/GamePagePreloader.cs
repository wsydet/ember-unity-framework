//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using Burner.Extensions;
//using Burner.Basic.Tasks;
//using System;
//using System.Collections.Generic;
//using Burner.Basic;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.Profiling;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class GamePagePreloader : GameUIComponent
//    {
//        PagePreloader loader;
//        List<IResourceHandle> handles;
//        int[] handleStartIdx;
//        public override void OnInit()
//        {
//            loader = GetComponent<PagePreloader>();
//        }
//        public override IBindlessUIBehaviour BindlessComponent => loader;
//
//        public override bool NeedPreload => true;
//        public override void OnPreload()
//        {
//            if (handles != null)
//                return;
//            Profiler.BeginSample("GamePagePreloader.OnPreload");
//            try
//            {
//                
//                IResourceLoader resLoader = ResourceEngine.Proxy.CreateLoader("GamePageLoader.ResourceLoader");
//                resLoader.BeginRecord(false);
//                resLoader.OnFinish(AssignResources);
//                
//                if (loader.PreloadInfos.Length > 0)
//                {
//                    handles = new List<IResourceHandle>();
//                    handleStartIdx = new int[loader.PreloadInfos.Length];
//                
//                    var arr = loader.PreloadInfos;
//                    var resMgr = ResourceEngine.Proxy;
//                    for (int i = 0; i < arr.Length; i++)
//                    {
//                        handleStartIdx[i] = handles.Count;
//                
//                        var info = arr[i];
//                        var assets = info.AssetsToLoad;
//                        for (int j = 0; j < assets.Length; j++)
//                        {
//                            if (string.IsNullOrEmpty(assets[j]))
//                            {
//                                handles.Add(null);
//                            }
//                            else
//                            {
//                                if (info.HasMultipleSprites)
//                                {
//                                    int idx = assets[j].IndexOf("/");
//                                    if (idx > 0)
//                                        handles.Add(resMgr.LoadAssetAsync(assets[j].Substring(0, idx), null, null));
//                                    else
//                                        handles.Add(resMgr.LoadAssetAsync(assets[j],null, null));
//                                }
//                                else
//                                    handles.Add(resMgr.LoadAssetAsync(assets[j], null, null));
//                            }
//                        }
//                    }
//                }
//                resLoader.EndRecord();
//            }
//            finally
//            {
//                Profiler.EndSample();
//            }
//        }
//
//        Sprite GetSpriteFromHandle(IResourceHandle resHandle, PagePreloader.PreloadInfo info, int idx)
//        {
//            if (resHandle != null)
//            {
//                if (resHandle.ResObject is Dictionary<string, Sprite> dic)
//                {
//                    string spriteName = info.AssetsToLoad[idx].Split('/')[1];
//                    if (dic.TryGetValue(spriteName, out var s))
//                        return s;
//                }
//                else
//                    return resHandle.ResObject as Sprite;
//            }
//            return null;
//        }
//
//        void AssignResources()
//        {
//            Profiler.BeginSample("GamePagePreloader.OnBecomeVisible");
//            try
//            {
//                if (loader.PreloadInfos.Length > 0 && handleStartIdx != null)
//                {
//                    var arr = loader.PreloadInfos;
//                    for (int i = 0; i < arr.Length; i++)
//                    {
//                        var info = arr[i];
//                        int idx = handleStartIdx[i];
//                        switch (info.Type)
//                        {
//                            case PagePreloader.ComponentTypes.Image:
//                                {
//                                    if (info.Target is ImageEx ex)
//                                    {
//                                        IResourceHandle resHandle;
//                                        resHandle = handles[idx];
//                                        var sprite = GetSpriteFromHandle(resHandle, info, 0);
//                                        if (sprite)
//                                            ex.sprite = sprite;
//                                        var spriteArr = ex.GetSpriteArray();
//                                        for (int j = 1; j < info.AssetsToLoad.Length; j++)
//                                        {
//                                            resHandle = handles[idx + j];
//                                            spriteArr[j - 1] = GetSpriteFromHandle(resHandle, info, j);
//                                        }
//                                        ex.RefreshSpriteState();
//                                    }
//                                    else if (info.Target is Image img)
//                                    {
//                                        IResourceHandle resHandle = handles[idx];
//                                        var sprite = GetSpriteFromHandle(resHandle, info, 0);
//                                        if (sprite)
//                                        {
//                                            img.sprite = sprite;
//                                        }
//                                    }
//                                }
//                                break;
//                            case PagePreloader.ComponentTypes.RawImage:
//                                {
//                                    if (info.Target is RawImage img)
//                                    {
//                                        IResourceHandle resHandle = handles[idx];
//                                        if (resHandle.ResObject is Texture2D tex)
//                                            img.texture = tex;
//                                        else if (resHandle.ResObject is Sprite sprite)
//                                        {
//                                            img.texture = sprite.texture;
//                                        }
//                                    }
//                                }
//                                break;
//                        }
//                    }
//                }
//            }
//            finally
//            {
//                Profiler.EndSample();
//            }
//        }
//
//        public override void OnDispose()
//        {
//            if (handles != null)
//            {
//                foreach (var i in handles)
//                {
//                    i?.Dispose();
//                }
//                handles = null;
//                handleStartIdx = null;
//            }
//        }
//    }
//}
