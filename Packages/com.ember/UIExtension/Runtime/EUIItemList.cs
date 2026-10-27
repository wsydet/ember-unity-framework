using System;
using System.Collections.Generic;
using Ember.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>单模板、整表重新绑定的小型列表；缓存只保留实例与逻辑，不保留数据借用。</summary>
    public sealed class EUIItemList<T> : IDisposable
    {
        #region 内部参数
        private readonly EUIDynamicListHost _host;
        private readonly Func<T, string> _key;
        private readonly Action<EUIItemLease<T>> _bind;
        private readonly List<EUIItemLease<T>> _active = new();
        private readonly Stack<EUIItem> _cache = new();
        private bool _disposed, _mutating;
        private long _generation;
        public int Count => _active.Count;
        public int CachedCount => _cache.Count;
        public int InstanceCount => Count + CachedCount;
        public int CreatedCount { get; private set; }
        public int DestroyedCount { get; private set; }
        public int LogicCreatedCount { get; private set; }
        public int RebindCount { get; private set; }
        public bool IsDisposed => _disposed;
        public EUIItemLease<T> this[int index] => _active[index];
        #endregion

        #region 内部方法
        internal EUIItemList(EUIDynamicListHost host, Func<T, string> key, Action<EUIItemLease<T>> bind)
        { _host = host; _key = key; _bind = bind; }

        private EUIItem Take()
        {
            if (_cache.Count > 0) return _cache.Pop();
            var instance = UnityEngine.Object.Instantiate(_host.ItemTemplate, _host.Content, false);
            instance.SetActive(false);
            CreatedCount++;
            if (!EUIItemFactory.TryCreate(instance, out var item, out var error))
            { DestroyObject(instance); DestroyedCount++; throw new InvalidOperationException(error); }
            if (item.Logic != null) LogicCreatedCount++;
            return item;
        }

        private void ReleaseRows()
        {
            // 逆序入栈，使下次首行优先借用原首行。不是按 key 做增量差分。
            var retiring = _active.ToArray();
            _active.Clear();
            for (var i = retiring.Length - 1; i >= 0; i--)
            {
                var lease = retiring[i];
                lease.Dispose();
                lease.Item.ResetForPool();
                if (_cache.Count < _host.CacheCapacity && !_disposed) _cache.Push(lease.Item);
                else DestroyItem(lease.Item);
            }
        }

        private void DestroyItem(EUIItem item)
        { item.Dispose(); DestroyObject(item.GameObject); DestroyedCount++; }

        private static void DestroyObject(GameObject value)
        {
            if (!value) return;
            value.SetActive(false);
            value.transform.SetParent(null, false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        #endregion

        #region 外部方法
        /// <summary>先验证整批 key；重复/空 key 拒绝且保留旧数据。绑定异常则清空本批，释放全部借用。</summary>
        public void Replace(IReadOnlyList<T> data)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EUIItemList<T>));
            if (_mutating) throw new InvalidOperationException("列表更新期间不可重入。");
            if (data == null) throw new ArgumentNullException(nameof(data));
            _mutating = true;
            try
            {
                var keys = new string[data.Count];
                var values = new T[data.Count];
                var unique = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < data.Count; i++)
                {
                    values[i] = data[i]; keys[i] = _key(values[i]);
                    if (string.IsNullOrWhiteSpace(keys[i]) || !unique.Add(keys[i]))
                        throw new ArgumentException("列表 key 为空或重复，index=" + i + ", key=" + keys[i]);
                }
                ReleaseRows();
                if (_disposed) throw new ObjectDisposedException(nameof(EUIItemList<T>));
                try
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        var item = Take();
                        var lease = new EUIItemLease<T>(item, values[i], keys[i], i, ++_generation);
                        _active.Add(lease);
                        item.RectTransform.SetSiblingIndex(i);
                        item.BeginUse();
                        _bind(lease);
                        if (_disposed) throw new ObjectDisposedException(nameof(EUIItemList<T>));
                        item.Show();
                        RebindCount++;
                    }
                }
                catch { ReleaseRows(); throw; }
                LayoutRebuilder.MarkLayoutForRebuild(_host.Content);
            }
            finally { _mutating = false; }
        }

        public void Clear() => Replace(Array.Empty<T>());
        public void Hide() { foreach (var row in _active) row.Item.Hide(); }
        public void Show() { foreach (var row in _active) row.Item.Show(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseRows();
            while (_cache.Count > 0) DestroyItem(_cache.Pop());
            _host.ReleaseOwner(this);
        }
        #endregion
    }
}
