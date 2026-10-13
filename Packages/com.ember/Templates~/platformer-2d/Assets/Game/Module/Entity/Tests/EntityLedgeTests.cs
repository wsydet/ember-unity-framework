using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Entity.Tests
{
    public sealed class EntityLedgeTests
    {
        #region 内部参数
        private Scene _scene;
        private PhysicsScene2D _physics;
        private EntityMotor2D _motor;
        private EntityLedgeProbe2D _probe;
        private BoxCollider2D _actor;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private BoxCollider2D Box(string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>(); box.size = size; return box;
        }
        private BoxCollider2D Arrange(int direction = 1)
        {
            // 平台顶 y=1，近侧 x=±.6；手部 y=.875，距墙 .1。
            var platform = Box("ledge", new Vector2(direction * 2.6f, 0), new Vector2(4, 2));
            _actor = Box("actor", new Vector2(0, .2f), new Vector2(1, 1.75f));
            var body = _actor.gameObject.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            var entity = _actor.gameObject.AddComponent<Entity2D>();
            _motor = _actor.gameObject.AddComponent<EntityMotor2D>();
            _probe = new EntityLedgeProbe2D(entity, _actor); Physics2D.SyncTransforms(); return platform;
        }
        private bool Find(int direction, out EntityLedgeProbe2D.Candidate candidate)
            => _probe.TryFind(direction, 1, .2f, .4f, .2f, .02f, out candidate);
        private void Move(Vector2 position)
        {
            _motor.Freeze(); _motor.Apply(position, Vector2.zero, 0); _motor.Resume(); Physics2D.SyncTransforms();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _scene = SceneManager.CreateScene("LedgeTest-" + Guid.NewGuid().ToString("N"), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            _physics = _scene.GetPhysicsScene2D();
        }
        [UnityTearDown]
        public IEnumerator TearDown() { yield return SceneManager.UnloadSceneAsync(_scene); }

        [TestCase(1)] [TestCase(-1)]
        public void RealCornerQueryIsReadOnlyAndSupportsBothDirections(int direction)
        {
            var platform = Arrange(direction); var before = _motor.Position;
            Assert.That(Find(direction, out var candidate), Is.True);
            Assert.That(candidate.Surface, Is.SameAs(platform)); Assert.That(candidate.Corner.y, Is.EqualTo(1).Within(.001f));
            Assert.That(candidate.HangPosition.x, Is.EqualTo(direction * .08f).Within(.001f));
            Assert.That(candidate.HangPosition.y, Is.EqualTo(.325f).Within(.001f));
            Assert.That(_probe.CanClimb(candidate, 1, .02f), Is.True);
            Assert.That(_motor.Position, Is.EqualTo(before)); Assert.That(_motor.Velocity, Is.EqualTo(Vector2.zero));
        }
        [TestCase("trigger")] [TestCase("one-way")] [TestCase("kinematic")] [TestCase("dynamic")]
        public void UnsupportedPlatformsAreRejected(string kind)
        {
            var platform = Arrange();
            if (kind == "trigger") platform.isTrigger = true;
            else if (kind == "one-way")
            { platform.gameObject.AddComponent<PlatformEffector2D>().useOneWay = true; platform.usedByEffector = true; }
            else platform.gameObject.AddComponent<Rigidbody2D>().bodyType = kind == "dynamic" ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            Physics2D.SyncTransforms(); Assert.That(Find(1, out _), Is.False);
        }
        [Test]
        public void TallWallHasNoReachableTopAndDistantWallIsNotGrabbable()
        {
            var platform = Arrange(); platform.size = new Vector2(4, 6); Physics2D.SyncTransforms();
            Assert.That(Find(1, out _), Is.False);
            platform.size = new Vector2(4, 2); platform.transform.position += Vector3.right; Physics2D.SyncTransforms();
            Assert.That(Find(1, out _), Is.False);
        }
        [Test]
        public void HeadBlockRejectsHangWhileClimbOnlyBlockAllowsHang()
        {
            Arrange(); var block = Box("head", new Vector2(.08f, 1.17f), new Vector2(.3f, .1f)); Physics2D.SyncTransforms();
            Assert.That(Find(1, out _), Is.False);
            block.transform.position = new Vector2(1.2f, 2.4f); Physics2D.SyncTransforms();
            Assert.That(Find(1, out var candidate), Is.True); Assert.That(_probe.CanClimb(candidate, 1, .02f), Is.False);
        }
        [Test]
        public void CandidateInvalidatesOnMovementDisableDestroyAndBodyTypeChange()
        {
            var platform = Arrange(); Assert.That(Find(1, out var c), Is.True);
            platform.transform.position += Vector3.right; Physics2D.SyncTransforms(); Assert.That(c.IsValid, Is.False);
            platform.transform.position -= Vector3.right; Physics2D.SyncTransforms(); Assert.That(c.IsValid, Is.True);
            platform.enabled = false; Assert.That(c.IsValid, Is.False); platform.enabled = true;
            var body = platform.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; Assert.That(c.IsValid, Is.False);
            UnityEngine.Object.DestroyImmediate(platform.gameObject); Assert.That(c.IsValid, Is.False);
        }
        [Test]
        public void SweptMotionMovesThroughPhysicsAndStopsBeforeObstacle()
        {
            Arrange(); _motor.BeginStep();
            Assert.That(_motor.SetSweptDisplacement(Vector2.right, .02f, 1, .02f, out var allowed), Is.False);
            Assert.That(allowed.x, Is.InRange(.07f, .081f)); Assert.That(_motor.Position.x, Is.Zero);
            _motor.ExecuteStep(); Assert.That(_physics.Simulate(.02f), Is.True);
            Assert.That(_motor.Position.x, Is.EqualTo(allowed.x).Within(.003f)); Assert.That(_motor.Body.gravityScale, Is.Zero);
        }
        [Test]
        public void ClimbUsesVerticalThenHorizontalSweepsAndDetectsNewBlock()
        {
            Arrange(); Assert.That(Find(1, out var c), Is.True); Move(c.HangPosition);
            _motor.BeginStep(); Assert.That(_motor.SetSweptDisplacement(c.RaisedPosition - _motor.Position, .2f, 1, .01f, out _), Is.True);
            _motor.ExecuteStep(); _physics.Simulate(.2f); Assert.That(_motor.Position.y, Is.EqualTo(c.RaisedPosition.y).Within(.01f));
            Box("new blocker", new Vector2(.8f, 2), new Vector2(.1f, 2)); Physics2D.SyncTransforms();
            _motor.BeginStep(); Assert.That(_motor.SetSweptDisplacement(c.StandPosition - _motor.Position, .2f, 1, .01f, out _), Is.False);
            _motor.ExecuteStep(); _physics.Simulate(.2f); Assert.That(_motor.Position.x, Is.LessThan(.26f));
        }
        [Test]
        public void PauseRejectsMovementAndResumeCanUseFreshCommand()
        {
            Arrange(); var before = _motor.Position; _motor.Freeze();
            Assert.That(_motor.SetSweptDisplacement(Vector2.up, .02f, 1, .02f, out var allowed), Is.False);
            Assert.That(allowed, Is.EqualTo(Vector2.zero)); _motor.ExecuteStep(); _physics.Simulate(.02f);
            Assert.That(_motor.Position, Is.EqualTo(before)); _motor.Resume(); _motor.BeginStep();
            Assert.That(_motor.SetSweptDisplacement(Vector2.up * .1f, .02f, 1, .02f, out _), Is.True);
            _motor.ExecuteStep(); _physics.Simulate(.02f); Assert.That(_motor.Position.y, Is.EqualTo(before.y + .1f).Within(.001f));
        }
        [Test]
        public void InvalidQueriesAndCommandsDoNotChangeBody()
        {
            Arrange(); var before = _motor.Position;
            Assert.Throws<ArgumentOutOfRangeException>(() => _probe.TryFind(0, 1, .2f, .4f, .2f, .02f, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => _motor.SetSweptDisplacement(Vector2.one, 0, 1, .02f, out _));
            Assert.That(_motor.Position, Is.EqualTo(before));
        }
        [Test]
        public void NarrowTopAllowsHangButCannotSupportClimbDestination()
        {
            var platform = Arrange(); platform.size = new Vector2(.2f, 2);
            platform.transform.position = new Vector2(.7f, 0); Physics2D.SyncTransforms();
            Assert.That(Find(1, out var c), Is.True); Assert.That(_probe.CanClimb(c, 1, .02f), Is.False);
        }
        [Test]
        public void PolygonUsesActualNearTopInsteadOfHighestBoundsPoint()
        {
            var box = Arrange(); var go = box.gameObject; UnityEngine.Object.DestroyImmediate(box);
            var polygon = go.AddComponent<PolygonCollider2D>();
            polygon.points = new[] { new Vector2(-2,-1), new Vector2(2,-1), new Vector2(2,2), new Vector2(0,2), new Vector2(0,1), new Vector2(-2,1) };
            Physics2D.SyncTransforms(); Assert.That(Find(1, out var c), Is.True);
            Assert.That(c.Corner.y, Is.EqualTo(1).Within(.001f)); Assert.That(polygon.bounds.max.y, Is.EqualTo(2).Within(.001f));
            polygon.points = new[] { new Vector2(-2,-1), new Vector2(2,-1), new Vector2(2,2), new Vector2(0,2), new Vector2(0,.9f), new Vector2(-2,.9f) };
            Physics2D.SyncTransforms(); Assert.That(c.IsValid, Is.False, "同包围盒形状改变也必须失效");
        }
        [Test]
        public void OffsetActorAndAdditionalColliderParticipateInClearance()
        {
            Arrange(); _actor.offset = new Vector2(.1f, 0); Physics2D.SyncTransforms();
            Assert.That(Find(1, out var c), Is.True); Assert.That(c.HangPosition.x, Is.EqualTo(-.02f).Within(.002f));
            var child = Box("extra shape", new Vector2(-.4f, .2f), new Vector2(.2f, 1.75f));
            child.transform.SetParent(_actor.transform, true);
            Box("left obstruction", new Vector2(-.55f, .8f), new Vector2(.1f, .5f)); Physics2D.SyncTransforms();
            Assert.That(Find(1, out _), Is.False);
        }
        [Test]
        public void ZeroDisplacementHoldsAndOrdinaryCommandsRestoreControl()
        {
            Arrange(); _motor.Body.linearVelocity = Vector2.up;
            _motor.BeginStep(); Assert.That(_motor.SetSweptDisplacement(Vector2.zero, .02f, 1, .02f, out _), Is.True);
            _motor.ExecuteStep(); _physics.Simulate(.02f); Assert.That(_motor.Position.y, Is.EqualTo(.2f).Within(.001f));
            _motor.BeginStep(); _motor.SetHorizontalVelocity(-2); _motor.SetVerticalVelocity(0); _motor.SetGravityScale(1);
            _motor.ExecuteStep(); _physics.Simulate(.02f);
            Assert.That(_motor.Position.x, Is.LessThan(0)); Assert.That(_motor.Velocity.y, Is.LessThan(0));
        }
        #endregion
    }
}
