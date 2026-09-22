using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeChapterExitSO : NarrativeNodeSO
    {
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter) => new(NodeId, NovelNodeKind.ChapterExit);
        #endregion
    }
}
