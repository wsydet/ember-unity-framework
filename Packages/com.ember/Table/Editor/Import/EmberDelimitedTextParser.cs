// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Ember.Table.Editor
{
    public sealed class EmberDelimitedCell
    {
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public EmberDelimitedCell(string value, int line, int column)
        {
            Value = value;
            Line = line;
            Column = column;
        }
    }

    public sealed class EmberDelimitedRow
    {
        private readonly ReadOnlyCollection<EmberDelimitedCell> _cells;

        public IReadOnlyList<EmberDelimitedCell> Cells => _cells;
        public int Line { get; }

        public EmberDelimitedRow(IList<EmberDelimitedCell> cells, int line)
        {
            _cells = new ReadOnlyCollection<EmberDelimitedCell>(new List<EmberDelimitedCell>(cells));
            Line = line;
        }
    }

    public sealed class EmberDelimitedDocument
    {
        private readonly ReadOnlyCollection<EmberDelimitedRow> _rows;

        public IReadOnlyList<EmberDelimitedRow> Rows => _rows;

        public EmberDelimitedDocument(IList<EmberDelimitedRow> rows)
        {
            _rows = new ReadOnlyCollection<EmberDelimitedRow>(new List<EmberDelimitedRow>(rows));
        }
    }

    /// <summary>支持 UTF-8 BOM、CRLF/LF 和 RFC 4180 引号语义的 CSV/TSV 解析器。</summary>
    public static class EmberDelimitedTextParser
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static bool TryParse(
            byte[] bytes,
            char delimiter,
            string filePath,
            out EmberDelimitedDocument document,
            out EmberTableDiagnostic diagnostic)
        {
            document = null;
            diagnostic = null;
            if (delimiter != ',' && delimiter != '\t')
                throw new ArgumentOutOfRangeException(nameof(delimiter));

            string text;
            try
            {
                text = StrictUtf8.GetString(bytes ?? Array.Empty<byte>());
            }
            catch (DecoderFallbackException ex)
            {
                diagnostic = Error(filePath, 1, 1, "Source is not valid UTF-8: " + ex.Message);
                return false;
            }
            if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);

            var rows = new List<EmberDelimitedRow>();
            var cells = new List<EmberDelimitedCell>();
            var value = new StringBuilder();
            int line = 1;
            int column = 1;
            int rowLine = 1;
            int cellLine = 1;
            int cellColumn = 1;
            bool inQuotes = false;
            bool quoteClosed = false;
            bool quotedCell = false;
            bool rowHadSyntax = false;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (inQuotes)
                {
                    if (current == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            value.Append('"');
                            i++;
                            column += 2;
                            continue;
                        }
                        inQuotes = false;
                        quoteClosed = true;
                        column++;
                        continue;
                    }

                    if (current == '\r' || current == '\n') value.Append('\n');
                    else value.Append(current);
                    AdvancePosition(text, ref i, current, ref line, ref column);
                    continue;
                }

                if (quoteClosed && current != delimiter && current != '\r' && current != '\n')
                {
                    diagnostic = Error(filePath, line, column, "Only a delimiter or line ending may follow a closing quote.");
                    return false;
                }

                if (current == '"')
                {
                    if (value.Length != 0 || quotedCell)
                    {
                        diagnostic = Error(filePath, line, column, "A quote may only begin an empty cell.");
                        return false;
                    }
                    inQuotes = true;
                    quotedCell = true;
                    rowHadSyntax = true;
                    column++;
                    continue;
                }

                if (current == delimiter)
                {
                    cells.Add(new EmberDelimitedCell(value.ToString(), cellLine, cellColumn));
                    value.Clear();
                    quoteClosed = false;
                    quotedCell = false;
                    rowHadSyntax = true;
                    column++;
                    cellLine = line;
                    cellColumn = column;
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    cells.Add(new EmberDelimitedCell(value.ToString(), cellLine, cellColumn));
                    if (rowHadSyntax || cells.Count != 1 || cells[0].Value.Length != 0)
                        rows.Add(new EmberDelimitedRow(cells, rowLine));
                    cells = new List<EmberDelimitedCell>();
                    value.Clear();
                    quoteClosed = false;
                    quotedCell = false;
                    rowHadSyntax = false;
                    AdvancePosition(text, ref i, current, ref line, ref column);
                    rowLine = line;
                    cellLine = line;
                    cellColumn = column;
                    continue;
                }

                value.Append(current);
                rowHadSyntax = true;
                column++;
            }

            if (inQuotes)
            {
                diagnostic = Error(filePath, cellLine, cellColumn, "Quoted cell is not terminated.");
                return false;
            }

            if (value.Length > 0 || cells.Count > 0 || quoteClosed || quotedCell || rowHadSyntax)
            {
                cells.Add(new EmberDelimitedCell(value.ToString(), cellLine, cellColumn));
                if (rowHadSyntax || cells.Count != 1 || cells[0].Value.Length != 0)
                    rows.Add(new EmberDelimitedRow(cells, rowLine));
            }

            document = new EmberDelimitedDocument(rows);
            return true;
        }

        private static void AdvancePosition(
            string text,
            ref int index,
            char current,
            ref int line,
            ref int column)
        {
            if (current != '\r' && current != '\n')
            {
                column++;
                return;
            }

            if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index++;
            }
            line++;
            column = 1;
        }

        private static EmberTableDiagnostic Error(string path, int line, int column, string message)
        {
            return new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                EmberTableErrorCode.CsvSyntax,
                message,
                filePath: path,
                line: line,
                column: column);
        }
    }
}
