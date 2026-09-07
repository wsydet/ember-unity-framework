// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>资源型 SceneUI ViewHost 的准备状态。</summary>
    public enum ResourceSceneUIViewHostState
    {
        Configuring = 0,
        Preparing = 1,
        Prepared = 2,
        Ready = 3,
        Failed = 4,
        Disposed = 5,
    }

    /// <summary>
    /// 先异步加载全部 Prefab，再向 SceneUI 暴露同步 Acquire/Release 的资源型 ViewHost。
    /// 加载句柄会保持到 Host Dispose，确保后端资源引用覆盖全部池实例生命周期。
    /// </summary>
    public sealed class ResourceSceneUIViewHost :
        ISceneUIViewHost,
        ISceneUIViewHostReadiness,
        ISceneUIViewHostDiagnostics,
        ISceneUIViewHostPrewarm,
        ISceneUIViewMetricsProvider,
        IDisposable
    {
        private const string TAG = "SceneUI";

        private sealed class ResourceEntry
        {
            public SceneUIViewKey ViewKey;
            public string AssetPath;
            public SceneUIViewMetrics Metrics;
            public int PrewarmCount;
            public int MaxPooledCount;
            public ISceneUIViewResourceHandle Handle;
            public Action CompletionCallback;
            public GameObject Prefab;
        }

        #region 内部参数

        private readonly ISceneUIViewResourceLoader _resourceLoader;
        private readonly List<ResourceEntry> _entries = new List<ResourceEntry>();
        private readonly Dictionary<SceneUIViewKey, ResourceEntry> _entriesByKey =
            new Dictionary<SceneUIViewKey, ResourceEntry>();

        private PrefabSceneUIViewHost _innerHost;
        private RectTransform _layerRoot;
        private int _viewLayer = -1;
        private bool _deferPrewarm;
        private int _pendingResourceCount;
        private int _loadedResourceCount;
        private int _resourceFailureCount;
        private string _lastError;
        private Action<bool, string> _prepareCallback;
        private ResourceSceneUIViewHostState _state;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public ResourceSceneUIViewHostState State => _state;
        public bool IsPrepared => _state == ResourceSceneUIViewHostState.Prepared
            || _state == ResourceSceneUIViewHostState.Ready;
        public bool IsReady => _state == ResourceSceneUIViewHostState.Ready;
        public int PendingResourceCount => _pendingResourceCount;
        public int LoadedResourceCount => _loadedResourceCount;
        public int ResourceFailureCount => _resourceFailureCount;
        public string LastError => _lastError;
        public int PooledViewCount => _innerHost?.PooledViewCount ?? 0;
        public int PoolHitCount => _innerHost?.PoolHitCount ?? 0;
        public int PoolMissCount => _innerHost?.PoolMissCount ?? 0;
        public int PendingPrewarmCount => _innerHost?.PendingPrewarmCount ?? 0;
        public int PrewarmedViewCount => _innerHost?.PrewarmedViewCount ?? 0;
        public int PrewarmFailureCount => _innerHost?.PrewarmFailureCount ?? 0;

        public ResourceSceneUIViewHost(ISceneUIViewResourceLoader resourceLoader = null)
        {
            _resourceLoader = resourceLoader ?? new EmberSceneUIViewResourceLoader();
            _state = ResourceSceneUIViewHostState.Configuring;
        }

        /// <summary>注册一个尚未加载的 View Prefab 路径。</summary>
        [HasGC]
        public bool Register(
            SceneUIViewKey viewKey,
            string assetPath,
            int prewarmCount = 0,
            int maxPooledCount = 32)
        {
            return Register(viewKey, assetPath, default, prewarmCount, maxPooledCount);
        }

        /// <summary>注册资源路径，并可显式提供精确边界元数据。</summary>
        [HasGC]
        public bool Register(
            SceneUIViewKey viewKey,
            string assetPath,
            in SceneUIViewMetrics metrics,
            int prewarmCount = 0,
            int maxPooledCount = 32)
        {
            if (_state != ResourceSceneUIViewHostState.Configuring
                || !viewKey.IsValid
                || string.IsNullOrWhiteSpace(assetPath)
                || _entriesByKey.ContainsKey(viewKey))
                return false;

            var entry = new ResourceEntry
            {
                ViewKey = viewKey,
                AssetPath = assetPath,
                Metrics = metrics,
                PrewarmCount = Mathf.Max(0, prewarmCount),
                MaxPooledCount = Mathf.Max(0, maxPooledCount),
            };
            _entries.Add(entry);
            _entriesByKey.Add(viewKey, entry);
            return true;
        }

        /// <summary>
        /// 绑定最终 LayerRoot。可以在 PrepareAsync 前绑定，也可以在资源 Prepared 后绑定。
        /// Prepared 状态绑定成功后立即转为 Ready。
        /// </summary>
        [HasGC]
        public bool Attach(
            RectTransform layerRoot,
            int viewLayer,
            bool deferPrewarm,
            out string error)
        {
            if (!layerRoot)
            {
                error = "LayerRoot is missing.";
                return false;
            }

            if (_state == ResourceSceneUIViewHostState.Failed
                || _state == ResourceSceneUIViewHostState.Disposed
                || _state == ResourceSceneUIViewHostState.Ready
                || _layerRoot)
            {
                error = "Resource ViewHost cannot be attached in its current state.";
                return false;
            }

            _layerRoot = layerRoot;
            _viewLayer = viewLayer;
            _deferPrewarm = deferPrewarm;
            if (_state != ResourceSceneUIViewHostState.Prepared)
            {
                error = null;
                return true;
            }

            if (TryBuildInnerHost(out error))
                return true;

            FailPreparedHost(error);
            return false;
        }

        /// <summary>异步准备全部 Prefab。回调成功时状态至少为 Prepared；已绑定根节点时为 Ready。</summary>
        [HasGC]
        public bool PrepareAsync(Action<bool, string> onComplete)
        {
            if (_state == ResourceSceneUIViewHostState.Prepared
                || _state == ResourceSceneUIViewHostState.Ready)
            {
                onComplete?.Invoke(true, null);
                return true;
            }

            if (_state != ResourceSceneUIViewHostState.Configuring)
                return false;

            _state = ResourceSceneUIViewHostState.Preparing;
            _prepareCallback = onComplete;
            _pendingResourceCount = _entries.Count;
            if (_pendingResourceCount == 0)
            {
                CompletePreparation();
                return true;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_state != ResourceSceneUIViewHostState.Preparing)
                    break;

                ResourceEntry entry = _entries[i];
                try
                {
                    entry.Handle = _resourceLoader.LoadPrefab(entry.AssetPath);
                    if (entry.Handle == null)
                    {
                        FailPreparation(
                            $"Resource loader returned no handle for ViewKey {entry.ViewKey.Value}.");
                        break;
                    }

                    entry.CompletionCallback = () => OnResourceCompleted(entry);
                    entry.Handle.Completed += entry.CompletionCallback;
                }
                catch (Exception ex)
                {
                    FailPreparation($"Loading ViewKey {entry.ViewKey.Value} threw an exception: {ex}");
                    break;
                }
            }

            return true;
        }

        [NoGC]
        public bool TryGetMetrics(SceneUIViewKey viewKey, out SceneUIViewMetrics metrics)
        {
            if (IsReady)
                return _innerHost.TryGetMetrics(viewKey, out metrics);

            metrics = default;
            return false;
        }

        [HasGC("Pool misses instantiate a prefab; resource loading never occurs here.")]
        public bool TryAcquire(SceneUIViewKey viewKey, out ISceneUIView view)
        {
            if (IsReady)
                return _innerHost.TryAcquire(viewKey, out view);

            view = null;
            return false;
        }

        [NoGC]
        public void Release(SceneUIViewKey viewKey, ISceneUIView view)
        {
            _innerHost?.Release(viewKey, view);
        }

        [HasGC]
        public int ProcessPrewarm(int maxInstantiateCount)
        {
            return IsReady ? _innerHost.ProcessPrewarm(maxInstantiateCount) : 0;
        }

        [NoGC]
        public void Dispose()
        {
            if (_state == ResourceSceneUIViewHostState.Disposed)
                return;

            bool wasPreparing = _state == ResourceSceneUIViewHostState.Preparing;
            Action<bool, string> callback = wasPreparing ? _prepareCallback : null;
            _prepareCallback = null;
            _state = ResourceSceneUIViewHostState.Disposed;
            _lastError = wasPreparing ? "Resource ViewHost preparation was cancelled." : _lastError;

            DisposeInnerHost();
            ReleaseResourceHandles(true);
            _entries.Clear();
            _entriesByKey.Clear();
            _pendingResourceCount = 0;

            callback?.Invoke(false, _lastError);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void OnResourceCompleted(ResourceEntry entry)
        {
            if (_state != ResourceSceneUIViewHostState.Preparing || entry.Handle == null)
                return;

            if (entry.CompletionCallback != null)
            {
                try
                {
                    entry.Handle.Completed -= entry.CompletionCallback;
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"Detaching completed resource callback failed: {ex}");
                }
                finally
                {
                    entry.CompletionCallback = null;
                }
            }

            if (!entry.Handle.Succeeded || !entry.Handle.Asset)
            {
                string detail = string.IsNullOrEmpty(entry.Handle.Error)
                    ? "unknown resource error"
                    : entry.Handle.Error;
                FailPreparation(
                    $"Loading ViewKey {entry.ViewKey.Value} from '{entry.AssetPath}' failed: {detail}");
                return;
            }

            entry.Prefab = entry.Handle.Asset;
            if (!PrefabSceneUIViewHost.TryValidatePrefab(entry.Prefab, out string validationError))
            {
                FailPreparation($"Resource '{entry.AssetPath}' is not a valid EUI Item: {validationError}");
                return;
            }

            _loadedResourceCount++;
            _pendingResourceCount = Mathf.Max(0, _pendingResourceCount - 1);
            if (_pendingResourceCount == 0)
                CompletePreparation();
        }

        private void CompletePreparation()
        {
            if (_state != ResourceSceneUIViewHostState.Preparing)
                return;

            _state = ResourceSceneUIViewHostState.Prepared;
            if (_layerRoot && !TryBuildInnerHost(out string buildError))
            {
                FailPreparedHost(buildError);
                return;
            }

            Action<bool, string> callback = _prepareCallback;
            _prepareCallback = null;
            callback?.Invoke(true, null);
        }

        private bool TryBuildInnerHost(out string error)
        {
            if (_state != ResourceSceneUIViewHostState.Prepared || !_layerRoot)
            {
                error = "Resource ViewHost must be Prepared and have a LayerRoot before activation.";
                return false;
            }

            var innerHost = new PrefabSceneUIViewHost(_layerRoot, _viewLayer);
            try
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    ResourceEntry entry = _entries[i];
                    SceneUIViewMetrics metrics = entry.Metrics.IsValid
                        ? entry.Metrics
                        : SceneUIViewMetrics.From(
                            entry.Prefab ? entry.Prefab.GetComponent<RectTransform>() : null);
                    if (!innerHost.Register(
                            entry.ViewKey,
                            entry.Prefab,
                            metrics,
                            entry.PrewarmCount,
                            entry.MaxPooledCount,
                            _deferPrewarm))
                    {
                        innerHost.Dispose();
                        error = $"Could not activate loaded ViewKey {entry.ViewKey.Value}.";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                innerHost.Dispose();
                error = $"Activating loaded SceneUI prefabs failed: {ex}";
                return false;
            }

            _innerHost = innerHost;
            _state = ResourceSceneUIViewHostState.Ready;
            error = null;
            return true;
        }

        private void FailPreparation(string error)
        {
            if (_state != ResourceSceneUIViewHostState.Preparing)
                return;

            _state = ResourceSceneUIViewHostState.Failed;
            _lastError = error;
            _resourceFailureCount++;
            _pendingResourceCount = 0;
            ReleaseResourceHandles(true);

            Action<bool, string> callback = _prepareCallback;
            _prepareCallback = null;
            callback?.Invoke(false, error);
        }

        private void FailPreparedHost(string error)
        {
            _state = ResourceSceneUIViewHostState.Failed;
            _lastError = error;
            _resourceFailureCount++;
            _pendingResourceCount = 0;
            DisposeInnerHost();
            ReleaseResourceHandles(false);

            Action<bool, string> callback = _prepareCallback;
            _prepareCallback = null;
            callback?.Invoke(false, error);
        }

        private void ReleaseResourceHandles(bool cancelPending)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                ResourceEntry entry = _entries[i];
                ISceneUIViewResourceHandle handle = entry.Handle;
                if (handle == null)
                    continue;

                if (entry.CompletionCallback != null)
                {
                    try
                    {
                        handle.Completed -= entry.CompletionCallback;
                    }
                    catch (Exception ex)
                    {
                        EmberDebug.LogError(TAG, $"Detaching resource callback failed: {ex}");
                    }

                    entry.CompletionCallback = null;
                }

                try
                {
                    if (cancelPending && !handle.IsDone)
                        handle.Cancel();
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"Cancelling resource handle failed: {ex}");
                }

                try
                {
                    handle.Dispose();
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"Disposing resource handle failed: {ex}");
                }

                entry.Handle = null;
                entry.Prefab = null;
            }
        }

        private void DisposeInnerHost()
        {
            PrefabSceneUIViewHost innerHost = _innerHost;
            _innerHost = null;
            if (innerHost == null)
                return;

            try
            {
                innerHost.Dispose();
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"Disposing resource-backed ViewHost failed: {ex}");
            }
        }

        #endregion
    }
}
