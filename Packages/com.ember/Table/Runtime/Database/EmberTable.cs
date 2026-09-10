// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Ember.Basic;

namespace Ember.Table
{
    /// <summary>保持源行顺序、仅支持主键和显式二级索引查询的只读表。</summary>
    public sealed class EmberTable<TRow> : IEmberTableData, IReadOnlyList<TRow>
    {
        #region 内部参数

        private readonly ReadOnlyCollection<TRow> _rows;
        private readonly Dictionary<string, TRow> _byPrimaryKey;
        private readonly Dictionary<string, object> _indexes;

        public string TableId { get; }
        public string RowTypeId { get; }
        public int Count => _rows.Count;
        public TRow this[int index] => _rows[index];

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        internal EmberTable(
            string tableId,
            string rowTypeId,
            IList<TRow> rows,
            Func<TRow, string> primaryKeySelector)
        {
            TableId = tableId;
            RowTypeId = rowTypeId;
            var copy = rows == null ? new List<TRow>() : new List<TRow>(rows);
            _rows = new ReadOnlyCollection<TRow>(copy);
            _byPrimaryKey = new Dictionary<string, TRow>(copy.Count, StringComparer.Ordinal);
            _indexes = new Dictionary<string, object>(StringComparer.Ordinal);

            for (int i = 0; i < copy.Count; i++)
            {
                string key = primaryKeySelector(copy[i]);
                if (string.IsNullOrEmpty(key))
                    throw new EmberTableDataException(
                        EmberTableErrorCode.EmptyPrimaryKey,
                        $"Row {i + 1} has a null or empty primary key.");
                if (!_byPrimaryKey.TryAdd(key, copy[i]))
                    throw new EmberTableDataException(
                        EmberTableErrorCode.DuplicatePrimaryKey,
                        $"Primary key '{key}' occurs more than once.");
            }
        }

        internal void AddIndex<TKey>(
            string name,
            Func<TRow, TKey> selector,
            bool unique)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Index name cannot be null or empty.", nameof(name));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (_indexes.ContainsKey(name))
                throw new EmberTableDataException(
                    EmberTableErrorCode.DuplicateSecondaryKey,
                    $"Secondary index '{name}' is registered more than once.",
                    name);

            _indexes.Add(name, new EmberTableIndex<TKey, TRow>(name, _rows, selector, unique));
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [NoGC]
        public bool TryGet(string key, out TRow row)
        {
            if (key != null) return _byPrimaryKey.TryGetValue(key, out row);
            row = default;
            return false;
        }

        [NoGC]
        public bool ContainsKey(string key)
        {
            return key != null && _byPrimaryKey.ContainsKey(key);
        }

        [NoGC]
        public bool TryGetIndex<TKey>(string name, out EmberTableIndex<TKey, TRow> index)
        {
            if (name != null
                && _indexes.TryGetValue(name, out object value)
                && value is EmberTableIndex<TKey, TRow> typed)
            {
                index = typed;
                return true;
            }

            index = null;
            return false;
        }

        [HasGC]
        public IEnumerator<TRow> GetEnumerator()
        {
            return _rows.GetEnumerator();
        }

        IEnumerator<TRow> IEnumerable<TRow>.GetEnumerator()
        {
            return _rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _rows.GetEnumerator();
        }

        #endregion
    }

    /// <summary>仅由 Binding 显式建立的确定性二级索引。</summary>
    public sealed class EmberTableIndex<TKey, TRow>
    {
        #region 内部参数

        private readonly Dictionary<TKey, ReadOnlyCollection<TRow>> _rowsByKey;

        public string Name { get; }
        public int KeyCount => _rowsByKey.Count;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        internal EmberTableIndex(
            string name,
            IReadOnlyList<TRow> rows,
            Func<TRow, TKey> selector,
            bool unique)
        {
            Name = name;
            IEqualityComparer<TKey> comparer = typeof(TKey) == typeof(string)
                ? (IEqualityComparer<TKey>)(object)StringComparer.Ordinal
                : EqualityComparer<TKey>.Default;
            var staging = new Dictionary<TKey, List<TRow>>(comparer);
            for (int i = 0; i < rows.Count; i++)
            {
                TKey key = selector(rows[i]);
                if (!staging.TryGetValue(key, out List<TRow> values))
                {
                    values = new List<TRow>();
                    staging.Add(key, values);
                }
                else if (unique)
                {
                    throw new EmberTableDataException(
                        EmberTableErrorCode.DuplicateSecondaryKey,
                        $"Secondary index '{name}' contains duplicate key '{key}'.",
                        name);
                }
                values.Add(rows[i]);
            }

            _rowsByKey = new Dictionary<TKey, ReadOnlyCollection<TRow>>(staging.Count, comparer);
            foreach (KeyValuePair<TKey, List<TRow>> pair in staging)
                _rowsByKey.Add(pair.Key, new ReadOnlyCollection<TRow>(pair.Value));
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [NoGC]
        public bool TryGet(TKey key, out IReadOnlyList<TRow> rows)
        {
            if (_rowsByKey.TryGetValue(key, out ReadOnlyCollection<TRow> values))
            {
                rows = values;
                return true;
            }

            rows = null;
            return false;
        }

        #endregion
    }
}
