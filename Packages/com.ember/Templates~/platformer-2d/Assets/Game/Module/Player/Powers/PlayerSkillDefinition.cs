using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>玩家动作配置；公共身份/资源继承 Entity 基础，只有支撑补充属于玩家。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Skill", fileName = "PlayerSkill")]
    public sealed class PlayerSkillDefinition : EntitySkillDefinition
    {
        #region 编辑器面板参数
        [SerializeField] private bool _refillOnGroundSupport;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool RefillOnGroundSupport => _refillOnGroundSupport;
        public bool DefaultUnlocked => DefaultOpen;
        #endregion
    }
}
