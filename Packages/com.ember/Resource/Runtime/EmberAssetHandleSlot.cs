using System;
using Ember.Basic;
using Object = UnityEngine.Object;

namespace Ember.Resource
{
    /// <summary>主线程资源槽。Ordinal 地址去重；显示切换回调结束后才释放旧凭证。</summary>
    public sealed class EmberAssetHandleSlot<T> : IDisposable where T : Object
    {
        #region 内部参数
        private string _currentPath;
        private EmberAssetHandle<T> _currentHandle;
        private string _loadingPath;
        private EmberAssetHandle<T> _loadingHandle;
        private Action<T> _loadingCallback;
        private Action<T> _subscription;
        private long _generation;
        private bool _disposed;
        private string _lastError;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Notify(Action<T> callback, T asset)
        {
            try { callback?.Invoke(asset); }
            catch (Exception e) { EmberDebug.LogWarning(LogTags.ResourceManager, $"Asset slot callback: {e}"); }
        }

        private void OnCompleted(EmberAssetHandle<T> handle, long generation, T asset)
        {
            if (_disposed || generation != _generation || handle != _loadingHandle) return;
            var callback = _loadingCallback;
            var path = _loadingPath;
            handle.Completed -= _subscription;
            _loadingHandle = null;
            _loadingCallback = null;
            _subscription = null;
            _loadingPath = null;
            _lastError = handle.Error;
            if (!handle.Succeeded || asset == null)
            {
                try { Notify(callback, null); }
                finally { handle.Dispose(); }
                return;
            }
            var old = _currentHandle;
            _currentHandle = handle;
            _currentPath = path;
            try { Notify(callback, asset); }
            finally { if (old != null && old != _currentHandle) old.Dispose(); }
        }

        private bool SamePath(string a, string b) => !string.IsNullOrEmpty(a) && string.Equals(a, b, StringComparison.Ordinal);
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public string CurrentAssetPath => _currentPath;
        public T CurrentAsset => _currentHandle?.Asset;
        public string LoadingAssetPath => _loadingPath;
        public string TargetAssetPath => _loadingPath ?? _currentPath;
        public bool IsLoading => _loadingHandle != null;
        public string LastError => _lastError;

        /// <summary>
        /// 同成功地址同步回调；同 pending 地址只替换回调；失败地址可重试，不改变大小写/空白。
        /// reapplyIfCurrent=false 仍调用回调，但传 null（旧兼容语义）。
        /// 回调必须先切换显示再返回，旧资源凭证在 finally 中释放。
        /// </summary>
        public void LoadAsync(string assetPath, Action<T> onLoaded, bool reapplyIfCurrent = true)
        {
            if (_disposed) { _lastError = "Asset slot disposed."; Notify(onLoaded, null); return; }
            if (SamePath(_currentPath, assetPath) && _currentHandle.Succeeded)
            {
                CancelLoading();
                _lastError = null;
                Notify(onLoaded, reapplyIfCurrent ? CurrentAsset : null);
                return;
            }
            if (SamePath(_loadingPath, assetPath)) { _loadingCallback = onLoaded; return; }
            CancelLoading();
            var generation = ++_generation;
            EmberAssetHandle<T> handle;
            try { handle = EmberResourceManager.Instance.LoadAssetHandle<T>(assetPath); }
            catch (Exception e) { _lastError = e.Message; Notify(onLoaded, null); return; }
            if (_disposed || generation != _generation) { handle?.Dispose(); return; }
            if (handle == null) { _lastError = "Provider returned a null handle."; Notify(onLoaded, null); return; }
            _lastError = null;
            _loadingHandle = handle;
            _loadingPath = assetPath;
            _loadingCallback = onLoaded;
            _subscription = asset => OnCompleted(handle, generation, asset);
            handle.Completed += _subscription;
        }

        /// <summary>取消并释放 pending 凭证，保留当前成功资源。</summary>
        public void CancelLoading()
        {
            ++_generation;
            var handle = _loadingHandle;
            var subscription = _subscription;
            _loadingHandle = null;
            _loadingPath = null;
            _loadingCallback = null;
            _subscription = null;
            if (handle == null) return;
            handle.Completed -= subscription;
            handle.Dispose();
        }

        /// <summary>清空后可复用。调用前先卸下显示引用。</summary>
        public void Clear()
        {
            CancelLoading();
            var current = _currentHandle;
            _currentHandle = null;
            _currentPath = null;
            _lastError = null;
            current?.Dispose();
        }

        public void Dispose() { if (_disposed) return; _disposed = true; Clear(); }
        #endregion
    }
}
