using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Table;
using Game.Table.Generated;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeRunnerTests
    {
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => id == "alice" || id == "lin";
            public bool TryResolve(NovelCommandKind kind, string key, out string path)
            { path = key == "test_resource" ? "test/resource" : null; return path != null; }
        }

        private static readonly Catalog Ready = new();
        private static NovelNode End(string id = "end") => new(id, NovelNodeKind.Ending, endingId: id);
        private static NovelCommand Say(string id) => new(id, NovelCommandKind.Say, "文字", "line_" + id);
        private static NovelChapter Chapter(params NovelNode[] nodes) => new("test", 1, nodes[0].Id, nodes,
            new[] { new NovelVariable("trust", new NovelValue(0)) });
        private static NovelNode Dialogue(string id, string next, params NovelCommand[] commands)
            => new(id, NovelNodeKind.Dialogue, next, commands: commands);
        private static NovelCondition Trust(int value) => new(NovelJunction.All,
            new NovelPredicate("trust", NovelComparison.Equal, new NovelValue(value)));
        private static void Advance(NarrativeRunner runner, long frame)
        { var s = runner.Snapshot; Assert.IsTrue(runner.Advance(s.SessionGeneration, s.PositionVersion, frame)); }

        [Test]
        public void RevealAndAdvanceAreSeparateAndSameFrameCannotCrossLine()
        {
            var runner = new NarrativeRunner();
            runner.Start(Chapter(Dialogue("a", "end", Say("one"), Say("two")), End()), Ready);
            var old = runner.Snapshot;
            Advance(runner, 1);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, runner.Snapshot.State);
            var now = runner.Snapshot;
            Assert.IsFalse(runner.Advance(old.SessionGeneration, old.PositionVersion, 2));
            Assert.IsFalse(runner.Advance(now.SessionGeneration, now.PositionVersion, 1));
            Advance(runner, 2);
            Assert.AreEqual("two", runner.Snapshot.CommandId);
        }

        [TestCase("yes", "good", 1)]
        [TestCase("no", "bad", 0)]
        public void BranchesMergeAndReachDeterministicEndings(string option, string ending, int trust)
        {
            var chapter = Chapter(
                new NovelNode("choice", NovelNodeKind.Choice, routes: new[] {
                    new NovelRoute("yes", "询问", "ask"), new NovelRoute("no", "离开", "leave") }),
                Dialogue("ask", "merge", new NovelCommand("set", NovelCommandKind.SetVariable,
                    variableId: "trust", value: new NovelValue(1))),
                Dialogue("leave", "merge"), Dialogue("merge", "branch", Say("merge_line")),
                new NovelNode("branch", NovelNodeKind.Branch, "bad", routes: new[] { new NovelRoute("condition", "", "good", Trust(1)) }),
                End("good"), End("bad"));
            for (int pass = 0; pass < 2; pass++)
            {
                var runner = new NarrativeRunner(); Assert.IsTrue(runner.Start(chapter, Ready));
                var s = runner.Snapshot;
                Assert.IsNull(s.CommandId);
                Assert.IsFalse(runner.Choose(s.SessionGeneration, s.PositionVersion, "missing", 0));
                Assert.IsTrue(runner.Choose(s.SessionGeneration, s.PositionVersion, option, 1));
                Assert.IsFalse(runner.Choose(s.SessionGeneration, s.PositionVersion, option, 2));
                Advance(runner, 2); Advance(runner, 3);
                Assert.AreEqual(ending, runner.Snapshot.EndingId);
                Assert.AreEqual(trust, runner.Snapshot.Variables["trust"].Int);
                Assert.AreEqual(0, chapter.Variables[0].Value.Int);
            }
        }

        [Test]
        public void FirstMatchingBranchWins()
        {
            var runner = new NarrativeRunner();
            runner.Start(Chapter(new NovelNode("b", NovelNodeKind.Branch, "fallback", routes: new[] {
                new NovelRoute("first", "", "one", Trust(0)), new NovelRoute("second", "", "two", Trust(0)) }),
                End("one"), End("two"), End("fallback")), Ready);
            Assert.AreEqual("one", runner.Snapshot.EndingId);
        }

        [Test]
        public void FilteredZeroChoicesFaultAtChoiceNode()
        {
            var runner = new NarrativeRunner();
            Assert.IsFalse(runner.Start(Chapter(new NovelNode("choice", NovelNodeKind.Choice, routes: new[] {
                new NovelRoute("hidden", "不可见", "end", Trust(1)) }), End()), Ready));
            Assert.AreEqual("ZeroOptions", runner.Snapshot.Error.Code);
            Assert.AreEqual("choice", runner.Snapshot.Error.NodeId);
            Assert.IsNull(runner.Snapshot.CommandId);
        }

        [Test]
        public void EmptyNodeCycleAndImmediateCommandsShareOneBudget()
        {
            var runner = new NarrativeRunner(8);
            Assert.IsFalse(runner.Start(Chapter(Dialogue("a", "b"), Dialogue("b", "a")), Ready));
            Assert.AreEqual("StepLimit", runner.Snapshot.Error.Code);
            Assert.IsFalse(runner.Start(Chapter(Dialogue("a", "a", new NovelCommand("set", NovelCommandKind.SetVariable,
                variableId: "trust", value: new NovelValue(1)))), Ready));
            Assert.AreEqual("StepLimit", runner.Snapshot.Error.Code);
        }

        [Test]
        public void MissingTargetsAndDuplicateIdsAreRejectedBeforeSideEffects()
        {
            var runner = new NarrativeRunner();
            Assert.IsFalse(runner.Start(Chapter(Dialogue("a", null, new NovelCommand("set", NovelCommandKind.SetVariable,
                variableId: "trust", value: new NovelValue(1)))), Ready));
            Assert.AreEqual("BadTarget", runner.Snapshot.Error.Code);
            Assert.IsEmpty(runner.Snapshot.Variables);
            var errors = NarrativeValidator.Validate(Chapter(Dialogue("a", "end", Say("same"), Say("same")), End()), Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "BadCommandId"));
            Assert.IsTrue(errors.Any(e => e.Code == "BadLineId"));
        }

        [Test]
        public void TypeAndResourceErrorsContainExactPosition()
        {
            var chapter = Chapter(Dialogue("a", "end", new NovelCommand("wrong_type", NovelCommandKind.SetVariable,
                variableId: "trust", value: new NovelValue(true)),
                new NovelCommand("missing_bg", NovelCommandKind.Background, resourceKey: "absent")), End());
            var errors = NarrativeValidator.Validate(chapter, Ready);
            Assert.IsTrue(errors.Any(e => e.Code == "BadAssignment" && e.CommandId == "wrong_type"));
            Assert.IsTrue(errors.Any(e => e.NodeId == "a" && e.CommandId == "missing_bg" && e.ResourceKey == "absent"));
            Assert.IsTrue(NarrativeValidator.Validate(chapter, null).Any(e => e.Code == "CatalogNotReady"));
        }

        [Test]
        public void NestedPauseFreezesWaitAndReleasesOnlyItsOwnToken()
        {
            var runner = new NarrativeRunner();
            runner.Start(Chapter(Dialogue("a", "end", new NovelCommand("wait", NovelCommandKind.Wait, duration: 2)), End()), Ready);
            long generation = runner.Snapshot.SessionGeneration;
            runner.Pause("settings"); runner.Pause("confirm");
            Assert.IsFalse(runner.Tick(generation, 100, 1));
            runner.Resume("confirm"); Assert.IsFalse(runner.Tick(generation, 100, 2));
            runner.Resume("settings"); Assert.IsTrue(runner.Tick(generation, 1, 3));
            Assert.AreEqual(NarrativeWait.Timer, runner.Snapshot.Wait);
            Assert.IsFalse(runner.Tick(generation, 100, 3));
            Assert.IsTrue(runner.Tick(generation, 1, 4));
            Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
        }

        [Test]
        public void CancelAndRestartRejectOldCallbacksAndSnapshotsRemainImmutable()
        {
            var runner = new NarrativeRunner();
            var chapter = Chapter(Dialogue("a", "end", Say("one")), End());
            runner.Start(chapter, Ready); var old = runner.Snapshot;
            runner.Cancel(); Assert.IsFalse(runner.CompleteReveal(old.SessionGeneration, old.PositionVersion));
            runner.Start(chapter, Ready);
            Assert.AreNotEqual(old.SessionGeneration, runner.Snapshot.SessionGeneration);
            Assert.IsFalse(runner.CompleteReveal(old.SessionGeneration, old.PositionVersion));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, NovelValue>)old.Variables)["trust"] = new NovelValue(9));
            Assert.AreEqual(NarrativeState.Revealing, old.State);
        }

        [Test]
        public void ReentrantOrThrowingObserverCannotDriveOrBreakRunner()
        {
            var runner = new NarrativeRunner(); int notifications = 0;
            runner.Changed += () => { notifications++; Assert.IsFalse(runner.Cancel()); throw new InvalidOperationException("observer"); };
            Assert.IsTrue(runner.Start(Chapter(Dialogue("a", "end", Say("one")), End()), Ready));
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(NarrativeState.Revealing, runner.Snapshot.State);
            StringAssert.Contains("observer", runner.LastObserverError);
        }

        [Test]
        public void PresentationWaitIsExplicitAndFailureRetainsResourceKey()
        {
            var runner = new NarrativeRunner();
            runner.Start(Chapter(Dialogue("a", "end", new NovelCommand("bg", NovelCommandKind.Background, resourceKey: "test_resource")), End()), Ready);
            var s = runner.Snapshot;
            Assert.AreEqual(NarrativeWait.Presentation, s.Wait);
            Assert.IsTrue(runner.CompletePresentation(s.SessionGeneration, s.PositionVersion, "加载失败"));
            Assert.AreEqual("test_resource", runner.Snapshot.Error.ResourceKey);
            Assert.AreEqual(NarrativeState.Faulted, runner.Snapshot.State);
        }

        [Test]
        public void BooleanStringAndOrConditionsUseDeclaredTypes()
        {
            var vars = new Dictionary<string, NovelValue> { ["flag"] = new(true), ["name"] = new("Alice") };
            var yes = new NovelPredicate("flag", NovelComparison.Equal, new NovelValue(true));
            var no = new NovelPredicate("name", NovelComparison.Equal, new NovelValue("alice"));
            Assert.IsFalse(new NovelCondition(NovelJunction.All, yes, no).Evaluate(vars));
            Assert.IsTrue(new NovelCondition(NovelJunction.Any, yes, no).Evaluate(vars));
        }

        [Test]
        public void RealTableArtifactsLoadAndSampleSOsPlayWithoutMutation()
        {
            using var engine = new EmberTableEngine();
            var catalog = GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                TextAsset asset = Resources.Load<TextAsset>(entry.ResourcePath);
                Assert.IsNotNull(asset, entry.ResourcePath); bytes.Add(entry.TableId, asset.bytes);
            }
            Assert.IsTrue(engine.Load(catalog, bytes).Succeeded);
            var tables = new NarrativeTableCatalog(engine.Database);
            Assert.IsTrue(tables.IsReady); Assert.IsTrue(tables.HasCharacter("alice"));
            Assert.IsFalse(tables.TryResolve(NovelCommandKind.Background, "missing", out _));
            var chapter = AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/Chapters/CH01_School/Chapter.asset");
            Assert.IsNotNull(chapter);
            var before = chapter.Nodes.ToDictionary(n => n, n => EditorJsonUtility.ToJson(n));
            string chapterBefore = EditorJsonUtility.ToJson(chapter);
            Assert.IsTrue(chapter.TryReadDefinition(tables, out var definition, out var errors), string.Join("\n", errors));
            foreach (string option in new[] { "ask_option", "leave_option" })
            {
                var runner = new NarrativeRunner(); Assert.IsTrue(runner.Start(definition, tables));
                for (int frame = 0; frame < 30 && runner.Snapshot.HasActiveSession; frame++)
                {
                    var s = runner.Snapshot;
                    if (s.State == NarrativeState.AwaitingChoice) runner.Choose(s.SessionGeneration, s.PositionVersion, option, frame);
                    else if (s.Wait == NarrativeWait.Timer) runner.Tick(s.SessionGeneration, 1, frame);
                    // 此用例只验证纯运行器路线；真实加载/演出由 M2 Gameplay 整合测试覆盖。
                    else if ((s.Wait & NarrativeWait.Presentation) != 0) runner.CompletePresentation(s.SessionGeneration, s.PositionVersion);
                    else runner.Advance(s.SessionGeneration, s.PositionVersion, frame);
                }
                Assert.AreEqual(option == "ask_option" ? "good" : "quiet", runner.Snapshot.EndingId);
            }
            foreach (var pair in before) Assert.AreEqual(pair.Value, EditorJsonUtility.ToJson(pair.Key));
            Assert.AreEqual(chapterBefore, EditorJsonUtility.ToJson(chapter));
        }

        [Test]
        public void AssetAdapterRejectsCrossChapterReference()
        {
            var chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>();
            var node = ScriptableObject.CreateInstance<NarrativeDialogueSO>();
            var end = ScriptableObject.CreateInstance<NarrativeEndingSO>();
            try
            {
                var s = new SerializedObject(chapter);
                s.FindProperty("_chapterId").stringValue = "local";
                s.FindProperty("_entry").objectReferenceValue = node;
                var nodes = s.FindProperty("_nodes"); nodes.arraySize = 2;
                nodes.GetArrayElementAtIndex(0).objectReferenceValue = node;
                nodes.GetArrayElementAtIndex(1).objectReferenceValue = end;
                s.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(chapter.TryReadDefinition(Ready, out _, out var errors));
                Assert.AreEqual("BadTarget", errors[0].Code);
            }
            finally { UnityEngine.Object.DestroyImmediate(chapter); UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(end); }
        }

        [Test]
        public void PauseNotificationsDoNotInvalidatePendingCompletionToken()
        {
            var runner = new NarrativeRunner();
            runner.Start(Chapter(Dialogue("a", "end", Say("one")), End()), Ready);
            var token = runner.Snapshot;
            runner.Pause("settings");
            Assert.IsFalse(runner.CompleteReveal(token.SessionGeneration, token.PositionVersion));
            runner.Resume("settings");
            Assert.IsTrue(runner.CompleteReveal(token.SessionGeneration, token.PositionVersion));
        }

        [Test]
        public void DefinitionSnapshotDoesNotTrackLaterSOEdits()
        {
            var chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>();
            var node = ScriptableObject.CreateInstance<NarrativeDialogueSO>();
            var end = ScriptableObject.CreateInstance<NarrativeEndingSO>();
            try
            {
                var n = new SerializedObject(node);
                n.FindProperty("_chapterId").stringValue = "local";
                n.FindProperty("_nodeId").stringValue = "talk";
                n.FindProperty("_next").objectReferenceValue = end;
                var commands = n.FindProperty("_commands"); commands.arraySize = 1;
                var command = commands.GetArrayElementAtIndex(0);
                command.FindPropertyRelative("_commandId").stringValue = "say";
                command.FindPropertyRelative("_lineId").stringValue = "line";
                command.FindPropertyRelative("_textRevision").intValue = 1;
                command.FindPropertyRelative("_text").stringValue = "original";
                n.ApplyModifiedPropertiesWithoutUndo();
                var e = new SerializedObject(end);
                e.FindProperty("_chapterId").stringValue = "local";
                e.FindProperty("_endingId").stringValue = "end";
                e.ApplyModifiedPropertiesWithoutUndo();
                var s = new SerializedObject(chapter);
                s.FindProperty("_chapterId").stringValue = "local";
                s.FindProperty("_entry").objectReferenceValue = node;
                var nodes = s.FindProperty("_nodes"); nodes.arraySize = 2;
                nodes.GetArrayElementAtIndex(0).objectReferenceValue = node;
                nodes.GetArrayElementAtIndex(1).objectReferenceValue = end;
                s.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsTrue(chapter.TryReadDefinition(Ready, out var definition, out var errors), string.Join("\n", errors));
                n.Update();
                n.FindProperty("_commands").GetArrayElementAtIndex(0).FindPropertyRelative("_text").stringValue = "edited";
                n.ApplyModifiedPropertiesWithoutUndo();
                var runner = new NarrativeRunner(); runner.Start(definition, Ready);
                Assert.AreEqual("original", runner.CurrentCommand.Text);
                Assert.AreEqual("edited", node.Commands[0].Text);
                string before = EditorJsonUtility.ToJson(node);
                Advance(runner, 1); Advance(runner, 2);
                Assert.AreEqual(before, EditorJsonUtility.ToJson(node));
                e.Update(); e.FindProperty("_chapterId").stringValue = "foreign";
                e.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsFalse(chapter.TryReadDefinition(Ready, out _, out errors));
                Assert.AreEqual("talk", errors[0].NodeId);
            }
            finally { UnityEngine.Object.DestroyImmediate(chapter); UnityEngine.Object.DestroyImmediate(node); UnityEngine.Object.DestroyImmediate(end); }
        }
    }
}
