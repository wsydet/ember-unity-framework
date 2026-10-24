using UnityEngine;

namespace Game.Entity
{
    /// <summary>动作能力的设计数据。稳定 ID 不依赖 FSM 节点；不保存角色运行数据。</summary>
    [CreateAssetMenu(menuName = "Game/Entity/Ability", fileName = "EntityAbility")]
    public class EntityAbilityDefinition : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [UnityEngine.Serialization.FormerlySerializedAs("_defaultUnlocked")][SerializeField] private bool _defaultOpen;
        [SerializeField] private bool _limitedUses;
        [SerializeField, Min(1)] private int _maximumUses = 1;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public bool DefaultOpen => _defaultOpen;
        public bool LimitedUses => _limitedUses;
        public int MaximumUses => _maximumUses;
        internal EntityPowerSpec Capture(bool enabled) => new(_id, enabled, _defaultOpen,
            _limitedUses, _maximumUses, 0);
        #endregion
    }
}

