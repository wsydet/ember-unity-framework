using System;
using System.Reflection;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void SetAirField(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value); return;
            }
            throw new MissingFieldException(name);
        }
        private void InstallAirJump(int maximum = 1, bool enabled = true, bool unlocked = true)
        {
            var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            SetAirField(ability, "_id", PlayerAirJumpState.ABILITY_ID);
            SetAirField(ability, "_defaultOpen", unlocked); SetAirField(ability, "_limitedUses", true);
            SetAirField(ability, "_maximumUses", maximum);
            var config = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            SetAirField(config, "_abilities", new[] { new EntityAbilitySlot(ability, enabled) });
            SetAirField(_config, "_powers", config);
        }
        private EntityPowerRuntime AirResource(PlayerController player)
        {
            Assert.That(player.Context.Abilities.TryGet(PlayerAirJumpState.ABILITY_ID, out var power), Is.True);
            return power;
        }
        [Test]
        public void AirJumpCommitsOnce_HoldAndReleaseDoNotRetriggerOrShorten()
        {
            InstallAirJump(); var player = Character(new Vector2(0, 8));
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("air-jump"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Assert.That(AirResource(player).RemainingUses, Is.Zero);
            for (int i = 0; i < 3; i++) Decide(player);
            Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Keys(Key.Space); Decide(player);
            Assert.That(AirResource(player).RemainingUses, Is.Zero);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void AirJumpDisabledOrLockedRejectsWithoutSpending(bool enabled, bool unlocked)
        {
            InstallAirJump(enabled: enabled, unlocked: unlocked);
            var player = Character(new Vector2(0, 8)); Keys(Key.Space); player.Tick(Step);
            Assert.That(Fsm(player).Request(Fsm(player).GetActivation("air"), PlayerAirState.AirJump), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Assert.That(AirResource(player).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void AirJumpRefillsOnlyOnActualNormalJump_NotLandingOrUpwardProbe()
        {
            InstallAirJump(2); var player = Character(); Land(player);
            var resource = AirResource(player);
            resource.TryCommit(resource.CreateAttempt()); resource.TryCommit(resource.CreateAttempt());
            for (int i = 0; i < 3; i++) Decide(player);
            Assert.That(resource.RemainingUses, Is.Zero);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(resource.RemainingUses, Is.EqualTo(2));
            Keys(); Decide(player); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("air-jump"));
            Assert.That(resource.RemainingUses, Is.EqualTo(1));
            Keys(); Decide(player); Keys(Key.Space); Decide(player);
            Assert.That(resource.RemainingUses, Is.Zero);
            Keys(); Land(player);
            Assert.That(resource.RemainingUses, Is.Zero, "单纯落地不得补充二段跳");
            player.Context.Motor.Body.position = new Vector2(30, 5); Physics2D.SyncTransforms();
            for (int i = 0; i < 8; i++) Decide(player);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(resource.RemainingUses, Is.Zero);
        }
        [Test]
        public void CoyoteJumpHasPriorityAndRefillsAirJump()
        {
            InstallAirJump(); var player = Character(); Land(player);
            var resource = AirResource(player); resource.TryCommit(resource.CreateAttempt());
            player.Context.Motor.Body.position = new Vector2(30, 5); Physics2D.SyncTransforms();
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(resource.RemainingUses, Is.EqualTo(1));
        }
        [TestCase(PlayerAirJumpHorizontalMode.PreserveVelocity, 3f)]
        [TestCase(PlayerAirJumpHorizontalMode.FacingSpeed, -6.4f)]
        public void AirJumpHorizontalPolicyIsEntryOnly(PlayerAirJumpHorizontalMode mode, float expected)
        {
            InstallAirJump(); SetJumpFeel("_airJumpHorizontalMode", mode);
            var player = Character(new Vector2(0, 8), new Vector2(3, -1)); player.Context.Facing.SetFacing(false);
            Keys(Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(expected).Within(.001f));
            Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5), "下一步恢复常规空中控制");
        }
        [Test]
        public void AirJumpRestoreAscendingPreservesSpentUsesAndDoesNotReapplyImpulse()
        {
            InstallAirJump(); var player = Character(new Vector2(0, 8));
            Keys(Key.Space); Decide(player); Keys();
            var snapshot = player.Capture("GameplayScene"); player.Stop();
            var owner = new object(); var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var candidate = typeof(PlayerController).GetMethod("PrepareSnapshot", flags).Invoke(player, new object[] { owner, _config, snapshot, _input });
            typeof(PlayerController).GetMethod("CommitSnapshot", flags).Invoke(player, new object[] { candidate, null });
            player.StartPrepared(owner);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(AirResource(player).RemainingUses, Is.Zero);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(snapshot.Velocity.y));
            Decide(player); Assert.That(AirResource(player).RemainingUses, Is.Zero);
        }
        [Test]
        public void BufferedLandingNormalJumpRefillsBeforeLeavingWithoutGroundEnter()
        {
            InstallAirJump(); var player = Character(new Vector2(0, 1.05f), new Vector2(0, -2));
            var power = AirResource(player); power.TryCommit(power.CreateAttempt());
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            for (int i = 0; i < 6; i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step); Decide(player);
                if (player.CurrentStateId != "jump") continue;
                Assert.That(power.RemainingUses, Is.EqualTo(1));
                Assert.That(player.ActiveStatePath, Does.Not.Contain("ground"));
                return;
            }
            Assert.Fail("应在缓冲有效期内从 Air 直接普通起跳并补满。");
        }
        [Test]
        public void AirJumpResourcesAndCapturedParametersAreIsolatedPerHost()
        {
            InstallAirJump(); var first = Character(new Vector2(-3, 8));
            SetJumpFeel("_airJumpSpeed", 14f); var second = Character(new Vector2(3, 8));
            Assert.That(first.Context.Feel.AirJumpSpeed, Is.EqualTo(10));
            Assert.That(second.Context.Feel.AirJumpSpeed, Is.EqualTo(14));
            AirResource(first).TryCommit(AirResource(first).CreateAttempt());
            Assert.That(AirResource(second).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void AirJumpRejectsOldEmptyAssemblyAndInvalidEnabledParameters()
        {
            InstallAirJump();
            Assert.Throws<ArgumentException>(() => EntityPowerHost.Restore(_config.Powers,
                new EntityPowerSnapshot(Array.Empty<EntityPowerRecord>(), Array.Empty<EntityPowerRecord>())));
            SetJumpFeel("_airJumpSpeed", float.NaN);
            Assert.Throws<InvalidOperationException>(() => Character());
        }
    }
}
