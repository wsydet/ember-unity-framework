using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Narrative.Editor;
using Game.NovelSave;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        [Test]
        public void GeneratedPredecessorSaveRestoresAndStartsCustomGameOnlyAfterAdvance()
        {
            ResetCustomStepStatics();
            var chapter = _story.Entry;
            var talk = (NarrativeDialogueSO)chapter.Entry;
            var ending = chapter.Nodes.OfType<NarrativeEndingSO>().Single();
            var game = Create<NarrativeDialogueSO>();
            var script = Create<TestWriteStep>();
            Set(game, "_chapterId", chapter.ChapterId); Set(game, "_nodeId", "minigame");
            Ref(talk, "_next", game); Ref(game, "_next", ending);
            List(chapter, "_nodes", talk, game, ending); List(_story, "_customSteps", script);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = new List<NovelCommand> {
                new("before", NovelCommandKind.Say, "开始小游戏", "before-line") } }), talk);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = new List<NovelCommand> {
                new("game", NovelCommandKind.CustomStep, customStepId: script.ScriptId) } }), game);
            Assert.IsTrue(_story.TryReadDefinition(_tables, out var definition, out _));
            var target = NovelTestSaveBuilder.FindTargets(definition, chapter.ChapterId, "minigame").Single();
            var settings = NovelTestSaveBuilder.Defaults(definition, target, "Tests/Story", Array.Empty<string>());
            var checkpoint = NovelTestSaveBuilder.Build(definition, _tables, settings);
            Assert.AreEqual(0, TestWriteStep.Begins);
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            using var session = new NovelSession(checkpoint, () => _tables, resources);
            for (int i = 0; i < 5 && !session.RestoreReady; i++) session.Tick(0, i);
            Assert.IsTrue(session.RestoreReady, session.Snapshot.Error?.ToString());
            session.CommitRestore(new View());
            Assert.AreEqual(0, TestWriteStep.Begins);
            session.Advance(10); session.Tick(0, 11);
            Assert.AreEqual(1, TestWriteStep.Begins);
            Assert.AreEqual("minigame", session.Snapshot.NodeId);
        }
    }

    public sealed class NovelTestSaveTests
    {
        #region 内部参数
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => false;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = null; return false; }
        }
        private static readonly Catalog Ready = new();
        private TestWriteStep _script;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static NovelCommand Say(string id) => new(id, NovelCommandKind.Say, "测试台词", id + "-line");
        private static NovelNode Talk(string id, string next, string scope = "")
            => new(id, NovelNodeKind.Dialogue, next, commands: new[] { Say(id + "-say") }, scopeId: scope);
        private static NovelNode End() => new("end", NovelNodeKind.Ending, endingId: "done");
        private static NovelStory Story(params NovelNode[] nodes) => new("test-save", 1, "chapter", new[] {
            new NovelChapter("chapter", 1, nodes[0].Id, nodes, new[] { new NovelVariable("score", new NovelValue(2)) }) },
            new[] { new NovelVariable("name", new NovelValue("旅人")) });
        private static NovelCheckpoint Settings(NovelStory story, string node)
        {
            var target = NovelTestSaveBuilder.FindTargets(story, "chapter", node).Single();
            return NovelTestSaveBuilder.Defaults(story, target, "Config/Narrative/TestStory", NovelTestSaveBuilder.CallChains(target.Chapter, target.Node).First());
        }
        [TearDown]
        public void TearDown() { if (_script) UnityEngine.Object.DestroyImmediate(_script); TestWriteStep.ResetState(); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void AutomationJsonRoundTripFindsFreeSlotsAndRefusesOverwrite()
        {
            string root = Path.GetFullPath(Path.Combine(".utmp", "novel-test-save", Guid.NewGuid().ToString("N")));
            var request = new NovelTestSaveAutomation.Request {
                StoryAssetPath = "Assets/GameResource/Resources/Config/Narrative/LastLight/Story.asset",
                ChapterId = "last_light_ch01", NodeId = "last_light_common", OutputRoot = root, Slot = -1 };
            request.Variables.Add(new NovelTestSaveAutomation.VariableOverride { Scope = "Global", Id = "playerName", String = "测试玩家" });
            StringAssert.Contains("last_light_common", NovelTestSaveAutomation.Generate(JsonUtility.ToJson(request)));
            var store = new NovelSaveStore(root);
            Assert.IsTrue(store.Read(0, out var checkpoint, out var error), error);
            Assert.AreEqual("测试玩家", checkpoint.Globals.Single(v => v.Id == "playerName").Value.String);
            var before = File.ReadAllBytes(Path.Combine(root, "index.json"));
            request.Slot = 0;
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveAutomation.Generate(JsonUtility.ToJson(request)));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Path.Combine(root, "index.json")));
            request.Slot = -1; NovelTestSaveAutomation.Generate(JsonUtility.ToJson(request));
            Assert.AreEqual(1, new NovelSaveStore(root).Latest);
        }

        [Test]
        public void OrdinaryTargetUsesDefaultsAndRoundTripsThroughTheRealStore()
        {
            var story = Story(Talk("before", "target"), Talk("target", "end"), End());
            var settings = Settings(story, "target");
            settings.Locals[0] = new NovelVariable("score", new NovelValue(9));
            var checkpoint = NovelTestSaveBuilder.Build(story, Ready, settings);
            Assert.AreEqual("target", checkpoint.NodeId); Assert.AreEqual(NarrativeState.AwaitingAdvance, checkpoint.Stop);
            Assert.AreEqual("旅人", checkpoint.Globals[0].Value.String); Assert.IsEmpty(settings.History);
            var store = new NovelSaveStore(Path.Combine(".utmp", "novel-test-save", Guid.NewGuid().ToString("N")));
            Assert.IsTrue(store.Save(0, checkpoint, out var error), error);
            Assert.IsTrue(store.Read(0, out var read, out error), error);
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.TryRestore(story, Ready, read, out error), error);
            Assert.AreEqual(9, runner.Snapshot.Variables["score"].Int);
            Assert.IsTrue(runner.Advance(runner.Snapshot.SessionGeneration, runner.Snapshot.PositionVersion, 1));
            Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
            Assert.AreEqual(2, story.Chapters[0].Variables[0].Value.Int, "不能修改剧情默认值");
        }

        [Test]
        public void CustomStepFallsBackAndIsReachedOnlyAfterLoadingAndAdvancing()
        {
            TestWriteStep.ResetState(); _script = ScriptableObject.CreateInstance<TestWriteStep>();
            var source = Story(Talk("before", "game"), new NovelNode("game", NovelNodeKind.Dialogue, "end", commands: new[] {
                new NovelCommand("mini", NovelCommandKind.CustomStep, customStepId: _script.ScriptId), Say("after-game") }), End());
            var story = new NovelStory(source.Id, source.Revision, source.EntryChapterId, source.Chapters.ToArray(), source.Globals.ToArray(),
                customSteps: new Dictionary<string, NovelCustomStepSO> { [_script.ScriptId] = _script });
            var target = NovelTestSaveBuilder.FindTargets(story, "chapter", "game").Single();
            Assert.AreEqual("before", target.Node.Id); Assert.AreEqual(1, target.Distance);
            var settings = Settings(story, "game");
            var checkpoint = NovelTestSaveBuilder.Build(story, Ready, settings);
            Assert.AreEqual(0, TestWriteStep.Begins);
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.TryRestore(story, Ready, checkpoint, out var error), error);
            runner.Advance(runner.Snapshot.SessionGeneration, runner.Snapshot.PositionVersion, 1);
            Assert.AreEqual("game", runner.Snapshot.NodeId); Assert.AreEqual(NovelCommandKind.CustomStep, runner.CurrentCommand.Kind);
            settings.NodeId = "game";
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Build(story, Ready, settings));
        }

        [Test]
        public void MultiplePredecessorsRemainExplicitAndJumpLinksAreFollowed()
        {
            var story = Story(Talk("a", "j1"), Talk("b", "j2"),
                new NovelNode("j1", NovelNodeKind.Jump, linkId: "r"), new NovelNode("j2", NovelNodeKind.Jump, linkId: "r"),
                new NovelNode("r", NovelNodeKind.Receiver, "end"), End());
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, NovelTestSaveBuilder.FindTargets(story, "chapter", "r").Select(t => t.Node.Id));
        }

        [Test]
        public void NoOrdinaryPredecessorAndCyclesReturnNoCandidate()
        {
            var story = Story(new NovelNode("a", NovelNodeKind.Receiver, "b"), new NovelNode("b", NovelNodeKind.Jump, linkId: "a"));
            Assert.IsEmpty(NovelTestSaveBuilder.FindTargets(story, "chapter", "a"));
        }

        [Test]
        public void CrossChapterFallbackUsesTheActualPreviousChapter()
        {
            var story = new NovelStory("cross", 1, "one", new[] {
                new NovelChapter("one", 1, "talk", new[] { Talk("talk", "exit"), new NovelNode("exit", NovelNodeKind.ChapterExit) }),
                new NovelChapter("two", 1, "target", new[] { new NovelNode("target", NovelNodeKind.Receiver, "end"), End() }) },
                exits: new[] { new NovelChapterExit("one", "exit", "two") });
            var target = NovelTestSaveBuilder.FindTargets(story, "two", "target").Single();
            Assert.AreEqual("one", target.Chapter.Id); Assert.AreEqual("talk", target.Node.Id);
        }

        [Test]
        public void ChoiceOptionsAreRecomputedFromEditedVariables()
        {
            var condition = new NovelCondition(NovelJunction.All, new NovelPredicate("score", NovelComparison.Greater, new NovelValue(5)));
            var story = Story(new NovelNode("choice", NovelNodeKind.Choice, routes: new[] {
                new NovelRoute("high", "高分", "end", condition), new NovelRoute("always", "继续", "end") }), End());
            var settings = Settings(story, "choice");
            CollectionAssert.AreEqual(new[] { "always" }, NovelTestSaveBuilder.Build(story, Ready, settings).Options);
            settings.Locals[0] = new NovelVariable("score", new NovelValue(8));
            CollectionAssert.AreEqual(new[] { "high", "always" }, NovelTestSaveBuilder.Build(story, Ready, settings).Options);
        }

        [Test]
        public void NoLegalChoiceAndWrongVariableTypesAreRejected()
        {
            var condition = new NovelCondition(NovelJunction.All, new NovelPredicate("score", NovelComparison.Greater, new NovelValue(5)));
            var story = Story(new NovelNode("choice", NovelNodeKind.Choice, routes: new[] { new NovelRoute("high", "高分", "end", condition) }), End());
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Build(story, Ready, Settings(story, "choice")));
            story = Story(Talk("talk", "end"), End()); var settings = Settings(story, "talk");
            settings.Locals[0] = new NovelVariable("score", new NovelValue("bad"));
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Build(story, Ready, settings));
        }

        [Test]
        public void SelectedLineIsExactAndEarlierAssignmentsAreNotExecuted()
        {
            var story = Story(new NovelNode("talk", NovelNodeKind.Dialogue, "end", commands: new[] {
                new NovelCommand("set", NovelCommandKind.SetVariable, variableId: "score", value: new NovelValue(99)), Say("first"), Say("last") }), End());
            var checkpoint = NovelTestSaveBuilder.Build(story, Ready, Settings(story, "talk"), "last");
            Assert.AreEqual("last-line", checkpoint.LineId); Assert.AreEqual(2, checkpoint.Locals[0].Value.Int);
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Build(story, Ready, Settings(story, "talk"), "missing"));
        }

        [Test]
        public void StoryChangesInvalidatePendingSettings()
        {
            var story = Story(Talk("talk", "end"), End()); var settings = Settings(story, "talk");
            var changed = Story(Talk("talk", "new"), Talk("new", "end"), End());
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Build(changed, Ready, settings));
        }

        [Test]
        public void FlowSaveUsesChosenRealCallAndReturnsToItsResultTarget()
        {
            var story = Story(new NovelNode("a", NovelNodeKind.FlowCall, linkId: "f", routes: new[] { new NovelRoute("完成", "完成", "b") }),
                new NovelNode("b", NovelNodeKind.FlowCall, linkId: "f", routes: new[] { new NovelRoute("完成", "完成", "end") }),
                new NovelNode("f", NovelNodeKind.FlowStart, "body", scopeId: "f", flowVariables: new[] { new NovelVariable("x", new NovelValue(3)) }),
                Talk("body", "ret", "f"), new NovelNode("ret", NovelNodeKind.FlowReturn, scopeId: "f"), End());
            var target = NovelTestSaveBuilder.FindTargets(story, "chapter", "body").Single();
            var chains = NovelTestSaveBuilder.CallChains(target.Chapter, target.Node);
            Assert.AreEqual(2, chains.Count);
            var settings = NovelTestSaveBuilder.Defaults(story, target, "Test/Flow", new[] { "b" });
            settings.CallStack[0].Variables[0] = new NovelVariable("x", new NovelValue(7));
            var checkpoint = NovelTestSaveBuilder.Build(story, Ready, settings);
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.TryRestore(story, Ready, checkpoint, out var error), error);
            Assert.IsTrue(runner.TryGetVariable(NovelVariableScope.Flow, "x", out var value)); Assert.AreEqual(7, value.Int);
            runner.Advance(runner.Snapshot.SessionGeneration, runner.Snapshot.PositionVersion, 1);
            Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
            Assert.Throws<InvalidOperationException>(() => NovelTestSaveBuilder.Defaults(story, target, "Test/Flow", Array.Empty<string>()));
        }

        [Test]
        public void FlowReturnEdgesDoNotSkipTheBodyWhenFindingPredecessors()
        {
            var story = Story(Talk("before", "call"),
                new NovelNode("call", NovelNodeKind.FlowCall, linkId: "f", routes: new[] { new NovelRoute("完成", "完成", "end") }),
                new NovelNode("f", NovelNodeKind.FlowStart, "body", scopeId: "f"), Talk("body", "ret", "f"),
                new NovelNode("ret", NovelNodeKind.FlowReturn, scopeId: "f"), End());
            Assert.AreEqual("body", NovelTestSaveBuilder.FindTargets(story, "chapter", "end").Single().Node.Id);
        }
        #endregion
    }
}
