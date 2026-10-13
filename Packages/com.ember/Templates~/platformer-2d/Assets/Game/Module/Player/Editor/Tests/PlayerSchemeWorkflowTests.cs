using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeWorkflowTests
    {
        private string _folder;
        private PlayerScheme First => PlayerSchemeOwnership.Builtins[0];
        private PlayerScheme Second => PlayerSchemeOwnership.Builtins[1];
        private static void Edit(Object target, string name, Action<SerializedProperty> change)
        { using var data = new SerializedObject(target); change(data.FindProperty(name)); data.ApplyModifiedPropertiesWithoutUndo(); }
        private PlayerScheme Copy(string name, PlayerScheme source = null, bool blank = false) =>
            PlayerSchemeWorkflow.Copy(source ?? First, _folder + "/" + name + ".asset", blank);
        [SetUp] public void Setup()
        {
            Assert.That(PlayerSchemeOwnership.Builtins.Count, Is.EqualTo(2));
            _folder = "Assets/SchemeWorkflowTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
        }
        [TearDown] public void Cleanup() { Undo.ClearAll(); if (_folder != null) AssetDatabase.DeleteAsset(_folder); }

        [Test] public void BuiltinsKeepTraceableStyleValuesCurrentAssemblyAndSharedGraph()
        {
            var original = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme;
            Assert.That(First.name, Is.EqualTo("空洞骑士风格")); Assert.That(Second.name, Is.EqualTo("蔚蓝风格"));
            int i = 0;
            foreach (var scheme in PlayerSchemeOwnership.Builtins)
            {
                PlayerSchemeWorkflow.Validate(scheme);
                var style = Resources.Load<PlayerFeelProfile>("Config/Player/Feel/" + (i++ == 0 ? "HollowKnightStyle" : "CelesteStyle") + "Profile");
                Assert.That(PlayerFeelPresetWindow.Compare(scheme.Feel, style), Is.Empty);
                Assert.That(scheme.Graph, Is.SameAs(original.Graph)); Assert.That(scheme.Graph.Nodes.Count, Is.EqualTo(21));
                Assert.That(scheme.Powers.AbilitySlots.Select(s => s.Enabled), Is.EqualTo(original.Powers.AbilitySlots.Select(s => s.Enabled)));
                Assert.That(scheme.Powers.SkillSlots.Select(s => s.Definition.Id), Is.EqualTo(original.Powers.SkillSlots.Select(s => s.Definition.Id)));
                Assert.That(scheme.Animation.Slots.Count, Is.EqualTo(21));
                Assert.That(PlayerSchemeWorkflow.Configuration(scheme).Values.All(PlayerSchemeOwnership.IsReadOnly), Is.True);
                Assert.That(PlayerSchemeOwnership.IsReadOnly(scheme.Animation.Controller), Is.True);
                Assert.That(scheme.Animation.Slots.All(s => PlayerSchemeOwnership.IsReadOnly(s.Key) && PlayerSchemeOwnership.IsReadOnly(s.Clip)), Is.True);
            }
        }
        [Test] public void CopiesOwnAllMutableConfigurationAndRetainStableIdsAndArt()
        {
            var a = Copy("A"); var b = Copy("B");
            var originals = PlayerSchemeWorkflow.Configuration(First);
            var copies = PlayerSchemeWorkflow.Configuration(a); var others = PlayerSchemeWorkflow.Configuration(b);
            foreach (var pair in originals)
            {
                Assert.That(copies[pair.Key], Is.Not.SameAs(pair.Value)); Assert.That(copies[pair.Key], Is.Not.SameAs(others[pair.Key]));
                Assert.That(AssetDatabase.GetAssetPath(copies[pair.Key]), Is.EqualTo(AssetDatabase.GetAssetPath(a)));
                Assert.That(PlayerSchemeOwnership.IsReadOnly(copies[pair.Key]), Is.False);
                Assert.That(copies[pair.Key].hideFlags & HideFlags.NotEditable, Is.EqualTo(HideFlags.None));
            }
            Assert.That(PlayerSchemeWorkflow.Compare(a, First), Is.Empty);
            Assert.That(a.Graph, Is.SameAs(First.Graph)); Assert.That(a.Animation.Controller, Is.SameAs(First.Animation.Controller));
            Edit(a.Feel.Movement, "_speed", p => p.floatValue = 12);
            Edit(a.Powers.SkillSlots[0].Definition, "_cooldownSeconds", p => p.floatValue = 3);
            Edit(a.Animation, "_slots.Array.data[0]._speed", p => p.floatValue = 2);
            Assert.That(PlayerSchemeWorkflow.Compare(b, First), Is.Empty);
            Assert.That(PlayerSchemeWorkflow.Compare(a, First).Count, Is.GreaterThanOrEqualTo(3));
        }
        private void AddSharedEffect(PlayerScheme scheme)
        {
            var effect = ScriptableObject.CreateInstance<EntityEffectDefinition>(); effect.name = "SharedProtection";
            Edit(effect, "_id", p => p.stringValue = "workflow.shared");
            Edit(effect, "_lifetime", p => p.enumValueIndex = (int)EntityEffectLifetime.Owned);
            AssetDatabase.AddObjectToAsset(effect, scheme);
            scheme.ActionEffects.Configure(scheme.ActionEffects.Slots.Select(s => new PlayerActionEffectSlot(s.NodeId, effect, null)).ToArray());
            EditorUtility.SetDirty(scheme.ActionEffects); Edit(scheme, "_enableEffects", p => p.boolValue = true);
            PlayerSchemeWorkflow.Save(scheme);
        }
        [Test] public void SharedEffectsCopyOnceWithinEachSchemeWithoutCrossSchemeWrites()
        {
            var source = Copy("Source"); AddSharedEffect(source);
            var a = Copy("A", source); var b = Copy("B", source);
            var effect = a.ActionEffects.Slots[0].ActionEffect;
            Assert.That(effect, Is.SameAs(a.ActionEffects.Slots[1].ActionEffect));
            Assert.That(effect, Is.Not.SameAs(source.ActionEffects.Slots[0].ActionEffect));
            Assert.That(effect, Is.Not.SameAs(b.ActionEffects.Slots[0].ActionEffect));
            Assert.That(effect.Id, Is.EqualTo("workflow.shared"));
            Edit(effect, "_alpha", p => p.floatValue = .25f);
            Assert.That(PlayerSchemeWorkflow.Compare(b, source), Is.Empty);
            Assert.That(PlayerSchemeWorkflow.Compare(a, source), Is.Not.Empty);
            Assert.That(First.ActionEffects.Slots.All(s => s.ActionEffect == null && s.TailEffect == null), Is.True);
        }
        [Test] public void BlankDisablesExtrasAndKeepsCompleteValidBasicAndAnimationAssembly()
        {
            var blank = Copy("Blank", Second, true); PlayerSchemeWorkflow.Validate(blank);
            Assert.That(blank.Powers.AbilitySlots.All(s => !s.Enabled), Is.True);
            Assert.That(blank.Powers.SkillSlots.All(s => !s.Enabled), Is.True);
            Assert.That(blank.EnableEffects, Is.False);
            Assert.That(blank.ActionEffects.Version, Is.EqualTo(1));
            Assert.That(blank.ActionEffects.Slots.All(s => s.ActionEffect == null && s.TailEffect == null), Is.True);
            foreach (var asset in new Object[] { blank.Feel.Movement, blank.Feel.Jump, blank.Feel.Input })
            {
                using var data = new SerializedObject(asset); var p = data.GetIterator();
                while (p.Next(true)) if (p.propertyType == SerializedPropertyType.Boolean && p.propertyPath.StartsWith("_", StringComparison.Ordinal))
                    Assert.That(p.boolValue, Is.False, p.propertyPath);
            }
            Assert.That(blank.Feel.Capture(Physics2D.gravity).MoveSpeed, Is.GreaterThan(0));
            Assert.That(blank.Feel.Capture(Physics2D.gravity).LaunchSpeed, Is.GreaterThan(0));
            Assert.That(blank.Animation.Slots.Count, Is.EqualTo(21)); Assert.That(blank.Graph, Is.SameAs(First.Graph));
        }
        [Test] public void BlankRejectsImmobileReferenceWithoutCreatingPartialAsset()
        {
            var source = Copy("Stopped"); Edit(source.Feel.Movement, "_speed", p => p.floatValue = 0);
            Assert.Throws<InvalidOperationException>(() => Copy("InvalidBlank", source, true));
            Assert.That(File.Exists(_folder + "/InvalidBlank.asset"), Is.False);
            Assert.That(source.Feel.Movement.Speed, Is.Zero);
        }
        [Test] public void FullRestorePreservesAssetIdentityDoesNotSaveAndSupportsUndoRedo()
        {
            var current = Copy("Current"); string path = AssetDatabase.GetAssetPath(current);
            string bytes = File.ReadAllText(path); var identities = PlayerSchemeWorkflow.Configuration(current).ToDictionary(p => p.Key, p => GlobalObjectId.GetGlobalObjectIdSlow(p.Value));
            PlayerSchemeWorkflow.Restore(current, Second);
            Assert.That(PlayerSchemeWorkflow.Compare(current, Second), Is.Empty); Assert.That(File.ReadAllText(path), Is.EqualTo(bytes));
            foreach (var pair in PlayerSchemeWorkflow.Configuration(current)) Assert.That(GlobalObjectId.GetGlobalObjectIdSlow(pair.Value), Is.EqualTo(identities[pair.Key]));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(PlayerSchemeWorkflow.Compare(current, First), Is.Empty);
            Undo.PerformRedo(); Assert.That(PlayerSchemeWorkflow.Compare(current, Second), Is.Empty);
            PlayerSchemeWorkflow.Save(current); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Assert.That(PlayerSchemeWorkflow.Compare(AssetDatabase.LoadAssetAtPath<PlayerScheme>(path), Second), Is.Empty);
        }
        [Test] public void RestoreCanAddSharedEffectsAndUndoRedoRetainsNewConfiguration()
        {
            var reference = Copy("Reference"); AddSharedEffect(reference); var current = Copy("Current");
            string disk = File.ReadAllText(AssetDatabase.GetAssetPath(current));
            PlayerSchemeWorkflow.Restore(current, reference);
            Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(current)), Is.EqualTo(disk), "添加配置依赖也不能自动保存");
            var effect = current.ActionEffects.Slots[0].ActionEffect;
            Assert.That(effect, Is.Not.SameAs(reference.ActionEffects.Slots[0].ActionEffect));
            Assert.That(effect, Is.SameAs(current.ActionEffects.Slots[1].ActionEffect));
            Assert.That(PlayerSchemeWorkflow.Compare(current, reference), Is.Empty);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(current.ActionEffects.Slots[0].ActionEffect, Is.Null);
            Assert.That(effect != null, Is.True); Undo.PerformRedo();
            Assert.That(current.ActionEffects.Slots[0].ActionEffect, Is.SameAs(effect));
            Assert.That(PlayerSchemeWorkflow.Compare(current, reference), Is.Empty);
        }
        [Test] public void ReadOnlyPolicyCoversCompleteLegacyMigrationAndSavePipelineEntries()
        {
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Save(First));
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Restore(First, Second));
            Assert.Throws<InvalidOperationException>(() => PlayerFsmParameterEditor.Save(First.Feel));
            Assert.Throws<InvalidOperationException>(() => PlayerFeelPresetWindow.Restore(First.Feel, Second.Feel));
            Assert.Throws<InvalidOperationException>(() => PlayerActionEffectMigration.Save(First));
            Assert.Throws<InvalidOperationException>(() => PlayerActionEffectMigration.Migrate(First, _folder + "/Forbidden.asset"));
            var type = typeof(PlayerSchemeWorkflow).Assembly.GetType("Game.Player.Editor.PlayerSchemeSaveProtection");
            var filter = type.GetMethod("OnWillSaveAssets", BindingFlags.NonPublic | BindingFlags.Static);
            string writable = AssetDatabase.GetAssetPath(Copy("Writable"));
            var result = (string[])filter.Invoke(null, new object[] { new[] { AssetDatabase.GetAssetPath(First), AssetDatabase.GetAssetPath(First.Animation.Controller), writable } });
            Assert.That(result, Is.EqualTo(new[] { writable }));
            Assert.That(File.Exists(_folder + "/Forbidden.asset"), Is.False);
        }
        [Test] public void LegacyCopiesAndReferenceRestoreFromBuiltinRemainEditable()
        {
            var copy = PlayerFeelPresetWindow.SaveCopy(First.Feel, _folder + "/Legacy.asset");
            PlayerFeelPresetWindow.Restore(copy, Second.Feel);
            foreach (var asset in new Object[] { copy, copy.Movement, copy.Jump, copy.Input })
            { Assert.That(asset.hideFlags & HideFlags.NotEditable, Is.EqualTo(HideFlags.None)); Assert.That(PlayerSchemeOwnership.IsReadOnly(asset), Is.False); }
            Assert.That(PlayerFeelPresetWindow.Compare(copy, Second.Feel), Is.Empty);
        }
        [Test] public void InvalidDependenciesAndOccupiedDestinationsFailWithoutOverwrite()
        {
            var current = Copy("Current"); string path = AssetDatabase.GetAssetPath(current); string bytes = File.ReadAllText(path);
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Copy(Second, path)); Assert.That(File.ReadAllText(path), Is.EqualTo(bytes));
            int hang = current.Powers.AbilitySlots.ToList().FindIndex(s => s.Definition.Id == PlayerLedgeHangState.ABILITY_ID);
            Edit(current.Powers, "_abilities.Array.data[" + hang + "]._enabled", p => p.boolValue = false);
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Validate(current));
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Restore(Copy("Target"), current));
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Copy(current, _folder + "/Invalid.asset"));
            Assert.That(File.Exists(_folder + "/Invalid.asset"), Is.False);
            Edit(current.Powers, "_abilities.Array.data[" + hang + "]._enabled", p => p.boolValue = true);
            Edit(current.Feel.Jump, "_airJumpSpeed", p => p.floatValue = -1);
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeWorkflow.Validate(current));
        }
        [Test] public void IndependentArtCopyDoesNotChangeBuiltinOrAutomaticallyReplaceMapping()
        {
            var source = First.Animation.Slots[0].Clip; string path = AssetDatabase.GetAssetPath(source); string bytes = File.ReadAllText(path);
            var copy = PlayerSchemeWorkflow.CopyArt(source, _folder + "/Own.anim");
            Assert.That(copy, Is.TypeOf<AnimationClip>()); Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(PlayerSchemeOwnership.IsReadOnly(copy), Is.False); Assert.That(copy.hideFlags & HideFlags.NotEditable, Is.EqualTo(HideFlags.None));
            Assert.That(File.ReadAllText(path), Is.EqualTo(bytes)); Assert.That(First.Animation.Slots[0].Clip, Is.SameAs(source));
            Assert.Throws<ArgumentException>(() => PlayerSchemeWorkflow.CopyArt(source, _folder + "/Own.anim"));
        }
        [Test] public void CompatibleProgressPreservesValuesWhileDisabledAndResourceMismatchIsRejected()
        {
            var blank = Copy("Blank", First, true);
            using var original = PlayerPowerSession.CreateNewGame(First.Powers);
            var progress = original.Capture();
            using var disabled = PlayerPowerSession.Restore(blank.Powers, progress);
            Assert.That(disabled.Capture().GetAbility(0).IsOpen, Is.EqualTo(progress.GetAbility(0).IsOpen));
            Assert.That(disabled.Abilities.TryGet(progress.GetAbility(0).Id, out var ability), Is.True);
            Assert.That(ability.IsEnabled, Is.False);
            using var sameAssembly = PlayerPowerSession.Restore(Second.Powers, progress);
            Assert.That(sameAssembly.Capture().AbilityCount, Is.EqualTo(progress.AbilityCount));
            Edit(blank.Powers.AbilitySlots[0].Definition, "_limitedUses", p => p.boolValue = !p.boolValue);
            Assert.That(() => { using var rejected = PlayerPowerSession.Restore(blank.Powers, progress); }, Throws.Exception);
        }
        [Test] public void TrialSelectionDoesNotChangeProjectDefaultAndDefaultChangeIsUndoable()
        {
            var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH); var original = config.Scheme;
            var selection = PlayerDevelopmentEnvironment.SelectedScheme;
            var position = PlayerDevelopmentEnvironment.SelectedPosition; var velocity = PlayerDevelopmentEnvironment.SelectedVelocity;
            try
            {
                PlayerSchemeWorkflow.Use(Second); Assert.That(config.Scheme, Is.SameAs(original));
                Assert.That(PlayerDevelopmentEnvironment.SelectedPosition, Is.EqualTo(position)); Assert.That(PlayerDevelopmentEnvironment.SelectedVelocity, Is.EqualTo(velocity));
                PlayerSchemeWorkflow.SetDefault(First); Assert.That(config.Scheme, Is.SameAs(First));
                Assert.That(PlayerDevelopmentEnvironment.SelectedScheme, Is.SameAs(Second));
                Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(config.Scheme, Is.SameAs(original));
            }
            finally
            {
                if (config.Scheme != original) PlayerSchemeConversion.SelectScheme(config, original, false);
                PlayerDevelopmentEnvironment.SetSchemeParameters(selection, position, velocity);
            }
        }
    }
}
