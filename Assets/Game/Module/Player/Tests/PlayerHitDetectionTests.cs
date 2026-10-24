#if UNITY_EDITOR
using System.Linq;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private Entity2D HitEntity(string name, Vector2 position, bool colliders)
        {
            var go = Keep(new GameObject(name)); SceneManager.MoveGameObjectToScene(go, _scene); go.transform.position = position;
            var entity = go.AddComponent<Entity2D>();
            if (colliders)
            {
                go.AddComponent<BoxCollider2D>().size = Vector2.one * .2f;
                var child = new GameObject("SecondHurtbox"); child.transform.SetParent(go.transform, false);
                child.AddComponent<CircleCollider2D>().radius = .1f;
            }
            return entity;
        }
        private PlayerAttackSegment HitSegment(string shape, bool retryAccepted = false, bool shortWindow = false)
        {
            string start = shortWindow ? "0.04" : "0.01", end = shortWindow ? "0.06" : "0.3";
            var segment = JsonUtility.FromJson<PlayerAttackSegment>("{\"_id\":\"hit\",\"_abilityId\":\"test\",\"_recoveryId\":\"test\",\"_duration\":0.5,\"_windupEnd\":0.1,\"_activeEnd\":0.3," +
                "\"_windows\":[{\"_id\":\"one\",\"_start\":" + start + ",\"_end\":" + end + ",\"_layers\":-1,\"_retry\":" + (retryAccepted ? "1" : "0") +
                ",\"_payload\":{\"_category\":\"physical\",\"_damage\":1,\"_stunSeconds\":0,\"_braking\":30},\"_keyframes\":[{\"_time\":" + start + ",\"_shapes\":[" + shape + "]}]}]}");
            segment.Windows[0].Validate(segment.Duration, "test"); return segment;
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void NativeHitShapesSweepAcrossCompleteShortWindowAndDeduplicateEntity(int kind)
        {
            using var batch = new EntityHitBatch();
            var source = batch.Register(HitEntity("source", new Vector2(-5, 3), false), "source", 1, new EntityHitReceiver(null, null, null));
            var health = new EntityHealth(10, 10);
            var target = batch.Register(HitEntity("target", new Vector2(0, 3), true), "target", 2, new EntityHitReceiver(health, null, null));
            Physics2D.SyncTransforms();
            var shape = "{\"_kind\":" + kind + ",\"_size\":{\"x\":0.2,\"y\":0.2},\"_radius\":0.1,\"_points\":[{\"x\":-0.1,\"y\":-0.1},{\"x\":0.1,\"y\":-0.1},{\"x\":0,\"y\":0.1}]}";
            var segment = HitSegment(shape, shortWindow: true);
            using var detector = new PlayerAttackHitSession(batch, source); detector.Begin(segment, 1, new Vector2(-5, 3));
            detector.Sample(0, .1, new Vector2(5, 3));
            Assert.That(health.Current, Is.EqualTo(10), "查询期间不结算"); batch.Resolve();
            Assert.That(health.Current, Is.EqualTo(9), "跨完整短窗口仍扫掠，多个Collider只扣一次");
            detector.Sample(.1, .2, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(9));
        }
        [TestCase(false, 10)] [TestCase(true, 9)]
        public void WindowRetryPolicyUsesAcceptedResultAndIgnoresBodyContactExclusion(bool retry, int expected)
        {
            using var batch = new EntityHitBatch(); using var effects = new EntityEffectContainer();
            var source = batch.Register(HitEntity("source", new Vector2(0, 3), true), "source", 1, new EntityHitReceiver(null, null, null));
            foreach (var collider in source.Entity.GetComponentsInChildren<Collider2D>()) collider.excludeLayers = -1;
            var health = new EntityHealth(10, 10);
            batch.Register(HitEntity("target", new Vector2(.2f, 3), true), "target", 2, new EntityHitReceiver(health, effects, null));
            var protection = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            JsonUtility.FromJsonOverwrite("{\"_id\":\"protection\",\"_duration\":0.1,\"_lifetime\":0,\"_damageCategories\":[\"physical\"]}", protection);
            effects.ApplyIndependent(protection, "test"); Physics2D.SyncTransforms();
            var segment = HitSegment("{\"_kind\":0,\"_size\":{\"x\":1,\"y\":1}}", retry);
            using var detector = new PlayerAttackHitSession(batch, source); detector.Begin(segment, 1, new Vector2(0, 3));
            detector.Sample(0, .02, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(10));
            effects.Tick(.2f); detector.Sample(.02, .04, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(expected));
            detector.Sample(.04, .06, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(expected));
        }

        [Test]
        public void HitFiltersSelfFriendlyAndLayerWithoutRecordingExcludedTarget()
        {
            using var batch = new EntityHitBatch();
            var selfHealth = new EntityHealth(10, 10);
            var source = batch.Register(HitEntity("source", new Vector2(0, 3), true), "source", 1, new EntityHitReceiver(selfHealth, null, null));
            var friendly = new EntityHealth(10, 10);
            batch.Register(HitEntity("friend", new Vector2(.1f, 3), true), "friend", 1, new EntityHitReceiver(friendly, null, null));
            var health = new EntityHealth(10, 10);
            var enemy = HitEntity("enemy", new Vector2(.2f, 3), true);
            foreach (var collider in enemy.GetComponentsInChildren<Collider2D>()) collider.gameObject.layer = 8;
            batch.Register(enemy, "enemy", 2, new EntityHitReceiver(health, null, null));
            var segment = HitSegment("{\"_kind\":0,\"_size\":{\"x\":2,\"y\":2}}");
            PlayerDashEffectTestAssets.Set(segment.Windows[0], "_layers", (LayerMask)1);
            Physics2D.SyncTransforms();
            using var detector = new PlayerAttackHitSession(batch, source);
            detector.Begin(segment, 1, new Vector2(0, 3)); detector.Sample(0, .02, new Vector2(0, 3)); batch.Resolve();
            Assert.That(selfHealth.Current, Is.EqualTo(10)); Assert.That(friendly.Current, Is.EqualTo(10));
            Assert.That(health.Current, Is.EqualTo(10));
            foreach (var collider in enemy.GetComponentsInChildren<Collider2D>()) collider.gameObject.layer = 0;
            Physics2D.SyncTransforms();
            detector.Sample(.02, .04, new Vector2(0, 3)); batch.Resolve();
            Assert.That(health.Current, Is.EqualTo(9), "被层过滤的目标不能提前写入窗口账本");
            Assert.That(selfHealth.Current + friendly.Current, Is.EqualTo(20));
        }

        [Test]
        public void ShapeKeyframeChangesKeepLedgerWhileNewWindowAndAttackResetIt()
        {
            using var batch = new EntityHitBatch();
            var source = batch.Register(HitEntity("source", new Vector2(0, 3), false), "source", 1, new EntityHitReceiver(null, null, null));
            var health = new EntityHealth(10, 10);
            batch.Register(HitEntity("target", new Vector2(.2f, 3), true), "target", 2, new EntityHitReceiver(health, null, null));
            var segment = HitSegment("{\"_kind\":0,\"_size\":{\"x\":1,\"y\":1}}");
            var first = segment.Windows[0];
            var changed = JsonUtility.FromJson<PlayerHitKeyframe>("{\"_time\":0.03,\"_shapes\":[{\"_kind\":1,\"_radius\":1}]}");
            PlayerDashEffectTestAssets.Set(first, "_keyframes", new[] { first.Keyframes[0], changed });
            var second = JsonUtility.FromJson<PlayerHitWindow>(JsonUtility.ToJson(first));
            PlayerDashEffectTestAssets.Set(second, "_id", "two");
            PlayerDashEffectTestAssets.Set(second, "_start", .08f);
            PlayerDashEffectTestAssets.Set(second, "_singleSample", true);
            PlayerDashEffectTestAssets.Set(second, "_keyframes", new[] { JsonUtility.FromJson<PlayerHitKeyframe>("{\"_time\":0.08,\"_shapes\":[{\"_kind\":1,\"_radius\":1}]}") });
            PlayerDashEffectTestAssets.Set(segment, "_windows", new[] { first, second });
            foreach (var window in segment.Windows) window.Validate(segment.Duration, "test");
            Physics2D.SyncTransforms();
            using var detector = new PlayerAttackHitSession(batch, source); detector.Begin(segment, 1, new Vector2(0, 3));
            detector.Sample(0, .02, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(9));
            detector.Sample(.02, .04, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(9));
            detector.Sample(.04, .1, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(8));
            detector.Sample(.1, .2, new Vector2(0, 3)); batch.Resolve(); Assert.That(health.Current, Is.EqualTo(8));
            detector.Begin(segment, 1, new Vector2(0, 3)); detector.Sample(0, .02, new Vector2(0, 3)); batch.Resolve();
            Assert.That(health.Current, Is.EqualTo(7));
        }

        [TestCase(0, -1)] [TestCase(0, 1)]
        [TestCase(1, -1)] [TestCase(1, 1)]
        [TestCase(2, -1)] [TestCase(2, 1)]
        public void NativeHitGeometryMirrorsOffsetAndShapeInBothDirections(int kind, int direction)
        {
            using var batch = new EntityHitBatch();
            var source = batch.Register(HitEntity("source", new Vector2(0, 3), false), "source", 1, new EntityHitReceiver(null, null, null));
            var intended = new EntityHealth(10, 10); var opposite = new EntityHealth(10, 10);
            batch.Register(HitEntity("intended", new Vector2(direction, 3), true), "intended", 2, new EntityHitReceiver(intended, null, null));
            batch.Register(HitEntity("opposite", new Vector2(-direction, 3), true), "opposite", 2, new EntityHitReceiver(opposite, null, null));
            var segment = HitSegment("{\"_kind\":" + kind + ",\"_offset\":{\"x\":1,\"y\":0},\"_size\":{\"x\":0.4,\"y\":0.4},\"_radius\":0.2,\"_points\":[{\"x\":-0.2,\"y\":-0.2},{\"x\":0.2,\"y\":-0.2},{\"x\":0,\"y\":0.2}]}");
            Physics2D.SyncTransforms();
            using var detector = new PlayerAttackHitSession(batch, source);
            detector.Begin(segment, direction, new Vector2(0, 3)); detector.Sample(0, .02, new Vector2(0, 3)); batch.Resolve();
            Assert.That(intended.Current, Is.EqualTo(9)); Assert.That(opposite.Current, Is.EqualTo(10));
        }

        [Test]
        public void RealAttackCollectsIntoSceneBatchAndReplacementPreservesDummyHealth()
        {
            WithThreeHit((player, scheme) =>
            {
                var dummy = HitEntity("dummy", player.Context.Motor.Position + Vector2.right * .2f, true).gameObject.AddComponent<EntityHitActor>();
                PlayerDashEffectTestAssets.Set(dummy, "_stableId", "dummy");
                using var scene = new PlayerHitScene(_scene, player.Context);
                Physics2D.SyncTransforms(); Keys(UnityEngine.InputSystem.Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                scene.Step(() => player.FixedTick(.02f));
                Assert.That(scene.Actors.Single().Health.Current, Is.EqualTo(99));
                Assert.That(scene.Actors.Single().PendingReaction.HasValue, Is.True);
                scene.Bind(player.Context);
                Assert.That(scene.Actors.Single().Health.Current, Is.EqualTo(99));
            }, scheme =>
            {
                var window = HitSegment("{\"_kind\":0,\"_size\":{\"x\":2,\"y\":2}}").Windows[0];
                PlayerDashEffectTestAssets.Set(window.Payload, "_stunSeconds", .2f);
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0], "_windows", new[] { window });
            });
        }
    }
}
#endif
