using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerScheme _scheme;
        [SerializeField] private EmberFsmGraph _graph;
        [SerializeField] private PlayerPowerConfig _powers;
        [SerializeField] private bool _enableEffects;
        [SerializeField] private string _inputChannel = "gameplay";
        [SerializeField] private string _moveAction = "Direction";
        [SerializeField] private string _jumpAction = "Primary";
        [SerializeField] private string _dashAction = "Dash";
        [SerializeField] private string _scene = "GameplayScene";
        [SerializeField] private Vector2 _spawn = new(0, 1);
        [SerializeField] private PlayerFeelProfile _feel;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmGraph SourceGraph { get; private set; }
        internal EmberFsmObservationOrigin ObservationOrigin { get; private set; }
        internal PlayerFeelRuntime CapturedFeel { get; private set; }
        internal PlayerAnimationProfile CapturedAnimation { get; private set; }
        internal EmberFsmRulePlan<PlayerContext> CapturedRules { get; private set; }
#if UNITY_EDITOR
        internal PlayerSchemeObservation SchemeObservation { get; private set; }
#endif
        public PlayerAnimationProfile Animation => _scheme != null ? _scheme.Animation : CapturedAnimation;
        internal void SetCapturedAssembly(PlayerConfig source, EmberFsmGraph graph, PlayerPowerConfig powers, PlayerFeelProfile feel,
            PlayerAnimationProfile animation, EmberFsmRulePlan<PlayerContext> rules = null)
        {
            SourceGraph = source.Graph;
            ObservationOrigin = EmberFsmObservationOrigin.Capture(source.Graph, graph);
            CapturedFeel = feel.Capture(Physics2D.gravity);
            CapturedAnimation = animation;
            CapturedRules = rules;
#if UNITY_EDITOR
            SchemeObservation = source.Scheme != null ? PlayerSchemeObservation.Capture(source.Scheme) : source.SchemeObservation;
#endif
            _graph = graph; _powers = powers; _feel = feel;
            _enableEffects = source.EnableEffects; _inputChannel = source.Channel;
            _moveAction = source.MoveAction; _jumpAction = source.JumpAction; _dashAction = source.DashAction;
            _scheme = null;
        }
        // 未转换资产使用完整旧装配；绑定方案后不逐字段回退，缺失引用由准备链明确拒绝。
        public PlayerScheme Scheme => _scheme;
        public EmberFsmGraph Graph => _scheme != null ? _scheme.Graph : _graph;
        public PlayerPowerConfig Powers => _scheme != null ? _scheme.Powers : _powers;
        public bool EnableEffects => _scheme != null ? _scheme.EnableEffects : _enableEffects;
        public string Channel => _scheme != null ? _scheme.Channel : _inputChannel;
        public string MoveAction => _scheme != null ? _scheme.MoveAction : _moveAction;
        public string JumpAction => _scheme != null ? _scheme.JumpAction : _jumpAction;
        public string DashAction => _scheme != null ? _scheme.DashAction : _dashAction;
        public string Scene => _scene;
        public Vector2 Spawn => _spawn;
        public PlayerFeelProfile Feel => _scheme != null ? _scheme.Feel : _feel;
        public void SetFeelProfile(PlayerFeelProfile feel)
        {
            if (_scheme != null) throw new System.InvalidOperationException("已绑定完整方案，不能通过旧配置单独覆盖 Feel。");
            _feel = feel;
        }
        public void Configure(EmberFsmGraph graph, Vector2 spawn)
        {
            if (_scheme != null && graph != _scheme.Graph)
                throw new System.InvalidOperationException("已绑定完整方案，不能通过旧配置单独覆盖 Graph。");
            if (_scheme == null) _graph = graph;
            _spawn = spawn;
        }
        #endregion
    }
}
