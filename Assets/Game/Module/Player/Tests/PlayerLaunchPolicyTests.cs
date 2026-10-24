using System;
using System.Collections;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private LaunchRequest2D VerticalLaunch(float y = 20, float duration = 13f / 60,
            LaunchEnd2D end = LaunchEnd2D.ContinueVelocity,
            LaunchWallQualification2D wall = LaunchWallQualification2D.Preserve)
            => new("vertical-policy", new Vector2(999, y), LaunchMode2D.ConstantRise, duration, end,
                horizontal: LaunchHorizontal2D.PreserveControl, wallQualification: wall);

        private void LaunchInertia()
        {
            var movement = _config.Feel.Movement;
            foreach (var pair in new (string name, object value)[]
            {
                ("_speed", 10f), ("_independentAir", true), ("_airSpeed", 10f),
                ("_airAccelerated", true), ("_airAcceleration", 40f), ("_airBraking", 20f), ("_airTurning", 60f)
            })
                typeof(PlayerMoveFeel).GetField(pair.name, BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(movement, pair.value);
        }

        [Test]
        public void LaunchPolicy_ValidationAndDefaults()
        {
            var legacy = new LaunchRequest2D("legacy", Vector2.up);
            Assert.That(legacy.Horizontal, Is.EqualTo(LaunchHorizontal2D.HoldRequestVelocity));
            Assert.That(legacy.WallQualification, Is.EqualTo(LaunchWallQualification2D.Preserve));
            Assert.Throws<ArgumentException>(() => new LaunchRequest2D("invalid", Vector2.up,
                horizontal: LaunchHorizontal2D.PreserveControl));
            Assert.Throws<ArgumentException>(() => new LaunchRequest2D("invalid", Vector2.up,
                wallQualification: (LaunchWallQualification2D)99));
            Assert.Throws<ArgumentException>(() => new PlayerExternalMotionRequest("invalid",
                PlayerExternalMotionMode.AddVelocity, Vector2.up, preserveHorizontalControl: true));
        }

        [TestCase(-1, false, 20)] [TestCase(1, false, 20)]
        [TestCase(-1, true, 20)] [TestCase(1, true, 20)]
        [TestCase(-1, false, 15)] [TestCase(1, false, 15)]
        [TestCase(-1, true, 15)] [TestCase(1, true, 15)]
        public void LaunchPolicy_HorizontalCurveMatchesOrdinaryAirWithWind(int sign, bool release, float y)
        {
            LaunchInertia();
            foreach (var winds in new[] { new[] { 0f }, new[] { 3f }, new[] { -3f }, new[] { 3f, -5f } })
            {
                Keys();
                var p = Character(new Vector2(0, 30), new Vector2(sign * 10, 0));
                var baseline = Character(new Vector2(50, 30), new Vector2(sign * 10, 0));
                float wind = 0;
                foreach (float value in winds)
                {
                    wind += value;
                    p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest(
                        "wind", PlayerExternalMotionMode.AddVelocity, new Vector2(value, 0)));
                }
                var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                Assert.That(target.TryLaunch(ExternalOwner(), VerticalLaunch(y)), Is.EqualTo(LaunchResult2D.Accepted));
                Keys(release ? Array.Empty<Key>() : new[] { sign > 0 ? Key.A : Key.D });
                for (int i = 0; i < 11; i++)
                {
                    p.Tick(Step);
                    Assert.That(p.Context.Input.Horizontal, Is.EqualTo(release ? 0 : -sign));
                    p.FixedTick(Step); Decide(baseline);
                    Assert.That(p.Context.Motor.Velocity.x - wind,
                        Is.EqualTo(baseline.Context.Motor.Velocity.x).Within(.001f), $"step={i} wind={wind}");
                    Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(y).Within(.001f));
                    Assert.That(p.Context.Motor.Body.gravityScale, Is.Zero);
                    Assert.That(_scene.GetPhysicsScene2D().Simulate(Step), Is.True);
                    TestContext.WriteLine($"P02 sign={sign} release={release} wind={wind} t={(i+1)*Step:R} x={p.Context.Motor.Velocity.x:R} y={p.Context.Motor.Velocity.y:R}");
                }
                Assert.That(target.IsLaunching, Is.False);
                p.Stop(); baseline.Stop();
                UnityEngine.Object.DestroyImmediate(p.gameObject); UnityEngine.Object.DestroyImmediate(baseline.gameObject);
            }
        }

        [TestCase(LaunchEnd2D.ContinueVelocity)] [TestCase(LaunchEnd2D.StopVertical)]
        public void LaunchPolicy_SupportDepartureAndWindComposeOnce(LaunchEnd2D end)
        {
            var support = MovingSupport();
            var p = Character(new Vector2(0, 6.15f)); Land(p);
            support.SetVelocity(new Vector2(2, 0));
            var wind = p.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("wind", PlayerExternalMotionMode.AddVelocity, Vector2.right * 3));
            Simulate(p);
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), VerticalLaunch(duration: .08f, end: end));
            for (int i = 0; i < 4; i++)
            {
                Simulate(p);
                Assert.That(p.Context.Motor.Velocity, Is.EqualTo(new Vector2(5, 20)));
                Assert.That(wind.IsActive, Is.True);
            }
            Assert.That(p.Context.Support.Source, Is.Null);
            Assert.That(p.Context.Support.InheritedVelocity.x, Is.EqualTo(2));
            Decide(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(end == LaunchEnd2D.StopVertical ? 0 : 20).Within(.001f));
            Assert.That(p.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
        }

        [TestCase(-1, false)] [TestCase(1, false)] [TestCase(-1, true)] [TestCase(1, true)]
        public void LaunchPolicy_RefreshBeforeAcceptedThenRealWallJump(int wall, bool side)
        {
            WallContract();
            WallBox("Wall", new Vector2(wall * 1.02f, 8), new Vector2(1, 24));
            var p = Character(new Vector2(0, 5), Vector2.down);
            Keys(Key.Space, wall > 0 ? Key.D : Key.A); Simulate(p);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.False);
            Keys(); Simulate(p);
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            int accepted = 0; bool callbackSawQualification = false, callbackSawLaunch = false;
            target.Accepted += _ =>
            {
                accepted++;
                callbackSawQualification = p.Context.Wall.VerticalAvailable;
                callbackSawLaunch = target.IsLaunching;
            };
            var request = new LaunchRequest2D("refresh", new Vector2(side ? wall * 3 : 0, 20),
                LaunchMode2D.ConstantRise, .04f, LaunchEnd2D.StopVertical,
                wallQualification: LaunchWallQualification2D.RefreshFirstVertical);
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                Keys(); Simulate(p);
                Assert.That(target.TryLaunch(ExternalOwner(), request), Is.EqualTo(LaunchResult2D.Accepted));
                Assert.That(accepted, Is.EqualTo(cycle));
                Assert.That(callbackSawQualification && callbackSawLaunch, Is.True);
                for (int i = 0; i < 4; i++) Simulate(p);
                Assert.That(p.Context.JumpWindow.HasGroundSupport, Is.False);
                Keys(Key.Space, wall > 0 ? Key.D : Key.A); Decide(p);
                Assert.That(p.Context.Wall.VerticalBranch, Is.True);
                Assert.That(p.Context.Motor.Velocity.x, Is.Zero);
                Assert.That(p.Context.Wall.VerticalAvailable, Is.False);
                TestContext.WriteLine($"P03 wall={wall} side={side} cycle={cycle} accepted={accepted} x={p.Context.Motor.Velocity.x:R} y={p.Context.Motor.Velocity.y:R} grounded={p.Context.JumpWindow.HasGroundSupport}");
            }
        }

        [Test]
        public void LaunchPolicy_RejectionReplacementAndOwnerCleanup()
        {
            WallContract(); var p = AtRightWall();
            Keys(Key.Space, Key.D); Simulate(p); Keys(); Simulate(p);
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            var owner = ExternalOwner(); int accepted = 0;
            target.Accepted += _ => accepted++;
            var refresh = VerticalLaunch(wall: LaunchWallQualification2D.RefreshFirstVertical);
            Assert.That(target.TryLaunch(null, refresh), Is.EqualTo(LaunchResult2D.Inactive));
            var destroyed = ExternalOwner(); UnityEngine.Object.DestroyImmediate(destroyed);
            Assert.That(target.TryLaunch(destroyed, refresh), Is.EqualTo(LaunchResult2D.Inactive));
            owner.enabled = false;
            Assert.That(target.TryLaunch(owner, refresh), Is.EqualTo(LaunchResult2D.Inactive));
            owner.enabled = true;
            target.TryLaunch(owner, VerticalLaunch());
            var busy = new LaunchRequest2D("busy", Vector2.up * 20, LaunchMode2D.ConstantRise, .2f,
                interrupt: LaunchInterrupt2D.RejectWhileBusy,
                wallQualification: LaunchWallQualification2D.RefreshFirstVertical);
            Assert.That(target.TryLaunch(owner, busy), Is.EqualTo(LaunchResult2D.Busy));
            Assert.That(p.Context.Wall.VerticalAvailable, Is.False);
            Assert.That(accepted, Is.EqualTo(1));
            var next = ExternalOwner();
            Assert.That(target.TryLaunch(next, refresh), Is.EqualTo(LaunchResult2D.Accepted));
            owner.enabled = false; Decide(p);
            Assert.That(target.IsLaunching, Is.True);
            Assert.That(p.Context.Wall.VerticalAvailable, Is.True);
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            p.Pause();
            for (int i = 0; i < 20; i++) Decide(p);
            Assert.That(target.IsLaunching, Is.True);
            p.Resume(); Keys(Key.A); Decide(p);
            Assert.That(p.Context.Input.Horizontal, Is.EqualTo(-1));
            UnityEngine.Object.DestroyImmediate(next); Decide(p);
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
            Assert.That(accepted, Is.EqualTo(2));
        }

        [Test]
        public void LaunchPolicy_CarrierKeepsHorizontalDriverAndRejectsInvalidOwner()
        {
            var support = MovingSupport(); support.SetVelocity(new Vector2(7, 0));
            var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            var owner = ExternalOwner();
            Assert.That(target.TryLaunch(owner, VerticalLaunch()), Is.EqualTo(LaunchResult2D.Accepted));
            Assert.That(support.Velocity, Is.EqualTo(new Vector2(7, 20)));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(target.TrySetHorizontalVelocity(7 - i * 2), Is.True);
                Assert.That(support.Velocity, Is.EqualTo(new Vector2(7 - i * 2, 20)));
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
            owner.enabled = false;
            Assert.That(target.TrySetHorizontalVelocity(100), Is.False);
            Assert.That(target.TryLaunch(owner, VerticalLaunch()), Is.EqualTo(LaunchResult2D.Inactive));
        }

        [Test]
        public void LaunchPolicy_SameStepOrderAndActionInputDoNotLeak()
        {
            var p = Character(new Vector2(0, 30));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            int count = 0; target.Accepted += _ => count++;
            var oldOwner = ExternalOwner(); var newOwner = ExternalOwner();
            target.TryLaunch(oldOwner, VerticalLaunch(y: 20));
            target.TryLaunch(newOwner, VerticalLaunch(y: 15, duration: .04f, end: LaunchEnd2D.StopVertical));
            oldOwner.enabled = false;
            Keys(Key.D, Key.Space, Key.LeftShift, Key.J); Decide(p);
            Assert.That(p.Context.Input.Horizontal, Is.EqualTo(1));
            Assert.That(p.Context.Input.JumpHeld, Is.False);
            Assert.That(p.Context.Input.JumpRequested, Is.False);
            Assert.That(p.Context.Input.DashRequested, Is.False);
            Assert.That(p.Context.Input.AttackRequested, Is.False);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(15));
            Decide(p);
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.ExternalMotion.HasTransientMotion, Is.True, "到期停速尚未结算时不能采集快照");
            Decide(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.Zero);
            Assert.That(count, Is.EqualTo(2));
        }

        [TestCase(-1)] [TestCase(1)]
        public void LaunchPolicy_FirstFallingStepTowardWallDoesNotEnterSlide(int wall)
        {
            WallContract();
            WallBox("Wall", new Vector2(wall * 1.02f, 8), new Vector2(1, 24));
            var p = Character(new Vector2(0, 5), Vector2.down);
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), VerticalLaunch()), Is.EqualTo(LaunchResult2D.Accepted));
            Keys(wall > 0 ? Key.D : Key.A); Decide(p);
            Assert.That(p.Context.Wall.Sliding, Is.False);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(20));
            Assert.That(p.Context.Input.Horizontal, Is.EqualTo(wall));
        }

        [Test]
        public void LaunchPolicy_CeilingAndHostExitReleaseControl()
        {
            WallBox("Ceiling", new Vector2(0, 8), new Vector2(10, .5f));
            var p = Character(new Vector2(0, 5));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), VerticalLaunch(duration: 2));
            for (int i = 0; i < 30 && target.IsLaunching; i++) Simulate(p);
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.Motor.Position.y, Is.LessThan(7));
            Keys(Key.D); Decide(p);
            Assert.That(p.Context.Input.Horizontal, Is.EqualTo(1));
            target.TryLaunch(ExternalOwner(), VerticalLaunch());
            p.Stop();
            Assert.That(target.IsLaunching, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchPolicy_DeathAndReactionClearVerticalPolicy(bool lethal)
        {
            WithThreeHit((p, scheme) =>
            {
                var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                target.TryLaunch(ExternalOwner(), VerticalLaunch());
                Simulate(p);
                p.Context.HitReceiver.Receive(TestHit(damage: lethal ? 20 : 1)); Decide(p);
                Assert.That(target.IsLaunching, Is.False);
            }, scheme => InstallLifeTest(scheme));
        }

        [UnityTest]
        public IEnumerator LaunchPolicy_CarrierExpiryUsesRealFixedUpdates()
        {
            var support = MovingSupport(); support.SetVelocity(new Vector2(-3, 0));
            var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), VerticalLaunch(duration: .04f, end: LaunchEnd2D.StopVertical));
            for (int i = 0; i < 10 && target.IsLaunching; i++) yield return new WaitForFixedUpdate();
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(support.Velocity, Is.EqualTo(new Vector2(-3, 0)));
            Assert.That(target.TrySetHorizontalVelocity(4), Is.False);
        }

        [Test]
        public void LaunchPolicy_ZoneConfigurationIsCopiedAtAcceptance()
        {
            var p = Character(new Vector2(0, 30));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            var zone = Keep(new GameObject("Configured launch zone")).AddComponent<LaunchZone2D>();
            JsonUtility.FromJsonOverwrite("{\"_sourceId\":\"configured\",\"_velocity\":{\"x\":99,\"y\":20},\"_mode\":1,\"_duration\":0.08,\"_horizontal\":1,\"_wallQualification\":1}", zone);
            int count = 0; target.Accepted += request =>
            {
                count++;
                Assert.That(request.Horizontal, Is.EqualTo(LaunchHorizontal2D.PreserveControl));
                Assert.That(request.WallQualification, Is.EqualTo(LaunchWallQualification2D.RefreshFirstVertical));
            };
            Assert.That(zone.TryTrigger(p.GetComponent<Collider2D>()), Is.True);
            JsonUtility.FromJsonOverwrite("{\"_velocity\":{\"x\":88,\"y\":15},\"_horizontal\":0,\"_wallQualification\":0}", zone);
            Keys(Key.D); Decide(p);
            Assert.That(p.Context.Motor.Velocity, Is.EqualTo(new Vector2(5, 20)));
            Assert.That(count, Is.EqualTo(1));
            Assert.That(zone.TryTrigger(p.GetComponent<Collider2D>()), Is.False);
        }

        [UnityTest]
        public IEnumerator LaunchPolicy_CarrierPauseReplacementAndOwnerLifecycleMatrix()
        {
            float oldScale = Time.timeScale;
            try
            {
                foreach (float y in new[] { 15f, 20f })
                foreach (var end in new[] { LaunchEnd2D.ContinueVelocity, LaunchEnd2D.StopVertical })
                {
                    var support = MovingSupport(); support.SetVelocity(new Vector2(-3, 0));
                    var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
                    var oldOwner = ExternalOwner(); var nextOwner = ExternalOwner();
                    Assert.That(target.TryLaunch(oldOwner, VerticalLaunch(y, .08f, end)), Is.EqualTo(LaunchResult2D.Accepted));
                    Time.timeScale = 0;
                    for (int i = 0; i < 3; i++) yield return null;
                    Assert.That(target.IsLaunching, Is.True);
                    Assert.That(target.TryLaunch(nextOwner, new LaunchRequest2D("busy", Vector2.up * y,
                        LaunchMode2D.ConstantRise, .08f, interrupt: LaunchInterrupt2D.RejectWhileBusy)), Is.EqualTo(LaunchResult2D.Busy));
                    Assert.That(target.TryLaunch(nextOwner, VerticalLaunch(y, .08f, end)), Is.EqualTo(LaunchResult2D.Accepted));
                    oldOwner.enabled = false;
                    Assert.That(target.TrySetHorizontalVelocity(-3 + 2 - 4), Is.True, "原有水平驱动已合成两路风");
                    Assert.That(support.Velocity, Is.EqualTo(new Vector2(-5, y)));
                    Time.timeScale = oldScale;
                    yield return new WaitForFixedUpdate();
                    Assert.That(target.IsLaunching, Is.True, "旧owner不能取消新请求");
                    UnityEngine.Object.DestroyImmediate(nextOwner);
                    yield return new WaitForFixedUpdate();
                    Assert.That(target.IsLaunching, Is.False);
                    Assert.That(support.Velocity, Is.EqualTo(new Vector2(-5, end == LaunchEnd2D.StopVertical ? 0 : y)));
                    TestContext.WriteLine($"CARRIER y={y} end={end} x={support.Velocity.x} ownerReleased=true fixedStep={Time.fixedDeltaTime:R}");
                    UnityEngine.Object.DestroyImmediate(support.gameObject);
                }
            }
            finally { Time.timeScale = oldScale; }
        }

        [UnityTest]
        public IEnumerator LaunchPolicy_RenderRateAndPhysicsStepMatrix()
        {
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                QualitySettings.vSyncCount = 0; LaunchInertia();
                foreach (int rate in new[] { 30, 120, 240 })
                foreach (float step in new[] { .02f, 1f / 60 })
                {
                    Application.targetFrameRate = rate;
                    Keys(); var p = Character(new Vector2(0, 30), new Vector2(10, 0));
                    var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                    target.TryLaunch(ExternalOwner(), VerticalLaunch()); Keys(Key.A);
                    float accumulator = 0, elapsed = 0, physical = 0; int frames = 0, steps = 0;
                    while (physical < .4f && frames < 300)
                    {
                        yield return null;
                        // 每个真实渲染帧重新提交合成设备状态，避免原生输入更新/焦点变化重置虚拟键盘。
                        Keys(Key.A);
                        float frame = Time.unscaledDeltaTime; frames++; elapsed += frame;
                        p.Tick(frame); accumulator += frame;
                        while (accumulator >= step && physical < .4f)
                        {
                            bool held = target.IsLaunching;
                            p.FixedTick(step);
                            if (held) Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(20).Within(.001f));
                            _scene.GetPhysicsScene2D().Simulate(step);
                            accumulator -= step; physical += step; steps++;
                        }
                    }
                    Assert.That(target.IsLaunching, Is.False);
                    Assert.That(p.Context.Motor.Velocity.x, Is.LessThan(0));
                    TestContext.WriteLine($"P02 render requested={rate} observedMeanFPS={frames/elapsed:R} fixedStep={step:R} frames={frames} steps={steps}");
                    p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject);
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync; }
        }
    }
}
