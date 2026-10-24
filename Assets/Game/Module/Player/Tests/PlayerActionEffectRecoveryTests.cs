#if UNITY_EDITOR
using System.Collections;
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
        [UnityTest] public IEnumerator SchemeActionEffectsRemainFrozenAcrossSaveLoadRecoverAndNewHostUsesEdits()
            => ActionEffectRecovery().ToCoroutine();
        private async UniTask ActionEffectRecovery()
        {
            var source = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            var scheme = Keep(UnityEngine.Object.Instantiate(source.Scheme));
            var tail = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            PlayerDashEffectTestAssets.Set(tail, "_id", "frozen.tail");
            PlayerDashEffectTestAssets.Set(tail, "_lifetime", EntityEffectLifetime.Timed);
            var effects = Keep(ScriptableObject.CreateInstance<PlayerActionEffects>());
            effects.Configure(new[] { new PlayerActionEffectSlot("ground-dash", null, tail), new PlayerActionEffectSlot("air-dash", null, tail) });
            PlayerDashEffectTestAssets.Set(scheme, "_actionEffects", effects);
            PlayerDashEffectTestAssets.Set(scheme, "_enableEffects", true);
            PlayerDashEffectTestAssets.Set(_config, "_scheme", scheme);
            var module = Module(); var controller = FormalCharacter(); var scope = Scope(module, controller);
            var session = await scope.PrepareAsync(401, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start();
            var host = new RecoveryFlowHost(module, session, scope.GetComponent<PlayerScopeDependencyProbe>());
            var flow = new EmberStateMachine(); flow.Register(host); flow.Start<RecoveryFlowHost>();
            try
            {
                string original = controller.Context.FrozenActionEffects;
                Assert.That(original, Does.Contain("frozen.tail / 1s"));
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                PlayerDashEffectTestAssets.Set(tail, "_duration", 7f);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(controller.Context.FrozenActionEffects, Is.EqualTo(original));
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(controller.Context.FrozenActionEffects, Is.EqualTo(original));
                var secondScheme = Keep(UnityEngine.Object.Instantiate(scheme));
                PlayerDashEffectTestAssets.Set(_config, "_scheme", secondScheme);
                var second = Module(); var other = FormalCharacter(); var candidate = await Prepare(second, other);
                ((IEmberModule)second).OnInit(); second.StartSession(candidate);
                Assert.That(other.Context.FrozenActionEffects, Does.Contain("frozen.tail / 7s"));
                Assert.That(other.ObservationGraph, Is.SameAs(controller.ObservationGraph));
                Assert.That(other.Context.Effects, Is.Not.SameAs(controller.Context.Effects));
                second.StopSession(); Assert.That(controller.Context.FrozenActionEffects, Is.EqualTo(original));
            }
            finally { host.Session.Dispose(); }
        }
    }
}
#endif
