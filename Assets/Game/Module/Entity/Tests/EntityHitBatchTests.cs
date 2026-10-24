using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Entity.Tests
{
    public sealed class EntityHitBatchTests
    {
        private readonly List<GameObject> _objects = new();
        private sealed class Reactions : IEntityReactionSink
        {
            internal int Count;
            public bool Request(in EntityHit hit) { Count++; return true; }
        }
        private Entity2D Entity(string id)
        { var go = new GameObject(id); _objects.Add(go); return go.AddComponent<Entity2D>(); }
        [TearDown] public void Cleanup() { foreach (var go in _objects) UnityEngine.Object.DestroyImmediate(go); _objects.Clear(); }
        [Test]
        public void StableBatchAllowsTradeAndDeadTargetRejectsLaterCandidates()
        {
            using var batch = new EntityHitBatch();
            var aHealth = new EntityHealth(1, 1); var bHealth = new EntityHealth(1, 1);
            var a = batch.Register(Entity("a"), "a", 1, new EntityHitReceiver(aHealth, null, null));
            var b = batch.Register(Entity("b"), "b", 2, new EntityHitReceiver(bHealth, null, null));
            var hit = new EntityHit(new EntityHitPayload(), 1); var outcomes = new List<EntityHitOutcome>();
            batch.Collect(b, 1, "slash", a, hit, result => outcomes.Add(result.Outcome));
            batch.Collect(a, 1, "slash", b, hit, result => outcomes.Add(result.Outcome));
            batch.Collect(a, 2, "later", b, hit, result => outcomes.Add(result.Outcome));
            batch.Resolve();
            Assert.That(aHealth.Current, Is.Zero); Assert.That(bHealth.Current, Is.Zero);
            Assert.That(outcomes, Is.EqualTo(new[] { EntityHitOutcome.Lethal, EntityHitOutcome.Rejected, EntityHitOutcome.Lethal }));
        }
        [Test]
        public void NewProtectionStopsNextHitButNotCurrentReactionAndDoesNotRefreshOnReject()
        {
            using var batch = new EntityHitBatch(); using var effects = new EntityEffectContainer();
            var health = new EntityHealth(10, 10); var reactions = new Reactions();
            var definition = ScriptableObject.CreateInstance<EntityEffectDefinition>();
            EntityEffectSpec protection;
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_id\":\"hurt\",\"_lifetime\":0,\"_duration\":0.5,\"_damageCategories\":[\"physical\"],\"_preventsStagger\":true,\"_hasAppearance\":true,\"_alpha\":0.5,\"_blinkHz\":5}", definition);
                protection = definition.Capture();
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
            var receiver = new EntityHitReceiver(health, effects, reactions, protection: protection);
            var target = batch.Register(Entity("target"), "target", 2, receiver);
            var source = batch.Register(Entity("source"), "source", 1, new EntityHitReceiver(null, null, null));
            var hit = new EntityHit(new EntityHitPayload(), 1);
            batch.Collect(source, 1, "one", target, hit); batch.Collect(source, 1, "two", target, hit); batch.Resolve();
            Assert.That(health.Current, Is.EqualTo(9)); Assert.That(reactions.Count, Is.EqualTo(1)); Assert.That(effects.Count, Is.EqualTo(1));
            effects.Tick(.2f); float remaining = effects.GetAt(0).RemainingSeconds;
            Assert.That(receiver.Receive(hit).Accepted, Is.False); Assert.That(effects.GetAt(0).RemainingSeconds, Is.EqualTo(remaining));
        }
        [Test]
        public void ReactionWithoutHealthIsAcceptedAndReplacementInvalidatesOldTarget()
        {
            using var batch = new EntityHitBatch(); var reactions = new Reactions();
            var receiver = new EntityHitReceiver(null, null, reactions);
            var entity = Entity("target"); var target = batch.Register(entity, "target", 2, receiver);
            var source = batch.Register(Entity("source"), "source", 1, new EntityHitReceiver(null, null, null));
            var hit = new EntityHit(new EntityHitPayload(), 1);
            Assert.That(receiver.Receive(hit).Outcome, Is.EqualTo(EntityHitOutcome.Reaction));
            batch.Collect(source, 1, "one", target, hit); target.Dispose();
            batch.Register(entity, "target", 2, receiver); batch.Resolve(); Assert.That(reactions.Count, Is.EqualTo(1));
        }
        [Test]
        public void DamageModesAndExplicitNonLethalPolicyAreIndependentOfReaction()
        {
            var hit = new EntityHit(new EntityHitPayload(), 1); var health = new EntityHealth(10, 10);
            var receiver = new EntityHitReceiver(health, null, null, multiplier: 3, reduction: 1);
            Assert.That(receiver.Receive(hit).Damage, Is.EqualTo(2));
            receiver = new EntityHitReceiver(health, null, null, fixedDamage: true, fixedValue: 20, nonLethal: true);
            Assert.That(receiver.Receive(hit).Damage, Is.EqualTo(7)); Assert.That(health.Current, Is.EqualTo(1));
            Assert.That(receiver.IsDead, Is.False);
        }
    }
}
