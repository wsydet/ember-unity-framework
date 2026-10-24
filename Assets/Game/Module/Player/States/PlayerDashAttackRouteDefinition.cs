using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    /// <summary>静态声明全部可路由目标。运行选择只返回已声明槽，不写配置或消费请求。</summary>
    public sealed class PlayerDashAttackRouteDefinition : EmberFsmStateDefinition<PlayerContext, PlayerDashAttackRouteState>, IPlayerCombatNodeDefinition
    {
        [SerializeField] private string[] _segments = Array.Empty<string>();
        public PlayerCombatNodeRole Role => PlayerCombatNodeRole.Route;
        internal void Configure(IEnumerable<string> segments) => _segments = segments.ToArray();
        public override PlayerDashAttackRouteState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
        public override IReadOnlyList<string> GetRedirectSlots() => _segments.Select(s => "attack:" + s).ToArray();
    }
    public sealed class PlayerDashAttackRouteState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter()
        {
            var dash = Context.Combat?.Dash;
            return dash != null && dash.Gate && Context.Combat.CanAttack(dash.Target)
                ? EmberFsmEntryDecision.Redirect("attack:" + dash.Target) : EmberFsmEntryDecision.Reject;
        }
        public override void Enter(EmberFsmActivation activation) => throw new InvalidOperationException("出招路由只能预检重定向，不可成为活动节点。");
    }
}
