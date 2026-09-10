// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ember.Table.Editor
{
    public sealed class EmberTableColumnSchema
    {
        public string ColumnName { get; }
        public string MemberName { get; }
        public Type ValueType { get; }
        public Type StorageType { get; }
        public string WireTypeId { get; }
        public bool IsNullable { get; }
        public bool IsPrimaryKey { get; }
        public string ReferenceTableId { get; }
        public MemberInfo Member { get; }

        public EmberTableColumnSchema(
            string columnName,
            string memberName,
            Type valueType,
            Type storageType,
            string wireTypeId,
            bool isNullable,
            bool isPrimaryKey,
            string referenceTableId,
            MemberInfo member)
        {
            ColumnName = columnName;
            MemberName = memberName;
            ValueType = valueType;
            StorageType = storageType;
            WireTypeId = wireTypeId;
            IsNullable = isNullable;
            IsPrimaryKey = isPrimaryKey;
            ReferenceTableId = referenceTableId;
            Member = member;
        }
    }

    public sealed class EmberTableSchema
    {
        public string TableId { get; }
        public string RowTypeId { get; }
        public Type RowType { get; }
        public ConstructorInfo Constructor { get; }
        public IReadOnlyList<EmberTableColumnSchema> Columns { get; }
        public IReadOnlyList<EmberTableColumnSchema> ConstructorColumns { get; }
        public byte[] SchemaHashBytes { get; }
        public string SchemaHash { get; }
        public EmberTableColumnSchema PrimaryKey { get; }

        public EmberTableSchema(
            string tableId,
            string rowTypeId,
            Type rowType,
            ConstructorInfo constructor,
            IList<EmberTableColumnSchema> columns,
            IList<EmberTableColumnSchema> constructorColumns,
            byte[] schemaHash)
        {
            TableId = tableId;
            RowTypeId = rowTypeId;
            RowType = rowType;
            Constructor = constructor;
            Columns = new List<EmberTableColumnSchema>(columns).AsReadOnly();
            ConstructorColumns = new List<EmberTableColumnSchema>(constructorColumns).AsReadOnly();
            SchemaHashBytes = (byte[])schemaHash.Clone();
            SchemaHash = EmberTableBinaryFormat.ToHex(schemaHash);
            PrimaryKey = Columns.First(item => item.IsPrimaryKey);
        }
    }

    /// <summary>Editor-only Row 反射分析；Runtime Binding 和查询路径不使用反射。</summary>
    public static class EmberTableSchemaAnalyzer
    {
        public static bool TryAnalyze(
            Type rowType,
            string definitionTableId,
            string definitionPath,
            out EmberTableSchema schema,
            out IReadOnlyList<EmberTableDiagnostic> diagnostics)
        {
            schema = null;
            var errors = new List<EmberTableDiagnostic>();
            if (rowType == null)
            {
                errors.Add(Error(definitionTableId, definitionPath, EmberTableErrorCode.InvalidRowType, "Definition has no Row type."));
                diagnostics = errors.AsReadOnly();
                return false;
            }

            bool validShape = rowType.IsClass
                ? rowType.IsSealed && !rowType.IsAbstract
                : rowType.IsValueType && IsReadOnlyStruct(rowType);
            if (!validShape || typeof(UnityEngine.Object).IsAssignableFrom(rowType))
                errors.Add(Error(
                    definitionTableId,
                    definitionPath,
                    EmberTableErrorCode.InvalidRowType,
                    "Row must be a non-abstract sealed class or readonly struct and cannot derive from UnityEngine.Object."));

            EmberTableAttribute tableAttribute = rowType.GetCustomAttribute<EmberTableAttribute>(false);
            string tableId = tableAttribute?.TableId;
            if (string.IsNullOrEmpty(tableId))
                errors.Add(Error(definitionTableId, definitionPath, EmberTableErrorCode.InvalidRowType, "Row is missing EmberTableAttribute or has an empty Table ID."));
            else if (!string.Equals(tableId, definitionTableId, StringComparison.Ordinal))
                errors.Add(Error(
                    definitionTableId,
                    definitionPath,
                    EmberTableErrorCode.InvalidRowType,
                    $"Definition Table ID '{definitionTableId}' does not match Row Table ID '{tableId}'."));

            var columns = CollectColumns(rowType, tableId ?? definitionTableId, definitionPath, errors);
            if (columns.Count == 0)
                errors.Add(Error(tableId, definitionPath, EmberTableErrorCode.InvalidRowType, "Row has no persistent readonly members."));

            List<EmberTableColumnSchema> keys = columns.Where(item => item.IsPrimaryKey).ToList();
            if (keys.Count != 1)
                errors.Add(Error(tableId, definitionPath, EmberTableErrorCode.InvalidRowType, "Row must declare exactly one EmberTableKey member."));
            else if (keys[0].ValueType != typeof(string))
                errors.Add(Error(tableId, definitionPath, EmberTableErrorCode.UnsupportedColumnType, "V1 primary key type must be string.", keys[0].ColumnName));

            ConstructorInfo[] markedConstructors = rowType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(item => item.GetCustomAttribute<EmberTableConstructorAttribute>(false) != null)
                .ToArray();
            if (markedConstructors.Length != 1 || !markedConstructors[0].IsPublic)
                errors.Add(Error(tableId, definitionPath, EmberTableErrorCode.InvalidConstructor, "Row must have exactly one public constructor marked EmberTableConstructor."));

            var constructorColumns = new List<EmberTableColumnSchema>();
            if (markedConstructors.Length == 1)
                MatchConstructor(markedConstructors[0], columns, tableId, definitionPath, constructorColumns, errors);

            columns.Sort((left, right) =>
            {
                if (left.IsPrimaryKey != right.IsPrimaryKey) return left.IsPrimaryKey ? -1 : 1;
                return string.Compare(left.ColumnName, right.ColumnName, StringComparison.Ordinal);
            });

            if (errors.Count == 0)
            {
                string rowTypeId = rowType.Assembly.GetName().Name + ":" + rowType.FullName;
                byte[] schemaHash = ComputeSchemaHash(tableId, rowTypeId, columns);
                schema = new EmberTableSchema(
                    tableId,
                    rowTypeId,
                    rowType,
                    markedConstructors[0],
                    columns,
                    constructorColumns,
                    schemaHash);
            }

            diagnostics = errors.AsReadOnly();
            return schema != null;
        }

        private static List<EmberTableColumnSchema> CollectColumns(
            Type rowType,
            string tableId,
            string path,
            List<EmberTableDiagnostic> errors)
        {
            var columns = new List<EmberTableColumnSchema>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (FieldInfo field in rowType.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                bool decorated = HasColumnMetadata(field);
                if (!field.IsInitOnly)
                {
                    if (decorated)
                        errors.Add(Error(tableId, path, EmberTableErrorCode.InvalidRowType, "Persistent field must be readonly.", field.Name));
                    continue;
                }
                AddColumn(field, field.FieldType, field.Name, tableId, path, columns, names, errors);
            }

            foreach (PropertyInfo property in rowType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0 || property.GetMethod == null) continue;
                FieldInfo backing = rowType.GetField(
                    "<" + property.Name + ">k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bool decorated = HasColumnMetadata(property);
                if (property.SetMethod != null || backing == null || !backing.IsInitOnly)
                {
                    if (decorated)
                        errors.Add(Error(tableId, path, EmberTableErrorCode.InvalidRowType, "Persistent property must be an auto-property with only a getter.", property.Name));
                    continue;
                }
                AddColumn(property, property.PropertyType, property.Name, tableId, path, columns, names, errors);
            }
            return columns;
        }

        private static void AddColumn(
            MemberInfo member,
            Type valueType,
            string memberName,
            string tableId,
            string path,
            List<EmberTableColumnSchema> columns,
            HashSet<string> names,
            List<EmberTableDiagnostic> errors)
        {
            string columnName = member.GetCustomAttribute<EmberTableColumnAttribute>(false)?.Name ?? memberName;
            if (string.IsNullOrEmpty(columnName))
            {
                errors.Add(Error(tableId, path, EmberTableErrorCode.InvalidRowType, "Column name cannot be empty.", memberName));
                return;
            }
            if (!names.Add(columnName))
            {
                errors.Add(Error(tableId, path, EmberTableErrorCode.DuplicateColumn, $"Column '{columnName}' is declared more than once.", columnName));
                return;
            }
            if (!TryGetWireType(valueType, out Type storageType, out string wireType, out bool nullable))
            {
                errors.Add(Error(tableId, path, EmberTableErrorCode.UnsupportedColumnType, $"Type '{valueType}' is not supported by V1.", columnName));
                return;
            }

            bool key = member.GetCustomAttribute<EmberTableKeyAttribute>(false) != null;
            string reference = member.GetCustomAttribute<EmberTableReferenceAttribute>(false)?.TargetTableId;
            if (reference != null && (valueType != typeof(string) || string.IsNullOrEmpty(reference)))
            {
                errors.Add(Error(tableId, path, EmberTableErrorCode.InvalidRowType, "Table references require a string column and non-empty target Table ID.", columnName));
                return;
            }

            columns.Add(new EmberTableColumnSchema(
                columnName,
                memberName,
                valueType,
                storageType,
                wireType,
                nullable,
                key,
                reference,
                member));
        }

        private static void MatchConstructor(
            ConstructorInfo constructor,
            IList<EmberTableColumnSchema> columns,
            string tableId,
            string path,
            List<EmberTableColumnSchema> result,
            List<EmberTableDiagnostic> errors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length != columns.Count)
            {
                errors.Add(Error(tableId, path, EmberTableErrorCode.InvalidConstructor, "Marked constructor parameter count must equal persistent column count."));
                return;
            }

            var used = new HashSet<EmberTableColumnSchema>();
            foreach (ParameterInfo parameter in parameters)
            {
                string explicitName = parameter.GetCustomAttribute<EmberTableColumnAttribute>(false)?.Name;
                EmberTableColumnSchema match = null;
                if (!string.IsNullOrEmpty(explicitName))
                    match = columns.FirstOrDefault(item => string.Equals(item.ColumnName, explicitName, StringComparison.Ordinal));
                else
                    match = columns.FirstOrDefault(item => string.Equals(item.ColumnName, parameter.Name, StringComparison.Ordinal))
                            ?? columns.FirstOrDefault(item => string.Equals(item.MemberName, parameter.Name, StringComparison.Ordinal))
                            ?? columns.FirstOrDefault(item => string.Equals(item.MemberName, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (match == null || used.Contains(match) || match.ValueType != parameter.ParameterType)
                {
                    errors.Add(Error(
                        tableId,
                        path,
                        EmberTableErrorCode.InvalidConstructor,
                        $"Constructor parameter '{parameter.Name}' does not map uniquely to a column of the same type.",
                        parameter.Name));
                    continue;
                }
                used.Add(match);
                result.Add(match);
            }
        }

        private static byte[] ComputeSchemaHash(
            string tableId,
            string rowTypeId,
            IList<EmberTableColumnSchema> columns)
        {
            var writer = new EmberTableBinaryWriter();
            writer.WriteString("EmberTableSchemaV1");
            writer.WriteString(tableId);
            writer.WriteString(rowTypeId);
            writer.WriteInt32(columns.Count);
            for (int i = 0; i < columns.Count; i++)
            {
                EmberTableColumnSchema column = columns[i];
                writer.WriteString(column.ColumnName);
                writer.WriteString(column.WireTypeId);
                writer.WriteBoolean(column.IsNullable);
                writer.WriteBoolean(column.IsPrimaryKey);
                writer.WriteString(column.ReferenceTableId);
            }
            return EmberTableBinaryFormat.ComputeSha256(writer.ToArray());
        }

        private static bool TryGetWireType(
            Type valueType,
            out Type storageType,
            out string wireType,
            out bool nullable)
        {
            storageType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            nullable = storageType != valueType;
            if (valueType == typeof(string)) { wireType = "string"; return true; }
            if (storageType == typeof(bool)) { wireType = "bool"; return true; }
            if (storageType == typeof(sbyte)) { wireType = "i8"; return true; }
            if (storageType == typeof(byte)) { wireType = "u8"; return true; }
            if (storageType == typeof(short)) { wireType = "i16"; return true; }
            if (storageType == typeof(ushort)) { wireType = "u16"; return true; }
            if (storageType == typeof(int)) { wireType = "i32"; return true; }
            if (storageType == typeof(uint)) { wireType = "u32"; return true; }
            if (storageType == typeof(long)) { wireType = "i64"; return true; }
            if (storageType == typeof(ulong)) { wireType = "u64"; return true; }
            if (storageType == typeof(float)) { wireType = "f32"; return true; }
            if (storageType == typeof(double)) { wireType = "f64"; return true; }
            if (storageType == typeof(decimal)) { wireType = "decimal"; return true; }
            if (storageType.IsEnum)
            {
                TryGetWireType(Enum.GetUnderlyingType(storageType), out _, out string underlying, out _);
                wireType = "enum:" + underlying + ":" + storageType.Assembly.GetName().Name + ":" + storageType.FullName;
                return true;
            }
            wireType = null;
            return false;
        }

        private static bool IsReadOnlyStruct(Type type)
        {
            return type.CustomAttributes.Any(item =>
                item.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
        }

        private static bool HasColumnMetadata(MemberInfo member)
        {
            return member.GetCustomAttribute<EmberTableColumnAttribute>(false) != null
                   || member.GetCustomAttribute<EmberTableKeyAttribute>(false) != null
                   || member.GetCustomAttribute<EmberTableReferenceAttribute>(false) != null;
        }

        private static EmberTableDiagnostic Error(
            string tableId,
            string path,
            EmberTableErrorCode code,
            string message,
            string field = null)
        {
            return new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                tableId,
                path,
                field: field);
        }
    }
}
