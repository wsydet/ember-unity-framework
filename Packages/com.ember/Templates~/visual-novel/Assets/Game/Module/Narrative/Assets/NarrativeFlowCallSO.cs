using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeFlowCallSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeFlowStartSO _callee;
        [SerializeField] private List<NarrativeFlowResult> _results = new() { new NarrativeFlowResult() };
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NarrativeFlowStartSO Callee => _callee;
        public IReadOnlyList<NarrativeFlowResult> Results => _results.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
        {
            var routes = new List<NovelRoute>();
            foreach (var r in _results) routes.Add(new NovelRoute(r.Name, r.Name, chapter.ResolveTarget(r.Target, NodeId, r.Name)));
            return new NovelNode(NodeId, NovelNodeKind.FlowCall, linkId: chapter.ResolveTarget(_callee, NodeId), routes: routes);
        }
        #endregion
    }
}
