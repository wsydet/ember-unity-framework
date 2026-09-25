using Ember.Table;
using Game.Table;
using System;
using System.Collections.Generic;

namespace Game.Narrative
{
    /// <summary>复用 GameTableModule.Database；路径由现有 Resource Provider 解释。</summary>
    public sealed class NarrativeTableCatalog : INarrativeCatalog
    {
        #region 内部参数
        private readonly EmberTable<NovelCharacterRow> _characters;
        private readonly EmberTable<NovelPortraitRow> _portraits;
        private readonly EmberTable<NovelBackgroundRow> _backgrounds;
        private readonly EmberTable<NovelAudioRow> _audio;
        private readonly EmberTable<NovelLanguageRow> _languages;
        private readonly EmberTable<NovelUiTextRow> _uiText;
        private readonly EmberTable<NovelContentTextRow> _contentText;
        private readonly EmberTable<NovelSkinRow> _skins;
        private readonly EmberTable<NovelSkinSpriteRow> _skinSprites;
        private readonly EmberTable<NovelStorySkinRow> _storySkins;
        public bool IsReady { get; }
        /// <summary>多语言三张表是否齐备。缺表时多语言整体回退原文，未迁移的项目行为不变。</summary>
        public bool LocalizationReady { get; }
        /// <summary>皮肤三张表是否齐备。缺表时运行期不套任何皮肤，沿用 Prefab 外观。</summary>
        public bool SkinsReady { get; }
        public IReadOnlyList<NovelCharacterRow> Characters => _characters ?? (IReadOnlyList<NovelCharacterRow>)Array.Empty<NovelCharacterRow>();
        public IReadOnlyList<NovelPortraitRow> Portraits => _portraits ?? (IReadOnlyList<NovelPortraitRow>)Array.Empty<NovelPortraitRow>();
        public IReadOnlyList<NovelBackgroundRow> Backgrounds => _backgrounds ?? (IReadOnlyList<NovelBackgroundRow>)Array.Empty<NovelBackgroundRow>();
        public IReadOnlyList<NovelAudioRow> Audio => _audio ?? (IReadOnlyList<NovelAudioRow>)Array.Empty<NovelAudioRow>();
        public IReadOnlyList<NovelLanguageRow> Languages => _languages ?? (IReadOnlyList<NovelLanguageRow>)Array.Empty<NovelLanguageRow>();
        public IReadOnlyList<NovelUiTextRow> UiText => _uiText ?? (IReadOnlyList<NovelUiTextRow>)Array.Empty<NovelUiTextRow>();
        public IReadOnlyList<NovelContentTextRow> ContentText => _contentText ?? (IReadOnlyList<NovelContentTextRow>)Array.Empty<NovelContentTextRow>();
        public IReadOnlyList<NovelSkinRow> Skins => _skins ?? (IReadOnlyList<NovelSkinRow>)Array.Empty<NovelSkinRow>();
        public IReadOnlyList<NovelSkinSpriteRow> SkinSprites => _skinSprites ?? (IReadOnlyList<NovelSkinSpriteRow>)Array.Empty<NovelSkinSpriteRow>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeTableCatalog(EmberTableDatabase database)
        {
            if (database == null) return;
            bool characters = database.TryGetTable("novel_characters", out _characters);
            bool portraits = database.TryGetTable("novel_portraits", out _portraits);
            bool backgrounds = database.TryGetTable("novel_backgrounds", out _backgrounds);
            bool audio = database.TryGetTable("novel_audio", out _audio);
            IsReady = characters && portraits && backgrounds && audio;
            // 新增表是可选的：老剧情只带四张基础表时依然可用，只是多语言与皮肤功能整体降级。
            bool languages = database.TryGetTable("novel_languages", out _languages);
            bool uiText = database.TryGetTable("novel_ui_text", out _uiText);
            bool contentText = database.TryGetTable("novel_content_text", out _contentText);
            LocalizationReady = languages && uiText && contentText;
            bool skins = database.TryGetTable("novel_skins", out _skins);
            bool skinSprites = database.TryGetTable("novel_skin_sprites", out _skinSprites);
            bool storySkins = database.TryGetTable("novel_story_skin", out _storySkins);
            SkinsReady = skins && skinSprites && storySkins;
        }

        /// <summary>UI 文案表按 key 查询；未接入多语言时返回 false。</summary>
        public bool TryGetUiText(string key, out NovelUiTextRow row)
        {
            row = null;
            return LocalizationReady && !string.IsNullOrWhiteSpace(key) && _uiText.TryGet(key, out row);
        }

        /// <summary>内容文案表按 key 查询；未接入多语言时返回 false。</summary>
        public bool TryGetContentText(string key, out NovelContentTextRow row)
        {
            row = null;
            return LocalizationReady && !string.IsNullOrWhiteSpace(key) && _contentText.TryGet(key, out row);
        }

        /// <summary>取某部剧情被赋值的皮肤；没有赋值或未接入皮肤表时返回 false（沿用 Prefab 外观）。</summary>
        public bool TryGetSkinId(string storyId, out string skinId)
        {
            skinId = null;
            if (!SkinsReady || string.IsNullOrWhiteSpace(storyId) || !_storySkins.TryGet(storyId, out NovelStorySkinRow row)) return false;
            skinId = row.SkinId;
            return !string.IsNullOrWhiteSpace(skinId);
        }

        public bool HasCharacter(string characterId) => IsReady && characterId != null && _characters.TryGet(characterId, out _);

        public bool TryGetCharacter(string characterId, out NovelCharacterRow character)
        {
            character = null;
            return IsReady && characterId != null && _characters.TryGet(characterId, out character);
        }

        public bool TryResolve(NovelCommandKind kind, string key, out string resourcePath)
        {
            resourcePath = null;
            if (!IsReady || string.IsNullOrWhiteSpace(key)) return false;
            if (kind == NovelCommandKind.EffectPlay) { resourcePath = NovelMediaRules.EffectPath(key); return resourcePath != null; }
            if (kind == NovelCommandKind.AmbientPlay) kind = NovelCommandKind.SFX;
            if (kind == NovelCommandKind.Background && _backgrounds.TryGet(key, out NovelBackgroundRow background))
                resourcePath = background.ResourcePath;
            else if (kind == NovelCommandKind.Character && _portraits.TryGet(key, out NovelPortraitRow portrait)
                && HasCharacter(portrait.CharacterId) && !string.IsNullOrWhiteSpace(portrait.Expression))
                resourcePath = portrait.ResourcePath;
            else if ((kind == NovelCommandKind.BGM || kind == NovelCommandKind.SFX || kind == NovelCommandKind.Voice)
                && _audio.TryGet(key, out NovelAudioRow audio) && audio.Category == kind.ToString())
                resourcePath = audio.ResourcePath;
            return !string.IsNullOrWhiteSpace(resourcePath);
        }
        #endregion
    }
}
