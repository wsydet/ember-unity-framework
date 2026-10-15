#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void WithThreeHit(Action<PlayerController, PlayerScheme> verify, Action<PlayerScheme> customize = null, bool animated = false)
        {
            string name = "CombatPlayTest_" + Guid.NewGuid().ToString("N"), folder = "Assets/" + name;
            AssetDatabase.CreateFolder("Assets", name);
            var original = _config;
            PlayerSchemeCapture capture = null; PlayerController player = null;
            try
            {
                var source = Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/空洞骑士风格");
                var copyType = Type.GetType("Game.Player.Editor.PlayerSchemeStructureWorkflow, Game.Player.Editor", true);
                var scheme = (PlayerScheme)copyType.GetMethod("CopyIndependent").Invoke(null,
                    new object[] { source, folder + "/Scheme.asset", folder + "/Graph.asset", folder + "/Visual.controller" });
                var example = Type.GetType("Game.Player.Editor.PlayerCombatExampleWorkflow, Game.Player.Editor", true);
                var clip = source.Animation.Slots.First(s => s.NodeId == "idle").Clip;
                example.GetMethod("InstallThreeHit").Invoke(null, new object[] { scheme, clip, clip, clip });
                customize?.Invoke(scheme);
                var config = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
                PlayerDashEffectTestAssets.Set(config, "_scheme", scheme);
                capture = new PlayerSchemeCapture(config); _config = capture.Config;
                var animation = _config.CapturedAnimation;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_config, null);
                player = animated ? AnimatedCharacter(animation) : Character(); Land(player); verify(player, scheme);
            }
            finally
            {
                player?.Stop(); capture?.Dispose(); _config = original;
                foreach (string path in new[] { "/Scheme.asset", "/Graph.asset", "/Visual.controller" })
                    foreach (var item in AssetDatabase.LoadAllAssetsAtPath(folder + path)) if (item != null) Undo.ClearUndo(item);
                AssetDatabase.DeleteAsset(folder);
            }
        }
        private void UntilCombat(PlayerController player, string node, int limit = 80)
        {
            for (int i = 0; i < limit && player.CurrentStateId != node; i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(node), player.CaptureFsmObservation().ToString());
        }
        [Test]
        public void AttackVisualUsesFrozenLogicalClockAndIsIndependentOfBodyFlash()
        {
            var first = Keep(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
            var second = Keep(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
            WithThreeHit((player, scheme) =>
            {
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0].Visual, "_enabled", false);
                Keys(Key.J, Key.A); Decide(player);
                var slash = player.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Attack visual (session)");
                Assert.That(slash.enabled, Is.True); Assert.That(slash.sprite, Is.SameAs(first));
                Assert.That(slash.transform.localScale.x, Is.LessThan(0));
                Assert.That(slash.GetComponent<Collider2D>(), Is.Null);
                var body = player.GetComponentInChildren<PlayerAnimationView>().Sprite;
                body.color = new Color(1, 1, 1, .2f);
                Assert.That(slash.color.a, Is.EqualTo(1));
                Keys(); for (int i = 0; i < 6; i++) Simulate(player);
                Assert.That(slash.sprite, Is.SameAs(second));
                player.Pause(); player.Tick(1); player.FixedTick(1);
                Assert.That(slash.sprite, Is.SameAs(second)); player.Resume();
                for (int i = 0; i < 8; i++) Simulate(player);
                Assert.That(slash.enabled, Is.False);
                player.Stop(); Assert.That(slash == null, Is.True);
            }, scheme =>
            {
                var visual = scheme.Feel.Attack.Segments[0].Visual;
                PlayerDashEffectTestAssets.Set(visual, "_enabled", true);
                PlayerDashEffectTestAssets.Set(visual, "_opensAt", 0f);
                PlayerDashEffectTestAssets.Set(visual, "_fps", 10f);
                PlayerDashEffectTestAssets.Set(visual, "_frames", new[] { first, second });
            }, animated: true);
        }

        [Test]
        public void FormalCombatAssetsDealDamageShowSlashAndResetDummy()
        {
            var original = _config; PlayerSchemeCapture capture = null; PlayerController player = null;
            try
            {
                var scheme = Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/通用攻击模板1");
                var config = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
                PlayerDashEffectTestAssets.Set(config, "_scheme", scheme);
                capture = new PlayerSchemeCapture(config); _config = capture.Config;
                var animation = _config.CapturedAnimation;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_config, null);
                player = AnimatedCharacter(animation); Land(player);
                Assert.That(player.Context.Health.Current, Is.EqualTo(10));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/Combat/TrainingDummy.prefab");
                var dummy = Keep(UnityEngine.Object.Instantiate(prefab));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dummy, _scene);
                var motor = dummy.GetComponent<Game.Entity.EntityMotor2D>();
                motor.Freeze(); motor.Apply(player.Context.Motor.Position + Vector2.right * 1.2f, Vector2.zero, 1);
                using var hits = new PlayerHitScene(_scene, player.Context);
                Physics2D.SyncTransforms(); Keys(Key.J, Key.D); Decide(player); Keys();
                  var observedFrames = new System.Collections.Generic.HashSet<Sprite>();
                for (int i = 0; i < 8; i++)
                {
                    player.Tick(.02f); hits.Step(() => player.FixedTick(.02f));
                    _scene.GetPhysicsScene2D().Simulate(.02f);
                      var visible = player.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.name == "Attack visual (session)" && r.enabled);
                      if (visible != null) observedFrames.Add(visible.sprite);
                }
                Assert.That(hits.Actors.Single().Health.Current, Is.EqualTo(28));
                var slash = player.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Attack visual (session)");
                Assert.That(slash.enabled, Is.True);
                Assert.That(AssetDatabase.GetAssetPath(slash.sprite), Does.EndWith("SlashSheet.png"));
                Assert.That(slash.GetComponent<Collider2D>(), Is.Null);
                  for (int i = 0; i < 20; i++)
                  {
                      player.Tick(.02f); hits.Step(() => player.FixedTick(.02f));
                      _scene.GetPhysicsScene2D().Simulate(.02f);
                      if (slash.enabled) observedFrames.Add(slash.sprite);
                  }
                  CollectionAssert.AreEquivalent(scheme.Feel.Attack.Segments[0].Visual.Frames, observedFrames,
                      "正式配置六帧均须由攻击逻辑时钟驱动显示");
                  Assert.That(slash.enabled, Is.False, "本体结束后刀光必须关闭");
                hits.ResetActors();
                Assert.That(hits.Actors.Single().Health.Current, Is.EqualTo(30));
                Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            }
            finally { player?.Stop(); capture?.Dispose(); _config = original; }
        }

        [Test]
        public void ExplicitSelfLinkReentersAttackWithFreshClockAndDirection()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J, Key.D); Decide(player); Keys(Key.A);
                for (int i = 0; i < 13; i++) Simulate(player);
                Assert.That(player.Context.Combat.Progress, Is.GreaterThan(.2));
                Keys(Key.J, Key.A); for (int i = 0; i < 3; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Assert.That(player.Context.Combat.Progress, Is.LessThan(.1));
                Assert.That(player.Context.Facing.FacesRight, Is.False);
                Assert.That(player.Context.Input.Combat.TryGet(PlayerCombatButton.Attack, out _), Is.False);
            }, scheme =>
            {
                PlayerDashEffectTestAssets.Set(scheme.Combo.Connections[0], "_normal", "first");
                var graph = scheme.Graph;
                graph.Configure(graph.EntryId, graph.Nodes.Select(node =>
                {
                    var pair = scheme.CombatGraph.Nodes.FirstOrDefault(p => p.AttackNodeId == node.Id || p.RecoveryNodeId == node.Id);
                    return new Ember.Core.Fsm.EmberFsmNode(node.Id, node.DisplayName, node.Definition, node.ParentId, node.DefaultChildId,
                        pair == null ? node.Bindings.ToArray() : scheme.CombatGraph.Bindings(pair, scheme.Combo,
                            ((IPlayerCombatNodeDefinition)node.Definition).Role));
                }).ToArray());
            });
        }

        [Test]
        public void ThreeHitEarlyLinksLockFacingAndLastRecoveryRequiresFreshPress()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J, Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Keys(Key.A); for (int i = 0; i < 11; i++) Simulate(player);
                Assert.That(player.Context.Facing.FacesRight, Is.True, "本刀移动时仍锁向");
                Keys(Key.J, Key.A); UntilCombat(player, "combat.attack.second", 8);
                Assert.That(player.Context.Facing.FacesRight, Is.False, "下一刀读取最新方向");
                Keys(Key.D); for (int i = 0; i < 12; i++) Simulate(player);
                Keys(Key.J, Key.D); UntilCombat(player, "combat.attack.third", 8);
                Assert.That(player.Context.Facing.FacesRight, Is.True);
                Keys(); for (int i = 0; i < 22; i++) Simulate(player);
                Keys(Key.J); UntilCombat(player, "combat.recovery.third", 12);
                Assert.That(player.Context.Input.AttackRequested, Is.False, "末刀本体旧缓存不能补起手");
                Simulate(player); Assert.That(player.CurrentStateId, Is.EqualTo("combat.recovery.third"));
                Keys(); Decide(player); Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Keys(); UntilCombat(player, "combat.recovery.first");
                Assert.That(player.Context.Combat.IsRecovery, Is.True, "没有收刀 Clip 仍执行逻辑收刀");
                UntilCombat(player, "idle");
            });
        }

        [Test]
        public void AttackJumpWaitsForGateThenConsumesOnceAndReleasesControl()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player); Keys();
                for (int i = 0; i < 6; i++) Simulate(player);
                Keys(Key.Space); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Assert.That(player.Context.Input.JumpRequested, Is.True);
                UntilCombat(player, "jump", 10);
                Assert.That(player.Context.Combat.IsActive, Is.False);
                Assert.That(player.Context.Input.JumpRequested, Is.False);
                Assert.That(player.Context.MotionControl.Owns(PlayerMotionChannels.Horizontal), Is.False);
            });
        }

        [Test]
        public void StationaryMoveCancelBreaksChainAndGroundAttackCannotContinueInAir()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J, Key.D); Decide(player);
                for (int i = 0; i < 8; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
                Keys(Key.D); UntilCombat(player, "move", 12);
                Assert.That(player.Context.Combat.IsActive, Is.False);
                Keys(); Decide(player); Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                player.Context.Motor.Body.position += Vector2.up * 3; Physics2D.SyncTransforms();
                Keys(); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
                Assert.That(player.Context.Combat.IsActive, Is.False);
            }, scheme => PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0], "_motion", PlayerAttackMotion.Stationary));
        }

        [Test]
        public void OptionalAttackAbilityNotInstalledRejectsActionWithoutBreakingMovement()
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Assert.That(player.Context.Input.AttackRequested, Is.True, "拒绝没有消费请求");
                Keys(Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("move"));
                Assert.That(player.Context.Combat.IsActive, Is.False);
            }, scheme => PlayerDashEffectTestAssets.Set(scheme.Powers, "_abilities",
                scheme.Powers.AbilitySlots.Where(slot => slot.Definition.Id != "basic-attack").ToArray()));
        }

        [TestCase(PlayerAttackEnvironment.Air)]
        [TestCase(PlayerAttackEnvironment.Both)]
        public void AirAttackLandingCancelsOnlyAirExclusiveSegment(PlayerAttackEnvironment environment)
        {
            WithThreeHit((player, scheme) =>
            {
                Place(player, new Vector2(0, 4), -1); Keys(); Decide(player);
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Place(player, new Vector2(0, .875f), 0); Keys(); Decide(player);
                Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True);
                if (environment == PlayerAttackEnvironment.Air)
                {
                    Assert.That(player.Context.Combat.IsActive, Is.False);
                    Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                }
                else Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
            }, scheme =>
            {
                var attack = PlayerCombatData.Copy(scheme.Feel.Attack);
                PlayerDashEffectTestAssets.Set(attack.Segments[0], "_environment", environment);
                var combo = PlayerCombatData.Copy(scheme.Combo);
                PlayerDashEffectTestAssets.Set(combo, "_groundEntry", null);
                PlayerDashEffectTestAssets.Set(combo, "_airEntry", "first");
                AssetDatabase.SaveAssetIfDirty(scheme.Animation.Controller);
                Type.GetType("Game.Player.Editor.PlayerCombatExampleWorkflow, Game.Player.Editor", true)
                    .GetMethod("ApplyConfiguration").Invoke(null, new object[] { scheme, attack, combo, null });
            });
        }

        [Test]
        public void AttackDisabledKeepsIndependentGraphPlayableAsMovement()
        {
            WithThreeHit((player, scheme) =>
            {
                Assert.That(player.Context.Combat, Is.Null);
                Keys(Key.J, Key.D); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            }, scheme => PlayerDashEffectTestAssets.Set(scheme.Feel.Attack, "_enabled", false));
        }

        [TestCase(false)] [TestCase(true)]
        public void AttackOwnsWallAndLedgeUntilRecoveryFinishes(bool ledge)
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player);
                if (ledge) LedgePlatform();
                else WallBox("Combat wall", new Vector2(1.02f, 5), new Vector2(1, 12));
                Place(player, new Vector2(0, ledge ? 3.2f : 5), -1); Keys(Key.D); Decide(player);
                Assert.That(ledge ? player.Context.Ledge.TryCandidate(out _) : player.Context.CanStartWallSlide, Is.True);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                Assert.That(player.Context.Wall.Sliding || player.Context.Ledge.IsActive, Is.False);
                for (int i = 0; i < 60 && player.Context.Combat.IsActive; i++) Decide(player);
                Decide(player); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(ledge ? "ledge-hang" : "wall-slide"));
            }, scheme =>
            {
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0], "_environment", PlayerAttackEnvironment.Both);
                var ids = new[] { "wall-slide", "ledge-hang" };
                var slots = scheme.Powers.AbilitySlots.ToList();
                foreach (var id in ids)
                {
                    var definition = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
                    PlayerDashEffectTestAssets.Set(definition, "_id", id);
                    PlayerDashEffectTestAssets.Set(definition, "_defaultOpen", true);
                    PlayerDashEffectTestAssets.Set(definition, "_limitedUses", false);
                    slots.RemoveAll(s => s.Definition.Id == id); slots.Add(new Game.Entity.EntityAbilitySlot(definition));
                }
                PlayerDashEffectTestAssets.Set(scheme.Powers, "_abilities", slots.ToArray());
            });
        }

        [Test]
        public void FourthAttackFromPublicConfigurationEntryPlaysAndHits()
        {
            WithThreeHit((player, scheme) =>
            {
                var dummy = HitEntity("fourth target", player.Context.Motor.Position + Vector2.right * .2f, true)
                    .gameObject.AddComponent<Game.Entity.EntityHitActor>();
                PlayerDashEffectTestAssets.Set(dummy, "_stableId", "fourth-target");
                using var world = new PlayerHitScene(_scene, player.Context);
                Keys(Key.J); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.fourth"));
                world.Step(() => player.FixedTick(.02f));
                Assert.That(world.Actors.Single().Health.Current, Is.EqualTo(99));
                Keys(); UntilCombat(player, "combat.recovery.fourth"); UntilCombat(player, "idle");
            }, scheme =>
            {
                var attack = PlayerCombatData.Copy(scheme.Feel.Attack);
                var fourth = PlayerCombatData.Copy(attack.Segments[0]);
                PlayerDashEffectTestAssets.Set(fourth, "_id", "fourth");
                PlayerDashEffectTestAssets.Set(fourth, "_windows", HitSegment("{\"_kind\":0,\"_size\":{\"x\":2,\"y\":2}}").Windows.ToArray());
                PlayerDashEffectTestAssets.Set(attack, "_segments", attack.Segments.Concat(new[] { fourth }).ToArray());
                var combo = PlayerCombatData.Copy(scheme.Combo); PlayerDashEffectTestAssets.Set(combo, "_groundEntry", "fourth");
                AssetDatabase.SaveAssetIfDirty(scheme.Animation.Controller);
                var clips = new System.Collections.Generic.Dictionary<string, AnimationClip>
                { ["combat.attack.fourth"] = scheme.Animation.Slots.First(s => s.NodeId == "idle").Clip };
                Type.GetType("Game.Player.Editor.PlayerCombatExampleWorkflow, Game.Player.Editor", true)
                    .GetMethod("ApplyConfiguration").Invoke(null, new object[] { scheme, attack, combo, clips });
            }, animated: true);
        }

        [TestCase(false, false)] [TestCase(true, false)]
        [TestCase(false, true)] [TestCase(true, true)]
        public void SharedAndIndependentAttackAbilitiesSpendOnlyEnabledSegments(bool independent, bool disableThird)
        {
            WithThreeHit((player, scheme) =>
            {
                Keys(Key.J); Decide(player); Keys(); for (int i = 0; i < 11; i++) Simulate(player);
                Keys(Key.J); UntilCombat(player, "combat.attack.second", 8);
                Keys(); for (int i = 0; i < 12; i++) Simulate(player);
                Keys(Key.J); for (int i = 0; i < 6; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(disableThird ? "combat.attack.second" : "combat.attack.third"));
                player.Context.Abilities.TryGet(independent ? "combat-test-third" : "basic-attack", out var last);
                Assert.That(last.RemainingUses, Is.EqualTo(disableThird ? 1 : 0));
                if (independent)
                    foreach (var id in new[] { "combat-test-first", "combat-test-second" })
                    { player.Context.Abilities.TryGet(id, out var used); Assert.That(used.RemainingUses, Is.Zero); }
            }, scheme =>
            {
                var basic = scheme.Powers.AbilitySlots.First(s => s.Definition.Id == "basic-attack").Definition;
                PlayerDashEffectTestAssets.Set(basic, "_limitedUses", true);
                PlayerDashEffectTestAssets.Set(basic, "_maximumUses", independent ? 1 : 3);
                if (independent)
                {
                    var slots = scheme.Powers.AbilitySlots.ToList();
                    foreach (var segment in scheme.Feel.Attack.Segments)
                    {
                        var definition = UnityEngine.Object.Instantiate(basic);
                        string id = "combat-test-" + segment.Id;
                        PlayerDashEffectTestAssets.Set(definition, "_id", id);
                        AssetDatabase.AddObjectToAsset(definition, scheme);
                        slots.Add(new Game.Entity.EntityAbilitySlot(definition));
                        PlayerDashEffectTestAssets.Set(segment, "_abilityId", id);
                    }
                    PlayerDashEffectTestAssets.Set(scheme.Powers, "_abilities", slots.ToArray());
                }
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[2], "_enabled", !disableThird);
            });
        }

        [Test]
        public void DedicatedRecoveryMovementAndClipCompletionOnlyChangeVisuals()
        {
            WithThreeHit((player, scheme) =>
            {
                var animator = player.GetComponentInChildren<Animator>(); var session = AnimationSession(player);
                var recovery = scheme.Animation.Slots.First(s => s.NodeId == "combat.recovery.first");
                Keys(Key.J); Decide(player); Keys(); UntilCombat(player, recovery.NodeId);
                double progress = player.Context.Combat.Progress;
                animator.Update(recovery.Clip.length + .1f); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(recovery.NodeId));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    Is.EqualTo(Animator.StringToHash(scheme.Animation.Slots.First(s => s.NodeId == "idle").State)));
                Keys(Key.D); Decide(player);
                Assert.That(session.Override[recovery.Key], Is.EqualTo(recovery.MovingRecoveryClip));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, Is.EqualTo(Animator.StringToHash(recovery.State)));
                Assert.That(player.Context.Combat.Progress, Is.GreaterThan(progress));
                Keys(); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo(recovery.NodeId));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    Is.EqualTo(Animator.StringToHash(scheme.Animation.Slots.First(s => s.NodeId == "idle").State)));
            }, scheme =>
            {
                var recovery = scheme.Animation.Slots.First(s => s.NodeId == "combat.recovery.first");
                PlayerDashEffectTestAssets.Set(recovery, "_clip", scheme.Animation.Slots.First(s => s.NodeId == "idle").Clip);
                PlayerDashEffectTestAssets.Set(recovery, "_movingRecoveryClip", scheme.Animation.Slots.First(s => s.NodeId == "move").Clip);
            }, true);
        }

        [TestCase("entry")] [TestCase("normal")]
        [TestCase("delayed")] [TestCase("dash")]
        public void DisabledTargetIsRejectedByEveryCombatRouteWithoutSpending(string route)
        {
            string target = route == "entry" ? "first" : route == "normal" ? "second" : route == "delayed" ? "special" : "third";
            WithThreeHit((player, scheme) =>
            {
                player.Context.Abilities.TryGet(route == "delayed" ? "test-special" : "basic-attack", out var ability);
                if (route == "normal")
                { Keys(Key.J); Decide(player); Keys(); for (int i = 0; i < 11; i++) Simulate(player); }
                if (route == "dash")
                { Keys(Key.LeftShift, Key.D); Decide(player); Keys(); for (int i = 0; i < 6; i++) Simulate(player); }
                if (route == "delayed")
                {
                    EnterSecondBody(player); UntilCombat(player, "combat.recovery.second");
                    Assert.That(player.Context.Combat.IsDelayedRecovery, Is.False);
                    Assert.That(player.Context.Effects.CaptureActive().Any(e => e.EffectId == "CanEAttack"), Is.False);
                }
                var remaining = ability.RemainingUses;
                if (route != "delayed")
                {
                    Keys(Key.J); Decide(player);
                    Assert.That(player.CurrentStateId, Is.Not.EqualTo("combat.attack." + target));
                    Assert.That(player.Context.Input.AttackRequested, Is.True, "拒绝不消费有效请求");
                }
                Assert.That(ability.RemainingUses, Is.EqualTo(remaining));
            }, scheme =>
            {
                InstallDelayedTest(scheme, limited: true); InstallCombatDashTest(scheme, dedicated: true);
                var basic = scheme.Powers.AbilitySlots.First(s => s.Definition.Id == "basic-attack").Definition;
                PlayerDashEffectTestAssets.Set(basic, "_limitedUses", true); PlayerDashEffectTestAssets.Set(basic, "_maximumUses", 10);
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments.First(s => s.Id == target), "_enabled", false);
            });
        }

        [Test]
        public void AttackAnimationSpeedPauseAndRecoveryMovementVariantKeepLogicalClock()
        {
            WithThreeHit((player, scheme) =>
            {
                var animator = player.GetComponentInChildren<Animator>(); var session = AnimationSession(player);
                Keys(Key.J); Decide(player);
                Assert.That(animator.speed, Is.EqualTo(2));
                Keys(); Simulate(player);
                double progress = player.Context.Combat.Progress;
                Assert.That(progress, Is.EqualTo(.04).Within(.00001));
                player.Pause(); player.FixedTick(.2f); animator.Update(.2f);
                Assert.That(player.Context.Combat.Progress, Is.EqualTo(progress)); Assert.That(animator.speed, Is.Zero);
                player.Resume(); Assert.That(animator.speed, Is.EqualTo(2));
                PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0], "_duration", 9f);
                UntilCombat(player, "combat.recovery.first", 15);
                Assert.That(animator.speed, Is.EqualTo(1));
                var recovery = scheme.Animation.Slots.First(s => s.NodeId == "combat.recovery.first");
                Assert.That(session.Override[recovery.Key], Is.EqualTo(scheme.Animation.Slots.First(s => s.NodeId == "idle").Clip));
                Keys(Key.D); Decide(player); progress = player.Context.Combat.Progress; int plays = session.PlayCount;
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    Is.EqualTo(Animator.StringToHash(scheme.Animation.Slots.First(s => s.NodeId == "move").State)));
                Decide(player); Assert.That(session.PlayCount, Is.EqualTo(plays));
                Keys(); Decide(player);
                Assert.That(session.NodeId, Is.EqualTo("combat.recovery.first"));
                Assert.That(player.Context.Combat.Progress, Is.GreaterThan(progress));
                Assert.That(session.PlayCount, Is.EqualTo(plays + 1));
            }, scheme => PlayerDashEffectTestAssets.Set(scheme.Feel.Attack.Segments[0], "_speed", 2f), true);
        }
    }
}
#endif
