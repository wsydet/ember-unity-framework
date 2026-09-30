using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeReceiverSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeNodeSO _next;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NarrativeNodeSO Next => _next;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
            => new(NodeId, NovelNodeKind.Receiver, chapter.ResolveTarget(_next, NodeId));
        #endregion
    }
}
