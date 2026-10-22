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
        [UnityTest]
        public IEnumerator CarrierDescent_ActualRenderRateMatrix()
        {
            int oldRate = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
            try
            {
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 30, 120, 240 })
                foreach (float dt in new[] { .02f, 1f / 60 })
                foreach (float trough in new[] { -6.4f, -4.4f })
                {
                    Application.targetFrameRate = rate;
                    var support = MovingSupport(); support.ConfigureMotion(true, ~0);
                    var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
                    float accumulator = 0, elapsed = 0;
                    int frames = 0, step = 0;
                    while (step < 25 && frames < 600)
                    {
                        yield return null;
                        Keys();
                        float frame = Time.unscaledDeltaTime; elapsed += frame; frames++;
                        p.Tick(frame); accumulator += frame;
                        while (accumulator >= dt && step < 25)
                        {
                            step++;
                            float y = step <= 11 ? 20 : step <= 22 ? 20 - (step - 11) * 2.4f : -4;
                            if (step == 22) y = trough;
                            support.SetVelocity(new Vector2(0, y)); support.PrepareStep(dt);
                            var before = p.Context.Motor.Position; var platformBefore = support.Body.position;
                            p.FixedTick(dt);
                            var command = p.Context.Motor.CommandVelocity;
                            Assert.That(command.y, Is.EqualTo(support.Velocity.y).Within(.001f), "submission");
                            Assert.That(_scene.GetPhysicsScene2D().Simulate(dt), Is.True);
                            var delta = p.Context.Motor.Position - before;
                            var platformDelta = support.Body.position - platformBefore;
                            TestContext.WriteLine($"D01-render rate={rate} dt={dt:R} trough={trough:R} step={step} allowed={support.Velocity.y:R} command={command.y:R} postBody={p.Context.Motor.Velocity.y:R} dy={delta.y:R} platformDy={platformDelta.y:R}");
                            Assert.That(delta.y, Is.EqualTo(platformDelta.y).Within(.003f), "actual motion");
                            Assert.That(p.Context.Support.Source, Is.SameAs(support));
                            accumulator -= dt;
                        }
                    }
                    Assert.That(step, Is.EqualTo(25));
                    TestContext.WriteLine($"D01-render requested={rate} actualMeanFPS={frames / elapsed:R} dt={dt:R} trough={trough:R} frames={frames} steps={step} driver=render-paced-isolated-physics");
                    p.Stop(); Object.DestroyImmediate(p.gameObject); Object.DestroyImmediate(support.gameObject);
                }
            }
            finally { Application.targetFrameRate = oldRate; QualitySettings.vSyncCount = oldVsync; Keys(); }
        }

        [Test]
        public void CarrierDescent_LifecycleCross(
            [Values(-6.4f, -4.4f)] float trough,
            [Values("jump", "disable", "teleport", "replace", "owner", "floor", "roof", "stop")] string boundary)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            CarrierStep(support, Vector2.down * 4, p);
            CarrierStep(support, Vector2.up * trough, p);
            if (boundary == "stop")
            {
                var motion = p.Context.ExternalMotion; var supportMotion = p.Context.Support;
                p.Stop(); Assert.That(motion.HasTransientMotion, Is.False);
                Assert.That(supportMotion.Source, Is.Null); return;
            }
            if (boundary == "jump") Keys(Key.Space);
            if (boundary == "disable") support.enabled = false;
            if (boundary == "teleport") support.Teleport(new Vector2(20, 5));
            if (boundary == "replace")
            {
                var position = support.Body.position; support.gameObject.SetActive(false);
                support = MovingSupport(); support.Teleport(position); support.ConfigureMotion(true, ~0);
            }
            if (boundary == "owner")
            {
                var owner = ExternalOwner();
                var old = p.Context.ExternalMotion.Register(owner, new PlayerExternalMotionRequest("D01-owner", PlayerExternalMotionMode.AddVelocity, Vector2.down));
                var current = p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("D01-owner", PlayerExternalMotionMode.AddVelocity, Vector2.down * 2));
                old.Dispose(); owner.enabled = false;
                Assert.That(current.IsActive, Is.True);
            }
            if (boundary == "floor")
                CarrierBarrier("D01-floor", support.Body.position + Vector2.down * .52f, new Vector2(30, .5f));
            if (boundary == "roof")
                CarrierBarrier("D01-roof", new Vector2(0, p.Context.Sensors.Shape.bounds.max.y + .12f), new Vector2(30, .2f));
            Physics2D.SyncTransforms();
            if (support.IsAvailable)
            {
                support.SetVelocity(boundary == "roof" ? Vector2.up * 4 : Vector2.down * 4);
                support.PrepareStep(Step);
            }
            var before = p.Context.Motor.Position; var platformBefore = support.Body.position;
            Decide(p); var command = p.Context.Motor.CommandVelocity;
            Assert.That(_scene.GetPhysicsScene2D().Simulate(Step), Is.True);
            TestContext.WriteLine($"D01-cross boundary={boundary} trough={trough:R} support={p.Context.Support.Velocity.y:R} command={command.y:R} postBody={p.Context.Motor.Velocity.y:R} dy={(p.Context.Motor.Position-before).y:R} platformDy={(support.Body.position-platformBefore).y:R}");
            if (boundary == "jump")
            {
                Assert.That(command.y, Is.GreaterThan(0)); Keys();
                for (int i = 0; i < 3; i++) Simulate(p);
                Assert.That(p.Context.Support.Source, Is.Null);
            }
            else if (boundary == "disable" || boundary == "teleport")
            {
                Assert.That(p.Context.Support.Source, Is.Null);
                Assert.That(command.y, Is.EqualTo(0).Within(.001f), "old carrier Y must not remain as self motion");
            }
            else
            {
                Assert.That(p.Context.Support.Source, Is.SameAs(support));
                Assert.That(command.y, Is.EqualTo(p.Context.Support.Velocity.y - (boundary == "owner" ? 2 : 0)).Within(.001f));
                Assert.That((p.Context.Motor.Position-before).y, Is.EqualTo((support.Body.position-platformBefore).y).Within(.003f));
            }
        }
    }
}
