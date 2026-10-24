using UnityEngine;

namespace Game.Entity
{
    /// <summary>技能设计数据。CD 为游戏秒；0 表示无 CD，可按需叠加次数限制。</summary>
    [CreateAssetMenu(menuName = "Game/Entity/Skill", fileName = "EntitySkill")]
    public class EntitySkillDefinition : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [UnityEngine.Serialization.FormerlySerializedAs("_defaultUnlocked")][SerializeField] private bool _defaultOpen;
        [SerializeField, Min(0)] private float _cooldownSeconds;
        [SerializeField] private bool _limitedUses;
        [SerializeField, Min(1)] private int _maximumUses = 1;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public bool DefaultOpen => _defaultOpen;
        public float CooldownSeconds => _cooldownSeconds;
        public bool LimitedUses => _limitedUses;
        public int MaximumUses => _maximumUses;
        internal EntityPowerSpec Capture(bool enabled) => new(_id, enabled, _defaultOpen,
            _limitedUses, _maximumUses, _cooldownSeconds);
        #endregion
    }
}

