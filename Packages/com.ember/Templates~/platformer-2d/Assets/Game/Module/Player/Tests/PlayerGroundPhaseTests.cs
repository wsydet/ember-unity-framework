using System;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void GradualGround()
        {
            SetMoveFeel("_accelerated", true);
            SetMoveFeel("_acceleration", 20f);
            SetMoveFeel("_braking", 10f);
            SetMoveFeel("_turning", 60f);
        }

        [Test]
        public void GroundPhases_ReleaseRepressReverse_PreserveNumericMotionAndParents()
        {
            GradualGround(); InstallPowers(); var player = Character(); Land(player);
            var ground = Fsm(player).GetActivation("ground");
            var resource = Power(player); resource.TryCommit(resource.CreateAttempt());
            Keys(Key.D); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("accelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(.4f).Within(.0001));
            Keys(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(.2f).Within(.0001));
            Keys(Key.D); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("accelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(.6f).Within(.0001));
            Keys(Key.A); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-.6f).Within(.0001));
            Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("accelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-1f).Within(.0001));
            for (int i = 0; i < 20; i++) Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-5));
            Keys(); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            for (int i = 0; i < 30; i++) Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
            Assert.That(Fsm(player).GetActivation("ground"), Is.EqualTo(ground));
            Assert.That(resource.RemainingUses, Is.EqualTo(1), "阶段切换不能重复补资源");
        }

        [TestCase(false)] [TestCase(true)]
        public void GroundPhases_InstantGroundSkipsNewStates_EvenWithAirGradual(bool airGradual)
        {
            SetMoveFeel("_independentAir", true); SetMoveFeel("_airAccelerated", airGradual);
            var player = Character(); Land(player);
            Keys(Key.D); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5));
            Keys(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
        }

        [TestCase(0f, 0f, "idle", 0f)]
        [TestCase(0f, 2f, "decelerate", 1.8f)]
        [TestCase(.5f, 4f, "decelerate", 3.8f)]
        [TestCase(.5f, 1f, "accelerate", 1.4f)]
        [TestCase(.5f, 2.5f, "move", 2.5f)]
        [TestCase(-.5f, 1f, "decelerate", -.2f)]
        public void GroundPhases_AnalogTargetsUseSameStepFacts(float input, float velocity, string expectedState, float expectedSpeed)
        {
            GradualGround(); var player = Character(); Land(player);
            player.Context.Motor.Body.linearVelocity = new Vector2(velocity, 0);
            typeof(PlayerInputAdapter).GetProperty(nameof(PlayerInputAdapter.Horizontal)).SetValue(player.Context.Input, input);
            player.FixedTick(Step);
            Assert.That(player.CurrentStateId, Is.EqualTo(expectedState));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(expectedSpeed).Within(.0001));
        }

        [TestCase("_acceleration", 0f, true, "accelerate")]
        [TestCase("_braking", 2f, false, "decelerate")]
        [TestCase("_turning", -2f, true, "decelerate")]
        public void GroundPhases_ZeroRateNeverBecomesInstant(string field, float velocity, bool input, string expected)
        {
            GradualGround(); SetMoveFeel(field, 0f); var player = Character(); Land(player);
            player.Context.Motor.Body.linearVelocity = new Vector2(velocity, 0);
            if (input) Keys(Key.D); else Keys();
            Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(expected));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(velocity));
        }

        [TestCase(false)] [TestCase(true)]
        public void GroundPhases_JumpInterruptsBothStages(bool braking)
        {
            GradualGround(); var player = Character(); Land(player);
            Keys(Key.D); Decide(player); if (braking) { Keys(); Decide(player); }
            Assert.That(player.CurrentStateId, Is.EqualTo(braking ? "decelerate" : "accelerate"));
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
        }

        [Test]
        public void WallFastSlide_DisabledFallsBackAndSessionRemainsFrozen()
        {
            InstallWalls(); SetJumpFeel("_enableWallFastSlide", false);
            var player = AtRightWall(); SetJumpFeel("_enableWallFastSlide", true);
            Keys(Key.D, Key.S); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-.5f));
        }

        [Test]
        public void ClimbWithoutHang_RejectsPreparationWithoutAutoEnabling()
        {
            InstallLedge(hangEnabled: false);
            Assert.Throws<InvalidOperationException>(() => Character());
            Assert.That(_config.Powers.AbilitySlots.Single(s => s.Definition.Id == "ledge-hang").Enabled, Is.False);
        }

        [TestCase(false, false)] [TestCase(true, false)]
        [TestCase(false, true)] [TestCase(true, true)]
        public void GroundPhases_DashOrJumpPriorityDoesNotDuplicateCommit(bool braking, bool jump)
        {
            GradualGround(); InstallDash(); var player = Character(); Land(player);
            Keys(Key.D); Decide(player); if (braking) { Keys(); Decide(player); }
            Assert.That(player.CurrentStateId, Is.EqualTo(braking ? "decelerate" : "accelerate"));
            Keys(jump ? new[] { Key.Space, Key.LeftShift } : new[] { Key.LeftShift }); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(jump ? "jump" : "ground-dash"));
            Assert.That(DashResource(player, true).RemainingUses, Is.EqualTo(jump ? 2 : 1));
        }

        [TestCase(false)] [TestCase(true)]
        public void GroundPhases_DropBeatsJumpAndDash(bool braking)
        {
            GradualGround(); InstallDrop(); InstallDash(); DropPlatform();
            var player = OnDropPlatform(); Keys(Key.D); Decide(player);
            if (braking) { Keys(); Decide(player); }
            Assert.That(player.CurrentStateId, Is.EqualTo(braking ? "decelerate" : "accelerate"));
            Keys(Key.S, Key.Space, Key.LeftShift); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("drop-through"));
            Assert.That(DropResource(player).RemainingUses, Is.EqualTo(1));
            Assert.That(DashResource(player, true).RemainingUses, Is.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void GroundPhases_LostSupportImmediatelyReturnsToAir(bool braking)
        {
            GradualGround(); var player = Character(); Land(player); Keys(Key.D); Decide(player);
            if (braking) { Keys(); Decide(player); }
            player.Context.Motor.Body.position = new Vector2(0, 6); Physics2D.SyncTransforms();
            Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.ActiveStatePath, Does.Not.Contain("ground"));
        }

        [TestCase("accelerate")] [TestCase("decelerate")]
        public void GroundPhases_DisabledDefaultEntryRedirectsWithoutLoop(string entry)
        {
            var graph = Keep(UnityEngine.Object.Instantiate(_config.Graph));
            graph.Configure(graph.EntryId, graph.Nodes.Select(n => n.Id == "ground"
                ? new EmberFsmNode(n.Id, n.DisplayName, n.Definition, n.ParentId, entry, n.Bindings.ToArray()) : n).ToArray());
            _config.Configure(graph, _config.Spawn);
            var player = Character(); Land(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("move"));
        }

        [Test]
        public void GroundPhases_LandingRetainsVelocityAndSelectsBraking()
        {
            GradualGround(); SetMoveFeel("_braking", 0f);
            var player = Character(new Vector2(0, 1.1f), new Vector2(2, -1));
            for (int i = 0; i < 60 && !player.ActiveStatePath.Contains("ground"); i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(2).Within(.0001));
        }

        [Test]
        public void GroundPhases_SnapshotRestoreDoesNotResetHorizontalVelocityOrResources()
        {
            GradualGround(); InstallPowers(); var player = Character(); Land(player);
            var resource = Power(player); resource.TryCommit(resource.CreateAttempt());
            player.Context.Motor.Body.linearVelocity = new Vector2(2, 0);
            Keys(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            var snapshot = player.Capture("GameplayScene"); player.Stop();
            var candidate = player.PrepareSnapshot(new object(), _config, snapshot, _input);
            player.CommitSnapshot(candidate);
            // 正式图仍从 Air/Fall 启动，第一固定步由 Air 的支撑规则进入 Ground；不为新阶段改变恢复启动协议。
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(1.8f).Within(.0001));
            Assert.That(Power(player).RemainingUses, Is.EqualTo(1));
            Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("decelerate"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(1.6f).Within(.0001));
            Assert.That(Power(player).RemainingUses, Is.EqualTo(1));
        }
    }
}
