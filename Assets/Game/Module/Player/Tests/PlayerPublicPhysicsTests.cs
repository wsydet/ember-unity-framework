using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [Test]
        public void ExternalMotion_OpposingVelocityAtZeroRetainsSelfVelocity()
        {
            var player = Character(new Vector2(0, 10));
            Keys(Key.A);
            player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("opposing", PlayerExternalMotionMode.AddVelocity, new Vector2(5, 0)));
            Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero.Within(.001f));
            Assert.That(player.Context.ExternalMotion.SelfVelocity.x, Is.EqualTo(-5).Within(.001f));
        }

        [Test]
        public void ExternalMotion_WallContactDoesNotCreateReverseResidual()
        {
            var player = Character(); Land(player);
            var wall = Keep(new GameObject("External motion wall"));
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(1, 2);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.5f, 6);
            Physics2D.SyncTransforms();
            var lease = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("wall source", PlayerExternalMotionMode.AddVelocity, new Vector2(5, 0)));
            for (int i = 0; i < 20; i++) Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero.Within(.001f));
            Assert.That(player.Context.ExternalMotion.SelfVelocity.x, Is.Zero.Within(.001f));
            lease.Dispose(); Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero.Within(.001f));
        }

        private void TickCollapsePhysics(CollapsingPlatform2D platform)
        {
            Physics2D.SyncTransforms();
            typeof(CollapsingPlatform2D).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(platform, null);
            Assert.That(_scene.GetPhysicsScene2D().Simulate(Step), Is.True);
        }

        [Test]
        public void Region_OverlapPriorityDisableAndTeleportReevaluate()
        {
            var player = Character(); Land(player);
            var regions = new EntityRegion2D[2];
            for (int i = 0; i < 2; i++)
            {
                var go = Keep(new GameObject("Region " + i)); SceneManager.MoveGameObjectToScene(go, _scene);
                var area = go.AddComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(10, 10);
                regions[i] = go.AddComponent<EntityRegion2D>();
                SetAirField(regions[i], "_area", area); SetAirField(regions[i], "_regionId", "region-" + i);
                SetAirField(regions[i], "_priority", i);
            }
            var tracker = player.gameObject.AddComponent<PlayerRegionTracker2D>();
            SetAirField(tracker, "_regions", regions); Physics2D.SyncTransforms(); tracker.Reevaluate();
            Assert.That(tracker.Current, Is.SameAs(regions[1]));
            regions[1].enabled = false; tracker.Reevaluate(); Assert.That(tracker.Current, Is.SameAs(regions[0]));
            player.Context.Motor.Body.position = new Vector2(50, 50); tracker.Reevaluate(); Assert.That(tracker.Current, Is.Null);
        }

        [Test]
        public void CollapsingPlatform_RealProbesPauseOccupancyAndOwnerDisable()
        {
            var platform = CollapseFixture();
            SceneManager.MoveGameObjectToScene(platform.gameObject, _scene);
            platform.transform.position = new Vector3(0, 10);
            var colliders = platform.GetComponents<BoxCollider2D>();
            colliders[0].size = new Vector2(3, .5f);
            colliders[1].size = new Vector2(3, .2f);
            colliders[1].offset = new Vector2(0, .4f);
            colliders[2].size = new Vector2(3, .5f);
            TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Ready));
            var occupant = Keep(new GameObject("Occupant"));
            SceneManager.MoveGameObjectToScene(occupant, _scene);
            occupant.transform.position = new Vector3(0, 10.6f);
            occupant.AddComponent<BoxCollider2D>().size = new Vector2(.5f, .5f);
            occupant.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Counting));
            platform.SetPaused(true);
            float remaining = platform.RemainingSeconds;
            for (int i = 0; i < 10; i++) TickCollapsePhysics(platform);
            Assert.That(platform.RemainingSeconds, Is.EqualTo(remaining));
            platform.SetPaused(false);
            for (int i = 0; i < 6; i++) TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Hidden));
            occupant.GetComponent<Rigidbody2D>().position = new Vector2(0, 10);
            for (int i = 0; i < 20; i++) TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.WaitingForClearance));
            Assert.That(colliders[0].enabled, Is.False);
            occupant.SetActive(false);
            TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Ready));
            platform.enabled = false;
            Assert.That(colliders[0].enabled, Is.False);
            platform.enabled = true;
            TickCollapsePhysics(platform);
            Assert.That(platform.Phase, Is.EqualTo(CollapsingPlatformPhase.Ready));
        }
    }
}

