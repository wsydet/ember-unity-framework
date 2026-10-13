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
        #endregion
    }
}
