using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeFlowStartSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeNodeSO _next;
        [SerializeField] private List<NovelVariable> _variables = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NarrativeNodeSO Next => _next;
        public IReadOnlyList<NovelVariable> Variables => _variables.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
            => new(NodeId, NovelNodeKind.FlowStart, chapter.ResolveTarget(_next, NodeId), flowVariables: _variables);
        #endregion
    }
}
