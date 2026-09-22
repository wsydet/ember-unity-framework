using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public abstract class NarrativeNodeSO : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _nodeId = Guid.NewGuid().ToString("N");
        [SerializeField] private string _chapterId;
        [SerializeField] private int _contentRevision = 1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string NodeId => _nodeId;
        public string ChapterId => _chapterId;
        public int ContentRevision => _contentRevision;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal abstract NovelNode ReadDefinition(NarrativeChapterSO chapter);
        #endregion
    }

    [Serializable]
    public sealed class NarrativeRoute
    {
        #region 编辑器面板参数
        [SerializeField] private string _optionId = Guid.NewGuid().ToString("N");
        [SerializeField] private string _text;
        [SerializeField] private NovelCondition _condition = new();
        [SerializeField] private NarrativeNodeSO _target;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string OptionId => _optionId;
        public string Text => _text;
        public NovelCondition Condition => _condition;
        public NarrativeNodeSO Target => _target;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal NovelRoute ReadDefinition(NarrativeChapterSO chapter, string nodeId)
        {
            return new NovelRoute(_optionId, _text, chapter.ResolveTarget(_target, nodeId, _optionId),
                _condition == null ? null : JsonUtility.FromJson<NovelCondition>(JsonUtility.ToJson(_condition)));
        }
        #endregion
    }
}

