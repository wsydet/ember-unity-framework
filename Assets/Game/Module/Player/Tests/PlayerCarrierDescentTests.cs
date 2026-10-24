using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [Test]
        public void Carrier_DescentRecoverySeparatesSubmissionAndPhysics(
            [Values(-6.4f, -4.4f)] float trough, [Values(.01f, .02f, .04f)] float dt,
            [Values(false, true)] bool dual, [Values(false, true)] bool oneWay,
            [Values(false, true)] bool combined)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            support.GetComponent<BoxCollider2D>().size = new Vector2(30, .5f);
            if (oneWay)
            {
                support.GetComponent<Collider2D>().usedByEffector = true;
                var effector = support.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.useOneWayGrouping = true;
            }
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            if (dual) p.gameObject.AddComponent<CircleCollider2D>().radius = .25f;
            if (combined)
            {
                Keys(Key.D);
                p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest(
                    "D01 wind", PlayerExternalMotionMode.AddVelocity, new Vector2(.5f, -1)));
            }
            Physics2D.SyncTransforms();
            bool submission = true, motion = true;
            for (int step = 1; step <= 25; step++)
            {
                float y = step <= 11 ? 20 : step <= 22 ? 20 - (step - 11) * 2.4f : -4;
                if (step == 22) y = trough;
                support.SetVelocity(new Vector2(combined ? 1 : 0, y)); support.PrepareStep(dt);
                var before = p.Context.Motor.Position; var platformBefore = support.Body.position;
                p.Tick(dt); p.FixedTick(dt);
                var command = p.Context.Motor.CommandVelocity;
                var submittedBody = p.Context.Motor.Velocity;
                Assert.That(_scene.GetPhysicsScene2D().Simulate(dt), Is.True);
                var delta = p.Context.Motor.Position - before;
                var platformDelta = support.Body.position - platformBefore;
                TestContext.WriteLine($"D01 step={step} dt={dt:R} allowed={support.Velocity.y:R} support={p.Context.Support.Velocity.y:R} command={command.y:R} submittedBody={submittedBody.y:R} postBody={p.Context.Motor.Velocity.y:R} dy={delta.y:R} platformDy={platformDelta.y:R} gap={p.Context.Sensors.Shape.bounds.min.y-support.GetComponent<Collider2D>().bounds.max.y:R} same={p.Context.Support.Source == support} fps=N/A-isolated-physics");
                submission &= Mathf.Abs(command.y - p.Context.Support.Velocity.y + (combined ? 1 : 0)) < .001f;
                if (step >= 20) Assert.That(support.Velocity.y, Is.EqualTo(y).Within(.001f), "exact descent waveform");
                motion &= Mathf.Abs(delta.y - platformDelta.y) < .003f;
                Assert.That(p.Context.Support.Source, Is.SameAs(support));
            }
            Assert.That(new[] { motion, submission }, Is.EqualTo(new[] { true, true }),
                $"actual displacement consistency={motion}; submission consistency={submission}");
        }
    }
}
