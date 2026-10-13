using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    // 隔离图验证真实 Hang/Climb 状态；正式 Air 的动作争用须在资产安装后的集成回归验证。
    public sealed class LedgeTestAirState : EmberFsmState<PlayerContext>
    {
        public static readonly EmberFsmTransition<PlayerContext, LedgeTestAirState> Hang =
            new("hang", "ledge-hang", (state, ctx) => ctx.Ledge.TryCandidate(out _));
        public override void FixedUpdate(float deltaTime)
        { if (Context.Ledge.TryCandidate(out _)) Activation.Request(Hang); }
    }
    public sealed class LedgeTestAirDefinition : EmberFsmStateDefinition<PlayerContext, LedgeTestAirState>
    {
        public override LedgeTestAirState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => new EmberFsmTransition[] { LedgeTestAirState.Hang };
    }
    public sealed partial class PlayerStateTests
    {
        private void InstallLedge(bool hangEnabled = true, bool climbOpen = true)
        {
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            var slots = new List<EntityAbilitySlot>();
            foreach (var id in new[] { PlayerLedgeHangState.ABILITY_ID, PlayerLedgeClimbState.ABILITY_ID })
            {
                var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
                SetAirField(ability, "_id", id); SetAirField(ability, "_defaultOpen", id == PlayerLedgeHangState.ABILITY_ID || climbOpen);
                SetAirField(ability, "_limitedUses", true); SetAirField(ability, "_maximumUses", 2);
                slots.Add(new EntityAbilitySlot(ability, id != PlayerLedgeHangState.ABILITY_ID || hangEnabled));
            }
            SetAirField(powers, "_abilities", slots.ToArray()); SetAirField(_config, "_powers", powers);
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("root",
                new EmberFsmNode("root", "Root", Keep(ScriptableObject.CreateInstance<PlayerLocomotionStateDefinition>()), defaultChildId: "fall", bindings: new[] { new EmberFsmTargetBinding("land", "fall"), new EmberFsmTargetBinding("hard-land", "fall"), new EmberFsmTargetBinding("rolling", "fall") }),
                new EmberFsmNode("fall", "Fall", Keep(ScriptableObject.CreateInstance<LedgeTestAirDefinition>()), "root", bindings: new[] { new EmberFsmTargetBinding("ledge-hang", "ledge-hang") }),
                new EmberFsmNode("ledge-hang", "Hang", Keep(ScriptableObject.CreateInstance<PlayerLedgeHangStateDefinition>()), "root", bindings: new[] { new EmberFsmTargetBinding("ledge-climb", "ledge-climb"), new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("ledge-climb", "Climb", Keep(ScriptableObject.CreateInstance<PlayerLedgeClimbStateDefinition>()), "root", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }));
            _config.Configure(graph, new Vector2(0, 3.2f));
        }
        private BoxCollider2D LedgePlatform()
        {
            var go = Keep(new GameObject("LedgePlatform")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector2(2.6f, 3);
            var box = go.AddComponent<BoxCollider2D>(); box.size = new Vector2(4, 2); Physics2D.SyncTransforms(); return box;
        }
        private EntityPowerRuntime LedgeResource(PlayerController p, string id)
        { p.Context.Abilities.TryGet(id, out var ability); return ability; }
        private PlayerController Hanging()
        {
            LedgePlatform(); var player = Character(); Keys(Key.D); Simulate(player); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang")); return player;
        }
        private void InstallFormalLedge(bool enabled = true, bool unlocked = true)
        {
            InstallWalls(); InstallDash();
            var slots = _config.Powers.AbilitySlots.ToList();
            foreach (var id in new[] { "ledge-hang", "ledge-climb" })
            {
                var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
                SetAirField(ability, "_id", id); SetAirField(ability, "_defaultOpen", unlocked);
                SetAirField(ability, "_limitedUses", true); SetAirField(ability, "_maximumUses", 2);
                slots.Add(new EntityAbilitySlot(ability, enabled));
            }
            SetAirField(_config.Powers, "_abilities", slots.ToArray());
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), new Vector2(0, 3.2f));
        }
        [TestCase(false)] [TestCase(true)]
        public void FormalLedgeJumpAndDashTakePriorityWithoutHangCost(bool dash)
        {
            InstallFormalLedge(); LedgePlatform(); var player = Character();
            Keys(Key.D, dash ? Key.LeftShift : Key.Space); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(dash ? "air-dash" : "air-jump"));
            Assert.That(LedgeResource(player, "ledge-hang").RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void FormalLedgePrecedesSlideAndDoesNotRefillAirJump()
        {
            InstallFormalLedge(); LedgePlatform(); var player = Character();
            var air = AirResource(player); air.TryCommit(air.CreateAttempt());
            Keys(Key.D); Simulate(player); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
            CollectionAssert.AreEqual(new[] { "basic-drive", "ledge-hang" }, player.ActiveStatePath);
            Assert.That(air.RemainingUses, Is.Zero);
            Keys(Key.W); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-climb"));
            Keys(); for (int i = 0; i < 60 && player.Context.Ledge.IsActive; i++) Simulate(player);
            Assert.That(player.Context.Ledge.IsActive, Is.False);
            Assert.That(air.RemainingUses, Is.Zero);
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void FormalLedgeDisabledOrLockedHasNoCost(bool enabled, bool unlocked)
        {
            InstallFormalLedge(enabled, unlocked); LedgePlatform(); var player = Character();
            Keys(Key.D); Simulate(player);
            Assert.That(player.Context.Ledge.IsActive, Is.False);
            Assert.That(LedgeResource(player, "ledge-hang").RemainingUses, Is.EqualTo(2));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(float.NaN)]
        public void LedgeRejectsInvalidEnabledDuration(float duration)
        {
            InstallFormalLedge();
            var field = typeof(PlayerMoveFeel).GetField("_ledge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            SetAirField(field.GetValue(_config.Feel.Movement), "_riseDuration", duration);
            Assert.Throws<InvalidOperationException>(() => Character());
        }
        [Test]
        public void LedgeMovementConfigurationIsFrozenPerSession()
        {
            InstallFormalLedge(); var player = Character();
            var field = typeof(PlayerMoveFeel).GetField("_ledge", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            SetAirField(field.GetValue(_config.Feel.Movement), "_riseDuration", 2f);
            Assert.That(player.Context.Feel.Ledge.RiseDuration, Is.EqualTo(.2f));
            var other = Character(new Vector2(-3, 3));
            Assert.That(other.Context.Feel.Ledge.RiseDuration, Is.EqualTo(2));
        }
        [Test]
        public void LedgeHangCommitsOnceHoldsWithoutDirectionAndRejectsSave()
        {
            InstallLedge(); var player = Hanging(); Keys(); for (int i = 0; i < 8; i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
            Assert.That(LedgeResource(player, PlayerLedgeHangState.ABILITY_ID).RemainingUses, Is.EqualTo(1));
            Assert.That(player.Context.Motor.Velocity.sqrMagnitude, Is.LessThan(.001f));
            Assert.That(player.Context.JumpWindow.CanJump, Is.False);
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            Assert.That(player.Context.CanStartAirJump || player.Context.CanStartWallJump || player.Context.Dash.CanStart(false), Is.False);
        }
        [Test]
        public void LedgeReleaseRequiresReleaseOfTowardInputBeforeRegrab()
        {
            InstallLedge(); var player = Hanging(); Keys(Key.D, Key.S); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Keys(Key.D); Simulate(player); Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Keys(); Simulate(player); Keys(Key.D); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
            Assert.That(LedgeResource(player, PlayerLedgeHangState.ABILITY_ID).RemainingUses, Is.Zero);
        }
        [TestCase(true)] [TestCase(false)]
        public void LedgeClimbUsesIndependentAbilityAndCompletesWithoutTeleport(bool useJump)
        {
            InstallLedge(); var player = Hanging(); Keys(useJump ? Key.Space : Key.W); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-climb"));
            Assert.That(LedgeResource(player, PlayerLedgeClimbState.ABILITY_ID).RemainingUses, Is.EqualTo(1));
            Assert.That(player.Context.Motor.Position.y, Is.LessThan(4.8f));
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            Keys(); for (int i = 0; i < 60 && player.Context.Ledge.IsActive; i++) Simulate(player);
            Assert.That(player.Context.Ledge.IsActive, Is.False); Assert.That(player.Context.Motor.Position.x, Is.GreaterThan(1));
            Assert.DoesNotThrow(() => player.Capture("GameplayScene"));
        }
        [Test]
        public void LedgeLockedClimbStaysHangingWithoutCost()
        {
            InstallLedge(climbOpen: false); var player = Hanging(); Keys(Key.W, Key.Space); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
            Assert.That(LedgeResource(player, PlayerLedgeClimbState.ABILITY_ID).RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void LedgeDisabledDoesNotGrab()
        {
            InstallLedge(hangEnabled: false);
            // 合法的关闭组合：同时关闭依赖挂边的翻越；非法组合由独立准备拒绝用例覆盖。
            SetAirField(_config.Powers, "_abilities", _config.Powers.AbilitySlots.Select(s => new EntityAbilitySlot(s.Definition, false)).ToArray());
            LedgePlatform(); var p = Character(); Keys(Key.D); Simulate(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(LedgeResource(p, PlayerLedgeHangState.ABILITY_ID).RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void LedgePauseFreezesClimbAndStopClearsOwnership()
        {
            InstallLedge(); var player = Hanging(); Keys(Key.W); Simulate(player);
            var ctx = player.Context; float elapsed = ctx.Ledge.Elapsed; Vector2 position = ctx.Motor.Position;
            player.Pause(); player.Tick(2); player.FixedTick(2); _scene.GetPhysicsScene2D().Simulate(.2f);
            Assert.That(ctx.Ledge.Elapsed, Is.EqualTo(elapsed)); Assert.That(ctx.Motor.Position, Is.EqualTo(position));
            player.Resume(); Keys(); Simulate(player); Assert.That(ctx.Ledge.Elapsed, Is.GreaterThan(elapsed));
            player.Stop(); Assert.That(ctx.Ledge.IsActive, Is.False); Assert.That(ctx.Ledge.Target, Is.Null);
        }
        [Test]
        public void LedgeTargetLossExitsAndRestoreDoesNotResumeAction()
        {
            InstallLedge(); var player = Hanging(); var target = player.Context.Ledge.Target;
            target.enabled = false; Keys(); Simulate(player); Simulate(player);
            Assert.That(player.Context.Ledge.IsActive, Is.False);
            var snapshot = player.Capture("GameplayScene"); player.Stop();
            var owner = new object(); var candidate = player.PrepareSnapshot(owner, _config, snapshot, _input);
            player.CommitSnapshot(candidate); player.StartPrepared(owner);
            Assert.That(player.Context.Ledge.IsActive, Is.False);
            Assert.That(LedgeResource(player, PlayerLedgeHangState.ABILITY_ID).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void LedgeRisingDoesNotGrabOrConsume()
        {
            InstallLedge(); LedgePlatform(); var p = Character(velocity: Vector2.up * 2); Keys(Key.D); Decide(p);
            Assert.That(p.Context.Ledge.IsActive, Is.False);
            Assert.That(LedgeResource(p, PlayerLedgeHangState.ABILITY_ID).RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void LedgeBlockedClimbRemainsHangingWithoutConsuming()
        {
            InstallLedge(); var player = Hanging();
            var go = Keep(new GameObject("ClimbCeiling")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector2(1.2f, 5.4f); go.AddComponent<BoxCollider2D>().size = new Vector2(.3f, .1f);
            Physics2D.SyncTransforms(); Keys(Key.W); Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
            Assert.That(LedgeResource(player, PlayerLedgeClimbState.ABILITY_ID).RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void LedgeNewObstacleInterruptsClimbFromSafePosition()
        {
            InstallLedge(); var player = Hanging(); Keys(Key.W); Simulate(player);
            var go = Keep(new GameObject("NewClimbBlock")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector2(1.2f, 5.4f); go.AddComponent<BoxCollider2D>().size = new Vector2(.3f, .1f);
            Physics2D.SyncTransforms(); Keys();
            for (int i = 0; i < 50 && player.Context.Ledge.IsActive; i++) Simulate(player);
            Assert.That(player.Context.Ledge.IsActive, Is.False);
            Assert.That(player.Context.Motor.Position.x, Is.LessThan(.6f));
            Assert.That(LedgeResource(player, PlayerLedgeClimbState.ABILITY_ID).RemainingUses, Is.EqualTo(1));
        }
    }
}
