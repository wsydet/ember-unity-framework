using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeChapterSO : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _chapterId = Guid.NewGuid().ToString("N");
        [SerializeField] private string _displayName;
        /// <summary>章节名的多语言 Key；留空用 _displayName。</summary>
        [SerializeField] private string _displayNameKey;
        [SerializeField] private string _assetPrefix;
        [SerializeField] private int _storyRevision = 1;
        [SerializeField] private NarrativeNodeSO _entry;
        [SerializeField] private List<NarrativeNodeSO> _nodes = new();
        [SerializeField] private List<NovelVariable> _variables = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string ChapterId => _chapterId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        /// <summary>运行期显示名：填了 Key 就取当前语言，否则用 DisplayName（源语言）。</summary>
        public string LocalizedDisplayName => NovelLocalization.TryGetContent(_displayNameKey, out string localized) ? localized : DisplayName;
        public string AssetPrefix => string.IsNullOrWhiteSpace(_assetPrefix) ? _chapterId : _assetPrefix;
        public int StoryRevision => _storyRevision;
        public NarrativeNodeSO Entry => _entry;
        public IReadOnlyList<NarrativeNodeSO> Nodes => _nodes.AsReadOnly();
        public IReadOnlyList<NovelVariable> Variables => _variables.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal string ResolveTarget(NarrativeNodeSO target, string sourceId, string optionId = null)
        {
            if (target == null || !_nodes.Contains(target) || target.ChapterId != _chapterId)
                throw new NarrativeDefinitionException(new NarrativeError("BadTarget",
                    "连接目标缺失或不属于当前章节", _chapterId, sourceId, optionId: optionId));
            return target.NodeId;
        }

        /// <summary>创建本次会话的纯数据副本；编辑或播放均不共享可变运行状态。</summary>
        public bool TryReadDefinition(INarrativeCatalog catalog, out NovelChapter definition,
            out IReadOnlyList<NarrativeError> errors, IReadOnlyDictionary<string, NovelValue> globals = null, bool allowChapterExit = false,
            IReadOnlyDictionary<string, NovelCustomStepSO> customSteps = null)
        {
            definition = null;
            var failures = new List<NarrativeError>();
            try
            {
                string entry = ResolveTarget(_entry, null);
                var nodes = new List<NovelNode>();
                foreach (NarrativeNodeSO node in _nodes)
                {
                    if (node == null || node.ChapterId != _chapterId || node.ContentRevision < 1)
                        throw new NarrativeDefinitionException(new NarrativeError("BadNode",
                            "节点为空、章节归属不符或内容修订无效", _chapterId, node?.NodeId));
                    nodes.Add(node.ReadDefinition(this));
                }
                var variables = new List<NovelVariable>();
                foreach (NovelVariable variable in _variables)
                    variables.Add(variable == null ? null : new NovelVariable(variable.Id, variable.Value));
                var result = new NovelChapter(_chapterId, _storyRevision, entry, nodes, variables);
                failures.AddRange(NarrativeValidator.Validate(result, catalog, globals, allowChapterExit, customSteps));
                if (failures.Count == 0) definition = result;
            }
            catch (NarrativeDefinitionException ex) { failures.Add(ex.Error); }
            errors = failures.AsReadOnly();
            return definition != null;
        }
        #endregion
    }

    internal sealed class NarrativeDefinitionException : Exception
    {
        internal NarrativeError Error { get; }
        internal NarrativeDefinitionException(NarrativeError error) : base(error.Message) { Error = error; }
    }
}
