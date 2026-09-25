using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>仅 Editor 使用。没有运行路线或节点资产引用。</summary>
    public sealed class NarrativeGraphLayout : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _chapterGuid;
        [SerializeField] private List<NodePosition> _positions = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string ChapterGuid => _chapterGuid;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [Serializable]
        private struct NodePosition
        {
            [SerializeField] internal string _nodeId;
            [SerializeField] internal Vector2 _position;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Initialize(string guid) { _chapterGuid = guid; }
        public Vector2 GetPosition(string id, int index)
        {
            foreach (var item in _positions) if (item._nodeId == id) return item._position;
            return new Vector2(index % 3 * 300, index / 3 * 240);
        }
        internal void SetPosition(string id, Vector2 value)
        {
            var position = new NodePosition { _nodeId = id, _position = value };
            for (int i = 0; i < _positions.Count; i++)
                if (_positions[i]._nodeId == id) { _positions[i] = position; return; }
            _positions.Add(position);
        }
        #endregion
    }
}
