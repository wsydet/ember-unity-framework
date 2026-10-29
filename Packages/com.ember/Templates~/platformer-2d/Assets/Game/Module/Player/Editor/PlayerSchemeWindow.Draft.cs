using System;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public sealed partial class PlayerSchemeWindow
    {
        #region 内部参数
        private string DraftKey => "Game.Player.SchemeWindow.Draft." + Application.dataPath + "." + _ownerTemplate + "." + _ownerVersion;
        private string ReadDraft()
        {
            string current = SessionState.GetString(DraftKey, "");
            // One-way read fallback preserves existing unversioned platformer drafts without deleting or overwriting them.
            return !string.IsNullOrEmpty(current) || _ownerTemplate != "platformer-2d" ? current
                : SessionState.GetString("Game.Player.SchemeWindow.Draft." + Application.dataPath, "");
        }
        [Serializable]
        private sealed class WindowDraft
        {
            public string selected, reference, art, legacyFeel, legacyScheme, section, filter;
            public Vector2 position, velocity;
            public bool ruler, advanced, availability;
            public string combatSource, combatAttack, combatCombo, combatBaseline;
            public string[] combatClipIds, combatClips;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static string DraftId(UnityEngine.Object asset) => asset != null ? GlobalObjectId.GetGlobalObjectIdSlow(asset).ToString() : "";
        private static UnityEngine.Object DraftAsset(string id) => GlobalObjectId.TryParse(id, out var parsed)
            ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsed) : null;

        // 仅在窗口关闭/域重载时缓存本次 Editor 会话草稿；不保存资产、不应用白盒、不写项目默认。
        private void CacheDraft()
        {
            if (!_initialized) return;
            SessionState.SetString(DraftKey, JsonUtility.ToJson(new WindowDraft
            {
                selected = DraftId(_selected), reference = DraftId(_reference), art = DraftId(_art),
                legacyFeel = DraftId(_legacyScaleFeel), legacyScheme = DraftId(_legacyLabScheme),
                position = _position, velocity = _velocity, ruler = _ruler, section = _section, filter = _filter,
                advanced = _advanced, availability = _showAvailability,
                combatSource = DraftId(_combatDraftSource), combatAttack = _combatDraft == null ? null : JsonUtility.ToJson(_combatDraft.Attack),
                combatCombo = _comboDraftJson, combatBaseline = _combatBaseline,
                combatClipIds = _combatClipIds.ToArray(), combatClips = _combatClips.ConvertAll(DraftId).ToArray()
            }));
        }

        private bool RestoreDraft()
        {
            string json = ReadDraft();
            if (string.IsNullOrEmpty(json)) return false;
            var draft = JsonUtility.FromJson<WindowDraft>(json);
            if (draft == null) return false;
            _selected = DraftAsset(draft.selected) as PlayerScheme;
            _reference = DraftAsset(draft.reference) as PlayerScheme;
            _art = DraftAsset(draft.art);
            _legacyScaleFeel = DraftAsset(draft.legacyFeel) as PlayerFeelProfile;
            _legacyLabScheme = DraftAsset(draft.legacyScheme) as PlayerScheme;
            _position = draft.position; _velocity = draft.velocity; _ruler = draft.ruler;
            _section = draft.section; _filter = draft.filter;
            _advanced = draft.advanced; _showAvailability = draft.availability;
            return true;
        }
        private void RestoreCombatDraft()
        {
            if (_combatDraft != null) return;
            var json = ReadDraft();
            if (string.IsNullOrEmpty(json)) return;
            var draft = JsonUtility.FromJson<WindowDraft>(json);
            if (string.IsNullOrEmpty(draft?.combatAttack)) return;
            _combatDraft = CreateInstance<PlayerFeelProfile>(); _combatDraft.hideFlags = HideFlags.HideAndDontSave;
            _combatDraft.SetAttackConfiguration(JsonUtility.FromJson<PlayerAttackProfile>(draft.combatAttack));
            _combatDraftSource = DraftAsset(draft.combatSource) as PlayerScheme;
            _comboDraftJson = draft.combatCombo; _combatBaseline = draft.combatBaseline;
            _combatClipIds = new System.Collections.Generic.List<string>(draft.combatClipIds ?? Array.Empty<string>());
            _combatClips = new System.Collections.Generic.List<AnimationClip>();
            foreach (var id in draft.combatClips ?? Array.Empty<string>()) _combatClips.Add(DraftAsset(id) as AnimationClip);
        }
        #endregion
    }
}
