using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase("Base")]
        [TestCase("LegacyReference")]
        [TestCase("HollowKnightStyle")]
        [TestCase("CelesteStyle")]
        public void PresetMeasuresRealJumpAndShortJumpWithFormalAssembly(string preset)
        {
            var source = Resources.Load<PlayerFeelProfile>("Config/Player/Feel/" + preset + "Profile");
            Assert.That(source, Is.Not.Null);
            var copy = Keep(source.CreateIndependentCopy());
            Keep(copy.Movement); Keep(copy.Jump); Keep(copy.Input);
            Assert.That(copy.Movement, Is.Not.SameAs(source.Movement));
            Assert.That(copy.Jump, Is.Not.SameAs(source.Jump));
            Assert.That(copy.Input, Is.Not.SameAs(source.Input));
            _config.SetFeelProfile(copy);
            SetAirField(_config, "_powers", Resources.Load<PlayerConfig>("Config/Player/PlayerConfig").Powers);
            var player = Character(); Keys(); Land(player);
            float baseline = player.Context.Motor.Position.y;
            Keys(Key.Space);
            float peak = baseline, apex = 0;
            for (int n = 0; n < 150; n++)
            {
                Simulate(player);
                if (player.Context.Motor.Position.y > peak) { peak = player.Context.Motor.Position.y; apex = (n + 1) * Step; }
                if (n > 1 && player.Context.JumpWindow.HasGroundSupport) break;
            }
            Assert.That(peak - baseline, Is.InRange(.5f, 8f));
            Assert.That(apex, Is.InRange(.1f, 1f));
            Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True);
            Keys(); Simulate(player); baseline = player.Context.Motor.Position.y;
            Keys(Key.Space); Simulate(player); float shortPeak = player.Context.Motor.Position.y;
            Keys();
            for (int n = 0; n < 150; n++)
            {
                Simulate(player); shortPeak = Mathf.Max(shortPeak, player.Context.Motor.Position.y);
                if (player.Context.JumpWindow.HasGroundSupport) break;
            }
            Assert.That(shortPeak - baseline, Is.GreaterThan(0));
            Assert.That(shortPeak, Is.LessThan(peak), "松键短跳必须低于同预设按住跳跃");
            Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True);
            Keys(Key.D); float toSpeed = 0;
            for (int n = 0; n < 50; n++)
            {
                Simulate(player);
                if (player.Context.Motor.Velocity.x >= player.Context.Feel.MoveSpeed * .99f) { toSpeed = (n + 1) * Step; break; }
            }
            Assert.That(toSpeed, Is.GreaterThan(0));
            Keys(); float toStop = 0;
            for (int n = 0; n < 50; n++)
            {
                Simulate(player);
                if (Mathf.Abs(player.Context.Motor.Velocity.x) < .01f) { toStop = (n + 1) * Step; break; }
            }
            Assert.That(toStop, Is.GreaterThan(0));
            TestContext.WriteLine($"PRESET {preset}: dt={Step:F2}s; fullHeight={peak - baseline:F4}u; apex={apex:F2}s; shortHeight={shortPeak - baseline:F4}u; reach99={toSpeed:F2}s; stop={toStop:F2}s; estimateHeight={player.Context.Feel.EstimatedHeight:F4}u; estimateApex={player.Context.Feel.EstimatedApexTime:F4}s");
        }
    }
}
