using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerCombatConfigurationTests
    {
        #region 内部方法
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        internal static PlayerAttackProfile Attack()
        {
            var shape = new PlayerHitShape();
            Set(shape, "_kind", PlayerHitShapeKind.Polygon);
            Set(shape, "_points", new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
            var frame = new PlayerHitKeyframe();
            Set(frame, "_time", .1f); Set(frame, "_shapes", new[] { shape });
            var window = new PlayerHitWindow();
            Set(window, "_id", "edge"); Set(window, "_keyframes", new[] { frame });
            var segment = new PlayerAttackSegment();
            Set(segment, "_id", "first"); Set(segment, "_abilityId", "optional-sword");
            Set(segment, "_recoveryId", "sheathe"); Set(segment, "_windows", new[] { window });
            var recovery = new PlayerAttackRecovery(); Set(recovery, "_id", "sheathe");
            var attack = new PlayerAttackProfile(); Set(attack, "_enabled", true);
            Set(attack, "_segments", new[] { segment }); Set(attack, "_recoveries", new[] { recovery });
            return attack;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void FrozenNestedGeometryAndPayloadRemainIndependent()
        {
            var source = Attack(); source.Validate();
            var frozen = PlayerCombatData.Copy(source);
            var shape = source.Segments[0].Windows[0].Keyframes[0].Shapes[0];
            Set(shape, "_points", new[] { Vector2.zero, Vector2.right * 9, Vector2.up });
            Set(source.Segments[0].Windows[0].Payload, "_damage", 37f);
            Set(source.Segments[0], "_duration", 8f);
            Assert.That(frozen.Segments[0].Duration, Is.EqualTo(.5f));
            Assert.That(frozen.Segments[0].Windows[0].Payload.Damage, Is.EqualTo(1));
            Assert.That(frozen.Segments[0].Windows[0].Keyframes[0].Shapes[0].Points[1], Is.EqualTo(Vector2.right));
            frozen.Validate();
        }

        [Test]
        public void InvalidPolygonAndMissingAbilityHaveLocatedErrors()
        {
            var attack = Attack();
            Set(attack.Segments[0].Windows[0].Keyframes[0].Shapes[0], "_points",
                new[] { Vector2.zero, Vector2.one, Vector2.up, Vector2.right });
            StringAssert.Contains("Attack[first].Window[edge].Keyframes[0].Shapes[0].Points", Assert.Throws<InvalidOperationException>(attack.Validate).Message);
            attack = Attack(); Set(attack.Segments[0], "_abilityId", " ");
            StringAssert.Contains("Attack[first].AbilityId", Assert.Throws<InvalidOperationException>(attack.Validate).Message);
        }

        [Test]
        public void OptionalUninstalledAbilityIsValidButDanglingComboIsRejected()
        {
            var scheme = ScriptableObject.CreateInstance<PlayerScheme>();
            try
            {
                var attack = Attack(); attack.Validate();
                Set(scheme.Combo, "_groundEntry", "first");
                Assert.DoesNotThrow(() => new PlayerCombatConfiguration(scheme, attack));
                Set(scheme.Combo, "_groundEntry", "missing");
                StringAssert.Contains("Combo.GroundEntry", Assert.Throws<InvalidOperationException>(() => new PlayerCombatConfiguration(scheme, attack)).Message);
            }
            finally { Object.DestroyImmediate(scheme); }
        }

        [Test]
        public void RecapturePreservesFrozenLifeAndRejectsMissingSourceGroups()
        {
            var scheme = ScriptableObject.CreateInstance<PlayerScheme>();
            try
            {
                var attack = new PlayerAttackProfile();
                Set(scheme.Life, "_enabled", true); Set(scheme.Life, "_initial", 42f);
                Set(scheme, "_enableEffects", true);
                var first = new PlayerCombatConfiguration(scheme, attack);
                Set(scheme.Life, "_initial", 19f);
                var second = new PlayerCombatConfiguration(first, attack);
                Assert.That(second.Life.Enabled, Is.True); Assert.That(second.Life.Initial, Is.EqualTo(42));
                Assert.That(second.Life, Is.Not.SameAs(first.Life));
                Set(scheme, "_combo", null);
                StringAssert.Contains("Scheme.Combat", Assert.Throws<InvalidOperationException>(() => new PlayerCombatConfiguration(scheme, attack)).Message);
            }
            finally { Object.DestroyImmediate(scheme); }
        }

        [Test]
        public void ExistingMovementSchemesKeepCombatDisabledAndInputCaptureIsFrozen()
        {
            foreach (var scheme in PlayerSchemeOwnership.Builtins.Take(2))
            {
                Assert.That(scheme.Feel.Attack.Enabled, Is.False);
                PlayerSchemeWorkflow.Validate(scheme);
            }
            var source = PlayerSchemeOwnership.Builtins[0].Feel.CreateIndependentCopy();
            try
            {
                var frozen = source.Capture(Physics2D.gravity);
                Set(source.Input, "_attackBufferDuration", 9f);
                Assert.That(frozen.AttackBufferDuration, Is.EqualTo(.2f));
                Set(source.Input, "_combatDashBufferDuration", float.NaN);
                StringAssert.Contains("Input.CombatDashBufferDuration", Assert.Throws<InvalidOperationException>(() => source.Capture(Physics2D.gravity)).Message);
            }
            finally
            {
                Object.DestroyImmediate(source.Movement); Object.DestroyImmediate(source.Jump);
                Object.DestroyImmediate(source.Input); Object.DestroyImmediate(source);
            }
        }
        #endregion
    }
}
