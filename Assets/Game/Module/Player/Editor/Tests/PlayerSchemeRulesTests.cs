using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor.Tests
{
    public sealed class PlayerSchemeRulesTests
    {
        private string _folder;
        private PlayerScheme Copy(string name, PlayerScheme source = null, bool blank = false)
            => PlayerSchemeWorkflow.Copy(source ?? PlayerSchemeOwnership.Builtins[0], _folder + "/" + name + ".asset", blank);
        private static void Set(UnityEngine.Object target, string field, Action<SerializedProperty> edit)
        { using var data = new SerializedObject(target); edit(data.FindProperty(field)); data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Condition(PlayerScheme scheme, EmberConditionExpression expression)
        { typeof(PlayerSchemeRule).GetField("_condition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scheme.Rules.Rules[2], expression); EditorUtility.SetDirty(scheme.Rules); }
        private static EmberConditionExpression False() => new("not", new EmberConditionNode("not", "not", null, "value"), new EmberConditionNode("value", "true"));
        [SetUp] public void Setup() { _folder = "Assets/E02RulesTest_" + Guid.NewGuid().ToString("N"); AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder)); }
        [TearDown] public void Cleanup()
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { _folder }))
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))) Undo.ClearUndo(asset);
            AssetDatabase.DeleteAsset(_folder);
        }
        [Test] public void MigrationIsExplicitEquivalentAndUndoRedoRestoresOwnedAsset()
        {
            var scheme = Copy("A"); string before = EditorJsonUtility.ToJson(scheme);
            Assert.That(PlayerSchemeRules.Capture(scheme), Is.Null); Assert.That(EditorJsonUtility.ToJson(scheme), Is.EqualTo(before));
            PlayerSchemeRuleWorkflow.Enable(scheme);
            Assert.That(PlayerSchemeRules.Capture(scheme).Descriptions.Count, Is.EqualTo(3));
            Assert.That(scheme.Rules.Owner, Is.SameAs(scheme)); Assert.That(EditorUtility.IsDirty(scheme), Is.True);
            Assert.That(AssetDatabase.GetAssetPath(scheme.Rules), Is.EqualTo(AssetDatabase.GetAssetPath(scheme)));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(scheme.RuleMode, Is.EqualTo(PlayerRuleMode.LegacyCode)); Assert.That(scheme.Rules, Is.Null);
            Undo.PerformRedo(); Assert.That(scheme.RuleMode, Is.EqualTo(PlayerRuleMode.Configured)); PlayerSchemeWorkflow.Validate(scheme);
        }
        [Test] public void CopyCompareBlankAndReloadRespectRulesWithoutCopyingGraph()
        {
            var a = Copy("A"); PlayerSchemeRuleWorkflow.Enable(a); Condition(a, False()); PlayerSchemeWorkflow.Save(a);
            var b = Copy("B", a); Assert.That(b.Graph, Is.SameAs(a.Graph)); Assert.That(b.Rules, Is.Not.SameAs(a.Rules));
            Assert.That(b.Rules.Owner, Is.SameAs(b)); Assert.That(PlayerSchemeWorkflow.Compare(a, b), Is.Empty);
            var blank = Copy("Blank", a, true);
            Assert.That(blank.RuleMode, Is.EqualTo(PlayerRuleMode.Configured));
            Assert.That(blank.Rules.Rules.All(r => PlayerRuleConditions.CreateCatalog(blank).Compile(r.Condition).Evaluate(null)), Is.True);
            string path = AssetDatabase.GetAssetPath(b); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<PlayerScheme>(path); PlayerSchemeWorkflow.Validate(reloaded);
            Assert.That(PlayerRuleConditions.CreateCatalog(reloaded).Compile(reloaded.Rules.Rules[2].Condition).Evaluate(null), Is.False);
            Assert.That(a.Rules.Rules[2].Condition.Root, Is.EqualTo("not"));
        }
        [Test] public void RestoreLegacyAndConfiguredModesIsUndoable()
        {
            var a = Copy("A"); var reference = Copy("Reference"); PlayerSchemeRuleWorkflow.Enable(a); Condition(a, False());
            PlayerSchemeWorkflow.Restore(a, reference); Assert.That(a.Rules, Is.Null); PlayerSchemeWorkflow.Validate(a);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(a.Rules, Is.Not.Null); PlayerSchemeWorkflow.Validate(a);
            Undo.PerformRedo(); Assert.That(a.Rules, Is.Null);
            PlayerSchemeRuleWorkflow.Enable(reference); Condition(reference, False());
            PlayerSchemeWorkflow.Restore(a, reference); PlayerSchemeWorkflow.Validate(a);
            Assert.That(a.Rules.Owner, Is.SameAs(a)); Assert.That(a.Rules, Is.Not.SameAs(reference.Rules));
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo(); Assert.That(a.Rules, Is.Null);
            Undo.PerformRedo(); PlayerSchemeWorkflow.Validate(a);
        }
        [Test] public void InvalidModeVersionOwnershipAndMissingConfiguredAssetNeverFallback()
        {
            var a = Copy("A"); var b = Copy("B"); PlayerSchemeRuleWorkflow.Enable(a);
            Set(a.Rules, "_schemaVersion", p => p.intValue = 99); Assert.Throws<InvalidOperationException>(() => PlayerSchemeRules.Capture(a));
            Set(a.Rules, "_schemaVersion", p => p.intValue = 1);
            Set(b, "_rules", p => p.objectReferenceValue = a.Rules); Set(b, "_ruleMode", p => p.enumValueIndex = 1);
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeRules.Capture(b));
            Set(b, "_rules", p => p.objectReferenceValue = null); Assert.Throws<InvalidOperationException>(() => PlayerSchemeRules.Capture(b));
            Assert.Throws<InvalidOperationException>(() => PlayerSchemeRuleWorkflow.Enable(PlayerSchemeOwnership.Builtins[0]));
        }
        [Test] public void UnknownPowerAndOutOfRangeInputAreInvalidEvenIfBranchWouldShortCircuit()
        {
            var a = Copy("A"); var catalog = PlayerRuleConditions.CreateCatalog(a);
            Assert.Throws<ArgumentException>(() => catalog.Compile(new EmberConditionExpression("p", new EmberConditionNode("p", "player.power", new[] {
                new EmberConditionParameter("kind", EmberConditionValueKind.Choice), new EmberConditionParameter("id", EmberConditionValueKind.Text, text: "missing") }))));
            Assert.Throws<ArgumentException>(() => catalog.Compile(new EmberConditionExpression("v", new EmberConditionNode("v", "player.input-axis", new[] {
                new EmberConditionParameter("axis", EmberConditionValueKind.Choice), new EmberConditionParameter("absolute", EmberConditionValueKind.Boolean),
                new EmberConditionParameter("compare", EmberConditionValueKind.Choice), new EmberConditionParameter("threshold", EmberConditionValueKind.Number, number: 2) }))));
        }
        [Test] public void FrozenPlanSurvivesLaterAssetChangesAndDescriptionsDoNotQuery()
        {
            var a = Copy("A"); PlayerSchemeRuleWorkflow.Enable(a);
            var frozen = PlayerSchemeRules.Capture(a); Condition(a, False()); var next = PlayerSchemeRules.Capture(a);
            Assert.That(frozen.Descriptions[2].Identity, Is.Not.EqualTo(next.Descriptions[2].Identity));
            Assert.That(frozen.Descriptions[2].Condition, Does.Contain("无额外限制"));
            Assert.That(next.Descriptions[2].Condition, Does.Contain("NOT"));
        }
        [Test] public void SerializedTreeWrappingPreservesChildrenAndSupportsUndoRedo()
        {
            var a = Copy("Tree"); PlayerSchemeRuleWorkflow.Enable(a);
            var wrap = typeof(Ember.Core.Editor.EmberConditionEditor).GetMethod("Wrap", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(PlayerContext));
            var catalog = PlayerRuleConditions.CreateCatalog(a);
            using var data = new SerializedObject(a.Rules);
            var expression = data.FindProperty("_rules").GetArrayElementAtIndex(2).FindPropertyRelative("_condition");
            var nodes = expression.FindPropertyRelative("_nodes");
            string root = expression.FindPropertyRelative("_root").stringValue;
            Undo.IncrementCurrentGroup();
            wrap.Invoke(null, new object[] { nodes, root, "and", catalog }); data.ApplyModifiedProperties();
            Assert.That(a.Rules.Rules[2].Condition.Nodes.Count, Is.EqualTo(3));
            Assert.That(catalog.Compile(a.Rules.Rules[2].Condition).Evaluate(null), Is.True);
            data.Update(); nodes = data.FindProperty("_rules").GetArrayElementAtIndex(2).FindPropertyRelative("_condition._nodes");
            string child = a.Rules.Rules[2].Condition.Nodes.Single(n => n.Id == root).Children[0];
            wrap.Invoke(null, new object[] { nodes, child, "not", catalog }); data.ApplyModifiedProperties();
            Assert.That(catalog.Compile(a.Rules.Rules[2].Condition).Evaluate(null), Is.False);
            Undo.FlushUndoRecordObjects(); Undo.PerformUndo();
            Assert.That(a.Rules.Rules[2].Condition.Nodes.Count, Is.EqualTo(1));
            Undo.PerformRedo(); Assert.That(catalog.Compile(a.Rules.Rules[2].Condition).Evaluate(null), Is.False);
            PlayerSchemeWorkflow.Save(a);
        }
    }
}
