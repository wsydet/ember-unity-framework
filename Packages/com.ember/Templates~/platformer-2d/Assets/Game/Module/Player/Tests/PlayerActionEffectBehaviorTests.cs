using System.Linq;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(true)] [TestCase(false)]
        public void SchemeEffectsMatchExplicitLegacyFixtureForSameDashInput(bool ground)
        {
            InstallDashEffects();
            var original = _config;
            var legacy = Character(new Vector2(-3, ground ? .9f : 6)); if (ground) Land(legacy);
            var scheme = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme));
            var effects = Keep(ScriptableObject.CreateInstance<PlayerActionEffects>());
            effects.Configure(original.Graph.Nodes.Where(n => n.Definition is PlayerGroundDashStateDefinition or PlayerAirDashStateDefinition).Select(n =>
            {
                var g = n.Definition as PlayerGroundDashStateDefinition; var a = n.Definition as PlayerAirDashStateDefinition;
                return new PlayerActionEffectSlot(n.Id, g != null ? g.ActionEffect : a.ActionEffect, g != null ? g.TailEffect : a.TailEffect);
            }).ToArray());
            PlayerDashEffectTestAssets.Set(scheme, "_graph", original.Graph);
            PlayerDashEffectTestAssets.Set(scheme, "_feel", original.Feel);
            PlayerDashEffectTestAssets.Set(scheme, "_powers", original.Powers);
            PlayerDashEffectTestAssets.Set(scheme, "_enableEffects", true);
            PlayerDashEffectTestAssets.Set(scheme, "_actionEffects", effects);
            var source = Keep(UnityEngine.Object.Instantiate(original)); PlayerDashEffectTestAssets.Set(source, "_scheme", scheme);
            using var capture = new PlayerSchemeCapture(source);
            _config = capture.Config;
            // 此行为夹具只测逻辑；完整方案捕获已校验动画，随后显式去除夹具不用的视觉绑定。
            typeof(PlayerConfig).GetProperty("CapturedAnimation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_config, null);
            var migrated = Character(new Vector2(3, ground ? .9f : 6)); if (ground) Land(migrated);
            Keys(Key.LeftShift); Decide(legacy); Decide(migrated);
            Assert.That(migrated.CurrentStateId, Is.EqualTo(legacy.CurrentStateId));
            Assert.That(migrated.Context.Motor.Velocity, Is.EqualTo(legacy.Context.Motor.Velocity));
            Assert.That(migrated.Context.Effects.GetAt(0).EffectId, Is.EqualTo(legacy.Context.Effects.GetAt(0).EffectId));
            Assert.That(migrated.Context.Effects.ProtectsDamage("test.contact"), Is.True);
            Assert.Throws<System.InvalidOperationException>(() => migrated.Capture("GameplayScene"));
            FinishDash(legacy); FinishDash(migrated);
            Assert.That(migrated.Context.Effects.Count, Is.EqualTo(1));
            Assert.That(migrated.Context.Effects.GetAt(0).RemainingSeconds, Is.EqualTo(legacy.Context.Effects.GetAt(0).RemainingSeconds));
            Assert.That(migrated.Context.Effects.GetAt(0).Lifetime, Is.EqualTo(EntityEffectLifetime.Timed));
            legacy.Stop(); Assert.That(migrated.Context.Effects.Count, Is.EqualTo(1));
            migrated.Tick(1.1f); Assert.That(migrated.Context.Effects.Count, Is.Zero);
            migrated.Stop(); _config = original;
        }
    }
}
