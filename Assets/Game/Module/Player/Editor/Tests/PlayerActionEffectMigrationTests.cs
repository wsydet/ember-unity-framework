using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerActionEffectMigrationTests
    {
        private string _folder;
        private PlayerScheme _scheme;
        private PlayerGroundDashStateDefinition _definition;
        private EntityEffectDefinition _action, _tail;
        private readonly List<UnityEngine.Object> _memory = new();
        private T Keep<T>(T value) where T : UnityEngine.Object { _memory.Add(value); return value; }
        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        { using var data = new SerializedObject(target); data.FindProperty(property).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        [SetUp] public void Setup()
        {
            _folder = "Assets/ActionEffectMigration_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            _scheme = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Scheme));
            Set(_scheme, "_actionEffects", null);
            _definition = Keep(ScriptableObject.CreateInstance<PlayerGroundDashStateDefinition>());
            _action = Keep(ScriptableObject.CreateInstance<EntityEffectDefinition>());
            using (var data = new SerializedObject(_action))
            { data.FindProperty("_id").stringValue = "migration.protect"; data.FindProperty("_lifetime").enumValueIndex = (int)EntityEffectLifetime.Owned; data.ApplyModifiedPropertiesWithoutUndo(); }
            _tail = Keep(UnityEngine.Object.Instantiate(_action));
            using (var data = new SerializedObject(_tail))
            { data.FindProperty("_lifetime").enumValueIndex = (int)EntityEffectLifetime.Timed; data.FindProperty("_duration").floatValue = 2.75f; data.ApplyModifiedPropertiesWithoutUndo(); }
            Set(_definition, "_actionEffect", _action); Set(_definition, "_tailEffect", _tail);
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("one", new EmberFsmNode("one", "One", _definition), new EmberFsmNode("two", "Two", _definition));
            Set(_scheme, "_graph", graph);
        }
        [TearDown] public void Teardown()
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { _folder }))
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))) Undo.ClearUndo(asset);
            foreach (var item in _memory) if (item != null) Undo.ClearUndo(item);
            AssetDatabase.DeleteAsset(_folder);
            foreach (var item in _memory) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            _memory.Clear();
        }
        [Test] public void MigrationClonesMutableEffectsPreservesSharingAndBusinessValues()
        {
            string before = EditorJsonUtility.ToJson(_definition);
            var graph = _scheme.Graph;
            var config = PlayerActionEffectMigration.Migrate(_scheme, _folder + "/Effects.asset");
            Assert.That(config.Version, Is.EqualTo(1)); Assert.That(config.Slots.Count, Is.EqualTo(2));
            Assert.That(config.Slots[0].ActionEffect, Is.Not.SameAs(_action));
            Assert.That(config.Slots[0].ActionEffect, Is.SameAs(config.Slots[1].ActionEffect));
            Assert.That(config.Slots[0].ActionEffect.Id, Is.EqualTo(_action.Id));
            Assert.That(config.Slots[0].TailEffect.Duration, Is.EqualTo(2.75f));
            Assert.That(_scheme.Graph, Is.SameAs(graph));
            Assert.That(EditorJsonUtility.ToJson(_definition), Is.EqualTo(before));
            Assert.That(_definition.ActionEffect, Is.Null, "旧字段不再成为运行来源");
        }
        [Test] public void RepeatedMigrationPreservesEditsAndUndoRedoPreservesCreatedConfiguration()
        {
            Undo.IncrementCurrentGroup();
            var config = PlayerActionEffectMigration.Migrate(_scheme, _folder + "/Effects.asset");
            using (var data = new SerializedObject(config.Slots[0].TailEffect))
            { data.FindProperty("_duration").floatValue = 8; data.ApplyModifiedPropertiesWithoutUndo(); }
            Assert.That(PlayerActionEffectMigration.Migrate(_scheme, _folder + "/Other.asset"), Is.SameAs(config));
            Assert.That(File.Exists(_folder + "/Other.asset"), Is.False);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(_scheme.ActionEffects, Is.Null); Assert.That(config != null, Is.True);
            Assert.Throws<InvalidOperationException>(() => PlayerActionEffectMigration.Migrate(_scheme, _folder + "/Effects.asset"));
            Undo.PerformRedo(); Assert.That(_scheme.ActionEffects, Is.SameAs(config));
            Assert.That(config.Slots[0].TailEffect.Duration, Is.EqualTo(8));
        }
        [Test] public void InvalidEffectsAndOccupiedPathFailWithoutChangingSourceOrCreatingPartialFiles()
        {
            string before = EditorJsonUtility.ToJson(_scheme);
            string path = _folder + "/Effects.asset";
            using (var data = new SerializedObject(_tail))
            { data.FindProperty("_lifetime").enumValueIndex = (int)EntityEffectLifetime.Owned; data.ApplyModifiedPropertiesWithoutUndo(); }
            Assert.Throws<ArgumentException>(() => PlayerActionEffectMigration.Migrate(_scheme, path));
            Assert.That(File.Exists(path), Is.False); Assert.That(EditorJsonUtility.ToJson(_scheme), Is.EqualTo(before));
            File.WriteAllText(path, "occupied");
            Assert.Throws<InvalidOperationException>(() => PlayerActionEffectMigration.Migrate(_scheme, path));
            Assert.That(File.ReadAllText(path), Is.EqualTo("occupied"));
        }
        [Test] public void ExplicitEmptyMigrationDoesNotFallbackAndSurvivesAssetReload()
        {
            Set(_definition, "_actionEffect", null); Set(_definition, "_tailEffect", null);
            var config = PlayerActionEffectMigration.Migrate(_scheme, _folder + "/Effects.asset");
            Set(_definition, "_actionEffect", _action);
            string path = AssetDatabase.GetAssetPath(config); string guid = AssetDatabase.AssetPathToGUID(path);
            AssetDatabase.SaveAssetIfDirty(config); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            config = AssetDatabase.LoadAssetAtPath<PlayerActionEffects>(path);
            Assert.That(config.Version, Is.EqualTo(1)); Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(config.Validate(_scheme.Graph).Values.All(s => s.ActionEffect == null && s.TailEffect == null), Is.True);
        }
        [Test] public void LegacyFieldsAreHiddenAndOnlyRuntimeInjectionFeedsCreateState()
        {
            foreach (var type in new[] { typeof(PlayerGroundDashStateDefinition), typeof(PlayerAirDashStateDefinition) })
                foreach (var field in new[] { "_actionEffect", "_tailEffect" })
                    Assert.That(Attribute.IsDefined(type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), typeof(HideInInspector)), Is.True);
            Assert.That(_definition.ActionEffect, Is.Null); Assert.That(_definition.TailEffect, Is.Null);
        }
    }
}
