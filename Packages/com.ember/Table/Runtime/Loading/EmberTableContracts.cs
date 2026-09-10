// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ember.Table
{
    /// <summary>Runtime 数据库中一张已完成构建的只读表。</summary>
    public interface IEmberTableData
    {
        string TableId { get; }
        string RowTypeId { get; }
        int Count { get; }
    }

    /// <summary>生成 Binding 的无反射 Runtime 契约。</summary>
    public interface IEmberTableBinding
    {
        string TableId { get; }
        string RowTypeId { get; }
        string ResourcePath { get; }
        ushort FormatVersion { get; }
        string SchemaHash { get; }
        IEmberTableData ReadTable(EmberTableBinaryReader reader, int rowCount);
    }

    /// <summary>Catalog 中单张表的加载元数据和安全上限。</summary>
    public sealed class EmberTableCatalogEntry
    {
        public const int DEFAULT_MAX_FILE_BYTES = 16 * 1024 * 1024;
        public const int DEFAULT_MAX_ROWS = 100000;
        public const int DEFAULT_MAX_STRING_BYTES = 1024 * 1024;

        public IEmberTableBinding Binding { get; }
        public bool Required { get; }
        public int MaxFileBytes { get; }
        public int MaxRows { get; }
        public int MaxStringBytes { get; }

        public string TableId => Binding?.TableId;
        public string ResourcePath => Binding?.ResourcePath;

        public EmberTableCatalogEntry(
            IEmberTableBinding binding,
            bool required = true,
            int maxFileBytes = DEFAULT_MAX_FILE_BYTES,
            int maxRows = DEFAULT_MAX_ROWS,
            int maxStringBytes = DEFAULT_MAX_STRING_BYTES)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Required = required;
            MaxFileBytes = maxFileBytes;
            MaxRows = maxRows;
            MaxStringBytes = maxStringBytes;
        }
    }

    public interface IEmberTableCatalog
    {
        IReadOnlyList<EmberTableCatalogEntry> Entries { get; }
    }

    /// <summary>项目生成 Catalog 可直接复用的不可变实现。</summary>
    public sealed class EmberTableCatalog : IEmberTableCatalog
    {
        private readonly ReadOnlyCollection<EmberTableCatalogEntry> _entries;

        public IReadOnlyList<EmberTableCatalogEntry> Entries => _entries;

        public EmberTableCatalog(IList<EmberTableCatalogEntry> entries)
        {
            _entries = new ReadOnlyCollection<EmberTableCatalogEntry>(
                entries == null
                    ? new List<EmberTableCatalogEntry>()
                    : new List<EmberTableCatalogEntry>(entries));
        }

        public static EmberTableCatalog Empty { get; } =
            new EmberTableCatalog(Array.Empty<EmberTableCatalogEntry>());
    }

    /// <summary>二进制格式版本入口；Engine 默认使用严格的 V1 Codec。</summary>
    public interface IEmberTableCodec
    {
        bool TryDecode(
            EmberTableCatalogEntry entry,
            byte[] fileBytes,
            out IEmberTableData table,
            out EmberTableBinaryHeader header,
            out EmberTableDiagnostic diagnostic);
    }

    /// <summary>Row 或索引构建失败时由 Codec 转换为结构化诊断。</summary>
    public sealed class EmberTableDataException : Exception
    {
        public EmberTableErrorCode Code { get; }
        public string Field { get; }

        public EmberTableDataException(EmberTableErrorCode code, string message, string field = null)
            : base(message)
        {
            Code = code;
            Field = field;
        }
    }
}
