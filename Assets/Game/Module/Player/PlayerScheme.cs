using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    /// <summary>玩家装配入口。运动和能力数值仍由所引用配置持有，场景与存储不属于方案。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Scheme", fileName = "PlayerScheme")]
    public sealed class PlayerScheme : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private EmberFsmGraph _graph;
        [SerializeField] private PlayerFeelProfile _feel;
        [SerializeField] private PlayerPowerConfig _powers;
        [SerializeField] private PlayerAnimationProfile _animation;
        [SerializeField] private PlayerActionEffects _actionEffects;
        [SerializeField] private PlayerRuleMode _ruleMode;
        [SerializeField] private PlayerSchemeRules _rules;
        [Tooltip("新游戏是否装配效果容器。动作效果唯一来源为本方案的 Action Effects。")]
        [SerializeField] private bool _enableEffects;
        [SerializeField] private string _inputChannel = "gameplay";
        [SerializeField] private string _moveAction = "Direction";
        [SerializeField] private string _jumpAction = "Primary";
        [SerializeField] private string _dashAction = "Dash";
        [SerializeField] private string _attackAction = "Attack";
        [SerializeField] private PlayerCombo _combo = new();
        [SerializeField] private PlayerCombatGraphMap _combatGraph = new();
        [SerializeField] private PlayerLifeConfiguration _life = new();
        [SerializeField] private PlayerReactionConfiguration _reactions = new();
        [SerializeField] private PlayerRecoveryConfiguration _recovery = new();
        [Tooltip("停顿派生可执行提示；空引用明确表示不显示提示。要求启用效果容器及 Owned 生命周期。")]
        [SerializeField] private Game.Entity.EntityEffectDefinition _delayedAttackPrompt;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberFsmGraph Graph => _graph;
        public PlayerFeelProfile Feel => _feel;
        public PlayerPowerConfig Powers => _powers;
        public PlayerAnimationProfile Animation => _animation;
        public PlayerActionEffects ActionEffects => _actionEffects;
        public PlayerRuleMode RuleMode => _ruleMode;
        public PlayerSchemeRules Rules => _rules;
        public bool EnableEffects => _enableEffects;
        public string Channel => _inputChannel;
        public string MoveAction => _moveAction;
        public string JumpAction => _jumpAction;
        public string DashAction => _dashAction;
        public string AttackAction => _attackAction;
        public PlayerCombo Combo => _combo;
        public PlayerCombatGraphMap CombatGraph => _combatGraph;
        public PlayerLifeConfiguration Life => _life;
        public PlayerReactionConfiguration Reactions => _reactions;
        public PlayerRecoveryConfiguration Recovery => _recovery;
        public Game.Entity.EntityEffectDefinition DelayedAttackPrompt => _delayedAttackPrompt;
        /// <summary>仅供隔离候选预检；临时图不得随候选方案保存。</summary>
        internal void SetPreparedGraph(EmberFsmGraph graph) => _graph = graph;
        internal void SetLifeConfiguration(PlayerLifeConfiguration life, PlayerReactionConfiguration reactions)
        { _life = PlayerCombatData.Copy(life); _reactions = PlayerCombatData.Copy(reactions); }
        internal void SetCombatConfiguration(PlayerCombo combo, PlayerCombatGraphMap map)
        {
            _combo = PlayerCombatData.Copy(combo);
            _combatGraph = PlayerCombatData.Copy(map);
        }
        #endregion
    }
}
