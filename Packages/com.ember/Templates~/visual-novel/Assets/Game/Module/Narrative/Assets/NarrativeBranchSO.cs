using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeBranchSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private List<NarrativeRoute> _branches = new();
        [SerializeField] private NarrativeNodeSO _fallback;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public IReadOnlyList<NarrativeRoute> Branches => _branches.AsReadOnly();
        public NarrativeNodeSO Fallback => _fallback;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
        {
            var routes = new List<NovelRoute>();
            foreach (NarrativeRoute branch in _branches) routes.Add(branch?.ReadDefinition(chapter, NodeId));
            return new NovelNode(NodeId, NovelNodeKind.Branch,
                chapter.ResolveTarget(_fallback, NodeId), routes: routes);
        }
        #endregion
    }
}

