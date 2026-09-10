// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

namespace Ember.Table
{
    /// <summary>纯实例表引擎：构建 staging database，并且只在 Required 表全部成功后交换快照。</summary>
    public sealed class EmberTableEngine : IDisposable
    {
        #region 内部参数

        private readonly IEmberTableCodec _codec;
        private EmberTableDatabase _database;
        private bool _disposed;

        public EmberTableDatabase Database => _database;
        public bool IsDisposed => _disposed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public EmberTableEngine(IEmberTableCodec codec = null)
        {
            _codec = codec ?? new EmberTableCodecV1();
            _database = EmberTableDatabase.Empty;
        }

        [HasGC]
        public EmberTableLoadResult Load(
            IEmberTableCatalog catalog,
            IReadOnlyDictionary<string, byte[]> bytesByTableId)
        {
            var diagnostics = new List<EmberTableDiagnostic>();
            var tableInfos = new List<EmberTableLoadInfo>();
            if (_disposed)
            {
                diagnostics.Add(Error(EmberTableErrorCode.EngineDisposed, "The table Engine has been disposed."));
                return new EmberTableLoadResult(false, diagnostics, tableInfos);
            }
            if (catalog?.Entries == null)
            {
                diagnostics.Add(Error(EmberTableErrorCode.CatalogMissing, "Table Catalog is missing."));
                return new EmberTableLoadResult(false, diagnostics, tableInfos);
            }

            var seenTableIds = new HashSet<string>(StringComparer.Ordinal);
            bool catalogValid = true;
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                EmberTableCatalogEntry entry = catalog.Entries[i];
                string tableId = entry?.TableId;
                if (entry?.Binding == null || string.IsNullOrEmpty(tableId))
                {
                    diagnostics.Add(Error(EmberTableErrorCode.CatalogMissing, "Catalog contains an invalid entry.", tableId));
                    catalogValid = false;
                }
                else if (!seenTableIds.Add(tableId))
                {
                    diagnostics.Add(Error(
                        EmberTableErrorCode.DuplicateTableId,
                        $"Catalog contains duplicate Table ID '{tableId}'.",
                        tableId));
                    catalogValid = false;
                }
            }
            if (!catalogValid) return new EmberTableLoadResult(false, diagnostics, tableInfos);

            var staging = new Dictionary<string, IEmberTableData>(StringComparer.Ordinal);
            bool requiredFailed = false;
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                EmberTableCatalogEntry entry = catalog.Entries[i];
                string tableId = entry.TableId;
                byte[] bytes = null;
                if (bytesByTableId != null) bytesByTableId.TryGetValue(tableId, out bytes);
                if (!_codec.TryDecode(
                        entry,
                        bytes,
                        out IEmberTableData table,
                        out EmberTableBinaryHeader header,
                        out EmberTableDiagnostic diagnostic))
                {
                    if (entry.Required)
                    {
                        requiredFailed = true;
                        diagnostics.Add(diagnostic);
                    }
                    else
                    {
                        diagnostics.Add(new EmberTableDiagnostic(
                            EmberTableDiagnosticSeverity.Warning,
                            diagnostic.Code,
                            diagnostic.Message,
                            diagnostic.TableId,
                            diagnostic.FilePath,
                            diagnostic.Line,
                            diagnostic.Column,
                            diagnostic.Field));
                        diagnostics.Add(new EmberTableDiagnostic(
                            EmberTableDiagnosticSeverity.Information,
                            EmberTableErrorCode.OptionalTableOmitted,
                            "Optional table was omitted from the new snapshot.",
                            tableId));
                    }
                    tableInfos.Add(new EmberTableLoadInfo(tableId, entry.Required, false, 0, null));
                    continue;
                }

                staging.Add(tableId, table);
                tableInfos.Add(new EmberTableLoadInfo(
                    tableId,
                    entry.Required,
                    true,
                    table.Count,
                    header.SourceHash));
            }

            if (requiredFailed)
                return new EmberTableLoadResult(false, diagnostics, tableInfos);

            _database = new EmberTableDatabase(staging);
            return new EmberTableLoadResult(true, diagnostics, tableInfos);
        }

        [NoGC]
        public void Dispose()
        {
            if (_disposed) return;
            _database = EmberTableDatabase.Empty;
            _disposed = true;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static EmberTableDiagnostic Error(
            EmberTableErrorCode code,
            string message,
            string tableId = null)
        {
            return new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                tableId);
        }

        #endregion
    }
}
