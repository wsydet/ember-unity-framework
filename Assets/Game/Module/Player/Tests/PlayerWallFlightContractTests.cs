using System;
using System.Collections;
using System.Collections.Generic;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void WallFlightLog(string line)
        {
            TestContext.WriteLine(line);

        }
        private void WallContract(bool restore = false)
        {
            InstallWalls();
            _config.Feel.Jump.ConfigureWallJump(new PlayerWallJumpSpec(
                new PlayerOrdinaryJumpSpec(14, 2f / 60, 13f / 60, 120, 120, 20),
                15, 10, 30, true, 3f / 60, restore));
        }

        [TestCase(-1, -1)] [TestCase(-1, 0)] [TestCase(-1, 1)]
        [TestCase(1, -1)] [TestCase(1, 0)] [TestCase(1, 1)]
        public void WallFlight_BranchesBothSidesAndQualification(int wall, int direction)
        {
            WallContract();
            WallBox("Wall", new Vector2(wall * 1.02f, 5), new Vector2(1, 12));
            var p = Character(new Vector2(0, 5), Vector2.down);
            Keys(direction == 0 ? new[] { Key.Space } : new[] { Key.Space, direction > 0 ? Key.D : Key.A });
            Decide(p);
            bool vertical = direction == wall;
            Assert.That(p.Context.Wall.VerticalBranch, Is.EqualTo(vertical));
            Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(vertical ? 0 : -wall * 15));
            Assert.That(p.Context.VerticalMotion.WallActive, Is.True);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.EqualTo(!vertical));
            // 再贴墙：不恢复消耗的资格；落地才恢复。
            Keys(); Place(p, new Vector2(0, 5), -1); Decide(p); Decide(p);
            Keys(Key.Space, wall > 0 ? Key.D : Key.A); Decide(p);
            Assert.That(p.Context.Wall.VerticalBranch, Is.EqualTo(!vertical));
            Keys(); Place(p, new Vector2(-wall * 3, .9f), -1); Land(p);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void WallFlight_EarlyConversionRestoresOnlyWhenConfigured(bool restore)
        {
            WallContract(restore); var p = AtRightWall(); Keys(Key.D, Key.Space); Simulate(p);
            Keys(Key.A, Key.Space); Decide(p);
            Assert.That(p.Context.Wall.VerticalBranch, Is.False);
            Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(-15));
            Assert.That(p.Context.Wall.VerticalAvailable, Is.EqualTo(restore));
            Simulate(p);
            Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(-14.4f).Within(.001f));
        }

        [TestCase(-1, false)] [TestCase(-1, true)] [TestCase(1, false)] [TestCase(1, true)]
        public void WallFlight_FullShortLongCurvesAndCeiling(int wall, bool toward)
        {
            WallContract();
            WallBox("Wall", new Vector2(wall * 1.02f, 5), new Vector2(1, 12));
            float Run(float hold)
            {
                Keys(); var p = Character(new Vector2(0, 2), Vector2.down);
                float start = p.Context.Motor.Position.y, peak = start; bool air = false, landed = false;
                Keys(toward ? new[] { wall > 0 ? Key.D : Key.A, Key.Space } : new[] { Key.Space });
                for (int i = 0; i < 200; i++)
                {
                    if (i * Step >= hold) Keys();
                    Simulate(p); peak = Mathf.Max(peak, p.Context.Motor.Position.y);
                    WallFlightLog($"SYNTHETIC wall={wall} toward={toward} hold={hold:R} t={(i+1)*Step:R} p={p.Context.Motor.Position.ToString("F6")} v={p.Context.Motor.Velocity.ToString("F6")} state={p.CurrentStateId}");
                    air |= !p.Context.JumpWindow.HasGroundSupport;
                    if (air && p.Context.JumpWindow.HasGroundSupport && !p.Context.VerticalMotion.HasFlightPolicy) { landed = true; break; }
                }
                Assert.That(landed, Is.True); Assert.That(p.Context.VerticalMotion.WallActive, Is.False);
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); return peak - start;
            }
            Assert.That(Run(.6f), Is.GreaterThan(Run(.04f) + 1));
            WallBox("Ceiling", new Vector2(0, 4), new Vector2(8, 1));
            var c = Character(new Vector2(0, 2), Vector2.down); Keys(toward ? new[] { wall > 0 ? Key.D : Key.A, Key.Space } : new[] { Key.Space });
            for (int i = 0; i < 30; i++) Simulate(c);
            Assert.That(c.Context.Motor.Position.y, Is.LessThan(2.65f));
            Assert.That(c.Context.VerticalMotion.HoldingJump, Is.False);
        }

        [TestCase("air-auto", true)] [TestCase("ground-auto", true)] [TestCase("ground-flag-jump", false)]
        public void WallFlight_ActionExitPreservesSelectedDescentAndAscent(string source, bool enabled)
        {
            InstallActionGraph(10); float initialY = source == "air-auto" ? 3 : .9f;
            var p = Character(new Vector2(0, initialY)); var probe = StartAction(p);
            Assert.That(probe.Lease.LaunchVertical(14, 3, probe, source, enabled ? new PlayerFlightSpec(120, 20) : default), Is.True);
            Simulate(p); var flight = probe.Lease.Flight;
            for (int i = 0; i < 3; i++) Simulate(p);
            probe.Lease.Release(PlayerActionChannels.Vertical);
            Assert.That(probe.Lease.End(), Is.EqualTo(EmberFsmResult.Changed));
            float peak = p.Context.Motor.Position.y; bool fell = false, landed = false;
            for (int i = 0; i < 160; i++)
            {
                float before = p.Context.Motor.Velocity.y; Simulate(p);
                peak = Mathf.Max(peak, p.Context.Motor.Position.y);
                WallFlightLog($"SYNTHETIC {source} t={(i+5)*Step:R} p={p.Context.Motor.Position.ToString("F6")} v={p.Context.Motor.Velocity.ToString("F6")} state={p.CurrentStateId}");
                if (before > .7f) Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(3).Within(.001));
                if (before < -1 && !p.Context.JumpWindow.HasGroundSupport)
                {
                    fell = true;
                    if (enabled) { if (before > -15) Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(120 / -Physics2D.gravity.y).Within(.001)); Assert.That(flight.IsActive, Is.True); Assert.That(p.Context.Motor.Velocity.y, Is.GreaterThanOrEqualTo(-20.001f)); }
                    else Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(3));
                }
                if (p.Context.JumpWindow.HasGroundSupport && !p.Context.VerticalMotion.HasFlightPolicy) { landed = true; break; }
            }
            Assert.That(peak - initialY, Is.EqualTo(3.19093f).Within(.06));
            Assert.That(fell && landed, Is.True);
            Assert.That(p.Context.VerticalMotion.Flight, Is.Null);
        }

        [Test]
        public void WallFlight_OldLeaseReplacementDisablePauseAndSnapshot()
        {
            var p = Character(new Vector2(0, 8)); var owner = ExternalOwner();
            var old = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("first", PlayerExternalMotionMode.InitialVelocity,
                Vector2.up * 14, flight: new PlayerFlightSpec(120, 20)));
            Simulate(p); var flight = old.Flight;
            Assert.That(flight.IsActive, Is.True); Assert.That(old.IsActive, Is.False);
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            p.Pause(); Decide(p); Assert.That(flight.IsActive, Is.True); p.Resume();
            var next = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("second", PlayerExternalMotionMode.InitialVelocity,
                Vector2.up * 14, flight: new PlayerFlightSpec(90, 18)));
            Simulate(p); flight.Dispose(); old.Dispose();
            Assert.That(next.Flight.IsActive, Is.True); Assert.That(next.Flight.Generation, Is.GreaterThan(flight.Generation));
            owner.enabled = false; Decide(p); Assert.That(next.Flight.IsActive, Is.False);
            Assert.That(p.Context.VerticalMotion.Flight, Is.Null);
            owner.enabled = true;
            var last = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("third", PlayerExternalMotionMode.InitialVelocity,
                Vector2.up * 14, flight: new PlayerFlightSpec(120, 20)));
            Simulate(p); p.Stop(); Assert.That(last.Flight.IsActive, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void WallFlightBoundary_LifeAndHostClear(bool lethal)
        {
            WithThreeHit((p, scheme) =>
            {
                var owner = ExternalOwner();
                var external = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("life-flight",
                    PlayerExternalMotionMode.InitialVelocity, Vector2.up * 14, flight: new PlayerFlightSpec(120, 20)));
                Simulate(p); var flight = external.Flight;
                Assert.That(flight.IsActive, Is.True);
                p.Context.HitReceiver.Receive(TestHit(damage: lethal ? 20 : 1)); Decide(p);
                Assert.That(flight.IsActive, Is.False);
            }, scheme => InstallLifeTest(scheme));
        }

        [Test]
        public void WallFlightBoundary_HeldFallPauseWallAndOrdinaryIsolation()
        {
            WallContract(); _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var p = AtRightWall(5); var owner = ExternalOwner(); Keys(Key.Space);
            var external = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("hold-fall",
                PlayerExternalMotionMode.InitialVelocity, Vector2.up * 14, flight: new PlayerFlightSpec(120, 20, true, 5, 60)));
            Simulate(p); Place(p, new Vector2(-3, 5), -2); Decide(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(60 / -Physics2D.gravity.y).Within(.001));
            p.Pause(); p.Resume(); Keys(Key.Space); Decide(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(120 / -Physics2D.gravity.y).Within(.001));
            Place(p, new Vector2(0, 5), -1); Keys(); Decide(p); Keys(Key.Space, Key.D); Decide(p);
            Assert.That(external.Flight.IsActive, Is.False); Assert.That(p.Context.VerticalMotion.WallActive, Is.True);
            Keys(); Place(p, new Vector2(-3, .9f), -1); Land(p); Keys(Key.Space); Decide(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True); Assert.That(p.Context.VerticalMotion.WallActive, Is.False);
            Assert.That(p.Context.VerticalMotion.Flight, Is.Null);
        }

        [Test]
        public void WallFlightBoundary_NewActionOldActionAndOwnerDestroy()
        {
            InstallActionGraph(10); var p = Character(new Vector2(0, 8)); var probe = StartAction(p);
            var old = probe.Lease; old.LaunchVertical(14, 3, probe, "old", new PlayerFlightSpec(120, 20));
            Simulate(p); var first = old.Flight; old.End();
            var next = StartAction(p); next.Lease.LaunchVertical(14, 3, probe, "next", new PlayerFlightSpec(90, 18));
            Simulate(p); var second = next.Lease.Flight;
            old.Release(PlayerActionChannels.All); old.Dispose(); first.Dispose();
            Assert.That(second.IsActive, Is.True);
            // 策略在动作持有纵向通道时也覆盖下降，随后结束动作仍继续。
            Place(p, new Vector2(0, 8), -2); Decide(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(90 / -Physics2D.gravity.y).Within(.001));
            next.Lease.End(); UnityEngine.Object.DestroyImmediate(probe); Decide(p);
            Assert.That(second.IsActive, Is.False); Assert.That(p.Context.VerticalMotion.Flight, Is.Null);
        }

        [Test]
        public void WallFlightBoundary_LateReverseDoesNotConvertAndLosesWallNormally()
        {
            WallContract(); var p = AtRightWall(); Keys(Key.D, Key.Space);
            for (int i = 0; i < 5; i++) Simulate(p);
            Keys(Key.A, Key.Space); Decide(p);
            Assert.That(p.Context.Wall.VerticalBranch, Is.True); Assert.That(p.Context.Wall.VerticalAvailable, Is.False);
            Place(p, new Vector2(-3, 8), 5); Keys();
            for (int i = 0; i < 15; i++) Simulate(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(p.Context.Wall.VerticalAvailable, Is.False);
        }

        [Test]
        public void WallFlightBoundary_ImmediateDownwardLaunchUsesPolicyBeforeEnvironment()
        {
            var p = Character(new Vector2(0, 8));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("down",
                PlayerExternalMotionMode.InitialVelocity, Vector2.down * 30, flight: new PlayerFlightSpec(120, 20)));
            Simulate(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(-20).Within(.001));
            Assert.That(p.Context.VerticalMotion.Flight.IsActive, Is.True);
        }

        [UnityTest]
        public IEnumerator WallFlight_ActualFrameRateCurves()
        {
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 30, 60, 240 })
                foreach (bool wall in new[] { true, false })
                {
                    Application.targetFrameRate = rate;
                    if (wall) WallContract();
                    Keys(); var p = wall ? AtRightWall(2) : Character(new Vector2(0, 3));
                    if (wall) Keys(Key.D, Key.Space);
                    else p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("frame-flight", PlayerExternalMotionMode.InitialVelocity,
                        Vector2.up * 14, flight: new PlayerFlightSpec(120, 20)));
                    float acc = 0, time = 0, real = 0, releasedAt = 0; int frames = 0; bool landed = false;
                    while (!landed && frames < 500)
                    {
                        yield return null;
                        float frame = Time.unscaledDeltaTime; frames++; real += frame;
                        if (wall && real >= .12f && releasedAt == 0) { Keys(); releasedAt = real; }
                        p.Tick(frame); acc += frame;
                        while (acc >= Step && !landed)
                        {
                            p.FixedTick(Step); _scene.GetPhysicsScene2D().Simulate(Step); acc -= Step; time += Step;
                            WallFlightLog($"SYNTHETIC target={rate} wall={wall} fixed={Step:R} t={time:R} p={p.Context.Motor.Position.ToString("F6")} v={p.Context.Motor.Velocity.ToString("F6")} phase={p.CurrentStateId}");
                            landed = time > .1f && p.Context.JumpWindow.HasGroundSupport && !p.Context.VerticalMotion.HasFlightPolicy;
                        }
                    }
                    WallFlightLog($"SYNTHETIC target={rate} actualMeanFPS={frames/real:R} fixed={Step:R} requestedHold=.12 actualHold={releasedAt:R} wall={wall}");
                    Assert.That(landed, Is.True); p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject);
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync; }
        }
    }
}
