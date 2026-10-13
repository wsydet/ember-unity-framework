using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerGroundPhaseEditorTests
    {
        private readonly List<UnityEngine.Object> _owned = new();
        private T Keep<T>(T item) where T : UnityEngine.Object { _owned.Add(item); return item; }
        private string Describe(EmberFsmStateDefinition definition, PlayerFeelRuntime feel, PlayerPowerConfig powers)
        {
            var type = typeof(PlayerFsmParameterEditor).Assembly.GetType("Game.Player.Editor.PlayerStateAvailability");
            return (string)type.GetMethod("Describe", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { definition, feel, powers });
        }
        [TearDown]
        public void TearDown()
        {
            foreach (var item in _owned) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            _owned.Clear();
        }

        [Test]
        public void AvailabilitySeparatesAssetToggleFromCapturedFeelAndSupportsUndo()
        {
            var original = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            var feel = Keep(original.Feel.CreateIndependentCopy());
            Keep(feel.Movement); Keep(feel.Jump); Keep(feel.Input);
            using var serialized = new SerializedObject(feel.Movement);
            serialized.FindProperty("_accelerated").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var frozen = feel.Capture(Physics2D.gravity);
            var definition = Keep(ScriptableObject.CreateInstance<PlayerAccelerateStateDefinition>());
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(feel.Movement, "Ground phase toggle");
            serialized.Update(); serialized.FindProperty("_accelerated").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo(); Undo.FlushUndoRecordObjects();
            Assert.That(Describe(definition, feel.Capture(Physics2D.gravity), original.Powers), Does.Contain("已启用"));
            Assert.That(Describe(definition, frozen, original.Powers), Does.Contain("禁用"));
            Undo.PerformUndo();
            Assert.That(feel.Movement.Accelerated, Is.False);
            Undo.PerformRedo();
            Assert.That(feel.Movement.Accelerated, Is.True);
            Undo.RevertAllDownToGroup(group);
        }

        [Test]
        public void AllFormalNodesHaveAvailabilityWithoutChangingGraphOrResources()
        {
            var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            string before = EditorJsonUtility.ToJson(config.Graph);
            var feel = config.Feel.Capture(Physics2D.gravity);
            foreach (var node in config.Graph.Nodes)
                Assert.That(Describe(node.Definition, feel, config.Powers), Is.Not.Null.And.Not.Empty, node.Id);
            Assert.That(EditorJsonUtility.ToJson(config.Graph), Is.EqualTo(before));
            Assert.That(config.Graph.Nodes.Count, Is.EqualTo(21));
        }

        [Test]
        public void GraphAndNewDefinitionsReloadWithStableIdsAndBindings()
        {
            var graph = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Graph;
            var path = AssetDatabase.GetAssetPath(graph);
            string before = EditorJsonUtility.ToJson(graph), guid = AssetDatabase.AssetPathToGUID(path);
            foreach (var node in graph.Nodes.Where(n => n.Id == "accelerate" || n.Id == "decelerate"))
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(node.Definition), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<EmberFsmGraph>(path);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            Assert.That(EditorJsonUtility.ToJson(reloaded), Is.EqualTo(before));
            Assert.That(reloaded.Inspect(typeof(PlayerContext)).IsValid, Is.True);
            Assert.That(reloaded.Nodes.Single(n => n.Id == "accelerate").Definition, Is.TypeOf<PlayerAccelerateStateDefinition>());
            Assert.That(reloaded.Nodes.Single(n => n.Id == "decelerate").Definition, Is.TypeOf<PlayerDecelerateStateDefinition>());
        }
    }
}
