// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Ember.Basic;

namespace Ember.Table
{
    /// <summary>一次加载批次产生的不可变表快照；不使用静态全局缓存。</summary>
    public sealed class EmberTableDatabase
    {
        #region 内部参数

        private readonly Dictionary<string, IEmberTableData> _tables;
        private readonly ReadOnlyCollection<string> _tableIds;

        public int Count => _tables.Count;
        public IReadOnlyList<string> TableIds => _tableIds;

        public static EmberTableDatabase Empty { get; } =
            new EmberTableDatabase(new Dictionary<string, IEmberTableData>(StringComparer.Ordinal));

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        internal EmberTableDatabase(Dictionary<string, IEmberTableData> tables)
        {
            _tables = tables == null
                ? new Dictionary<string, IEmberTableData>(StringComparer.Ordinal)
                : new Dictionary<string, IEmberTableData>(tables, StringComparer.Ordinal);
            var ids = new List<string>(_tables.Keys);
            ids.Sort(StringComparer.Ordinal);
            _tableIds = new ReadOnlyCollection<string>(ids);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [NoGC]
        public bool ContainsTable(string tableId)
        {
            return tableId != null && _tables.ContainsKey(tableId);
        }

        [NoGC]
        public bool TryGetTable<TRow>(string tableId, out EmberTable<TRow> table)
        {
            if (tableId != null
                && _tables.TryGetValue(tableId, out IEmberTableData value)
                && value is EmberTable<TRow> typed)
            {
                table = typed;
                return true;
            }

            table = null;
            return false;
        }

        [NoGC]
        public bool TryGetTable(string tableId, out IEmberTableData table)
        {
            if (tableId != null) return _tables.TryGetValue(tableId, out table);
            table = null;
            return false;
        }

        #endregion
    }
}
