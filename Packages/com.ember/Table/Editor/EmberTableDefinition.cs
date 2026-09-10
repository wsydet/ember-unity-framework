// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    public enum EmberTableUnknownColumnPolicy
    {
        Error,
        Warning,
    }

    [Serializable]
    public sealed class EmberTableSecondaryIndexDefinition
    {
        [SerializeField] private string _name;
        [SerializeField] private string _columnName;
        [SerializeField] private bool _unique;

        public string Name => _name;
        public string ColumnName => _columnName;
        public bool Unique => _unique;
    }

    /// <summary>项目声明的单表编辑源、Row Schema、加载策略和安全上限。</summary>
    [CreateAssetMenu(fileName = "TableDefinition", menuName = "Ember/Table Definition")]
    public sealed class EmberTableDefinition : ScriptableObject
    {
        #region 编辑器面板参数

        [SerializeField] private string _tableId;
        [SerializeField] private TextAsset _source;
        [SerializeField] private MonoScript _rowScript;
        [SerializeField] private bool _required = true;
        [SerializeField] private EmberTableUnknownColumnPolicy _unknownColumnPolicy =
            EmberTableUnknownColumnPolicy.Error;
        [SerializeField] private string _runtimeOutputPath;
        [SerializeField] private int _maxFileBytes = EmberTableCatalogEntry.DEFAULT_MAX_FILE_BYTES;
        [SerializeField] private int _maxRows = EmberTableCatalogEntry.DEFAULT_MAX_ROWS;
        [SerializeField] private int _maxStringBytes = EmberTableCatalogEntry.DEFAULT_MAX_STRING_BYTES;
        [SerializeField] private List<EmberTableSecondaryIndexDefinition> _secondaryIndexes = new();

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public string TableId => _tableId;
        public TextAsset Source => _source;
        public MonoScript RowScript => _rowScript;
        public Type RowType => _rowScript ? _rowScript.GetClass() : null;
        public bool Required => _required;
        public EmberTableUnknownColumnPolicy UnknownColumnPolicy => _unknownColumnPolicy;
        public string RuntimeOutputPath => string.IsNullOrEmpty(_runtimeOutputPath)
            ? GetDefaultOutputPath(_tableId)
            : _runtimeOutputPath.Replace('\\', '/');
        public int MaxFileBytes => _maxFileBytes;
        public int MaxRows => _maxRows;
        public int MaxStringBytes => _maxStringBytes;
        public IReadOnlyList<EmberTableSecondaryIndexDefinition> SecondaryIndexes => _secondaryIndexes;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public string GetLogicalResourcePath()
        {
            string path = RuntimeOutputPath;
            const string marker = "/Resources/";
            int markerIndex = path.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0 || !path.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                return null;
            return path.Substring(markerIndex + marker.Length, path.Length - markerIndex - marker.Length - 6);
        }

        public static string GetDefaultOutputPath(string tableId)
        {
            return $"Assets/GameResource/Resources/Config/Tables/{tableId}.bytes";
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnValidate()
        {
            if (_maxFileBytes <= 0) _maxFileBytes = EmberTableCatalogEntry.DEFAULT_MAX_FILE_BYTES;
            if (_maxRows < 0) _maxRows = EmberTableCatalogEntry.DEFAULT_MAX_ROWS;
            if (_maxStringBytes < 0) _maxStringBytes = EmberTableCatalogEntry.DEFAULT_MAX_STRING_BYTES;
        }

        #endregion
    }
}
