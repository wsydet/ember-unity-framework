using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeJumpSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _receiverId;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string ReceiverId => _receiverId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
            => new(NodeId, NovelNodeKind.Jump, linkId: _receiverId);
        #endregion
    }
}
