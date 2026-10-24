using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class PlayerActionEffectSlot
    {
        #region 编辑器面板参数
        [SerializeField] private string _nodeId;
        [SerializeField] private EntityEffectDefinition _actionEffect;
        [SerializeField] private EntityEffectDefinition _tailEffect;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string NodeId => _nodeId;
        public EntityEffectDefinition ActionEffect => _actionEffect;
        public EntityEffectDefinition TailEffect => _tailEffect;
        public PlayerActionEffectSlot(string nodeId, EntityEffectDefinition action, EntityEffectDefinition tail)
        { _nodeId = nodeId; _actionEffect = action; _tailEffect = tail; }
        #endregion
    }

    /// <summary>完整方案拥有的动作接线。版本 1 的空引用明确表示无效果，不能回读遗留 Definition。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Action Effects", fileName = "PlayerActionEffects")]
    public sealed class PlayerActionEffects : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField, HideInInspector] private int _version;
        [SerializeField] private PlayerActionEffectSlot[] _slots = Array.Empty<PlayerActionEffectSlot>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public int Version => _version;
        public IReadOnlyList<PlayerActionEffectSlot> Slots => _slots;
        internal void Configure(PlayerActionEffectSlot[] slots) { _slots = slots; _version = 1; }
        internal Dictionary<string, PlayerActionEffectSlot> Validate(EmberFsmGraph graph)
        {
            if (_version != 1 || _slots == null || graph == null)
                throw new InvalidOperationException("动作效果尚未迁移或版本无效，请显式迁移完整方案。");
            var supported = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
                if (node.Definition is PlayerGroundDashStateDefinition or PlayerAirDashStateDefinition)
                    supported.Add(node.Id);
            var result = new Dictionary<string, PlayerActionEffectSlot>(StringComparer.Ordinal);
            foreach (var slot in _slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.NodeId) || !supported.Contains(slot.NodeId))
                    throw new InvalidOperationException("动作效果接线包含未知或不支持的图节点：" + slot?.NodeId);
                if (!result.TryAdd(slot.NodeId, slot))
                    throw new InvalidOperationException("动作效果接线包含重复节点：" + slot.NodeId);
                _ = new PlayerDashEffects(slot.ActionEffect, slot.TailEffect);
            }
            foreach (string id in supported)
                if (!result.ContainsKey(id)) throw new InvalidOperationException("动作效果接线缺少节点（无效果也需明确记录）：" + id);
            return result;
        }
        #endregion
    }
}
