#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallCombatDashTest(PlayerScheme scheme, bool dedicated = false, bool airFallback = false)
        {
            var attack = PlayerCombatData.Copy(scheme.Feel.Attack);
            PlayerDashEffectTestAssets.Set(attack.Dash, "_mode", dedicated ? PlayerDashAttackMode.Dedicated : PlayerDashAttackMode.ContinueNormal);
            PlayerDashEffectTestAssets.Set(attack.Dash, "_opensAt", .1f);
            PlayerDashEffectTestAssets.Set(attack.Dash, "_duration", .3f);
            PlayerDashEffectTestAssets.Set(attack.Dash, "_airBraking", 10f);
            PlayerDashEffectTestAssets.Set(attack.Dash, "_airTurning", 30f);
            var combo = PlayerCombatData.Copy(scheme.Combo);
            if (airFallback)
            {
                PlayerDashEffectTestAssets.Set(attack.Segments[0], "_environment", PlayerAttackEnvironment.Both);
                PlayerDashEffectTestAssets.Set(attack.Segments[2], "_environment", PlayerAttackEnvironment.Air);
                PlayerDashEffectTestAssets.Set(combo, "_airEntry", "third");
            }
            PlayerDashEffectTestAssets.Set(combo, "_dashEntry", "third");
            var body = Keep(ScriptableObject.CreateInstance<PlayerAttackStateDefinition>());
            var recovery = Keep(ScriptableObject.CreateInstance<PlayerAttackRecoveryStateDefinition>());
            var root = Keep(ScriptableObject.CreateInstance<PlayerDashCombatLocomotionStateDefinition>());
            var route = Keep(ScriptableObject.CreateInstance<PlayerDashAttackRouteDefinition>());
            route.Configure(attack.Segments.Select(s => s.Id));
            var tail = Keep(ScriptableObject.CreateInstance<PlayerDashRecoveryStateDefinition>());
            var delayedBody = combo.Connections.Any(c => c.DelayedEnabled) ? Keep(ScriptableObject.CreateInstance<PlayerDelayedAttackStateDefinition>()) : null;
            var delayedRecovery = delayedBody != null ? Keep(ScriptableObject.CreateInstance<PlayerDelayedRecoveryStateDefinition>()) : null;
            AssetDatabase.SaveAssetIfDirty(scheme.Animation.Controller);
            var transaction = Type.GetType("Game.Player.Editor.PlayerCombatStructureTransaction, Game.Player.Editor", true);
            transaction.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
            { scheme, attack, combo, body, recovery, new Dictionary<string, AnimationClip>(), root, delayedBody, delayedRecovery, route, tail });
        }
        private void AttackIntoDash(PlayerController player)
        {
            Keys(Key.J); Decide(player); Keys(); for (int i = 0; i < 10; i++) Simulate(player);
            Keys(Key.LeftShift, Key.J, Key.D); UntilCombat(player, "ground-dash", 8);
            Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.False, "原攻击缓存必须被冲刺清掉");
        }
        [TestCase(false, "second")]
        [TestCase(true, "third")]
        public void DashNewPressUsesConfiguredModeAndWaitsForGate(bool dedicated, string target)
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys(Key.D); Simulate(player); Keys(Key.J, Key.D); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("ground-dash"));
                UntilCombat(player, "combat.attack." + target, 10);
                Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            }, scheme => InstallCombatDashTest(scheme, dedicated));
        }
        [Test]
        public void DashTailPreservesUnexpiredLatePressWithoutRefreshingIt()
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys();
                while (player.Context.Dash.Elapsed < .14f) Simulate(player);
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("ground-dash"));
                UntilCombat(player, "combat.attack.second", 12);
            }, scheme =>
            {
                InstallCombatDashTest(scheme);
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Dash, "_opensAt", 10f);
            });
        }
        [Test]
        public void DashFromSpecialRecoveryCarriesOnlyNormalNext()
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second"); WaitDelayedWindow(player);
                Keys(Key.LeftShift); UntilCombat(player, "ground-dash", 2); Keys();
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
                Assert.That(player.Context.Combat.Dash.Target, Is.EqualTo("third"));
                for (int i = 0; i < 6; i++) Simulate(player);
                Keys(Key.J); UntilCombat(player, "combat.attack.third", 2);
            }, scheme => { InstallDelayedTest(scheme); InstallCombatDashTest(scheme); });
        }
        [Test]
        public void NaturalDashTailAllowsFreshAttackWhileMovementPreservesNext()
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys(Key.D); UntilCombat(player, "combat.dash-recovery", 30);
                double time = player.Context.Combat.Dash.TailTime;
                Keys(Key.A); for (int i = 0; i < 3; i++) Simulate(player);
                Assert.That(player.Context.Combat.Dash.TailTime, Is.GreaterThan(time));
                Assert.That(player.Context.Combat.Dash.Target, Is.EqualTo("second"));
                Keys(Key.J, Key.A); UntilCombat(player, "combat.attack.second", 2);
                Assert.That(player.Context.Facing.FacesRight, Is.False);
            }, scheme => InstallCombatDashTest(scheme));
        }
        [Test]
        public void RedashFromTailConsumesNewRequestAndDropsOldNext()
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys(); UntilCombat(player, "combat.dash-recovery", 30);
                player.Context.Skills.TryGet("ground-dash", out var skill); skill.RefillUses();
                Keys(Key.LeftShift); UntilCombat(player, "ground-dash", 6);
                Assert.That(player.Context.Combat.Dash.Target, Is.EqualTo("first"));
                Assert.That(player.Context.Input.DashRequested, Is.False);
            }, scheme =>
            {
                foreach (var slot in scheme.Powers.SkillSlots)
                    if (slot.Definition.Id == "ground-dash") PlayerDashEffectTestAssets.Set(slot.Definition, "_cooldownSeconds", 0f);
                InstallCombatDashTest(scheme);
            });
        }
        [Test]
        public void NormalLandingKeepsTailRemainingAndJumpBreaksOldNext()
        {
            WithThreeHit((player, scheme) =>
            {
                Place(player, new Vector2(0, 8), 0); Keys(Key.D, Key.LeftShift); Decide(player);
                Keys(); UntilCombat(player, "combat.dash-recovery", 30);
                for (int i = 0; i < 5; i++) Simulate(player);
                double before = player.Context.Combat.Dash.TailTime;
                Place(player, new Vector2(0, .89f), -1);
                for (int i = 0; i < 3; i++) Simulate(player);
                Assert.That(player.Context.Sensors.Shape.IsTouching(player.Context.Sensors.Ground), Is.True);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.dash-recovery"));
                Assert.That(player.Context.Combat.Dash.TailTime, Is.GreaterThan(before));
                Keys(Key.Space); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
                Assert.That(player.Context.Combat.Dash.OpportunityActive, Is.False);
            }, scheme =>
            {
                var landing = typeof(PlayerMoveFeel).GetField("_landing", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scheme.Feel.Movement);
                PlayerDashEffectTestAssets.Set(landing, "_normalEnabled", true);
                InstallCombatDashTest(scheme);
            });
        }
        [TestCase(false)] [TestCase(true)]
        public void DashBodyAndTailUseSameEnvironmentFallback(bool tail)
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player); Keys(); for (int i = 0; i < 12; i++) Simulate(player);
                Place(player, new Vector2(0, 8), 0); Keys(Key.LeftShift, Key.D); UntilCombat(player, "air-dash", 3);
                Keys();
                if (tail) UntilCombat(player, "combat.dash-recovery", 30);
                else for (int i = 0; i < 6; i++) Simulate(player);
                Assert.That(player.Context.Combat.Dash.Target, Is.EqualTo("third"));
                Keys(Key.J); UntilCombat(player, "combat.attack.third", 2);
            }, scheme => InstallCombatDashTest(scheme, airFallback: true));
        }
        [Test]
        public void WallInterruptedDashDoesNotCreateNaturalTail()
        {
            WithThreeHit((player, scheme) =>
            {
                var wall = Keep(new GameObject("CombatWall")); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, _scene);
                wall.transform.position = new Vector3(2, 3); wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 8);
                Physics2D.SyncTransforms(); Keys(Key.LeftShift, Key.D); Decide(player); Keys();
                for (int i = 0; i < 30 && player.Context.Dash.IsActive; i++) Simulate(player);
                Assert.That(player.Context.Dash.EndReason, Is.EqualTo(PlayerDashEndReason.Wall));
                Assert.That(player.Context.Combat.Dash.InTail, Is.False);
                Simulate(player); Assert.That(player.Context.Motor.Velocity.x, Is.Zero.Within(.01f));
            }, scheme => InstallCombatDashTest(scheme));
        }
        [Test]
        public void UnavailableNextDoesNotFallbackAndAirTailReversesSmoothly()
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys(); UntilCombat(player, "combat.dash-recovery", 30);
                player.Context.Abilities.TryGet("test-next", out var next); next.SetOpen(false);
                Keys(Key.J); Decide(player);
                Assert.That(player.Context.Combat.Dash.Target, Is.EqualTo("second"));
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.dash-recovery"));
                Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.True);
                Keys(); UntilCombat(player, "idle", 30);
                Place(player, new Vector2(0, 10), 0); Keys(Key.D, Key.LeftShift); Decide(player); Keys();
                UntilCombat(player, "combat.dash-recovery", 30);
                float before = player.Context.Motor.Velocity.x;
                Keys(Key.A); Simulate(player);
                Assert.That(player.Context.Motor.Velocity.x, Is.InRange(before - .61f, before - .59f));
                for (int i = 0; i < 55; i++) Simulate(player);
                Assert.That(player.Context.Motor.Velocity.x, Is.LessThan(0));
            }, scheme =>
            {
                var ability = ScriptableObject.CreateInstance<PlayerAbilityDefinition>();
                PlayerDashEffectTestAssets.Set(ability, "_id", "test-next"); PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", true);
                AssetDatabase.AddObjectToAsset(ability, scheme);
                PlayerDashEffectTestAssets.Set(scheme.Powers, "_abilities", scheme.Powers.AbilitySlots.Concat(new[] { new Game.Entity.EntityAbilitySlot(ability) }).ToArray());
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[1], "_abilityId", "test-next");
                InstallCombatDashTest(scheme);
            });
        }
        [TestCase(true, "hard-land")] [TestCase(false, "rolling")]
        public void HardLandingOrRollingInterruptsTail(bool hard, string state)
        {
            WithThreeHit((player, scheme) =>
            {
                Place(player, new Vector2(0, 8), 0); Keys(Key.D, Key.LeftShift); Decide(player); Keys();
                UntilCombat(player, "combat.dash-recovery", 30);
                Place(player, new Vector2(0, 1.2f), -20); Keys(Key.D); UntilCombat(player, state, 12);
                Assert.That(player.Context.Combat.Dash.OpportunityActive, Is.False);
                Assert.That(player.Context.MotionControl.Owns(PlayerMotionChannels.Horizontal), Is.False);
            }, scheme =>
            {
                var landing = typeof(PlayerMoveFeel).GetField("_landing", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scheme.Feel.Movement);
                PlayerDashEffectTestAssets.Set(landing, "_hardEnabled", hard); PlayerDashEffectTestAssets.Set(landing, "_hardSpeed", 1f);
                var rolling = typeof(PlayerMoveFeel).GetField("_rolling", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scheme.Feel.Movement);
                PlayerDashEffectTestAssets.Set(rolling, "_enabled", !hard);
                InstallCombatDashTest(scheme);
            });
        }
        [Test]
        public void AirDashTailRestoresGravityAndInertiaOutlivesOpportunity()
        {
            WithThreeHit((player, scheme) =>
            {
                Place(player, new Vector2(0, 10), 0); Keys(Key.D, Key.LeftShift); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("air-dash"));
                Keys(); UntilCombat(player, "combat.dash-recovery", 30);
                float entry = player.Context.Motor.Velocity.x;
                Assert.That(entry, Is.GreaterThan(player.Context.Feel.EffectiveAirSpeed));
                Assert.That(player.Context.VerticalMotion.EffectiveGravityScale, Is.GreaterThan(0));
                for (int i = 0; i < 18; i++) Simulate(player);
                Assert.That(player.Context.Combat.Dash.OpportunityActive, Is.False);
                Assert.That(player.Context.Motor.Velocity.x, Is.GreaterThan(0), "机会截止不能归零惯性");
                float before = player.Context.Motor.Velocity.x; Simulate(player);
                Assert.That(player.Context.Motor.Velocity.x, Is.InRange(before - .21f, before));
                Keys(Key.Space); Decide(player);
                Assert.That(player.Context.MotionControl.Owns(PlayerMotionChannels.Horizontal), Is.False, "新跳跃接管后停止旧惯性");
            }, scheme => InstallCombatDashTest(scheme));
        }
    }
}
#endif
