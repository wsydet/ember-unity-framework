using System;
using Ember.Basic;
using Object = UnityEngine.Object;

namespace Ember.Resource
{
    /// <summary>
    /// 异步资源加载槽 —— 持有"当前已加载"和"正在加载中"两个状态，
    /// 自动处理去重、取消和重入。
    ///
    /// 设计参考了 burner 的 AssetHandleSlot《T》，核心机制：
    ///
    /// <b>去重：</b>同一个资源路径多次 LoadAsync 只会发起一次请求，后续请求复用第一个的回调。
    /// <b>取消：</b>请求新资源时自动取消旧请求（忽略旧回调），防止旧资源覆盖新资源。
    /// <b>重入安全：</b>回调中可以立即再次 LoadAsync 或 Dispose，不会产生状态错乱。
    ///
    /// 典型应用场景：UI Image 切换头像、Renderer 切换材质、Text 切换字体等
    /// "动态切换同一槽位的资源"的场景。
    ///
    /// 用法：
    /// <code>
    /// private readonly EmberAssetHandleSlot&lt;Sprite&gt; _iconSlot = new();
    ///
    /// public void SetIcon(string path)
    /// {
    ///     _iconSlot.LoadAsync(path, sprite => {
    ///         if (sprite != null) iconImage.sprite = sprite;
    ///     });
    /// }
    ///
    /// void OnDestroy() { _iconSlot.Dispose(); }
    /// </code>
    /// </summary>
    /// <typeparam name="T">资源类型，必须是 UnityEngine.Object 的子类</typeparam>
    public sealed class EmberAssetHandleSlot<T> : IDisposable where T : Object
    {
        private const string TAG = LogTags.ResourceManager;

        #region 内部参数

        private string _currentPath;
        private EmberAssetHandle<T> _currentHandle;
        private string _loadingPath;
        private EmberAssetHandle<T> _loadingHandle;
        private Action<T> _loadingCallback;
        private bool _disposed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前已加载的资源路径（null 表示尚未加载任何资源）。</summary>
        public string CurrentAssetPath => _currentPath;

        /// <summary>正在加载的资源路径（null 表示无加载中请求）。</summary>
        public string LoadingAssetPath => _loadingPath;

        /// <summary>槽的目标资源路径：优先返回正在加载的，其次已加载的。</summary>
        public string TargetAssetPath => _loadingPath ?? _currentPath;

        /// <summary>是否正在加载中。</summary>
        public bool IsLoading => _loadingHandle != null;

        /// <summary>
        /// 异步加载资源。
        ///
        /// <list type="number">
        ///   <item>如果 assetPath 与 <see cref="CurrentAssetPath"/> 相同 → 立即用已加载资源回调</item>
        ///   <item>如果 assetPath 与 <see cref="LoadingAssetPath"/> 相同 → 替换回调（合并请求）</item>
        ///   <item>如果是新路径 → 取消旧加载，发起新加载</item>
        /// </list>
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="onLoaded">加载完成回调，失败时为 null。回调在 Handle.Completed 事件中触发</param>
        /// <param name="reapplyIfCurrent">路径与当前相同时是否重新回调（默认 true）</param>
        public void LoadAsync(string assetPath, Action<T> onLoaded, bool reapplyIfCurrent = true)
        {
            if (_disposed)
            {
                EmberDebug.LogWarning(TAG,
                    "[EmberAssetHandleSlot] 已释放的资源槽不应再次发起加载。");
                onLoaded?.Invoke(null);
                return;
            }

            var normalized = NormalizePath(assetPath);

            // 1. 路径与当前已加载的相同 → 立即用已有资源回调
            if (IsSamePath(_currentPath, normalized))
            {
                CancelLoading();
                if (reapplyIfCurrent)
                    ApplyCurrent(onLoaded);
                else
                    onLoaded?.Invoke(null);
                return;
            }

            // 2. 路径与正在加载的相同 → 替换回调（不发起新请求）
            if (IsSamePath(_loadingPath, normalized))
            {
                _loadingCallback = onLoaded;
                return;
            }

            // 3. 新资源 → 取消旧加载，发起新加载
            CancelLoading();

            var handle = EmberResourceManager.Instance.LoadAssetHandle<T>(assetPath);
            if (handle == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            _loadingPath = normalized;
            _loadingCallback = onLoaded;
            _loadingHandle = handle;
            handle.Completed += OnHandleCompleted;
        }

        /// <summary>
        /// 取消正在进行的加载（不影响已加载的当前资源）。
        /// </summary>
        public void CancelLoading()
        {
            _loadingPath = null;
            _loadingCallback = null;

            if (_loadingHandle != null)
            {
                _loadingHandle.Completed -= OnHandleCompleted;
                _loadingHandle.Cancel();
                _loadingHandle = null;
            }
        }

        /// <summary>
        /// 释放槽：取消加载 + 释放当前资源引用。
        /// Dispose 后不能再调用 LoadAsync（会打印警告并回调 null）。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelLoading();
            ClearCurrent();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// Handle 加载完成回调（来自 EmberAssetHandle.Completed 事件）。
        ///
        /// 重入安全：回调里可能立即再次 LoadAsync 或 Dispose。
        /// 因此先提取所有本地变量，再执行回调；释放旧 Handle 前检查是否已被重新启用。
        /// </summary>
        private void OnHandleCompleted(T asset)
        {
            if (_disposed) return;

            // 防御：回调可能晚于取消（Handle 已完成但 Completed 事件在 CancelLoading 之后触发）
            if (_loadingHandle == null) return;

            var path = _loadingPath;
            var callback = _loadingCallback;
            var handle = _loadingHandle;

            _loadingHandle = null;
            _loadingPath = null;
            _loadingCallback = null;
            handle.Completed -= OnHandleCompleted;

            // 先让槽持有新 Handle 和路径，再回调业务 ——
            // 这样回调里如果再次 LoadAsync，_currentPath/_currentHandle 已经是新的了
            var oldHandle = _currentHandle;
            _currentHandle = handle;
            _currentPath = path;

            callback?.Invoke(asset);

            // 释放旧 Handle，但前提是它不是当前持有的（防止误释放回调里重新启用的 Handle）
            if (oldHandle != null && oldHandle != _currentHandle)
            {
                oldHandle.Dispose();
            }
        }

        /// <summary>
        /// 立即用当前已加载的资源回调。
        /// </summary>
        private void ApplyCurrent(Action<T> callback)
        {
            if (_currentHandle != null && _currentHandle.Succeeded)
                callback?.Invoke(_currentHandle.Asset);
            else
                callback?.Invoke(null);
        }

        /// <summary>
        /// 释放当前持有的资源。
        /// </summary>
        private void ClearCurrent()
        {
            _currentPath = null;
            if (_currentHandle != null)
            {
                _currentHandle.Dispose();
                _currentHandle = null;
            }
        }

        /// <summary>
        /// 规范化资源路径：空串原样返回（不参与去重），非空转小写。
        ///
        /// 空字符串是非法资源名 —— 不参与去重，也不缓存成稳定失败状态。
        /// 这样重复请求仍会交给 Provider 暴露配置错误。
        /// </summary>
        [NoGC]
        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? path : path.ToLower();
        }

        /// <summary>
        /// 路径相同判断：仅都不是空串时才比较，大小写敏感。
        /// 空串之间视为"不同"（不参与去重）。
        /// </summary>
        [NoGC]
        private static bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        #endregion
    }
}
