//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using UnityEngine;
//
//using Burner.Basic;
//using Burner.Extensions;
//using Burner.Basic.Tasks;
//using UnityEngine.Profiling;
//
//namespace Burner.UIExtension
//{
//    public class GameUIAttachment : GameUIComponent
//    {
//        string assetName;
//        IResourceHandle handle;
//        bool disableCache;
//        protected Transform trans;
//        Vector3 scale = Vector3.one;
//        GameUIComponent parent;
//        bool isLoading;
//        bool isUILogic;
//        GameUILogic attachmentLogic;
//        Action<GameUILogic> loadCompleteCB;
//
//        internal Action<GameUIAttachment> OnLoaded { get; set; }
//        internal Action<GameUILogic> OnUILogicLoaded { get; set; }
//
//        internal GameUIAttachment(GameUIComponent parent, string prefabName, bool disableCache, bool isUILogic)
//        {
//            this.parent = parent;
//            this.assetName = prefabName;
//            this.disableCache = disableCache;
//            this.isUILogic = isUILogic;
//        }
//
//        public Vector3 Scale
//        {
//            get => trans ? trans.localScale : scale;
//            set
//            {
//                scale = value;
//                if (trans)
//                    trans.localScale = value;
//            }
//        }
//
//        internal void Load()
//        {
//            if (!isLoading)
//            {
//                isLoading = true;
//                if (disableCache)
//                    handle = ResourceEngine.Proxy.LoadAssetAsync<GameObject>(assetName, (go, handle) =>
//                    {
//                        OnLoadRes(handle);
//                    }, parent.GameObject);
//                else
//                    handle = CacheManager.Instance.GetObject(assetName, OnLoadRes, parent.GameObject);
//            }
//            else
//                throw new NotSupportedException("Cannot load the prefab twice");
//        }
//
//        public STTask<T> GetUILogicAsync<T>() where T : GameUILogic
//        {
//            if (!isUILogic)
//                throw new NotSupportedException($"{assetName} is not a ui attachment");
//            if (isLoading)
//            {
//                STTaskCompletionSource<T> tcs = new STTaskCompletionSource<T>();
//                loadCompleteCB = (logic) => tcs.SetResult(logic as T);
//                return tcs.Task;
//            }
//            else
//            {
//                return STTask.FromResult(attachmentLogic as T);
//            }
//        }
//
//        public async void GetUILogicAsync<T>(Action<T> cb) where T : GameUILogic
//        {
//            if (cb == null)
//                return;
//            var logic = await GetUILogicAsync<T>();
//            cb.Invoke(logic);
//        }
//
//        public override void OnDispose()
//        {
//            if (isUILogic && attachmentLogic != null)
//            {
//                attachmentLogic.DoDispose(true);
//            }
//            if (handle != null && !isLoading)
//            {
//                if (disableCache)
//                    handle.Dispose();
//                else
//                    CacheManager.Instance.ReleaseObject(assetName, handle);
//            }
//            handle = null;
//        }
//
//        public void Dispose()
//        {
//            parent.RemoveAttachment(this);
//        }
//
//        void OnLoadRes(IResourceHandle handle)
//        {
//            isLoading = false;
//            if (Disposed)
//            {
//                if (handle != null)
//                {
//                    if (disableCache)
//                        handle.Dispose();
//                    else
//                        CacheManager.Instance.ReleaseObject(assetName, handle);
//                }
//                this.handle = null;
//                return;
//            }
//            if (this.handle != null && this.handle != handle)
//                throw new Exception("??????? 黑人问号");
//            var go = handle.ResObject as GameObject;
//            if (!go)
//            {
//                throw new Exception("从缓存池拿出来的对象居然GameObject为空");
//            }
//            if (UILogic != null)
//                GameObjectUtils.SetLayer(go, UILogic.Layer);
//            trans = go.transform;
//            trans.SetParent(parent.Widget);
//            trans.localPosition = Vector3.zero;
//            trans.localScale = scale;
//            trans.localRotation = Quaternion.identity;
//            Initialize(go);
//            if (isUILogic)
//            {
//                GameUIBinding binding = go.GetComponent<GameUIBinding>();
//                if (binding)
//                {
//                    attachmentLogic = UILogic?.DoCreateNewLogicFromBinding(binding);
//                }
//                else
//                {
//                    Burner.Logger.Error($"Trying to load {assetName} as UILogic, but doesn't contain GameUIBinding");
//                }
//            }
//            go.SetActive(true);
//            OnLoaded?.Invoke(this);
//            if (isUILogic)
//            {
//                OnUILogicLoaded?.Invoke(attachmentLogic);
//                loadCompleteCB?.Invoke(attachmentLogic);
//                loadCompleteCB = null;
//            }
//        }
//
//        public override void OnUpdate()
//        {
//            Profiler.BeginSample("GameUIAttachment_OnUpdate");
//            base.OnUpdate();
//            Profiler.BeginSample("GameUIAttachment_AttachmentLogicDoUpdate");
//            attachmentLogic?.DoUpdate();
//            Profiler.EndSample();
//            Profiler.EndSample();
//        }
//
//        public override void OnLateUpdate()
//        {
//            Profiler.BeginSample("GameUIAttachment_OnLateUpdate");
//            base.OnLateUpdate();
//            Profiler.BeginSample("GameUIAttachment_AttachmentLogicDoLateUpdate");
//            attachmentLogic?.DoLateUpdate();
//            Profiler.EndSample();
//            Profiler.EndSample();
//        }
//    }
//}
