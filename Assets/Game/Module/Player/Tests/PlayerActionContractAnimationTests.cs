#if UNITY_EDITOR
using System;
using System.Linq;
using ActionContractConsumer;
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
        [TestCase(false)] [TestCase(true)]
        public void ActionContract_IndependentSchemeCopyReloadAndAnimationObserveActualFsm(bool combat)
        {
            string folder = SkillFolder();
            try
            {
                var originalScheme = combat ? FeelFixScheme() : Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme;
                var workflow = Type.GetType("Game.Player.Editor.PlayerSchemeStructureWorkflow, Game.Player.Editor", true);
                var baseScheme = (PlayerScheme)workflow.GetMethod("CopyIndependent").Invoke(null,
                    new object[] { originalScheme, folder + "/Base.asset", folder + "/BaseGraph.asset", folder + "/Base.controller" });
                _config.Configure(baseScheme.Graph, _config.Spawn);
                var settings = InstallActionGraph();
                var graph = _config.Graph; AssetDatabase.CreateAsset(graph, folder + "/SourceGraph.asset");
                var action = (ActionProbeDefinition)graph.Nodes.First(n => n.Id == "project-probe").Definition;
                AssetDatabase.AddObjectToAsset(action, graph); AssetDatabase.AddObjectToAsset(settings, graph);
                AssetDatabase.AddObjectToAsset(graph.Nodes.First(n => n.Id == graph.EntryId).Definition, graph);
                var sourceProfile = Keep(UnityEngine.Object.Instantiate(baseScheme.Animation));
                var controller = (AnimatorController)sourceProfile.Controller;
                var key = UnityEngine.Object.Instantiate(sourceProfile.Slots[0].Clip); key.name = "project-action-key";
                AssetDatabase.CreateAsset(key, folder + "/Key.anim");
                controller.layers[0].stateMachine.AddState("project-probe").motion = key;
                using (var serialized = new SerializedObject(sourceProfile))
                {
                    serialized.FindProperty("_controller").objectReferenceValue = controller;
                    var slots = serialized.FindProperty("_slots"); slots.arraySize++;
                    var slot = slots.GetArrayElementAtIndex(slots.arraySize - 1);
                    slot.FindPropertyRelative("_nodeId").stringValue = "project-probe";
                    slot.FindPropertyRelative("_state").stringValue = "Base Layer.project-probe";
                    slot.FindPropertyRelative("_key").objectReferenceValue = key;
                    slot.FindPropertyRelative("_clip").objectReferenceValue = key;
                    slot.FindPropertyRelative("_speed").floatValue = 1;
                    slot.FindPropertyRelative("_blendSeconds").floatValue = 0;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(sourceProfile, folder + "/Animation.asset");
                AssetDatabase.SaveAssetIfDirty(graph); AssetDatabase.SaveAssetIfDirty(controller); AssetDatabase.SaveAssetIfDirty(sourceProfile);
                sourceProfile.Validate(graph);
                var scheme = Keep(UnityEngine.Object.Instantiate(baseScheme));
                PlayerDashEffectTestAssets.Set(scheme, "_graph", graph);
                PlayerDashEffectTestAssets.Set(scheme, "_animation", sourceProfile);
                AssetDatabase.CreateAsset(scheme, folder + "/SourceScheme.asset"); AssetDatabase.SaveAssetIfDirty(scheme);
                var independent = (PlayerScheme)workflow.GetMethod("CopyIndependent").Invoke(null,
                    new object[] { scheme, folder + "/Copy.asset", folder + "/CopyGraph.asset", folder + "/Copy.controller" });
                var copied = (ActionProbeDefinition)independent.Graph.Nodes.First(n => n.Id == "project-probe").Definition;
                Assert.That(copied.Settings, Is.Not.SameAs(settings));
                settings.Duration = 99;
                Assert.That(copied.Settings.Duration, Is.EqualTo(1));
                AssetDatabase.ImportAsset(folder + "/Copy.asset", ImportAssetOptions.ForceUpdate);
                independent = AssetDatabase.LoadAssetAtPath<PlayerScheme>(folder + "/Copy.asset");
                independent.Animation.Validate(independent.Graph);
                var sourceConfig = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
                PlayerDashEffectTestAssets.Set(sourceConfig, "_scheme", independent);
                using var captured = new PlayerSchemeCapture(sourceConfig);
                var original = _config;
                try
                {
                    _config = captured.Config; var animation = _config.CapturedAnimation;
                    typeof(PlayerConfig).GetProperty("CapturedAnimation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_config, null);
                    var player = AnimatedCharacter(animation); Land(player);
                    var probe = StartAction(player);
                    Assert.That(AnimationSession(player).NodeId, Is.EqualTo("project-probe"));
                    var animator = player.GetComponentInChildren<Animator>(); animator.Update(0);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.project-probe"), Is.True);
                    var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>();
                    Assert.That(target.TryLaunch(ExternalOwner(), new LaunchRequest2D("animation", Vector2.up * 8)), Is.EqualTo(LaunchResult2D.Accepted));
                    Decide(player); Assert.That(AnimationSession(player).NodeId, Is.EqualTo(player.CurrentStateId));
                    Assert.That(AnimationSession(player).NodeId, Is.Not.EqualTo("project-probe"));
                    if (combat)
                    {
                        // 弹射写入后的首步仍可能沿用地面探测事实；等待真实飞行和落地后再测地面攻击。
                        for (int i = 0; i < 100; i++) Simulate(player);
                        Land(player); Keys(Key.J); Decide(player);
                        Assert.That(player.Context.Combat.IsActive, Is.True,
                            "项目动作退出后保留正常战斗入口：" + player.CurrentStateId + "; grounded=" + player.Context.JumpWindow.HasGroundSupport);
                    }
                    player.Stop();
                }
                finally { _config = original; }
            }
            finally { CloseSkillAssets(folder); }
        }
    }
}
#endif
