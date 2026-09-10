// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ember.Table.Editor
{
    /// <summary>把已验证 Schema 和数据转换为配置表中心可展示、可复制的内容。</summary>
    internal static class EmberTableExplorerPresenter
    {
        #region 外部方法

        /// <summary>生成不依赖 using 的强类型查询示例，可直接复制到项目方法中。</summary>
        public static string BuildUsageCode(
            EmberTableDefinition definition,
            EmberTableSchema schema,
            IReadOnlyList<object[]> rows)
        {
            if (!definition) throw new ArgumentNullException(nameof(definition));
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            string rowType = CSharpTypeName(schema.RowType);
            string sampleKey = "\"your_key\"";
            int primaryKeyIndex = IndexOf(schema, schema.PrimaryKey);
            if (rows != null && rows.Count > 0 && primaryKeyIndex >= 0 && primaryKeyIndex < rows[0].Length)
                sampleKey = FormatCSharpLiteral(rows[0][primaryKeyIndex], schema.PrimaryKey.ValueType);

            var builder = new StringBuilder();
            builder.Append("// Table ID: ").AppendLine(schema.TableId);
            builder.Append("// Row: ").AppendLine(schema.RowType.FullName);
            builder.AppendLine("// 前置：GameTableModule 的 EmberModuleAttribute 已启用，并已完成 Global 初始化。");
            builder.AppendLine("if (global::Ember.Core.EmberModuleCollector.TryGetInstance(out var moduleCollector)");
            builder.AppendLine("    && moduleCollector.TryGetModule<global::Game.Module.GameTableModule>(out var tableModule)");
            builder.AppendLine("    && tableModule.IsReady");
            builder.Append("    && tableModule.Database.TryGetTable<").Append(rowType).Append(">(\"")
                .Append(EscapeString(schema.TableId)).AppendLine("\", out var table))");
            builder.AppendLine("{");
            builder.Append("    if (table.TryGet(").Append(sampleKey).AppendLine(", out var row))");
            builder.AppendLine("    {");
            for (int i = 0; i < schema.Columns.Count; i++)
            {
                EmberTableColumnSchema column = schema.Columns[i];
                builder.Append("        var ").Append(LocalName(column.MemberName)).Append("Value = row.")
                    .Append(EscapeIdentifier(column.MemberName)).Append("; // ")
                    .Append(column.ColumnName).Append(" : ").AppendLine(DisplayTypeName(column.ValueType));
            }
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.Append("    foreach (").Append(rowType).AppendLine(" item in table)");
            builder.AppendLine("    {");
            builder.Append("        // item.").Append(EscapeIdentifier(schema.PrimaryKey.MemberName))
                .AppendLine(" 可用于遍历全部行");
            builder.AppendLine("    }");

            for (int i = 0; i < definition.SecondaryIndexes.Count; i++)
            {
                EmberTableSecondaryIndexDefinition index = definition.SecondaryIndexes[i];
                EmberTableColumnSchema column = FindColumn(schema, index.ColumnName);
                if (column == null) continue;
                object sample = SampleValue(schema, rows, column);
                string literal = sample == null
                    ? DefaultLiteral(column.ValueType)
                    : FormatCSharpLiteral(sample, column.ValueType);
                builder.AppendLine();
                builder.Append("    if (table.TryGetIndex<").Append(CSharpTypeName(column.ValueType)).Append(">(\"")
                    .Append(EscapeString(index.Name)).Append("\", out var index").Append(i).AppendLine(")");
                builder.Append("        && index").Append(i).Append(".TryGet(").Append(literal)
                    .AppendLine(", out var matchingRows))");
                builder.AppendLine("    {");
                builder.AppendLine("        // matchingRows 是该索引键对应的只读行集合");
                builder.AppendLine("    }");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        /// <summary>把严格转换后的值显示为接近可粘贴 C# 的字面量。</summary>
        public static string FormatCSharpLiteral(object value, Type declaredType)
        {
            if (value == null) return "null";
            Type type = Nullable.GetUnderlyingType(declaredType) ?? declaredType ?? value.GetType();
            if (type == typeof(string)) return "\"" + EscapeString((string)value) + "\"";
            if (type == typeof(bool)) return (bool)value ? "true" : "false";
            if (type == typeof(float))
            {
                float number = (float)value;
                if (float.IsNaN(number)) return "float.NaN";
                if (float.IsPositiveInfinity(number)) return "float.PositiveInfinity";
                if (float.IsNegativeInfinity(number)) return "float.NegativeInfinity";
                return number.ToString("R", CultureInfo.InvariantCulture) + "f";
            }
            if (type == typeof(double))
            {
                double number = (double)value;
                if (double.IsNaN(number)) return "double.NaN";
                if (double.IsPositiveInfinity(number)) return "double.PositiveInfinity";
                if (double.IsNegativeInfinity(number)) return "double.NegativeInfinity";
                return number.ToString("R", CultureInfo.InvariantCulture) + "d";
            }
            if (type == typeof(decimal))
                return ((decimal)value).ToString(CultureInfo.InvariantCulture) + "m";
            if (type == typeof(uint)) return Convert.ToString(value, CultureInfo.InvariantCulture) + "u";
            if (type == typeof(long)) return Convert.ToString(value, CultureInfo.InvariantCulture) + "L";
            if (type == typeof(ulong)) return Convert.ToString(value, CultureInfo.InvariantCulture) + "UL";
            if (type.IsEnum)
            {
                string name = Enum.GetName(type, value);
                return name == null
                    ? "(" + CSharpTypeName(type) + ")" + Convert.ToString(value, CultureInfo.InvariantCulture)
                    : CSharpTypeName(type) + "." + EscapeIdentifier(name);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public static string DisplayTypeName(Type type)
        {
            return CSharpTypeName(type).Replace("global::", string.Empty);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static EmberTableColumnSchema FindColumn(EmberTableSchema schema, string columnName)
        {
            for (int i = 0; i < schema.Columns.Count; i++)
                if (string.Equals(schema.Columns[i].ColumnName, columnName, StringComparison.Ordinal))
                    return schema.Columns[i];
            return null;
        }

        private static int IndexOf(EmberTableSchema schema, EmberTableColumnSchema target)
        {
            for (int i = 0; i < schema.Columns.Count; i++)
                if (ReferenceEquals(schema.Columns[i], target)) return i;
            return -1;
        }

        private static object SampleValue(
            EmberTableSchema schema,
            IReadOnlyList<object[]> rows,
            EmberTableColumnSchema column)
        {
            if (rows == null || rows.Count == 0) return null;
            int index = IndexOf(schema, column);
            return index >= 0 && index < rows[0].Length ? rows[0][index] : null;
        }

        private static string DefaultLiteral(Type type)
        {
            Type storage = Nullable.GetUnderlyingType(type) ?? type;
            if (storage == typeof(string)) return "\"value\"";
            return "default(" + CSharpTypeName(type) + ")";
        }

        private static string CSharpTypeName(Type type)
        {
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return CSharpTypeName(nullable) + "?";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            return "global::" + type.FullName.Replace('+', '.');
        }

        private static string LocalName(string memberName)
        {
            string source = string.IsNullOrEmpty(memberName) ? "field" : memberName;
            var builder = new StringBuilder(source.Length + 5);
            for (int i = 0; i < source.Length; i++)
            {
                char character = source[i];
                if (i == 0 && char.IsLetter(character)) builder.Append(char.ToLowerInvariant(character));
                else if (char.IsLetterOrDigit(character) || character == '_') builder.Append(character);
            }
            if (builder.Length == 0 || char.IsDigit(builder[0])) builder.Insert(0, "field");
            return builder.ToString();
        }

        private static string EscapeIdentifier(string value)
        {
            return "@" + value;
        }

        private static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '\"': builder.Append("\\\""); break;
                    case '\0': builder.Append("\\0"); break;
                    case '\a': builder.Append("\\a"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\v': builder.Append("\\v"); break;
                    default:
                        if (char.IsControl(character))
                            builder.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }

        #endregion
    }
}
