using System;
using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallDrop(bool enabled = true, bool unlocked = true)
        {
            if (_config.Powers == null) SetAirField(_config, "_powers", Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>()));
            var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            SetAirField(ability, "_id", PlayerDropThroughState.ABILITY_ID);
            SetAirField(ability, "_defaultOpen", unlocked); SetAirField(ability, "_limitedUses", true);
            SetAirField(ability, "_maximumUses", 2);
            SetAirField(_config.Powers, "_abilities", _config.Powers.AbilitySlots.Concat(new[] { new EntityAbilitySlot(ability, enabled) }).ToArray());
        }
        private BoxCollider2D DropPlatform(float y = 4)
        {
            var go = Keep(new GameObject("DropPlatform")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector2(0, y);
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(8, .2f);
            var effector = go.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
            effector.useColliderMask = false; collider.usedByEffector = true;
            return collider;
        }
        private EntityPowerRuntime DropResource(PlayerController player)
        { player.Context.Abilities.TryGet(PlayerDropThroughState.ABILITY_ID, out var value); return value; }
        private PlayerController OnDropPlatform()
        { var player = Character(new Vector2(0, 6)); Land(player); Assert.That(player.Context.Sensors.OneWayGround, Is.Not.Null); return player; }

        [Test]
        public void DropCommitsOnce_ClearsJumpAndHover_PreservesHorizontal_NoAirRefill()
        {
            InstallAirJump(); InstallDrop(); DropPlatform();
            SetJumpFeel("_enableEdgeHover", true);
            var player = OnDropPlatform(); var air = AirResource(player); air.TryCommit(air.CreateAttempt());
            Keys(Key.S, Key.D, Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("drop-through"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5 / Mathf.Sqrt(2)).Within(.001f), "2DVector 斜向输入保持既有归一化控制");
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-4));
            Assert.That(DropResource(player).RemainingUses, Is.EqualTo(1));
            Assert.That(player.Context.JumpWindow.CanJump, Is.False);
            Assert.That(player.Context.VerticalMotion.HoverRemaining, Is.Zero);
            Assert.That(player.Context.Input.JumpRequested, Is.False);
            for (int i = 0; i < 4; i++) Decide(player);
            Assert.That(DropResource(player).RemainingUses, Is.EqualTo(1));
            Assert.That(air.RemainingUses, Is.Zero);
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void DropDisabledOrLockedFallsBackToNormalJump(bool enabled, bool unlocked)
        {
            InstallDrop(enabled, unlocked); DropPlatform(); var player = OnDropPlatform();
            Keys(Key.S, Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(DropResource(player).RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void DropOnSolidGroundFallsBack_AndDownAloneDoesNothing()
        {
            InstallDrop(); var player = Character(); Land(player);
            Keys(Key.S); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Keys(Key.S, Key.Space); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }
        [Test]
        public void DropDoesNotUseOldBufferedJump()
        {
            InstallDrop(); DropPlatform(); var player = OnDropPlatform();
            Keys(Key.S, Key.Space); player.Tick(Step);
            player.Context.Input.EndPhysicsStep(0);
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Assert.That(player.Context.DropThrough.CanStart, Is.False);
            player.FixedTick(Step); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }
        [Test]
        public void DropBlocksJumpDashAndSaveUntilFullyClear_ThenLandsOnOtherPlatform()
        {
            InstallAirJump(); InstallDrop(); InstallDash(); var upper = DropPlatform(); var lower = DropPlatform(1);
            var player = OnDropPlatform(); Keys(Key.S, Key.Space); Decide(player);
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            Keys(); Decide(player); Keys(Key.Space, Key.LeftShift); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("drop-through"));
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(2));
            Assert.That(AirResource(player).RemainingUses, Is.EqualTo(1));
            Keys(); for (int i = 0; i < 80 && player.Context.DropThrough.IsActive; i++) Simulate(player);
            Assert.That(player.Context.DropThrough.IsActive, Is.False);
            Assert.That(Physics2D.GetIgnoreCollision(player.Context.Sensors.Shape, upper), Is.False);
            Assert.DoesNotThrow(() => player.Capture("GameplayScene"));
            Land(player); Assert.That(player.Context.Sensors.OneWayGround, Is.SameAs(lower));
        }
        [Test]
        public void DropPauseResumeAndStopReleaseExactPair()
        {
            InstallDrop(); var platform = DropPlatform(); var player = OnDropPlatform();
            Keys(Key.S, Key.Space); Decide(player); var context = player.Context; var p = context.Motor.Position;
            player.Pause(); player.Tick(3); player.FixedTick(3); _scene.GetPhysicsScene2D().Simulate(.2f);
            Assert.That(context.Motor.Position, Is.EqualTo(p)); Assert.That(context.DropThrough.IsActive, Is.True);
            player.Resume(); Assert.That(Physics2D.GetIgnoreCollision(context.Sensors.Shape, platform), Is.True);
            player.Stop(); Assert.That(context.DropThrough.IsActive, Is.False);
            context.Motor.Resume(); Assert.That(EntityCollisionIgnoreLease2D.IsIgnored(context.Sensors.Shape, platform), Is.False);
        }
        [Test]
        public void DropDestroyedTargetReleasesAndSnapshotRestoreDoesNotResumeAction()
        {
            InstallDrop(); var platform = DropPlatform(); var player = OnDropPlatform();
            Keys(Key.S, Key.Space); Decide(player); UnityEngine.Object.DestroyImmediate(platform.gameObject);
            Keys(); Decide(player); Assert.That(player.Context.DropThrough.IsActive, Is.False);
            var snapshot = player.Capture("GameplayScene"); player.Stop();
            var owner = new object(); var candidate = player.PrepareSnapshot(owner, _config, snapshot, _input);
            player.CommitSnapshot(candidate); player.StartPrepared(owner);
            Assert.That(player.Context.DropThrough.IsActive, Is.False);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(DropResource(player).RemainingUses, Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)]
        public void DropRejectsInvalidEnabledSpeed(float speed)
        {
            InstallDrop(); SetAirField(_config.Feel.Movement, "_dropThroughSpeed", speed);
            Assert.Throws<InvalidOperationException>(() => Character());
        }
    }
}
