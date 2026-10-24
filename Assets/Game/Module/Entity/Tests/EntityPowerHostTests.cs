using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Entity.Tests
{
    /// <summary>无 Player/Input/FSM 引用的最小非玩家宿主，验证阶段开放而不实现 Boss。</summary>
    public sealed class EntityPowerHostTests
    {
        #region 内部参数
        private EntitySkillDefinition _skill;
        private EntityPowerConfig _config;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private EntityPowerRuntime Skill(EntityPowerHost owner)
        { Assert.That(owner.Skills.TryGet("phase.two", out var item), Is.True); return item; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        {
            _skill = ScriptableObject.CreateInstance<EntitySkillDefinition>();
            Set(_skill, "_id", "phase.two"); Set(_skill, "_limitedUses", true);
            Set(_skill, "_maximumUses", 2); Set(_skill, "_cooldownSeconds", 1f);
            _config = ScriptableObject.CreateInstance<EntityPowerConfig>();
            Set(_config, "_skills", new[] { new EntitySkillSlot(_skill) });
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(_config); UnityEngine.Object.DestroyImmediate(_skill); }

        [Test] public void PhaseOpeningDoesNotBypassResourcesEnvironmentOrProhibition()
        {
            using var owner = EntityPowerHost.Create(_config); var skill = Skill(owner);
            Assert.That(skill.Query(), Is.EqualTo(EntityUseStatus.Closed));
            var rejected = skill.CreateAttempt();
            Assert.That(skill.TryCommit(rejected), Is.EqualTo(EntityUseStatus.Closed));
            skill.SetOpen(true);
            Assert.That(skill.TryCommit(rejected, false), Is.EqualTo(EntityUseStatus.EnvironmentDenied));
            Assert.That(skill.TryCommit(rejected, prohibited:true), Is.EqualTo(EntityUseStatus.Prohibited));
            Assert.That(skill.TryCommit(rejected), Is.EqualTo(EntityUseStatus.Available));
            skill.SetOpen(false); skill.SetOpen(true);
            Assert.That(skill.RemainingUses, Is.EqualTo(1)); Assert.That(skill.RemainingCooldown, Is.EqualTo(1));
            Assert.That(skill.Query(), Is.EqualTo(EntityUseStatus.CoolingDown));
            owner.Tick(1); skill.TryCommit(skill.CreateAttempt()); owner.Tick(1);
            skill.SetOpen(false); skill.SetOpen(true);
            Assert.That(skill.Query(), Is.EqualTo(EntityUseStatus.NoUses));
        }
        [Test] public void TwoHostsHaveIndependentOpeningClockResourcesAndAttempts()
        {
            using var a = EntityPowerHost.Create(_config); using var b = EntityPowerHost.Create(_config);
            var sa = Skill(a); var sb = Skill(b); sa.SetOpen(true);
            Assert.That(sb.IsOpen, Is.False); sb.SetOpen(true);
            var attempt = sa.CreateAttempt();
            Assert.That(sb.TryCommit(attempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
            sa.TryCommit(attempt); sb.TryCommit(sb.CreateAttempt()); a.Tick(.3f);
            Assert.That(sa.RemainingCooldown, Is.EqualTo(.7f).Within(.000001));
            Assert.That(sb.RemainingCooldown, Is.EqualTo(1));
            sa.RefillUses(); Assert.That(sa.RemainingUses, Is.EqualTo(2)); Assert.That(sb.RemainingUses, Is.EqualTo(1));
            Assert.That(sa.RemainingCooldown, Is.EqualTo(.7f).Within(.000001));
        }
        [Test] public void DestroyedHostCannotAffectRecreatedHost_IndependentDataSurvives()
        {
            var old = EntityPowerHost.Create(_config); var skill = Skill(old); skill.SetOpen(true);
            var data = old.Availability; var attempt = skill.CreateAttempt(); old.Dispose(); old.Dispose();
            using var next = EntityPowerHost.Create(_config);
            Assert.That(data.IsSkillOpen("phase.two"), Is.True);
            Assert.That(Skill(next).IsOpen, Is.False);
            Assert.That(skill.TryCommit(attempt), Is.EqualTo(EntityUseStatus.Disposed));
            Assert.Throws<ObjectDisposedException>(()=>skill.SetOpen(false));
            Assert.That(Skill(next).TryCommit(attempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
        }
        [Test] public void RuntimeAssemblyHasNoPlayerInputStorageOrFsmDependency()
        {
            foreach (var reference in typeof(EntityPowerHost).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name.StartsWith("Game.Player") || reference.Name.StartsWith("Game.Input")
                    || reference.Name == "Ember.Core.Runtime", Is.False, reference.Name);
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(typeof(EntityPowerHost)), Is.False);
        }
        #endregion
    }
}
