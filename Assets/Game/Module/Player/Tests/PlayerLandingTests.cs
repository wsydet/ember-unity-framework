using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void LandingSettings(bool hard)
        {
            var settings = typeof(PlayerMoveFeel).GetField("_landing", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_config.Feel.Movement);
            SetAirField(settings, "_normalEnabled", true); SetAirField(settings, "_normalDuration", .2f);
            SetAirField(settings, "_hardEnabled", hard); SetAirField(settings, "_hardSpeed", 10f);
            SetAirField(settings, "_hardDuration", .4f);
        }
        private void ReachLanding(PlayerController player)
        {
            for (int i = 0; i < 150 && !player.Context.Landing.IsActive; i++) Simulate(player);
            Assert.That(player.Context.Landing.IsActive, Is.True);
        }
        [TestCase(false)] [TestCase(true)]
        public void LandingStopsHorizontalAndSaveUntilExpiry(bool hard)
        {
            LandingSettings(hard); var player = Character(new Vector2(0, 6), new Vector2(0, -12));
            Keys(Key.D); ReachLanding(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(hard ? "hard-land" : "land"));
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            for (int i = 0; i < 30 && player.Context.Landing.IsActive; i++) Simulate(player);
            Assert.That(player.Context.Landing.IsActive, Is.False);
            Assert.DoesNotThrow(() => player.Capture("GameplayScene"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LandingNormalCancelsButHardDoesNotExtendJumpBuffer(bool hard)
        {
            LandingSettings(hard); var player = Character(new Vector2(0, 6), new Vector2(0, -12));
            ReachLanding(player); Keys(Key.Space); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(hard ? "hard-land" : "jump"));
            if (!hard) return;
            Keys(); for (int i = 0; i < 30; i++) Simulate(player);
            Assert.That(player.Context.Input.JumpRequested, Is.False);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
        }
        [Test]
        public void LandingSpawnOnGroundDoesNotPause()
        {
            LandingSettings(true); var player = Character(); Simulate(player);
            Assert.That(player.Context.Landing.IsActive, Is.False);
        }
        [Test]
        public void LandingPauseFreezesAndStopClears()
        {
            LandingSettings(true); var player = Character(new Vector2(0, 6), new Vector2(0, -12)); ReachLanding(player);
            var context = player.Context; var time = context.Landing.Remaining;
            player.Pause(); player.Tick(3); player.FixedTick(3);
            Assert.That(context.Landing.Remaining, Is.EqualTo(time));
            player.Resume(); player.Stop(); Assert.That(context.Landing.IsActive, Is.False);
        }
        [Test]
        public void LandingLostSupportImmediatelyFalls()
        {
            LandingSettings(true); var player = Character(new Vector2(0, 6), new Vector2(0, -12)); ReachLanding(player);
            player.Context.Motor.Body.position = new Vector2(30, 3); Physics2D.SyncTransforms(); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.Landing.IsActive, Is.False);
        }
        [Test]
        public void LandingRequiresContactAndUsesLastStepRatherThanMaximumSpeed()
        {
            LandingSettings(true); var player = Character(new Vector2(0, 8), new Vector2(0, -20));
            Simulate(player);
            player.Context.Motor.Body.linearVelocity = new Vector2(0, -2);
            player.Context.Motor.Body.position = new Vector2(0, .93f); Physics2D.SyncTransforms();
            Decide(player);
            Assert.That(player.Context.Landing.IsActive, Is.False, "探地命中但尚未接触不能触发");
            float previousStep = player.Context.Motor.Velocity.y;
            for (int i = 0; i < 20 && !player.Context.Landing.IsActive; i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step); Decide(player);
                if (!player.Context.Landing.IsActive) previousStep = player.Context.Motor.Velocity.y;
            }
            Assert.That(player.CurrentStateId, Is.EqualTo("land"));
            Assert.That(player.Context.Landing.ImpactSpeed, Is.EqualTo(-previousStep).Within(.0001f));
            Assert.That(player.Context.Sensors.Shape.IsTouching(player.Context.Sensors.Ground), Is.True);
        }
        [TestCase(false, false, "idle")]
        [TestCase(false, true, "hard-land")]
        [TestCase(true, false, "land")]
        public void LandingRulesHaveIndependentEnableFlags(bool normal, bool hard, string expected)
        {
            LandingSettings(hard);
            var settings = typeof(PlayerMoveFeel).GetField("_landing", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_config.Feel.Movement);
            SetAirField(settings, "_normalEnabled", normal);
            var player = Character(new Vector2(0, 3), new Vector2(0, -12));
            for (int i = 0; i < 100; i++)
            {
                Simulate(player);
                if (player.Context.Landing.IsActive) break;
            }
            Assert.That(player.CurrentStateId, Is.EqualTo(expected));
        }
        [Test]
        public void LandingSettingsAreFrozenPerSessionAndPlayersDoNotShareTimer()
        {
            LandingSettings(true); var first = Character(new Vector2(0, 3), new Vector2(0, -12));
            LandingSettings(false); var second = Character(new Vector2(5, 3), new Vector2(0, -12));
            Decide(second); ReachLanding(first); ReachLanding(second);
            Assert.That(first.Context.Landing.IsHard, Is.True);
            Assert.That(second.Context.Landing.IsHard, Is.False);
            float remaining = first.Context.Landing.Remaining;
            Decide(second);
            Assert.That(first.Context.Landing.Remaining, Is.EqualTo(remaining));
        }
        [Test]
        public void LandingEnabledWithMissingStateRejectsPreparation()
        {
            LandingSettings(true);
            var graph = Keep(ScriptableObject.CreateInstance<Ember.Core.Fsm.EmberFsmGraph>());
            graph.Configure("only", new Ember.Core.Fsm.EmberFsmNode("only", "only", Keep(ScriptableObject.CreateInstance<PlayerFallStateDefinition>())));
            _config.Configure(graph, Vector2.zero);
            var error = Assert.Throws<InvalidOperationException>(() => Character());
            StringAssert.Contains("Land/HardLand", error.Message);
        }
        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void LandingOnlyNormalPauseAllowsDashOrDrop(bool hard, bool drop)
        {
            LandingSettings(hard); InstallDash(); InstallDrop();
            if (drop) DropPlatform();
            var player = Character(new Vector2(0, 8), new Vector2(0, -12)); ReachLanding(player);
            if (drop) Keys(Key.S, Key.Space); else Keys(Key.LeftShift);
            Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(hard ? "hard-land" : drop ? "drop-through" : "ground-dash"));
            Assert.That(drop ? DropResource(player).RemainingUses : DashResource(player, true).RemainingUses,
                Is.EqualTo(hard ? 2 : 1));
        }
        [TestCase(false)] [TestCase(true)]
        public void LandingContactStepPrioritizesHardPauseOverJumpButNormalYields(bool hard)
        {
            LandingSettings(hard); var player = Character(new Vector2(0, 4), new Vector2(0, -12));
            for (int i = 0; i < 100; i++)
            {
                Simulate(player);
                if (player.Context.Sensors.Shape.IsTouchingLayers(1)) break;
            }
            Assert.That(player.Context.Sensors.Shape.IsTouchingLayers(1), Is.True);
            Assert.That(player.Context.Landing.IsActive, Is.False);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(hard ? "hard-land" : "jump"));
        }
    }
}
