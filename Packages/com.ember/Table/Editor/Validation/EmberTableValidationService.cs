// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;

namespace Ember.Table.Editor
{
    public sealed class EmberTableValidatedData
    {
        public EmberTableDefinition Definition { get; }
        public EmberTableSchema Schema { get; }
        public IReadOnlyList<object[]> Rows { get; }
        public IReadOnlyList<EmberDelimitedCell[]> SourceCells { get; }
        public byte[] SourceHashBytes { get; }
        public string SourceHash { get; }

        public EmberTableValidatedData(
            EmberTableDefinition definition,
            EmberTableSchema schema,
            IList<object[]> rows,
            IList<EmberDelimitedCell[]> sourceCells,
            byte[] sourceHash)
        {
            Definition = definition;
            Schema = schema;
            Rows = new List<object[]>(rows).AsReadOnly();
            SourceCells = new List<EmberDelimitedCell[]>(sourceCells).AsReadOnly();
            SourceHashBytes = (byte[])sourceHash.Clone();
            SourceHash = EmberTableBinaryFormat.ToHex(sourceHash);
        }
    }

    public sealed class EmberTableValidationResult
    {
        public bool Succeeded { get; }
        public IReadOnlyList<EmberTableDiagnostic> Diagnostics { get; }
        public IReadOnlyList<EmberTableValidatedData> Tables { get; }

        public EmberTableValidationResult(
            bool succeeded,
            IList<EmberTableDiagnostic> diagnostics,
            IList<EmberTableValidatedData> tables)
        {
            Succeeded = succeeded;
            Diagnostics = new List<EmberTableDiagnostic>(diagnostics).AsReadOnly();
            Tables = new List<EmberTableValidatedData>(tables).AsReadOnly();
        }
    }

    /// <summary>Definition、Schema、主键、类型和跨表引用的 Editor 严格校验。</summary>
    public static class EmberTableValidationService
    {
        public static EmberTableValidationResult ValidateAll(IList<EmberTableDefinition> definitions)
        {
            var diagnostics = new List<EmberTableDiagnostic>();
            var tables = new List<EmberTableValidatedData>();
            var tableIds = new HashSet<string>(StringComparer.Ordinal);
            definitions ??= Array.Empty<EmberTableDefinition>();

            for (int i = 0; i < definitions.Count; i++)
            {
                EmberTableDefinition definition = definitions[i];
                string definitionPath = definition ? AssetDatabase.GetAssetPath(definition) : null;
                if (!definition || string.IsNullOrEmpty(definition.TableId))
                {
                    diagnostics.Add(Error(null, definitionPath, EmberTableErrorCode.InvalidRowType, "Definition or Table ID is missing."));
                    continue;
                }
                if (!tableIds.Add(definition.TableId))
                {
                    diagnostics.Add(Error(
                        definition.TableId,
                        definitionPath,
                        EmberTableErrorCode.DuplicateTableId,
                        $"Table ID '{definition.TableId}' is declared by more than one Definition."));
                    continue;
                }

                if (TryValidateDefinition(definition, diagnostics, out EmberTableValidatedData data))
                    tables.Add(data);
            }

            ValidateReferences(tables, diagnostics);
            bool success = diagnostics.All(item => item.Severity != EmberTableDiagnosticSeverity.Error);
            return new EmberTableValidationResult(success, diagnostics, tables);
        }

        public static EmberTableValidationResult Validate(EmberTableDefinition definition)
        {
            return ValidateAll(new[] { definition });
        }

        public static byte[] ComputeSourceHash(IReadOnlyList<IReadOnlyList<string>> matrix)
        {
            var writer = new EmberTableBinaryWriter();
            writer.WriteString("EmberTableSourceV1");
            writer.WriteInt32(matrix?.Count ?? 0);
            if (matrix != null)
            {
                for (int row = 0; row < matrix.Count; row++)
                {
                    IReadOnlyList<string> cells = matrix[row];
                    writer.WriteInt32(cells?.Count ?? 0);
                    if (cells == null) continue;
                    for (int column = 0; column < cells.Count; column++)
                        writer.WriteString(cells[column] ?? string.Empty);
                }
            }
            return EmberTableBinaryFormat.ComputeSha256(writer.ToArray());
        }

