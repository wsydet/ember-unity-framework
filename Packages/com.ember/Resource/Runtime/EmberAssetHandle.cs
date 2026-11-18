using System;
using Ember.Basic;
using Object = UnityEngine.Object;

namespace Ember.Resource
{
    /// <summary>一次主线程资源请求及其 Provider 引用凭证。Cancel 结束请求；Dispose 释放凭证。</summary>
    public sealed class EmberAssetHandle<T> : IDisposable where T : Object
    {
        #region 内部参数
        private readonly string _assetPath;
        private T _asset;
        private bool _isDone;
        private bool _isCancelled;
        private bool _disposed;
        private bool _succeeded;
        private string _error;
        private Action _cancelAction;
        private Action _releaseAction;
        private Action<T> _completed;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        internal EmberAssetHandle(string path) { _assetPath = path; }
        internal void SetCancellation(Action cancel) { if (!_isDone) _cancelAction = cancel; }
        internal void Complete(T asset, bool succeeded, string error) { TryComplete(asset, succeeded, error); }

        private void Notify(Action<T> callbacks, T asset)
        {
            if (callbacks == null) return;
            foreach (Action<T> callback in callbacks.GetInvocationList())
            {
                try { callback(asset); }
                catch (Exception e) { EmberDebug.LogWarning(LogTags.ResourceManager, $"Asset completion callback: {e}"); }
            }
        }

        private void InvokeHook(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception e) { EmberDebug.LogWarning(LogTags.ResourceManager, $"Asset lifetime hook: {e}"); }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public string AssetPath => _assetPath;
        public bool IsDone => _isDone;
        public bool IsCancelled => _isCancelled;
        public bool IsDisposed => _disposed;
        public bool Succeeded => _succeeded;
        public T Asset => _asset;
        public string Error => _error;

        /// <summary>成功传资源；失败/取消传 null。完成后订阅立即通知；Dispose 后不再订阅。</summary>
        public event Action<T> Completed
        {
            add { if (_disposed) return; if (_isDone) Notify(value, _succeeded ? _asset : null); else _completed += value; }
            remove { _completed -= value; }
        }

        /// <summary>供外部 Provider 创建请求。cancel 与 release 是独立、每请求至多一次的 hook。</summary>
        [HasGC]
        public static EmberAssetHandle<T> Create(string path, Action cancel = null, Action release = null)
        {
            return new EmberAssetHandle<T>(path) { _cancelAction = cancel, _releaseAction = release };
        }

        /// <summary>只接受 pending 的终态。false 表示 Provider 仍须清理其后端 token/迟到资源。</summary>
        public bool TryComplete(T asset, bool succeeded, string error = null)
        {
            if (_isDone || _disposed) return false;
            _isDone = true;
            _succeeded = succeeded && asset != null;
            _asset = _succeeded ? asset : null;
            _error = _succeeded ? null : error ?? "Asset load failed.";
            _cancelAction = null;
            var callbacks = _completed;
            _completed = null;
            Notify(callbacks, _asset);
            return true;
        }

        /// <summary>先进入取消终态，再调用后端。Cancel 不释放凭证，调用方仍须 Dispose。</summary>
        public void Cancel()
        {
            if (_isDone) return;
            _isDone = true;
            _isCancelled = true;
            _succeeded = false;
            _asset = null;
            _error = "Cancelled";
            var cancel = _cancelAction;
            var callbacks = _completed;
            _cancelAction = null;
            _completed = null;
            InvokeHook(cancel);
            Notify(callbacks, null);
        }

        /// <summary>幂等释放本请求 token；pending 先取消。先从 Graphic 卸下其拥有引用再调用。</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            var release = _releaseAction;
            _releaseAction = null;
            Cancel();
            _asset = null;
            _succeeded = false;
            _completed = null;
            _cancelAction = null;
            InvokeHook(release);
        }

        [NoGC]
        public static implicit operator T(EmberAssetHandle<T> handle) => handle?._asset;
        #endregion
    }
}
