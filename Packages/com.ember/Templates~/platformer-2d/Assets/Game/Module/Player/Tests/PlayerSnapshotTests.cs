using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game.Entity;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private PlatformerSnapshot EmptySnapshot(bool installed = false) => new("GameplayScene", new Vector2(2, 3),
            new Vector2(-1, 4), false, new EntityPowerSnapshot(Array.Empty<EntityPowerRecord>(), Array.Empty<EntityPowerRecord>()),
            installed, Array.Empty<EntityTimedEffectSnapshot>());

        [UnityTest] public IEnumerator CompleteSnapshotRoundTripKeepsResourcesAndIndependentTimers() => SnapshotRoundTrip().ToCoroutine();
        private async UniTask SnapshotRoundTrip()
        {
            InstallSessionPowers(); InstallEffects(); var module = await Running(); var context = module.Controller.Context;
            context.Abilities.TryGet("test.air", out var ability); context.Skills.TryGet("test.skill", out var skill);
            ability.TryCommit(ability.CreateAttempt()); skill.TryCommit(skill.CreateAttempt());
            var attempt = skill.CreateAttempt(); skill.SetOpen(false); module.Controller.Tick(.3f);
            var definition = SessionEffect(); context.Effects.ApplyIndependent(definition, "first");
            module.Controller.Tick(.2f); context.Effects.ApplyIndependent(definition, "second");
            var snapshot = PlatformerSnapshot.Capture(_config.Scene, context);
            var codec = new PlatformerSnapshotCodec(); var encoded = codec.Encode(snapshot);
            module.Controller.Tick(2); ability.RefillUses(); skill.SetOpen(true); context.Effects.RemoveAll(definition.Id);
            var decoded = codec.Decode(encoded.Payload, encoded.DataVersion);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Decoded)); var restored = decoded.Progress;
            using var powers = restored.RestorePowers(_config.Powers); using var effects = restored.RestoreEffects();
            powers.Abilities.TryGet("test.air", out var savedAbility); powers.Skills.TryGet("test.skill", out var savedSkill);
            Assert.That(savedAbility.RemainingUses, Is.EqualTo(1)); Assert.That(savedSkill.IsOpen, Is.False);
            Assert.That(savedSkill.RemainingCooldown, Is.EqualTo(9.5f).Within(.0001));
            Assert.That(savedSkill.TryCommit(attempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
            Assert.That(effects.Count, Is.EqualTo(2)); Assert.That(effects.GetAt(0).RemainingSeconds, Is.EqualTo(9.8f).Within(.0001));
            Assert.That(effects.GetAt(1).RemainingSeconds, Is.EqualTo(10));
            Assert.That(effects.GetAt(0).SourceKey, Is.EqualTo("first"));
            effects.Tick(9.8f); Assert.That(effects.Count, Is.EqualTo(1)); Assert.That(effects.GetAt(0).SourceKey, Is.EqualTo("second"));
            Assert.That(snapshot.GetEffect(0).RemainingSeconds, Is.EqualTo(9.8f).Within(.0001));
        }
        [TestCase(false)] [TestCase(true)] public void EmptySnapshotPreservesOptionalContainer(bool installed)
        {
            var codec = new PlatformerSnapshotCodec(); var encoded = codec.Encode(EmptySnapshot(installed));
            var result = codec.Decode(encoded.Payload, encoded.DataVersion);
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Decoded)); Assert.That(result.Progress.EffectsInstalled, Is.EqualTo(installed));
            Assert.That(result.Progress.Position, Is.EqualTo(new Vector2(2, 3)));
            using var effects = result.Progress.RestoreEffects(); Assert.That(effects == null, Is.EqualTo(!installed));
            using var powers = result.Progress.RestorePowers(null); Assert.That(powers.HasEntries, Is.False);
        }
        [UnityTest] public IEnumerator OwnedAndTimingTailAreRejectedWithoutTouchingSession() => SnapshotRejectSources().ToCoroutine();
        private async UniTask SnapshotRejectSources()
        {
            InstallEffects(); var module = await Running(); var context = module.Controller.Context; var definition = SessionEffect();
            var field = typeof(EntityEffectDefinition).GetField("_lifetime", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var lifetime in new[] { EntityEffectLifetime.Owned, EntityEffectLifetime.OwnedThenTimed })
            {
                field.SetValue(definition, lifetime); var handle = context.Effects.ApplyIndependent(definition, "action-owner");
                var error = Assert.Throws<NotSupportedException>(() => PlatformerSnapshot.Capture(_config.Scene, context));
                StringAssert.Contains(definition.Id, error.Message); StringAssert.Contains("action-owner", error.Message);
                Assert.Throws<NotSupportedException>(() => module.Save(0));
                try { await module.ExitToMenuAsync(ExitFlow(module), 0); Assert.Fail("不可恢复效果必须在暂停及接受保存前拒绝"); }
                catch (NotSupportedException) { }
                if (lifetime == EntityEffectLifetime.OwnedThenTimed)
                {
                    handle.EndAction(); context.Effects.Tick(.2f);
                    Assert.Throws<NotSupportedException>(() => PlatformerSnapshot.Capture(_config.Scene, context));
                    Assert.Throws<NotSupportedException>(() => module.Save(0));
                }
                Assert.That(module.Controller.Context, Is.SameAs(context)); Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(handle.IsActive, Is.True); Assert.That(_data.LastSuccessfulSaveSlot, Is.Null); handle.Dispose();
            }
            Assert.DoesNotThrow(() => PlatformerSnapshot.Capture(_config.Scene, context));
        }
        [UnityTest] public IEnumerator AcceptedFullSnapshotIsFrozenWhileHostKeepsRunning() => SnapshotDisk().ToCoroutine();
        private async UniTask SnapshotDisk()
        {
            InstallSessionPowers(); InstallEffects(); var module = await Running(); var context = module.Controller.Context;
            context.Skills.TryGet("test.skill", out var skill); skill.TryCommit(skill.CreateAttempt());
            context.Effects.ApplyIndependent(SessionEffect(), "timed"); module.Controller.Tick(.4f);
            var snapshot = PlatformerSnapshot.Capture(_config.Scene, context);
            var save = _data.SaveAsync(0, snapshot, new SaveSummary("full snapshot test"), new PlatformerSnapshotCodec());
            module.Controller.Tick(1); var owned = SessionEffect();
            typeof(EntityEffectDefinition).GetField("_lifetime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owned, EntityEffectLifetime.Owned);
            context.Effects.ApplyIndependent(owned, "after-acceptance");
            Assert.That((await save.Completion).Succeeded, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1));
            var read = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
            Assert.That(read.Succeeded, Is.True); Assert.That(read.Progress.EffectCount, Is.EqualTo(1));
            Assert.That(read.Progress.GetEffect(0).RemainingSeconds, Is.EqualTo(9.6f).Within(.0001));
            Assert.That(read.Progress.Powers.GetSkill(0).RemainingCooldown, Is.EqualTo(9.6f).Within(.0001));
            Assert.That(context.Effects.Count, Is.EqualTo(2)); Assert.That(skill.RemainingCooldown, Is.EqualTo(8.6f).Within(.0001));
            // 新增 Owned 不改变已接受的快照，但下一次正式保存必须拒绝。
            Assert.Throws<NotSupportedException>(() => module.Save(1));
        }
        [UnityTest] public IEnumerator V1ReadRejectedWithoutChangingFilesOrCurrentSession() => SnapshotOldVersion().ToCoroutine();
        private async UniTask SnapshotOldVersion()
        {
            var module = await Running(); var context = module.Controller.Context; long version = module.SessionVersion;
            Assert.That((await _data.SaveAsync(0, new PlatformerProgress { Scene = _config.Scene },
                new SaveSummary("legacy"), new PlatformerProgressCodec()).Completion).Succeeded, Is.True);
            // 运行服务独占 .writer.lock；比较持久化数据字节，并单独比较所有文件名，不能读取进程锁。
            var paths = Directory.GetFiles(_directory, "*", SearchOption.AllDirectories);
            var files = paths.Where(path => Path.GetFileName(path) != ".writer.lock").ToDictionary(path => path, File.ReadAllBytes);
            var read = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
            Assert.That(read.Status, Is.EqualTo(ReadStatus.DataVersionUnsupported)); Assert.That(read.Progress, Is.Null);
            CollectionAssert.AreEquivalent(paths, Directory.GetFiles(_directory, "*", SearchOption.AllDirectories));
            foreach (var pair in files) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key));
            Assert.That(module.Controller.Context, Is.SameAs(context)); Assert.That(module.SessionVersion, Is.EqualTo(version));
        }
        [Test] public void FullCodecRejectsCorruptionAndUnsupportedVersions()
        {
            var codec = new PlatformerSnapshotCodec(); var bytes = codec.Encode(EmptySnapshot()).Payload;
            Assert.That(codec.Decode(bytes, 1).Status, Is.EqualTo(DecodeStatus.DataVersionUnsupported));
            Assert.That(codec.Decode(bytes, 999).Status, Is.EqualTo(DecodeStatus.DataVersionUnsupported));
            for (int i = 0; i < bytes.Length; i++)
                Assert.That(codec.Decode(new ReadOnlyMemory<byte>(bytes, 0, i), 2).Status, Is.EqualTo(DecodeStatus.DecodeFailed));
            Assert.That(codec.Decode(bytes.Concat(new byte[] { 0 }).ToArray(), 2).Status, Is.EqualTo(DecodeStatus.DecodeFailed));
            var changed = (byte[])bytes.Clone(); changed[0] = 1;
            Assert.That(codec.Decode(changed, 2).Status, Is.EqualTo(DecodeStatus.DecodeFailed));
        }
        [Test] public void ResourceRestoreRejectsMissingUnknownDuplicateAndOutOfRangeWithoutMutatingHost()
        {
            InstallSessionPowers(); using var original = PlayerPowerSession.CreateNewGame(_config.Powers);
            original.Abilities.TryGet("test.air", out var ability); ability.TryCommit(ability.CreateAttempt());
            var record = original.Capture().GetSkill(0);
            Assert.Throws<ArgumentException>(() => PlayerPowerSession.Restore(_config.Powers, EmptySnapshot().Powers));
            foreach (var bad in new[] { new EntityPowerRecord("unknown", true, 1, 0), new EntityPowerRecord("test.air", true, 99, 0),
                new EntityPowerRecord("test.air", true, null, 0) })
                Assert.Throws<ArgumentException>(() => PlayerPowerSession.Restore(_config.Powers,
                    new EntityPowerSnapshot(new[] { bad }, new[] { record })));
            var duplicate = new EntityPowerRecord("a", true, 1, 0);
            Assert.Throws<ArgumentException>(() => new EntityPowerSnapshot(new[] { duplicate, duplicate }, Array.Empty<EntityPowerRecord>()));
            Assert.That(ability.RemainingUses, Is.EqualTo(1));
            using var restored = PlayerPowerSession.Restore(_config.Powers, original.Capture());
            restored.Abilities.TryGet("test.air", out var fresh); Assert.That(fresh.RemainingUses, Is.EqualTo(1));
        }
        [Test] public void SnapshotDefensivelyCopiesArraysAndRestoresPlainEffectParameters()
        {
            var categories = new[] { "contact" }; var rules = new EntityEffectRules(categories, true, 1 << 8);
            var effects = new[] { new EntityTimedEffectSnapshot("debuff", "source", .6f, rules,
                new EntityEffectAppearance(true, 3, .25f, 2), EntityEffectKind.Debuff) };
            var snapshot = new PlatformerSnapshot("GameplayScene", Vector2.zero, Vector2.zero, true, EmptySnapshot().Powers, true, effects);
            categories[0] = "changed"; effects[0] = default;
            var codec = new PlatformerSnapshotCodec(); var bytes = codec.Encode(snapshot);
            using var restored = codec.Decode(bytes.Payload, 2).Progress.RestoreEffects();
            Assert.That(restored.ProtectsDamage("contact"), Is.True); Assert.That(restored.ProtectsDamage("changed"), Is.False);
            Assert.That(restored.PreventsStagger, Is.True); Assert.That(restored.ExcludedLayers, Is.EqualTo(1 << 8));
            Assert.That(restored.GetAt(0).Kind, Is.EqualTo(EntityEffectKind.Debuff)); Assert.That(restored.GetAt(0).ElapsedSeconds, Is.Zero);
            Assert.That(restored.GetAt(0).Appearance.Priority, Is.EqualTo(3)); restored.Tick(.6f); Assert.That(restored.Count, Is.Zero);
        }
    }
}
