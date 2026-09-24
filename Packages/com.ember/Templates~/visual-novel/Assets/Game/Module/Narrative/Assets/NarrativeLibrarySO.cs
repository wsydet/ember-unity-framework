using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>小说入口与稳定 ID 索引。剧情通过 Unity 资源引用保存，允许重命名和移动。</summary>
    public sealed class NarrativeLibrarySO : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeStorySO _current;
        [SerializeField] private List<NarrativeStorySO> _stories = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public const string RESOURCE_PATH = "Config/Narrative/Library";
        public const string CURRENT_STORY = "story:current";
        public NarrativeStorySO Current => _current;
        public IReadOnlyList<NarrativeStorySO> Stories => _stories.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeStorySO Find(string storyId)
        {
            NarrativeStorySO found = null;
            foreach (var story in _stories)
            {
                if (!story || story.StoryId != storyId) continue;
                if (found && found != story) throw new InvalidOperationException("小说 ID 重复，请在当前小说配置中检查：" + storyId);
                found = story;
            }
            return found;
        }
        #endregion
    }
}
