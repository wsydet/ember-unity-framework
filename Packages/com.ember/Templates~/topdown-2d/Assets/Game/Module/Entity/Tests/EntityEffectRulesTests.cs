using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Entity.Tests
{
    public sealed class EntityEffectRulesTests
    {
        #region 内部参数
        private readonly List<UnityEngine.Object> _objects = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private T Keep<T>(T item) where T : UnityEngine.Object { _objects.Add(item); return item; }
        private void Set(object item, string field, object value)
            => item.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(item, value);
        private EntityEffectDefinition Definition(string[] damage = null, bool stagger = false, int layers = 0,
            bool appearance = false, int priority = 0, float alpha = 1, float hz = 0, float duration = 2)
        {
            var result = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            Set(result, "_id", "test.effect"); Set(result, "_duration", duration);
            Set(result, "_damageCategories", damage ?? Array.Empty<string>());
            Set(result, "_preventsStagger", stagger); Set(result, "_excludedLayers", (LayerMask)layers);
            Set(result, "_hasAppearance", appearance); Set(result, "_appearancePriority", priority);
            Set(result, "_alpha", alpha); Set(result, "_blinkHz", hz);
            return result;
        }
        private EntityEffectBinding Bind(EntityEffectContainer effects, out BoxCollider2D collider, out SpriteRenderer sprite)
        {
            var go = Keep(new GameObject("EffectHost"));
            collider = go.AddComponent<BoxCollider2D>(); collider.excludeLayers = 1 << 10;
            sprite = go.AddComponent<SpriteRenderer>(); sprite.color = new Color(.2f, .3f, .4f, .8f);
            return new EntityEffectBinding(effects, new Collider2D[] { collider }, new[] { sprite });
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [TearDown] public void Cleanup()
        { foreach (var item in _objects) if (item != null) UnityEngine.Object.DestroyImmediate(item); _objects.Clear(); }

        [Test] public void ProtectionKindsAreIndependentAndCombineEverySource()
        {
            using var effects = new EntityEffectContainer();
            var damage = effects.ApplyIndependent(Definition(new[] { "test.contact" }), "hit");
            var physics = effects.ApplyIndependent(Definition(layers: 1 << 8), "dash");
            var stagger = effects.ApplyIndependent(Definition(stagger: true), "story");
            Assert.That(effects.ProtectsDamage("test.contact"), Is.True);
            Assert.That(effects.ProtectsDamage("test.projectile"), Is.False);
            Assert.That(effects.ProtectsDamage(null), Is.False);
            Assert.That(effects.PreventsStagger, Is.True); Assert.That(effects.ExcludedLayers, Is.EqualTo(1 << 8));
            damage.Dispose(); Assert.That(effects.ProtectsDamage("test.contact"), Is.False);
            Assert.That(effects.PreventsStagger, Is.True); Assert.That(physics.IsActive, Is.True);
            stagger.Dispose(); Assert.That(effects.PreventsStagger, Is.False);
        }
        [Test] public void RepeatedProtectionDoesNotRefreshOrAddStrength()
        {
            using var effects = new EntityEffectContainer();
            var definition = Definition(new[] { "hit" }, layers: 1 << 8, duration: 1);
            var first = effects.ApplyIndependent(definition, "same"); effects.Tick(.5f);
            var second = effects.ApplyIndependent(definition, "same"); effects.Tick(.5f);
            Assert.That(first.IsActive, Is.False); Assert.That(second.IsActive, Is.True);
            Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(.5f));
            Assert.That(effects.ExcludedLayers, Is.EqualTo(1 << 8));
            effects.Tick(.5f); Assert.That(effects.ProtectsDamage("hit"), Is.False);
        }
        [Test] public void RemovingOneCollisionSourcePreservesOtherSourcesAndBaseline()
        {
            using var effects = new EntityEffectContainer(); using var binding = Bind(effects, out var collider, out _);
            var first = effects.ApplyIndependent(Definition(layers: (1 << 8) | (1 << 9)), "first");
            var second = effects.ApplyIndependent(Definition(layers: 1 << 9), "second");
            Assert.That(collider.excludeLayers.value, Is.EqualTo((1 << 8) | (1 << 9) | (1 << 10)));
            first.Dispose(); Assert.That(collider.excludeLayers.value, Is.EqualTo((1 << 9) | (1 << 10)));
            second.Dispose(); Assert.That(collider.excludeLayers.value, Is.EqualTo(1 << 10));
        }
        [Test] public void HighestAppearancePriorityDoesNotSuppressOtherRules()
        {
            using var effects = new EntityEffectContainer(); using var binding = Bind(effects, out var collider, out var sprite);
            var low = effects.ApplyIndependent(Definition(new[] { "hit" }, layers: 1 << 8, appearance: true, alpha: .2f), "low");
            var high = effects.ApplyIndependent(Definition(appearance: true, priority: 4, alpha: .7f), "high");
            effects.ApplyIndependent(Definition(priority: 100), "no-appearance");
            Assert.That(sprite.color.a, Is.EqualTo(.56f).Within(.00001));
            Assert.That(effects.ProtectsDamage("hit"), Is.True); Assert.That(collider.excludeLayers.value, Is.EqualTo((1 << 8) | (1 << 10)));
            high.Dispose(); Assert.That(sprite.color.a, Is.EqualTo(.16f).Within(.00001));
            low.Dispose(); Assert.That(sprite.color.a, Is.EqualTo(.8f)); Assert.That(collider.excludeLayers.value, Is.EqualTo(1 << 10));
        }
        [Test] public void EqualPriorityComparesActualFrameAlphaAndPauseDoesNotAnimate()
        {
            using var effects = new EntityEffectContainer(); using var binding = Bind(effects, out _, out var sprite);
            effects.ApplyIndependent(Definition(appearance: true, alpha: .1f, hz: 1), "blink");
            effects.ApplyIndependent(Definition(appearance: true, alpha: .4f), "constant");
            Assert.That(sprite.color.a, Is.EqualTo(.32f).Within(.00001));
            effects.Tick(.5f); Assert.That(sprite.color.a, Is.EqualTo(.08f).Within(.00001));
            effects.Tick(0); Assert.That(sprite.color.a, Is.EqualTo(.08f).Within(.00001));
            effects.Tick(.5f); Assert.That(sprite.color.a, Is.EqualTo(.32f).Within(.00001));
        }
        [TestCase("expire")] [TestCase("handle")] [TestCase("dispel")] [TestCase("dispose")]
        public void EveryRemovalPathImmediatelyRestoresOutputs(string path)
        {
            using var effects = new EntityEffectContainer(); using var binding = Bind(effects, out var collider, out var sprite);
            var handle = effects.ApplyIndependent(Definition(new[] { "hit" }, true, 1 << 8, true, alpha: .3f), "owner");
            var color = sprite.color; color.r = .9f; sprite.color = color;
            if (path == "expire") effects.Tick(2);
            else if (path == "handle") handle.Dispose();
            else if (path == "dispel") effects.RemoveAll("test.effect");
            else effects.Dispose();
            Assert.That(collider.excludeLayers.value, Is.EqualTo(1 << 10));
            Assert.That(sprite.color.a, Is.EqualTo(.8f)); Assert.That(sprite.color.r, Is.EqualTo(.9f));
            Assert.That(effects.ProtectsDamage("hit"), Is.False); Assert.That(effects.PreventsStagger, Is.False);
            handle.Dispose(); Assert.That(effects.Count, Is.Zero);
        }
        [Test] public void BindingAndHostCleanupDoNotAffectAnotherHostOrLeaveSubscriptions()
        {
            using var first = new EntityEffectContainer(); using var second = new EntityEffectContainer();
            var binding = Bind(first, out var collider, out var sprite);
            using var otherBinding = Bind(second, out var other, out _);
            var definition = Definition(layers: 1 << 8, appearance: true, alpha: .2f);
            var old = first.ApplyIndependent(definition, "same"); second.ApplyIndependent(definition, "same");
            binding.Dispose(); binding.Dispose();
            first.Tick(.3f); Assert.That(sprite.color.a, Is.EqualTo(.8f)); Assert.That(collider.excludeLayers.value, Is.EqualTo(1 << 10));
            first.Dispose(); old.Dispose(); Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(other.excludeLayers.value, Is.EqualTo((1 << 10) | (1 << 8)));
        }
        [Test] public void ParametersAreFrozenAndInvalidValuesDoNotPartiallyApply()
        {
            using var effects = new EntityEffectContainer(); var categories = new[] { "hit" };
            var definition = Definition(categories, layers: 1 << 8, appearance: true, alpha: .3f);
            effects.ApplyIndependent(definition, "source"); var snapshot = effects.CaptureIndependentTimed()[0];
            categories[0] = "changed"; Set(definition, "_alpha", .9f); Set(definition, "_excludedLayers", (LayerMask)0);
            Assert.That(snapshot.Rules.ProtectsDamage("hit"), Is.True); Assert.That(snapshot.Appearance.Alpha, Is.EqualTo(.3f));
            Assert.That(effects.ExcludedLayers, Is.EqualTo(1 << 8));
            Set(definition, "_damageCategories", new[] { " " });
            Assert.Throws<ArgumentException>(() => effects.ApplyIndependent(definition, "source"));
            Set(definition, "_damageCategories", Array.Empty<string>()); Set(definition, "_blinkHz", float.NaN);
            Assert.Throws<ArgumentOutOfRangeException>(() => effects.ApplyIndependent(definition, "source"));
            Assert.That(effects.Count, Is.EqualTo(1));
        }
        [Test] public void NotificationsObserveCommittedResultAndRejectReentrantMutation()
        {
            using var effects = new EntityEffectContainer(); var definition = Definition(); int notifications = 0;
            Action observer = () =>
            {
                notifications++; effects.CaptureActive();
                Assert.Throws<InvalidOperationException>(() => effects.ApplyIndependent(definition, "reentrant"));
            };
            effects.Changed += observer; var handle = effects.ApplyIndependent(definition, "source");
            Assert.That(notifications, Is.EqualTo(1)); handle.Dispose(); Assert.That(notifications, Is.EqualTo(2));
            effects.Changed -= observer; effects.Dispose(); Assert.That(notifications, Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator CollisionFilterChangesRealPhysicsAndRestoresContact()
        {
            var scene = SceneManager.CreateScene("EffectPhysics", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            using var effects = new EntityEffectContainer();
            using var binding = Bind(effects, out var shape, out _);
            SceneManager.MoveGameObjectToScene(shape.gameObject, scene);
            var body = shape.gameObject.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            var wall = Keep(new GameObject("TestMonsterLayer8")); wall.layer = 8;
            SceneManager.MoveGameObjectToScene(wall, scene); wall.transform.position = new Vector3(2, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 20);
            try
            {
                Physics2D.SyncTransforms();
                for (int i = 0; i < 100; i++) { body.linearVelocity = Vector2.right * 3; scene.GetPhysicsScene2D().Simulate(.02f); }
                Assert.That(body.position.x, Is.LessThan(1.1f));
                body.position = Vector2.zero; var handle = effects.ApplyIndependent(Definition(layers: 1 << 8), "pass");
                for (int i = 0; i < 100; i++) { body.linearVelocity = Vector2.right * 3; scene.GetPhysicsScene2D().Simulate(.02f); }
                Assert.That(body.position.x, Is.GreaterThan(4));
                handle.Dispose(); body.position = Vector2.zero;
                for (int i = 0; i < 100; i++) { body.linearVelocity = Vector2.right * 3; scene.GetPhysicsScene2D().Simulate(.02f); }
                Assert.That(body.position.x, Is.LessThan(1.1f));
            }
            finally { binding.Dispose(); SceneManager.UnloadSceneAsync(scene); }
            yield return null;
        }
        #endregion
    }
}
