using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NovelUISkinAssignments : ScriptableObject
    {
        #region 编辑器面板参数
        public List<NovelUISkinAssignment> Stories = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public const string RESOURCE_PATH = "Config/Narrative/UISkinAssignments";
        #endregion
    }

    [Serializable]
    public sealed class NovelUISkinAssignment
    {
        #region 编辑器面板参数
        public NarrativeStorySO Story;
        // 存在记录但 Skin 为空，表示明确恢复基础外观，不回退到旧配表皮肤。
        public NovelUISkin Skin;
        #endregion
    }
}
