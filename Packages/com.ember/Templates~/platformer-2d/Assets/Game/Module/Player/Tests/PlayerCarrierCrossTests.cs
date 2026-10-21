using System;
using Game.Entity;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(4, -4)] [TestCase(-4, -4)]
        [TestCase(4, 0)] [TestCase(-4, 0)]
        [TestCase(4, 4)] [TestCase(-4, 4)]
        public void CarrierCross_RiderFloorWallAndTangentialMotion(int x, int y)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            bool floor = y < 0;
            CarrierBarrier("rider barrier", floor ? new Vector2(0, 3) : new Vector2(Mathf.Sign(x) * 5, 5),
                floor ? new Vector2(30, .5f) : new Vector2(.5f, 30));
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            var start = support.Body.position;
            for (int i = 0; i < 30; i++)
            {
                CarrierStep(support, new Vector2(x, y), p);
                Assert.That(p.Context.Support.Velocity, Is.EqualTo(support.Velocity));
                Assert.That(p.Context.Support.Source, Is.SameAs(support));
            }
            Assert.That(floor ? support.Body.position.y : Mathf.Abs(support.Body.position.x),
                floor ? Is.GreaterThan(3.49f) : Is.LessThan(1.751f));
            Assert.That(floor ? support.Body.position.x - start.x : support.Body.position.y - start.y,
                Is.EqualTo((floor ? x : y) * .6f).Within(.04f));
        }

        [TestCase(-3)] [TestCase(0)] [TestCase(2)]
        public void CarrierCross_RiderMultipleAndOpposingWindContributesOnce(int x)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            using var a = p.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("wind-a", PlayerExternalMotionMode.AddVelocity, Vector2.right * 2));
            using var b = p.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("wind-b", PlayerExternalMotionMode.AddVelocity, Vector2.left * 3));
            for (int i = 0; i < 10; i++)
            {
                support.SetVelocity(new Vector2(x, 0)); support.PrepareStep(Step); Decide(p);
                Assert.That(p.Context.Motor.CommandVelocity.x, Is.EqualTo(x - 1).Within(.001f));
                Assert.That(p.Context.ExternalMotion.EnvironmentVelocity.x, Is.EqualTo(-1).Within(.001f));
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
        }

        [TestCase(SupportDepartureVelocity.None)]
        [TestCase(SupportDepartureVelocity.Horizontal)]
        [TestCase(SupportDepartureVelocity.Full)]
        public void CarrierCross_DepartureModesUseLastAllowedVelocity(SupportDepartureVelocity mode)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            JsonUtility.FromJsonOverwrite("{\"_departure\":" + (int)mode + "}", support);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            for (int i = 0; i < 3; i++) CarrierStep(support, new Vector2(2, 2), p);
            Keys(Key.Space); support.PrepareStep(Step); Simulate(p); Keys();
            for (int i = 0; i < 3; i++) { support.PrepareStep(Step); Simulate(p); }
            Assert.That(p.Context.Support.Source, Is.Null);
            Assert.That(p.Context.Support.InheritedVelocity, Is.EqualTo(mode switch
            {
                SupportDepartureVelocity.Horizontal => new Vector2(2, 0),
                SupportDepartureVelocity.Full => new Vector2(2, 2),
                _ => Vector2.zero
            }));
        }

        [TestCase(LaunchEnd2D.ContinueVelocity)] [TestCase(LaunchEnd2D.StopVertical)]
        public void CarrierCross_ReplacementExpiryAndOwnerInvalidation(LaunchEnd2D end)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            var old = ExternalOwner(); var next = ExternalOwner();
            LaunchRequest2D Request(string id, float y, float duration) =>
                new(id, Vector2.up * y, LaunchMode2D.ConstantRise, duration, end,
                    horizontal: LaunchHorizontal2D.PreserveControl);
            launch.TryLaunch(old, Request("old", 20, Step));
            launch.TryLaunch(next, Request("replacement", 10, Step * 2)); old.enabled = false;
            support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.True); Assert.That(support.Velocity.y, Is.EqualTo(10));
            support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.False);
            Assert.That(support.Velocity.y, Is.EqualTo(end == LaunchEnd2D.StopVertical ? 0 : 10));
            launch.TryLaunch(next, Request("invalid-owner", 20, 1)); next.enabled = false;
            support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.False);
            Assert.That(support.Velocity.y, Is.EqualTo(end == LaunchEnd2D.StopVertical ? 0 : 20));
        }

        [Test]
        public void CarrierCross_RiderPassesSourceOneWayFromBelow()
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            for (int i = 0; i < 14; i++)
            {
                var shape = CarrierBarrier("source tile", new Vector2(-6 + i, 8), new Vector2(1, .3f));
                shape.offset = new Vector2(0, .35f); shape.usedByEffector = true;
                var effector = shape.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.surfaceArc = 179.5f; effector.useOneWayGrouping = false;
            }
            Physics2D.SyncTransforms();
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            launch.TryLaunch(ExternalOwner(), new LaunchRequest2D("oneway rider", Vector2.up * 20,
                LaunchMode2D.ConstantRise, .4f, horizontal: LaunchHorizontal2D.PreserveControl));
            var trace = new System.Text.StringBuilder();
            for (int i = 0; i < 15; i++)
            {
                launch.TrySetHorizontalVelocity(2); support.PrepareStep(Step); Simulate(p);
                string row = $"oneway i={i} carrier={support.Body.position:F5} allowed={support.Velocity:F5} player={p.Context.Motor.Position:F5} actual={p.Context.Motor.Velocity:F5} source={p.Context.Support.Source} ground={p.Context.Sensors.Ground} contribution={p.Context.Support.Velocity:F5}";
                TestContext.WriteLine(row); trace.AppendLine(row);
                if (Directory.Exists("docs/dev/evidence/carrier-contract"))
                    File.WriteAllText("docs/dev/evidence/carrier-contract/oneway-rider-latest.txt", trace.ToString());
                Assert.That(support.Velocity.y, Is.EqualTo(20));
            }
            Assert.That(support.Body.position.y, Is.GreaterThan(10));
            Assert.That(p.Context.Support.Source, Is.SameAs(support));
        }

        [Test]
        public void CarrierCross_PausedSimulationAndResetDoNotRestoreOldLaunch()
        {
            var support = MovingSupport(); var reset = support.gameObject.AddComponent<TranslationSupportReset2D>();
            string baseline = reset.CaptureState();
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            launch.TryLaunch(ExternalOwner(), new LaunchRequest2D("pause-reset", Vector2.up * 20,
                LaunchMode2D.ConstantRise, Step * 2, LaunchEnd2D.StopVertical));
            support.Body.simulated = false;
            for (int i = 0; i < 20; i++) support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.True);
            support.Body.simulated = true; support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.True);
            reset.RestoreState(baseline, WorldResetReason2D.Death); support.PrepareStep(Step);
            Assert.That(launch.IsLaunching, Is.False); Assert.That(support.Velocity, Is.EqualTo(Vector2.zero));
        }
    }
}
