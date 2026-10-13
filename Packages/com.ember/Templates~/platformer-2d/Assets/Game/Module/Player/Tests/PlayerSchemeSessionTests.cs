using System;
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
        [UnityTest] public IEnumerator SchemeRecoveryUsesFrozenAssemblyAndPreservesSavedResources()
            => SchemeRecoveryFrozen().ToCoroutine();
        private async UniTask SchemeRecoveryFrozen()
        {
            InstallSessionPowers();
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var before = module.Controller.Context;
                float speed = before.Feel.MoveSpeed;
                SessionSkill(module.Controller).TryCommit(SessionSkill(module.Controller).CreateAttempt());
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                PlayerDashEffectTestAssets.Set(_config.Feel.Movement, "_speed", speed + 17);
                PlayerDashEffectTestAssets.Set(_config, "_inputChannel", "changed-channel");
                PlayerDashEffectTestAssets.Set(_config.Powers.SkillSlots[0].Definition, "_id", "changed-skill");
                var target = module.RecoveryTarget;
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(module.Controller.Context.Feel.MoveSpeed, Is.EqualTo(speed));
                Assert.That(SessionSkill(module.Controller).RemainingCooldown, Is.EqualTo(10));
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(module.Controller.Context.Feel.MoveSpeed, Is.EqualTo(speed));
                Assert.That(module.RecoveryTarget.Position, Is.EqualTo(target.Position));
                Assert.That(module.Controller.Context, Is.Not.SameAs(before));
                Assert.That(module.Controller.ObservationGraph, Is.SameAs(_config.Graph));
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator NewHostCapturesEditsWithoutChangingExistingHostOrProgressCompatibility()
            => SchemeNewHost().ToCoroutine();
        private async UniTask SchemeNewHost()
        {
            InstallSessionPowers();
            var first = await Running();
            float speed = first.Controller.Context.Feel.MoveSpeed;
            Assert.That((await first.Save(0).Completion).Succeeded, Is.True);
            var target = first.RecoveryTarget;
            PlayerDashEffectTestAssets.Set(_config.Feel.Movement, "_speed", speed + 11);
            var second = await Running();
            Assert.That(second.Controller.Context.Feel.MoveSpeed, Is.EqualTo(speed + 11));
            Assert.That(first.Controller.Context.Feel.MoveSpeed, Is.EqualTo(speed));
            var firstSkill = SessionSkill(first.Controller);
            firstSkill.TryCommit(firstSkill.CreateAttempt());
            Assert.That(SessionSkill(second.Controller).RemainingCooldown, Is.Zero);
            second.StopSession();
            PlayerDashEffectTestAssets.Set(_config.Powers.SkillSlots[0].Definition, "_id", "incompatible");
            bool rejected = false;
            try { using var candidate = await Prepare(second, Character(), PlayerEntryRequest.FromSlot(0)); }
            catch (ArgumentException) { rejected = true; }
            Assert.That(rejected, Is.True);
            Assert.That(first.RecoveryTarget, Is.SameAs(target));
            Assert.That(first.HasSession, Is.True);
            Assert.That((await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion).Succeeded, Is.True);
        }

        [UnityTest] public IEnumerator FrozenAssemblyReleasedOnCandidateCancelAndStop()
            => SchemeRelease().ToCoroutine();
        private async UniTask SchemeRelease()
        {
            InstallSessionPowers();
            var module = Module();
            var candidate = await Prepare(module, Character());
            var frozen = candidate.Config;
            Assert.That(frozen, Is.Not.SameAs(_config));
            Assert.That(frozen.Powers.SkillSlots[0].Definition, Is.Not.SameAs(_config.Powers.SkillSlots[0].Definition));
            candidate.Dispose();
            Assert.That(frozen == null, Is.True);
            candidate = await Prepare(module, Character());
            frozen = candidate.Config;
            ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            module.StopSession();
            Assert.That(frozen == null, Is.True);
            Assert.That(_config != null && _config.Feel != null, Is.True);
        }
    }
}
