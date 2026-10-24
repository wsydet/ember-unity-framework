using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Entity.Tests
{
    public sealed class EntityOneWayPlatformTests
    {
        #region 内部参数
        private Scene _scene;
        private PhysicsScene2D _physics;
        private readonly List<IDisposable> _leases = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private GameObject Object(string name, Vector2 position)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position; return go;
        }
        private BoxCollider2D Platform(bool oneWay = true)
        {
            var go = Object("platform", Vector2.zero);
            var shape = go.AddComponent<BoxCollider2D>(); shape.size = new Vector2(8, .2f);
            if (oneWay)
            {
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true; effector.surfaceArc = 180; effector.useColliderMask = false;
                shape.usedByEffector = true;
            }
            return shape;
        }
        private EntitySensors2D Actor(float x = 0, float y = 2)
        {
            var go = Object("actor", new Vector2(x, y));
            var body = go.AddComponent<Rigidbody2D>(); body.freezeRotation = true;
            var shape = go.AddComponent<BoxCollider2D>(); shape.size = new Vector2(1, 1.75f);
            var entity = go.AddComponent<Entity2D>();
            var sensors = go.AddComponent<EntitySensors2D>(); sensors.Configure(shape, 1); sensors.Bind(entity);
            return sensors;
        }
        private void Step(int count = 1)
        {
            Physics2D.SyncTransforms();
            for (int i = 0; i < count; i++) Assert.That(_physics.Simulate(.02f), Is.True);
        }
        private EntityCollisionIgnoreLease2D Ignore(Collider2D a, Collider2D b)
        { var lease = EntityCollisionIgnoreLease2D.Acquire(a, b); _leases.Add(lease); return lease; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _scene = SceneManager.CreateScene("OneWayTest-" + Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            _physics = _scene.GetPhysicsScene2D();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var lease in _leases) lease.Dispose(); _leases.Clear();
            yield return SceneManager.UnloadSceneAsync(_scene);
        }
        [Test]
        public void FallingOntoOneWayIsRealSupportButSolidGroundIsNotOneWay()
        {
            var platform = Platform(); var actor = Actor(); Step(100); actor.Sample();
            Assert.That(actor.Ground, Is.SameAs(platform));
            Assert.That(actor.OneWayGround, Is.SameAs(platform));
            platform.usedByEffector = false; Step(); actor.Sample();
            Assert.That(actor.IsGrounded, Is.True); Assert.That(actor.OneWayGround, Is.Null);
        }
        [Test]
        public void RisingThroughPlatformNeverReportsGroundOrWalls()
        {
            Platform(); var actor = Actor(y: -1.3f); var body = actor.GetComponent<Rigidbody2D>();
            body.gravityScale = 0; body.linearVelocity = Vector2.up * 5;
            for (int i = 0; i < 27; i++)
            {
                Step(); actor.Sample();
                Assert.That(actor.IsGrounded, Is.False, "step " + i);
                Assert.That(actor.LeftWall, Is.Null); Assert.That(actor.RightWall, Is.Null);
            }
            Assert.That(body.position.y, Is.GreaterThan(1));
        }
        [Test]
        public void PairExclusionDropsOnlyOwnerAndRestoresLandingAfterRelease()
        {
            var platform = Platform(); var a = Actor(-1); var b = Actor(1); Step(100);
            var effector = platform.GetComponent<PlatformEffector2D>(); int mask = effector.colliderMask;
            var lease = Ignore(a.Shape, platform); a.Sample(); b.Sample();
            Assert.That(a.Ground, Is.Null); Assert.That(b.Ground, Is.SameAs(platform));
            Step(40);
            Assert.That(a.GetComponent<Rigidbody2D>().position.y, Is.LessThan(-1));
            b.Sample(); Assert.That(b.OneWayGround, Is.SameAs(platform));
            Assert.That(effector.colliderMask, Is.EqualTo(mask));
            lease.Dispose(); Assert.That(Physics2D.GetIgnoreCollision(a.Shape, platform), Is.False);
            var body = a.GetComponent<Rigidbody2D>(); body.position = new Vector2(-1, 2); body.linearVelocity = Vector2.zero;
            Step(100); a.Sample(); Assert.That(a.OneWayGround, Is.SameAs(platform));
        }
        [Test]
        public void OtherPlatformStillCatchesExcludedActor()
        {
            var upper = Platform(); var lower = Platform(); lower.transform.position = new Vector3(0, -3, 0);
            var actor = Actor(); Step(100); Ignore(actor.Shape, upper); Step(100); actor.Sample();
            Assert.That(actor.OneWayGround, Is.SameAs(lower));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void NestedOwnersAndReversedPairPreserveOriginalIgnore(bool original)
        {
            var platform = Platform(); var actor = Actor();
            Physics2D.IgnoreCollision(actor.Shape, platform, original);
            var first = Ignore(actor.Shape, platform); var second = Ignore(platform, actor.Shape);
            first.Dispose(); first.Dispose();
            Assert.That(Physics2D.GetIgnoreCollision(actor.Shape, platform), Is.True);
            second.Dispose(); Assert.That(Physics2D.GetIgnoreCollision(actor.Shape, platform), Is.EqualTo(original));
        }
        [Test]
        public void MotorPauseAndResumeReapplyKeepsLeaseAndPosition()
        {
            var platform = Platform(); var actor = Actor(); Step(100);
            var lease = Ignore(actor.Shape, platform); var motor = actor.gameObject.AddComponent<EntityMotor2D>();
            var position = motor.Position; motor.Freeze(); Step(20);
            Assert.That(motor.Position, Is.EqualTo(position)); Assert.That(lease.IsDisposed, Is.False);
            motor.Resume(); lease.Reapply(); actor.Sample(); Assert.That(actor.IsGrounded, Is.False);
            Step(40); Assert.That(motor.Position.y, Is.LessThan(-1));
        }
        [Test]
        public void ReenabledColliderReappliesWithoutSecondOwnerAndDestroyedTargetDisposes()
        {
            var platform = Platform(); var actor = Actor(); var lease = Ignore(actor.Shape, platform);
            actor.Shape.enabled = false; actor.Shape.enabled = true; lease.Reapply();
            Assert.That(Physics2D.GetIgnoreCollision(actor.Shape, platform), Is.True);
            UnityEngine.Object.DestroyImmediate(platform.gameObject);
            Assert.DoesNotThrow(lease.Dispose); Assert.That(lease.IsDisposed, Is.True);
        }
        [Test]
        public void SensorsRespectExistingPairIgnoreWithoutLease()
        {
            var platform = Platform(false); var actor = Actor(); Step(100);
            Physics2D.IgnoreCollision(actor.Shape, platform, true); actor.Sample();
            Assert.That(actor.IsGrounded, Is.False);
            Physics2D.IgnoreCollision(actor.Shape, platform, false);
        }
        [Test]
        public void InvalidAcquisitionCannotAlterValidPair()
        {
            var platform = Platform(); var actor = Actor();
            Assert.Throws<ArgumentException>(() => EntityCollisionIgnoreLease2D.Acquire(actor.Shape, actor.Shape));
            Assert.Throws<ArgumentException>(() => EntityCollisionIgnoreLease2D.Acquire(actor.Shape, null));
            platform.enabled = false;
            Assert.Throws<ArgumentException>(() => EntityCollisionIgnoreLease2D.Acquire(actor.Shape, platform));
            platform.enabled = true;
            Assert.That(Physics2D.GetIgnoreCollision(actor.Shape, platform), Is.False);
        }
        #endregion
    }
}
