// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;
using Ember.UI;
using Ember.UIExtension;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// 使用已经加载的 EUI Item Prefab 提供同步获取和回收。
    /// Context 激活前应完成注册和预热，Flush 中不会触发资源加载。
    /// </summary>
    public sealed class PrefabSceneUIViewHost :
        ISceneUIViewHost,
        ISceneUIViewHostDiagnostics,
        ISceneUIViewHostPrewarm,
        ISceneUIViewMetricsProvider,
        IDisposable
    {
        private sealed class ViewPool
        {
            public readonly GameObject Prefab;
            public readonly Stack<ISceneUIView> InactiveViews;
            public readonly int MaxPooledCount;
            public readonly SceneUIViewMetrics Metrics;
            public int PendingPrewarmCount;
            public bool IsPrewarmQueued;

            public ViewPool(
                GameObject prefab,
                in SceneUIViewMetrics metrics,
                int maxPooledCount)
            {
                Prefab = prefab;
                Metrics = metrics;
                MaxPooledCount = Mathf.Max(0, maxPooledCount);
                InactiveViews = new Stack<ISceneUIView>(Mathf.Max(0, maxPooledCount));
            }
        }

        #region 内部参数

        private const string TAG = "SceneUI";

        private readonly RectTransform _layerRoot;
        private readonly int _viewLayer;
        private readonly Dictionary<SceneUIViewKey, ViewPool> _pools =
            new Dictionary<SceneUIViewKey, ViewPool>();
        private readonly List<ViewPool> _prewarmPools = new List<ViewPool>();

        private bool _isDisposed;
        private int _pooledViewCount;
        private int _poolHitCount;
        private int _poolMissCount;
        private int _pendingPrewarmCount;
        private int _prewarmedViewCount;
        private int _prewarmFailureCount;
        private int _prewarmCursor;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public RectTransform LayerRoot => _layerRoot;
        public int PooledViewCount => _pooledViewCount;
        public int PoolHitCount => _poolHitCount;
        public int PoolMissCount => _poolMissCount;
        public int PendingPrewarmCount => _pendingPrewarmCount;
        public int PrewarmedViewCount => _prewarmedViewCount;
        public int PrewarmFailureCount => _prewarmFailureCount;

        public PrefabSceneUIViewHost(RectTransform layerRoot, int viewLayer = -1)
        {
            _layerRoot = layerRoot;
            _viewLayer = viewLayer;
        }

        /// <summary>注册 EUI Item Prefab，并可选择同步或分帧预热。</summary>
        [HasGC]
        public bool Register(
            SceneUIViewKey viewKey,
            GameObject prefab,
            int prewarmCount = 0,
            int maxPooledCount = 32,
            bool deferPrewarm = false)
        {
            RectTransform rectTransform = prefab ? prefab.GetComponent<RectTransform>() : null;
            return Register(
                viewKey,
                prefab,
                SceneUIViewMetrics.From(rectTransform),
                prewarmCount,
                maxPooledCount,
                deferPrewarm);
        }

        /// <summary>注册 EUI Item Prefab，并显式提供精确边界所需的矩形元数据。</summary>
        [HasGC]
        public bool Register(
            SceneUIViewKey viewKey,
            GameObject prefab,
            in SceneUIViewMetrics metrics,
            int prewarmCount = 0,
            int maxPooledCount = 32,
            bool deferPrewarm = false)
        {
            if (_isDisposed
                || !_layerRoot
                || !viewKey.IsValid
                || !prefab
                || _pools.ContainsKey(viewKey))
                return false;

            if (!TryValidatePrefab(prefab, out string validationError))
            {
                EmberDebug.LogError(TAG, validationError);
                return false;
            }

            var pool = new ViewPool(prefab, metrics, maxPooledCount);
            _pools.Add(viewKey, pool);

            int count = Mathf.Min(Mathf.Max(0, prewarmCount), pool.MaxPooledCount);
            if (deferPrewarm)
                return QueuePrewarm(pool, count);

            for (int i = 0; i < count; i++)
            {
                ISceneUIView view = CreateView(pool);
                if (!IsViewAlive(view))
                {
                    _prewarmFailureCount++;
                    break;
                }

                pool.InactiveViews.Push(view);
                _pooledViewCount++;
                _prewarmedViewCount++;
            }

            return true;
        }

        /// <summary>为已注册 ViewKey 追加分帧预热任务，数量受池容量约束。</summary>
        [HasGC]
        public bool QueuePrewarm(SceneUIViewKey viewKey, int count)
        {
            if (_isDisposed || count < 0 || !_pools.TryGetValue(viewKey, out ViewPool pool))
                return false;

            return QueuePrewarm(pool, count);
        }

        /// <summary>按 ViewKey 轮转执行预热，每次创建不超过给定硬预算。</summary>
        [HasGC]
        public int ProcessPrewarm(int maxInstantiateCount)
        {
            if (_isDisposed || !_layerRoot || maxInstantiateCount <= 0)
                return 0;

            int createdCount = 0;
            while (createdCount < maxInstantiateCount && _prewarmPools.Count > 0)
            {
                if (_prewarmCursor < 0 || _prewarmCursor >= _prewarmPools.Count)
                    _prewarmCursor = 0;

                ViewPool pool = _prewarmPools[_prewarmCursor];
                int availableCapacity = Mathf.Max(0, pool.MaxPooledCount - pool.InactiveViews.Count);
                if (pool.PendingPrewarmCount > availableCapacity)
                    CancelPendingPrewarm(pool, pool.PendingPrewarmCount - availableCapacity);

                if (pool.PendingPrewarmCount <= 0)
                {
                    RemovePrewarmPoolAt(_prewarmCursor);
                    continue;
                }

                if (!pool.Prefab)
                {
                    _prewarmFailureCount++;
                    RemovePrewarmPoolAt(_prewarmCursor);
                    continue;
                }

                ISceneUIView view;
                try
                {
                    view = CreateView(pool);
                }
                catch (Exception ex)
                {
                    _prewarmFailureCount++;
                    EmberDebug.LogError(TAG, $"Incremental prewarm failed: {ex}");
                    RemovePrewarmPoolAt(_prewarmCursor);
                    continue;
                }

                if (!IsViewAlive(view))
                {
                    _prewarmFailureCount++;
                    RemovePrewarmPoolAt(_prewarmCursor);
                    continue;
                }

                pool.InactiveViews.Push(view);
                pool.PendingPrewarmCount--;
                _pendingPrewarmCount = Mathf.Max(0, _pendingPrewarmCount - 1);
                _pooledViewCount++;
                _prewarmedViewCount++;
                createdCount++;

                if (pool.PendingPrewarmCount <= 0)
                    RemovePrewarmPoolAt(_prewarmCursor);
                else
                    _prewarmCursor = (_prewarmCursor + 1) % _prewarmPools.Count;
            }

            return createdCount;
        }

        [NoGC]
        public bool TryGetMetrics(SceneUIViewKey viewKey, out SceneUIViewMetrics metrics)
        {
            if (!_isDisposed && _pools.TryGetValue(viewKey, out ViewPool pool) && pool.Metrics.IsValid)
            {
                metrics = pool.Metrics;
                return true;
            }

            metrics = default;
            return false;
        }

        [HasGC("Pool misses instantiate a prefab.")]
        public bool TryAcquire(SceneUIViewKey viewKey, out ISceneUIView view)
        {
            view = null;
            if (_isDisposed || !_layerRoot || !_pools.TryGetValue(viewKey, out ViewPool pool))
                return false;

            ISceneUIView instance = null;
            while (pool.InactiveViews.Count > 0)
            {
                ISceneUIView candidate = pool.InactiveViews.Pop();
                _pooledViewCount = Mathf.Max(0, _pooledViewCount - 1);
                if (IsViewAlive(candidate))
                {
                    instance = candidate;
                    break;
                }

                DestroyView(candidate);
            }

            if (IsViewAlive(instance))
            {
                _poolHitCount++;
                PrepareTransform(instance.RectTransform);
            }
            else
            {
                _poolMissCount++;
                instance = CreateView(pool);
            }

            if (!IsViewAlive(instance))
                return false;

            ((EUIItemSceneUIView)instance).BeginUse();
            view = instance;
            return true;
        }

        [HasGC]
        public void Release(SceneUIViewKey viewKey, ISceneUIView view)
        {
            if (!IsViewAlive(view))
                return;

            try
            {
                view.SetVisible(false);
                view.ResetView();
            }
            finally
            {
                if (_isDisposed
                    || !_layerRoot
                    || !_pools.TryGetValue(viewKey, out ViewPool pool)
                    || pool.InactiveViews.Count >= pool.MaxPooledCount)
                {
                    DestroyView(view);
                }
                else
                {
                    PrepareTransform(view.RectTransform);
                    pool.InactiveViews.Push(view);
                    _pooledViewCount++;
                }
            }
        }

        [NoGC]
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

            foreach (KeyValuePair<SceneUIViewKey, ViewPool> pair in _pools)
            {
                Stack<ISceneUIView> views = pair.Value.InactiveViews;
                while (views.Count > 0)
                    DestroyView(views.Pop());
            }

            _pools.Clear();
            _prewarmPools.Clear();
            _pooledViewCount = 0;
            _pendingPrewarmCount = 0;
            _prewarmCursor = 0;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        internal static bool TryValidatePrefab(GameObject prefab, out string error)
        {
            if (!prefab)
            {
                error = "View prefab is missing.";
                return false;
            }

            if (!prefab.GetComponent<RectTransform>())
            {
                error = $"View prefab '{prefab.name}' requires RectTransform on its root.";
                return false;
            }

            EUIBinding binding = prefab.GetComponent<EUIBinding>();
            if (binding)
            {
                if (binding.Role == EUIBindingRole.Item)
                {
                    Canvas[] prefabCanvases = prefab.GetComponentsInChildren<Canvas>(true);
                    for (int i = 0; i < prefabCanvases.Length; i++)
                    {
                        if (!prefabCanvases[i] || !prefabCanvases[i].overrideSorting)
                            continue;

                        error = $"View prefab '{prefab.name}' contains overrideSorting Canvas "
                            + $"'{prefabCanvases[i].name}' and would escape its host page layer.";
                        return false;
                    }

                    error = null;
                    return true;
                }

                error = $"View prefab '{prefab.name}' is an EUI Page. "
                    + "SceneUI only accepts EUI Item prefabs.";
                return false;
            }

            error = $"View prefab '{prefab.name}' requires an Item-role EUIBinding on its root.";
            return false;
        }

        private bool QueuePrewarm(ViewPool pool, int count)
        {
            int availableCapacity = Mathf.Max(
                0,
                pool.MaxPooledCount - pool.InactiveViews.Count - pool.PendingPrewarmCount);
            int queuedCount = Mathf.Min(Mathf.Max(0, count), availableCapacity);
            if (queuedCount <= 0)
                return true;

            pool.PendingPrewarmCount += queuedCount;
            _pendingPrewarmCount += queuedCount;
            if (!pool.IsPrewarmQueued)
            {
                pool.IsPrewarmQueued = true;
                _prewarmPools.Add(pool);
            }

            return true;
        }

        private void CancelPendingPrewarm(ViewPool pool, int count)
        {
            int cancelledCount = Mathf.Min(Mathf.Max(0, count), pool.PendingPrewarmCount);
            pool.PendingPrewarmCount -= cancelledCount;
            _pendingPrewarmCount = Mathf.Max(0, _pendingPrewarmCount - cancelledCount);
        }

        private void RemovePrewarmPoolAt(int index)
        {
            ViewPool pool = _prewarmPools[index];
            CancelPendingPrewarm(pool, pool.PendingPrewarmCount);
            pool.IsPrewarmQueued = false;
            _prewarmPools.RemoveAt(index);
            if (_prewarmPools.Count == 0 || _prewarmCursor >= _prewarmPools.Count)
                _prewarmCursor = 0;
        }

        private ISceneUIView CreateView(ViewPool pool)
        {
            if (!pool.Prefab || !_layerRoot)
                return null;

            GameObject instance = UnityEngine.Object.Instantiate(pool.Prefab, _layerRoot, false);
            if (!instance)
                return null;

            instance.name = pool.Prefab.name;
            instance.SetActive(false);
            if (_viewLayer >= 0)
                SetLayerRecursively(instance.transform, _viewLayer);

            if (!instance.TryGetComponent(out EUIBinding _))
            {
                EmberDebug.LogError(TAG, $"EUIBinding disappeared from '{pool.Prefab.name}'.");
                DestroyGameObject(instance);
                return null;
            }

            if (!EUIItemFactory.TryCreate(instance, out EUIItem item, out string error))
            {
                EmberDebug.LogError(TAG, error);
                DestroyGameObject(instance);
                return null;
            }

            ISceneUIView view = new EUIItemSceneUIView(item);

            if (!IsViewAlive(view))
            {
                EmberDebug.LogError(TAG, $"Could not create SceneUI view from '{pool.Prefab.name}'.");
                DestroyGameObject(instance);
                return null;
            }

            PrepareTransform(view.RectTransform);
            return view;
        }

        [NoGC]
        private void PrepareTransform(RectTransform rectTransform)
        {
            if (!rectTransform || !_layerRoot)
                return;

            rectTransform.SetParent(_layerRoot, false);
            Vector2 anchor = _layerRoot.pivot;
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static bool IsViewAlive(ISceneUIView view)
        {
            return view != null && view.RectTransform;
        }

        [NoGC]
        private static void DestroyView(ISceneUIView view)
        {
            if (view == null)
                return;

            GameObject gameObject = null;
            if (view is EUIItemSceneUIView itemView)
            {
                gameObject = itemView.GameObject;
                itemView.Dispose();
            }
            DestroyGameObject(gameObject);
        }

        [NoGC]
        private static void DestroyGameObject(GameObject gameObject)
        {
            if (!gameObject)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [NoGC]
        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (!root)
                return;

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        #endregion
    }
}
