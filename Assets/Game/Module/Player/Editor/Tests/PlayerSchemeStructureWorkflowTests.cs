using System;
using System.Linq;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeStructureWorkflowTests
    {
        #region 内部参数
        private string _folder;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void SetUp()
        {
            string name = "CombatStructureTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name); _folder = "Assets/" + name;
        }
        [TearDown] public void TearDown()
        {
            if (_folder != null) AssetDatabase.DeleteAsset(_folder);
        }
        [TestCase(0)]
        [TestCase(2)]
        public void IndependentCopyPreservesIdsBindingsAndOwnsStructure(int builtinIndex)
        {
            var source = PlayerSchemeOwnership.Builtins[builtinIndex];
            string sourceGraphJson = EditorJsonUtility.ToJson(source.Graph);
            var copy = PlayerSchemeStructureWorkflow.CopyIndependent(source, _folder + "/Scheme.asset", _folder + "/Graph.asset", _folder + "/Visual.controller");
            Assert.That(copy.Graph, Is.Not.SameAs(source.Graph));
            Assert.That(copy.Animation.Controller, Is.Not.SameAs(source.Animation.Controller));
            Assert.That(copy.Graph.Nodes.Select(n => n.Id), Is.EqualTo(source.Graph.Nodes.Select(n => n.Id)));
            for (int i = 0; i < copy.Graph.Nodes.Count; i++)
            {
                var a = copy.Graph.Nodes[i]; var b = source.Graph.Nodes[i];
                Assert.That(a.Definition, Is.Not.SameAs(b.Definition));
                Assert.That(AssetDatabase.GetAssetPath(a.Definition), Is.EqualTo(_folder + "/Graph.asset"));
                Assert.That(a.Bindings.Select(x => x.Slot + ":" + x.TargetId), Is.EqualTo(b.Bindings.Select(x => x.Slot + ":" + x.TargetId)));
            }
            Assert.That(EditorJsonUtility.ToJson(source.Graph), Is.EqualTo(sourceGraphJson));
            PlayerSchemeWorkflow.Validate(copy);
            AssetDatabase.ImportAsset(_folder + "/Graph.asset", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(_folder + "/Visual.controller", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(_folder + "/Scheme.asset", ImportAssetOptions.ForceUpdate);
            copy = AssetDatabase.LoadAssetAtPath<PlayerScheme>(_folder + "/Scheme.asset");
            PlayerSchemeWorkflow.Validate(copy);
            Assert.That(AssetDatabase.LoadAssetAtPath<PlayerScheme>(_folder + "/Scheme.asset").Graph, Is.SameAs(copy.Graph));
            var controller = (UnityEditor.Animations.AnimatorController)copy.Animation.Controller;
            controller.layers[0].stateMachine.states[0].state.name = "ChangedOnlyInCopy";
            Assert.That(((UnityEditor.Animations.AnimatorController)source.Animation.Controller).layers[0].stateMachine.states.Any(s => s.state.name == "ChangedOnlyInCopy"), Is.False);
        }

        [Test]
        public void CombatWindowDraftSurvivesCloseAndRejectsChangedSource()
        {
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[0],
                _folder + "/Draft.asset", _folder + "/DraftGraph.asset", _folder + "/Draft.controller");
            var idle = target.Animation.Slots.First(s => s.NodeId == "idle").Clip;
            PlayerCombatExampleWorkflow.InstallThreeHit(target, idle, idle, idle);
            string key = "Game.Player.SchemeWindow.Draft." + Application.dataPath;
            string previous = SessionState.GetString(key, "");
            PlayerSchemeWindow window = null;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object Call(string name) => typeof(PlayerSchemeWindow).GetMethod(name, flags).Invoke(window, null);
            object Field(string name) => typeof(PlayerSchemeWindow).GetField(name, flags).GetValue(window);
            try
            {
                SessionState.EraseString(key);
                window = ScriptableObject.CreateInstance<PlayerSchemeWindow>();
                typeof(PlayerSchemeWindow).GetField("_selected", flags).SetValue(window, target);
                Call("ReadCombatDraft");
                var draft = (PlayerFeelProfile)Field("_combatDraft");
                using (var data = new SerializedObject(draft))
                {
                    data.FindProperty("_attack._segments").GetArrayElementAtIndex(0).FindPropertyRelative("_duration").floatValue = .9f;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.That(target.Feel.Attack.Segments[0].Duration, Is.EqualTo(.45f));
                UnityEngine.Object.DestroyImmediate(window); window = null;
                window = ScriptableObject.CreateInstance<PlayerSchemeWindow>();
                Assert.That(Field("_selected"), Is.SameAs(target));
                Assert.That(((PlayerFeelProfile)Field("_combatDraft")).Attack.Segments[0].Duration, Is.EqualTo(.9f));
                Call("ApplyCombatDraft");
                Assert.That(target.Feel.Attack.Segments[0].Duration, Is.EqualTo(.9f));
                Undo.PerformUndo();
                Assert.That(target.Feel.Attack.Segments[0].Duration, Is.EqualTo(.45f));
                var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => Call("ApplyCombatDraft"));
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(((PlayerFeelProfile)Field("_combatDraft")).Attack.Segments[0].Duration, Is.EqualTo(.9f));
                PlayerSchemeWorkflow.Validate(target);
            }
            finally
            {
                if (window != null) UnityEngine.Object.DestroyImmediate(window);
                if (string.IsNullOrEmpty(previous)) SessionState.EraseString(key); else SessionState.SetString(key, previous);
            }
        }

        [Test]
        public void CombatWindowAddsFourthWithClipAndRejectsInvalidDraftWithoutMutation()
        {
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[2],
                _folder + "/Window.asset", _folder + "/WindowGraph.asset", _folder + "/Window.controller");
            string key = "Game.Player.SchemeWindow.Draft." + Application.dataPath;
            string previous = SessionState.GetString(key, "");
            PlayerSchemeWindow window = null;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            object Call(string name) => typeof(PlayerSchemeWindow).GetMethod(name, flags).Invoke(window, null);
            void Set(string name, object value) => typeof(PlayerSchemeWindow).GetField(name, flags).SetValue(window, value);
            try
            {
                SessionState.EraseString(key);
                window = ScriptableObject.CreateInstance<PlayerSchemeWindow>(); Set("_selected", target); Call("ReadCombatDraft");
                var draft = (PlayerFeelProfile)typeof(PlayerSchemeWindow).GetField("_combatDraft", flags).GetValue(window);
                using (var data = new SerializedObject(draft))
                {
                    var segments = data.FindProperty("_attack._segments"); segments.arraySize = 4;
                    segments.GetArrayElementAtIndex(3).FindPropertyRelative("_id").stringValue = "fourth";
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Set("_combatClipIds", new System.Collections.Generic.List<string> { "combat.attack.fourth" });
                Set("_combatClips", new System.Collections.Generic.List<AnimationClip> { null });
                string before = EditorJsonUtility.ToJson(target.Graph);
                Assert.Throws<System.Reflection.TargetInvocationException>(() => Call("ApplyCombatDraft"));
                Assert.That(EditorJsonUtility.ToJson(target.Graph), Is.EqualTo(before));
                Assert.That(target.Feel.Attack.Segments.Count, Is.EqualTo(3));
                Set("_combatClips", new System.Collections.Generic.List<AnimationClip> { target.Animation.Slots.First(s => s.NodeId == "combat.attack.third").Clip });
                Call("ApplyCombatDraft"); PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.Feel.Attack.Segments.Count, Is.EqualTo(4));
                Assert.That(target.Life.Enabled, Is.True); Assert.That(target.CombatGraph.DashRouteNodeId, Is.Not.Empty);
                Assert.That(target.Graph.Nodes.First(n => n.Id == "combat.attack.fourth").Definition, Is.TypeOf<PlayerAttackStateDefinition>());
            }
            finally
            {
                if (window != null) UnityEngine.Object.DestroyImmediate(window);
                if (string.IsNullOrEmpty(previous)) SessionState.EraseString(key); else SessionState.SetString(key, previous);
                foreach (var guid in AssetDatabase.FindAssets("", new[] { _folder }))
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))) Undo.ClearUndo(asset);
            }
        }

        [Test]
        public void ExistingDestinationRejectsWithoutCreatingPartialAssets()
        {
            var existing = ScriptableObject.CreateInstance<EmberFsmGraph>();
            AssetDatabase.CreateAsset(existing, _folder + "/Graph.asset");
            string guid = AssetDatabase.AssetPathToGUID(_folder + "/Graph.asset");
            Assert.Throws<ArgumentException>(() => PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[0],
                _folder + "/Scheme.asset", _folder + "/Graph.asset", _folder + "/Visual.controller"));
            Assert.That(AssetDatabase.AssetPathToGUID(_folder + "/Graph.asset"), Is.EqualTo(guid));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(_folder + "/Scheme.asset"), Is.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(_folder + "/Visual.controller"), Is.Null);
        }

        [Test]
        public void LifeJointCommitRejectsInvalidAnimationAndSupportsUndoRedoReload()
        {
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[0],
                _folder + "/Life.asset", _folder + "/LifeGraph.asset", _folder + "/Life.controller");
            var oldIds = target.Graph.Nodes.Select(n => n.Id).ToArray(); string before = EditorJsonUtility.ToJson(target.Graph);
            var life = JsonUtility.FromJson<PlayerLifeConfiguration>("{\"_enabled\":true,\"_maximum\":10,\"_initial\":10,\"_multiplier\":1,\"_death\":1}");
            var reactions = JsonUtility.FromJson<PlayerReactionConfiguration>("{\"_enabled\":true,\"_invulnerability\":false}");
            var bad = new AnimationClip { legacy = true };
            try
            {
                Assert.Throws<InvalidOperationException>(() => PlayerCombatExampleWorkflow.InstallLife(target, life, reactions, bad));
                Assert.That(EditorJsonUtility.ToJson(target.Graph), Is.EqualTo(before)); Assert.That(target.Life.Enabled, Is.False);
                PlayerCombatExampleWorkflow.InstallLife(target, life, reactions);
                Assert.That(target.Life.Enabled, Is.True); Assert.That(target.Graph.Nodes.Count, Is.EqualTo(oldIds.Length + 2));
                Undo.PerformUndo(); Assert.That(target.Life.Enabled, Is.False);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.EqualTo(oldIds));
                Undo.PerformRedo(); PlayerSchemeWorkflow.Validate(target); Assert.That(target.Life.Enabled, Is.True);
                PlayerSchemeWorkflow.Save(target); AssetDatabase.SaveAssetIfDirty(target.Graph); AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                foreach (string suffix in new[] { "/Life.asset", "/LifeGraph.asset", "/Life.controller" }) AssetDatabase.ImportAsset(_folder + suffix, ImportAssetOptions.ForceUpdate);
                target = AssetDatabase.LoadAssetAtPath<PlayerScheme>(_folder + "/Life.asset"); PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.SupersetOf(oldIds)); Assert.That(target.Life.Maximum, Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bad);
                foreach (string suffix in new[] { "/Life.asset", "/LifeGraph.asset", "/Life.controller" })
                    foreach (var item in AssetDatabase.LoadAllAssetsAtPath(_folder + suffix)) if (item != null) Undo.ClearUndo(item);
            }
        }

        [Test]
        public void FourthAttackPreservesLifeDashAndDelayedToggleAcrossUndoRedoReload()
        {
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[0],
                _folder + "/Fourth.asset", _folder + "/FourthGraph.asset", _folder + "/Fourth.controller");
            var clip = target.Animation.Slots.First(s => s.NodeId == "idle").Clip;
            try
            {
                PlayerCombatExampleWorkflow.InstallThreeHit(target, clip, clip, clip);
                AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                PlayerCombatExampleWorkflow.EnableDashFollowup(target);
                AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                PlayerCombatExampleWorkflow.InstallLife(target,
                    JsonUtility.FromJson<PlayerLifeConfiguration>("{\"_enabled\":true,\"_maximum\":10,\"_initial\":10,\"_multiplier\":1,\"_death\":1}"),
                    JsonUtility.FromJson<PlayerReactionConfiguration>("{\"_enabled\":true,\"_invulnerability\":false}"));
                AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                var originalIds = target.Graph.Nodes.Select(n => n.Id).ToArray();
                var attack = PlayerCombatData.Copy(target.Feel.Attack);
                var fourth = PlayerCombatData.Copy(attack.Segments[0]);
                typeof(PlayerAttackSegment).GetField("_id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(fourth, "fourth");
                typeof(PlayerAttackProfile).GetField("_segments", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(attack, attack.Segments.Concat(new[] { fourth }).ToArray());
                var combo = JsonUtility.FromJson<PlayerCombo>("{\"_groundEntry\":\"first\",\"_connections\":[{\"_source\":\"first\",\"_normal\":\"second\",\"_delayedEnabled\":true,\"_delayed\":\"fourth\"},{\"_source\":\"second\",\"_normal\":\"third\"},{\"_source\":\"third\",\"_normal\":\"fourth\"}]}");
                PlayerCombatExampleWorkflow.ApplyConfiguration(target, attack, combo,
                    new System.Collections.Generic.Dictionary<string, AnimationClip> { ["combat.attack.fourth"] = clip });
                PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.SupersetOf(originalIds));
                Assert.That(target.Graph.Nodes.First(n => n.Id == target.Graph.EntryId).Definition, Is.TypeOf<PlayerLifeDashLocomotionStateDefinition>());
                Assert.That(target.Life.Enabled, Is.True); Assert.That(target.CombatGraph.DashRouteNodeId, Is.Not.Empty);
                Assert.That(target.Graph.Nodes.First(n => n.Id == "combat.attack.fourth").Definition, Is.TypeOf<PlayerAttackStateDefinition>());
                Undo.PerformUndo(); PlayerSchemeWorkflow.Validate(target); Assert.That(target.Feel.Attack.Segments.Count, Is.EqualTo(3));
                Undo.PerformRedo(); PlayerSchemeWorkflow.Validate(target); Assert.That(target.Feel.Attack.Segments.Count, Is.EqualTo(4));
                var allIds = target.Graph.Nodes.Select(n => n.Id).ToArray();
                var connection = target.Combo.Connections.First(c => c.Source == "first");
                var flag = typeof(PlayerComboConnection).GetField("_delayedEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                flag.SetValue(connection, false); PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.EqualTo(allIds));
                AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                PlayerCombatExampleWorkflow.ApplyConfiguration(target, PlayerCombatData.Copy(target.Feel.Attack), PlayerCombatData.Copy(target.Combo), null);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.EquivalentTo(allIds));
                flag.SetValue(target.Combo.Connections.First(c => c.Source == "first"), true); PlayerSchemeWorkflow.Validate(target);
                PlayerSchemeWorkflow.Save(target); AssetDatabase.SaveAssetIfDirty(target.Graph); AssetDatabase.SaveAssetIfDirty(target.Animation.Controller);
                foreach (string suffix in new[] { "/Fourth.asset", "/FourthGraph.asset", "/Fourth.controller" }) AssetDatabase.ImportAsset(_folder + suffix, ImportAssetOptions.ForceUpdate);
                target = AssetDatabase.LoadAssetAtPath<PlayerScheme>(_folder + "/Fourth.asset"); PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.Graph.Nodes.Select(n => n.Id), Is.EquivalentTo(allIds));
                Assert.That(target.Combo.Connections.First(c => c.Source == "first").DelayedEnabled, Is.True);
                string graphBefore = EditorJsonUtility.ToJson(target.Graph), controllerBefore = EditorJsonUtility.ToJson(target.Animation.Controller);
                var parameters = PlayerCombatData.Copy(target.Feel.Attack);
                typeof(PlayerAttackSegment).GetField("_duration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(parameters.Segments[0], .8f);
                PlayerCombatExampleWorkflow.ApplyConfiguration(target, parameters, PlayerCombatData.Copy(target.Combo), null);
                Assert.That(target.Feel.Attack.Segments[0].Duration, Is.EqualTo(.8f));
                Assert.That(EditorJsonUtility.ToJson(target.Graph), Is.EqualTo(graphBefore));
                Assert.That(EditorJsonUtility.ToJson(target.Animation.Controller), Is.EqualTo(controllerBefore));
                Undo.PerformUndo(); Assert.That(target.Feel.Attack.Segments[0].Duration, Is.Not.EqualTo(.8f));
                Undo.PerformRedo(); Assert.That(target.Feel.Attack.Segments[0].Duration, Is.EqualTo(.8f));
                PlayerSchemeWorkflow.Validate(target);
            }
            finally
            {
                foreach (string suffix in new[] { "/Fourth.asset", "/FourthGraph.asset", "/Fourth.controller" })
                    foreach (var item in AssetDatabase.LoadAllAssetsAtPath(_folder + suffix)) if (item != null) Undo.ClearUndo(item);
            }
        }

        [Test]
        public void JointCombatCommitRejectsMissingClipThenSupportsUndoRedoAndReload()
        {
            var original = PlayerSchemeOwnership.Builtins[0];
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(original,
                _folder + "/Target.asset", _folder + "/Graph.asset", _folder + "/Visual.controller");
            var graph = target.Graph; var controller = target.Animation.Controller;
            var body = ScriptableObject.CreateInstance<CombatGraphFixtureDefinition>();
            var recovery = ScriptableObject.CreateInstance<CombatGraphFixtureDefinition>(); recovery.Role = PlayerCombatNodeRole.Recovery;
            var effect = ScriptableObject.CreateInstance<Game.Entity.EntityEffectDefinition>();
            body.Effect = effect; recovery.Effect = effect;
            var attack = PlayerCombatConfigurationTests.Attack();
            var combo = JsonUtility.FromJson<PlayerCombo>("{\"_groundEntry\":\"first\",\"_connections\":[]}");
            var clips = new System.Collections.Generic.Dictionary<string, AnimationClip>();
            string before = EditorJsonUtility.ToJson(graph);
            try
            {
                Assert.Throws<InvalidOperationException>(() => PlayerCombatStructureTransaction.Apply(target, attack, combo, body, recovery, clips));
                Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(before));
                Assert.That(target.Feel.Attack.Enabled, Is.False);
                clips.Add("combat.attack.first", original.Animation.Slots.First(s => s.NodeId == "idle").Clip);
                body.RejectClone = true;
                StringAssert.Contains("目标提交", Assert.Throws<InvalidOperationException>(() =>
                    PlayerCombatStructureTransaction.Apply(target, attack, combo, body, recovery, clips)).Message);
                Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(before), "提交失败恢复原图");
                Assert.That(target.Feel.Attack.Enabled, Is.False, "提交失败恢复原配置");
                Assert.That(((UnityEditor.Animations.AnimatorController)controller).layers[0].stateMachine.states.Length,
                    Is.EqualTo(original.Animation.Slots.Count), "提交失败恢复 Controller");
                PlayerSchemeWorkflow.Validate(target);
                body.RejectClone = false;
                PlayerCombatStructureTransaction.Apply(target, attack, combo, body, recovery, clips);
                Assert.That(target.Feel.Attack.Enabled, Is.True, "提交后立即启用攻击");
                Assert.That(target.CombatGraph.Nodes.Count, Is.EqualTo(1), "提交后立即登记节点归属");
                Assert.That(target.Graph, Is.SameAs(graph)); Assert.That(target.Animation.Controller, Is.SameAs(controller));
                Assert.That(graph.Nodes.Count, Is.EqualTo(original.Graph.Nodes.Count + 2));
                var copiedBody = (CombatGraphFixtureDefinition)graph.Nodes.First(n => n.Id == "combat.attack.first").Definition;
                var copiedRecovery = (CombatGraphFixtureDefinition)graph.Nodes.First(n => n.Id == "combat.recovery.first").Definition;
                Assert.That(copiedBody.Effect, Is.Not.SameAs(effect));
                Assert.That(copiedBody.Effect, Is.SameAs(copiedRecovery.Effect));
                Assert.That(AssetDatabase.GetAssetPath(copiedBody.Effect), Is.EqualTo(AssetDatabase.GetAssetPath(graph)));
                PlayerSchemeWorkflow.Validate(target);
                Undo.PerformUndo();
                Assert.That(target.Feel.Attack.Enabled, Is.False); Assert.That(graph.Nodes.Count, Is.EqualTo(original.Graph.Nodes.Count));
                PlayerSchemeWorkflow.Validate(target);
                Undo.PerformRedo();
                Assert.That(target.Feel.Attack.Enabled, Is.True, "Redo 应恢复攻击开关"); PlayerSchemeWorkflow.Validate(target);
                PlayerSchemeWorkflow.Save(target); AssetDatabase.SaveAssetIfDirty(graph); AssetDatabase.SaveAssetIfDirty(controller);
                foreach (string path in new[] { "/Target.asset", "/Graph.asset", "/Visual.controller" })
                    AssetDatabase.ImportAsset(_folder + path, ImportAssetOptions.ForceUpdate);
                target = AssetDatabase.LoadAssetAtPath<PlayerScheme>(_folder + "/Target.asset");
                PlayerSchemeWorkflow.Validate(target);
                Assert.That(target.CombatGraph.Nodes[0].AttackNodeId, Is.EqualTo("combat.attack.first"));
                var config = ScriptableObject.CreateInstance<PlayerConfig>();
                try
                {
                    PlayerSchemeConversion.SelectScheme(config, target, false);
                    using var captured = new PlayerSchemeCapture(config);
                    target.Feel.SetAttackConfiguration(new PlayerAttackProfile());
                    target.SetCombatConfiguration(new PlayerCombo(), new PlayerCombatGraphMap());
                    Assert.That(captured.Config.CapturedFeel.Attack.Enabled, Is.True);
                    Assert.That(captured.Config.CapturedFeel.Attack.Segments[0].Id, Is.EqualTo("first"));
                    Assert.That(captured.Config.CapturedCombat.Combo.GroundEntry, Is.EqualTo("first"));
                    Assert.That(captured.Config.CapturedCombat.Graph.Nodes[0].AttackNodeId, Is.EqualTo("combat.attack.first"));
                    using var recaptured = new PlayerSchemeCapture(captured.Config);
                    Assert.That(recaptured.Config.CapturedCombat.Combo.GroundEntry, Is.EqualTo("first"));
                    Assert.That(recaptured.Config.CapturedFeel.Attack.Enabled, Is.True);
                }
                finally { UnityEngine.Object.DestroyImmediate(config); }
            }
            finally
            {
                foreach (var item in PlayerSchemeWorkflow.Configuration(target).Values) Undo.ClearUndo(item);
                foreach (string path in new[] { "/Graph.asset", "/Visual.controller" })
                    foreach (var item in AssetDatabase.LoadAllAssetsAtPath(_folder + path)) if (item != null) Undo.ClearUndo(item);
                UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(recovery);
                UnityEngine.Object.DestroyImmediate(effect);
            }
        }

        [Test]
        public void DefinitionDependenciesAreCopiedOnceAndRemainIndependent()
        {
            var source = PlayerSchemeStructureWorkflow.CopyIndependent(PlayerSchemeOwnership.Builtins[0],
                _folder + "/Source.asset", _folder + "/SourceGraph.asset", _folder + "/Source.controller");
            var definition = (PlayerGroundDashStateDefinition)source.Graph.Nodes.First(n => n.Definition is PlayerGroundDashStateDefinition).Definition;
            var effect = ScriptableObject.CreateInstance<Game.Entity.EntityEffectDefinition>();
            AssetDatabase.CreateAsset(effect, _folder + "/ExternalEffect.asset");
            using (var data = new SerializedObject(definition))
            {
                data.FindProperty("_actionEffect").objectReferenceValue = effect;
                data.FindProperty("_tailEffect").objectReferenceValue = effect;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssetIfDirty(definition);
            var target = PlayerSchemeStructureWorkflow.CopyIndependent(source,
                _folder + "/Target.asset", _folder + "/TargetGraph.asset", _folder + "/Target.controller");
            var copied = (PlayerGroundDashStateDefinition)target.Graph.Nodes.First(n => n.Definition is PlayerGroundDashStateDefinition).Definition;
            Assert.That(copied.LegacyActionEffect, Is.Not.SameAs(effect));
            Assert.That(copied.LegacyActionEffect, Is.SameAs(copied.LegacyTailEffect));
            Assert.That(AssetDatabase.GetAssetPath(copied.LegacyActionEffect), Is.EqualTo(_folder + "/TargetGraph.asset"));
            using (var data = new SerializedObject(copied.LegacyActionEffect))
            {
                data.FindProperty("_duration").floatValue = 9;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.That(effect.Duration, Is.EqualTo(1));
            Assert.That(definition.LegacyActionEffect, Is.SameAs(effect));
        }

        [Test]
        public void ParameterRestoreKeepsIndependentStructureAndSupportsUndoRedo()
        {
            var source = PlayerSchemeOwnership.Builtins[0];
            var copy = PlayerSchemeStructureWorkflow.CopyIndependent(source, _folder + "/Scheme.asset", _folder + "/Graph.asset", _folder + "/Visual.controller");
            var graph = copy.Graph; var controller = copy.Animation.Controller;
            using (var data = new SerializedObject(copy.Feel.Movement))
            {
                data.FindProperty("_speed").floatValue = 37;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            try
            {
                PlayerSchemeWorkflow.Restore(copy, source);
                Assert.That(copy.Feel.Movement.Speed, Is.EqualTo(source.Feel.Movement.Speed));
                Assert.That(copy.Graph, Is.SameAs(graph)); Assert.That(copy.Animation.Controller, Is.SameAs(controller));
                Undo.PerformUndo();
                Assert.That(copy.Feel.Movement.Speed, Is.EqualTo(37));
                Assert.That(copy.Graph, Is.SameAs(graph));
                Undo.PerformRedo();
                Assert.That(copy.Feel.Movement.Speed, Is.EqualTo(source.Feel.Movement.Speed));
                Assert.That(copy.Animation.Controller, Is.SameAs(controller));
            }
            finally
            {
                foreach (var owned in PlayerSchemeWorkflow.Configuration(copy).Values) Undo.ClearUndo(owned);
            }
        }

        [Test]
        public void SavedLayoutCopiesWithoutOverwritingOrPlacingNewNodes()
        {
            var source = ScriptableObject.CreateInstance<EmberFsmGraph>();
            var target = ScriptableObject.CreateInstance<EmberFsmGraph>();
            source.Configure("stable", new EmberFsmNode("stable", "Stable", null));
            target.Configure("stable", new EmberFsmNode("stable", "Stable", null), new EmberFsmNode("added", "Added", null));
            AssetDatabase.CreateAsset(source, _folder + "/Source.asset");
            AssetDatabase.CreateAsset(target, _folder + "/Target.asset");
            string root = "Ember.Fsm.Layout." + Application.dataPath + ".";
            string a = root + AssetDatabase.AssetPathToGUID(_folder + "/Source.asset") + ".v4.exterior.";
            string b = root + AssetDatabase.AssetPathToGUID(_folder + "/Target.asset") + ".v4.exterior.";
            try
            {
                EditorPrefs.SetBool(a + "saved", true);
                EditorPrefs.SetFloat(a + "stable.x", 137); EditorPrefs.SetFloat(a + "stable.y", -42);
                EditorPrefs.SetBool(a + "stable.pinned", true); EditorPrefs.SetBool(a + "stable.fold", true);
                EditorPrefs.SetFloat(a + "zoom", .75f);
                Ember.Core.Editor.EmberFsmBlueprintWindow.CopySavedLayout(source, target);
                Assert.That(EditorPrefs.GetFloat(b + "stable.x"), Is.EqualTo(137));
                Assert.That(EditorPrefs.GetFloat(b + "stable.y"), Is.EqualTo(-42));
                Assert.That(EditorPrefs.GetBool(b + "stable.pinned"), Is.True);
                Assert.That(EditorPrefs.GetBool(b + "stable.fold"), Is.True);
                Assert.That(EditorPrefs.GetFloat(b + "zoom"), Is.EqualTo(.75f));
                Assert.That(EditorPrefs.HasKey(b + "added.x"), Is.False);
                Assert.Throws<InvalidOperationException>(() => Ember.Core.Editor.EmberFsmBlueprintWindow.CopySavedLayout(source, target));
                Assert.That(EditorPrefs.GetFloat(a + "stable.x"), Is.EqualTo(137));
            }
            finally
            {
                foreach (string prefix in new[] { a, b })
                    foreach (string suffix in new[] { "saved", "stable.x", "stable.y", "stable.pinned", "stable.fold", "zoom" })
                        EditorPrefs.DeleteKey(prefix + suffix);
            }
        }
        #endregion
    }
}
