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
        public bool IsReady { get; }
        public IReadOnlyList<NovelCharacterRow> Characters => _characters ?? (IReadOnlyList<NovelCharacterRow>)Array.Empty<NovelCharacterRow>();
        public IReadOnlyList<NovelPortraitRow> Portraits => _portraits ?? (IReadOnlyList<NovelPortraitRow>)Array.Empty<NovelPortraitRow>();
        public IReadOnlyList<NovelBackgroundRow> Backgrounds => _backgrounds ?? (IReadOnlyList<NovelBackgroundRow>)Array.Empty<NovelBackgroundRow>();
        public IReadOnlyList<NovelAudioRow> Audio => _audio ?? (IReadOnlyList<NovelAudioRow>)Array.Empty<NovelAudioRow>();
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
