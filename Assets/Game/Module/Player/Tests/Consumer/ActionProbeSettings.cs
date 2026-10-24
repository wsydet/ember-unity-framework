using System;
using Game.Player;
using UnityEngine;

namespace ActionContractConsumer
{
    public sealed class ActionProbeSettings : ScriptableObject, IPlayerConfigurationCapture
    {
        public float Duration = 1;
        public void CapturePlayerConfiguration(PlayerConfigurationCapture capture)
        { if (!float.IsFinite(Duration) || Duration <= 0) throw new InvalidOperationException("非法动作时间。"); }
    }
}
