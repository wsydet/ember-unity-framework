using System;
using System.Reflection;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private object RollingSettings(bool enabled = true, float speed = 8, float duration = .4f)
        {
            var settings = typeof(PlayerMoveFeel).GetField("_rolling", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_config.Feel.Movement);
            SetAirField(settings, "_enabled", enabled); SetAirField(settings, "_speed", speed); SetAirField(settings, "_duration", duration);
            return settings;
        }
        private void ReachRolling(PlayerController player, Key direction = Key.D)
        {
            Keys(direction);
            for (int i = 0; i < 150 && !player.Context.Rolling.IsActive; i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("rolling"));
        }
        [TestCase(Key.D, 1)] [TestCase(Key.A, -1)]
        public void RollingLocksEntryDirectionAndIgnoresReleaseOrReverse(Key direction, int sign)
        {
            RollingSettings(); InstallAirJump();
            var player = Character(new Vector2(0, 3)); var power = AirResource(player); power.TryCommit(power.CreateAttempt());
            ReachRolling(player, direction);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(8 * sign));
            Keys(); Decide(player); Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(8 * sign));
            Keys(sign > 0 ? Key.A : Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(8 * sign));
            Assert.That(player.Context.Facing.FacesRight, Is.EqualTo(sign > 0));
            Assert.That(power.RemainingUses, Is.Zero);
            Assert.That(player.Context.Sensors.Shape.bounds.size, Is.EqualTo(new Vector3(1, 1.75f, 0)));
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
        }
        [TestCase(false, true, "land")] [TestCase(true, false, "land")] [TestCase(true, true, "rolling")]
        public void RollingOptionalEntryUsesHorizontalInputAndOverridesNormalPause(bool enabled, bool direction, string expected)
        {
            LandingSettings(false); RollingSettings(enabled); var player = Character(new Vector2(0, 3));
            if (direction) Keys(Key.D);
            for (int i = 0; i < 150; i++)
            {
                Simulate(player); if (player.Context.Rolling.IsActive || player.Context.Landing.IsActive) break;
            }
            Assert.That(player.CurrentStateId, Is.EqualTo(expected));
        }
        [Test]
        public void RollingDoesNotOverrideHardLandingOrTriggerOnSpawn()
        {
            LandingSettings(true); RollingSettings(); var grounded = Character(); Keys(Key.D);
            for (int i = 0; i < 5; i++) Simulate(grounded);
            Assert.That(grounded.Context.Rolling.IsActive, Is.False);
            var falling = Character(new Vector2(5, 4), new Vector2(0, -12)); ReachLanding(falling);
            Assert.That(falling.CurrentStateId, Is.EqualTo("hard-land"));
        }
        [TestCase("jump")] [TestCase("ground-dash")] [TestCase("drop-through")]
        public void RollingCanBeCancelledByValidActions(string action)
        {
            RollingSettings(); InstallDash(); InstallDrop(); if (action == "drop-through") DropPlatform();
            var player = Character(new Vector2(0, 7)); ReachRolling(player); Keys(); Decide(player);
            if (action == "ground-dash") Keys(Key.LeftShift);
            else if (action == "drop-through") Keys(Key.S, Key.Space);
            else Keys(Key.Space);
            Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo(action));
            Assert.That(player.Context.Rolling.IsActive, Is.False);
        }
        [TestCase("jump")] [TestCase("ground-dash")] [TestCase("drop-through")]
        public void RollingYieldsToActionsOnContactStep(string action)
        {
            RollingSettings(); InstallDash(); InstallDrop(); if (action == "drop-through") DropPlatform();
            var player = Character(new Vector2(0, 7)); Keys(Key.D);
            for (int i = 0; i < 150; i++)
            { Simulate(player); if (player.Context.Sensors.Shape.IsTouchingLayers(1)) break; }
            Assert.That(player.Context.Rolling.IsActive, Is.False);
            if (action == "ground-dash") Keys(Key.D, Key.LeftShift);
            else if (action == "drop-through") Keys(Key.D, Key.S, Key.Space);
            else Keys(Key.D, Key.Space);
            Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo(action));
        }
        [Test]
        public void RollingExpiresOnceAndClampsExitSpeed()
        {
            RollingSettings(); var player = Character(new Vector2(0, 3)); ReachRolling(player); Keys();
            for (int i = 0; i < 25 && player.Context.Rolling.IsActive; i++) Decide(player);
            Assert.That(player.Context.Rolling.IsActive, Is.False); Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5));
            Assert.DoesNotThrow(() => player.Capture("GameplayScene"));
            Decide(player); Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
        }
        [Test]
        public void RollingWallStopsAndLostSupportFalls()
        {
            RollingSettings(); var player = Character(new Vector2(0, 3)); ReachRolling(player);
            var wall = Keep(new GameObject("RollingWall")); SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = player.Context.Motor.Position + new Vector2(.62f, 0);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 4); Physics2D.SyncTransforms();
            Decide(player); Assert.That(player.Context.Rolling.IsActive, Is.False); Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
            var second = Character(new Vector2(-5, 3)); ReachRolling(second);
            second.Context.Motor.Body.position = new Vector2(30, 3); Physics2D.SyncTransforms(); Decide(second);
            Assert.That(second.CurrentStateId, Is.EqualTo("fall")); Assert.That(second.Context.Rolling.IsActive, Is.False);
        }
        [Test]
        public void RollingPauseAndStopCleanUpWithoutSharingSessionParameters()
        {
            RollingSettings(); var first = Character(new Vector2(0, 3)); ReachRolling(first);
            RollingSettings(true, 2, .8f); var second = Character(new Vector2(-5, 3)); ReachRolling(second);
            Assert.That(first.Context.Feel.Rolling.Speed, Is.EqualTo(8)); Assert.That(second.Context.Feel.Rolling.Speed, Is.EqualTo(2));
            float remaining = first.Context.Rolling.Remaining; Decide(second);
            Assert.That(first.Context.Rolling.Remaining, Is.EqualTo(remaining));
            first.Pause(); first.Tick(3); first.FixedTick(3); Assert.That(first.Context.Rolling.Remaining, Is.EqualTo(remaining));
            var old = first.Context; first.Resume(); first.Stop(); Assert.That(old.Rolling.IsActive, Is.False);
        }
        [TestCase(0, .3f)] [TestCase(5, 0)] [TestCase(float.NaN, .3f)]
        public void RollingInvalidConfigurationRejects(float speed, float duration)
        {
            RollingSettings(true, speed, duration); Assert.Throws<InvalidOperationException>(() => Character());
        }
        [Test]
        public void RollingEnabledRequiresCorrespondingState()
        {
            RollingSettings(); var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("only", new EmberFsmNode("only", "only", Keep(ScriptableObject.CreateInstance<PlayerBasicStateDefinition>())));
            _config.Configure(graph, Vector2.zero);
            StringAssert.Contains("Rolling", Assert.Throws<InvalidOperationException>(() => Character()).Message);
        }
    }
}
