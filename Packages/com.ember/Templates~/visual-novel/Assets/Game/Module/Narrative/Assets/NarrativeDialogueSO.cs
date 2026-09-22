using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeDialogueSO : NarrativeNodeSO
    {
        #region 编辑器面板参数
        [SerializeField] private List<NovelCommand> _commands = new();
        [SerializeField] private NarrativeNodeSO _next;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public IReadOnlyList<NovelCommand> Commands => _commands.AsReadOnly();
        public NarrativeNodeSO Next => _next;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal override NovelNode ReadDefinition(NarrativeChapterSO chapter)
        {
            var commands = new List<NovelCommand>();
            foreach (NovelCommand command in _commands)
                commands.Add(command == null ? null : JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(command)));
            return new NovelNode(NodeId, NovelNodeKind.Dialogue, chapter.ResolveTarget(_next, NodeId), commands: commands);
        }
        #endregion
    }
}

