using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>由多语言中心从源表生成的运行数据；项目拥有文案，框架拥有解析契约。</summary>
    public sealed class EmberLocalizationTable : ScriptableObject
    {
        #region 编辑器面板参数
        public string TableId;
        public string SourceLanguage = "zh_Hans";
        public string[] Languages = { "zh_Hans", "zh_Hant", "ja", "en" };
        public Entry[] Entries = Array.Empty<Entry>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable]
        public sealed class Entry
        {
            public string Key;
            public string[] Values;
        }
        #endregion
    }

    /// <summary>与玩法、表名和语言数量无关的解析器。全局 Key 必须唯一。</summary>
    public sealed class EmberTextLocalizer : ITextLocalizer
    {
        #region 内部参数
        private readonly Dictionary<string, Func<string, string>> _entries = new(StringComparer.Ordinal);
        private readonly List<string> _languages = new();
        private readonly Func<string> _current;
        private readonly string _source;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public IReadOnlyList<string> Languages => _languages;
        public string CurrentLanguage => string.IsNullOrEmpty(_current()) ? _source : _current();
        public EmberTextLocalizer(string source, IEnumerable<string> languages, Func<string> current)
        {
            _source = source;
            _current = current ?? (() => source);
            foreach (string language in languages)
                if (!_languages.Contains(language)) _languages.Add(language);
        }
        public void Add(string key, Func<string, string> resolve)
        {
            if (string.IsNullOrWhiteSpace(key) || resolve == null) throw new ArgumentException("多语言 Key 和解析器不能为空。");
            if (!_entries.TryAdd(key, resolve)) throw new InvalidOperationException("重复的多语言 Key：" + key);
        }
        public void AddTable(EmberLocalizationTable table)
        {
            foreach (string language in table.Languages)
                if (!_languages.Contains(language)) _languages.Add(language);
            foreach (var entry in table.Entries)
                Add(entry.Key, language => ResolveValue(table.Languages, entry.Values, language, table.SourceLanguage));
        }
        public bool TryGet(string key, out string text) => TryGet(key, null, out text);
        public bool TryGet(string key, string language, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(key) || !_entries.TryGetValue(key, out var resolve)) return false;
            text = resolve(string.IsNullOrEmpty(language) ? CurrentLanguage : language);
            return !string.IsNullOrEmpty(text);
        }
        [NoGC]
        public static string ResolveValue(string[] languages, string[] values, string target, string source)
        {
            int index = Array.IndexOf(languages, target);
            if (index >= 0 && index < values.Length && !string.IsNullOrEmpty(values[index])) return values[index];
            index = Array.IndexOf(languages, source);
            return index >= 0 && index < values.Length ? values[index] : null;
        }
        #endregion
    }
}
