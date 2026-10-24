#if UNITY_EDITOR
using System;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void InstallLifeTest(PlayerScheme scheme, bool protection = false, bool reaction = true)
        {
            AssetDatabase.SaveAssetIfDirty(scheme.Animation.Controller);
            var life = JsonUtility.FromJson<PlayerLifeConfiguration>("{\"_enabled\":true,\"_maximum\":10,\"_initial\":10,\"_multiplier\":1,\"_death\":1}");
            var reactions = JsonUtility.FromJson<PlayerReactionConfiguration>("{\"_enabled\":" + (reaction ? "true" : "false") +
                ",\"_invulnerability\":" + (protection ? "true" : "false") + ",\"_invulnerabilitySeconds\":0.5,\"_protectReaction\":true,\"_damageCategories\":[\"physical\"]}");
            PlayerDashEffectTestAssets.Set(scheme, "_enableEffects", true);
            Type.GetType("Game.Player.Editor.PlayerCombatExampleWorkflow, Game.Player.Editor", true).GetMethod("InstallLife").Invoke(null,
                new object[] { scheme, life, reactions, null, null });
        }
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void LifeInterruptClearsDashBodyAndTail(bool tail, bool lethal)
        {
            WithThreeHit((player, scheme) =>
            {
                AttackIntoDash(player); Keys();
                if (tail) UntilCombat(player, "combat.dash-recovery", 30);
                player.Context.HitReceiver.Receive(TestHit(damage: lethal ? 20 : 1));
                Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(lethal ? "life.dead" : "life.hit"));
                Assert.That(player.Context.Combat.IsActive, Is.False);
                Assert.That(player.Context.Combat.Dash.HasResidualMotion, Is.False);
                Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.False);
                Keys(Key.J, Key.LeftShift); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(lethal ? "life.dead" : "life.hit"));
            }, scheme => { InstallCombatDashTest(scheme); InstallLifeTest(scheme); });
        }
        [TestCase(PlayerRecoveryHealthMode.Full, 10)]
        [TestCase(PlayerRecoveryHealthMode.Fixed, 4)]
        [TestCase(PlayerRecoveryHealthMode.Ratio, 5)]
        [TestCase(PlayerRecoveryHealthMode.Saved, 7)]
        public void RecoveryPolicyBuildsCandidateWithoutMutatingDeadPlayer(PlayerRecoveryHealthMode mode, float expected)
        {
            WithThreeHit((player, scheme) =>
            {
                player.Context.HitReceiver.Receive(TestHit(damage: 3));
                var checkpoint = player.Capture(_scene.name);
                player.Context.HitReceiver.Receive(TestHit(damage: 20)); Decide(player);
                var spawn = new Vector2(7, 9);
                var candidate = player.Context.Life.RecoverySnapshot(checkpoint, spawn);
                Assert.That(candidate.Life.Health, Is.EqualTo(expected));
                Assert.That(candidate.Position, Is.EqualTo(spawn));
                Assert.That(player.Context.Health.Current, Is.Zero);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.dead"));
            }, scheme =>
            {
                InstallLifeTest(scheme, reaction: false);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_health", mode);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_position", PlayerRecoveryPositionMode.Spawn);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_fixedHealth", 4f);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_healthRatio", .5f);
            });
        }
        private EntityHit TestHit(float damage = 1, float stun = .3f, float x = 4, float y = 5, int priority = 1)
        {
            var payload = new EntityHitPayload();
            PlayerDashEffectTestAssets.Set(payload, "_damage", damage); PlayerDashEffectTestAssets.Set(payload, "_stunSeconds", stun);
            PlayerDashEffectTestAssets.Set(payload, "_knockbackVelocity", new Vector2(x, y)); PlayerDashEffectTestAssets.Set(payload, "_priority", priority);
            PlayerDashEffectTestAssets.Set(payload, "_braking", 10f); return new EntityHit(payload, 1);
        }
        [Test]
        public void MissingCustomLifeProvidersRejectWithoutChangingCurrentSession()
        {
            WithThreeHit((player, scheme) =>
            {
                var current = player.Context;
                PlayerDashEffectTestAssets.Set(scheme.Life, "_death", PlayerDeathMode.Custom);
                PlayerDashEffectTestAssets.Set(scheme.Life, "_customDeathHandler", "missing");
                var config = new PlayerCombatConfiguration(scheme, scheme.Feel.Attack);
                StringAssert.Contains("自定义死亡", Assert.Throws<InvalidOperationException>(() => new PlayerLifeSession(current, config)).Message);
                PlayerDashEffectTestAssets.Set(scheme.Life, "_death", PlayerDeathMode.ConfirmRecovery);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_position", PlayerRecoveryPositionMode.Custom);
                PlayerDashEffectTestAssets.Set(scheme.Recovery, "_customPositionProvider", "missing");
                config = new PlayerCombatConfiguration(scheme, scheme.Feel.Attack);
                StringAssert.Contains("自定义恢复位置", Assert.Throws<InvalidOperationException>(() => new PlayerLifeSession(current, config)).Message);
                Assert.That(player.Context, Is.SameAs(current)); Assert.That(current.Health.Current, Is.EqualTo(10));
            }, scheme => InstallLifeTest(scheme, reaction: false));
        }
        [Test]
        public void ExplicitV2MigrationPreservesResourcesAndStillValidatesCandidateHealth()
        {
            WithThreeHit((player, scheme) =>
            {
                var saved = player.Capture(_scene.name); var codec = new PlatformerSnapshotCodec();
                var current = codec.Encode(saved).Payload;
                int offset = LifeOffset(current);
                var legacy = new byte[current.Length - 5];
                Array.Copy(current, legacy, offset); Array.Copy(current, offset + 5, legacy, offset, legacy.Length - offset);
                BitConverter.GetBytes(2).CopyTo(legacy, 0); var original = (byte[])legacy.Clone();
                Assert.That(codec.Decode(legacy, 2).Status, Is.EqualTo(Game.PlayerData.DecodeStatus.DataVersionUnsupported));
                var migrated = new PlatformerSnapshotCodec(new V2LifeMigration(6)).Decode(legacy, 2);
                Assert.That(migrated.Status, Is.EqualTo(Game.PlayerData.DecodeStatus.Decoded));
                Assert.That(migrated.Progress.Life.Health, Is.EqualTo(6));
                Assert.That(migrated.Progress.Powers.AbilityCount, Is.EqualTo(saved.Powers.AbilityCount));
                using (var candidate = player.PrepareSnapshot(new object(), _config, migrated.Progress, _input))
                    Assert.That(candidate.Context.Health.Current, Is.EqualTo(6));
                var invalid = new PlatformerSnapshotCodec(new V2LifeMigration(11)).Decode(legacy, 2);
                Assert.Throws<ArgumentOutOfRangeException>(() => player.PrepareSnapshot(new object(), _config, invalid.Progress, _input));
                CollectionAssert.AreEqual(original, legacy); Assert.That(player.Context.Health.Current, Is.EqualTo(10));
            }, scheme => InstallLifeTest(scheme, reaction: false));
        }
        private static int LifeOffset(byte[] bytes)
        {
            using var stream = new System.IO.MemoryStream(bytes);
            using var reader = new System.IO.BinaryReader(stream);
            reader.ReadInt32(); reader.ReadString();
            for (int i = 0; i < 4; i++) reader.ReadSingle();
            reader.ReadBoolean(); return (int)stream.Position;
        }
        // 隔离项目迁移示例：显式选择新增生命值，旧能力和效果字节保持，最终仍交给生产候选校验。
        private sealed class V2LifeMigration : IPlatformerSnapshotMigration
        {
            private readonly float _health;
            internal V2LifeMigration(float health) => _health = health;
            public Game.PlayerData.DecodeResult<PlatformerSnapshot> Migrate(ReadOnlyMemory<byte> payload, int version)
            {
                if (version != 2) return new PlatformerSnapshotCodec().Decode(payload, version);
                var old = payload.ToArray(); int offset = LifeOffset(old); var bytes = new byte[old.Length + 5];
                Array.Copy(old, bytes, offset); bytes[offset] = 1; BitConverter.GetBytes(_health).CopyTo(bytes, offset + 1);
                Array.Copy(old, offset, bytes, offset + 5, old.Length - offset); BitConverter.GetBytes(3).CopyTo(bytes, 0);
                return new PlatformerSnapshotCodec().Decode(bytes, 3);
            }
        }
        [Test]
        public void HitPreemptsAttackAndLandingKeepsRemainingStun()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player); Assert.That(player.Context.Combat.IsActive, Is.True);
                var result = player.Context.HitReceiver.Receive(TestHit(stun: .6f));
                Assert.That(result.Outcome, Is.EqualTo(EntityHitOutcome.DamageAndReaction));
                Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("life.hit"));
                Assert.That(player.Context.Combat.IsActive, Is.False); Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(4).Within(.01));
                Keys(Key.J, Key.Space, Key.D); for (int i = 0; i < 8; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.hit"));
                Place(player, new Vector2(0, .9f), -1);
                for (int i = 0; i < 4; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.hit")); Assert.That(player.Context.Life.Reaction.Remaining, Is.GreaterThan(.2f));
                Keys(); for (int i = 0; i < 40; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.Not.EqualTo("life.hit"));
            }, scheme => InstallLifeTest(scheme));
        }
        [Test]
        public void ReactionPriorityReplacesVelocityWithoutAddingImpulseAndProtectionKeepsFirstReaction()
        {
            WithThreeHit((player, scheme) =>
            {
                var receiver = player.Context.HitReceiver;
                Assert.That(receiver.Receive(TestHit()).ReactionAccepted, Is.True);
                Assert.That(receiver.Receive(TestHit()).Accepted, Is.False, "本击建立的保护影响下一击");
                Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("life.hit"));
                Assert.That(player.Context.Health.Current, Is.EqualTo(9));
                player.Context.Effects.Tick(.6f);
                Assert.That(receiver.Receive(TestHit(stun: .5f, x: -6, y: 2)).ReactionAccepted, Is.True);
                Decide(player); Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-6).Within(.01));
                Assert.That(player.Context.Life.Reaction.Remaining, Is.GreaterThanOrEqualTo(.49));
                player.Context.Effects.Tick(.6f);
                Assert.That(receiver.Receive(TestHit(priority: 0)).ReactionAccepted, Is.False);
                Assert.That(player.Context.Health.Current, Is.EqualTo(7), "低优先级仍可扣血");
            }, scheme => InstallLifeTest(scheme, true));
        }
        [Test]
        public void DeathCommitsOnceAndDisabledReactionStillTakesDamage()
        {
            WithThreeHit((player, scheme) =>
            {
                Assert.That(player.Context.HitReceiver.Receive(TestHit()).Outcome, Is.EqualTo(EntityHitOutcome.Damage));
                Decide(player); Assert.That(player.CurrentStateId, Is.Not.EqualTo("life.hit"));
                Assert.That(player.Context.HitReceiver.Receive(TestHit(damage: 20)).Outcome, Is.EqualTo(EntityHitOutcome.Lethal));
                Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("life.dead"));
                Assert.That(player.Context.Life.DeathCommitted, Is.True);
                Assert.That(player.Context.HitReceiver.Receive(TestHit()).Accepted, Is.False);
                Keys(Key.J, Key.Space, Key.D); for (int i = 0; i < 5; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.dead")); Assert.That(player.Context.Motor.Velocity, Is.EqualTo(Vector2.zero));
            }, scheme => InstallLifeTest(scheme, reaction: false));
        }
        [Test]
        public void LifeSnapshotPreservesSavedHealthWhileDeathCandidateAppliesFrozenRecoveryPolicy()
        {
            WithThreeHit((player, scheme) =>
            {
                player.Context.HitReceiver.Receive(TestHit(damage: 3));
                var saved = player.Capture(_scene.name); var codec = new PlatformerSnapshotCodec(); var bytes = codec.Encode(saved);
                Assert.That(bytes.DataVersion, Is.EqualTo(3));
                Assert.That(codec.Decode(bytes.Payload, 2).Status, Is.EqualTo(Game.PlayerData.DecodeStatus.DataVersionUnsupported));
                var decoded = codec.Decode(bytes.Payload, 3).Progress;
                Assert.That(decoded.Life.Health, Is.EqualTo(7)); Assert.That(decoded.Life.Installed, Is.True);
                using (var ordinary = player.PrepareSnapshot(new object(), _config, decoded, _input))
                    Assert.That(ordinary.Context.Health.Current, Is.EqualTo(7));
                player.Context.HitReceiver.Receive(TestHit(damage: 20)); Decide(player);
                Assert.Throws<InvalidOperationException>(() => player.Capture(_scene.name));
                var dead = player.Context;
                var restored = dead.Life.RecoverySnapshot(saved, Vector2.one * 100);
                Assert.That(restored.Position, Is.EqualTo(saved.Position)); Assert.That(restored.Life.Health, Is.EqualTo(10));
                PlayerDashEffectTestAssets.Set(scheme.Life, "_maximum", 100f);
                using (var candidate = player.PrepareSnapshot(new object(), _config, restored, _input))
                {
                    Assert.That(candidate.Context.Health.Current, Is.EqualTo(10)); Assert.That(candidate.Context.Health.Maximum, Is.EqualTo(10));
                    Assert.That(candidate.Context.Combat.IsActive, Is.False); Assert.That(candidate.Context.Life.Reaction, Is.Null);
                    Assert.That(player.Context, Is.SameAs(dead)); Assert.That(dead.Health.Current, Is.Zero);
                }
                Assert.Throws<ArgumentOutOfRangeException>(() => player.PrepareSnapshot(new object(), _config, saved.WithRecovery(saved.Position, 11), _input));
                Assert.That(player.Context, Is.SameAs(dead)); Assert.That(player.CurrentStateId, Is.EqualTo("life.dead"));
            }, scheme => InstallLifeTest(scheme, reaction: false));
        }
        [Test]
        public void AttackAndReactionCaptureRejectsTemporaryStateAndLateReactionInputCanResume()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player); Assert.Throws<InvalidOperationException>(() => player.Capture(_scene.name));
                player.Context.HitReceiver.Receive(TestHit(stun: .2f, x: 0, y: 0)); Decide(player);
                Assert.Throws<InvalidOperationException>(() => player.Capture(_scene.name));
                Keys(); for (int i = 0; i < 8; i++) Simulate(player);
                Keys(Key.J); for (int i = 0; i < 4; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
            }, scheme => InstallLifeTest(scheme));
        }
        [Test]
        public void DummyMovesThroughMotorAndResetInvalidatesQueuedHitsEffectsAndReaction()
        {
            WithThreeHit((player, scheme) =>
            {
                var entity = HitEntity("dummy", new Vector2(5, 2), true);
                var actor = entity.gameObject.AddComponent<EntityHitActor>(); PlayerDashEffectTestAssets.Set(actor, "_stableId", "dummy");
                using var world = new PlayerHitScene(_scene, player.Context);
                var runtime = world.Actors[0]; var motor = entity.GetComponent<EntityMotor2D>(); var spawn = motor.Position;
                world.Batch.TryGet(player.Context.Entity, out var source);
                world.Batch.Collect(source, 1, "test", runtime.Target, TestHit()); world.Batch.Resolve();
                for (int i = 0; i < 5; i++) { world.Step(() => { }); _scene.GetPhysicsScene2D().Simulate(.02f); }
                Assert.That(motor.Position.x, Is.GreaterThan(spawn.x)); Assert.That(motor.Position.y, Is.GreaterThan(spawn.y));
                var spec = new EntityEffectSpec("test", EntityEffectKind.Buff, EntityEffectLifetime.Timed, 1,
                    new EntityEffectRules(new[] { "physical" }, true, 0), default);
                var effect = runtime.Effects.ApplyCaptured(spec, "test"); var previous = runtime.Target;
                world.Batch.Collect(source, 2, "old", previous, TestHit(damage: 20));
                world.ResetActors(); world.Batch.Resolve();
                Assert.That(previous.IsValid, Is.False); Assert.That(runtime.Health.Current, Is.EqualTo(100));
                Assert.That(motor.Position, Is.EqualTo(spawn)); Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
                Assert.That(runtime.Reaction.IsActive, Is.False); Assert.That(effect.IsActive, Is.False); Assert.That(runtime.Effects.Count, Is.Zero);
            });
        }
        [Test]
        public void TrapUsesIndependentContactIntervalAndReceiverProtection()
        {
            WithThreeHit((player, scheme) =>
            {
                var entity = HitEntity("trap", player.Context.Motor.Position, false);
                var shape = entity.gameObject.AddComponent<BoxCollider2D>(); shape.isTrigger = true; shape.size = Vector2.one * 2;
                var trap = entity.gameObject.AddComponent<EntityContactTrap>();
                PlayerDashEffectTestAssets.Set(trap, "_stableId", "trap"); PlayerDashEffectTestAssets.Set(trap, "_shape", shape);
                PlayerDashEffectTestAssets.Set(trap, "_interval", .2f);
                using var world = new PlayerHitScene(_scene, player.Context); Physics2D.SyncTransforms();
                world.Step(() => { }); Assert.That(player.Context.Health.Current, Is.EqualTo(9));
                world.Step(() => { }, .1f); Assert.That(player.Context.Health.Current, Is.EqualTo(9));
                world.Step(() => { }, .2f); Assert.That(player.Context.Health.Current, Is.EqualTo(9), "到间隔但仍有无敌，拒绝扣血");
                player.Context.Effects.Tick(.6f); world.Step(() => { }, .21f);
                Assert.That(player.Context.Health.Current, Is.EqualTo(8));
            }, scheme => InstallLifeTest(scheme, protection: true));
        }
        [Test]
        public void TwoRealPlayersTradeLethalHitsAndDoNotShareHealthOrReactions()
        {
            WithThreeHit((player, scheme) =>
            {
                var other = Character(new Vector2(6, 3));
                using var batch = new EntityHitBatch();
                var a = batch.Register(player.Context.Entity, "a", 1, player.Context.HitReceiver);
                var b = batch.Register(other.Context.Entity, "b", 2, other.Context.HitReceiver);
                batch.Collect(b, 1, "blade", a, TestHit(damage: 20));
                batch.Collect(a, 1, "blade", b, TestHit(damage: 20)); batch.Resolve();
                Assert.That(player.Context.Health.Current, Is.Zero); Assert.That(other.Context.Health.Current, Is.Zero);
                Decide(player); Decide(other);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.dead")); Assert.That(other.CurrentStateId, Is.EqualTo("life.dead"));
                Assert.That(player.Context.Health, Is.Not.SameAs(other.Context.Health));
                Assert.That(player.Context.Life.Reaction, Is.Not.SameAs(other.Context.Life.Reaction));
            }, scheme => InstallLifeTest(scheme));
        }
        [Test]
        public void ReactionProtectionDoesNotGrantDamageImmunityAndPauseFreezesStun()
        {
            WithThreeHit((player, scheme) =>
            {
                var armour = new EntityEffectSpec("armour", EntityEffectKind.Buff, EntityEffectLifetime.Timed, 1,
                    new EntityEffectRules(Array.Empty<string>(), true, 0), default);
                using (player.Context.Effects.ApplyCaptured(armour, "test"))
                {
                    Assert.That(player.Context.HitReceiver.Receive(TestHit()).Outcome, Is.EqualTo(EntityHitOutcome.Damage));
                    Assert.That(player.Context.Health.Current, Is.EqualTo(9)); Assert.That(player.Context.Life.Reaction.HasPending, Is.False);
                }
                player.Context.HitReceiver.Receive(TestHit()); Decide(player);
                float remaining = player.Context.Life.Reaction.Remaining;
                player.Pause(); player.Tick(1); player.FixedTick(1);
                Assert.That(player.Context.Life.Reaction.Remaining, Is.EqualTo(remaining));
                player.Resume(); Decide(player); Assert.That(player.Context.Life.Reaction.Remaining, Is.LessThan(remaining));
            }, scheme => InstallLifeTest(scheme));
        }
    }
}
#endif
