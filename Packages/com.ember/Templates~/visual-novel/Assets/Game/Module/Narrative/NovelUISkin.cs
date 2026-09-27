using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative
{
    /// <summary>独立皮肤快照。来源仅作记录；创建后的图片与参数不再跟随来源。</summary>
    public sealed class NovelUISkin : ScriptableObject
    {
        #region 编辑器面板参数
        public string Id;
        public string DisplayName;
        public string Source;
        public List<NovelUISkinImage> Images = new();
        #endregion
    }

    [Serializable]
    public sealed class NovelUISkinImage
    {
        #region 编辑器面板参数
        public string Page;
        public string Control;
        public string Node;
        public Sprite Sprite;
        public Color Color = Color.white;
        public Image.Type Type;
        public bool PreserveAspect;
        public float PixelsPerUnitMultiplier = 1;
        public bool Pending = true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Key => Page + "|" + Control + "|" + Node;
        #endregion
    }
}
