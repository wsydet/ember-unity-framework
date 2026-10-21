using System.Collections;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private BoxCollider2D CarrierBarrier(string name, Vector2 position, Vector2 size)
        {
            var go = Keep(new GameObject(name)); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position; var shape = go.AddComponent<BoxCollider2D>(); shape.size = size;
            return shape;
        }
        private EntityTranslationSupport2D Carrier(Vector2 position)
        {
            var support = MovingSupport(); support.Body.position = position;
            support.GetComponent<BoxCollider2D>().size = Vector2.one;
            support.Body.useFullKinematicContacts = true;
            support.ConfigureMotion(true, ~0); Physics2D.SyncTransforms(); return support;
        }
        private void CarrierStep(EntityTranslationSupport2D support, Vector2 target, PlayerController rider = null)
        {
            var before = support.Body.position; support.SetVelocity(target); support.PrepareStep(Step);
            if (rider != null) Decide(rider);
            var allowed = support.Velocity;
            Assert.That(_scene.GetPhysicsScene2D().Simulate(Step), Is.True);
            TestContext.WriteLine($"carrier target={target:F5} allowed={allowed:F5} actual={(support.Body.position-before)/Step:F5} position={support.Body.position:F5} support={rider?.Context.Support.Velocity} command={rider?.Context.Motor.CommandVelocity} dt={Step:R} input=scripted-public-api fps=N/A-synchronous");
        }

        [TestCase(0, -4)] [TestCase(-4, 0)] [TestCase(4, 0)] [TestCase(0, 4)]
        [TestCase(4, -4)] [TestCase(-4, -4)] [TestCase(4, 4)] [TestCase(-4, 4)]
        public void Carrier_SolidNormalStopsAndTangentContinues(int x, int y)
        {
            var start = new Vector2(30, 30); var carrier = Carrier(start);
            bool floor = y < 0, roof = y > 0 && x == 0;
            var normal = floor ? Vector2.down : roof ? Vector2.up : new Vector2(Mathf.Sign(x), 0);
            CarrierBarrier("barrier", start + normal * 2, normal.x == 0 ? new Vector2(30, .5f) : new Vector2(.5f, 30));
            Physics2D.SyncTransforms();
            for (int i = 0; i < 41; i++) CarrierStep(carrier, new Vector2(x, y));
            var delta = carrier.Body.position - start;
            Assert.That(Vector2.Dot(delta, normal), Is.InRange(1.20f, 1.251f));
            float tangent = normal.x == 0 ? delta.x : delta.y;
            float wanted = (normal.x == 0 ? x : y) * .82f;
            Assert.That(tangent, Is.EqualTo(wanted).Within(.02f));
        }

        [TestCase(20, 0)] [TestCase(15, 10)] [TestCase(-15, 10)]
        [TestCase(7.5f, 8)] [TestCase(-10.5f, 5)] [TestCase(200, 100)]
        public void Carrier_ThinCornerAndAllAttachedShapes(float x, float y)
        {
            var carrier = Carrier(new Vector2(30, 30));
            var extra = Keep(new GameObject("child solid")); extra.transform.SetParent(carrier.transform, false);
            extra.transform.localPosition = new Vector3(x < 0 ? -.75f : .75f, 0);
            extra.AddComponent<CircleCollider2D>().radius = .25f;
            CarrierBarrier("thin wall", new Vector2(30 + Mathf.Sign(x) * 2, 32), new Vector2(.02f, 20));
            CarrierBarrier("roof", new Vector2(30, 34), new Vector2(30, .02f)); Physics2D.SyncTransforms();
            for (int i = 0; i < 25; i++) CarrierStep(carrier, new Vector2(x, y));
            Assert.That(Mathf.Abs(carrier.Body.position.x - 30), Is.LessThan(1.01f));
            Assert.That(carrier.Body.position.y, Is.LessThan(33.51f));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void Carrier_TriggerPairLayerAndMaskExclusions(int exclusion)
        {
            var carrier = Carrier(new Vector2(30, 30));
            var wall = CarrierBarrier("ignored wall", new Vector2(32, 30), new Vector2(.1f, 20));
            if (exclusion == 0) wall.isTrigger = true;
            if (exclusion == 1) Physics2D.IgnoreCollision(carrier.GetComponent<Collider2D>(), wall);
            if (exclusion >= 2) wall.gameObject.layer = 2;
            bool previous = Physics2D.GetIgnoreLayerCollision(0, 2);
            try
            {
                if (exclusion == 2) Physics2D.IgnoreLayerCollision(0, 2, true);
                if (exclusion == 3) carrier.ConfigureMotion(true, 1);
                Physics2D.SyncTransforms();
                for (int i = 0; i < 41; i++) CarrierStep(carrier, Vector2.right * 4);
                Assert.That(carrier.Body.position.x, Is.EqualTo(33.28f).Within(.02f));
            }
            finally { Physics2D.IgnoreLayerCollision(0, 2, previous); }
        }

        [Test]
        public void Carrier_SourceOneWayUpThroughDownLandAndWindAcrossSeams()
        {
            var carrier = Carrier(new Vector2(30, 28));
            for (int i = 0; i < 10; i++)
            {
                var shape = CarrierBarrier("source tile", new Vector2(28 + i, 30), new Vector2(1, .3f));
                shape.offset = new Vector2(0, .35f); shape.usedByEffector = true;
                var effector = shape.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.surfaceArc = 179.5f; effector.useOneWayGrouping = false;
            }
            Physics2D.SyncTransforms();
            for (int i = 0; i < 15; i++) CarrierStep(carrier, Vector2.up * 20);
            Assert.That(carrier.Body.position.y, Is.GreaterThan(33));
            for (int i = 0; i < 45; i++) CarrierStep(carrier, new Vector2(4, -4));
            Assert.That(carrier.Body.position.y, Is.InRange(31, 31.06f));
            Assert.That(carrier.Body.position.x, Is.EqualTo(33.6f).Within(.03f));
        }

        [TestCase(-1, LaunchEnd2D.StopVertical)] [TestCase(1, LaunchEnd2D.StopVertical)]
        [TestCase(-1, LaunchEnd2D.ContinueVelocity)] [TestCase(1, LaunchEnd2D.ContinueVelocity)]
        public void Carrier_ExpiryStepRiderUsesFinalVelocity(int sign, LaunchEnd2D end)
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            launch.TryLaunch(ExternalOwner(), new LaunchRequest2D("carrier expiry", Vector2.up * 20,
                LaunchMode2D.ConstantRise, 13f / 60, end, horizontal: LaunchHorizontal2D.PreserveControl));
            for (int i = 0; i < 12; i++)
            {
                launch.TrySetHorizontalVelocity(sign * 2); support.PrepareStep(Step); Decide(p);
                TestContext.WriteLine($"expiry step={i+1} end={end} target={support.TargetVelocity:F5} allowed={support.Velocity:F5} support={p.Context.Support.Velocity:F5} command={p.Context.Motor.CommandVelocity:F5} position={p.Context.Motor.Position:F5} dt={Step:R} input=synthetic-keyboard-released fps=N/A-synchronous");
                Assert.That(p.Context.Support.Velocity.y, Is.EqualTo(support.Velocity.y).Within(.001f), "same step " + (i + 1));
                if (i == 10)
                {
                    Assert.That(launch.IsLaunching, Is.False);
                    Assert.That(support.Velocity.y, Is.EqualTo(end == LaunchEnd2D.StopVertical ? 0 : 20));
                    Assert.That(p.Context.Motor.CommandVelocity.y, Is.EqualTo(support.Velocity.y).Within(.001f));
                }
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
        }

        [Test]
        public void Carrier_RiderHeadroomAndOwnWallCannotRestoreLaunchY()
        {
            var support = MovingSupport(); support.ConfigureMotion(true, ~0);
            CarrierBarrier("rider roof", new Vector2(0, 9), new Vector2(30, .5f));
            CarrierBarrier("carrier wall", new Vector2(4, 6), new Vector2(.5f, 20));
            var p = Character(new Vector2(0, 6.15f)); Land(p); Simulate(p);
            var launch = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            launch.TryLaunch(ExternalOwner(), new LaunchRequest2D("headroom", Vector2.up * 20,
                LaunchMode2D.ConstantRise, 1, horizontal: LaunchHorizontal2D.PreserveControl));
            for (int i = 0; i < 20; i++)
            {
                launch.TrySetHorizontalVelocity(2); support.PrepareStep(Step); Decide(p);
                Assert.That(p.Context.Support.Velocity.y, Is.EqualTo(support.Velocity.y).Within(.001f));
                _scene.GetPhysicsScene2D().Simulate(Step);
                Assert.That(p.Context.Sensors.Shape.bounds.max.y, Is.LessThan(8.78f));
                Assert.That(support.Body.position.x, Is.LessThan(.751f));
            }
            Assert.That(support.Velocity.y, Is.Zero); Assert.That(launch.IsLaunching, Is.False);
        }

        [Test]
        public void Carrier_TeleportAndDisableCancelLaunchWithoutResurrection()
        {
            var support = MovingSupport(); var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            var owner = ExternalOwner(); var request = new LaunchRequest2D("lifecycle", Vector2.up * 20, LaunchMode2D.ConstantRise, 1);
            target.TryLaunch(owner, request); uint revision = support.Revision;
            support.Teleport(new Vector2(0, 5)); support.PrepareStep(Step);
            Assert.That(target.IsLaunching, Is.False); Assert.That(support.Revision, Is.GreaterThan(revision));
            Assert.That(support.Velocity, Is.EqualTo(Vector2.zero));
            target.TryLaunch(owner, request); support.enabled = false; support.enabled = true; support.PrepareStep(Step);
            Assert.That(target.IsLaunching, Is.False); Assert.That(support.Velocity, Is.EqualTo(Vector2.zero));
        }
    }
}
