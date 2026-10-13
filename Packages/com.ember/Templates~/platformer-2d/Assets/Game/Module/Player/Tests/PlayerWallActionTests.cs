using System;
using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallWalls(bool enabled = true, bool unlocked = true)
        {
            InstallAirJump();
            var slots = _config.Powers.AbilitySlots.ToList();
            foreach (var id in new[] { "wall-jump", "wall-slide" })
            {
                var definition = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
                SetAirField(definition, "_id", id); SetAirField(definition, "_defaultOpen", unlocked);
                SetAirField(definition, "_limitedUses", false);
                slots.Add(new EntityAbilitySlot(definition, enabled));
            }
            SetAirField(_config.Powers, "_abilities", slots.ToArray());
        }
        private PlayerController AtRightWall(float y = 5, float vy = -1)
        {
            WallBox("Wall", new Vector2(1.02f, 5), new Vector2(1, 12));
            return Character(new Vector2(0, y), new Vector2(0, vy));
        }
        [Test]
        public void WallSlide_RequiresTowardInput_QuickSlideAndReleaseRestoreGravity()
        {
            InstallWalls(); var player = AtRightWall();
            Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Keys(Key.D); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-.5f));
            Assert.That(player.Context.VerticalMotion.EffectiveGravityScale, Is.Zero);
            Keys(Key.D, Key.S); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-8));
            Keys(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.VerticalMotion.EffectiveGravityScale, Is.EqualTo(3));
        }
        [Test]
        public void WallSlide_AscendingAndLostWallDoNotStick()
        {
            InstallWalls(); var player = AtRightWall(vy: 2);
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            player.Context.Motor.Body.linearVelocity = new Vector2(0, -1); Decide(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            player.Context.Motor.Body.position = new Vector2(-3, 5); Physics2D.SyncTransforms(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void WallAbilities_DisabledOrLockedFallBackToAirJump(bool enabled, bool unlocked)
        {
            InstallWalls(enabled, unlocked); var player = AtRightWall();
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Keys(Key.Space); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("air-jump"));
        }
        [Test]
        public void WallJump_BeatsAirJump_UsesWallNotFacing_AndProtectsHorizontal()
        {
            InstallWalls(); var player = AtRightWall(vy: 2);
            player.Context.Facing.SetFacing(false);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-jump"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-5.44f).Within(.001));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(11.22f).Within(.001));
            Assert.That(AirResource(player).RemainingUses, Is.EqualTo(1));
            Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-5.44f).Within(.001));
            Assert.That(player.Context.CanStartWallSlide, Is.False);
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.GreaterThan(0));
        }
        [Test]
        public void WallJump_ReturnTriggersOnce_EndsAndDoesNotShortJump()
        {
            InstallWalls(); var player = AtRightWall();
            Keys(Key.Space); Decide(player); Keys(Key.D);
            // 本例独立验证时间窗口；明确移到离墙位置，避免不模拟物理时一直留在原墙旁重新墙滑。
            player.Context.Motor.Body.position = new Vector2(-3, 5); Physics2D.SyncTransforms();
            for (int i = 0; i < 6; i++) Decide(player);
            Assert.That(player.Context.Wall.Returning, Is.True);
            Assert.That(player.Context.Motor.Velocity.y, Is.Zero);
            Assert.That(player.Context.VerticalMotion.EffectiveGravityScale, Is.EqualTo(1.92f).Within(.001));
            // 离墙后保持窗口，不依赖仍然接触墙面。
            player.Context.Motor.Body.position = new Vector2(-3, 5); Physics2D.SyncTransforms();
            player.Context.Motor.Body.linearVelocity = new Vector2(0, -1); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-1), "回墙清竖直速度只触发一次");
            for (int i = 0; i < 12; i++) Decide(player);
            Assert.That(player.Context.Wall.Returning, Is.False);
            Assert.That(player.Context.VerticalMotion.EffectiveGravityScale, Is.EqualTo(3));
        }
        [Test]
        public void NormalJumpWinsAtGround_AndWallJumpNeverRefillsAirUses()
        {
            InstallWalls(); var player = AtRightWall(.9f, 0); Land(player);
            Keys(Key.Space); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            var resource = AirResource(player); resource.TryCommit(resource.CreateAttempt());
            player.Context.Motor.Body.position = new Vector2(0, 5); Physics2D.SyncTransforms();
            Keys(); Decide(player); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-jump"));
            Assert.That(resource.RemainingUses, Is.Zero);
        }
        [Test]
        public void WallJump_RestoreKeepsResourcesAndVelocityWithoutWindowOrImpulse()
        {
            InstallWalls(); var player = AtRightWall();
            AirResource(player).TryCommit(AirResource(player).CreateAttempt());
            Keys(Key.Space); Decide(player); Keys();
            var snapshot = player.Capture("GameplayScene"); player.Stop();
            var owner = new object(); var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var candidate = typeof(PlayerController).GetMethod("PrepareSnapshot", flags).Invoke(player, new object[] { owner, _config, snapshot, _input });
            typeof(PlayerController).GetMethod("CommitSnapshot", flags).Invoke(player, new object[] { candidate, null });
            player.StartPrepared(owner);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(snapshot.Velocity));
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.Zero);
            Assert.That(player.Context.Wall.ReturnRemaining, Is.Zero);
            Assert.That(AirResource(player).RemainingUses, Is.Zero);
        }
        [Test]
        public void WallWindow_PauseDoesNotAdvance_AndHostsAreIndependent()
        {
            InstallWalls(); var player = AtRightWall(); var other = Character(new Vector2(-4, 5));
            Keys(Key.Space); Decide(player);
            var remaining = player.Context.Wall.ProtectionRemaining;
            typeof(PlayerController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            for (int i = 0; i < 10; i++) Decide(player);
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.EqualTo(remaining));
            Assert.That(other.Context.Wall.ProtectionRemaining, Is.Zero);
        }

        [Test]
        public void WallSlide_CornerLossAndLandingBufferHaveValidExit()
        {
            InstallWalls(); var player = AtRightWall();
            Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            player.Context.Motor.Body.position = new Vector2(0, 12); Physics2D.SyncTransforms(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"), "越过墙顶后不能留在墙滑");
            player.Context.Motor.Body.position = new Vector2(0, .9f);
            player.Context.Motor.Body.linearVelocity = new Vector2(0, -1);
            Physics2D.SyncTransforms(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Wall.Sliding, Is.False);
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.Zero);
        }

        [Test]
        public void WallReturnDisabledRestoresNormalAirControlAfterProtection()
        {
            InstallWalls(); SetJumpFeel("_wallReturnEnabled", false);
            var player = AtRightWall(); Keys(Key.Space); Decide(player);
            player.Context.Motor.Body.position = new Vector2(-3, 5); Physics2D.SyncTransforms(); Keys(Key.D);
            for (int i = 0; i < 8; i++) Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(11.22f).Within(.001));
            Assert.That(player.Context.Wall.Returning, Is.False);
        }
    }
}
