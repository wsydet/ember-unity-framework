using System;
using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player.Tests
{
    /// <summary>隔离请求顺序夹具，不保存到正式方案。</summary>
    public sealed class PlayerCombatArbitrationProbe : EmberFsmStateDefinition<PlayerContext, PlayerCombatArbitrationState>
    {
        internal int Action = -1;
        internal bool Reject;
        internal bool[] Gates = { true, true, true, true };
        internal int Commits;
        public override PlayerCombatArbitrationState CreateState() => new(this);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Action < 0
            ? PlayerCombatArbitrationState.Transitions : Array.Empty<EmberFsmTransition>();
    }

    public sealed class PlayerCombatArbitrationState : EmberFsmState<PlayerContext>
    {
        private readonly PlayerCombatArbitrationProbe _definition;
        internal static readonly EmberFsmTransition[] Transitions =
        {
            new EmberFsmTransition<PlayerContext, PlayerCombatArbitrationState>("dash", "dash", sites: new[] {
                PlayerRule.Fixed<PlayerCombatArbitrationState>(10, (s,c) => s._definition.Gates[0] && c.Input.DashRequested, "冲刺开放且请求有效") }),
            new EmberFsmTransition<PlayerContext, PlayerCombatArbitrationState>("jump", "jump", sites: new[] {
                PlayerRule.Fixed<PlayerCombatArbitrationState>(20, (s,c) => s._definition.Gates[1] && c.Input.JumpRequested, "跳跃开放且请求有效") }),
            new EmberFsmTransition<PlayerContext, PlayerCombatArbitrationState>("attack", "attack", sites: new[] {
                PlayerRule.Fixed<PlayerCombatArbitrationState>(30, (s,c) => s._definition.Gates[2] && c.Input.AttackRequested, "攻击开放且请求有效") }),
            new EmberFsmTransition<PlayerContext, PlayerCombatArbitrationState>("move", "move", sites: new[] {
                PlayerRule.Fixed<PlayerCombatArbitrationState>(40, (s,c) => s._definition.Gates[3] && c.Input.Horizontal != 0, "移动开放且有当前输入") })
        };
        internal PlayerCombatArbitrationState(PlayerCombatArbitrationProbe definition) => _definition = definition;
        public override EmberFsmEntryDecision CanEnter() => _definition.Reject ? EmberFsmEntryDecision.Reject : EmberFsmEntryDecision.Allow;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            if (_definition.Action < 0) return;
            bool accepted = _definition.Action switch
            {
                0 => Context.Input.ConsumeDash(), 1 => Context.Input.ConsumeJump(), 2 => Context.Input.ConsumeAttack(), _ => true
            };
            if (!accepted) throw new InvalidOperationException("预检通过后请求不应丢失。");
            if (_definition.Action == 3) Context.Input.Clear();
            _definition.Commits++;
        }
        public override void FixedUpdate(float dt)
        { if (_definition.Action < 0) Activation.AttemptGroup("decision"); }
    }
}
