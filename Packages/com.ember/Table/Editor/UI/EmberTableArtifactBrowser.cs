// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>通过项目生成 Catalog 和 Runtime Engine 浏览正式烘焙产物。</summary>
    public static class EmberTableArtifactBrowser
    {
        public static bool TryLoad(
            EmberTableDefinition definition,
            out IEmberTableData table,
            out EmberTableLoadResult loadResult,
            out string error)
        {
            table = null;
            loadResult = null;
            error = null;
            if (!definition)
            {
                error = "Select a Table Definition first.";
                return false;
            }

            IEmberTableCatalog catalog;
            try
            {
                catalog = ResolveCatalog();
            }
            catch (Exception ex)
            {
                error = "Generated Catalog could not be created: " + ex.GetBaseException().Message;
                return false;
            }
            if (catalog == null)
            {
                error = "Generated GameTables Catalog is not compiled.";
                return false;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                definition.RuntimeOutputPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(fullPath))
            {
                error = "Baked table file is missing.";
                return false;
            }

            var sources = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [definition.TableId] = File.ReadAllBytes(fullPath),
            };
            var selectedEntries = new List<EmberTableCatalogEntry>();
            for (int i = 0; i < catalog.Entries.Count; i++)
                if (string.Equals(catalog.Entries[i].TableId, definition.TableId, StringComparison.Ordinal))
                    selectedEntries.Add(catalog.Entries[i]);
            if (selectedEntries.Count != 1)
            {
                error = "Generated Catalog does not contain exactly one matching table.";
                return false;
            }

            using (var engine = new EmberTableEngine())
            {
                loadResult = engine.Load(new EmberTableCatalog(selectedEntries), sources);
                if (!loadResult.Succeeded || !engine.Database.TryGetTable(definition.TableId, out table))
                {
                    error = loadResult.Diagnostics.Count > 0
                        ? loadResult.Diagnostics[0].ToString()
                        : "Runtime Engine could not load the table.";
                    return false;
                }
            }
            return true;
        }

        public static IReadOnlyList<string> DescribeRows(
            EmberTableDefinition definition,
            IEmberTableData table,
            int maxRows = 200)
        {
            var result = new List<string>();
            if (!definition || table is not IEnumerable enumerable || maxRows <= 0) return result.AsReadOnly();
            if (!EmberTableSchemaAnalyzer.TryAnalyze(
                    definition.RowType,
                    definition.TableId,
                    AssetPath(definition),
                    out EmberTableSchema schema,
                    out _))
                return result.AsReadOnly();

            int rowIndex = 0;
            foreach (object row in enumerable)
            {
                if (rowIndex >= maxRows)
                {
                    result.Add($"… 仅显示前 {maxRows} 行");
                    break;
                }

                var cells = new List<string>(schema.Columns.Count);
                for (int i = 0; i < schema.Columns.Count; i++)
                {
                    EmberTableColumnSchema column = schema.Columns[i];
                    try
                    {
                        object value = column.Member is FieldInfo field
                            ? field.GetValue(row)
                            : ((PropertyInfo)column.Member).GetValue(row);
                        cells.Add(column.ColumnName + "=" + Format(value));
                    }
                    catch (Exception ex)
                    {
                        cells.Add(column.ColumnName + "=<" + ex.GetBaseException().Message + ">");
                    }
                }
                result.Add($"{rowIndex + 1}: " + string.Join(" | ", cells));
                rowIndex++;
            }
            return result.AsReadOnly();
        }

        private static IEmberTableCatalog ResolveCatalog()
        {
            const string typeName = "Game.Table.Generated.GameTables";
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName, false);
                MethodInfo method = type?.GetMethod("CreateCatalog", BindingFlags.Public | BindingFlags.Static);
                if (method != null && typeof(IEmberTableCatalog).IsAssignableFrom(method.ReturnType))
                    return method.Invoke(null, null) as IEmberTableCatalog;
            }
            return null;
        }

        private static string AssetPath(EmberTableDefinition definition)
        {
            return UnityEditor.AssetDatabase.GetAssetPath(definition);
        }

        private static string Format(object value)
        {
            if (value == null) return "null";
            if (value is string text) return "\"" + text.Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }
}
