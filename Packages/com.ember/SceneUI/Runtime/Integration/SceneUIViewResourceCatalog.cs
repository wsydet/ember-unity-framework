// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>一个通过 Ember.Resource 加载的 SceneUI View 配置。</summary>
    [Serializable]
    public sealed class SceneUIViewResourceCatalogEntry
    {
        #region 编辑器面板参数

        [SerializeField]
        private int key;

        [SerializeField]
        private string assetPath;

        [SerializeField, Min(0)]
        private int prewarmCount;

        [SerializeField, Min(0)]
        private int maxPooledCount = 32;

        [SerializeField]
        private bool overrideViewMetrics;

        [SerializeField]
        private Vector2 viewSize = new Vector2(100f, 30f);

        [SerializeField]
        private Vector2 viewPivot = new Vector2(0.5f, 0.5f);

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIViewKey Key => new SceneUIViewKey(key);
        public string AssetPath => assetPath;
        public int PrewarmCount => Mathf.Max(0, prewarmCount);
        public int MaxPooledCount => Mathf.Max(0, maxPooledCount);
        public bool HasViewMetricsOverride => overrideViewMetrics;
        public SceneUIViewMetrics ViewMetrics => overrideViewMetrics
            ? new SceneUIViewMetrics(
                new Vector2(Mathf.Max(0f, viewSize.x), Mathf.Max(0f, viewSize.y)),
                new Vector2(Mathf.Clamp01(viewPivot.x), Mathf.Clamp01(viewPivot.y)))
            : default;

        #endregion
    }

    /// <summary>SceneUI ViewKey 到资源路径的目录。</summary>
    [CreateAssetMenu(
        fileName = "SceneUIViewResourceCatalog",
        menuName = "Ember/SceneUI/Resource View Catalog")]
    public sealed class SceneUIViewResourceCatalog : ScriptableObject
    {
        #region 编辑器面板参数

        [SerializeField]
        private List<SceneUIViewResourceCatalogEntry> entries =
            new List<SceneUIViewResourceCatalogEntry>();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public IReadOnlyList<SceneUIViewResourceCatalogEntry> Entries => entries;

        /// <summary>校验目录并把全部资源定义注册到尚未开始准备的 Host。</summary>
        [HasGC]
        public bool TryRegisterAll(ResourceSceneUIViewHost host, out string error)
        {
            if (host == null)
            {
                error = "Resource ViewHost is missing.";
                return false;
            }

            var uniqueKeys = new HashSet<SceneUIViewKey>();
            for (int i = 0; i < entries.Count; i++)
            {
                SceneUIViewResourceCatalogEntry entry = entries[i];
                if (entry == null
                    || !entry.Key.IsValid
                    || string.IsNullOrWhiteSpace(entry.AssetPath)
                    || (entry.HasViewMetricsOverride && !entry.ViewMetrics.IsValid))
                {
                    error = $"Resource catalog entry {i} has an invalid key, path, or ViewMetrics override.";
                    return false;
                }

                if (!uniqueKeys.Add(entry.Key))
                {
                    error = $"Resource catalog contains duplicate ViewKey {entry.Key.Value}.";
                    return false;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                SceneUIViewResourceCatalogEntry entry = entries[i];
                if (!host.Register(
                        entry.Key,
                        entry.AssetPath,
                        entry.ViewMetrics,
                        entry.PrewarmCount,
                        entry.MaxPooledCount))
                {
                    error = $"Could not register resource ViewKey {entry.Key.Value}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        #endregion
    }
}
