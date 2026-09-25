using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeEndingSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _endingId;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string EndingId => _endingId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
            => new NovelNode(NodeId, NovelNodeKind.Ending, endingId: _endingId);
        #endregion
    }
}