        private static bool TryValidateDefinition(
            EmberTableDefinition definition,
            List<EmberTableDiagnostic> diagnostics,
            out EmberTableValidatedData data)
        {
            data = null;
            int errorsBefore = CountErrors(diagnostics);
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            if (!IsSafeTableId(definition.TableId))
                diagnostics.Add(Error(definition.TableId, definitionPath, EmberTableErrorCode.InvalidRowType, "Table ID may only contain letters, digits, '.', '_' and '-'."));
            if (definition.MaxFileBytes <= 0 || definition.MaxRows < 0 || definition.MaxStringBytes < 0)
                diagnostics.Add(Error(definition.TableId, definitionPath, EmberTableErrorCode.InvalidLength, "Definition limits are invalid."));
            if (!TryValidateOutputPath(definition.RuntimeOutputPath, out string pathError))
                diagnostics.Add(Error(definition.TableId, definitionPath, EmberTableErrorCode.OutputPathInvalid, pathError));
            if (!definition.Source)
                diagnostics.Add(Error(definition.TableId, definitionPath, EmberTableErrorCode.CsvSyntax, "Definition has no CSV/TSV source."));

            EmberTableSchema schema = null;
            if (!EmberTableSchemaAnalyzer.TryAnalyze(
                    definition.RowType,
                    definition.TableId,
                    definitionPath,
                    out schema,
                    out IReadOnlyList<EmberTableDiagnostic> schemaDiagnostics))
                diagnostics.AddRange(schemaDiagnostics);
            else
                ValidateIndexes(definition, schema, definitionPath, diagnostics);

            if (CountErrors(diagnostics) != errorsBefore || !definition.Source || schema == null)
                return false;

            string sourcePath = AssetDatabase.GetAssetPath(definition.Source);
            char delimiter;
            if (sourcePath.EndsWith(".etable.csv", StringComparison.OrdinalIgnoreCase)) delimiter = ',';
            else if (sourcePath.EndsWith(".etable.tsv", StringComparison.OrdinalIgnoreCase)) delimiter = '\t';
            else
            {
                diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.CsvSyntax, "Source extension must be .etable.csv or .etable.tsv."));
                return false;
            }

