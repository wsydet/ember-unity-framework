using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using Game.Player;

namespace ActionContractConsumer
{
    // 同时兼容基础/生命/战斗/冲刺战斗；声明始终委托实际装配的原根，不能擅自补不存在的槽。
    public sealed class ActionProbeRoot : PlayerDashCombatLocomotionStateDefinition, IPlayerConfigurationCapture, IPlayerLifeRootDefinition
    {
        public EmberFsmStateDefinition BaseDefinition;
        public static readonly EmberFsmTransition<PlayerContext, PlayerLocomotionState> EnterAction = new(
            "project-probe", "project-probe", sites: new[] {
                new EmberFsmRequestSite<PlayerContext, PlayerLocomotionState>("action", EmberFsmTriggerPhase.FixedUpdate,
                    "project-action", 0, (s,c) => c.Actions.CanEnter && c.Entity.GetComponent<ActionContractProbe>()?.Requested == true,
                    "项目输入已请求且宿主允许接管", EmberFsmContinuation.UntilChanged) });
        public override PlayerLocomotionState CreateState() => new(true);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => BaseDefinition.GetTransitions().Concat(new[] { EnterAction }).ToArray();
        public void CapturePlayerConfiguration(PlayerConfigurationCapture capture) => BaseDefinition = capture.Capture(BaseDefinition);
    }
}
