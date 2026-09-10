// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.Table
{
    /// <summary>严格验证 ETBL V1 文件头、Hash、长度和 Payload 消费边界。</summary>
    public sealed class EmberTableCodecV1 : IEmberTableCodec
    {
        private const int MAX_IDENTIFIER_BYTES = 4096;

        #region 外部方法

        public bool TryDecode(
            EmberTableCatalogEntry entry,
            byte[] fileBytes,
            out IEmberTableData table,
            out EmberTableBinaryHeader header,
            out EmberTableDiagnostic diagnostic)
        {
            table = null;
            header = null;
            diagnostic = null;
            string tableId = entry?.TableId;

            if (entry == null || entry.Binding == null)
                return Fail(EmberTableErrorCode.CatalogMissing, "Catalog entry or Binding is missing.", tableId, out diagnostic);
            if (fileBytes == null)
                return Fail(EmberTableErrorCode.ResourceMissing, "Table resource returned no bytes.", tableId, out diagnostic);
            if (entry.MaxFileBytes <= 0 || entry.MaxRows < 0 || entry.MaxStringBytes < 0)
                return Fail(EmberTableErrorCode.InvalidLength, "Catalog limits must be positive.", tableId, out diagnostic);
            if (fileBytes.Length > entry.MaxFileBytes)
                return Fail(
                    EmberTableErrorCode.LimitExceeded,
                    $"File length {fileBytes.Length} exceeds limit {entry.MaxFileBytes}.",
                    tableId,
                    out diagnostic);

            try
            {
                var reader = new EmberTableBinaryReader(fileBytes, maxStringBytes: entry.MaxStringBytes);
                if (reader.ReadByte() != (byte)'E'
                    || reader.ReadByte() != (byte)'T'
                    || reader.ReadByte() != (byte)'B'
                    || reader.ReadByte() != (byte)'L')
                    return Fail(EmberTableErrorCode.InvalidMagic, "Magic must be ASCII ETBL.", tableId, out diagnostic);

                ushort version = reader.ReadUInt16();
                if (version != EmberTableBinaryFormat.VERSION || version != entry.Binding.FormatVersion)
                    return Fail(
                        EmberTableErrorCode.UnsupportedFormatVersion,
                        $"Format version {version} is not supported.",
                        tableId,
                        out diagnostic);

                ushort flags = reader.ReadUInt16();
                if (flags != EmberTableBinaryFormat.FLAGS)
                    return Fail(
                        EmberTableErrorCode.UnsupportedFlags,
                        $"V1 flags must be 0, got {flags}.",
                        tableId,
                        out diagnostic);

                string encodedTableId = ReadIdentifier(reader);
                string rowTypeId = ReadIdentifier(reader);
                byte[] schemaHash = reader.ReadBytes(EmberTableBinaryFormat.HASH_BYTES);
                byte[] sourceHash = reader.ReadBytes(EmberTableBinaryFormat.HASH_BYTES);
                int rowCount = reader.ReadInt32();
                int payloadLength = reader.ReadInt32();
                byte[] expectedPayloadHash = reader.ReadBytes(EmberTableBinaryFormat.HASH_BYTES);

                if (!string.Equals(encodedTableId, entry.Binding.TableId, StringComparison.Ordinal))
                    return Fail(
                        EmberTableErrorCode.TableIdMismatch,
                        $"File Table ID '{encodedTableId}' does not match Binding '{entry.Binding.TableId}'.",
                        tableId,
                        out diagnostic);
                if (!string.Equals(rowTypeId, entry.Binding.RowTypeId, StringComparison.Ordinal))
                    return Fail(
                        EmberTableErrorCode.RowTypeIdMismatch,
                        $"File Row Type ID '{rowTypeId}' does not match Binding '{entry.Binding.RowTypeId}'.",
                        tableId,
                        out diagnostic);

                byte[] bindingSchemaHash = EmberTableBinaryFormat.ParseHash(entry.Binding.SchemaHash);
                if (!EmberTableBinaryFormat.HashEquals(schemaHash, bindingSchemaHash))
                    return Fail(EmberTableErrorCode.SchemaHashMismatch, "File and Binding Schema Hash differ.", tableId, out diagnostic);
                if (rowCount < 0 || payloadLength < 0)
                    return Fail(EmberTableErrorCode.InvalidLength, "Row count and Payload length cannot be negative.", tableId, out diagnostic);
                if (rowCount > entry.MaxRows)
                    return Fail(
                        EmberTableErrorCode.LimitExceeded,
                        $"Row count {rowCount} exceeds limit {entry.MaxRows}.",
                        tableId,
                        out diagnostic);
                if (payloadLength > entry.MaxFileBytes || payloadLength != reader.Remaining)
                    return Fail(
                        EmberTableErrorCode.PayloadLengthMismatch,
                        $"Payload declares {payloadLength} bytes but file contains {reader.Remaining} remaining bytes.",
                        tableId,
                        out diagnostic);

                int payloadOffset = reader.Position;
                byte[] actualPayloadHash = EmberTableBinaryFormat.ComputeSha256(fileBytes, payloadOffset, payloadLength);
                if (!EmberTableBinaryFormat.HashEquals(expectedPayloadHash, actualPayloadHash))
                    return Fail(EmberTableErrorCode.PayloadHashMismatch, "Payload SHA-256 does not match the file header.", tableId, out diagnostic);

                header = new EmberTableBinaryHeader(
                    version,
                    flags,
                    encodedTableId,
                    rowTypeId,
                    EmberTableBinaryFormat.ToHex(schemaHash),
                    EmberTableBinaryFormat.ToHex(sourceHash),
                    rowCount,
                    payloadLength,
                    EmberTableBinaryFormat.ToHex(expectedPayloadHash));

                var payloadReader = new EmberTableBinaryReader(
                    fileBytes,
                    payloadOffset,
                    payloadLength,
                    entry.MaxStringBytes);
                table = entry.Binding.ReadTable(payloadReader, rowCount);
                if (!payloadReader.IsFullyConsumed)
                {
                    table = null;
                    return Fail(
                        EmberTableErrorCode.PayloadTrailingData,
                        $"Binding left {payloadReader.Remaining} unread Payload bytes.",
                        tableId,
                        out diagnostic);
                }

                return true;
            }
            catch (EmberTableDataException ex)
            {
                table = null;
                return Fail(ex.Code, ex.Message, tableId, out diagnostic, ex.Field);
            }
            catch (Exception ex)
            {
                table = null;
                return Fail(EmberTableErrorCode.DecodeFailed, ex.Message, tableId, out diagnostic);
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static string ReadIdentifier(EmberTableBinaryReader reader)
        {
            int byteLength = reader.ReadUInt16();
            if (byteLength == 0)
                throw new EmberTableDataException(
                    EmberTableErrorCode.InvalidLength,
                    "Header identifier cannot be empty.");
            return reader.ReadUtf8(byteLength, MAX_IDENTIFIER_BYTES);
        }

        private static bool Fail(
            EmberTableErrorCode code,
            string message,
            string tableId,
            out EmberTableDiagnostic diagnostic,
            string field = null)
        {
            diagnostic = new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                tableId,
                field: field);
            return false;
        }

        #endregion
    }
}
