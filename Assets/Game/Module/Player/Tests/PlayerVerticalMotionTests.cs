using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(1f, .75f)] [TestCase(-1f, .75f)] [TestCase(4f, .75f)]
        [TestCase(-5f, 1.5f)] [TestCase(5f, 1f)]
        public void VerticalGravity_ApexHasPriorityAndDoesNotDependOnDirection(float speed, float multiplier)
        {
            SetJumpFeel("_enableApexGravity", true); SetJumpFeel("_enableFallGravity", true);
            var player = Character(new Vector2(14, 10), new Vector2(0, speed));
            Decide(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3 * multiplier));
            Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3 * multiplier));
            SettleGroundContact(player); Decide(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3), "落地必须恢复基础重力");
        }

        [Test]
        public void TerminalSpeed_ClampsExistingAndNextPhysicsStepVelocity_AndReleasesOnJump()
        {
            SetJumpFeel("_enableFallGravity", true); SetJumpFeel("_enableFallLimit", true);
            SetJumpFeel("_fallLimit", 3f);
            var player = Character(new Vector2(14, 20), new Vector2(0, -2.9f));
            for (int i = 0; i < 10; i++)
            {
                Simulate(player);
                Assert.That(player.Context.Motor.Velocity.y, Is.GreaterThanOrEqualTo(-3.00001f));
            }
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-3).Within(.00001));
            Place(player, new Vector2(14, 10), -20); Simulate(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-3).Within(.00001));
            SettleGroundContact(player); Keys(Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3));
        }

        [TestCase(false)] [TestCase(true)]
        public void EdgeHover_IsIndependentOfCoyoteAndDoesNotEndWhenDirectionReleased(bool coyote)
        {
            typeof(PlayerInputFeel).GetField("_enableCoyote", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_config.Feel.Input, coyote);
            SetJumpFeel("_enableEdgeHover", true); SetJumpFeel("_edgeHoverDuration", .06f);
            var player = Character(); Land(player); Place(player, new Vector2(14, 2), -1);
            Keys(Key.D); Simulate(player); float height = player.Context.Motor.Position.y;
            Assert.That(player.Context.Motor.Velocity.y, Is.Zero);
            Assert.That(player.Context.JumpWindow.CanJump, Is.EqualTo(coyote));
            Keys(); Simulate(player); Simulate(player);
            Assert.That(player.Context.Motor.Position.y, Is.EqualTo(height));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.Zero);
            Simulate(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.LessThan(0));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3));
        }

        [Test]
        public void EdgeHover_JumpAndLandingCancel_PauseFreezes_AndAirborneStartDoesNotGrantHover()
        {
            SetJumpFeel("_enableEdgeHover", true); SetJumpFeel("_edgeHoverDuration", .2f);
            var player = Character(); Land(player); Place(player, new Vector2(14, 2), -1); Decide(player);
            float remaining = player.Context.VerticalMotion.HoverRemaining;
            player.Pause(); for (int i = 0; i < 100; i++) Decide(player);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.EqualTo(remaining));
            player.Resume(); Keys(Key.Space); Decide(player);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.Zero);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Simulate(player);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.Zero, "主动起跳不能触发悬停");
            Decide(player); // 采样已经完成的离地物理步。
            Keys(); SettleGroundContact(player); Decide(player);
            Place(player, new Vector2(14, 2), -1); Decide(player);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.GreaterThan(0));
            SettleGroundContact(player); Decide(player);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.Zero);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.EqualTo(3));
            player.Stop();
            var airborne = Character(new Vector2(14, 5), new Vector2(0, -2)); Decide(airborne);
            Assert.That(airborne.Context.VerticalMotion.HoverRemaining, Is.Zero);
        }

        [Test]
        public void DisabledVerticalRules_KeepBaseline_AndEnabledValuesAreCapturedAndValidated()
        {
            SetJumpFeel("_fallLimit", float.NaN); SetJumpFeel("_edgeHoverDuration", float.NaN);
            var baseline = Character(new Vector2(14, 20), new Vector2(0, -40)); Decide(baseline);
            Assert.That(baseline.Context.Motor.Velocity.y, Is.EqualTo(-40));
            Assert.That(baseline.Context.Motor.Body.gravityScale, Is.EqualTo(3)); baseline.Stop();
            SetJumpFeel("_enableFallLimit", true);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetJumpFeel("_fallLimit", 3f);
            var captured = Character(new Vector2(14, 20), new Vector2(0, -40));
            SetJumpFeel("_fallLimit", 99f); Decide(captured);
            Assert.That(captured.Context.Motor.Velocity.y, Is.EqualTo(-3));
            SetJumpFeel("_enableEdgeHover", true);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetJumpFeel("_edgeHoverDuration", 0f);
            Assert.DoesNotThrow(() => _config.Feel.Capture(Physics2D.gravity));
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Vector2.zero));
        }
    }
}
