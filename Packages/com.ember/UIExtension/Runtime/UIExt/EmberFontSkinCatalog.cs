using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>字体皮肤与跨皮肤稳定的字体槽位。ID 不随列表顺序或名称变化。</summary>
    [CreateAssetMenu(menuName = "Ember/UI/字体皮肤库")]
    public sealed class EmberFontSkinCatalog : ScriptableObject
    {
        #region 编辑器面板参数
        public int DefaultSkinId = 1;
        [HideInInspector] public int NextSkinId = 4;
        [HideInInspector] public int NextSlotId = 4;
        public List<FontSlot> Slots = new List<FontSlot>();
        public List<FontSkin> Skins = new List<FontSkin>();
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        [Serializable]
        public sealed class FontSlot
        {
            public int Id;
            public string Name;
            public string EnumName;
        }

        [Serializable]
        public sealed class FontSkin
        {
            public int Id;
            public string Name;
            public string EnumName;
            public List<FontEntry> Fonts = new List<FontEntry>();
        }

        [Serializable]
        public sealed class FontEntry
        {
            public int SlotId;
            public TMP_FontAsset Font;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public FontSkin FindSkin(int id) => Skins.Find(s => s != null && s.Id == id);

        public bool TryGetFont(int skinId, int slotId, out TMP_FontAsset font)
        {
            font = null;
            var skin = FindSkin(skinId);
            if (skin == null || !Slots.Exists(s => s != null && s.Id == slotId)) return false;
            var entry = skin.Fonts.Find(f => f != null && f.SlotId == slotId);
            font = entry?.Font;
            return font;
        }

        /// <summary>防止重复 ID、空字体或不完整映射进入运行期。</summary>
        public bool Validate(out string error)
        {
            error = null;
            var slotIds = new HashSet<int>();
            var skinIds = new HashSet<int>();
            foreach (var slot in Slots)
                if (slot == null || slot.Id <= 0 || !slotIds.Add(slot.Id))
                { error = "字体槽位 ID 必须为不重复的正数。"; return false; }
            if (Slots.Count == 0) { error = "至少需要一个字体槽位。"; return false; }
            foreach (var skin in Skins)
            {
                if (skin == null || skin.Id <= 0 || !skinIds.Add(skin.Id))
                { error = "皮肤 ID 必须为不重复的正数。"; return false; }
                var mapped = new HashSet<int>();
                foreach (var entry in skin.Fonts)
                    if (entry == null || !slotIds.Contains(entry.SlotId) || !mapped.Add(entry.SlotId) || !entry.Font)
                    { error = skin.Name + " 存在缺失字体、重复或无效槽位。"; return false; }
                if (mapped.Count == 0)
                { error = skin.Name + " 至少需要一个字体选项。"; return false; }
            }
            if (!skinIds.Contains(DefaultSkinId)) { error = "默认皮肤不存在。"; return false; }
            return true;
        }
        #endregion
    }
}
