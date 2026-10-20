using System;
using System.Collections;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private LaunchTailSpec2D TailSpec() => new(120, 120, 20, true, 5, 60, true);
        private LaunchRequest2D TailRequest(bool side = false, int sign = 1, float duration = 13f / 60,
            LaunchInterrupt2D interrupt = LaunchInterrupt2D.ReplaceExternalMotion)
            => new("tail-fixture", new Vector2(side ? sign * 20 : 0, side ? 10 : 20),
                LaunchMode2D.ConstantRise, duration, interrupt: interrupt,
                horizontal: side ? LaunchHorizontal2D.HoldRequestVelocity : LaunchHorizontal2D.PreserveControl,
                wallQualification: LaunchWallQualification2D.RefreshFirstVertical, tail: TailSpec());

        private PlayerLaunchTailLease BeginTestTail(PlayerController p)
        {
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), TailRequest(duration: Step)), Is.EqualTo(LaunchResult2D.Accepted));
            Simulate(p);
            Assert.That(target.IsLaunching, Is.False);
            return p.Context.VerticalMotion.LaunchTail;
        }

        [Test]
        public void LaunchTail_ValidationDefaultsAndCarrierRejection()
        {
            Assert.That(new LaunchRequest2D("old", Vector2.up).Tail.Enabled, Is.False);
            Assert.Throws<InvalidOperationException>(() => new LaunchTailSpec2D(float.NaN, 120, 20));
            Assert.Throws<InvalidOperationException>(() => new LaunchTailSpec2D(120, 120, 20, true, 20, 60));
            Assert.Throws<InvalidOperationException>(() => new LaunchTailSpec2D(120, 120, 20, latchSlowUntilDescending: true));
            Assert.Throws<ArgumentException>(() => new LaunchRequest2D("initial", Vector2.up, tail: TailSpec()));
            Assert.Throws<ArgumentException>(() => new LaunchRequest2D("stop", Vector2.up, LaunchMode2D.ConstantRise,
                .1f, LaunchEnd2D.StopVertical, tail: TailSpec()));
            Assert.Throws<ArgumentException>(() => new PlayerExternalMotionRequest("initial",
                PlayerExternalMotionMode.InitialVelocity, Vector2.up, launchTail: TailSpec()));
            Assert.Throws<ArgumentException>(() => new PlayerExternalMotionRequest("rise",
                PlayerExternalMotionMode.ForcedRise, Vector2.up, .1f, flight: new PlayerFlightSpec(120, 20)));
            Assert.Throws<InvalidOperationException>(() => TailSpec().ValidateWorld(Vector2.zero));
            var support = MovingSupport(); support.SetVelocity(Vector2.right * 3);
            var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), TailRequest()), Is.EqualTo(LaunchResult2D.UnsupportedPolicy));
            Assert.That(support.Velocity, Is.EqualTo(Vector2.right * 3));
        }

        [TestCase(false, -1, false)] [TestCase(false, 1, false)]
        [TestCase(false, -1, true)] [TestCase(false, 1, true)]
        [TestCase(true, -1, false)] [TestCase(true, 1, false)]
        [TestCase(true, -1, true)] [TestCase(true, 1, true)]
        public void LaunchTail_FullPhysicsTrajectory(bool side, int sign, bool held)
        {
            var p = Character(new Vector2(0, 8));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), TailRequest(side, sign)), Is.EqualTo(LaunchResult2D.Accepted));
            int holds = 0, tailSteps = 0; bool slow = false, fastFall = false, landed = false;
            for (int i = 0; i < 250; i++)
            {
                bool holding = target.IsLaunching;
                if (holding && !side) Keys(held ? new[] { Key.Space, sign > 0 ? Key.D : Key.A } : new[] { sign > 0 ? Key.D : Key.A });
                else Keys(held ? new[] { Key.Space } : Array.Empty<Key>());
                float before = p.Context.ExternalMotion.SelfVelocity.y;
                Decide(p);
                if (holding)
                {
                    holds++;
                    Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(side ? 10 : 20).Within(.001));
                    Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(sign * (side ? 20 : 5)).Within(.001));
                    Assert.That(p.Context.Motor.Body.gravityScale, Is.Zero);
                }
                else if (p.Context.VerticalMotion.LaunchTail != null)
                {
                    tailSteps++;
                    Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(Mathf.Max(before, -20)).Within(.001), "不得重发速度");
                    float acceleration = Mathf.Abs(before) <= 5 ? 60 : 120;
                    float expected = Mathf.Min(acceleration, Mathf.Max(0, (before + 20) / Step));
                    Assert.That(-Physics2D.gravity.y * p.Context.Motor.Body.gravityScale, Is.EqualTo(expected).Within(.001),
                        "首尾相邻固定步与低速边界");
                    slow |= Mathf.Abs(before) <= 5;
                    fastFall |= before < -5;
                    if (before < -5) Assert.That(p.Context.VerticalMotion.LaunchTail.SlowPermissionLatched, Is.False);
                }
                _scene.GetPhysicsScene2D().Simulate(Step);
                TestContext.WriteLine($"P06 input=synthetic-keyboard side={side} sign={sign} held={held} dt={Step:R} t={(i+1)*Step:R} hold={holding} p={p.Context.Motor.Position.ToString("F6")} self={p.Context.ExternalMotion.SelfVelocity.ToString("F6")} world={p.Context.Motor.Velocity.ToString("F6")} gravity={p.Context.Motor.Body.gravityScale:R}");
                if (i > 11 && p.Context.JumpWindow.HasGroundSupport && p.Context.VerticalMotion.LaunchTail == null)
                { landed = true; break; }
            }
            Assert.That(holds, Is.EqualTo(11), "13/60秒以0.02秒固定步量化");
            Assert.That(tailSteps, Is.GreaterThan(5));
            Assert.That(slow && fastFall && landed, Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchTail_FromOrdinaryOrWallCancelsOnlyPreviousPolicy(bool wall)
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            if (wall) WallContract();
            var p = wall ? AtRightWall() : Character();
            if (!wall) Land(p);
            Keys(wall ? new[] { Key.Space, Key.D } : new[] { Key.Space }); Simulate(p);
            Assert.That(wall ? p.Context.VerticalMotion.WallActive : p.Context.VerticalMotion.OrdinaryActive, Is.True);
            Keys();
            var tail = BeginTestTail(p);
            Assert.That(tail.IsActive, Is.True);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive || p.Context.VerticalMotion.WallActive, Is.False);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.True);
            // 尾段本身不再刷新资格。
            Keys(Key.D); p.Tick(Step);
            p.Context.Wall.BeginJump(1);
            bool qualification = p.Context.Wall.VerticalAvailable;
            if (wall) Assert.That(qualification, Is.False);
            Decide(p);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.EqualTo(qualification));
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchTail_LowBandLatchInputAndPause(bool held)
        {
            var p = Character(new Vector2(0, 8));
            var tail = BeginTestTail(p);
            foreach (float y in new[] { 5.001f, 5f, 0f, -5f, -5.001f, -5f, 5f, 5.001f })
            {
                Keys(); if (held) Keys(Key.Space);
                Place(p, new Vector2(0, 8), y); Decide(p);
                bool low = Mathf.Abs(y) <= 5 && (tail.SlowPermissionLatched || held);
                Assert.That(-Physics2D.gravity.y * p.Context.Motor.Body.gravityScale, Is.EqualTo(low ? 60 : 120).Within(.001));
                if (y < -5) Assert.That(tail.SlowPermissionLatched, Is.False);
            }
            p.Pause();
            var position = p.Context.Motor.Position; var velocity = p.Context.Motor.Velocity;
            for (int i = 0; i < 5; i++) Decide(p);
            Assert.That(p.Context.Motor.Position, Is.EqualTo(position));
            Assert.That(p.Context.Motor.Velocity, Is.EqualTo(velocity));
            Assert.That(tail.IsActive, Is.True);
            p.Resume(); Keys(); Keys(Key.Space);
            Place(p, new Vector2(0, 8), -3); Decide(p);
            Assert.That(-Physics2D.gravity.y * p.Context.Motor.Body.gravityScale, Is.EqualTo(60).Within(.001),
                "闩锁结束后跟随当前JumpHeld，不能永久封锁暂停后的新输入");
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchTail_WindCompositionAndExpiry(bool side)
        {
            foreach (var windY in new[] { new[] { 0f }, new[] { 3f }, new[] { -3f }, new[] { 3f, -3f }, new[] { 3f, -5f } })
            {
                Keys(); var p = Character(new Vector2(0, 20));
                float wind = 0;
                foreach (float y in windY)
                {
                    wind += y;
                    p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("wind",
                        PlayerExternalMotionMode.AddVelocity, new Vector2(y, y)));
                }
                var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                target.TryLaunch(ExternalOwner(), TailRequest(side, duration: .04f));
                for (int i = 0; i < 2; i++)
                {
                    Simulate(p);
                    Assert.That(p.Context.ExternalMotion.SelfVelocity.y, Is.EqualTo(side ? 10 : 20).Within(.001));
                    Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo((side ? 10 : 20) + wind).Within(.001));
                }
                float before = p.Context.ExternalMotion.SelfVelocity.y;
                Simulate(p);
                Assert.That(p.Context.ExternalMotion.SelfVelocity.y, Is.EqualTo(before - 120 * Step).Within(.001));
                Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(before - 120 * Step + wind).Within(.001));
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject);
            }
        }

        [Test]
        public void LaunchTail_SupportDepartureComposesOnce()
        {
            var support = MovingSupport(); var p = Character(new Vector2(0, 6.15f)); Land(p);
            support.SetVelocity(new Vector2(2, 0));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("wind",
                PlayerExternalMotionMode.AddVelocity, Vector2.right * 3));
            Simulate(p);
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), TailRequest(duration: .08f));
            for (int i = 0; i < 4; i++) Simulate(p);
            Assert.That(p.Context.Support.Source, Is.Null);
            Assert.That(p.Context.Support.InheritedVelocity.x, Is.EqualTo(2));
            Simulate(p);
            Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(5).Within(.001));
            Assert.That(p.Context.ExternalMotion.SelfVelocity.y, Is.EqualTo(20 - 120 * Step).Within(.001));
        }

        [TestCase(-1)] [TestCase(1)]
        public void LaunchTail_P02InertiaCurveMatchesLegacyHoldAndFreeAir(int sign)
        {
            LaunchInertia();
            var candidate = Character(new Vector2(0, 100), new Vector2(sign * 10, 0));
            var baseline = Character(new Vector2(50, 100), new Vector2(sign * 10, 0));
            foreach (var p in new[] { candidate, baseline })
            {
                foreach (float x in new[] { 3f, -5f })
                    p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("wind",
                        PlayerExternalMotionMode.AddVelocity, Vector2.right * x));
                var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                target.TryLaunch(ExternalOwner(), p == candidate ? TailRequest() : VerticalLaunch());
            }
            for (int i = 0; i < 70; i++)
            {
                Keys(i < 10 ? new[] { sign > 0 ? Key.A : Key.D } : i < 30 ? Array.Empty<Key>() : new[] { sign > 0 ? Key.D : Key.A });
                Decide(candidate); Decide(baseline);
                _scene.GetPhysicsScene2D().Simulate(Step);
                Assert.That(candidate.Context.ExternalMotion.SelfVelocity.x,
                    Is.EqualTo(baseline.Context.ExternalMotion.SelfVelocity.x).Within(.00001), "self X step " + i);
                Assert.That(candidate.Context.Motor.Velocity.x,
                    Is.EqualTo(baseline.Context.Motor.Velocity.x).Within(.00001), "composed X step " + i);
            }
        }

        [Test]
        public void LaunchTail_CeilingHandsOffActualVelocityWithoutRepush()
        {
            WallBox("Tail ceiling", new Vector2(0, 8), new Vector2(10, .5f));
            var p = Character(new Vector2(0, 5));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), TailRequest(duration: 2));
            for (int i = 0; i < 40 && target.IsLaunching; i++) Simulate(p);
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.VerticalMotion.LaunchTail?.IsActive, Is.True);
            for (int i = 0; i < 5; i++)
            {
                Simulate(p);
                Assert.That(p.Context.Motor.Velocity.y, Is.LessThanOrEqualTo(0));
                Assert.That(p.Context.Motor.Position.y, Is.LessThan(7));
            }
        }

        [Test]
        public void LaunchTail_RejectionReplacementOwnerAndSnapshot()
        {
            var p = Character(new Vector2(0, 8)); var owner = ExternalOwner();
            var old = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("old",
                PlayerExternalMotionMode.ForcedRise, Vector2.up * 20, Step, launchTail: TailSpec()));
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            Simulate(p); var first = old.LaunchTail;
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), TailRequest(interrupt: LaunchInterrupt2D.RejectWhileBusy)),
                Is.EqualTo(LaunchResult2D.Busy));
            Assert.That(first.IsActive, Is.True);
            var nextOwner = ExternalOwner();
            target.TryLaunch(nextOwner, TailRequest(duration: Step));
            owner.enabled = false; old.Dispose(); Simulate(p);
            var next = p.Context.VerticalMotion.LaunchTail;
            Assert.That(first.IsActive, Is.False);
            Assert.That(next.IsActive, Is.True);
            Assert.That(next.Generation, Is.GreaterThan(first.Generation));
            first.Dispose(); Assert.That(next.IsActive, Is.True);
            UnityEngine.Object.DestroyImmediate(nextOwner); Decide(p);
            Assert.That(next.IsActive, Is.False);
            Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null);
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchTail_LifeClearsWithoutResurrection(bool lethal)
        {
            WithThreeHit((p, scheme) =>
            {
                var tail = BeginTestTail(p);
                p.Context.HitReceiver.Receive(TestHit(damage: lethal ? 20 : 1)); Decide(p);
                Assert.That(tail.IsActive, Is.False);
                Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null);
            }, scheme => InstallLifeTest(scheme));
        }

        [Test]
        public void LaunchTail_ZoneCopyFrozenAtAcceptanceAndHostExit()
        {
            var p = Character(new Vector2(0, 8)); p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            var zone = Keep(new GameObject("Tail zone")).AddComponent<LaunchZone2D>();
            string json = "{\"_sourceId\":\"frozen\",\"_velocity\":{\"y\":20},\"_mode\":1,\"_duration\":0.02,\"_horizontal\":1,\"_tail\":" + JsonUtility.ToJson(TailSpec()) + "}";
            JsonUtility.FromJsonOverwrite(json, zone);
            var copy = Keep(UnityEngine.Object.Instantiate(zone.gameObject)).GetComponent<LaunchZone2D>();
            Assert.That(copy.TryTrigger(p.GetComponent<Collider2D>()), Is.True);
            JsonUtility.FromJsonOverwrite("{\"_tail\":{\"_enabled\":false}}", copy);
            Simulate(p); var tail = p.Context.VerticalMotion.LaunchTail;
            Assert.That(tail.IsActive, Is.True);
            Simulate(p); Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(17.6f).Within(.001));
            p.Stop(); Assert.That(tail.IsActive, Is.False);
        }

        [Test]
        public void LaunchTail_SameStepReplacementPauseAndDisableDuringHold()
        {
            var p = Character(new Vector2(0, 8));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            var oldOwner = ExternalOwner(); var nextOwner = ExternalOwner();
            target.TryLaunch(oldOwner, TailRequest());
            target.TryLaunch(nextOwner, TailRequest(side: true, duration: .04f));
            oldOwner.enabled = false;
            p.Pause();
            for (int i = 0; i < 10; i++) Decide(p);
            Assert.That(target.IsLaunching, Is.True);
            Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null);
            p.Resume(); Simulate(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(10));
            nextOwner.enabled = false; Decide(p);
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null, "无效所有者不得交接尾段");
            nextOwner.enabled = true; Decide(p);
            Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null, "重新启用不复活旧请求");
            target.TryLaunch(nextOwner, TailRequest(duration: Step));
            Simulate(p); var tail = p.Context.VerticalMotion.LaunchTail;
            target.enabled = false;
            Assert.That(tail.IsActive, Is.False);
        }

        [Test]
        public void LaunchTail_UnlatchedPolicyFollowsJumpHeldAcrossApex()
        {
            var p = Character(new Vector2(0, 8));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("unlatched",
                PlayerExternalMotionMode.ForcedRise, Vector2.up * 10, Step,
                launchTail: new LaunchTailSpec2D(90, 120, 20, true, 5, 45)));
            Simulate(p);
            foreach (float y in new[] { 6f, 5f, 0f, -5f, -6f })
            foreach (bool held in new[] { false, true })
            {
                Keys(); if (held) Keys(Key.Space);
                Place(p, new Vector2(0, 8), y); Decide(p);
                float expected = held && Mathf.Abs(y) <= 5 ? 45 : y < 0 ? 120 : 90;
                Assert.That(-Physics2D.gravity.y * p.Context.Motor.Body.gravityScale, Is.EqualTo(expected).Within(.001));
                Assert.That(p.Context.VerticalMotion.LaunchTail.SlowPermissionLatched, Is.False);
            }
        }

        [Test]
        public void LaunchTail_PauseRetainsLatchAndWindOwnerDoesNotOwnTail()
        {
            var p = Character(new Vector2(0, 8));
            var tail = BeginTestTail(p);
            Place(p, new Vector2(0, 8), 3); Decide(p);
            var owner = ExternalOwner();
            var wind = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("late-wind",
                PlayerExternalMotionMode.AddVelocity, new Vector2(2, 3)));
            Simulate(p);
            Assert.That(tail.IsActive && tail.SlowPermissionLatched, Is.True);
            p.Pause();
            for (int i = 0; i < 10; i++) Decide(p);
            Assert.That(tail.IsActive && tail.SlowPermissionLatched, Is.True);
            p.Resume(); Keys(); Decide(p);
            Assert.That(-Physics2D.gravity.y * p.Context.Motor.Body.gravityScale, Is.EqualTo(60).Within(.001));
            owner.enabled = false; Decide(p); wind.Dispose();
            Assert.That(tail.IsActive, Is.True);
        }

        [Test]
        public void LaunchTail_OnlyAppliedRiseCanHandoffAndLegacyReplacementClearsIt()
        {
            var p = Character(new Vector2(0, 8));
            var old = p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("losing",
                PlayerExternalMotionMode.ForcedRise, Vector2.up * 20, Step, priority: 1, launchTail: TailSpec()));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("winning",
                PlayerExternalMotionMode.ForcedRise, Vector2.up * 10, Step, priority: 2));
            Simulate(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(10));
            Assert.That(old.LaunchTail, Is.Null);
            Assert.That(p.Context.VerticalMotion.LaunchTail, Is.Null);
            var tail = BeginTestTail(p);
            var legacy = p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("legacy",
                PlayerExternalMotionMode.InitialVelocity, Vector2.up * 7));
            Assert.That(tail.IsActive, Is.False);
            tail.Dispose(); Simulate(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(7 + Physics2D.gravity.y * 3 * Step).Within(.001));
            Assert.That(legacy.LaunchTail, Is.Null);
        }

        [UnityTest]
        public IEnumerator LaunchTail_RenderRateAndFixedStepTraces()
        {
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 30, 120, 240 })
                foreach (float step in new[] { .02f, 1f / 60 })
                {
                    Application.targetFrameRate = rate;
                    Keys(); var p = Character(new Vector2(0, 8));
                    var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                    target.TryLaunch(ExternalOwner(), TailRequest());
                    float accumulator = 0, real = 0, physics = 0; int frames = 0; bool landed = false;
                    while (!landed && frames < 1000)
                    {
                        yield return null;
                        float frame = Time.unscaledDeltaTime; frames++; real += frame;
                        Keys(); p.Tick(frame); accumulator += frame;
                        while (accumulator >= step && !landed)
                        {
                            bool holding = target.IsLaunching;
                            float before = p.Context.ExternalMotion.SelfVelocity.y;
                            p.FixedTick(step);
                            if (!holding && p.Context.VerticalMotion.LaunchTail != null)
                                Assert.That(p.Context.Motor.Body.gravityScale * -Physics2D.gravity.y,
                                    Is.EqualTo(Mathf.Min(Mathf.Abs(before) <= 5 ? 60 : 120, Mathf.Max(0, (before + 20) / step))).Within(.002));
                            _scene.GetPhysicsScene2D().Simulate(step); accumulator -= step; physics += step;
                            TestContext.WriteLine($"P06 input=synthetic-keyboard targetFPS={rate} dt={step:R} t={physics:R} hold={holding} p={p.Context.Motor.Position.ToString("F6")} self={p.Context.ExternalMotion.SelfVelocity.ToString("F6")}");
                            landed = physics > .3f && p.Context.JumpWindow.HasGroundSupport && p.Context.VerticalMotion.LaunchTail == null;
                        }
                    }
                    TestContext.WriteLine($"P06 targetFPS={rate} actualMeanFPS={frames/real:R} dt={step:R} frames={frames} seconds={real:R}");
                    Assert.That(landed, Is.True);
                    p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject);
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync; }
        }
    }
}
