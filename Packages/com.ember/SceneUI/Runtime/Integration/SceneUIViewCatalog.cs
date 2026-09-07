// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>一个 SceneUI View 类型的 Prefab 与池配置。</summary>
    [Serializable]
    public sealed class SceneUIViewCatalogEntry
    {
        #region 编辑器面板参数

        [SerializeField]
        private int key;

        [SerializeField]
        private GameObject prefab;

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
        public GameObject Prefab => prefab;
        public int PrewarmCount => Mathf.Max(0, prewarmCount);
        public int MaxPooledCount => Mathf.Max(0, maxPooledCount);
        public SceneUIViewMetrics ViewMetrics
        {
            get
            {
                if (overrideViewMetrics)
                {
                    return new SceneUIViewMetrics(
                        new Vector2(Mathf.Max(0f, viewSize.x), Mathf.Max(0f, viewSize.y)),
                        new Vector2(Mathf.Clamp01(viewPivot.x), Mathf.Clamp01(viewPivot.y)));
                }

                return SceneUIViewMetrics.From(
                    prefab ? prefab.GetComponent<RectTransform>() : null);
            }
        }

        #endregion
    }

    /// <summary>
    /// SceneUI View 类型目录。业务项目创建资产并在进入 Context 前完成同步预热。
    /// </summary>
    [CreateAssetMenu(fileName = "SceneUIViewCatalog", menuName = "Ember/SceneUI/View Catalog")]
    public sealed class SceneUIViewCatalog : ScriptableObject
    {
        #region 编辑器面板参数

        [SerializeField]
        private List<SceneUIViewCatalogEntry> entries = new List<SceneUIViewCatalogEntry>();

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public IReadOnlyList<SceneUIViewCatalogEntry> Entries => entries;

        /// <summary>校验目录并将全部 Prefab 注册到指定 Host。</summary>
        [HasGC]
        public bool TryRegisterAll(PrefabSceneUIViewHost host, out string error)
        {
            return TryRegisterAll(host, false, out error);
        }

        /// <summary>校验目录并注册全部 Prefab，可将预热数量加入分帧队列。</summary>
        [HasGC]
        public bool TryRegisterAll(
            PrefabSceneUIViewHost host,
            bool deferPrewarm,
            out string error)
        {
            if (host == null)
            {
                error = "ViewHost is missing.";
                return false;
            }

            var uniqueKeys = new HashSet<SceneUIViewKey>();
            for (int i = 0; i < entries.Count; i++)
            {
                SceneUIViewCatalogEntry entry = entries[i];
                if (entry == null || !entry.Key.IsValid || !entry.Prefab)
                {
                    error = $"Catalog entry {i} has an invalid key or prefab.";
                    return false;
                }

                if (!uniqueKeys.Add(entry.Key))
                {
                    error = $"Catalog contains duplicate ViewKey {entry.Key.Value}.";
                    return false;
                }
            }

            for (int i = 0; i < entries.Count; i++)
            {
                SceneUIViewCatalogEntry entry = entries[i];
                if (!host.Register(
                        entry.Key,
                        entry.Prefab,
                        entry.ViewMetrics,
                        entry.PrewarmCount,
                        entry.MaxPooledCount,
                        deferPrewarm))
                {
                    error = $"Could not register ViewKey {entry.Key.Value}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        #endregion
    }
}
