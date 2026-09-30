using System;
using UnityEngine;
namespace Game.Narrative
{
    [Serializable]
    public sealed class NarrativeFlowResult
    {
        #region 编辑器面板参数
        [SerializeField] private string _name = "完成";
        [SerializeField] private NarrativeNodeSO _target;
        #endregion
        #region 内部参数
        public string Name => _name;
        public NarrativeNodeSO Target => _target;
        #endregion
    }
}
