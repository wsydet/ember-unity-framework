using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private PlayerOrdinaryJumpSpec RhythmSpec(float hold = .2f, bool heldApex = true)
            => new(14, .02f, hold, 120, 120, 20, apexEnabled: true, apexRequiresHeld: heldApex);

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator OrdinaryRhythm_RealRenderFramesAndFixedStepMatrix()
        {
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 30, 60, 144 })
                foreach (float step in new[] { .02f, 1f / 60f })
                {
                    Application.targetFrameRate = rate;
                    _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
                    Keys(); var p = Character(); Land(p); Keys(Key.Space);
                    float accumulator = 0, physicsTime = 0, renderTime = 0, peak = p.Context.Motor.Position.y;
                    int frames = 0; bool air = false, landed = false;
                    while (!landed && frames < 500)
                    {
                        yield return null;
                        float frame = Time.unscaledDeltaTime; frames++; renderTime += frame;
                        if (renderTime >= .12f) Keys();
                        p.Tick(frame); accumulator += frame;
                        while (accumulator >= step && !landed)
                        {
                            p.FixedTick(step); _scene.GetPhysicsScene2D().Simulate(step);
                            accumulator -= step; physicsTime += step;
                            peak = Mathf.Max(peak, p.Context.Motor.Position.y);
                            air |= !p.Context.JumpWindow.HasGroundSupport;
                            landed = air && p.Context.JumpWindow.HasGroundSupport
                                && !p.Context.VerticalMotion.OrdinaryActive;
                        }
                    }
                    Assert.That(landed, Is.True); Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
                    TestContext.WriteLine($"render requested={rate} observedMeanFPS={frames/renderTime:R} dt={step:R} frames={frames} air={physicsTime:R} peakY={peak:R}");
                    p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject);
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync; }
        }

        [TestCase(.01f, .04f)] [TestCase(.02f, .04f)] [TestCase(.03333333f, .04f)]
        [TestCase(.01f, .12f)] [TestCase(.02f, .12f)] [TestCase(.03333333f, .12f)]
        [TestCase(.01f, .6f)] [TestCase(.02f, .6f)] [TestCase(.03333333f, .6f)]
        public void OrdinaryRhythm_FullFlightUsesIndependentPolicy(float step, float hold)
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var player = Character(); Land(player);
            for (int i = 0; i < 20; i++) Simulate(player);
            float ground = player.Context.Motor.Position.y;
            Keys(Key.Space); float peak = ground, apex = 0, elapsed = 0;
            bool landed = false, constant = false, falling = false;
            for (int i = 0; i < 300; i++)
            {
                if (elapsed >= hold) Keys();
                player.Tick(step); player.FixedTick(step);
                if (player.Context.VerticalMotion.OrdinaryActive && player.Context.Motor.Body.gravityScale == 0
                    && player.Context.Motor.Velocity.y > 0)
                { constant = true; Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(14)); }
                _scene.GetPhysicsScene2D().Simulate(step); elapsed += step;
                var p = player.Context.Motor.Position;
                TestContext.WriteLine($"TRACE\t{elapsed:R}\t{p.x:R}\t{p.y:R}\t{player.Context.Motor.Velocity.x:R}\t{player.Context.Motor.Velocity.y:R}\t{player.CurrentStateId}");
                if (p.y > peak) { peak = p.y; apex = elapsed; }
                falling |= player.Context.Motor.Velocity.y < 0;
                Assert.That(player.Context.Motor.Velocity.y, Is.GreaterThanOrEqualTo(-20.001f));
                if (falling && player.Context.JumpWindow.HasGroundSupport
                    && player.Context.Sensors.Shape.IsTouching(player.Context.Sensors.Ground)) { landed = true; break; }
            }
            Assert.That(constant, Is.True); Assert.That(landed, Is.True);
            Keys();
            for (int i = 0; i < 20; i++) Simulate(player);
            Assert.That(player.Context.Motor.Position.y, Is.EqualTo(ground).Within(.04f));
            Assert.That(player.Context.VerticalMotion.OrdinaryActive, Is.False);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3));
            TestContext.WriteLine($"ordinary dt={step:R} hold={hold:R} height={peak-ground:R} rise={apex:R} fall={elapsed-apex:R} air={elapsed:R}");
        }

        [TestCase(14f)] [TestCase(10f)]
        public void OrdinaryRhythm_ExternalFlagAndReboundFullTrajectoryUnchanged(float launch)
        {
            InstallActionGraph();
            List<Vector2> Run(bool enabled)
            {
                _config.Feel.Jump.ConfigureOrdinaryJump(enabled ? RhythmSpec() : default);
                Keys(); var p = Character(); Land(p);
                var action = StartAction(p); var stale = action.Lease;
                Assert.That(stale.End(), Is.EqualTo(EmberFsmResult.Changed));
                p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("flag-or-rebound",
                    PlayerExternalMotionMode.InitialVelocity, new Vector2(4, launch)));
                Keys(Key.D); var trace = new List<Vector2>(); bool airborne = false;
                for (int i = 0; i < 200; i++)
                {
                    Simulate(p); trace.Add(p.Context.Motor.Position);
                    Assert.That(stale.SetVertical(99, 0), Is.False);
                    Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
                    airborne |= !p.Context.JumpWindow.HasGroundSupport;
                    if (airborne && p.Context.JumpWindow.HasGroundSupport) break;
                }
                Assert.That(airborne, Is.True); Assert.That(p.Context.JumpWindow.HasGroundSupport, Is.True);
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); Keys(); return trace;
            }
            var baseline = Run(false); var candidate = Run(true);
            Assert.That(candidate.Count, Is.EqualTo(baseline.Count), "完整滞空物理步数");
            for (int i = 0; i < baseline.Count; i++) Assert.That(Vector2.Distance(candidate[i], baseline[i]), Is.LessThan(.00001f), "x/y sample " + i);
            for (int i = 0; i < candidate.Count; i++) TestContext.WriteLine($"TRACE\t{i * Step:R}\t{candidate[i].x:R}\t{candidate[i].y:R}");
            TestContext.WriteLine($"special launch={launch} samples={baseline.Count}; entire x/y trajectory equal within 1e-5");
        }

        [Test]
        public void OrdinaryRhythm_WallJumpFullTrajectoryUnchanged()
        {
            InstallWalls(); WallBox("RhythmWall", new Vector2(1.02f, 5), new Vector2(1, 12));
            List<Vector2> Run(bool enabled)
            {
                _config.Feel.Jump.ConfigureOrdinaryJump(enabled ? RhythmSpec() : default);
                Keys(); var p = Character(new Vector2(0, 5), new Vector2(0, -1));
                Keys(Key.Space); Simulate(p); Assert.That(p.CurrentStateId, Is.EqualTo("wall-jump"));
                Assert.That(p.Context.VerticalMotion.Origin, Is.EqualTo(PlayerFlightOrigin.WallJump));
                Keys(); var trace = new List<Vector2>();
                for (int i = 0; i < 200; i++)
                { Simulate(p); trace.Add(p.Context.Motor.Position); if (p.Context.JumpWindow.HasGroundSupport) break; }
                Assert.That(p.Context.JumpWindow.HasGroundSupport, Is.True);
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); return trace;
            }
            var a = Run(false); var b = Run(true); Assert.That(b.Count, Is.EqualTo(a.Count));
            for (int i = 0; i < a.Count; i++) Assert.That(Vector2.Distance(a[i], b[i]), Is.LessThan(.00001f));
            for (int i = 0; i < b.Count; i++) TestContext.WriteLine($"TRACE\t{i * Step:R}\t{b[i].x:R}\t{b[i].y:R}");
        }

        [Test]
        public void OrdinaryRhythm_OrdinaryToWallJumpAndWallTopClearPolicy()
        {
            InstallWalls(); _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var p = AtRightWall(.9f, 0); Land(p); Keys(Key.Space); Simulate(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True);
            Keys(); Simulate(p); Keys(Key.Space); Decide(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("wall-jump"));
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(3));
            Keys(Key.D); Place(p, new Vector2(0, 12), -1);
            Decide(p);
            Assert.That(p.CurrentStateId, Is.Not.EqualTo("wall-slide"));
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
        }

        [Test]
        public void OrdinaryRhythm_ActionInterruptPauseSnapshotAndFrozenSource()
        {
            InstallActionGraph(); _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var p = Character(); Land(p);
            _config.Feel.Jump.ConfigureOrdinaryJump(new PlayerOrdinaryJumpSpec(40, 0, 1, 10, 10, 50));
            Keys(Key.Space); Simulate(p);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(14));
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            float elapsed = p.Context.VerticalMotion.OrdinaryElapsed;
            p.Pause(); for (int i = 0; i < 10; i++) Decide(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryElapsed, Is.EqualTo(elapsed));
            p.Resume(); Simulate(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
            var action = StartAction(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
            action.Lease.End(); Simulate(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(3));
            var oldContext = p.Context; p.Stop();
            Assert.That(oldContext.VerticalMotion.OrdinaryActive, Is.False);
        }

        [Test]
        public void OrdinaryRhythm_CeilingEndsHoldWithoutRestart()
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec(1));
            var ceiling = Keep(new GameObject("RhythmCeiling")); SceneManager.MoveGameObjectToScene(ceiling, _scene);
            ceiling.transform.position = new Vector3(0, 3);
            ceiling.AddComponent<BoxCollider2D>().size = new Vector2(8, .2f);
            var p = Character(); Land(p); Keys(Key.Space); bool falling = false;
            for (int i = 0; i < 30; i++)
            {
                Simulate(p);
                if (p.Context.Motor.Velocity.y < 0) falling = true;
                if (falling) Assert.That(p.Context.Motor.Velocity.y, Is.LessThanOrEqualTo(0));
            }
            Assert.That(falling, Is.True); Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
        }

        [Test]
        public void OrdinaryRhythm_InvalidValuesRejectedAndValueCopyIndependent()
        {
            Assert.Throws<InvalidOperationException>(() => new PlayerOrdinaryJumpSpec(14, .3f, .2f, 120, 120, 20));
            Assert.Throws<InvalidOperationException>(() => new PlayerOrdinaryJumpSpec(14, 0, .2f, float.NaN, 120, 20));
            Assert.Throws<InvalidOperationException>(() => new PlayerOrdinaryJumpSpec(14, 0, .2f, 120, 120, 20, PlayerOrdinaryJumpRelease.MultiplySpeed, 2));
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            Assert.Throws<InvalidOperationException>(() => _config.Feel.Capture(Vector2.zero));
            var copy = Keep(UnityEngine.Object.Instantiate(_config.Feel.Jump)); copy.ConfigureOrdinaryJump(default);
            Assert.That(_config.Feel.Jump.OrdinaryJump.Enabled, Is.True);
        }

        [TestCase(PlayerOrdinaryJumpRelease.EndHold, 0f, 14f)]
        [TestCase(PlayerOrdinaryJumpRelease.LimitSpeed, 5f, 5f)]
        [TestCase(PlayerOrdinaryJumpRelease.MultiplySpeed, .5f, 7f)]
        public void OrdinaryRhythm_ReleasedBufferedInputAndReleaseModes(PlayerOrdinaryJumpRelease mode, float value, float expected)
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(new PlayerOrdinaryJumpSpec(14, 0, .2f, 120, 120, 20, mode, value));
            var p = Character(); Land(p);
            Keys(Key.Space); p.Tick(Step); Keys(); p.Tick(Step); p.FixedTick(Step);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True);
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(expected));
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(120 / -Physics2D.gravity.y).Within(.001f));
            Assert.That(p.Context.Input.JumpRequested, Is.False);
        }

        [TestCase(true)] [TestCase(false)]
        public void OrdinaryRhythm_ApexHoldGateDoesNotChangeFallOutsideApex(bool held)
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var p = Character(); Land(p); Keys(Key.Space); Simulate(p);
            Place(p, new Vector2(0, 5), 3);
            // 越过保持窗口，再单独观察顶点规则。
            for (int i = 0; i < 12; i++) Decide(p);
            if (!held) Keys();
            Place(p, new Vector2(0, 5), -3); Decide(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo((held ? 60 : 120) / -Physics2D.gravity.y).Within(.001f));
            Place(p, new Vector2(0, 5), -8); Decide(p);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(120 / -Physics2D.gravity.y).Within(.001f));
        }

        [TestCase(30)] [TestCase(60)] [TestCase(144)]
        public void OrdinaryRhythm_RenderSamplingDoesNotAdvanceHoldTimer(int renderRate)
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var p = Character(); Land(p); Keys(Key.Space); Simulate(p);
            float before = p.Context.VerticalMotion.OrdinaryElapsed;
            for (int i = 0; i < renderRate; i++) p.Tick(1f / renderRate);
            Assert.That(p.Context.VerticalMotion.OrdinaryElapsed, Is.EqualTo(before));
            p.FixedTick(.02f);
            Assert.That(p.Context.VerticalMotion.OrdinaryElapsed, Is.EqualTo(before + .02f).Within(.00001));
        }

        [Test]
        public void OrdinaryRhythm_HorizontalAccelerationBrakingTurningUnchanged()
        {
            var movement = _config.Feel.Movement;
            foreach (var field in new[] { "_accelerated", "_independentAir", "_airAccelerated" })
                typeof(PlayerMoveFeel).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, true);
            var baseline = _config.Feel.Capture(Physics2D.gravity);
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            var candidate = _config.Feel.Capture(Physics2D.gravity);
            foreach (bool ground in new[] { true, false })
            foreach (float current in new[] { -8f, 0f, 3f, 8f })
            foreach (float direction in new[] { -1f, 0f, 1f })
                Assert.That(candidate.CalculateHorizontalSpeed(current, direction, ground, Step),
                    Is.EqualTo(baseline.CalculateHorizontalSpeed(current, direction, ground, Step)));
        }

        [Test]
        public void OrdinaryRhythm_ExternalLaunchInterruptsOrdinaryAndOldLeaseCannotAffectNextJump()
        {
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec()); var p = Character(); Land(p);
            Keys(Key.Space); Simulate(p);
            var old = p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("launch",
                PlayerExternalMotionMode.InitialVelocity, Vector2.up * 14));
            Keys(); Simulate(p);
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
            Assert.That(p.Context.Motor.Body.gravityScale, Is.EqualTo(3));
            Land(p); Keys(Key.Space); Simulate(p); old.Dispose();
            Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True);
        }

        [TestCase(false)] [TestCase(true)]
        public void OrdinaryRhythm_GroundAndAirMovementPhysicsUnchanged(bool airborne)
        {
            foreach (var field in new[] { "_accelerated", "_independentAir", "_airAccelerated" })
                typeof(PlayerMoveFeel).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(_config.Feel.Movement, true);
            List<Vector2> Run(bool enabled)
            {
                _config.Feel.Jump.ConfigureOrdinaryJump(enabled ? RhythmSpec() : default);
                Keys(); var p = Character(new Vector2(0, airborne ? 50 : .9f));
                if (!airborne) Land(p);
                var trace = new List<Vector2>();
                for (int i = 0; i < 60; i++)
                {
                    if (i < 15) Keys(Key.D); else if (i < 30) Keys(); else if (i < 45) Keys(Key.A); else Keys(Key.D);
                    Simulate(p); trace.Add(new Vector2(p.Context.Motor.Position.x, p.Context.Motor.Velocity.x));
                }
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); return trace;
            }
            CollectionAssert.AreEqual(Run(false), Run(true));
        }

        [Test]
        public void OrdinaryRhythm_SharedGravityBaselineReproducesSpecialRegression()
        {
            float Height(float gravity, bool fastFall)
            {
                SetJumpFeel("_gravityScale", gravity); SetJumpFeel("_enableFallGravity", fastFall);
                SetJumpFeel("_fallMultiplier", 2f); SetJumpFeel("_enableFallLimit", fastFall); SetJumpFeel("_fallLimit", 20f);
                var p = Character(); Land(p); float start = p.Context.Motor.Position.y, peak = start;
                p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("baseline-flag",
                    PlayerExternalMotionMode.InitialVelocity, Vector2.up * 14));
                bool air = false; int samples = 0;
                for (; samples < 200; samples++)
                {
                    Simulate(p); peak = Mathf.Max(peak, p.Context.Motor.Position.y);
                    air |= !p.Context.JumpWindow.HasGroundSupport;
                    if (air && p.Context.JumpWindow.HasGroundSupport) break;
                }
                Assert.That(air && p.Context.JumpWindow.HasGroundSupport, Is.True);
                TestContext.WriteLine($"shared-baseline gravity={gravity:R} launch=14 height={peak-start:R} airSteps={samples + 1}");
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); return peak - start;
            }
            float retained = Height(3, false); float diagnostic = Height(60 / -Physics2D.gravity.y, true);
            Assert.That(diagnostic, Is.LessThan(retained * .6f), "初速度相同无法隔离共享重力回归");
        }

        [TestCase(.04f)] [TestCase(.12f)] [TestCase(.6f)]
        public void OrdinaryRhythm_RetainedAndIndependentCandidateAB(float hold)
        {
            SetJumpFeel("_launchSpeed", 15.6f); SetJumpFeel("_releaseSpeed", 8f);
            float Run(bool enabled)
            {
                _config.Feel.Jump.ConfigureOrdinaryJump(enabled ? RhythmSpec() : default);
                Keys(); var p = Character(); Land(p); float ground = p.Context.Motor.Position.y;
                Keys(Key.Space); float peak = ground, apex = 0, elapsed = 0; bool air = false;
                for (int i = 0; i < 200; i++)
                {
                    if (elapsed >= hold) Keys();
                    Simulate(p); elapsed += Step;
                    if (p.Context.Motor.Position.y > peak) { peak = p.Context.Motor.Position.y; apex = elapsed; }
                    air |= !p.Context.JumpWindow.HasGroundSupport;
                    if (air && p.Context.JumpWindow.HasGroundSupport) break;
                }
                Assert.That(air && p.Context.JumpWindow.HasGroundSupport, Is.True);
                TestContext.WriteLine($"AB enabled={enabled} hold={hold:R} height={peak-ground:R} rise={apex:R} fall={elapsed-apex:R} air={elapsed:R}");
                p.Stop(); UnityEngine.Object.DestroyImmediate(p.gameObject); return elapsed;
            }
            Assert.That(Run(true), Is.LessThan(Run(false)));
        }

#if UNITY_EDITOR
        [TestCase(false)] [TestCase(true)]
        public void OrdinaryRhythm_ReactionAndDeathCancelPolicy(bool lethal)
        {
            WithThreeHit((p, scheme) =>
            {
                Keys(Key.Space); Simulate(p);
                Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.True);
                p.Context.HitReceiver.Receive(TestHit(damage: lethal ? 20 : 1)); Decide(p);
                Assert.That(p.Context.VerticalMotion.OrdinaryActive, Is.False);
                Assert.That(p.CurrentStateId, Is.EqualTo(lethal ? "life.dead" : "life.hit"));
            }, scheme => { InstallLifeTest(scheme); scheme.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec()); });
        }
#endif
    }
}
