using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.UIExtension;
using Game.Table;

namespace Game.Narrative
{
    /// <summary>
    /// 配表驱动的多语言解析器：把 novel_content_text / novel_ui_text 接到框架的
    /// <see cref="ITextLocalizer"/> 上，供 UI 的 TMPEx 与剧情正文共用同一条回退链。
    /// <para><b>回退链：</b>目标语言列 → 源语言列（默认 zh_Hans）→ 返回 false，由调用方使用原文。</para>
    /// <para><b>加语言：</b>novel_languages 加一行、两张文案表与对应 Row 加一列、在
    /// <see cref="Pick"/> 里加一个分支。宽表方案的固有代价，已在文档中写明。</para>
    /// </summary>
    public sealed class NovelLocalizer : ITextLocalizer
    {
        #region 内部参数
        /// <summary>源语言列的固定列名；novel_languages 里 isSource 为 true 的行可以覆盖这个标识。</summary>
        public const string DEFAULT_SOURCE_LANGUAGE = "zh_Hans";
        private readonly NarrativeTableCatalog _catalog;
        private readonly List<string> _languages = new List<string>(8);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>源语言标识；表里没有标记源语言时用 <see cref="DEFAULT_SOURCE_LANGUAGE"/>。</summary>
        public string SourceLanguage { get; }

        /// <summary>解析所依赖的配表目录，供上层复用（例如取角色名回退值）。</summary>
        public NarrativeTableCatalog Catalog => _catalog;

