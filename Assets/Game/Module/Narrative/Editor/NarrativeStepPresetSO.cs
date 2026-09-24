using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>Authoring-only reusable command snapshot; inserted steps are independent copies.</summary>
    public sealed class NarrativeStepPresetSO : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _displayName;
        [SerializeField] private Color _color = new(.45f, .65f, .95f, 1);
        [SerializeField] private List<NovelCommand> _commands = new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string DisplayName => _displayName;
        public Color Color => _color;
        public IReadOnlyList<NovelCommand> Commands => _commands;
        public void Initialize(string label, Color color, IReadOnlyList<NovelCommand> commands)
        { _displayName = label; _color = color; _commands = new List<NovelCommand>(commands); }
        #endregion
    }
}
