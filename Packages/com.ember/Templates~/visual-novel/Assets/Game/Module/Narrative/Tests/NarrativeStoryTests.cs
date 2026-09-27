using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeStoryTests
    {
        [TestCase("VisualNovel/LastLight/Story", "Config/Narrative/LastLight/Story")]
        public void LegacyCheckpointStoryPathLoadsMigratedStory(string oldPath, string newPath)
        {
            var request = new NovelNewGameRequest(oldPath, "chapter", "node");
            Assert.AreEqual(newPath, request.StoryPath);
            Assert.IsNotNull(UnityEngine.Resources.Load<NarrativeStorySO>(request.StoryPath));
            Assert.AreEqual("chapter", request.ChapterId);
            Assert.AreEqual("node", request.NodeId);
            Assert.AreEqual(newPath, new NovelNewGameRequest(newPath).StoryPath);
            Assert.AreEqual("Custom/Story", new NovelNewGameRequest("Custom/Story").StoryPath);
        }

        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        private static readonly Catalog Ready = new();
        private static NovelNode Say(string id, string next) => new(id, NovelNodeKind.Dialogue, next,
            commands: new[] { new NovelCommand(id + "_say", NovelCommandKind.Say, "台词", id + "_line") });
        private static NovelNode Exit(string id = "exit") => new(id, NovelNodeKind.ChapterExit);
        private static NovelNode End(string id = "end") => new(id, NovelNodeKind.Ending, endingId: id);
        private static NovelCondition Global(string id, NovelValue value, NovelComparison comparison = NovelComparison.Equal) =>
            new(NovelJunction.All, new NovelPredicate(id, comparison, value, NovelVariableScope.Global));
        private static void Advance(NarrativeRunner runner, long frame)
        { var s = runner.Snapshot; Assert.IsTrue(runner.Advance(s.SessionGeneration, s.PositionVersion, frame)); }

        [Test]
        public void ExplicitEntryStartsFreshWithoutReplayingEarlierAssignmentsOrChangingDefinition()
        {
            var chapter = new NovelChapter("chapter", 1, "before", new[] {
                new NovelNode("before", NovelNodeKind.Dialogue, "say", commands: new[] {
                    new NovelCommand("write", NovelCommandKind.SetVariable, variableId: "value", value: new NovelValue(9), scope: NovelVariableScope.Global) }),
                Say("say", "end"), End() }, new[] { new NovelVariable("local", new NovelValue(2)) });
            var story = new NovelStory("entry-story", 1, "chapter", new[] { chapter }, new[] { new NovelVariable("value", new NovelValue(1)) });
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(story, Ready, "chapter", "say"));
            Assert.AreEqual("say", runner.Snapshot.NodeId);
            Assert.AreEqual(1, runner.Snapshot.GlobalVariables["value"].Int);
            Assert.AreEqual(2, runner.Snapshot.Variables["local"].Int);
            Assert.AreEqual("before", chapter.EntryId);
            Advance(runner, 1);
            Assert.IsTrue(runner.TryCapture(out var checkpoint, out var error), error);
            var restored = new NarrativeRunner();
            Assert.IsTrue(restored.TryRestore(story, Ready, checkpoint, out error), error);
            Assert.AreEqual("say", restored.Snapshot.NodeId);
            Assert.AreEqual(1, restored.Snapshot.GlobalVariables["value"].Int);
            Assert.IsTrue(runner.StartStory(story, Ready));
            Assert.AreEqual(9, runner.Snapshot.GlobalVariables["value"].Int);
        }

        [TestCase("missing", "say")]
        [TestCase("chapter", "missing")]
        public void ExplicitEntryRejectsUnknownChapterOrNode(string chapterId, string nodeId)
        {
            var story = new NovelStory("entry-story", 1, "chapter", new[] {
                new NovelChapter("chapter", 1, "say", new[] { Say("say", "end"), End() }) });
            var runner = new NarrativeRunner();
            Assert.IsFalse(runner.StartStory(story, Ready, chapterId, nodeId));
            Assert.AreEqual(NarrativeState.Faulted, runner.Snapshot.State);
            Assert.AreEqual("BadEntry", runner.Snapshot.Error.Code);
            Assert.AreEqual(chapterId, runner.Snapshot.Error.ChapterId);
            Assert.AreEqual(nodeId, runner.Snapshot.Error.NodeId);
        }

        [TestCase(NovelValueType.Bool)]
        [TestCase(NovelValueType.Int)]
        [TestCase(NovelValueType.String)]
        public void GlobalAssignmentSelectsFirstMatchingChapterAndPreservesDefinition(NovelValueType type)
        {
            var value = type == NovelValueType.Bool ? new NovelValue(true) : type == NovelValueType.Int ? new NovelValue(2) : new NovelValue("friend");
            var initial = type == NovelValueType.Bool ? new NovelValue(false) : type == NovelValueType.Int ? new NovelValue(0) : new NovelValue("");
            var a = new NovelChapter("a", 1, "start", new[] {
                new NovelNode("start", NovelNodeKind.Dialogue, "exit", commands: new[] {
                    new NovelCommand("set", NovelCommandKind.SetVariable, variableId: "route", value: value, scope: NovelVariableScope.Global) }), Exit() });
            var b = new NovelChapter("b", 1, "say", new[] { Say("say", "end"), End() });
            var c = new NovelChapter("c", 1, "end", new[] { End() });
            var story = new NovelStory("story", 1, "a", new[] { a, b, c }, new[] { new NovelVariable("route", initial) },
                new[] { new NovelChapterExit("a", "exit", "c", new[] {
                    new NovelRoute("first", "", "b", Global("route", value)), new NovelRoute("second", "", "c", Global("route", value)) }) });
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            Assert.AreEqual("b", runner.Snapshot.ChapterId); Assert.AreEqual("story", runner.Snapshot.StoryId);
            Assert.AreEqual(value.ToString(), runner.Snapshot.GlobalVariables["route"].ToString());
            Assert.AreEqual(initial.ToString(), story.Globals[0].Value.ToString());
            var old = runner.Snapshot; Advance(runner, 1); Advance(runner, 2);
            Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
            Assert.IsFalse(runner.CompleteReveal(old.SessionGeneration, old.PositionVersion));
        }

        [Test]
        public void ReenterChapterResetsLocalsButNotGlobalsAndNewGameResetsBoth()
        {
            var a = new NovelChapter("a", 1, "say", new[] { Say("say", "set"),
                new NovelNode("set", NovelNodeKind.Dialogue, "exit", commands: new[] {
                    new NovelCommand("local", NovelCommandKind.SetVariable, variableId: "same", value: new NovelValue(5)),
                    new NovelCommand("global", NovelCommandKind.SetVariable, variableId: "same", value: new NovelValue(9), scope: NovelVariableScope.Global) }), Exit() },
                new[] { new NovelVariable("same", new NovelValue(0)) });
            var b = new NovelChapter("b", 1, "say", new[] { Say("say", "exit"), Exit() }, new[] { new NovelVariable("same", new NovelValue(3)) });
            var story = new NovelStory("story", 1, "a", new[] { a, b }, new[] { new NovelVariable("same", new NovelValue(1)) },
                new[] { new NovelChapterExit("a", "exit", "b"), new NovelChapterExit("b", "exit", "a") });
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            Advance(runner, 1); var old = runner.Snapshot; Advance(runner, 2);
            Assert.AreEqual("b", runner.Snapshot.ChapterId); Assert.AreEqual(3, runner.Snapshot.Variables["same"].Int);
            Assert.AreEqual(9, runner.Snapshot.GlobalVariables["same"].Int);
            Assert.IsFalse(runner.CompleteReveal(old.SessionGeneration, old.PositionVersion));
            Advance(runner, 3); Advance(runner, 4);
            Assert.AreEqual("a", runner.Snapshot.ChapterId); Assert.AreEqual(0, runner.Snapshot.Variables["same"].Int);
            Assert.AreEqual(9, runner.Snapshot.GlobalVariables["same"].Int);
            runner.StartStory(story, Ready); Assert.AreEqual(1, runner.Snapshot.GlobalVariables["same"].Int);
        }

        [Test]
        public void CrossChapterImmediateCycleSharesStepLimit()
        {
            var a = new NovelChapter("a", 1, "exit", new[] { Exit() });
            var b = new NovelChapter("b", 1, "exit", new[] { Exit() });
            var runner = new NarrativeRunner(12);
            Assert.IsFalse(runner.StartStory(new NovelStory("story", 1, "a", new[] { a, b }, exits: new[] {
                new NovelChapterExit("a", "exit", "b"), new NovelChapterExit("b", "exit", "a") }), Ready));
            Assert.AreEqual("StepLimit", runner.Snapshot.Error.Code);
        }

        [Test]
        public void MissingFallbackDuplicateExitAndLocalTransitionConditionsAreRejected()
        {
            var a = new NovelChapter("a", 1, "exit", new[] { Exit() }, new[] { new NovelVariable("trust", new NovelValue(0)) });
            var b = new NovelChapter("b", 1, "end", new[] { End() });
            var invalid = new NovelChapterExit("a", "exit", null, new[] { new NovelRoute("r", "", "b",
                new NovelCondition(NovelJunction.All, new NovelPredicate("trust", NovelComparison.Equal, new NovelValue(0)))) });
            var story = new NovelStory("story", 1, "a", new[] { a, b }, exits: new[] { invalid, invalid });
            var errors = NarrativeStoryValidator.Validate(story, Ready);
            Assert.IsTrue(errors.Any(e => e.Message.Contains("兜底")));
            Assert.IsTrue(errors.Any(e => e.Message.Contains("重复")));
            Assert.IsTrue(errors.Any(e => e.Message.Contains("全局")));
            Assert.IsFalse(new NarrativeRunner().Start(a, Ready), "含出口的章节不能被静默当成独立剧情运行");
        }

        [Test]
        public void NoMatchingConditionUsesFallbackAndEndingDoesNotTransition()
        {
            var a = new NovelChapter("a", 1, "exit", new[] { Exit() });
            var b = new NovelChapter("b", 1, "end", new[] { End() });
            var story = new NovelStory("story", 1, "a", new[] { a, b }, new[] { new NovelVariable("trust", new NovelValue(0)) },
                new[] { new NovelChapterExit("a", "exit", "b", new[] { new NovelRoute("r", "", "a", Global("trust", new NovelValue(1))) }) });
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            Assert.AreEqual("b", runner.Snapshot.ChapterId); Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
        }

        [TestCase(0, "last_light_heard")]
        [TestCase(1, "last_light_tomorrow")]
        public void DeliverySampleHasTwoReachableEndingsWithoutAssetWrites(int option, string ending)
        {
            var asset = UnityEngine.Resources.Load<NarrativeStorySO>("Config/Narrative/LastLight/Story"); Assert.IsNotNull(asset);
            var assets = new List<UnityEngine.Object> { asset };
            foreach (var c in asset.Chapters) { assets.Add(c); assets.AddRange(c.Nodes.Cast<UnityEngine.Object>()); }
            var before = assets.ToDictionary(a => a, UnityEngine.JsonUtility.ToJson);
            Assert.IsTrue(asset.TryReadDefinition(Ready, out var definition, out var errors), string.Join("\n", errors));
            Assert.AreEqual(2, definition.Chapters.SelectMany(c => c.Nodes).Count(n => n.Kind == NovelNodeKind.Ending));
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(definition, Ready));
            for (int frame = 0; frame < 500 && runner.Snapshot.HasActiveSession; frame++)
            {
                var s = runner.Snapshot;
                string position = $"{s.ChapterId}/{s.NodeId}/{s.CommandId} state={s.State} wait={s.Wait}";
                bool accepted;
                if (s.State == NarrativeState.AwaitingChoice)
                    accepted = runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[option].Id, frame);
                // 测试宿主负责推进空镜等 Wait；Advance 不会消耗计时等待。
                else if (s.Wait == NarrativeWait.Timer) accepted = runner.Tick(s.SessionGeneration, 1, frame);
                else if ((s.Wait & NarrativeWait.Presentation) != 0)
                    accepted = runner.CompletePresentation(s.SessionGeneration, s.PositionVersion);
                else accepted = runner.Advance(s.SessionGeneration, s.PositionVersion, frame);
                Assert.IsTrue(accepted, "测试驱动未被接受：" + position);
            }
            var final = runner.Snapshot;
            Assert.AreEqual(NarrativeState.Ended, final.State,
                $"未到结局：{final.ChapterId}/{final.NodeId}/{final.CommandId} wait={final.Wait} error={final.Error}");
            Assert.AreEqual(ending, runner.Snapshot.EndingId);
            foreach (var pair in before) Assert.AreEqual(pair.Value, UnityEngine.JsonUtility.ToJson(pair.Key));
        }

        [TestCase(true, true, "good")]
        [TestCase(true, false, "quiet")]
        [TestCase(false, false, "return_home")]
        public void RealStoryAssetsRunAllThreeRoutesWithoutWrites(bool school, bool ask, string ending)
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<NarrativeStorySO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset"); Assert.IsNotNull(asset);
            var assets = new List<UnityEngine.Object> { asset };
            foreach (var c in asset.Chapters) { assets.Add(c); assets.AddRange(c.Nodes.Cast<UnityEngine.Object>()); }
            var before = assets.ToDictionary(a => a, UnityEngine.JsonUtility.ToJson);
            Assert.IsTrue(asset.TryReadDefinition(Ready, out var definition, out var errors), string.Join("\n", errors));
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(definition, Ready));
            for (int frame = 0; frame < 100 && runner.Snapshot.HasActiveSession; frame++)
            {
                var s = runner.Snapshot;
                if (s.State == NarrativeState.AwaitingChoice)
                {
                    bool first = s.ChapterId == asset.Entry.ChapterId ? school : ask;
                    runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[first ? 0 : s.Options.Count - 1].Id, frame);
                }
                else if (s.Wait == NarrativeWait.Timer) runner.Tick(s.SessionGeneration, 1, frame);
                else if ((s.Wait & NarrativeWait.Presentation) != 0) runner.CompletePresentation(s.SessionGeneration, s.PositionVersion);
                else runner.Advance(s.SessionGeneration, s.PositionVersion, frame);
            }
            Assert.AreEqual(ending, runner.Snapshot.EndingId);
            foreach (var pair in before) Assert.AreEqual(pair.Value, UnityEngine.JsonUtility.ToJson(pair.Key));
        }
    }
}
