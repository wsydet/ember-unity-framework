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
        /// <summary>内容多语言 Key；留空用 _text，填了就从 novel_content_text 取当前语言文本。</summary>
        [SerializeField] private string _textKey;
        [SerializeField] private NovelCondition _condition = new();
        [SerializeField] private NarrativeNodeSO _target;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string OptionId => _optionId;
        public string Text => _text;
        public string TextKey => _textKey;
        public string LocalizedText => NovelLocalization.TryGetContent(_textKey, out string localized) ? localized : _text;
        public NovelCondition Condition => _condition;
        public NarrativeNodeSO Target => _target;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal NovelRoute ReadDefinition(NarrativeChapterSO chapter, string nodeId)
        {
            // 选项/分流文字不进指纹（Routes 只写 Id/TargetId/Condition），所以在读定义时解析是安全的。
            // 同时带上 Key：会话进行中切语言时显示层按 Key 重新解析（NovelLocalization.RouteText）。
            return new NovelRoute(_optionId, LocalizedText, chapter.ResolveTarget(_target, nodeId, _optionId),
                _condition == null ? null : JsonUtility.FromJson<NovelCondition>(JsonUtility.ToJson(_condition)), _textKey);
        }
        #endregion
    }
}

