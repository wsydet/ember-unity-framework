#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallDelayedTest(PlayerScheme scheme, bool open = true, bool limited = false)
        {
            var ability = ScriptableObject.CreateInstance<PlayerAbilityDefinition>();
            PlayerDashEffectTestAssets.Set(ability, "_id", "test-special");
            PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", open);
            PlayerDashEffectTestAssets.Set(ability, "_limitedUses", limited);
            AssetDatabase.AddObjectToAsset(ability, scheme);
            PlayerDashEffectTestAssets.Set(scheme.Powers, "_abilities", scheme.Powers.AbilitySlots.Concat(new[] { new EntityAbilitySlot(ability) }).ToArray());
            var prompt = ScriptableObject.CreateInstance<EntityEffectDefinition>();
            PlayerDashEffectTestAssets.Set(prompt, "_id", "CanEAttack");
            PlayerDashEffectTestAssets.Set(prompt, "_lifetime", EntityEffectLifetime.Owned);
            PlayerDashEffectTestAssets.Set(prompt, "_hasAppearance", true);
            PlayerDashEffectTestAssets.Set(prompt, "_alpha", .6f);
            AssetDatabase.AddObjectToAsset(prompt, scheme);
            PlayerDashEffectTestAssets.Set(scheme, "_delayedAttackPrompt", prompt);
            PlayerDashEffectTestAssets.Set(scheme, "_enableEffects", true);
            var attack = PlayerCombatData.Copy(scheme.Feel.Attack);
            var special = PlayerCombatData.Copy(attack.Segments[0]);
            PlayerDashEffectTestAssets.Set(special, "_id", "special");
            PlayerDashEffectTestAssets.Set(special, "_abilityId", "test-special");
            PlayerDashEffectTestAssets.Set(attack, "_segments", attack.Segments.Concat(new[] { special }).ToArray());
            var combo = JsonUtility.FromJson<PlayerCombo>("{\"_groundEntry\":\"first\",\"_connections\":[{\"_source\":\"first\",\"_normal\":\"second\"},{\"_source\":\"second\",\"_normal\":\"third\",\"_delayed\":\"special\",\"_delayedEnabled\":true},{\"_source\":\"special\",\"_normal\":\"third\"}]}");
            var body = Keep(ScriptableObject.CreateInstance<PlayerAttackStateDefinition>());
            var recovery = Keep(ScriptableObject.CreateInstance<PlayerAttackRecoveryStateDefinition>());
            var root = Keep(ScriptableObject.CreateInstance<PlayerCombatLocomotionStateDefinition>());
            var delayedBody = Keep(ScriptableObject.CreateInstance<PlayerDelayedAttackStateDefinition>());
            var delayedRecovery = Keep(ScriptableObject.CreateInstance<PlayerDelayedRecoveryStateDefinition>());
            AssetDatabase.SaveAssetIfDirty(scheme.Animation.Controller);
            var transaction = Type.GetType("Game.Player.Editor.PlayerCombatStructureTransaction, Game.Player.Editor", true);
            transaction.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
            {
                scheme, attack, combo, body, recovery,
                new Dictionary<string, AnimationClip> { ["combat.attack.special"] = scheme.Animation.Slots.First(s => s.NodeId == "idle").Clip },
                root, delayedBody, delayedRecovery, null, null
            });
        }
        private void EnterSecondBody(PlayerController player)
        {
            Keys(Key.J); Decide(player); Keys(); for (int i = 0; i < 10; i++) Simulate(player);
            Keys(Key.J); UntilCombat(player, "combat.attack.second", 10); Keys();
        }
        [TestCase(false)]
        [TestCase(true)]
        public void DelayedToggleRetainsTopologyAndControlsRuntimeSelection(bool enabled)
        {
            WithThreeHit((player, scheme) =>
            {
                Assert.That(scheme.Graph.Nodes.Any(n => n.Id == "combat.delayed-recovery.second"), Is.True);
                EnterSecondBody(player);
                UntilCombat(player, enabled ? "combat.delayed-recovery.second" : "combat.recovery.second");
                Assert.That(player.Context.Combat.IsDelayedRecovery, Is.EqualTo(enabled));
            }, scheme =>
            {
                InstallDelayedTest(scheme);
                PlayerDashEffectTestAssets.Set(scheme.Combo.Connections.First(c => c.Source == "second"), "_delayedEnabled", false);
                if (enabled) PlayerDashEffectTestAssets.Set(scheme.Combo.Connections.First(c => c.Source == "second"), "_delayedEnabled", true);
            });
        }
        private void WaitDelayedWindow(PlayerController player)
        {
            for (int i = 0; i < 30 && !player.Context.Combat.DelayedWindow; i++) Simulate(player);
            Assert.That(player.Context.Combat.DelayedWindow, Is.True);
        }
        [Test]
        public void DelayedRecoveryStartsImmediatelyAndPromptTracksDynamicEligibility()
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second");
                Assert.That(player.Context.Combat.IsDelayedRecovery, Is.True);
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
                WaitDelayedWindow(player);
                var own = player.Context.Effects.CaptureActive().Single(e => e.EffectId == "CanEAttack");
                using var other = player.Context.Effects.ApplyIndependent(scheme.DelayedAttackPrompt, "other-source");
                player.Context.Abilities.TryGet("test-special", out var ability); ability.SetOpen(false); Simulate(player);
                Assert.That(player.Context.Effects.TryGet(own.ApplicationId, out _), Is.False);
                Assert.That(other.IsActive, Is.True);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.delayed-recovery.second"));
                double time = player.Context.Combat.Progress;
                ability.SetOpen(true); Simulate(player);
                Assert.That(player.Context.Combat.Progress, Is.GreaterThan(time));
                Assert.That(player.Context.Effects.CaptureActive().Count(e => e.EffectId == "CanEAttack"), Is.EqualTo(2));
                Keys(Key.J); UntilCombat(player, "combat.attack.special", 2);
                Assert.That(player.Context.Effects.CaptureActive().Count(e => e.EffectId == "CanEAttack"), Is.EqualTo(1));
                Keys(); for (int i = 0; i < 10; i++) Simulate(player);
                Keys(Key.J); UntilCombat(player, "combat.attack.third", 10);
            }, scheme => InstallDelayedTest(scheme));
        }
        [Test]
        public void ClosedDelayedAbilityChoosesOrdinaryAndOpeningDoesNotExtendThisRecovery()
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.recovery.second");
                player.Context.Abilities.TryGet("test-special", out var ability); ability.SetOpen(true);
                Assert.That(player.Context.Combat.IsDelayedRecovery, Is.False);
                for (int i = 0; i < 17; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
            }, scheme => InstallDelayedTest(scheme, false));
        }
        [Test]
        public void ExhaustedDelayedAbilityStillSelectsSpecialAndBufferedRequestCanUseRefill()
        {
            WithThreeHit((player, scheme) =>
            {
                player.Context.Abilities.TryGet("test-special", out var ability);
                Assert.That(ability.TryCommit(ability.CreateAttempt()), Is.EqualTo(EntityUseStatus.Available));
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second"); WaitDelayedWindow(player);
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.delayed-recovery.second"));
                Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.True);
                ability.RefillUses(); Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.special"));
                Assert.That(ability.RemainingUses, Is.Zero);
            }, scheme => InstallDelayedTest(scheme, true, true));
        }
        [Test]
        public void NormalRequestCannotCrossIntoDelayedWindowAndExpiredDelayedRequestCannotRevive()
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second");
                player.Context.Abilities.TryGet("basic-attack", out var normal); normal.SetOpen(false);
                for (int i = 0; i < 12; i++) Simulate(player);
                Keys(Key.J); Decide(player); Keys();
                WaitDelayedWindow(player); normal.SetOpen(true); Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.delayed-recovery.second"), "普通请求不能变成特殊刀或过窗补普通刀");
                player.Context.Abilities.TryGet("test-special", out var special); special.SetOpen(false);
                Keys(Key.J); Decide(player); Keys();
                for (int i = 0; i < 12; i++) Simulate(player);
                special.SetOpen(true); Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.delayed-recovery.second"), "过期输入不能随能力恢复复活");
                for (int i = 0; i < 70 && player.CurrentStateId == "combat.delayed-recovery.second"; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
                Keys(Key.J); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
            }, scheme => InstallDelayedTest(scheme));
        }
        [Test]
        public void DelayedPromptDependencyAndRecaptureAreExplicit()
        {
            var scheme = Keep(ScriptableObject.CreateInstance<PlayerScheme>());
            var prompt = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            PlayerDashEffectTestAssets.Set(prompt, "_id", "CanEAttack");
            PlayerDashEffectTestAssets.Set(prompt, "_lifetime", EntityEffectLifetime.Owned);
            PlayerDashEffectTestAssets.Set(prompt, "_hasAppearance", true);
            PlayerDashEffectTestAssets.Set(prompt, "_alpha", .6f);
            PlayerDashEffectTestAssets.Set(scheme, "_delayedAttackPrompt", prompt);
            Assert.Throws<InvalidOperationException>(() => new PlayerCombatConfiguration(scheme, new PlayerAttackProfile()));
            PlayerDashEffectTestAssets.Set(scheme, "_enableEffects", true);
            var first = new PlayerCombatConfiguration(scheme, new PlayerAttackProfile());
            PlayerDashEffectTestAssets.Set(prompt, "_id", "changed");
            var second = new PlayerCombatConfiguration(first, new PlayerAttackProfile());
            Assert.That(second.DelayedAttackPrompt.Id, Is.EqualTo("CanEAttack"));
            PlayerDashEffectTestAssets.Set(prompt, "_lifetime", EntityEffectLifetime.Timed);
            Assert.Throws<InvalidOperationException>(() => new PlayerCombatConfiguration(scheme, new PlayerAttackProfile()));
            var recovery = JsonUtility.FromJson<PlayerAttackRecovery>("{\"_id\":\"invalid\",\"_normalDuration\":0.5,\"_delayedNormalDuration\":0.2,\"_delayedTotalDuration\":1}");
            Assert.Throws<InvalidOperationException>(recovery.Validate);
        }
        [Test]
        public void EarlyRecoveryPressUsesNormalThirdAndNeverDelayedTarget()
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second");
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.third"));
                Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
            }, scheme => InstallDelayedTest(scheme));
        }

        [TestCase("jump")]
        [TestCase("hit")]
        [TestCase("death")]
        [TestCase("stop")]
        public void DelayedInterruptionReleasesOnlyItsPromptAndStopIsIdempotent(string action)
        {
            WithThreeHit((player, scheme) =>
            {
                EnterSecondBody(player); UntilCombat(player, "combat.delayed-recovery.second"); WaitDelayedWindow(player);
                var effects = player.Context.Effects;
                var own = effects.CaptureActive().Single(e => e.EffectId == "CanEAttack");
                using var other = effects.ApplyIndependent(scheme.DelayedAttackPrompt, "other-owner");
                if (action == "stop")
                {
                    player.Stop(); player.Stop();
                    Assert.That(player.Context, Is.Null); Assert.That(other.IsActive, Is.False);
                    return;
                }
                if (action == "jump") Keys(Key.Space);
                else player.Context.HitReceiver.Receive(TestHit(damage: action == "death" ? 20 : 1));
                Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(action == "jump" ? "jump" : action == "hit" ? "life.hit" : "life.dead"));
                Assert.That(effects.TryGet(own.ApplicationId, out _), Is.False);
                Assert.That(other.IsActive, Is.True, "动作退出不能释放其他拥有者的提示效果");
            }, scheme => { InstallDelayedTest(scheme); InstallLifeTest(scheme); });
        }
    }
}
#endif
