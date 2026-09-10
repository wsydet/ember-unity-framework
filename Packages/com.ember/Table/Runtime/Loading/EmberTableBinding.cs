// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

namespace Ember.Table
{
    /// <summary>每表生成 Binding 的强类型基类；加载路径不使用反射或 Activator。</summary>
    public abstract class EmberTableBinding<TRow> : IEmberTableBinding
    {
        #region 内部参数

        public string TableId { get; }
        public string RowTypeId { get; }
        public string ResourcePath { get; }
        public ushort FormatVersion => EmberTableBinaryFormat.VERSION;
        public string SchemaHash { get; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        protected EmberTableBinding(
            string tableId,
            string rowTypeId,
            string resourcePath,
            string schemaHash)
        {
            if (string.IsNullOrEmpty(tableId))
                throw new ArgumentException("Table ID cannot be null or empty.", nameof(tableId));
            if (string.IsNullOrEmpty(rowTypeId))
                throw new ArgumentException("Row Type ID cannot be null or empty.", nameof(rowTypeId));
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentException("Resource path cannot be null or empty.", nameof(resourcePath));
            EmberTableBinaryFormat.ParseHash(schemaHash);

            TableId = tableId;
            RowTypeId = rowTypeId;
            ResourcePath = resourcePath;
            SchemaHash = schemaHash.ToLowerInvariant();
        }

        protected abstract TRow ReadRow(EmberTableBinaryReader reader);

        protected abstract string GetPrimaryKey(TRow row);

        protected virtual void BuildSecondaryIndexes(EmberTable<TRow> table)
        {
        }

        protected void AddSecondaryIndex<TKey>(
            EmberTable<TRow> table,
            string name,
            Func<TRow, TKey> selector,
            bool unique = false)
        {
            table.AddIndex(name, selector, unique);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [Ember.Basic.HasGC]
        public IEmberTableData ReadTable(EmberTableBinaryReader reader, int rowCount)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));

            var rows = new List<TRow>(rowCount);
            for (int i = 0; i < rowCount; i++) rows.Add(ReadRow(reader));
            var table = new EmberTable<TRow>(TableId, RowTypeId, rows, GetPrimaryKey);
            BuildSecondaryIndexes(table);
            return table;
        }

        #endregion
    }
}