        public NovelLocalizer(NarrativeTableCatalog catalog)
        {
            _catalog = catalog;
            string source = DEFAULT_SOURCE_LANGUAGE;
            var rows = catalog == null ? null : catalog.Languages;
            if (rows != null)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < rows.Count; i++)
                {
                    NovelLanguageRow row = rows[i];
                    if (row == null || string.IsNullOrWhiteSpace(row.Id) || !seen.Add(row.Id)) continue;
                    _languages.Add(row.Id);
                    if (row.IsSource) source = row.Id;
                }
            }
            if (_languages.Count == 0) _languages.Add(DEFAULT_SOURCE_LANGUAGE);
            SourceLanguage = source;
        }

        /// <summary>当前语言；没设置过全局语言时等于源语言。</summary>
        public string CurrentLanguage
        {
            get
            {
                string current = NovelLanguageSettings.Current;
                return string.IsNullOrEmpty(current) ? SourceLanguage : current;
            }
        }

        /// <summary>可选语言清单，顺序来自 novel_languages.order。</summary>
        public IReadOnlyList<string> Languages => _languages;

        public bool TryGet(string key, out string text) => TryGet(key, null, out text);

        public bool TryGet(string key, string language, out string text)
        {
            text = null;
            if (_catalog == null || !_catalog.LocalizationReady || string.IsNullOrWhiteSpace(key)) return false;
            string target = string.IsNullOrEmpty(language) ? CurrentLanguage : language;
            // 内容表优先、UI 表兜底：两张表按 ui. 前缀分工，但这里不强制，
            // 避免 Key 放错表就直接失效（放错只是查表顺序不同，行为仍然可预期）。
            if (_catalog.TryGetContentText(key, out NovelContentTextRow content) &&
                Pick(content.ZhHans, content.ZhHant, content.Ja, content.En, target, out text)) return true;
            if (_catalog.TryGetUiText(key, out NovelUiTextRow ui) &&
                Pick(ui.ZhHans, ui.ZhHant, ui.Ja, ui.En, target, out text)) return true;
            return false;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>按语言取列；目标语言列为空时回退源语言列；源语言列也为空则判定为未命中。</summary>
        private bool Pick(string source, string traditional, string japanese, string english, string language, out string text)
        {
            text = null;
            string value = language switch
            {
                "zh_Hant" => traditional,
                "ja" => japanese,
                "en" => english,
                _ => source
            };
            if (string.IsNullOrEmpty(value)) value = source;
            if (string.IsNullOrEmpty(value)) return false;
            text = value;
            return true;
        }
        #endregion
    }

    /// <summary>
    /// 多语言的装配入口：把解析器接到框架的 <see cref="TextLocalization"/> 上，
    /// 并给剧情正文与角色名提供统一的解析方法。
    /// </summary>
    public static class NovelLocalization
    {
        #region 内部参数
        /// <summary>角色显示名在内容表里的 Key 前缀，例如 character.lastlight_wan。</summary>
        public const string CHARACTER_KEY_PREFIX = "character.";
        private static NovelLocalizer _localizer;
        private static NarrativeTableCatalog _catalog;
        /// <summary>语言切换后触发，供剧情定义重建、界面刷新等使用。</summary>
        public static event Action Changed;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>当前解析器；未安装时为 null。</summary>
        public static NovelLocalizer Localizer => _localizer;

        /// <summary>是否已接入多语言（配表齐备且已安装）。</summary>
        public static bool IsInstalled => _localizer != null;

        /// <summary>
        /// 安装（或替换）解析器。配表为空或缺多语言表时按卸载处理，
        /// 这样未迁移的项目拿到的行为与接入前完全一致。
        /// </summary>
        [HasGC]
        public static void Install(NarrativeTableCatalog catalog)
        {
            if (catalog == null || !catalog.LocalizationReady) { Uninstall(); return; }
            _catalog = catalog;
            _localizer = new NovelLocalizer(catalog);
            // SetLocalizer 内部会立即重刷所有活动 TMPEx。
            TextLocalization.SetLocalizer(_localizer);
        }

        /// <summary>卸载解析器：所有 TMPEx 与正文都回退原文。</summary>
        [HasGC]
        public static void Uninstall()
        {
            _localizer = null;
            _catalog = null;
            TextLocalization.SetLocalizer(null);
        }

        /// <summary>切换全局语言：持久化偏好、立即重刷 UI、再通知内容侧。</summary>
        [HasGC]
        public static void SetLanguage(string language)
        {
            string previous = NovelLanguageSettings.Current;
            NovelLanguageSettings.SetLanguage(language);
            if (previous == NovelLanguageSettings.Current) return;
            TextLocalization.RefreshAll();
            Changed?.Invoke();
        }

        /// <summary>已装配 TMPEx 的界面按当前语言重刷显示；供编辑器与语言切换调用，
        /// 这样编辑器程序集不必直接引用框架 UIExtension。</summary>
        [HasGC]
        public static void RefreshUiText() => TextLocalization.RefreshAll();

        /// <summary>按当前语言解析内容 Key（剧情正文、选项、章节名等）。</summary>
        [HasGC]
        public static bool TryGetContent(string key, out string text)
        {
            text = null;
            return _localizer != null && _localizer.TryGet(key, out text);
        }

        /// <summary>
        /// 角色显示名：优先内容表的 character.《角色键》条目，
        /// 未命中时返回 false，由调用方回退 novel_characters.displayName（源语言）。
        /// </summary>
        [HasGC]
        public static bool TryGetCharacterName(string characterId, out string name)
        {
            name = null;
            return _localizer != null && !string.IsNullOrWhiteSpace(characterId) &&
                _localizer.TryGet(CHARACTER_KEY_PREFIX + characterId, out name);
        }

        /// <summary>
        /// 角色显示名：优先内容表的 character.《角色键》条目；未命中时用传入的回退值
        /// （通常是 novel_characters.displayName，即源语言名）。
        /// </summary>
        [HasGC]
        public static string CharacterName(string characterId, string fallback)
            => TryGetCharacterName(characterId, out string localized) ? localized : fallback;

        /// <summary>解析 name 对应的语言列，供编辑器逐语言预览；未安装时返回 false。</summary>
        [HasGC]
        public static bool TryGetContent(string key, string language, out string text)
        {
            text = null;
            return _localizer != null && _localizer.TryGet(key, language, out text);
        }
        #endregion
    }
}
