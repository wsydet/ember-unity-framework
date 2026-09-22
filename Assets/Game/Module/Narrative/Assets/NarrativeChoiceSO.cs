using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeChoiceSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _prompt;
        [SerializeField] private List<NarrativeRoute> _options = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Prompt => _prompt;
        public IReadOnlyList<NarrativeRoute> Options => _options.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
        {
            var routes = new List<NovelRoute>();
            foreach (NarrativeRoute option in _options) routes.Add(option?.ReadDefinition(chapter, NodeId));
            return new NovelNode(NodeId, NovelNodeKind.Choice, routes: routes, prompt: _prompt);
        }
        #endregion
    }
}

