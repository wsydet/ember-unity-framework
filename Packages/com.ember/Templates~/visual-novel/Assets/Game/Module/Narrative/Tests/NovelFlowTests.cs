using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed class NovelFlowTests
    {
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => false;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = null; return false; }
        }
        private static readonly Catalog Ready = new();
        private static NovelCommand Say(string id) => new(id, NovelCommandKind.Say, "暂停", id + "-line");
        private static NovelNode Call(string id, string flow, string target, string scope = "", string result = "完成")
            => new(id, NovelNodeKind.FlowCall, scopeId: scope, linkId: flow, routes: new[] { new NovelRoute(result, result, target) });
        private static NovelNode Begin(string id, string target, int initial = 1)
            => new(id, NovelNodeKind.FlowStart, target, scopeId: id, flowVariables: new[] { new NovelVariable("x", new NovelValue(initial)) });
        private static NovelNode End(string id, string flow, string result = "完成") => new(id, NovelNodeKind.FlowReturn, scopeId: flow, result: result);
        private static NovelNode Exit() => new("end", NovelNodeKind.Ending, endingId: "end");
        private static NovelStory Story(params NovelNode[] nodes) => new("flow-test", 1, "chapter", new[] { new NovelChapter("chapter", 1, nodes[0].Id, nodes) });
        private static void Advance(NarrativeRunner r, int frame)
        {
            var s = r.Snapshot; r.CompleteReveal(s.SessionGeneration, s.PositionVersion);
            s = r.Snapshot; Assert.IsTrue(r.Advance(s.SessionGeneration, s.PositionVersion, frame));
        }

        [Test]
        public void SharedFlowReturnsToEachCallAndAllocatesFreshLocals()
        {
            var story = Story(Call("a", "f", "b"), Call("b", "f", "end"), Begin("f", "body"),
                new NovelNode("body", NovelNodeKind.Dialogue, "return", commands: new[] { Say("say") }, scopeId: "f"), End("return", "f"), Exit());
            var r = new NarrativeRunner(); Assert.IsTrue(r.StartStory(story, Ready), r.Snapshot.Error?.ToString());
            Assert.AreEqual("a", r.Snapshot.CallPath.Single());
            Assert.IsTrue(r.TrySetVariable(NovelVariableScope.Flow, "x", new NovelValue(9), out _));
            Advance(r, 1);
            Assert.AreEqual("b", r.Snapshot.CallPath.Single());
            Assert.AreEqual(1, r.Snapshot.FlowVariables["x"].Int);
            Advance(r, 2); Assert.AreEqual(NarrativeState.Ended, r.Snapshot.State);
            Assert.IsEmpty(r.Snapshot.CallPath); Assert.IsEmpty(r.Snapshot.FlowVariables);
        }

        [Test]
        public void NestedNamedReturnAndSavePreserveCallerLocalAndRandomState()
        {
            var story = Story(Call("main", "outer", "end"), Begin("outer", "innerCall", 7),
                Call("innerCall", "inner", "after", "outer", "成功"), Begin("inner", "roll", 2),
                new NovelNode("roll", NovelNodeKind.Dialogue, "returnInner", commands: new[] {
                    new NovelCommand("rng", NovelCommandKind.RandomVariable, variableId: "x", scope: NovelVariableScope.Flow, randomMin: 1, randomMax: 99), Say("innerSay") }, scopeId: "inner"),
                End("returnInner", "inner", "成功"), new NovelNode("after", NovelNodeKind.Dialogue, "returnOuter", commands: new[] { Say("outerSay") }, scopeId: "outer"),
                End("returnOuter", "outer"), Exit());
            var r = new NarrativeRunner(randomSeed: 19); Assert.IsTrue(r.StartStory(story, Ready), r.Snapshot.Error?.ToString());
            var s = r.Snapshot; r.CompleteReveal(s.SessionGeneration, s.PositionVersion);
            Assert.IsTrue(r.TryCapture(out var saved, out var error), error);
            saved = JsonUtility.FromJson<NovelCheckpoint>(JsonUtility.ToJson(saved));
            Assert.AreEqual(2, saved.CallStack.Count);
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, saved, out error), error);
            Assert.IsTrue(restored.TryCapture(out var roundtrip, out error), error);
            Assert.AreEqual(saved.RandomState, roundtrip.RandomState);
            Advance(restored, 1); Assert.AreEqual("after", restored.Snapshot.NodeId);
            Assert.AreEqual(7, restored.Snapshot.FlowVariables["x"].Int);
            Advance(restored, 2); Assert.AreEqual(NarrativeState.Ended, restored.Snapshot.State);
        }

        [Test]
        public void TimerCheckpointResumesRemainingTimeAndReturnsOnce()
        {
            var story = Story(Call("main", "f", "end"), Begin("f", "timer"),
                new NovelNode("timer", NovelNodeKind.Dialogue, "return", commands: new[] { new NovelCommand("wait", NovelCommandKind.Wait, duration: 10) }, scopeId: "f"), End("return", "f"), Exit());
            var r = new NarrativeRunner(); Assert.IsTrue(r.StartStory(story, Ready));
            r.Tick(r.Snapshot.SessionGeneration, 3, 1);
            Assert.IsTrue(r.TryCapture(out var save, out var error), error); Assert.AreEqual(7, save.RemainingWait);
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, save, out error), error);
            Assert.IsFalse(restored.Tick(r.Snapshot.SessionGeneration, 8, 2));
            Assert.IsTrue(restored.Tick(restored.Snapshot.SessionGeneration, 6, 2)); Assert.AreEqual(NarrativeWait.Timer, restored.Snapshot.Wait);
            Assert.IsTrue(restored.Tick(restored.Snapshot.SessionGeneration, 1, 3)); Assert.AreEqual(NarrativeState.Ended, restored.Snapshot.State);
            Assert.IsFalse(restored.Tick(restored.Snapshot.SessionGeneration, 1, 3));
        }

        [Test]
        public void SharedReceiverAndDuplicateInputKeepPlayerDrivenLoop()
        {
            var story = Story(new NovelNode("receiver", NovelNodeKind.Receiver, "choice"),
                new NovelNode("choice", NovelNodeKind.Choice, routes: new[] { new NovelRoute("a", "A", "ja"), new NovelRoute("b", "B", "jb") }),
                new NovelNode("ja", NovelNodeKind.Jump, linkId: "receiver"), new NovelNode("jb", NovelNodeKind.Jump, linkId: "receiver"));
            var r = new NarrativeRunner(); Assert.IsTrue(r.StartStory(story, Ready), r.Snapshot.Error?.ToString());
            var s = r.Snapshot; Assert.IsTrue(r.Choose(s.SessionGeneration, s.PositionVersion, "a", 1));
            Assert.IsFalse(r.Choose(s.SessionGeneration, s.PositionVersion, "a", 2));
            s = r.Snapshot; Assert.IsTrue(r.Choose(s.SessionGeneration, s.PositionVersion, "b", 3));
            Assert.AreEqual("choice", r.Snapshot.NodeId);
        }

        [Test]
        public void MissingReceiverImmediateCycleAndRecursiveCallReportLocation()
        {
            var stories = new[] {
                Story(new NovelNode("jump", NovelNodeKind.Jump, linkId: "missing")),
                Story(new NovelNode("receiver", NovelNodeKind.Receiver, "jump"), new NovelNode("jump", NovelNodeKind.Jump, linkId: "receiver")),
                Story(Call("main", "f", "end"), Begin("f", "self"), Call("self", "f", "return", "f"), End("return", "f"), Exit()) };
            var codes = new[] { "BadTarget", "ImmediateCycle", "RecursiveFlow" };
            for (int i = 0; i < stories.Length; i++)
            {
                var issues = NarrativeStoryValidator.Validate(stories[i], Ready);
                Assert.IsTrue(issues.Any(e => e.Code == codes[i] && e.ChapterId == "chapter" && !string.IsNullOrEmpty(e.NodeId)), string.Join("\n", issues));
            }
        }

        [Test]
        public void JumpCannotEscapeCallStackAndReturnAddressCannotBeTampered()
        {
            var bad = Story(Call("main", "f", "receiver"), Begin("f", "escape"),
                new NovelNode("escape", NovelNodeKind.Jump, scopeId: "f", linkId: "receiver"), End("ret", "f"),
                new NovelNode("receiver", NovelNodeKind.Receiver, "end"), Exit());
            Assert.IsTrue(NarrativeStoryValidator.Validate(bad, Ready).Any(e => e.Code == "IllegalScope"));
            var story = Story(Call("main", "f", "end"), Begin("f", "say"),
                new NovelNode("say", NovelNodeKind.Dialogue, "ret", commands: new[] { Say("text") }, scopeId: "f"), End("ret", "f"), Exit());
            var r = new NarrativeRunner(); Assert.IsTrue(r.StartStory(story, Ready));
            r.CompleteReveal(r.Snapshot.SessionGeneration, r.Snapshot.PositionVersion); Assert.IsTrue(r.TryCapture(out var save, out _));
            save.CallStack[0].Returns[0].NodeId = "say";
            Assert.IsFalse(new NarrativeRunner().TryRestore(story, Ready, save, out var error)); StringAssert.Contains("main", error);
        }

        [Test]
        public void LatePresentationCallbackCannotReturnTwice()
        {
            var story = Story(Call("a", "f", "b"), Call("b", "f", "end"), Begin("f", "present"),
                new NovelNode("present", NovelNodeKind.Dialogue, "return", commands: new[] {
                    new NovelCommand("present", NovelCommandKind.DialogueVisibility) }, scopeId: "f"), End("return", "f"), Exit());
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready), runner.Snapshot.Error?.ToString());
            var first = runner.Snapshot;
            Assert.IsTrue(runner.CompletePresentation(first.SessionGeneration, first.PositionVersion));
            Assert.AreEqual("b", runner.Snapshot.CallPath.Single());
            Assert.IsFalse(runner.CompletePresentation(first.SessionGeneration, first.PositionVersion));
            var second = runner.Snapshot;
            Assert.IsTrue(runner.CompletePresentation(second.SessionGeneration, second.PositionVersion));
            Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
            Assert.IsFalse(runner.CompletePresentation(second.SessionGeneration, second.PositionVersion));
        }

        [Test]
        public void ChoiceCheckpointRestoresLocalConditionAndSettlesOnlyOnce()
        {
            var gate = new NovelCondition(NovelJunction.All, new NovelPredicate("x", NovelComparison.Equal, new NovelValue(9), NovelVariableScope.Flow));
            var story = Story(Call("main", "f", "end", result: "取消"), Begin("f", "set"),
                new NovelNode("set", NovelNodeKind.Dialogue, "choice", commands: new[] {
                    new NovelCommand("set", NovelCommandKind.SetVariable, variableId: "x", value: new NovelValue(9), scope: NovelVariableScope.Flow) }, scopeId: "f"),
                new NovelNode("choice", NovelNodeKind.Choice, routes: new[] { new NovelRoute("cancel", "取消", "return", gate) }, scopeId: "f"),
                End("return", "f", "取消"), Exit());
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready), runner.Snapshot.Error?.ToString());
            Assert.IsTrue(runner.TryCapture(out var save, out var error), error);
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, JsonUtility.FromJson<NovelCheckpoint>(JsonUtility.ToJson(save)), out error), error);
            Assert.AreEqual(9, restored.Snapshot.FlowVariables["x"].Int);
            var s = restored.Snapshot; Assert.IsTrue(restored.Choose(s.SessionGeneration, s.PositionVersion, "cancel", 1));
            Assert.AreEqual(NarrativeState.Ended, restored.Snapshot.State);
            Assert.IsFalse(restored.Choose(s.SessionGeneration, s.PositionVersion, "cancel", 2));
        }

        [Test]
        public void SchemaEightRandomStoryRemainsRestorable()
        {
            var story = Story(new NovelNode("say", NovelNodeKind.Dialogue, "end", commands: new[] { Say("text") }), Exit());
            var r = new NarrativeRunner(randomSeed: 13); Assert.IsTrue(r.StartStory(story, Ready));
            r.CompleteReveal(r.Snapshot.SessionGeneration, r.Snapshot.PositionVersion); Assert.IsTrue(r.TryCapture(out var save, out _));
            save.SchemaVersion = 8; save.CallStack = null;
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, save, out var error), error);
            Assert.IsTrue(restored.TryCapture(out var next, out error), error); Assert.AreEqual(13, next.RandomState);
        }
    }
}
