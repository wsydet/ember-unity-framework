#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        #region 内部方法
        private string SkillFolder()
        {
            string name = "SkillAcceptance_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            return "Assets/" + name;
        }
        private void CloseSkillAssets(string folder)
        {
            foreach (var character in _characters) if (character != null) character.Stop();
            AssetDatabase.DeleteAsset(folder);
        }
        private AnimationClip SkillSpriteClip(string folder, bool loop)
        {
            var sprites = new List<Sprite>();
            for (int i = 0; i < 2; i++)
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                try
                {
                    texture.SetPixels(Enumerable.Repeat(i == 0 ? Color.cyan : Color.magenta, 256).ToArray());
                    texture.Apply();
                    string path = folder + "/idle_000" + (i + 1) + ".png";
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 16;
                    var textureSettings = new TextureImporterSettings();
                    importer.ReadTextureSettings(textureSettings);
                    // 现有玩家根与 Sprite 都居中；统一脚底不等于把 Pivot 改到底部。
                    textureSettings.spritePivot = new Vector2(.5f, .5f);
                    textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                    importer.SetTextureSettings(textureSettings);
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    sprites.Add(AssetDatabase.LoadAssetAtPath<Sprite>(path));
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            Assert.That(sprites.All(s => s != null && s.pivot == new Vector2(8, 8)), Is.True);
            var clip = new AnimationClip { name = "partial-idle", frameRate = 4 };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("Sprite", typeof(SpriteRenderer), "m_Sprite"),
                new[] { new ObjectReferenceKeyframe { time = 0, value = sprites[0] },
                    new ObjectReferenceKeyframe { time = .25f, value = sprites[1] } });
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            Assert.That(clip.length, Is.EqualTo(.5f).Within(.001f));
            AssetDatabase.CreateAsset(clip, folder + "/idle.anim");
            return clip;
        }
        private PlayerAnimationProfile SkillProbeGraph(string folder, bool enabled)
        {
            var source = _config.Graph;
            var definition = ScriptableObject.CreateInstance<PlayerSkillProbeDefinition>();
            AssetDatabase.CreateAsset(definition, folder + "/PlayerSkillProbe.asset");
            var graph = ScriptableObject.CreateInstance<EmberFsmGraph>();
            var nodes = source.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName, n.Definition, n.ParentId,
                n.Id == "ground" ? "skill-probe" : n.DefaultChildId, n.Bindings.ToArray())).ToList();
            nodes.Add(new EmberFsmNode("skill-probe", "Skill 展示验收", definition, "ground", null,
                new EmberFsmTargetBinding("idle", "idle")));
            graph.Configure(source.EntryId, nodes.ToArray());
            AssetDatabase.CreateAsset(graph, folder + "/ProbeGraph.asset");
            Assert.That(graph.Inspect(typeof(PlayerContext)).IsValid, Is.True);
            _config.Configure(graph, _config.Spawn);
            var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            PlayerDashEffectTestAssets.Set(ability, "_id", PlayerSkillProbeState.ABILITY_ID);
            PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", true);
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            PlayerDashEffectTestAssets.Set(powers, "_abilities", new[] { new EntityAbilitySlot(ability, enabled) });
            PlayerDashEffectTestAssets.Set(_config, "_powers", powers);

            var sourceProfile = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Animation;
            Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(sourceProfile.Controller), folder + "/Probe.controller"), Is.True);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "/Probe.controller");
            var clip = SkillSpriteClip(folder, false);
            var key = UnityEngine.Object.Instantiate(clip); key.name = "skill-probe-key";
            AssetDatabase.CreateAsset(key, folder + "/probe-key.anim");
            var state = controller.layers[0].stateMachine.AddState("skill-probe");
            state.motion = key; state.writeDefaultValues = false;
            var profile = UnityEngine.Object.Instantiate(sourceProfile);
            using (var data = new SerializedObject(profile))
            {
                data.FindProperty("_controller").objectReferenceValue = controller;
                var slots = data.FindProperty("_slots"); slots.arraySize++;
                var slot = slots.GetArrayElementAtIndex(slots.arraySize - 1);
                slot.FindPropertyRelative("_nodeId").stringValue = "skill-probe";
                slot.FindPropertyRelative("_state").stringValue = "Base Layer.skill-probe";
                slot.FindPropertyRelative("_key").objectReferenceValue = key;
                slot.FindPropertyRelative("_clip").objectReferenceValue = clip;
                slot.FindPropertyRelative("_speed").floatValue = 1;
                slot.FindPropertyRelative("_blendSeconds").floatValue = 0;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.CreateAsset(profile, folder + "/ProbeAnimation.asset");
            AssetDatabase.SaveAssetIfDirty(controller); AssetDatabase.SaveAssetIfDirty(profile);
            AssetDatabase.ImportAsset(folder + "/ProbeAnimation.asset", ImportAssetOptions.ForceUpdate);
            profile = AssetDatabase.LoadAssetAtPath<PlayerAnimationProfile>(folder + "/ProbeAnimation.asset");
            profile.Validate(graph);
            return profile;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [TestCase(false)] [TestCase(true)]
        public void Skill_PartialFrames_ImportReloadPlaybackPreservesOtherSlotsAndPhysics(bool loop)
        {
            string folder = SkillFolder();
            try
            {
                var profile = UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Animation);
                var originals = profile.Slots.ToDictionary(s => s.NodeId, s => s.Clip);
                var clip = SkillSpriteClip(folder, loop);
                PlayerDashEffectTestAssets.Set(profile.Slots.Single(s => s.NodeId == "idle"), "_clip", clip);
                AssetDatabase.CreateAsset(profile, folder + "/Animation.asset");
                AssetDatabase.SaveAssetIfDirty(profile);
                AssetDatabase.ImportAsset(folder + "/Animation.asset", ImportAssetOptions.ForceUpdate);
                profile = AssetDatabase.LoadAssetAtPath<PlayerAnimationProfile>(folder + "/Animation.asset");
                profile.Validate(_config.Graph);
                Assert.That(profile.Slots.Where(s => s.NodeId != "idle").All(s => s.Clip == originals[s.NodeId]), Is.True);
                var frames = AnimationUtility.GetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("Sprite", typeof(SpriteRenderer), "m_Sprite"));
                var player = AnimatedCharacter(profile); Land(player);
                var animator = player.GetComponentInChildren<Animator>();
                var view = player.GetComponentInChildren<PlayerAnimationView>();
                animator.Play("Base Layer.idle", 0, 0); animator.Update(0);
                Assert.That(view.Sprite.sprite, Is.SameAs(frames[0].value));
                view.Sprite.color = new Color(1, 1, 1, .35f);
                animator.Update(.3f);
                Assert.That(view.Sprite.sprite, Is.SameAs(frames[1].value));
                animator.Update(.3f);
                Assert.That(view.Sprite.sprite, Is.SameAs(frames[loop ? 0 : 1].value));
                player.Context.Facing.SetFacing(false);
                Assert.That(view.VisualRoot.localScale.x, Is.EqualTo(-1));
                Assert.That(view.Sprite.color.a, Is.EqualTo(.35f));
                Assert.That(player.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(1, 1.75f)));
                player.Pause(); float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                animator.Update(.3f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
                player.Resume();
                PlayerDashEffectTestAssets.Set(profile.Slots.Single(s => s.NodeId == "idle"), "_clip", null);
                Assert.Throws<InvalidOperationException>(() => profile.Validate(_config.Graph));
            }
            finally { CloseSkillAssets(folder); }
        }

        [TestCase(false)] [TestCase(true)]
        public void Skill_NewLeaf_CompleteBindingAnimationPauseExitAndDisabledFallback(bool enabled)
        {
            string folder = SkillFolder();
            try
            {
                var profile = SkillProbeGraph(folder, enabled);
                var player = AnimatedCharacter(profile);
                bool entered = player.CurrentStateId == "skill-probe";
                for (int i = 0; i < 30 && !entered; i++) { Simulate(player); entered = player.CurrentStateId == "skill-probe"; }
                Assert.That(entered, Is.EqualTo(enabled));
                if (!enabled) { Assert.That(player.CurrentStateId, Is.EqualTo("idle")); return; }
                Assert.That(AnimationSession(player).NodeId, Is.EqualTo("skill-probe"));
                var snap = player.Capture("GameplayScene"); // 纯展示不接管；只保存已有事实，不保存展示计时。
                Assert.That(snap.Position, Is.EqualTo(player.Context.Motor.Position));
                player.Pause(); for (int i = 0; i < 20; i++) Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("skill-probe"));
                player.Resume();
                for (int i = 0; i < 8; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Assert.That(AnimationSession(player).NodeId, Is.EqualTo("idle"));
                Assert.That(player.Context.Powers.Abilities.TryGet(PlayerSkillProbeState.ABILITY_ID, out var power), Is.True);
                Assert.That(power.RemainingUses, Is.Null);
                player.Stop();
                var restored = player.PrepareSnapshot(new object(), _config, snap, _input);
                player.CommitSnapshot(restored);
                // 快照不保存叶子身份；恢复由当前位置的支撑事实选择合法入口。
                Assert.That(player.IsRunning, Is.True);
                Assert.That(player.Context.Motor.Position, Is.EqualTo(snap.Position));
                bool reentered = player.CurrentStateId == "skill-probe";
                for (int i = 0; i < 30 && !reentered; i++)
                { Simulate(player); reentered = player.CurrentStateId == "skill-probe"; }
                Assert.That(reentered, Is.True, "恢复落地后应重新进入展示，而不是续接快照的旧计时。");
                for (int i = 0; i < 8; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                var bad = UnityEngine.Object.Instantiate(_config.Graph);
                try
                {
                    bad.Configure(bad.EntryId, bad.Nodes.Select(n => n.Id == "skill-probe"
                        ? new EmberFsmNode(n.Id, n.DisplayName, n.Definition, n.ParentId) : n).ToArray());
                    Assert.That(bad.Inspect(typeof(PlayerContext)).IsValid, Is.False);
                }
                finally { UnityEngine.Object.DestroyImmediate(bad); }
                PlayerDashEffectTestAssets.Set(profile, "_slots", profile.Slots.Where(s => s.NodeId != "skill-probe").ToArray());
                Assert.Throws<InvalidOperationException>(() => profile.Validate(_config.Graph));
            }
            finally { CloseSkillAssets(folder); }
        }

        [Test]
        public void Skill_NewLeaf_ParentJumpInterruptsAndStopReleasesVisual()
        {
            string folder = SkillFolder();
            try
            {
                var profile = SkillProbeGraph(folder, true);
                var player = AnimatedCharacter(profile);
                for (int i = 0; i < 30 && player.CurrentStateId != "skill-probe"; i++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("skill-probe"));
                Keys(Key.Space); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
                var owned = AnimationSession(player).Override;
                player.Stop();
                Assert.That(owned == null, Is.True);
            }
            finally { CloseSkillAssets(folder); }
        }

        [Test]
        public void Skill_NewLeaf_TwoHostsKeepTimersAndOverridesIndependent()
        {
            string folder = SkillFolder();
            try
            {
                var profile = SkillProbeGraph(folder, true);
                var first = AnimatedCharacter(profile);
                for (int i = 0; i < 30 && first.CurrentStateId != "skill-probe"; i++) Simulate(first);
                Assert.That(first.CurrentStateId, Is.EqualTo("skill-probe"));
                first.Pause();
                typeof(PlayerConfig).GetProperty("CapturedAnimation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(_config, null);
                var second = AnimatedCharacter(profile);
                Assert.That(AnimationSession(first).Override, Is.Not.SameAs(AnimationSession(second).Override));
                for (int i = 0; i < 30; i++) { Decide(first); Simulate(second); }
                Assert.That(first.CurrentStateId, Is.EqualTo("skill-probe"));
                Assert.That(second.CurrentStateId, Is.EqualTo("idle"));
                first.Resume();
                for (int i = 0; i < 8; i++) Simulate(first);
                Assert.That(first.CurrentStateId, Is.EqualTo("idle"));
            }
            finally { CloseSkillAssets(folder); }
        }
        #endregion
    }
}
#endif
