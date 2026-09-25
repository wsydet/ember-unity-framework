using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeChoiceSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _prompt;
        /// <summary>提示的多语言 Key；留空用 _prompt。</summary>
        [SerializeField] private string _promptTextKey;
        [SerializeField] private List<NarrativeRoute> _options = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Prompt => _prompt;
        public string PromptTextKey => _promptTextKey;
        public IReadOnlyList<NarrativeRoute> Options => _options.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
        {
            var routes = new List<NovelRoute>();
            foreach (NarrativeRoute option in _options) routes.Add(option?.ReadDefinition(chapter, NodeId));
            string prompt = NovelLocalization.TryGetContent(_promptTextKey, out string localizedPrompt) ? localizedPrompt : _prompt;
            return new NovelNode(NodeId, NovelNodeKind.Choice, routes: routes, prompt: prompt);
        }
        #endregion
    }
}

