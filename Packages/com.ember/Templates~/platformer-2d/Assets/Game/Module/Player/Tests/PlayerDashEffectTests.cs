using System;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private EntityEffectDefinition InstallDashEffects()
        { InstallDash(); return PlayerDashEffectTestAssets.Install(_config, x => Keep(x)); }
        [TestCase(true)] [TestCase(false)]
        public void DashEffects_NormalEndReleasesOwnedAndCreatesSeparateTimed(bool ground)
        {
            var tail = InstallDashEffects(); var player = Character(new Vector2(0, ground ? .9f : 6));
            if (ground) Land(player);
            var effects = player.Context.Effects;
            Keys(Key.LeftShift); Decide(player);
            var owned = effects.GetAt(0);
            Assert.That(owned.Lifetime, Is.EqualTo(EntityEffectLifetime.Owned));
            Assert.That(effects.ProtectsDamage("test.contact"), Is.True);
            Assert.That(effects.ProtectsDamage("test.projectile"), Is.False);
            Assert.That(effects.PreventsStagger, Is.False);
            Assert.That(player.GetComponent<Collider2D>().excludeLayers.value, Is.EqualTo(1 << 8));
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            SetAirField(tail, "_duration", 9f); // 准备后的 SO 改动不能影响本实例。
            FinishDash(player);
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects.GetAt(0).ApplicationId, Is.Not.EqualTo(owned.ApplicationId));
            Assert.That(effects.GetAt(0).Lifetime, Is.EqualTo(EntityEffectLifetime.Timed));
            Assert.That(effects.GetAt(0).RemainingSeconds, Is.EqualTo(1));
            var snapshot = player.Capture("GameplayScene");
            player.Tick(1.1f);
            Assert.That(effects.Count, Is.Zero);
            Assert.That(player.GetComponent<Collider2D>().excludeLayers.value, Is.Zero);
            Assert.That(snapshot, Is.Not.Null);
        }
        [TestCase("wall")] [TestCase("land")] [TestCase("ground-lost")] [TestCase("stop")]
        public void DashEffects_InterruptNeverCreatesTailOrRemovesOtherSource(string mode)
        {
            var tail = InstallDashEffects();
            bool ground = mode == "ground-lost";
            var player = Character(new Vector2(0, ground ? .9f : 6)); if (ground) Land(player);
            var effects = player.Context.Effects;
            var other = effects.ApplyIndependent(tail, "other");
            Keys(Key.LeftShift); Decide(player); Assert.That(effects.Count, Is.EqualTo(2));
            if (mode == "stop") { player.Stop(); Assert.That(effects.Count, Is.Zero); return; }
            if (mode == "wall") WallBox("InterruptWall", new Vector2(1.02f, 6), new Vector2(1, 12));
            else player.Context.Motor.Body.position = new Vector2(0, ground ? 6 : .9f);
            Physics2D.SyncTransforms(); Decide(player);
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(effects.Count, Is.EqualTo(1));
            Assert.That(effects.GetAt(0).ApplicationId, Is.EqualTo(other.ApplicationId));
            Assert.That(effects.ProtectsDamage("test.contact"), Is.True);
            other.Dispose(); Assert.That(effects.Count, Is.Zero);
        }
        [Test]
        public void DashTailPauseAndRestoreKeepRemainingWithoutActionOrDuplicate()
        {
            InstallDashEffects(); var player = Character(new Vector2(0, 6));
            Keys(Key.LeftShift); Decide(player); FinishDash(player); player.Tick(.25f);
            var effects = player.Context.Effects;
            typeof(PlayerController).GetMethod("Pause", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            player.Tick(5); Assert.That(effects.GetAt(0).RemainingSeconds, Is.EqualTo(.75f));
            typeof(PlayerController).GetMethod("Resume", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            var snapshot = player.Capture("GameplayScene"); player.Tick(2); player.Stop();
            var owner = new object(); var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var candidate = typeof(PlayerController).GetMethod("PrepareSnapshot", flags).Invoke(player, new object[] { owner, _config, snapshot, _input });
            typeof(PlayerController).GetMethod("CommitSnapshot", flags).Invoke(player, new object[] { candidate, null });
            player.StartPrepared(owner);
            Assert.That(player.Context.Dash.IsActive, Is.False);
            Assert.That(player.Context.Effects.Count, Is.EqualTo(1));
            Assert.That(player.Context.Effects.GetAt(0).RemainingSeconds, Is.EqualTo(.75f));
            Assert.That(player.Context.Effects.GetAt(0).SourceKey, Is.EqualTo("air-dash/tail"));
        }
        [Test]
        public void DashEffectPreparationRejectsWrongLifetimeOrMissingContainer()
        {
            var tail = InstallDashEffects(); SetAirField(tail, "_lifetime", EntityEffectLifetime.OwnedThenTimed);
            Assert.Throws<ArgumentException>(() => Character(new Vector2(0, 6)));
            SetAirField(tail, "_lifetime", EntityEffectLifetime.Timed); SetAirField(_config, "_enableEffects", false);
            Assert.Throws<InvalidOperationException>(() => Character(new Vector2(0, 6)));
        }
        [Test]
        public void SuccessiveDashTailsExpireIndependently_AndOtherHostIsIsolated()
        {
            InstallDashEffects(); var player = Character(new Vector2(0, 6)); var other = Character(new Vector2(5, 6));
            Keys(Key.LeftShift); Decide(player); FinishDash(player);
            player.Tick(.5f); Keys(); Decide(player); Keys(Key.LeftShift); Decide(player); FinishDash(player);
            Assert.That(player.Context.Effects.Count, Is.EqualTo(2));
            Assert.That(other.Context.Effects.Count, Is.Zero);
            Assert.That(player.Context.Effects.GetAt(0).ApplicationId, Is.Not.EqualTo(player.Context.Effects.GetAt(1).ApplicationId));
            player.Tick(.3f);
            Assert.That(player.Context.Effects.Count, Is.EqualTo(1));
            Assert.That(player.Context.Effects.GetAt(0).RemainingSeconds, Is.EqualTo(.7f).Within(.0001));
        }
    }
}
