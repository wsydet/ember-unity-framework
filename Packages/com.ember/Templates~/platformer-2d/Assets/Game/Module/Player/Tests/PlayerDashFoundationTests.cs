using System;
using System.Reflection;
using Game.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private static void SetDashField(PlayerDashSettings settings, string field, object value)
            => typeof(PlayerDashSettings).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, value);

        [Test]
        public void DashInput_ShortPressSurvivesUpdatesAndConsumesOnceIndependentlyOfJump()
        {
            using var adapter = new PlayerInputAdapter(_input.GetChannel("gameplay"), "Direction", "Primary", .12f, "Dash");
            using var allow = _input.Request(new object(), 0, new GameInputRule("gameplay", GameInputPermission.Allow));
            Keys(Key.LeftShift, Key.Space); Keys(); adapter.Sample(); adapter.Sample();
            Assert.That(adapter.DashRequested, Is.True); Assert.That(adapter.DashPressVersion, Is.EqualTo(1));
            Assert.That(adapter.ConsumeDash(), Is.True); Assert.That(adapter.ConsumeDash(), Is.False);
            Assert.That(adapter.JumpRequested, Is.True); Assert.That(adapter.ConsumeJump(), Is.True);
        }

        [Test]
        public void DashInput_HeldRejectedPressDoesNotRepeatAfterFirstPhysicsDecision()
        {
            using var adapter = new PlayerInputAdapter(_input.GetChannel("gameplay"), "Direction", "Primary", dash: "Dash");
            using var allow = _input.Request(new object(), 0, new GameInputRule("gameplay", GameInputPermission.Allow));
            Keys(Key.LeftShift); adapter.Sample(); adapter.EndPhysicsStep(.013f); adapter.Sample();
            Assert.That(adapter.DashRequested, Is.False); Assert.That(adapter.ConsumeDash(), Is.False);
            Keys(); Keys(Key.LeftShift); adapter.Sample();
            Assert.That(adapter.ConsumeDash(), Is.True); Assert.That(adapter.DashPressVersion, Is.EqualTo(2));
        }

        [Test]
        public void DashInput_ChannelPauseClearsPendingAndRequiresReleaseBeforeNewPress()
        {
            using var adapter = new PlayerInputAdapter(_input.GetChannel("gameplay"), "Direction", "Primary", dash: "Dash");
            using var allow = _input.Request(new object(), 0, new GameInputRule("gameplay", GameInputPermission.Allow));
            Keys(Key.LeftShift); adapter.Sample();
            using (var deny = _input.Request(new object(), 100, new GameInputRule("gameplay", GameInputPermission.Deny)))
                Assert.That(adapter.DashRequested, Is.False);
            adapter.Sample(); Assert.That(adapter.ConsumeDash(), Is.False);
            Keys(); Keys(Key.LeftShift); adapter.Sample(); Assert.That(adapter.ConsumeDash(), Is.True);
            adapter.Clear(); Assert.That(adapter.ConsumeDash(), Is.False);
        }

        [Test]
        public void DashMotion_CaptureFreezesSeparateGroundAndAirSettingsAndUsesSeconds()
        {
            var movement = _config.Feel.Movement;
            SetDashField(movement.GroundDash, "_speedMode", PlayerDashSpeedMode.Linear);
            SetDashField(movement.GroundDash, "_duration", .3f);
            SetDashField(movement.GroundDash, "_startSpeed", 18f);
            SetDashField(movement.GroundDash, "_endSpeed", 6f);
            var frozen = _config.Feel.Capture(Physics2D.gravity);
            SetDashField(movement.GroundDash, "_endSpeed", 1f);
            Assert.That(frozen.GroundDash.SpeedAt(.15f), Is.EqualTo(12).Within(.0001));
            Assert.That(frozen.GroundDash.SpeedAt(1), Is.EqualTo(6));
            Assert.That(frozen.GroundDash.SpeedAt(-1), Is.EqualTo(18));
            Assert.That(frozen.AirDash.SpeedAt(.15f), Is.EqualTo(16));
            var copy = _config.Feel.CreateIndependentCopy();
            Keep(copy); Keep(copy.Movement); Keep(copy.Jump); Keep(copy.Input);
            SetDashField(copy.Movement.AirDash, "_speed", 23f);
            Assert.That(_config.Feel.Capture(Physics2D.gravity).AirDash.Speed, Is.EqualTo(16));
        }

        [TestCase("_duration", 0f)]
        [TestCase("_speed", -1f)]
        [TestCase("_startSpeed", float.NaN)]
        [TestCase("_endSpeed", float.PositiveInfinity)]
        public void DashMotion_RejectsInvalidConfigurationBeforeRuntime(string field, float value)
        {
            SetDashField(_config.Feel.Movement.AirDash, field, value);
            Assert.Throws<InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
        }
    }
}
