using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private EntityTranslationSupport2D MovingSupport()
        {
            var go = Keep(new GameObject("Translation support"));
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector3(0, 5);
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<BoxCollider2D>().size = new Vector2(6, .5f);
            var source = go.AddComponent<EntityTranslationSupport2D>();
            Physics2D.SyncTransforms(); return source;
        }

        [TestCase(2, 0)]
        [TestCase(0, 2)]
        [TestCase(0, -2)]
        public void Support_TranslatesWithoutDoubleCounting(int x, int y)
        {
            var support = MovingSupport();
            var player = Character(new Vector2(0, 6.15f)); Land(player);
            var relative = player.Context.Motor.Position - support.Body.position;
            support.SetVelocity(new Vector2(x, y));
            for (int i = 0; i < 25; i++) Simulate(player);
            Assert.That(player.Context.Support.Source, Is.SameAs(support));
            var moved = player.Context.Motor.Position - support.Body.position;
            Assert.That(moved.x, Is.EqualTo(relative.x).Within(.08f));
            Assert.That(moved.y, Is.EqualTo(relative.y).Within(.08f));
        }

        [Test]
        public void Support_JumpInheritsOnceAndStopClearsOwnership()
        {
            var support = MovingSupport();
            var player = Character(new Vector2(0, 6.15f)); Land(player);
            support.SetVelocity(new Vector2(2, 0));
            for (int i = 0; i < 3; i++) Simulate(player);
            Keys(Key.Space); Simulate(player); Keys();
            for (int i = 0; i < 3; i++) Simulate(player);
            Assert.That(player.Context.Support.Source, Is.Null);
            Assert.That(player.Context.Support.InheritedVelocity.x, Is.EqualTo(2));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(2).Within(.001f));
            var motion = player.Context.Support;
            player.Stop(); Assert.That(motion.HasTransientMotion, Is.False);
        }

        [Test]
        public void Support_RisingIntoCeilingDoesNotPushPlayerThroughCeiling()
        {
            var support = MovingSupport();
            var ceiling = Keep(new GameObject("Support ceiling"));
            SceneManager.MoveGameObjectToScene(ceiling, _scene);
            ceiling.transform.position = new Vector3(0, 9);
            ceiling.AddComponent<BoxCollider2D>().size = new Vector2(8, .5f);
            var player = Character(new Vector2(0, 6.15f)); Land(player);
            support.SetVelocity(new Vector2(0, 4));
            for (int i = 0; i < 35; i++)
            {
                Simulate(player);
                Assert.That(player.Context.Sensors.Shape.bounds.max.y, Is.LessThanOrEqualTo(8.78f));
            }
        }

        [TestCase(.01f)]
        [TestCase(.04f)]
        public void Support_UsesSecondsAtDifferentPhysicsSteps(float dt)
        {
            var support = MovingSupport();
            var player = Character(new Vector2(0, 6.15f)); Land(player);
            support.SetVelocity(new Vector2(2, 0));
            float start = player.Context.Motor.Position.x;
            int steps = Mathf.RoundToInt(1 / dt);
            for (int i = 0; i < steps; i++)
            {
                player.Tick(dt); player.FixedTick(dt);
                Assert.That(_scene.GetPhysicsScene2D().Simulate(dt), Is.True);
            }
            Assert.That(player.Context.Motor.Position.x - start, Is.EqualTo(2).Within(.05f));
        }

        [Test]
        public void Support_WindAddsOnceAndDisableDropsReference()
        {
            var support = MovingSupport();
            var player = Character(new Vector2(0, 6.15f)); Land(player);
            support.SetVelocity(new Vector2(2, 0));
            player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("support wind", PlayerExternalMotionMode.AddVelocity, Vector2.right));
            for (int i = 0; i < 10; i++) Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(3).Within(.001f));
            support.SetVelocity(new Vector2(-2, 0)); Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-1).Within(.001f));
            support.enabled = false; Simulate(player);
            Assert.That(player.Context.Support.Source, Is.Null);
            Assert.That(player.Context.Support.InheritedVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(1).Within(.001f));
        }

        [Test]
        public void Support_OneWayDropDoesNotRetainCarrier()
        {
            InstallDrop();
            var collider = DropPlatform();
            collider.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var support = collider.gameObject.AddComponent<EntityTranslationSupport2D>();
            var player = OnDropPlatform(); Simulate(player);
            Assert.That(player.Context.Support.Source, Is.SameAs(support));
            Keys(Key.S, Key.Space); Simulate(player); Keys(); Simulate(player);
            Assert.That(player.Context.DropThrough.IsActive, Is.True);
            Assert.That(player.Context.Support.Source, Is.Null);
        }

        [Test]
        public void Support_TeleportDoesNotCarryPlayerToResetPosition()
        {
            var support = MovingSupport();
            var player = Character(new Vector2(0, 6.15f)); Land(player); Simulate(player);
            var before = player.Context.Motor.Position;
            support.Teleport(new Vector2(20, 5)); Physics2D.SyncTransforms(); Simulate(player);
            Assert.That(player.Context.Support.Source, Is.Null);
            Assert.That(player.Context.Support.InheritedVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.Context.Motor.Position.x, Is.EqualTo(before.x).Within(.001f));
        }
    }
}
