using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Game.Narrative.Editor
{
    internal sealed class NarrativeNodeSearch : ScriptableObject, ISearchWindowProvider
    {
        #region 内部参数
        private Action<NovelNodeKind> _create;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Configure(Action<NovelNodeKind> create) => _create = create;
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) => new()
        {
            new SearchTreeGroupEntry(new GUIContent("添加剧情节点"), 0),
            new SearchTreeEntry(new GUIContent("对话段 Dialogue")) { level = 1, userData = NovelNodeKind.Dialogue },
            new SearchTreeEntry(new GUIContent("选择 Choice")) { level = 1, userData = NovelNodeKind.Choice },
            new SearchTreeEntry(new GUIContent("条件分流 Branch")) { level = 1, userData = NovelNodeKind.Branch },
            new SearchTreeEntry(new GUIContent("结局 Ending")) { level = 1, userData = NovelNodeKind.Ending },
            new SearchTreeEntry(new GUIContent("章节出口 Chapter Exit")) { level = 1, userData = NovelNodeKind.ChapterExit }
        };
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is not NovelNodeKind kind) return false;
            _create?.Invoke(kind); return true;
        }
        #endregion
    }
}
