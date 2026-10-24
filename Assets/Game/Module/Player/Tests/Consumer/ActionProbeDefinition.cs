using System.Collections.Generic;
using Ember.Core.Fsm;
using Game.Player;

namespace ActionContractConsumer
{
    public sealed class ActionProbeDefinition : EmberFsmStateDefinition<PlayerContext, ActionProbeState>, IPlayerConfigurationCapture
    {
        public ActionProbeSettings Settings;
        public void CapturePlayerConfiguration(PlayerConfigurationCapture capture) => Settings = capture.Capture(Settings);
        public override ActionProbeState CreateState() => new(Settings.Duration);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { ActionProbeState.Finish };
    }
}
