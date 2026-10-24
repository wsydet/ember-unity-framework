using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerRuleMode { LegacyCode, Configured }

    [Serializable]
    public sealed class PlayerSchemeRule
    {
        [SerializeField] private string _nodeId = "air";
        [SerializeField] private string _transitionId;
        [SerializeField] private string _siteId = "fixed";
        [SerializeField] private EmberConditionExpression _condition;
        public string NodeId => _nodeId;
        public string TransitionId => _transitionId;
        public string SiteId => _siteId;
        public EmberConditionExpression Condition => _condition;
        public PlayerSchemeRule(string transition, EmberConditionExpression condition)
        { _transitionId = transition; _condition = condition; }
    }

    /// <summary>方案独占的静态附加条件；不可与其它方案共享，也不持有运行状态。</summary>
    public sealed class PlayerSchemeRules : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private int _schemaVersion = 1;
        [SerializeField] private PlayerScheme _owner;
        [SerializeField] private PlayerSchemeRule[] _rules = Array.Empty<PlayerSchemeRule>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public int SchemaVersion => _schemaVersion;
        public PlayerScheme Owner => _owner;
        public IReadOnlyList<PlayerSchemeRule> Rules => Array.AsReadOnly(_rules);
        [HasGC]
        public static PlayerSchemeRules CreateNeutral(PlayerScheme owner)
        {
            var asset = CreateInstance<PlayerSchemeRules>(); asset.name = "转换附加条件"; asset._owner = owner;
            asset._rules = new[] { new PlayerSchemeRule("air-jump", EmberConditionExpression.True()),
                new PlayerSchemeRule("ledge-hang", EmberConditionExpression.True()), new PlayerSchemeRule("wall-slide", EmberConditionExpression.True()) };
            return asset;
        }
        [HasGC]
        public static EmberFsmRulePlan<PlayerContext> Capture(PlayerScheme scheme)
        {
            if (scheme == null) return null;
            if (scheme.RuleMode == PlayerRuleMode.LegacyCode)
            {
                if (scheme.Rules != null) throw new InvalidOperationException("旧版代码模式不应携带规则配置；请显式修正方案。");
                return null;
            }
            if (scheme.RuleMode != PlayerRuleMode.Configured || scheme.Rules == null) throw new InvalidOperationException("配置模式缺少规则资产。");
            var asset = scheme.Rules;
            if (asset._schemaVersion != 1) throw new InvalidOperationException("规则版本不支持：" + asset._schemaVersion);
            if (asset._owner != scheme) throw new InvalidOperationException("规则资产属于另一方案；请复制为独立配置。");
            if (asset._rules == null || asset._rules.Length != 3) throw new InvalidOperationException("规则配置必须包含三个入口。");
            var handles = new EmberFsmTransition[] { PlayerAirState.AirJump, PlayerAirState.Hang, PlayerAirState.WallSlide };
            var catalog = PlayerRuleConditions.CreateCatalog(scheme);
            var bindings = new List<EmberFsmRuleBinding<PlayerContext>>();
            foreach (var rule in asset._rules)
            {
                var handle = handles.FirstOrDefault(h => h.Id == rule?.TransitionId);
                if (rule == null || rule.NodeId != "air" || rule.SiteId != "fixed" || handle == null)
                    throw new InvalidOperationException("规则只允许air的三个指定入口及fixed位置。");
                try { bindings.Add(new EmberFsmRuleBinding<PlayerContext>(rule.NodeId, handle, rule.SiteId, catalog.Compile(rule.Condition))); }
                catch (Exception ex) { throw new InvalidOperationException(rule.NodeId + "/" + rule.TransitionId + "/" + rule.SiteId + "：" + ex.Message, ex); }
            }
            var plan = new EmberFsmRulePlan<PlayerContext>(bindings.ToArray());
            if (scheme.Graph == null) throw new InvalidOperationException("规则方案缺少基础图。");
            plan.Validate(scheme.Graph.Inspect(typeof(PlayerContext))); return plan;
        }
        #endregion
    }
}
