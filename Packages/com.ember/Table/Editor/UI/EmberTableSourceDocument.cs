using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    /// <summary>源表编辑草稿。保存时检查外部修改；烘焙失败恢复原源表。</summary>
    [Serializable]
    public sealed class EmberTableSourceDocument
    {
        #region 编辑器面板参数
        public string Path;
        public string Original;
        public byte[] OriginalBytes;
        public string[] Columns;
        public List<Row> Rows = new();
        [Serializable] public sealed class Row { public string[] Values; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool IsStale => OriginalBytes == null ? Load(Path).Original != Original
            : !EmberTableSourceFiles.ReadBytes(Path).SequenceEqual(OriginalBytes);
        public bool Dirty => Original != Serialize();
        public int Column(string name) => Array.IndexOf(Columns, name);
        public string Get(Row row, string name) { int i = Column(name); return i < 0 ? "" : row.Values[i]; }
        public static EmberTableSourceDocument Load(string path)
        {
            byte[] bytes = EmberTableSourceFiles.ReadBytes(path);
            string text = new UTF8Encoding(false, true).GetString(bytes);
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            var result = Parse(path, text);
            result.OriginalBytes = bytes;
            return result;
        }
        public static EmberTableSourceDocument Parse(string path, string text)
        {
            if (!EmberDelimitedTextParser.TryParse(Encoding.UTF8.GetBytes(text), path.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',', path,
                    out var parsed, out var error)) throw new InvalidOperationException(error.ToString());
            if (parsed.Rows.Count == 0) throw new InvalidOperationException("源表没有表头。");
            var result = new EmberTableSourceDocument { Path = path, Columns = parsed.Rows[0].Cells.Select(c => c.Value).ToArray() };
            if (result.Columns.Distinct(StringComparer.Ordinal).Count() != result.Columns.Length)
                throw new InvalidOperationException("源表存在重复列名。");
            foreach (var row in parsed.Rows.Skip(1))
            {
                if (row.Cells.Count != result.Columns.Length) throw new InvalidOperationException($"第 {row.Line} 行列数不匹配。");
                result.Rows.Add(new Row { Values = row.Cells.Select(c => c.Value).ToArray() });
            }
            result.Original = result.Serialize();
            return result;
        }
        public string Serialize()
        {
            char delimiter = Path.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
            string Line(IEnumerable<string> values) => string.Join(delimiter.ToString(), values.Select(v =>
                "\"" + (v ?? "").Replace("\"", "\"\"") + "\""));
            return Line(Columns) + "\n" + string.Concat(Rows.Select(r => Line(r.Values) + "\n"));
        }
        public Row Add(string key, string keyColumn)
        {
            int column = Column(keyColumn);
            if (column < 0) throw new InvalidOperationException("缺少主键列：" + keyColumn);
            if (string.IsNullOrWhiteSpace(key) || key != key.Trim()) throw new InvalidOperationException("Key 不能为空或带首尾空格。");
            if (Rows.Any(r => r.Values[column] == key)) throw new InvalidOperationException("Key 已存在：" + key);
            var row = new Row { Values = Enumerable.Repeat("", Columns.Length).ToArray() };
            row.Values[column] = key;
            Rows.Add(row);
            return row;
        }
        public void Save(EmberTableDefinition definition = null)
        {
            if ((OriginalBytes != null && !EmberTableSourceFiles.ReadBytes(Path).SequenceEqual(OriginalBytes)) || Load(Path).Original != Original) throw new InvalidOperationException("源表已被外部修改，请先保留草稿并重新加载，不能覆盖保存。");
            if (definition && !EmberTableSourceFiles.SameFile(Path, definition.SourcePath))
                throw new InvalidOperationException("草稿与 Definition 源文件不匹配。");
            byte[] previous = EmberTableSourceFiles.ReadBytes(Path);
            byte[] next = new UTF8Encoding(false).GetBytes(Serialize());
            try
            {
                if (Dirty)
                {
                    EmberTableSourceFiles.WriteBytes(Path, next);
                }
                if (definition)
                {
                    var result = EmberTablePipeline.BakeCurrent(definition);
                    if (!result.Succeeded) throw new InvalidOperationException(string.Join("\n", result.Diagnostics.Select(d => d.ToString())));
                }
                Original = Serialize();
                OriginalBytes = EmberTableSourceFiles.ReadBytes(Path);
            }
            catch
            {
                if (!EmberTableSourceFiles.ReadBytes(Path).SequenceEqual(previous))
                {
                    if (!EmberTableSourceFiles.ReadBytes(Path).SequenceEqual(next))
                        throw new InvalidOperationException("保存失败且源表再次被修改；已保留草稿，请手动合并，禁止覆盖当前磁盘内容。");
                    EmberTableSourceFiles.WriteBytes(Path, previous);
                }
                throw;
            }
        }
        #endregion
    }
}
