using System.Collections;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private CompositeCollider2D PrecisionSupport(float halfWidth = 1, float rise = .000029f)
        {
            foreach (var root in _scene.GetRootGameObjects())
                if (root.name == "Floor") root.SetActive(false);
            var go = Keep(new GameObject("Precision composite support"));
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var polygon = go.AddComponent<PolygonCollider2D>();
            polygon.points = new[] { new Vector2(-halfWidth, -1), new Vector2(halfWidth, -1),
                new Vector2(halfWidth, 0), new Vector2(-halfWidth, -rise * halfWidth) };
            polygon.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite.GenerateGeometry();
            var points = new Vector2[composite.GetPathPointCount(0)]; composite.GetPath(0, points);
            TestContext.WriteLine("composite=" + string.Join(";", System.Array.ConvertAll(points, p => p.ToString("R"))));
            Physics2D.SyncTransforms();
            return composite;
        }

        [TestCase(.000029f)] [TestCase(.2f)] [Category("GroundSupportRegression")]
        public void GroundSupport_PrecisionSlopeRetainsOpportunityAcrossMovementPhases(float rise)
        {
            PrecisionSupport(20, rise); SetMoveFeel("_accelerated", true);
            var player = Character(); Land(player); for (int i = 0; i < 10; i++) Simulate(player);
            int positive = 0, legacyRejected = 0;
            for (int i = 0; i < 100; i++)
            {
                if (i < 10 || i >= 35 && i < 50) Keys();
                else if (i < 35 || i >= 75) Keys(Key.D);
                else Keys(Key.A);
                Simulate(player);
                if (player.Context.Motor.Velocity.y > .000001f) positive++;
                if (player.Context.Sensors.IsGrounded && player.Context.JumpWindow.Contact.ActorVelocity.y > 0)
                    legacyRejected++; // 原实现 grounded && worldY <= 0 在相同物理样本上会拒绝。
                TestContext.WriteLine($"{i}\t{player.Context.Motor.Velocity.ToString("R")}\t{player.Context.JumpWindow.SupportReason}\t{player.Context.JumpWindow.Contact.Normal.ToString("R")}\t{player.Context.JumpWindow.Contact.SeparationSpeed:R}");
                Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True, "step=" + i);
                Assert.That(player.Context.JumpWindow.CanJump, Is.True, "step=" + i);
            }
            Assert.That(positive, Is.GreaterThan(5), "夹具必须真正产生连续正向 Y 物理解算");
            Assert.That(legacyRejected, Is.GreaterThan(5));
            TestContext.WriteLine($"positiveSamples={positive}; legacyRejectedSamples={legacyRejected}");
        }

        [Test, Category("GroundSupportRegression")]
        public void GroundSupport_ContactWindowSeparatesNoiseSlopeLaunchAndCoyote()
        {
            var ground = PrecisionSupport();
            var w = new PlayerJumpWindow(.08f);
            EntityGroundContact2D Fact(Vector2 v, Vector2 n, Vector2 surface = default)
                => new(ground, Vector2.zero, n, v, surface);
            Assert.That(w.Observe(Fact(Vector2.zero, Vector2.up), Step), Is.True);
            for (int i = 0; i < 10; i++)
            {
                Assert.That(w.Observe(Fact(new Vector2(7.2f, .000104725361f), Vector2.up), Step), Is.False);
                Assert.That(w.CanJump && w.HasGroundSupport, Is.True);
            }
            // 真正斜面与上升载具：世界 Y 可远大于容差，相对法向速度才是判据。
            Assert.That(w.Observe(Fact(new Vector2(5, 2), new Vector2(-2, 5)), Step), Is.False);
            Assert.That(w.HasGroundSupport, Is.True);
            Assert.That(w.Observe(Fact(Vector2.up * 3, Vector2.up, Vector2.up * 3), Step), Is.False);
            Assert.That(w.HasGroundSupport, Is.True);
            w.Consume();
            for (int i = 0; i < 10; i++)
            {
                w.Observe(Fact(Vector2.up * .0001f, Vector2.up), Step);
                Assert.That(w.CanJump || w.HasGroundSupport, Is.False);
            }
            w.Observe(default(EntityGroundContact2D), Step);
            Assert.That(w.Observe(Fact(Vector2.up * .01f, Vector2.up), Step), Is.False);
            Assert.That(w.SupportReason, Is.EqualTo(PlayerGroundSupportReason.Separating));
            Assert.That(w.Observe(Fact(Vector2.zero, Vector2.up), Step), Is.True);
            for (int i = 0; i < 4; i++) w.Observe(default(EntityGroundContact2D), Step);
            Assert.That(w.CanJump, Is.False);
        }

        [Test, Category("GroundSupportRegression")]
        public void GroundSupport_ProbeWithoutContactCannotLandOrRefill()
        {
            InstallPowers();
            var p = Character(new Vector2(0, .93f), Vector2.zero);
            var power = Power(p); power.TryCommit(power.CreateAttempt());
            Decide(p);
            Assert.That(p.Context.Sensors.IsGrounded, Is.True);
            Assert.That(p.Context.Sensors.GroundContact.HasContact, Is.False);
            Assert.That(p.Context.JumpWindow.HasGroundSupport || p.Context.JumpWindow.CanJump, Is.False);
            Assert.That(power.RemainingUses, Is.EqualTo(1));
            Assert.That(p.Context.Landing.Pending, Is.False);
        }

        [Test, Category("GroundSupportRegression")]
        public void GroundSupport_SpentResourcesStaySpentAcrossSlopeAndConsumedContact()
        {
            PrecisionSupport(20); InstallPowers();
            var p = Character(); Land(p);
            var power = Power(p); power.TryCommit(power.CreateAttempt());
            Keys(Key.D);
            for (int i = 0; i < 15; i++) { Simulate(p); Assert.That(power.RemainingUses, Is.EqualTo(1)); }
            Keys(Key.D, Key.Space); Decide(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("jump"));
            for (int i = 0; i < 8; i++)
            {
                Decide(p); // 尚未推进物理，真实接触缓存也仍在；不得重授。
                Assert.That(p.Context.JumpWindow.HasGroundSupport || p.Context.JumpWindow.CanJump, Is.False);
                Assert.That(power.RemainingUses, Is.EqualTo(1));
            }
        }

        [TestCase(PlayerExternalMotionMode.InitialVelocity)]
        [TestCase(PlayerExternalMotionMode.ForcedRise)]
        [Category("GroundSupportRegression")]
        public void GroundSupport_ExplicitLaunchBelowToleranceStillConsumes(PlayerExternalMotionMode mode)
        {
            var p = Character(); Land(p);
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("small launch",
                mode, Vector2.up * .0001f, mode == PlayerExternalMotionMode.ForcedRise ? .2f : 0));
            Decide(p);
            for (int i = 0; i < 5; i++)
            {
                Decide(p);
                Assert.That(p.Context.JumpWindow.HasGroundSupport || p.Context.JumpWindow.CanJump, Is.False);
                Assert.That(p.Context.Landing.Pending, Is.False);
            }
        }

        [TestCase(false)] [TestCase(true)] [Category("GroundSupportRegression")]
        public void GroundSupport_CarrierDescentUsesPreviousSolvedSurfaceVelocity(bool oneWay)
            => Carrier_DescentRecoverySeparatesSubmissionAndPhysics(-6.4f, .02f, false, oneWay, true);

        [Test, Category("GroundSupportRegression")]
        public void GroundSupport_PhysicalEdgeDepartureExpiresWithoutNewGround()
        {
            PrecisionSupport(); var p = Character(); Land(p); Keys(Key.D);
            bool left = false; int airSteps = 0;
            for (int i = 0; i < 35; i++)
            {
                Simulate(p);
                if (!p.Context.Sensors.GroundContact.HasContact) { left = true; airSteps++; }
                if (airSteps >= 6) break;
            }
            Assert.That(left && airSteps >= 6, Is.True);
            Keys(Key.Space); Decide(p);
            Assert.That(p.Context.JumpWindow.CanJump, Is.False);
            Assert.That(p.CurrentStateId, Is.EqualTo("fall"));
        }

        [UnityTest, Category("GroundSupportRegression")]
        public IEnumerator GroundSupport_PrecisionPlatformRenderRateJumpMatrix()
        {
            int oldRate = Application.targetFrameRate, oldSync = QualitySettings.vSyncCount;
            var platform = PrecisionSupport();
            SetMoveFeel("_accelerated", true);
            try
            {
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 120, 30 })
                foreach (int direction in new[] { 1, -1 })
                foreach (bool braking in new[] { false, true })
                {
                    Application.targetFrameRate = rate; Keys();
                    var p = Character(new Vector2(-direction * .5f, .9f)); Land(p);
                    for (int i = 0; i < 5; i++) Simulate(p);
                    float accumulator = 0, elapsed = 0, startY = p.Context.Motor.Position.y, peakY = startY;
                    int frames = 0, ticks = 0, airTicks = 0, jumpEntries = 0;
                    bool released = false, pressed = false; string previous = p.CurrentStateId;
                    while (airTicks < 10 && frames < 300)
                    {
                        yield return null;
                        float dt = Time.unscaledDeltaTime; elapsed += dt; frames++; accumulator += dt;
                        // 每渲染帧只采样一次输入；一个慢帧可以有多个固定步。
                        // 松键后的下一渲染帧不保证已有完成的减速物理步，等待实际阶段再触发。
                        bool press = !pressed && ticks >= 10 &&
                            (!braking || released && p.CurrentStateId == "decelerate");
                        if (press)
                        {
                            Assert.That(p.Context.JumpWindow.CanJump, Is.True);
                            Assert.That(p.CurrentStateId, Is.EqualTo(braking ? "decelerate" : "move"));
                            pressed = true;
                        }
                        if (braking && ticks >= 9) released = true;
                        if (released) { if (press) Keys(Key.Space); else Keys(); }
                        else if (press) Keys(direction > 0 ? Key.D : Key.A, Key.Space);
                        else Keys(direction > 0 ? Key.D : Key.A);
                        p.Tick(dt);
                        while (accumulator >= Step && airTicks < 10)
                        {
                            p.FixedTick(Step); _scene.GetPhysicsScene2D().Simulate(Step);
                            peakY = Mathf.Max(peakY, p.Context.Motor.Position.y);
                            if (p.CurrentStateId == "jump" && previous != "jump") jumpEntries++;
                            previous = p.CurrentStateId;
                            if (pressed)
                            {
                                Assert.That(p.Context.JumpWindow.HasGroundSupport || p.Context.JumpWindow.CanJump, Is.False);
                                airTicks++;
                            }
                            accumulator -= Step; ticks++;
                        }
                    }
                    Assert.That(airTicks, Is.EqualTo(10)); Assert.That(jumpEntries, Is.EqualTo(1));
                    Assert.That(peakY, Is.GreaterThan(startY + .2f));
                    Assert.That(p.Context.Input.JumpRequested, Is.False);
                    TestContext.WriteLine($"target={rate} actualMeanFPS={frames / elapsed:R} frames={frames} seconds={elapsed:R} direction={direction} braking={braking} jumps={jumpEntries}");
                    p.Stop(); Object.DestroyImmediate(p.gameObject); Keys();
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldSync; }
        }
    }
}
