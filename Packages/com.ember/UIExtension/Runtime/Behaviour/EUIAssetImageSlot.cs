using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Resource;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Ember.UIExtension
{
    /// <summary>两种 Graphic 共用的请求/资源生命周期。仅通过 EmberAssetHandleSlot 消费资源。</summary>
    internal sealed class EUIAssetImageSlot<T> where T:Object
    {
        #region 内部参数
        private readonly MonoBehaviour _owner;
        private readonly Func<T> _read;
        private readonly Action<T,bool> _write;
        private readonly Action<T> _detach;
        private T _displayOwned;
        private readonly Func<bool> _external;
        private readonly Func<string> _invalid;
        private readonly EmberAssetHandleSlot<T> _slot=new();
        private CancellationTokenRegistration _destroyRegistration;
        private readonly CancellationToken _destroyToken;
        private Monitor _monitor;
        private bool _awake, _dead, _checking;
        private long _nextId, _version;
        private Request _request;
        internal T Placeholder, Failure;
        internal bool KeepNativeSize, KeepPreviousOnFailure;
        internal EUIImageLoadingDisplay LoadingDisplay;
        internal bool IsLoading=>_request!=null;
        internal string CurrentPath=>_slot.CurrentAssetPath;
        internal string LoadingPath=>_slot.LoadingAssetPath;
        private sealed class Request
        {internal long Id;internal string Path;internal Action<EUIImageLoadResult> Callback;}
        private sealed class Monitor:IPlayerLoopItem
        {
            internal EUIAssetImageSlot<T> State;
            public bool MoveNext()
            {
                var s=State;
                if(s==null)return false;
                if(!s._owner){s.Destroyed();State=null;return false;}
                if(s._awake || s._dead || (!s._slot.IsLoading && !s._slot.CurrentAsset))
                {s._monitor=null;State=null;return false;}
                return true;
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void ObserveIfNeeded()
        {
            if(_awake || _dead || !Application.isPlaying || _monitor!=null || (!_slot.IsLoading && !_slot.CurrentAsset))return;
            _monitor=new Monitor {State=this};PlayerLoopHelper.AddAction(PlayerLoopTiming.LastPostLateUpdate,_monitor);
        }
        private void StopObservation() {if(_monitor==null)return;_monitor.State=null;_monitor=null;}
        private void Notify(Request request,EUIImageLoadStatus status,string error=null)
        {
            if(request?.Callback==null)return;
            var result=new EUIImageLoadResult(request.Id,request.Path,status,error);
            foreach(Action<EUIImageLoadResult> callback in request.Callback.GetInvocationList())
            {try{callback(result);}catch(Exception e){EmberDebug.LogWarning(LogTags.UIManager,$"Image load callback: {e}");}}
        }
        private void Finish(Request request,EUIImageLoadStatus status,string error=null)
        {if(_request!=request)return;_request=null;Notify(request,status,error);}
        private void UnmountOwned()
        {
            // loader 可能已经销毁，而 Graphic 仍存活；detach 自身负责检查实际显示目标。
            var owned=_displayOwned;_displayOwned=null;
            if(owned)_detach(owned);
        }
        private void Loaded(Request request,T asset)
        {
            if(_dead || _request!=request)return;
            CheckManual();if(_request!=request)return;
            if(!_owner){Destroyed();return;}
            var invalid=_invalid();
            if(invalid!=null){UnmountOwned();_slot.Clear();Finish(request,EUIImageLoadStatus.Failed,invalid);return;}
            if(asset)
            {
                _write(asset,KeepNativeSize);
                _displayOwned=asset;
                Finish(request,EUIImageLoadStatus.Success);
            }
            else
            {
                var error=_slot.LastError ?? "Asset load failed.";
                if(KeepPreviousOnFailure && _slot.CurrentAsset){_write(_slot.CurrentAsset,false);_displayOwned=_slot.CurrentAsset;}
                else {_write(Failure ? Failure:Placeholder,false);_displayOwned=null;_slot.Clear();}
                Finish(request,EUIImageLoadStatus.Failed,error);
            }
            ObserveIfNeeded();
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EUIAssetImageSlot(MonoBehaviour owner,Func<T> read,Action<T,bool> write,Action<T> detach,Func<bool> external,Func<string> invalid,bool awake)
        {
            _owner=owner;_read=read;_write=write;_detach=detach;_external=external;_invalid=invalid;_awake=awake;
            // 缓存 token；Unity 的 never-active OnDestroy 并不可靠，另由 PlayerLoop 观察兜底。
            _destroyToken=owner.destroyCancellationToken;
            _destroyRegistration=_destroyToken.Register(Destroyed);
        }
        internal void MarkAwake() {_awake=true;StopObservation();}
        internal void CheckManual()
        {
            if(_dead || _checking || !_owner)return;
            _checking=true;
            try {if(_external())Retire(EUIImageLoadStatus.ManualOverride,false);}
            finally {_checking=false;}
        }
        internal long Load(string path,Action<EUIImageLoadResult> completed)
        {
            CheckManual();
            var request=new Request {Id=++_nextId,Path=path,Callback=completed};
            var old=_request;_request=request;++_version;
            Notify(old,EUIImageLoadStatus.Replaced);
            if(_request!=request)return request.Id;
            if(_dead || !_owner){Finish(request,EUIImageLoadStatus.Destroyed);return request.Id;}
            // 首次与复用实例都在 inactive Bind 后才 Show。显式 Load 是新请求，不能当作隐藏重播。
            if(!_owner.enabled){Finish(request,EUIImageLoadStatus.Disabled);return request.Id;}
            if(string.IsNullOrEmpty(path))
            {
                _write(null,false);_displayOwned=null;_slot.Clear();StopObservation();Finish(request,EUIImageLoadStatus.Cleared);return request.Id;
            }
            var invalid=_invalid();
            if(invalid!=null){_slot.CancelLoading();Finish(request,EUIImageLoadStatus.Failed,invalid);return request.Id;}
            if(LoadingDisplay==EUIImageLoadingDisplay.Placeholder)_write(Placeholder,false);
            _slot.LoadAsync(path,asset=>Loaded(request,asset));
            ObserveIfNeeded();return request.Id;
        }
        internal void CancelLoading()
        {
            CheckManual();
            var request=_request;_request=null;var version=++_version;_slot.CancelLoading();
            if(version==_version && _owner && _slot.CurrentAsset){_write(_slot.CurrentAsset,false);_displayOwned=_slot.CurrentAsset;}
            Notify(request,EUIImageLoadStatus.Cancelled);ObserveIfNeeded();
        }
        internal void Retire(EUIImageLoadStatus status,bool clearDisplay)
        {
            var request=_request;_request=null;++_version;
            if(_owner && clearDisplay){_write(null,false);_displayOwned=null;}else UnmountOwned();
            _slot.Clear();StopObservation();Notify(request,status);
        }
        internal void SetBorrowed(T borrowed)
        {
            var request=_request;_request=null;++_version;
            if(_owner)_write(borrowed,false);
            _displayOwned=null;
            _slot.Clear();StopObservation();Notify(request,EUIImageLoadStatus.ManualOverride);
        }
        internal void Destroyed()
        {
            if(_dead)return;_dead=true;
            Retire(EUIImageLoadStatus.Destroyed,false);_slot.Dispose();_destroyRegistration.Dispose();
        }
        #endregion
    }
}
