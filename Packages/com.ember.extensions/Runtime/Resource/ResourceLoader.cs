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
//namespace Burner.Extensions
//{
//    /// <summary>
//    /// 满足BurnerUI的批量Load功能，同时保证与资源管理本身解耦。或者也可以自行实现这套逻辑
//    /// </summary>
//    public class ResourceLoader : IResourceLoader
//    {
//        // 事件通知
//        public event System.Action<ResourceLoader, bool> OnDisposedEvent;
//        
//        protected Action _func;
//        
//        protected readonly List<ILoaderHandle> _loaders = new List<ILoaderHandle>();
//        
//        protected string _name = string.Empty;
//        protected bool _onResourceLoaded = true;
//        
//        protected bool _isRecord = false;
//
//        public string Name => _name;
//        
//        public bool Disposed { get; private set; }
//        
//        [ForTest]
//        public static int AllLoadersCount { get; private set; }
//        
//        /// <summary>
//        /// priority of loading resource
//        /// </summary>
//        public int Priority { get; }
//
//        /// <summary>
//        /// this refInfo must called after ResourceManager.UpdateAllAsyncLoadResHandle
//        /// </summary>
//        public bool PreOrPostAsyncList => false;
//        
//        internal ResourceLoader()
//        {
//            AllLoadersCount++;
//        }
//
//        public ResourceLoader(string name) : this()
//        {
//            _name = name;
//        }  
//
//        ~ResourceLoader()
//        {
//            DisposeImpl(true);
//        }
//        
//        public bool Update()
//        {
//            CheckFinish();
//            return _func == null || Disposed;
//        }
//        
//        protected bool CheckLoaderFinish()
//        {
//            bool allLoaded = true;
//            for(int i = 0; i < _loaders.Count; i++)
//            {
//                var handle = _loaders[i];
//                if(handle.Ready() || handle.IsDisposed())
//                {
//                    _loaders.RemoveAt(i--);
//                }
//                else
//                {
//                    allLoaded = false;
//                }
//            }
//
//            return allLoaded;
//        }
//
//        protected void OnExecuteFinish()
//        {
//            if(!_onResourceLoaded)
//            {
//                Debug.LogWarning($"[Burner]: Loader '{_name}' doesn't get any calling of AssetLoadAsync between BeginRecord and EndRecord");
//            }
//
//            EndRecord();
//                    
//            try
//            {
//                _func?.Invoke();
//            }
//            catch(Exception ex)
//            {
//                Debug.LogException(ex);
//            }
//            finally
//            {
//                _func = null;    
//            }
//        }
//        
//        public virtual void CheckFinish()
//        {
//            if(_func != null)
//            {
//                bool allLoaded = CheckLoaderFinish();
//                if(allLoaded)
//                {
//                    OnExecuteFinish();
//                }
//            }
//        }
//        
//        public void OnFinish(Action func)
//        {
//            if(Disposed)
//            {
//                throw new Exception($"[Burner]: Loader '{_name}' has been disposed, please use a new one");
//            }
//
//            if(_func != null && func != null)
//            {
//                throw new Exception("[Burner]: Can not set OnFinish twice");
//            }
//
//            _func = func;
//        }
//        
//        /// <summary>
//        /// 在LoadAssetAsync中的回调监听返回，只记录IResourceHandle相关
//        /// </summary>
//        /// <param name="resHandle"></param>
//        public void OnBeginLoadResource(IResourceHandle resHandle)
//        {
//            _onResourceLoaded = true;
//
//            // 开始记录的，那么操作的Handle准备开始添加到列表
//            if (_isRecord)
//            {
//                // 已经Ready的就不记录
//                if(!resHandle.Ready())
//                {
//                    _loaders.Add(resHandle);
//                }
//            }
//        }
//
//        public void BeginRecord(bool order = false)
//        {
//            if(Disposed)
//            {
//                throw new Exception($"[Burner]: Loader '{_name}' has been disposed, please use a new one");
//            }
//
//            _isRecord = true;
//            _onResourceLoaded = false;
//        }
//        
//        /// <summary>
//        /// 结束记录，在Begin和End之间的LoadAsset都会被记录
//        /// </summary>
//        public void EndRecord()
//        {
//            _isRecord = false;
//            _onResourceLoaded = true;
//        }
//
//        /// <summary>
//        /// 可以监听其他自定义实现类
//        /// </summary>
//        /// <param name="handle"></param>
//        /// <exception cref="Exception"></exception>
//        /// <exception cref="ArgumentNullException"></exception>
//        public void ListenHandle(ILoaderHandle handle)
//        {
//            if(Disposed)
//            {
//                throw new Exception($"[Burner]: Loader '{_name}' has been disposed, please use a new one");
//            }
//
//            if(handle == null)
//            {
//                throw new ArgumentNullException();
//            }
//
//            if(handle.Ready()) return;
//            if(_loaders.Contains(handle)) return;
//
//            _loaders.Add(handle);
//        }
//
//        public void Dispose() => DisposeImpl(false);
//
//        protected virtual void DisposeImpl(bool fromFinalizer)
//        {
//            if(Disposed) return;
//
//            if(fromFinalizer)
//            {
//                // 需要通知外部，然后通过外部统一进行Dispose逻辑。
//                // 但是因为C#的Finalizer是在另外的线程，所以正常要加入主线程队列才好。原Loader是放到ResourceManager的延迟队列去处理。
//            }
//            else
//            {
//                Disposed = true;
//                GC.SuppressFinalize(this);
//
//                EndRecord();
//                _loaders.Clear();
//                _func = null;
//                AllLoadersCount--;
//            }
//            OnDisposedEvent?.Invoke(this, fromFinalizer);
//        }
//    }
//}
