using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player.Tests
{
    /// <summary>仅用于 Skill 验收的无可变配置声明；不装入正式玩家图。</summary>
    public sealed class PlayerSkillProbeDefinition : EmberFsmStateDefinition<PlayerContext, PlayerSkillProbeState>
    {
        #region 外部方法
        public override PlayerSkillProbeState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { PlayerSkillProbeState.Finished };
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "idle" };
        #endregion
    }

    /// <summary>0.12 游戏秒展示叶子。只观察独立可选能力，不消费输入/资源、不接管运动，保存后不续接阶段。</summary>
    public sealed class PlayerSkillProbeState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        internal const string ABILITY_ID = "skill-acceptance-pose";
        private float _remaining;
        internal static readonly EmberFsmTransition<PlayerContext, PlayerSkillProbeState> Finished =
            new("finished", "idle", description: "展示结束回到地面待机",
                sites: new[] { PlayerRule.Fixed<PlayerSkillProbeState>(10, (state, ctx) => state._remaining <= .000001f,
                    "展示剩余时间 <= 0", segment: "elapsed", stopOnMatch: true) });
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmEntryDecision CanEnter() => Context.Powers.Abilities.TryGet(ABILITY_ID, out var power)
            && power.IsEnabled && power.IsOpen ? EmberFsmEntryDecision.Allow : EmberFsmEntryDecision.Redirect("idle");
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); _remaining = .12f; }
        public override void FixedUpdate(float deltaTime)
        { _remaining -= deltaTime; Activation.AttemptGroup("elapsed"); }
        public override void Exit() => _remaining = 0;
        public override void Dispose() => _remaining = 0;
        #endregion
    }
}
