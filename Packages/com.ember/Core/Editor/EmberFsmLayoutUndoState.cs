using System;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>仅供本机布局 Undo；不成为图资产或运行配置。</summary>
    internal sealed class EmberFsmLayoutUndoState : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] internal Entry[] Entries = Array.Empty<Entry>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable]
        internal struct Entry
        {
            public string Id;
            public Vector2 Position;
            public bool Pinned;
        }
        #endregion
    }
}
