using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeFlowReturnSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _result = "完成";
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Result => _result;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
            => new(NodeId, NovelNodeKind.FlowReturn, result: _result);
        #endregion
    }
}