            if (!EmberDelimitedTextParser.TryParse(
                    definition.Source.bytes,
                    delimiter,
                    sourcePath,
                    out EmberDelimitedDocument document,
                    out EmberTableDiagnostic parseDiagnostic))
            {
                diagnostics.Add(new EmberTableDiagnostic(
                    parseDiagnostic.Severity,
                    parseDiagnostic.Code,
                    parseDiagnostic.Message,
                    definition.TableId,
                    parseDiagnostic.FilePath,
                    parseDiagnostic.Line,
                    parseDiagnostic.Column,
                    parseDiagnostic.Field));
                return false;
            }
            if (document.Rows.Count == 0)
            {
                diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.MissingColumn, "Source has no header row."));
                return false;
            }

            EmberDelimitedRow header = document.Rows[0];
            var sourceColumns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < header.Cells.Count; i++)
            {
                EmberDelimitedCell cell = header.Cells[i];
                if (string.IsNullOrEmpty(cell.Value))
                    diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.MissingColumn, "Column name cannot be empty.", null, cell.Line, cell.Column));
                else if (!sourceColumns.TryAdd(cell.Value, i))
                    diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.DuplicateColumn, $"Column '{cell.Value}' appears more than once.", cell.Value, cell.Line, cell.Column));
            }

            var schemaColumns = new HashSet<string>(schema.Columns.Select(item => item.ColumnName), StringComparer.Ordinal);
            for (int i = 0; i < schema.Columns.Count; i++)
            {
                EmberTableColumnSchema column = schema.Columns[i];
                if (!sourceColumns.ContainsKey(column.ColumnName))
                    diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.MissingColumn, $"Required column '{column.ColumnName}' is missing.", column.ColumnName, header.Line, 1));
            }
            for (int i = 0; i < header.Cells.Count; i++)
            {
                EmberDelimitedCell cell = header.Cells[i];
                if (schemaColumns.Contains(cell.Value)) continue;
                diagnostics.Add(new EmberTableDiagnostic(
                    definition.UnknownColumnPolicy == EmberTableUnknownColumnPolicy.Error
                        ? EmberTableDiagnosticSeverity.Error
                        : EmberTableDiagnosticSeverity.Warning,
                    EmberTableErrorCode.UnknownColumn,
                    $"Unknown source column '{cell.Value}'.",
                    definition.TableId,
                    sourcePath,
                    cell.Line,
                    cell.Column,
                    cell.Value));
            }
            if (diagnostics.Any(item => item.Severity == EmberTableDiagnosticSeverity.Error && item.TableId == definition.TableId))
                return false;

            int rowCount = document.Rows.Count - 1;
            if (rowCount > definition.MaxRows)
            {
                diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.LimitExceeded, $"Row count {rowCount} exceeds limit {definition.MaxRows}."));
                return false;
            }

            var rows = new List<object[]>(rowCount);
            var rowCells = new List<EmberDelimitedCell[]>(rowCount);
            var primaryKeys = new HashSet<string>(StringComparer.Ordinal);
            var matrix = new List<IReadOnlyList<string>>(rowCount + 1)
            {
                schema.Columns.Select(item => item.ColumnName).ToArray(),
            };

            for (int rowIndex = 1; rowIndex < document.Rows.Count; rowIndex++)
            {
                EmberDelimitedRow row = document.Rows[rowIndex];
                if (row.Cells.Count != header.Cells.Count)
                {
                    diagnostics.Add(Error(
                        definition.TableId,
                        sourcePath,
                        EmberTableErrorCode.CsvSyntax,
                        $"Row has {row.Cells.Count} cells; header has {header.Cells.Count}.",
                        null,
                        row.Line,
                        1));
                    continue;
                }

                var values = new object[schema.Columns.Count];
                var locations = new EmberDelimitedCell[schema.Columns.Count];
                var canonicalCells = new string[schema.Columns.Count];
                bool rowValid = true;
                for (int schemaIndex = 0; schemaIndex < schema.Columns.Count; schemaIndex++)
                {
                    EmberTableColumnSchema column = schema.Columns[schemaIndex];
                    EmberDelimitedCell cell = row.Cells[sourceColumns[column.ColumnName]];
                    locations[schemaIndex] = cell;
                    canonicalCells[schemaIndex] = cell.Value;
                    if (!EmberTableValueCodec.TryParse(cell.Value, column, out object value, out string valueError))
                    {
                        diagnostics.Add(Error(
                            definition.TableId,
                            sourcePath,
                            EmberTableErrorCode.InvalidValue,
                            valueError,
                            column.ColumnName,
                            cell.Line,
                            cell.Column));
                        rowValid = false;
                        continue;
                    }
                    if (column.ValueType == typeof(string)
                        && Encoding.UTF8.GetByteCount((string)value) > definition.MaxStringBytes)
                    {
                        diagnostics.Add(Error(
                            definition.TableId,
                            sourcePath,
                            EmberTableErrorCode.LimitExceeded,
                            $"String exceeds UTF-8 byte limit {definition.MaxStringBytes}.",
                            column.ColumnName,
                            cell.Line,
                            cell.Column));
                        rowValid = false;
                    }
                    values[schemaIndex] = value;
                }

                if (rowValid)
                {
                    int keyIndex = IndexOf(schema.Columns, schema.PrimaryKey);
                    string key = values[keyIndex] as string;
                    EmberDelimitedCell keyCell = locations[keyIndex];
                    if (string.IsNullOrEmpty(key))
                    {
                        diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.EmptyPrimaryKey, "Primary key cannot be null or empty.", schema.PrimaryKey.ColumnName, keyCell.Line, keyCell.Column));
                        rowValid = false;
                    }
                    else if (!primaryKeys.Add(key))
                    {
                        diagnostics.Add(Error(definition.TableId, sourcePath, EmberTableErrorCode.DuplicatePrimaryKey, $"Primary key '{key}' occurs more than once.", schema.PrimaryKey.ColumnName, keyCell.Line, keyCell.Column));
                        rowValid = false;
                    }
                }

                if (!rowValid) continue;
                rows.Add(values);
                rowCells.Add(locations);
                matrix.Add(canonicalCells);
            }

            if (diagnostics.Any(item => item.Severity == EmberTableDiagnosticSeverity.Error && item.TableId == definition.TableId))
                return false;

            byte[] sourceHash = ComputeSourceHash(matrix);
            data = new EmberTableValidatedData(definition, schema, rows, rowCells, sourceHash);
            return true;
        }

        private static void ValidateReferences(
            IList<EmberTableValidatedData> tables,
            List<EmberTableDiagnostic> diagnostics)
        {
            var keysByTable = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            for (int i = 0; i < tables.Count; i++)
            {
                EmberTableValidatedData table = tables[i];
                int keyIndex = IndexOf(table.Schema.Columns, table.Schema.PrimaryKey);
                var keys = new HashSet<string>(StringComparer.Ordinal);
                for (int row = 0; row < table.Rows.Count; row++) keys.Add((string)table.Rows[row][keyIndex]);
                keysByTable.Add(table.Schema.TableId, keys);
            }

            for (int tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                EmberTableValidatedData table = tables[tableIndex];
                string sourcePath = AssetDatabase.GetAssetPath(table.Definition.Source);
                for (int columnIndex = 0; columnIndex < table.Schema.Columns.Count; columnIndex++)
                {
                    EmberTableColumnSchema column = table.Schema.Columns[columnIndex];
                    if (column.ReferenceTableId == null) continue;
                    if (!keysByTable.ContainsKey(column.ReferenceTableId))
                    {
                        diagnostics.Add(Error(
                            table.Schema.TableId,
                            sourcePath,
                            EmberTableErrorCode.MissingReference,
                            $"Referenced target table '{column.ReferenceTableId}' is not defined.",
                            column.ColumnName));
                        continue;
                    }
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        string value = table.Rows[row][columnIndex] as string;
                        HashSet<string> targetKeys = keysByTable[column.ReferenceTableId];
                        if (value != null && targetKeys.Contains(value))
                            continue;
                        EmberDelimitedCell cell = table.SourceCells[row][columnIndex];
                        diagnostics.Add(Error(
                            table.Schema.TableId,
                            sourcePath,
                            EmberTableErrorCode.MissingReference,
                            $"Reference '{value}' does not exist in table '{column.ReferenceTableId}'.",
                            column.ColumnName,
                            cell.Line,
                            cell.Column));
                    }
                }
            }
        }

        private static void ValidateIndexes(
            EmberTableDefinition definition,
            EmberTableSchema schema,
            string path,
            List<EmberTableDiagnostic> diagnostics)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definition.SecondaryIndexes.Count; i++)
            {
                EmberTableSecondaryIndexDefinition index = definition.SecondaryIndexes[i];
                if (index == null || string.IsNullOrEmpty(index.Name) || !names.Add(index.Name))
                {
                    diagnostics.Add(Error(definition.TableId, path, EmberTableErrorCode.DuplicateSecondaryKey, "Secondary index names must be non-empty and unique."));
                    continue;
                }
                if (!schema.Columns.Any(item => string.Equals(item.ColumnName, index.ColumnName, StringComparison.Ordinal)))
                    diagnostics.Add(Error(definition.TableId, path, EmberTableErrorCode.MissingColumn, $"Secondary index '{index.Name}' references unknown column '{index.ColumnName}'.", index.ColumnName));
            }
        }

        private static bool TryValidateOutputPath(string path, out string error)
        {
            string normalized = path?.Replace('\\', '/');
            if (string.IsNullOrEmpty(normalized)
                || !normalized.StartsWith("Assets/GameResource/Resources/", StringComparison.Ordinal)
                || !normalized.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/../")
                || normalized.Contains("/./"))
            {
                error = "Output must be a .bytes file below Assets/GameResource/Resources and cannot traverse directories.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool IsSafeTableId(string tableId)
        {
            if (string.IsNullOrEmpty(tableId)) return false;
            for (int i = 0; i < tableId.Length; i++)
            {
                char value = tableId[i];
                if (!char.IsLetterOrDigit(value) && value != '.' && value != '_' && value != '-') return false;
            }
            return true;
        }

        private static int IndexOf(IReadOnlyList<EmberTableColumnSchema> columns, EmberTableColumnSchema target)
        {
            for (int i = 0; i < columns.Count; i++) if (ReferenceEquals(columns[i], target)) return i;
            return -1;
        }

        private static int CountErrors(IEnumerable<EmberTableDiagnostic> diagnostics)
        {
            return diagnostics.Count(item => item.Severity == EmberTableDiagnosticSeverity.Error);
        }

        private static EmberTableDiagnostic Error(
            string tableId,
            string path,
            EmberTableErrorCode code,
            string message,
            string field = null,
            int line = 0,
            int column = 0)
        {
            return new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                tableId,
                path,
                line,
                column,
                field);
        }
    }

    /// <summary>Editor-only 严格文本类型转换与 V1 字段写入。</summary>
    public static class EmberTableValueCodec
    {
        public static bool TryParse(
            string text,
            EmberTableColumnSchema column,
            out object value,
            out string error)
        {
            value = null;
            error = null;
            Type type = column.StorageType;
            if (column.IsNullable && string.IsNullOrEmpty(text)) return true;
            if (column.ValueType == typeof(string)) { value = text; return true; }

            bool success;
            if (type == typeof(bool))
            {
                if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
                if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
                success = false;
            }
            else if (type == typeof(sbyte)) { success = sbyte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte parsedSByte); value = parsedSByte; }
            else if (type == typeof(byte)) { success = byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsedByte); value = parsedByte; }
            else if (type == typeof(short)) { success = short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short parsedInt16); value = parsedInt16; }
            else if (type == typeof(ushort)) { success = ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsedUInt16); value = parsedUInt16; }
            else if (type == typeof(int)) { success = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt32); value = parsedInt32; }
            else if (type == typeof(uint)) { success = uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsedUInt32); value = parsedUInt32; }
            else if (type == typeof(long)) { success = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedInt64); value = parsedInt64; }
            else if (type == typeof(ulong)) { success = ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsedUInt64); value = parsedUInt64; }
            else if (type == typeof(float)) { success = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedSingle); value = parsedSingle; }
            else if (type == typeof(double)) { success = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedDouble); value = parsedDouble; }
            else if (type == typeof(decimal)) { success = decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsedDecimal); value = parsedDecimal; }
            else if (type.IsEnum)
            {
                success = Enum.TryParse(type, text, false, out object parsed) && Enum.IsDefined(type, parsed);
                value = parsed;
            }
            else success = false;

            if (!success)
            {
                value = null;
                error = $"Value '{text}' is not a valid {column.ValueType.Name} in InvariantCulture.";
            }
            return success;
        }

        public static void WriteValue(
            EmberTableBinaryWriter writer,
            EmberTableColumnSchema column,
            object value,
            int maxStringBytes)
        {
            if (column.IsNullable)
            {
                writer.WriteNullableMarker(value != null);
                if (value == null) return;
            }

            Type type = column.StorageType;
            if (column.ValueType == typeof(string)) writer.WriteString((string)value, maxStringBytes);
            else if (type == typeof(bool)) writer.WriteBoolean((bool)value);
            else if (type == typeof(sbyte)) writer.WriteSByte((sbyte)value);
            else if (type == typeof(byte)) writer.WriteByte((byte)value);
            else if (type == typeof(short)) writer.WriteInt16((short)value);
            else if (type == typeof(ushort)) writer.WriteUInt16((ushort)value);
            else if (type == typeof(int)) writer.WriteInt32((int)value);
            else if (type == typeof(uint)) writer.WriteUInt32((uint)value);
            else if (type == typeof(long)) writer.WriteInt64((long)value);
            else if (type == typeof(ulong)) writer.WriteUInt64((ulong)value);
            else if (type == typeof(float)) writer.WriteSingle((float)value);
            else if (type == typeof(double)) writer.WriteDouble((double)value);
            else if (type == typeof(decimal)) writer.WriteDecimal((decimal)value);
            else if (type.IsEnum) WriteEnum(writer, type, value);
            else throw new NotSupportedException(type.FullName);
        }

        public static object ReadValue(
            EmberTableBinaryReader reader,
            EmberTableColumnSchema column)
        {
            if (column.IsNullable && !reader.ReadNullableMarker()) return null;
            Type type = column.StorageType;
            if (column.ValueType == typeof(string)) return reader.ReadString();
            if (type == typeof(bool)) return reader.ReadBoolean();
            if (type == typeof(sbyte)) return reader.ReadSByte();
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            if (type == typeof(float)) return reader.ReadSingle();
            if (type == typeof(double)) return reader.ReadDouble();
            if (type == typeof(decimal)) return reader.ReadDecimal();
            if (type.IsEnum)
            {
                object raw = ReadEnumStorage(reader, Enum.GetUnderlyingType(type));
                object enumValue = Enum.ToObject(type, raw);
                if (!Enum.IsDefined(type, enumValue))
                    throw new EmberTableDataException(
                        EmberTableErrorCode.InvalidValue,
                        $"Enum value '{raw}' is not defined by {type.FullName}.",
                        column.ColumnName);
                return enumValue;
            }
            throw new NotSupportedException(type.FullName);
        }

        private static void WriteEnum(EmberTableBinaryWriter writer, Type enumType, object value)
        {
            Type type = Enum.GetUnderlyingType(enumType);
            if (type == typeof(sbyte)) writer.WriteSByte(Convert.ToSByte(value, CultureInfo.InvariantCulture));
            else if (type == typeof(byte)) writer.WriteByte(Convert.ToByte(value, CultureInfo.InvariantCulture));
            else if (type == typeof(short)) writer.WriteInt16(Convert.ToInt16(value, CultureInfo.InvariantCulture));
            else if (type == typeof(ushort)) writer.WriteUInt16(Convert.ToUInt16(value, CultureInfo.InvariantCulture));
            else if (type == typeof(int)) writer.WriteInt32(Convert.ToInt32(value, CultureInfo.InvariantCulture));
            else if (type == typeof(uint)) writer.WriteUInt32(Convert.ToUInt32(value, CultureInfo.InvariantCulture));
            else if (type == typeof(long)) writer.WriteInt64(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            else if (type == typeof(ulong)) writer.WriteUInt64(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
        }

        private static object ReadEnumStorage(EmberTableBinaryReader reader, Type type)
        {
            if (type == typeof(sbyte)) return reader.ReadSByte();
            if (type == typeof(byte)) return reader.ReadByte();
            if (type == typeof(short)) return reader.ReadInt16();
            if (type == typeof(ushort)) return reader.ReadUInt16();
            if (type == typeof(int)) return reader.ReadInt32();
            if (type == typeof(uint)) return reader.ReadUInt32();
            if (type == typeof(long)) return reader.ReadInt64();
            if (type == typeof(ulong)) return reader.ReadUInt64();
            throw new NotSupportedException(type.FullName);
        }
    }
}
