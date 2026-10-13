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
        private PlayerAnimationProfile AnimationProfile()
        {
            var source = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Animation;
            Assert.That(source, Is.Not.Null);
            return Keep(UnityEngine.Object.Instantiate(source));
        }
        private PlayerController AnimatedCharacter(PlayerAnimationProfile profile = null)
        {
            var player = Character(); player.Stop();
            var visual = new GameObject("Visual").transform; visual.SetParent(player.transform, false);
            var animation = new GameObject("Animation"); animation.transform.SetParent(visual, false);
            var sprite = new GameObject("Sprite"); sprite.transform.SetParent(animation.transform, false);
            var renderer = sprite.AddComponent<SpriteRenderer>();
            var view = animation.AddComponent<PlayerAnimationView>();
            PlayerDashEffectTestAssets.Set(view, "_visualRoot", visual);
            PlayerDashEffectTestAssets.Set(view, "_sprite", renderer);
            player.Context?.Facing.Configure(visual);
            player.GetComponent<EntityFacing2D>().Configure(visual);
            typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_config, profile ?? AnimationProfile());
            var owner = new object();
            player.PrepareNewGame(owner, _config, _input);
            player.StartPrepared(owner);
            _input.Request(owner, 0, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
            Physics2D.SyncTransforms();
            return player;
        }
        private PlayerAnimationSession AnimationSession(PlayerController player) =>
            ((PlayerRuntimeCandidate)typeof(PlayerController).GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player)).Animation;

        [Test]
        public void Animation_AllNodesHaveIndependentKeysIncludingDisabledActions()
        {
            var profile = AnimationProfile();
            Assert.DoesNotThrow(() => profile.Validate(_config.Graph));
            CollectionAssert.AreEquivalent(_config.Graph.Nodes.Select(n => n.Id), profile.Slots.Select(s => s.NodeId));
            Assert.That(profile.Slots.Count, Is.EqualTo(21));
            Assert.That(profile.Slots.Select(s => s.Key).Distinct().Count(), Is.EqualTo(21));
            Assert.That(profile.Slots.Select(s => s.Clip).Distinct().Count(), Is.EqualTo(21));
        }

        [TestCase("missing")] [TestCase("duplicate-id")] [TestCase("duplicate-key")]
        [TestCase("missing-key")] [TestCase("missing-clip")] [TestCase("state")] [TestCase("speed")]
        [TestCase("event")] [TestCase("alpha")] [TestCase("root")]
        public void Animation_InvalidMappingOrGameplayCurveIsRejected(string fault)
        {
            var profile = AnimationProfile(); var first = profile.Slots[0];
            switch (fault)
            {
                case "missing": PlayerDashEffectTestAssets.Set(profile, "_slots", profile.Slots.Skip(1).ToArray()); break;
                case "duplicate-id": PlayerDashEffectTestAssets.Set(first, "_nodeId", profile.Slots[1].NodeId); break;
                case "duplicate-key": PlayerDashEffectTestAssets.Set(first, "_key", profile.Slots[1].Key); break;
                case "missing-key": PlayerDashEffectTestAssets.Set(first, "_key", Keep(new AnimationClip())); break;
                case "missing-clip": PlayerDashEffectTestAssets.Set(first, "_clip", null); break;
                case "state": PlayerDashEffectTestAssets.Set(first, "_state", "Base Layer.absent"); break;
                case "speed": PlayerDashEffectTestAssets.Set(first, "_speed", float.NaN); break;
                default:
                    var clip = Keep(UnityEngine.Object.Instantiate(first.Clip));
                    PlayerDashEffectTestAssets.Set(first, "_clip", clip);
                    if (fault == "event") clip.events = new[] { new AnimationEvent { functionName = "CommitGameplay" } };
#if UNITY_EDITOR
                    else UnityEditor.AnimationUtility.SetEditorCurve(clip,
                        UnityEditor.EditorCurveBinding.FloatCurve(fault == "root" ? "" : "Sprite",
                            fault == "root" ? typeof(Transform) : typeof(SpriteRenderer),
                            fault == "root" ? "m_LocalPosition.x" : "m_Color.a"), AnimationCurve.Constant(0, 1, .1f));
#endif
                    break;
            }
            Assert.Throws<InvalidOperationException>(() => profile.Validate(_config.Graph));
        }

        [Test]
        public void Animation_StableLeafDoesNotReplayAndSameLeafReentryDoes()
        {
            var player = AnimatedCharacter(); Land(player);
            var session = AnimationSession(player); int before = session.PlayCount;
            for (int i = 0; i < 12; i++) Decide(player);
            Assert.That(session.PlayCount, Is.EqualTo(before));
            Keys(Key.Space); Decide(player);
            Assert.That(session.NodeId, Is.EqualTo("jump"));
            Assert.That(session.PlayCount, Is.EqualTo(before + 1));
            Place(player, new Vector2(0, .9f), 0); Keys(); Keys(Key.Space); Decide(player);
            Assert.That(session.NodeId, Is.EqualTo("jump"));
            Assert.That(session.PlayCount, Is.EqualTo(before + 2));
        }

        [Test]
        public void Animation_PauseFreezesAndStopReleasesOnlyOwnOverride()
        {
            var player = AnimatedCharacter(); Land(player);
            var animator = player.GetComponentInChildren<Animator>();
            var session = AnimationSession(player); var owned = session.Override;
            animator.Update(.2f);
            player.Pause();
            float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            animator.Update(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
            player.Resume(); animator.Update(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(time));
            player.Stop();
            Assert.That(owned == null, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.Null);
        }

        [Test]
        public void Animation_TwoHostsUseIndependentOverridesAndFrozenMaps()
        {
            var profile = AnimationProfile();
            var sourceScheme = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme));
            PlayerDashEffectTestAssets.Set(sourceScheme, "_animation", profile);
            var sourceConfig = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
            PlayerDashEffectTestAssets.Set(sourceConfig, "_scheme", sourceScheme);
            using var firstCapture = new PlayerSchemeCapture(sourceConfig);
            var slot = profile.Slots.First(s => s.NodeId == "idle");
            var replacement = profile.Slots.First(s => s.NodeId == "move").Clip;
            PlayerDashEffectTestAssets.Set(slot, "_clip", replacement);
            PlayerDashEffectTestAssets.Set(slot, "_speed", 2f);
            using var secondCapture = new PlayerSchemeCapture(sourceConfig);
            var first = AnimatedCharacter(firstCapture.Config.Animation);
            // Character() creates a legacy headless fixture before adding its visual binding.
            typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
            var second = AnimatedCharacter(secondCapture.Config.Animation);
            Land(first); Land(second);
            var a = AnimationSession(first); var b = AnimationSession(second);
            Assert.That(a.Override, Is.Not.SameAs(b.Override));
            Assert.That(a.Override[slot.Key], Is.EqualTo(slot.Key));
            Assert.That(b.Override[slot.Key], Is.EqualTo(replacement));
            Assert.That(first.GetComponentInChildren<Animator>().speed, Is.EqualTo(1));
            Assert.That(second.GetComponentInChildren<Animator>().speed, Is.EqualTo(2));
            first.Stop(); Assert.That(b.Override != null, Is.True);
            Assert.That(profile.Controller.animationClips, Does.Contain(slot.Key));
        }

        [TestCase(.05f)] [TestCase(5f)]
        public void Animation_ClipDurationDoesNotChangeJumpMotionOrExit(float duration)
        {
            var plain = Character(new Vector2(-3, .9f)); Land(plain);
            var profile = AnimationProfile();
            var clip = Keep(new AnimationClip());
#if UNITY_EDITOR
            UnityEditor.AnimationUtility.SetEditorCurve(clip,
                UnityEditor.EditorCurveBinding.FloatCurve("Sprite", typeof(Transform), "m_LocalScale.x"), AnimationCurve.Constant(0, duration, 1));
#endif
            foreach (var slot in profile.Slots) PlayerDashEffectTestAssets.Set(slot, "_clip", clip);
            var animated = AnimatedCharacter(profile); Land(animated);
            Keys(Key.Space);
            for (int i = 0; i < 80; i++)
            {
                Decide(plain); Decide(animated);
                Assert.That(animated.CurrentStateId, Is.EqualTo(plain.CurrentStateId));
                Assert.That(animated.Context.Motor.Velocity.y, Is.EqualTo(plain.Context.Motor.Velocity.y).Within(.0001));
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
        }

        [Test]
        public void Animation_RestoreStartsActualStateAtZeroAndCancelPreservesOldBinding()
        {
            var player = AnimatedCharacter(); Land(player);
            var animator = player.GetComponentInChildren<Animator>();
            animator.Update(.4f);
            var snapshot = player.Capture("GameplayScene");
            var old = animator.runtimeAnimatorController;
            var cancelled = player.PrepareSnapshot(new object(), _config, snapshot, _input);
            cancelled.Dispose();
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(old));
            player.Stop();
            var candidate = player.PrepareSnapshot(new object(), _config, snapshot, _input);
            player.CommitSnapshot(candidate);
            Assert.That(AnimationSession(player).NodeId, Is.EqualTo(player.CurrentStateId));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.Zero.Within(.0001));
        }

        [Test]
        public void Animation_FacingAndEffectAlphaAreNotAnimated()
        {
            var player = AnimatedCharacter(); Land(player);
            var view = player.GetComponentInChildren<PlayerAnimationView>();
            view.Sprite.color = new Color(.7f, .8f, .9f, .35f);
            player.Context.Facing.SetFacing(false);
            player.GetComponentInChildren<Animator>().Update(.3f);
            Assert.That(view.Sprite.color.a, Is.EqualTo(.35f));
            Assert.That(view.VisualRoot.localScale.x, Is.EqualTo(-1));
            Assert.That(player.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [TestCase(.05f)] [TestCase(5f)]
        public void Animation_DashLengthDoesNotChangeResourceCostOrExit(float duration)
        {
            InstallDash(); var profile = AnimationProfile();
            var clip = Keep(new AnimationClip());
#if UNITY_EDITOR
            UnityEditor.AnimationUtility.SetEditorCurve(clip,
                UnityEditor.EditorCurveBinding.FloatCurve("Sprite", typeof(Transform), "m_LocalScale.x"), AnimationCurve.Constant(0, duration, 1));
#endif
            PlayerDashEffectTestAssets.Set(profile.Slots.First(s => s.NodeId == "ground-dash"), "_clip", clip);
            var plain = Character(new Vector2(-3, .9f)); Land(plain);
            var animated = AnimatedCharacter(profile); Land(animated);
            Keys(Key.D, Key.LeftShift);
            for (int i = 0; i < 20; i++)
            {
                Decide(plain); Decide(animated);
                Assert.That(animated.CurrentStateId, Is.EqualTo(plain.CurrentStateId));
                Assert.That(animated.Context.Motor.Velocity.x, Is.EqualTo(plain.Context.Motor.Velocity.x));
                Assert.That(DashResource(animated, true).RemainingUses, Is.EqualTo(DashResource(plain, true).RemainingUses));
                _scene.GetPhysicsScene2D().Simulate(Step);
            }
            Assert.That(animated.Context.Dash.IsActive, Is.False);
            Assert.That(DashResource(animated, true).RemainingUses, Is.EqualTo(1));
        }
    }
}
