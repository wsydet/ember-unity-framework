using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Tests
{
    /// <summary>R02.1 独立资源契约；不把此夹具解释为正式二段跳或冲刺实现。</summary>
    public sealed class PlayerPowerResourceTests
    {
        #region 内部参数
        private readonly List<UnityEngine.Object> _assets = new();
        private PlayerAbilityDefinition _ability;
        private PlayerSkillDefinition _skill;
        private PlayerPowerConfig _config;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); _assets.Add(asset); return asset; }
        private void Set(object target, string name, object value)
            { for (var type = target.GetType(); type != null; type = type.BaseType) { var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); if (field != null) { field.SetValue(target, value); return; } } throw new MissingFieldException(name); }
        private EntityPowerRuntime Ability(PlayerPowerSession session)
        { Assert.That(session.Abilities.TryGet("fixture.air", out var item), Is.True); return item; }
        private EntityPowerRuntime Skill(PlayerPowerSession session)
        { Assert.That(session.Skills.TryGet("fixture.skill", out var item), Is.True); return item; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void Setup()
        {
            _ability = Asset<PlayerAbilityDefinition>();
            Set(_ability, "_id", "fixture.air"); Set(_ability, "_defaultOpen", true);
            Set(_ability, "_limitedUses", true); Set(_ability, "_maximumUses", 2);
            Set(_ability, "_refillOnGroundSupport", true);
            _skill = Asset<PlayerSkillDefinition>();
            Set(_skill, "_id", "fixture.skill"); Set(_skill, "_defaultOpen", true);
            Set(_skill, "_cooldownSeconds", .5f);
            _config = Asset<PlayerPowerConfig>();
            Set(_config, "_abilities", new[] { new EntityAbilitySlot(_ability) });
            Set(_config, "_skills", new[] { new EntitySkillSlot(_skill) });
        }

        [TearDown]
        public void Cleanup()
        { foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset); _assets.Clear(); }

        [Test]
        public void DisabledLockedAndTemporaryRestrictionsRemainIndependent()
        {
            Set(_config, "_abilities", new[] { new EntityAbilitySlot(_ability, false) });
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Ability(session);
            item.SetOpen(false);
            Assert.That(item.Query(false, true), Is.EqualTo(EntityUseStatus.ProjectDisabled | EntityUseStatus.Closed
                | EntityUseStatus.EnvironmentDenied | EntityUseStatus.Prohibited));
            item.SetOpen(true);
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.ProjectDisabled));
            Assert.That(item.IsOpen, Is.True); Assert.That(item.RemainingUses, Is.EqualTo(2));
            Assert.That(session.Abilities.Query("missing"), Is.EqualTo(EntityUseStatus.NotInstalled));
        }

        [Test]
        public void QueryAndRejectedAttemptsNeverConsume_AndCommitIsOncePerAttempt()
        {
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Ability(session);
            var attempt = item.CreateAttempt();
            for (int i = 0; i < 100; i++) Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.Available));
            Assert.That(item.TryCommit(attempt, false), Is.EqualTo(EntityUseStatus.EnvironmentDenied));
            Assert.That(item.TryCommit(attempt, prohibited: true), Is.EqualTo(EntityUseStatus.Prohibited));
            Assert.That(attempt.IsCommitted, Is.False); Assert.That(item.RemainingUses, Is.EqualTo(2));
            Assert.That(item.TryCommit(attempt), Is.EqualTo(EntityUseStatus.Available));
            Assert.That(item.TryCommit(attempt), Is.EqualTo(EntityUseStatus.AlreadyCommitted));
            Assert.That(item.RemainingUses, Is.EqualTo(1));
            Assert.That(item.TryCommit(item.CreateAttempt()), Is.EqualTo(EntityUseStatus.Available));
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.NoUses));
            Assert.That(item.IsOpen, Is.True);
        }

        [Test]
        public void SharedSoDoesNotShareProgressResourcesOrAttemptIdentity()
        {
            using var first = PlayerPowerSession.CreateNewGame(_config);
            using var second = PlayerPowerSession.CreateNewGame(_config);
            var a = Ability(first); var b = Ability(second); var attempt = a.CreateAttempt();
            Assert.That(b.TryCommit(attempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
            a.TryCommit(attempt); a.SetOpen(false);
            Assert.That(b.RemainingUses, Is.EqualTo(2)); Assert.That(b.IsOpen, Is.True);
            Assert.That(second.Progress, Is.Not.SameAs(first.Progress));
            Assert.That(_ability.DefaultUnlocked, Is.True);
        }

        [Test]
        public void CooldownUsesSuppliedSeconds_ZeroTimeDoesNotAdvance_QueriesDoNotRestartIt()
        {
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            var attempt = item.CreateAttempt(); item.TryCommit(attempt);
            Assert.That(item.RemainingUses, Is.Null);
            session.Tick(.13f); Assert.That(item.RemainingCooldown, Is.EqualTo(.37f).Within(.000001f));
            for (int i = 0; i < 20; i++) { session.Tick(0); item.Query(); }
            Assert.That(item.RemainingCooldown, Is.EqualTo(.37f).Within(.000001f));
            Assert.That(item.TryCommit(attempt), Is.EqualTo(EntityUseStatus.AlreadyCommitted));
            Assert.That(item.TryCommit(item.CreateAttempt()), Is.EqualTo(EntityUseStatus.CoolingDown));
            session.Tick(.4f); Assert.That(item.RemainingCooldown, Is.Zero);
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.Available));
        }

        [Test]
        public void SkillCanCombineCountAndCooldown_UnlockingDoesNotRefillEither()
        {
            Set(_skill, "_limitedUses", true); Set(_skill, "_maximumUses", 1);
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            item.TryCommit(item.CreateAttempt()); item.SetOpen(false); item.SetOpen(true);
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.NoUses | EntityUseStatus.CoolingDown));
            session.Tick(1); Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.NoUses));
        }

        [Test]
        public void ExplicitGroundRefillIsCappedAndDoesNotClearCooldownOrUnlock()
        {
            Set(_skill, "_limitedUses", true); Set(_skill, "_refillOnGroundSupport", true);
            using var session = PlayerPowerSession.CreateNewGame(_config); var a = Ability(session); var s = Skill(session);
            a.TryCommit(a.CreateAttempt()); s.TryCommit(s.CreateAttempt()); a.SetOpen(false);
            for (int i = 0; i < 10; i++) session.RefillGroundUses();
            Assert.That(a.RemainingUses, Is.EqualTo(2)); Assert.That(a.IsOpen, Is.False);
            Assert.That(s.RemainingUses, Is.EqualTo(1)); Assert.That(s.RemainingCooldown, Is.EqualTo(.5f));
        }

        [Test]
        public void SoChangesAfterPreparationCannotChangeExistingRuntime()
        {
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            Set(_skill, "_cooldownSeconds", 9f); Set(_skill, "_id", "changed");
            Set(_skill, "_defaultOpen", false);
            item.TryCommit(item.CreateAttempt());
            Assert.That(item.Id, Is.EqualTo("fixture.skill")); Assert.That(item.RemainingCooldown, Is.EqualTo(.5f));
            Assert.That(item.IsOpen, Is.True);
        }

        [Test]
        public void DisposedResultsCannotMutateNewSession_ProgressOutlivesRuntime()
        {
            var old = PlayerPowerSession.CreateNewGame(_config); var item = Ability(old);
            var attempt = item.CreateAttempt(); var progress = old.Progress; old.Dispose(); old.Dispose();
            using var next = PlayerPowerSession.CreateNewGame(_config);
            Assert.That(item.TryCommit(attempt), Is.EqualTo(EntityUseStatus.Disposed));
            Assert.Throws<ObjectDisposedException>(() => item.SetOpen(false));
            Assert.That(progress.IsAbilityUnlocked("fixture.air"), Is.True);
            Assert.That(Ability(next).RemainingUses, Is.EqualTo(2));
            Assert.That(Ability(next).TryCommit(attempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
        }

        [TestCase("")]
        [TestCase(" fixture.air")]
        [TestCase("fixture.air ")]
        public void InvalidStableIdFailsPreparation(string id)
        { Set(_ability, "_id", id); Assert.Throws<ArgumentException>(() => PlayerPowerSession.CreateNewGame(_config)); }

        [Test]
        public void DuplicateOrMissingDefinitionFailsEvenWhenDisabled()
        {
            Set(_config, "_abilities", new[] { new EntityAbilitySlot(_ability), new EntityAbilitySlot(_ability, false) });
            Assert.Throws<ArgumentException>(() => PlayerPowerSession.CreateNewGame(_config));
            Set(_config, "_abilities", new[] { new EntityAbilitySlot(null, false) });
            Assert.Throws<ArgumentException>(() => PlayerPowerSession.CreateNewGame(_config));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidCooldownFailsPreparation(float seconds)
        { Set(_skill, "_cooldownSeconds", seconds); Assert.Throws<ArgumentOutOfRangeException>(() => PlayerPowerSession.CreateNewGame(_config)); }

        [Test]
        public void InvalidTimeRejectedBeforeAnyResourceAdvances()
        {
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            item.TryCommit(item.CreateAttempt());
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(-1));
            Assert.That(item.RemainingCooldown, Is.EqualTo(.5f));
        }

        [Test]
        public void CooldownBoundaryDoesNotRetainFloatingPointResidue()
        {
            Set(_skill, "_cooldownSeconds", .3f);
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            item.TryCommit(item.CreateAttempt());
            session.Tick(.1f); session.Tick(.1f);
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.CoolingDown));
            session.Tick(.1f);
            Assert.That(item.RemainingCooldown, Is.Zero);
            Assert.That(item.Query(), Is.EqualTo(EntityUseStatus.Available));
        }

        [Test]
        public void ZeroCooldownAndUnlimitedUsesStillRequireDifferentAttempts()
        {
            Set(_skill, "_cooldownSeconds", 0f);
            using var session = PlayerPowerSession.CreateNewGame(_config); var item = Skill(session);
            var first = item.CreateAttempt();
            Assert.That(item.TryCommit(first), Is.EqualTo(EntityUseStatus.Available));
            Assert.That(item.TryCommit(first), Is.EqualTo(EntityUseStatus.AlreadyCommitted));
            Assert.That(item.TryCommit(item.CreateAttempt()), Is.EqualTo(EntityUseStatus.Available));
            Assert.That(item.RemainingCooldown, Is.Zero); Assert.That(item.RemainingUses, Is.Null);
        }

        [Test]
        public void EmptyConfigurationDoesNotInventMechanics()
        {
            using var session = PlayerPowerSession.CreateNewGame(Asset<PlayerPowerConfig>());
            Assert.That(session.Abilities.Query("air-jump"), Is.EqualTo(EntityUseStatus.NotInstalled));
            Assert.That(session.Skills.Query("dash"), Is.EqualTo(EntityUseStatus.NotInstalled));
            session.Tick(1); session.RefillGroundUses();
        }
        #endregion
    }
}

