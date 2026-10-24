using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [Test] public void RecoveryEntryKeepsInitialSnapshotAfterConfigurationAndResourceChanges()
        {
            InstallSessionPowers();
            using var powers = PlayerPowerSession.CreateNewGame(_config.Powers);
            var initial = new PlatformerSnapshot(_config.Scene, _config.Spawn, Vector2.zero, true,
                powers.Capture(), false, Array.Empty<Game.Entity.EntityTimedEffectSnapshot>());
            using var ledger = new PlayerRecoveryLedger(initial, 1);
            powers.Abilities.TryGet("test.air", out var ability);
            ability.TryCommit(ability.CreateAttempt()); ability.SetOpen(false);
            _config.Configure(_config.Graph, new Vector2(100, 100));
            Assert.That(ledger.Target, Is.SameAs(initial));
            Assert.That(ledger.EntrySnapshot, Is.SameAs(initial));
            Assert.That(ledger.Target.Position, Is.EqualTo(new Vector2(0, 3)));
            Assert.That(ledger.Target.Powers.GetAbility(0).RemainingUses, Is.EqualTo(2));
            Assert.That(ledger.Target.Powers.GetAbility(0).IsOpen, Is.True);
            Assert.That(ledger.TargetSlot, Is.Null);
        }

        [Test] public void RecoveryLoadedEntryKeepsActualSnapshotInsteadOfResolvingSlotAgain()
        {
            var loaded = EmptySnapshot(true);
            using var ledger = new PlayerRecoveryLedger(loaded, 7, 1);
            Assert.That(ledger.EntrySnapshot, Is.SameAs(loaded));
            Assert.That(ledger.Target, Is.SameAs(loaded));
            Assert.That(ledger.TargetSlot, Is.EqualTo(1));
        }

        [Test] public void RecoveryFailuresNeverReplaceLastCommittedTarget()
        {
            var entry = EmptySnapshot(); var committed = EmptySnapshot(true);
            using var ledger = new PlayerRecoveryLedger(entry, 1);
            var first = new ProgressRequestContext(1, 0, 1);
            Assert.That(ledger.RecordResult(ledger.Track(first, committed), first, SaveStatus.Committed), Is.True);
            long id = 1;
            foreach (SaveStatus status in Enum.GetValues(typeof(SaveStatus)))
            {
                if (status == SaveStatus.Committed) continue;
                var request = new ProgressRequestContext(++id, 1, 1);
                var ticket = ledger.Track(request, entry);
                Assert.That(ledger.Target, Is.SameAs(committed), "Tracking must not replace the target.");
                Assert.That(ledger.RecordResult(ticket, request, status), Is.False, status.ToString());
                Assert.That(ledger.RecordResult(ticket, request, SaveStatus.Committed), Is.False, "A result is terminal.");
                Assert.That(ledger.Target, Is.SameAs(committed));
                Assert.That(ledger.TargetSlot, Is.EqualTo(0));
            }
            Assert.That(ledger.EntrySnapshot, Is.SameAs(entry));
        }

        [Test] public void RecoveryLateOlderSuccessCannotReplaceNewerSuccessButCanFollowNewerFailure()
        {
            var entry = EmptySnapshot(); var older = EmptySnapshot(); var newer = EmptySnapshot(true);
            foreach (bool newerSucceeds in new[] { false, true })
            {
                using var ledger = new PlayerRecoveryLedger(entry, 1);
                var a = new ProgressRequestContext(1, 0, 1); var b = new ProgressRequestContext(2, 1, 1);
                var ta = ledger.Track(a, older); var tb = ledger.Track(b, newer);
                ledger.RecordResult(tb, b, newerSucceeds ? SaveStatus.Committed : SaveStatus.WriteFailed);
                Assert.That(ledger.RecordResult(ta, a, SaveStatus.Committed), Is.EqualTo(!newerSucceeds));
                Assert.That(ledger.Target, Is.SameAs(newerSucceeds ? newer : older));
                Assert.That(ledger.TargetSlot, Is.EqualTo(newerSucceeds ? 1 : 0));
            }
        }

        [Test] public void RecoveryResultMustMatchRequestEpochAndSlotAndIsConsumedOnce()
        {
            var entry = EmptySnapshot(); var accepted = EmptySnapshot(true);
            using var ledger = new PlayerRecoveryLedger(entry, 3);
            var request = new ProgressRequestContext(4, 1, 3); var ticket = ledger.Track(request, accepted);
            foreach (var wrong in new[] { new ProgressRequestContext(5, 1, 3),
                new ProgressRequestContext(4, 0, 3), new ProgressRequestContext(4, 1, 4) })
                Assert.That(ledger.RecordResult(ticket, wrong, SaveStatus.Committed), Is.False);
            Assert.That(ledger.Target, Is.SameAs(entry));
            Assert.That(ledger.RecordResult(ticket, request, SaveStatus.Committed), Is.True);
            Assert.That(ledger.RecordResult(ticket, request, SaveStatus.Committed), Is.False);
            Assert.That(ledger.Target, Is.SameAs(accepted));
        }

        [Test] public void RecoveryClosedSessionAndForeignTicketsCannotAffectReplacementSession()
        {
            var entry = EmptySnapshot(); var replacement = EmptySnapshot(true);
            using var old = new PlayerRecoveryLedger(entry, 1);
            using var current = new PlayerRecoveryLedger(replacement, 1);
            var request = new ProgressRequestContext(1, 0, 1); var ticket = old.Track(request, EmptySnapshot());
            Assert.That(current.RecordResult(ticket, request, SaveStatus.Committed), Is.False);
            old.Dispose(); old.Dispose();
            Assert.That(old.RecordResult(ticket, request, SaveStatus.Committed), Is.False);
            Assert.Throws<ObjectDisposedException>(() => old.Track(new ProgressRequestContext(2, 0, 1), entry));
            Assert.That(old.Target, Is.SameAs(entry)); Assert.That(current.Target, Is.SameAs(replacement));
        }

        [Test] public void RecoveryRejectsInvalidTrackingWithoutChangingTarget()
        {
            var entry = EmptySnapshot();
            Assert.Throws<ArgumentNullException>(() => new PlayerRecoveryLedger(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerRecoveryLedger(entry, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerRecoveryLedger(entry, 1, -1));
            using var ledger = new PlayerRecoveryLedger(entry, 1);
            foreach (var request in new[] { default(ProgressRequestContext), new ProgressRequestContext(1, null, 1),
                new ProgressRequestContext(1, -1, 1), new ProgressRequestContext(1, 0, 2) })
                Assert.Throws<ArgumentException>(() => ledger.Track(request, entry));
            var valid = new ProgressRequestContext(2, 0, 1);
            Assert.Throws<ArgumentNullException>(() => ledger.Track(valid, null));
            ledger.Track(valid, entry);
            Assert.Throws<ArgumentException>(() => ledger.Track(valid, entry));
            Assert.Throws<ArgumentException>(() => ledger.Track(new ProgressRequestContext(1, 0, 1), entry));
            Assert.That(ledger.Target, Is.SameAs(entry));
        }

        [UnityTest] public IEnumerator RecoveryCommittedStorageResultUsesAcceptedSnapshotWhilePlayerRuns()
            => RecoveryAcceptedStorage().ToCoroutine();
        private async UniTask RecoveryAcceptedStorage()
        {
            InstallSessionPowers(); InstallEffects(); var module = await Running(); var context = module.Controller.Context;
            var initial = PlatformerSnapshot.Capture(_config.Scene, context);
            using var ledger = new PlayerRecoveryLedger(initial, _data.ServiceEpoch);
            context.Skills.TryGet("test.skill", out var skill); skill.TryCommit(skill.CreateAttempt());
            context.Effects.ApplyIndependent(SessionEffect(), "accepted"); module.Controller.Tick(.4f);
            var accepted = PlatformerSnapshot.Capture(_config.Scene, context);
            var operation = _data.SaveAsync(0, accepted, new SaveSummary("recovery target"), new PlatformerSnapshotCodec());
            var ticket = ledger.Track(operation.Context, accepted);
            module.Controller.Tick(1); skill.SetOpen(false);
            var result = await operation.Completion;
            Assert.That(result.Status, Is.EqualTo(SaveStatus.Committed));
            Assert.That(ledger.RecordResult(ticket, result.Context, result.Status), Is.True);
            Assert.That(ledger.Target, Is.SameAs(accepted));
            Assert.That(ledger.Target.Powers.GetSkill(0).RemainingCooldown, Is.EqualTo(9.6f).Within(.0001));
            Assert.That(ledger.Target.GetEffect(0).RemainingSeconds, Is.EqualTo(9.6f).Within(.0001));
            Assert.That(ledger.Target.Powers.GetSkill(0).IsOpen, Is.True);
            Assert.That(skill.IsOpen, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            var read = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
            Assert.That(read.Succeeded, Is.True);
            Assert.That(read.Progress.Powers.GetSkill(0).RemainingCooldown,
                Is.EqualTo(ledger.Target.Powers.GetSkill(0).RemainingCooldown));
        }

        [UnityTest] public IEnumerator RecoveryRejectedStorageRequestRetainsEntryTarget()
            => RecoveryRejectedStorage().ToCoroutine();
        private async UniTask RecoveryRejectedStorage()
        {
            await _data.WhenReady; var initial = EmptySnapshot();
            using var ledger = new PlayerRecoveryLedger(initial, _data.ServiceEpoch);
            var accepted = EmptySnapshot(true);
            var operation = _data.SaveAsync(2, accepted, new SaveSummary("invalid slot"), new PlatformerSnapshotCodec());
            var ticket = ledger.Track(operation.Context, accepted);
            var result = await operation.Completion;
            Assert.That(result.Status, Is.EqualTo(SaveStatus.InvalidSlot));
            Assert.That(ledger.RecordResult(ticket, result.Context, result.Status), Is.False);
            Assert.That(ledger.Target, Is.SameAs(initial)); Assert.That(ledger.TargetSlot, Is.Null);
        }
    }
}
