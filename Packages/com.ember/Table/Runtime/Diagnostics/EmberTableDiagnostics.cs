// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ember.Table
{
    public enum EmberTableDiagnosticSeverity
    {
        Information,
        Warning,
        Error,
    }

    public enum EmberTableErrorCode
    {
        None,
        CatalogMissing,
        DuplicateTableId,
        ResourceSystemNotReady,
        ResourceMissing,
        InvalidMagic,
        UnsupportedFormatVersion,
        UnsupportedFlags,
        TableIdMismatch,
        RowTypeIdMismatch,
        SchemaHashMismatch,
        InvalidLength,
        LimitExceeded,
        PayloadLengthMismatch,
        PayloadHashMismatch,
        PayloadTrailingData,
        InvalidBoolean,
        InvalidNullableMarker,
        InvalidUtf8,
        DecodeFailed,
        EmptyPrimaryKey,
        DuplicatePrimaryKey,
        DuplicateSecondaryKey,
        OptionalTableOmitted,
        EngineDisposed,
        CsvSyntax,
        DuplicateColumn,
        MissingColumn,
        UnknownColumn,
        InvalidRowType,
        InvalidConstructor,
        UnsupportedColumnType,
        InvalidValue,
        MissingReference,
        OutputPathInvalid,
        OutputPathConflict,
        UserFileProtected,
        ArtifactCommitFailed,
        ArtifactStale,
    }

    /// <summary>表加载、导入和生成共用的结构化诊断；纯容器不会自行输出日志。</summary>
    public sealed class EmberTableDiagnostic
    {
        #region 内部参数

        public EmberTableDiagnosticSeverity Severity { get; }
        public EmberTableErrorCode Code { get; }
        public string TableId { get; }
        public string FilePath { get; }
        public int Line { get; }
        public int Column { get; }
        public string Field { get; }
        public string Message { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public EmberTableDiagnostic(
            EmberTableDiagnosticSeverity severity,
            EmberTableErrorCode code,
            string message,
            string tableId = null,
            string filePath = null,
            int line = 0,
            int column = 0,
            string field = null)
        {
            Severity = severity;
            Code = code;
            Message = message ?? string.Empty;
            TableId = tableId;
            FilePath = filePath;
            Line = line;
            Column = column;
            Field = field;
        }

        public override string ToString()
        {
            string location = string.IsNullOrEmpty(FilePath)
                ? string.Empty
                : Line > 0
                    ? $"{FilePath}({Line},{Math.Max(1, Column)}): "
                    : FilePath + ": ";
            string table = string.IsNullOrEmpty(TableId) ? string.Empty : $"[{TableId}] ";
            string field = string.IsNullOrEmpty(Field) ? string.Empty : $"{Field}: ";
            return $"{location}{table}{Code} {field}{Message}";
        }

        #endregion
    }

    /// <summary>单张表在一次批量加载中的确定性摘要。</summary>
    public sealed class EmberTableLoadInfo
    {
        public string TableId { get; }
        public bool Required { get; }
        public bool Loaded { get; }
        public int RowCount { get; }
        public string SourceHash { get; }

        public EmberTableLoadInfo(
            string tableId,
            bool required,
            bool loaded,
            int rowCount,
            string sourceHash)
        {
            TableId = tableId;
            Required = required;
            Loaded = loaded;
            RowCount = rowCount;
            SourceHash = sourceHash;
        }
    }

    /// <summary>整批表加载结果；失败时 Engine 保留上一份完整数据库快照。</summary>
    public sealed class EmberTableLoadResult
    {
        #region 内部参数

        private readonly ReadOnlyCollection<EmberTableDiagnostic> _diagnostics;
        private readonly ReadOnlyCollection<EmberTableLoadInfo> _tables;

        public bool Succeeded { get; }
        public IReadOnlyList<EmberTableDiagnostic> Diagnostics => _diagnostics;
        public IReadOnlyList<EmberTableLoadInfo> Tables => _tables;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public EmberTableLoadResult(
            bool succeeded,
            IList<EmberTableDiagnostic> diagnostics,
            IList<EmberTableLoadInfo> tables)
        {
            Succeeded = succeeded;
            _diagnostics = new ReadOnlyCollection<EmberTableDiagnostic>(
                diagnostics == null
                    ? new List<EmberTableDiagnostic>()
                    : new List<EmberTableDiagnostic>(diagnostics));
            _tables = new ReadOnlyCollection<EmberTableLoadInfo>(
                tables == null ? new List<EmberTableLoadInfo>() : new List<EmberTableLoadInfo>(tables));
        }

        #endregion
    }
}
