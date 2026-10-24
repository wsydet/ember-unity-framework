using Game.Entity;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(LaunchEnd2D.StopVertical)] [TestCase(LaunchEnd2D.ContinueVelocity)]
        public void CarrierExpiryCross_OwnRoofAndRiderClearanceAtExpiry(LaunchEnd2D end)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            float head = p.Context.Sensors.Shape.bounds.max.y;
            CarrierBarrier("own roof at step11", new Vector2(2, 9.65f), new Vector2(.5f, .1f));
            CarrierBarrier("rider roof at step11", new Vector2(0, head + 4.25f), new Vector2(1.2f, .1f));
            Physics2D.SyncTransforms();
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            launch.TryLaunch(ExternalOwner(), new LaunchRequest2D("expiry-and-roofs", Vector2.up * 20,
                LaunchMode2D.ConstantRise, 13f / 60, end, horizontal: LaunchHorizontal2D.PreserveControl));
            for (int i = 0; i < 11; i++)
            {
                launch.TrySetHorizontalVelocity(0); support.PrepareStep(Step); Decide(p);
                TestContext.WriteLine($"expiry-roof step={i+1} end={end} allowed={support.Velocity:F5} support={p.Context.Support.Velocity:F5} command={p.Context.Motor.CommandVelocity:F5}");
                Assert.That(p.Context.Support.Velocity.y, Is.EqualTo(support.Velocity.y).Within(.001f));
                Assert.That(p.Context.Motor.CommandVelocity.y, Is.EqualTo(support.Velocity.y).Within(.001f));
                if (i < 10) Assert.That(support.Velocity.y, Is.EqualTo(20).Within(.01f));
                else
                {
                    Assert.That(launch.IsLaunching, Is.False);
                    Assert.That(support.Velocity.y, end == LaunchEnd2D.StopVertical ? Is.EqualTo(0) : Is.LessThan(20));
                }
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void CarrierExpiryCross_ReplacementAndCurrentOwnerLossBeforeRiderCommit(bool destroy)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            var old = ExternalOwner(); var next = ExternalOwner();
            launch.TryLaunch(old, new LaunchRequest2D("old", Vector2.up * 20, LaunchMode2D.ConstantRise,
                Step, LaunchEnd2D.StopVertical));
            launch.TryLaunch(next, new LaunchRequest2D("new", new Vector2(2, 10), LaunchMode2D.ConstantRise,
                .2f, LaunchEnd2D.StopVertical));
            old.enabled = false; support.PrepareStep(Step); Simulate(p);
            Assert.That(p.Context.Support.Velocity.y, Is.EqualTo(10));
            if (destroy) UnityEngine.Object.DestroyImmediate(next.gameObject); else next.enabled = false;
            support.PrepareStep(Step); Decide(p);
            Assert.That(launch.IsLaunching, Is.False); Assert.That(support.Velocity.y, Is.Zero);
            Assert.That(p.Context.Support.Velocity.y, Is.Zero); Assert.That(p.Context.Motor.CommandVelocity.y, Is.Zero);
        }
    }
}
