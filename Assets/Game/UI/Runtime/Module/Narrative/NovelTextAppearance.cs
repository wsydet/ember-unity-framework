using Ember.Basic;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Snapshot of author-defined text appearance, shared by playback and layout preview.</summary>
    public readonly struct NovelTextAppearance
    {
        #region 内部参数
        private readonly TMP_FontAsset _font;
        private readonly Material _material;
        private readonly float _size, _characters, _lines;
        private readonly Color _color;
        private readonly FontStyles _style;
        private readonly TextAlignmentOptions _alignment;
        private readonly Vector4 _margin;

        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public NovelTextAppearance(TMP_Text text)
        {
            _font = text.font; _material = text.fontSharedMaterial; _size = text.fontSize;
            _color = text.color; _style = text.fontStyle; _alignment = text.alignment;
            _characters = text.characterSpacing; _lines = text.lineSpacing; _margin = text.margin;
        }

        [HasGC]
        public void Apply(TMP_Text text, float fontScale = 1)
        {
            text.font = _font; text.fontSharedMaterial = _material; text.fontSize = _size * fontScale;
            text.color = _color; text.fontStyle = _style; text.alignment = _alignment;
            text.characterSpacing = _characters; text.lineSpacing = _lines; text.margin = _margin;
        }
        #endregion
    }
}
