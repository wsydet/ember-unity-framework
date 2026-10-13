using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest]
        public IEnumerator DashTailSurvivesPublicSaveLoadAndDeathRecovery()
            => DashTailRecovery().ToCoroutine();

        private async UniTask DashTailRecovery()
        {
            PlayerDashEffectTestAssets.Install(_config, x => Keep(x));
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            var skill = Keep(ScriptableObject.CreateInstance<PlayerSkillDefinition>());
            PlayerDashEffectTestAssets.Set(skill, "_id", "air-dash");
            PlayerDashEffectTestAssets.Set(skill, "_defaultOpen", true);
            PlayerDashEffectTestAssets.Set(skill, "_cooldownSeconds", 2f);
            PlayerDashEffectTestAssets.Set(skill, "_limitedUses", true);
            PlayerDashEffectTestAssets.Set(skill, "_maximumUses", 2);
            PlayerDashEffectTestAssets.Set(powers, "_skills", new[] { new EntitySkillSlot(skill) });
            PlayerDashEffectTestAssets.Set(_config, "_powers", powers);
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller;
                using var allow = _input.Service.Request(new object(), 100, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
                Keys(Key.LeftShift); player.Tick(.02f); player.FixedTick(.02f);
                Assert.That(player.CurrentStateId, Is.EqualTo("air-dash"));
                var initial = module.RecoveryTarget;
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial));
                using (module.Pause())
                {
                    player.Tick(3); player.FixedTick(3);
                    Assert.That(player.Context.Dash.IsActive, Is.True);
                    Assert.That(player.Context.Effects.GetAt(0).Lifetime, Is.EqualTo(EntityEffectLifetime.Owned));
                }
                Keys();
                for (int i = 0; i < 15 && player.Context.Dash.IsActive; i++) { player.Tick(.02f); player.FixedTick(.02f); }
                Assert.That(player.Context.Effects.GetAt(0).Lifetime, Is.EqualTo(EntityEffectLifetime.Timed));
                player.Tick(.25f);
                float remaining = player.Context.Effects.GetAt(0).RemainingSeconds;
                player.Context.Skills.TryGet("air-dash", out var resource);
                float cd = resource.RemainingCooldown;
                var save = module.Save(0);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                player.Tick(2); Assert.That(player.Context.Effects.Count, Is.Zero);
                Assert.That((await save.Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                AssertDashRestored(player, remaining, cd);
                player.Tick(2);
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                AssertDashRestored(player, remaining, cd);
            }
            finally { host.Session.Dispose(); }
        }
        private void AssertDashRestored(PlayerController player, float remaining, float cd)
        {
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(player.Context.Effects.Count, Is.EqualTo(1));
            Assert.That(player.Context.Effects.GetAt(0).RemainingSeconds, Is.EqualTo(remaining).Within(.0001));
            Assert.That(player.Context.Effects.GetAt(0).SourceKey, Is.EqualTo("air-dash/tail"));
            player.Context.Skills.TryGet("air-dash", out var skill);
            Assert.That(skill.RemainingUses, Is.EqualTo(1));
            Assert.That(skill.RemainingCooldown, Is.EqualTo(cd).Within(.0001));
        }
    }
}
