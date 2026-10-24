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
        private void InstallDash(bool enabled = true, bool unlocked = true)
        {
            if (_config.Powers == null) SetAirField(_config, "_powers", Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>()));
            var slots = new EntitySkillSlot[2];
            for (int i = 0; i < 2; i++)
            {
                var skill = Keep(ScriptableObject.CreateInstance<PlayerSkillDefinition>());
                SetAirField(skill, "_id", i == 0 ? "ground-dash" : "air-dash");
                SetAirField(skill, "_defaultOpen", unlocked); SetAirField(skill, "_limitedUses", true);
                SetAirField(skill, "_maximumUses", 2); SetAirField(skill, "_cooldownSeconds", .5f);
                slots[i] = new EntitySkillSlot(skill, enabled);
            }
            SetAirField(_config.Powers, "_skills", slots);
        }
        private EntityPowerRuntime DashResource(PlayerController player, bool ground)
        {
            Assert.That(player.Context.Skills.TryGet(ground ? "ground-dash" : "air-dash", out var skill), Is.True);
            return skill;
        }
        private void FinishDash(PlayerController player)
        {
            for (int i = 0; i < 15 && player.Context.Dash.IsActive; i++) Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.False);
        }
        [TestCase(true)] [TestCase(false)]
        public void DashCommitsOnce_LocksDirection_ReleaseDoesNotEnd(bool ground)
        {
            InstallDash(); var player = Character(new Vector2(0, ground ? .9f : 6));
            if (ground) Land(player);
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(ground ? "ground-dash" : "air-dash"));
            Assert.That(DashResource(player, ground).RemainingUses, Is.EqualTo(1));
            Assert.That(DashResource(player, ground).RemainingCooldown, Is.EqualTo(.5f));
            Assert.That(DashResource(player, !ground).RemainingUses, Is.EqualTo(2));
            Assert.That(DashResource(player, !ground).RemainingCooldown, Is.Zero);
            Keys(Key.A); Decide(player);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(16, 0)));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.Zero);
            Assert.That(player.Context.Facing.FacesRight, Is.True);
            FinishDash(player);
            Assert.That(player.Context.Dash.EndReason, Is.EqualTo(PlayerDashEndReason.Completed));
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(5, 0)));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3));
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void DashDisabledOrLocked_PrecheckDoesNotConsume(bool enabled, bool unlocked)
        {
            InstallDash(enabled, unlocked); var player = Character(new Vector2(0, 6));
            Keys(Key.LeftShift); player.Tick(Step);
            Assert.That(Fsm(player).Request(Fsm(player).GetActivation("air"), PlayerAirState.AirDash), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(2));
            Assert.That(DashResource(player, false).RemainingCooldown, Is.Zero);
            if (enabled) Assert.That(player.Context.Input.DashRequested, Is.True);
        }
        [TestCase(true)] [TestCase(false)]
        public void JumpWinsSameStep_NoDashCost(bool ground)
        {
            InstallAirJump(); InstallDash(); var player = Character(new Vector2(0, ground ? .9f : 6));
            if (ground) Land(player);
            Keys(Key.Space, Key.LeftShift); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(ground ? "jump" : "air-jump"));
            Assert.That(DashResource(player, ground).RemainingUses, Is.EqualTo(2));
            Keys(); Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.False, "失败请求不自动延迟补发");
        }
        [Test]
        public void DashLinearSpeedAndFacingFallback_UsesSeconds()
        {
            InstallDash(); SetDashField(_config.Feel.Movement.AirDash, "_speedMode", PlayerDashSpeedMode.Linear);
            var player = Character(new Vector2(0, 6)); player.Context.Facing.SetFacing(false);
            Keys(Key.LeftShift); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-16));
            Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-15.2f).Within(.001));
        }
        [TestCase(true)] [TestCase(false)]
        public void DashGroundBoundaryInterrupts_NoAutomaticOtherDash(bool ground)
        {
            InstallDash(); var player = Character(new Vector2(0, ground ? .9f : 6));
            if (ground) Land(player);
            Keys(Key.LeftShift); Decide(player);
            player.Context.Motor.Body.position = new Vector2(0, ground ? 6 : .9f); Physics2D.SyncTransforms();
            Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(player.Context.Dash.EndReason, Is.EqualTo(ground ? PlayerDashEndReason.GroundLost : PlayerDashEndReason.Landed));
            Assert.That(DashResource(player, ground).RemainingUses, Is.EqualTo(1));
            Assert.That(DashResource(player, !ground).RemainingCooldown, Is.Zero);
        }
        [Test]
        public void DashForwardWallInterrupts_BackWallDoesNot()
        {
            InstallDash(); var player = AtRightWall();
            Keys(Key.A, Key.LeftShift); Decide(player); Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.True);
            WallBox("LeftWall", new Vector2(-1.02f, 5), new Vector2(1, 12)); Physics2D.SyncTransforms();
            Decide(player);
            Assert.That(player.Context.Dash.EndReason, Is.EqualTo(PlayerDashEndReason.Wall));
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void WallSlideNoDirectionDashesAway_ProtectionRejectsAndReturnWindowClears()
        {
            InstallWalls(); InstallDash(); var player = AtRightWall();
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            Keys(Key.LeftShift); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-16));
            Assert.That(player.Context.Wall.Sliding, Is.False);
            FinishDash(player);
            Keys(); Decide(player); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-jump"));
            Keys(Key.LeftShift); Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void DashSaveRejectsWhileActive_CompletedSnapshotRestoresResourcesNotAction()
        {
            InstallDash(); var player = Character(new Vector2(0, 6)); Keys(Key.LeftShift); Decide(player);
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            FinishDash(player); var snapshot = player.Capture("GameplayScene");
            var remaining = DashResource(player, false).RemainingCooldown; player.Stop(); Keys();
            var owner = new object(); var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var candidate = typeof(PlayerController).GetMethod("PrepareSnapshot", flags).Invoke(player, new object[] { owner, _config, snapshot, _input });
            typeof(PlayerController).GetMethod("CommitSnapshot", flags).Invoke(player, new object[] { candidate, null });
            player.StartPrepared(owner);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall")); Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
            Assert.That(DashResource(player, false).RemainingCooldown, Is.EqualTo(remaining));
        }
        [Test]
        public void DashPauseFreezesActionAndCooldown_StopReleasesAction()
        {
            InstallDash(); var player = Character(new Vector2(0, 6)); Keys(Key.LeftShift); Decide(player);
            float remaining = player.Context.Dash.Remaining, cd = DashResource(player, false).RemainingCooldown;
            typeof(PlayerController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            for (int i = 0; i < 10; i++) Decide(player);
            Assert.That(player.Context.Dash.Remaining, Is.EqualTo(remaining));
            Assert.That(DashResource(player, false).RemainingCooldown, Is.EqualTo(cd));
            typeof(PlayerController).GetMethod("Resume", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            Decide(player); Assert.That(player.Context.Dash.Remaining, Is.LessThan(remaining));
            var context = player.Context; player.Stop();
            Assert.That(context.Dash.IsActive, Is.False);
            Assert.That(context.Dash.EndReason, Is.EqualTo(PlayerDashEndReason.Interrupted));
        }
        [Test]
        public void DashDoesNotRefillAirJump_AndHeldCooldownDoesNotRetry()
        {
            InstallAirJump(); InstallDash(); var player = Character(new Vector2(0, 6));
            Keys(Key.Space); Decide(player); Assert.That(AirResource(player).RemainingUses, Is.Zero);
            Keys(Key.LeftShift); Decide(player); FinishDash(player);
            for (int i = 0; i < 30; i++) Decide(player);
            Assert.That(AirResource(player).RemainingUses, Is.Zero);
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
            Assert.That(DashResource(player, false).RemainingCooldown, Is.Zero);
        }
        [Test]
        public void DashCooldownRejectsNewPress_WithoutConsumingResources()
        {
            InstallDash(); var player = Character(new Vector2(0, 6));
            Keys(Key.LeftShift); Decide(player); FinishDash(player);
            Keys(); Decide(player); Keys(Key.LeftShift); player.Tick(Step);
            Assert.That(Fsm(player).Request(Fsm(player).GetActivation("air"), PlayerAirState.AirDash), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.That(player.Context.Input.DashRequested, Is.True);
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void DashAfterWallProtectionClearsReturnWindow()
        {
            InstallWalls(); InstallDash(); var player = AtRightWall();
            Keys(Key.Space); Decide(player); Keys(Key.D);
            player.Context.Motor.Body.position = new Vector2(-3, 5); Physics2D.SyncTransforms();
            for (int i = 0; i < 6; i++) Decide(player);
            Assert.That(player.Context.Wall.Returning, Is.True);
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("air-dash"));
            Assert.That(player.Context.Wall.ReturnRemaining, Is.Zero);
            Assert.That(player.Context.Wall.Returning, Is.False);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(16, 0)));
        }
        [Test]
        public void DashRetainsOnlyExistingJumpBufferUntilFinished()
        {
            InstallDash();
            SetAirField(_config.Feel.Input, "_enableJumpBuffer", true);
            SetAirField(_config.Feel.Input, "_jumpBufferDuration", .12f);
            var player = Character(); Land(player);
            Keys(Key.LeftShift); Decide(player); Keys();
            for (int i = 0; i < 7; i++) Decide(player);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ground-dash"));
            FinishDash(player);
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }
        [Test]
        public void AirDashActualPhysicsKeepsHeightAndMovesHorizontally()
        {
            InstallDash(); var player = Character(new Vector2(0, 6), new Vector2(0, -4));
            Keys(Key.LeftShift); Simulate(player);
            for (int i = 0; i < 4; i++) Simulate(player);
            Assert.That(player.Context.Motor.Position.y, Is.EqualTo(6).Within(.001));
            Assert.That(player.Context.Motor.Position.x, Is.EqualTo(1.6f).Within(.02));
            Assert.That(player.Context.Dash.IsActive, Is.True);
        }
    }
}
