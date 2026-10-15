using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor.Tests
{
    public sealed class CombatGraphFixtureState : EmberFsmState<PlayerContext> { }

    public sealed class PlayerCombatGraphBuilderTests
    {
        [Test]
        public void OptionalRecoveryClipResolvesIdleButAttackClipIsRequired()
        {
            var source = PlayerSchemeOwnership.Builtins[0];
            var graph = Object.Instantiate(source.Graph);
            var animation = Object.Instantiate(source.Animation);
            var definition = ScriptableObject.CreateInstance<CombatGraphFixtureDefinition>();
            definition.Role = PlayerCombatNodeRole.Recovery;
            try
            {
                graph.Configure(source.Graph.EntryId, source.Graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                    n.Id == "move" ? definition : n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.ToArray())).ToArray());
                using (var data = new UnityEditor.SerializedObject(animation))
                {
                    var slots = data.FindProperty("_slots");
                    for (int i = 0; i < slots.arraySize; i++)
                        if (slots.GetArrayElementAtIndex(i).FindPropertyRelative("_nodeId").stringValue == "move")
                            slots.GetArrayElementAtIndex(i).FindPropertyRelative("_clip").objectReferenceValue = null;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                animation.Validate(graph);
                Assert.That(animation.ResolveClip(animation.Slots.First(s => s.NodeId == "move"), graph),
                    Is.SameAs(animation.Slots.First(s => s.NodeId == "idle").Clip));
                definition.Role = PlayerCombatNodeRole.Attack;
                StringAssert.Contains("move", Assert.Throws<InvalidOperationException>(() => animation.Validate(graph)).Message);
                Assert.That(source.Animation.Slots.First(s => s.NodeId == "move").Clip, Is.Not.Null);
            }
            finally { Object.DestroyImmediate(definition); Object.DestroyImmediate(animation); Object.DestroyImmediate(graph); }
        }

        [Test]
        public void CandidateOwnsOnlyCombatNodesAndDetectsComboDivergence()
        {
            var source = PlayerSchemeOwnership.Builtins[0].Graph;
            string before = JsonUtility.ToJson(source);
            var attack = PlayerCombatConfigurationTests.Attack();
            var combo = JsonUtility.FromJson<PlayerCombo>("{\"_groundEntry\":\"first\",\"_connections\":[]}");
            var body = ScriptableObject.CreateInstance<CombatGraphFixtureDefinition>();
            var recovery = ScriptableObject.CreateInstance<CombatGraphFixtureDefinition>(); recovery.Role = PlayerCombatNodeRole.Recovery;
            EmberFsmGraph candidate = null, updated = null;
            try
            {
                candidate = PlayerCombatGraphBuilder.Prepare(source, new PlayerCombatGraphMap(), attack, combo, body, recovery, out var map);
                Assert.That(candidate.Nodes.Count, Is.EqualTo(source.Nodes.Count + 2));
                Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
                foreach (var node in source.Nodes)
                    Assert.That(candidate.Nodes.First(n => n.Id == node.Id).Bindings.Select(b => b.Slot + ":" + b.TargetId),
                        Is.EqualTo(node.Bindings.Select(b => b.Slot + ":" + b.TargetId)));
                JsonUtility.FromJsonOverwrite("{\"_groundEntry\":\"first\",\"_connections\":[{\"_source\":\"first\",\"_normal\":\"first\"}]}", combo);
                StringAssert.Contains("Combo", Assert.Throws<InvalidOperationException>(() => map.Validate(candidate, attack, combo)).Message);
                updated = PlayerCombatGraphBuilder.Prepare(candidate, map, attack, combo, body, recovery, out var updatedMap);
                Assert.That(updatedMap.Nodes[0].AttackNodeId, Is.EqualTo(map.Nodes[0].AttackNodeId));
                Assert.That(updated.Nodes.Count, Is.EqualTo(candidate.Nodes.Count));
                updatedMap.Validate(updated, attack, combo);
                Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
            }
            finally
            {
                if (candidate != null) Object.DestroyImmediate(candidate);
                if (updated != null) Object.DestroyImmediate(updated);
                Object.DestroyImmediate(body); Object.DestroyImmediate(recovery);
            }
        }
    }
}
