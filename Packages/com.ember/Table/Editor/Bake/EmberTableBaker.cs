// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.Table.Editor
{
    public sealed class EmberTableBakeArtifact
    {
        public EmberTableValidatedData Table { get; }
        public byte[] Payload { get; }
        public byte[] FileBytes { get; }

        public EmberTableBakeArtifact(EmberTableValidatedData table, byte[] payload, byte[] fileBytes)
        {
            Table = table;
            Payload = (byte[])payload.Clone();
            FileBytes = (byte[])fileBytes.Clone();
        }
    }

    /// <summary>确定性写入 V1，并在提交前通过 Runtime Codec 回读和逐字段比对。</summary>
    public static class EmberTableBaker
    {
        public static bool TryBake(
            EmberTableValidatedData table,
            out EmberTableBakeArtifact artifact,
            out EmberTableDiagnostic diagnostic)
        {
            artifact = null;
            diagnostic = null;
            if (table == null)
            {
                diagnostic = Error(null, EmberTableErrorCode.DecodeFailed, "Validated table data is missing.");
                return false;
            }

            try
            {
                var payloadWriter = new EmberTableBinaryWriter();
                for (int row = 0; row < table.Rows.Count; row++)
                    for (int column = 0; column < table.Schema.Columns.Count; column++)
                        EmberTableValueCodec.WriteValue(
                            payloadWriter,
                            table.Schema.Columns[column],
                            table.Rows[row][column],
                            table.Definition.MaxStringBytes);

                byte[] payload = payloadWriter.ToArray();
                byte[] file = EmberTableBinaryFormat.BuildFile(
                    table.Schema.TableId,
                    table.Schema.RowTypeId,
                    table.Schema.SchemaHashBytes,
                    table.SourceHashBytes,
                    table.Rows.Count,
                    payload);
                if (file.Length > table.Definition.MaxFileBytes)
                {
                    diagnostic = Error(
                        table.Schema.TableId,
                        EmberTableErrorCode.LimitExceeded,
                        $"Baked file length {file.Length} exceeds limit {table.Definition.MaxFileBytes}.");
                    return false;
                }

                var binding = new VerificationBinding(table);
                var entry = new EmberTableCatalogEntry(
                    binding,
                    table.Definition.Required,
                    table.Definition.MaxFileBytes,
                    table.Definition.MaxRows,
                    table.Definition.MaxStringBytes);
                var codec = new EmberTableCodecV1();
                if (!codec.TryDecode(entry, file, out _, out EmberTableBinaryHeader header, out diagnostic))
                    return false;
                if (!string.Equals(header.SourceHash, table.SourceHash, StringComparison.Ordinal))
                {
                    diagnostic = Error(table.Schema.TableId, EmberTableErrorCode.DecodeFailed, "Read-back Source Hash differs from staged data.");
                    return false;
                }

                artifact = new EmberTableBakeArtifact(table, payload, file);
                return true;
            }
            catch (Exception ex)
            {
                diagnostic = Error(table.Schema.TableId, EmberTableErrorCode.DecodeFailed, ex.Message);
                return false;
            }
        }

        private static EmberTableDiagnostic Error(
            string tableId,
            EmberTableErrorCode code,
            string message)
        {
            return new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                tableId);
        }

        private sealed class VerificationBinding : IEmberTableBinding
        {
            private readonly EmberTableValidatedData _expected;

            public string TableId => _expected.Schema.TableId;
            public string RowTypeId => _expected.Schema.RowTypeId;
            public string ResourcePath => _expected.Definition.GetLogicalResourcePath();
            public ushort FormatVersion => EmberTableBinaryFormat.VERSION;
            public string SchemaHash => _expected.Schema.SchemaHash;

            public VerificationBinding(EmberTableValidatedData expected)
            {
                _expected = expected;
            }

            public IEmberTableData ReadTable(EmberTableBinaryReader reader, int rowCount)
            {
                if (rowCount != _expected.Rows.Count)
                    throw new EmberTableDataException(
                        EmberTableErrorCode.DecodeFailed,
                        $"Read-back row count {rowCount} differs from expected {_expected.Rows.Count}.");
                for (int row = 0; row < rowCount; row++)
                {
                    for (int column = 0; column < _expected.Schema.Columns.Count; column++)
                    {
                        object actual = EmberTableValueCodec.ReadValue(reader, _expected.Schema.Columns[column]);
                        object expected = _expected.Rows[row][column];
                        if (!Equals(actual, expected))
                            throw new EmberTableDataException(
                                EmberTableErrorCode.DecodeFailed,
                                $"Read-back mismatch at row {row + 1}, column '{_expected.Schema.Columns[column].ColumnName}'.",
                                _expected.Schema.Columns[column].ColumnName);
                    }
                }
                return new VerificationTableData(TableId, RowTypeId, rowCount);
            }
        }

        private sealed class VerificationTableData : IEmberTableData
        {
            public string TableId { get; }
            public string RowTypeId { get; }
            public int Count { get; }

            public VerificationTableData(string tableId, string rowTypeId, int count)
            {
                TableId = tableId;
                RowTypeId = rowTypeId;
                Count = count;
            }
        }
    }
}
