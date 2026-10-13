using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private void InstallEffects() => typeof(PlayerConfig).GetField("_enableEffects", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, true);
        private EntityEffectDefinition SessionEffect()
        {
            var value = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(EntityEffectDefinition).GetField("_id", flags).SetValue(value, "test.protection");
            typeof(EntityEffectDefinition).GetField("_duration", flags).SetValue(value, 10f);
            typeof(EntityEffectDefinition).GetField("_excludedLayers", flags).SetValue(value, (LayerMask)(1 << 8));
            typeof(EntityEffectDefinition).GetField("_hasAppearance", flags).SetValue(value, true);
            typeof(EntityEffectDefinition).GetField("_alpha", flags).SetValue(value, .25f);
            return value;
        }
        [UnityTest] public IEnumerator EffectsUseOnlyRunningModuleClockAndWholePause() => EffectClock().ToCoroutine();
        private async UniTask EffectClock()
        {
            InstallEffects(); var module = Module(); var controller = Character();
            var sprite = controller.gameObject.AddComponent<SpriteRenderer>();
            var candidate = await Prepare(module, controller); var effects = controller.Context.Effects;
            var handle = effects.ApplyIndependent(SessionEffect(), "test");
            controller.Tick(2); controller.FixedTick(2);
            Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(10));
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.That(controller.Context.Effects, Is.SameAs(effects));
            await UniTask.Yield(); Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(10));
            float dt = Time.deltaTime; ((IEmberUpdate)module).Update();
            float remaining = effects.CaptureIndependentTimed()[0].RemainingSeconds;
            Assert.That(remaining, Is.EqualTo(10 - dt).Within(.00001));
            for (int i = 0; i < 5; i++) ((IEmberFixedUpdate)module).FixedUpdate();
            Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(remaining));
            using (module.Pause())
            {
                await UniTask.Yield(); ((IEmberUpdate)module).Update(); controller.Tick(4); controller.FixedTick(4);
                Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.EqualTo(remaining));
                Assert.That(sprite.color.a, Is.EqualTo(.25f)); Assert.That(Time.timeScale, Is.Zero);
                Assert.That(AudioListener.pause, Is.False);
            }
            await UniTask.Yield(); ((IEmberUpdate)module).Update();
            Assert.That(effects.CaptureIndependentTimed()[0].RemainingSeconds, Is.LessThan(remaining));
            module.StopSession(); Assert.That(handle.IsActive, Is.False); Assert.That(effects.IsDisposed, Is.True);
            Assert.That(sprite.color.a, Is.EqualTo(1)); Assert.That(controller.GetComponent<Collider2D>().excludeLayers.value, Is.Zero);
        }
        [UnityTest] public IEnumerator EmptyInstalledEffectsSaveAndLoadV2() => EffectBoundary().ToCoroutine();
        private async UniTask EffectBoundary()
        {
            InstallEffects(); var module = await Running(); var controller = module.Controller; var context = controller.Context;
            Assert.That(context.Effects.Count, Is.Zero); var flow = ExitFlow(module); long version = module.SessionVersion;
            Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
            Assert.That(controller.Context, Is.SameAs(context)); Assert.That(module.SessionVersion, Is.EqualTo(version));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_data.LastSuccessfulSaveSlot, Is.EqualTo(0));
            Assert.That((await module.ExitToMenuAsync(flow, 0)).Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(context.Effects.IsDisposed, Is.True); Assert.That(module.HasSession, Is.False);
            var loaded = await Prepare(module, controller, PlayerEntryRequest.FromSlot(0));
            ((IEmberModule)module).OnInit(); module.StartSession(loaded);
            Assert.That(controller.Context.Effects, Is.Not.Null); Assert.That(controller.Context.Effects.Count, Is.Zero);
        }
        [UnityTest] public IEnumerator CancelDisableAndOldHandlesCannotLeakEffectsIntoNewSession() => EffectCleanup().ToCoroutine();
        private async UniTask EffectCleanup()
        {
            InstallEffects(); var module = Module(); var controller = Character(); var definition = SessionEffect();
            using var cancellation = new CancellationTokenSource();
            var candidate = await Prepare(module, controller, cancellation: cancellation.Token);
            var old = controller.Context.Effects; var handle = old.ApplyIndependent(definition, "source"); cancellation.Cancel();
            ((IEmberModule)module).OnInit(); Assert.Throws<OperationCanceledException>(() => module.StartSession(candidate));
            Assert.That(old.IsDisposed, Is.True); Assert.That(controller.GetComponent<Collider2D>().excludeLayers.value, Is.Zero);
            candidate = await Prepare(module, controller); module.StartSession(candidate);
            var current = controller.Context.Effects; var currentHandle = current.ApplyIndependent(definition, "source");
            handle.Dispose(); Assert.That(currentHandle.IsActive, Is.True);
            controller.gameObject.SetActive(false);
            Assert.That(module.HasSession, Is.False); Assert.That(current.IsDisposed, Is.True);
            Assert.That(controller.GetComponent<Collider2D>().excludeLayers.value, Is.Zero);
        }
        [UnityTest] public IEnumerator EffectBindingExcludesNestedEntityObjects() => EffectOwnership().ToCoroutine();
        private async UniTask EffectOwnership()
        {
            InstallEffects(); var module = Module(); var controller = Character();
            var child = Keep(new GameObject("SeparateEntity")); child.transform.SetParent(controller.transform);
            child.AddComponent<Entity2D>(); var shape = child.AddComponent<BoxCollider2D>();
            var sprite = child.AddComponent<SpriteRenderer>();
            var candidate = await Prepare(module, controller);
            controller.Context.Effects.ApplyIndependent(SessionEffect(), "parent");
            Assert.That(shape.excludeLayers.value, Is.Zero); Assert.That(sprite.color.a, Is.EqualTo(1));
            Assert.That(controller.GetComponent<Collider2D>().excludeLayers.value, Is.EqualTo(1 << 8));
            candidate.Dispose();
        }
    }
}
