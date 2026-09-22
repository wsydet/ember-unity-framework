using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.TestTools;
using System.IO;
using System.Linq;
using Game.NovelSave;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed class NovelCheckpointTests
    {
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        private static readonly Catalog Ready = new();
        [UnityTest]
        public IEnumerator CancelledModuleReadNotifiesOnceAndCannotCommitLater()
        {
            var store = new NovelSaveStore(Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N")));
            Assert.IsTrue(store.Save(0, Capture(AtSay(Story())), out _));
            var module = new NovelSaveModule();
            typeof(NovelSaveModule).GetProperty(nameof(module.Store)).SetValue(module, store);
            int cancelled = 0, committed = 0, failed = 0;
            Assert.IsTrue(module.BeginLoad(0, () => null, _ => committed++, _ => failed++, () => cancelled++));
            module.CancelLoad(); module.CancelLoad();
            for (int frame = 0; frame < 5; frame++) { yield return null; module.Update(); }
            Assert.AreEqual(1, cancelled); Assert.AreEqual(0, committed); Assert.AreEqual(0, failed);
            Assert.IsFalse(module.IsBusy);
        }
        private IEnumerator Await(Task task)
        {
            float deadline = Time.realtimeSinceStartup + 10;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(task.IsCompleted, "后台存档操作未在 10 秒内结束");
            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
        }

        [UnityTest]
        public IEnumerator AsyncWriteDoesNotPublishSuccessBeforeCommitAndRejectsConcurrentWriter()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N"));
            var store = new NovelSaveStore(root); var checkpoint = Capture(AtSay(Story()));
            using var reached = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
            store.BeforeCommit = path =>
            {
                if (Path.GetFileName(path) != "index.json") return;
                reached.Set(); if (!release.Wait(5000)) throw new IOException("test release timed out");
            };
            var saving = store.SaveAsync(0, checkpoint);
            try
            {
                float deadline = Time.realtimeSinceStartup + 4;
                while (!reached.IsSet && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(reached.IsSet); Assert.IsFalse(saving.IsCompleted);
                Assert.AreEqual(-1, store.Latest); Assert.IsEmpty(store.Slots);
                Assert.IsNotNull(store.SaveAsync(6, checkpoint).Result, "并行覆盖必须拒绝");
            }
            finally { release.Set(); }
            yield return Await(saving); Assert.IsNull(saving.Result); Assert.AreEqual(0, store.Latest);
            var reading = store.ReadAsync(0); yield return Await(reading);
            Assert.AreEqual(checkpoint.CommandId, reading.Result.CommandId);
            Assert.AreEqual(0, new NovelSaveStore(root).Latest);
        }
        [UnityTest]
        public IEnumerator AsyncCommitFailureRetainsPreviousLatestAndAccountQueueSnapshotsInOrder()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N"));
            var store = new NovelSaveStore(root); var checkpoint = Capture(AtSay(Story()));
            Assert.IsTrue(store.Save(0, checkpoint, out _));
            store.BeforeCommit = path => { if (Path.GetFileName(path) == "index.json") throw new IOException("injected async failure"); };
            var failure = store.SaveAsync(6, checkpoint); yield return Await(failure);
            StringAssert.Contains("injected async failure", failure.Result);
            Assert.AreEqual(0, store.Latest); Assert.AreEqual(0, new NovelSaveStore(root).Latest);
            store.BeforeCommit = null;
            var account = new NovelAccountData { TextSpeed = 40 }; var first = store.SaveAccountAsync(account);
            account.TextSpeed = 60; account.ReadLines.Add("latest"); var second = store.SaveAccountAsync(account);
            account.TextSpeed = 80; account.ReadLines.Clear();
            yield return Await(second); Assert.IsNull(first.Result); Assert.IsNull(second.Result);
            Assert.IsTrue(store.ReadAccount(out var loaded, out _)); Assert.AreEqual(60, loaded.TextSpeed);
            CollectionAssert.AreEqual(new[] { "latest" }, loaded.ReadLines);
        }
        private NovelStory Story(string text = "旧文字", bool reverse = false, bool changedTarget = false, bool changedType = false, bool reorderedCommands = false)
        {
            var commands = new List<NovelCommand>
            {
                new("set", NovelCommandKind.SetVariable, variableId: "local", value: new NovelValue(9)),
                new("sfx", NovelCommandKind.SFX, resourceKey: "effect"),
                new("say", NovelCommandKind.Say, text, "line", textRevision: text == "旧文字" ? 1 : 2),
                new("next", NovelCommandKind.Say, "后一句", "line-next")
            };
            if (reorderedCommands) (commands[2], commands[3]) = (commands[3], commands[2]);
            var routes = new List<NovelRoute> { new("yes", "是", changedTarget ? "end" : "branch"), new("no", "否", "end") };
            if (reverse) routes.Reverse();
            var nodes = new List<NovelNode>
            {
                new("talk", NovelNodeKind.Dialogue, "choice", commands: commands),
                new("choice", NovelNodeKind.Choice, routes: routes),
                new("branch", NovelNodeKind.Dialogue, "end", commands: new[] {
                    new NovelCommand("global", NovelCommandKind.SetVariable, variableId: "global", value: new NovelValue(7), scope: NovelVariableScope.Global),
                    new NovelCommand("branch-say", NovelCommandKind.Say, "分支", "branch-line") }),
                new("end", NovelNodeKind.Ending, endingId: "done")
            };
            if (reverse) nodes.Reverse();
            return new NovelStory("story", text == "旧文字" ? 1 : 9, "chapter", new[] {
                new NovelChapter("chapter", 1, "talk", nodes, new[] {new NovelVariable("local", new NovelValue(0))}) },
                new[] { new NovelVariable("global", changedType ? new NovelValue(false) : new NovelValue(1)) });
        }
        private NarrativeRunner AtSay(NovelStory story)
        {
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready));
            var s = runner.Snapshot; Assert.IsTrue(runner.CompletePresentation(s.SessionGeneration, s.PositionVersion));
            s = runner.Snapshot; Assert.IsTrue(runner.CompleteReveal(s.SessionGeneration, s.PositionVersion)); return runner;
        }
        private NovelCheckpoint Capture(NarrativeRunner runner)
        {
            Assert.IsTrue(runner.TryCapture(out var save, out var error), error); save.StoryPath = "Tests/CheckpointStory"; return save;
        }
        private void Advance(NarrativeRunner runner, int frame)
        { var s = runner.Snapshot; Assert.IsTrue(runner.Advance(s.SessionGeneration, s.PositionVersion, frame)); }

        [Test]
        public void DialogueRestoreUsesSavedLocalsWithoutReplayingAssignmentsOrSfx()
        {
            var definition = Story(); var source = AtSay(definition); var save = Capture(source);
            save.Locals[0] = new NovelVariable("local", new NovelValue(42));
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(definition, Ready, save, out var error), error);
            Assert.AreEqual(42, restored.Snapshot.Variables["local"].Int);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, restored.Snapshot.State);
            Assert.AreEqual("say", restored.Snapshot.CommandId);
            Assert.AreNotEqual(source.Snapshot.SessionGeneration, restored.Snapshot.SessionGeneration);
            Assert.IsFalse(restored.Advance(source.Snapshot.SessionGeneration, source.Snapshot.PositionVersion, 1));
            Advance(restored, 1); Assert.AreEqual("next", restored.Snapshot.CommandId);
        }
        [Test]
        public void RevisedTextAndUnorderedListsRemainCompatible()
        {
            var save = Capture(AtSay(Story())); var restored = new NarrativeRunner();
            Assert.IsTrue(restored.TryRestore(Story("润色后的文字", true), Ready, save, out var error), error);
            Assert.AreEqual("润色后的文字", restored.CurrentCommand.Text); Assert.AreEqual(2, restored.CurrentCommand.TextRevision);
        }
        [TestCase("target")]
        [TestCase("type")]
        [TestCase("executionOrder")]
        public void SemanticChangesRejectOldCheckpoint(string change)
        {
            var save = Capture(AtSay(Story())); var runner = new NarrativeRunner();
            Assert.IsFalse(runner.TryRestore(Story(changedTarget: change == "target", changedType: change == "type", reorderedCommands: change == "executionOrder"), Ready, save, out _));
            Assert.AreEqual(NarrativeState.Idle, runner.Snapshot.State);
        }
        [TestCase("schema")]
        [TestCase("command")]
        [TestCase("node")]
        [TestCase("locals")]
        [TestCase("stop")]
        public void MalformedCheckpointRejectedWithoutStartingRunner(string change)
        {
            var save = Capture(AtSay(Story()));
            switch(change)
            {
                case "schema": save.SchemaVersion = 99; break;
                case "command": save.CommandId = "deleted"; break;
                case "node": save.NodeId = "deleted"; break;
                case "locals": save.Locals.Clear(); break;
                case "stop": save.Stop = NarrativeState.Executing; break;
            }
            var runner = new NarrativeRunner(); Assert.IsFalse(runner.TryRestore(Story(), Ready, save, out _));
            Assert.AreEqual(NarrativeState.Idle, runner.Snapshot.State);
        }
        [Test]
        public void ChoiceAndBranchStopsRoundTripAndKeepScopedValues()
        {
            var story = Story(); var runner = AtSay(story); Advance(runner, 1); Advance(runner, 2); Advance(runner, 3);
            var choice = Capture(runner); Assert.AreEqual(NarrativeState.AwaitingChoice, choice.Stop);
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(Story(reverse: true), Ready, choice, out var error), error);
            CollectionAssert.AreEquivalent(new[] { "yes", "no" }, restored.Snapshot.Options.Select(o => o.Id));
            var s = restored.Snapshot; restored.Choose(s.SessionGeneration, s.PositionVersion, "yes", 4); Advance(restored, 5);
            var branch = Capture(restored); var next = new NarrativeRunner(); Assert.IsTrue(next.TryRestore(story, Ready, branch, out error), error);
            Assert.AreEqual(9, next.Snapshot.Variables["local"].Int); Assert.AreEqual(7, next.Snapshot.GlobalVariables["global"].Int);
        }
        [Test]
        public void NonStablePointsCannotBeCaptured()
        {
            var runner = new NarrativeRunner(); runner.StartStory(Story(), Ready);
            Assert.IsFalse(runner.TryCapture(out _, out _)); var s = runner.Snapshot;
            runner.CompletePresentation(s.SessionGeneration, s.PositionVersion); Assert.IsFalse(runner.TryCapture(out _, out _));
        }
        [Test]
        public void AutoSaveTriggerChangesOnlyOnChapterEntryOrAcceptedChoice()
        {
            var runner = AtSay(Story()); Assert.AreEqual(1, runner.AutoSaveRevision);
            Advance(runner, 1); Advance(runner, 2); Advance(runner, 3); Assert.AreEqual(1, runner.AutoSaveRevision);
            var s = runner.Snapshot; Assert.IsFalse(runner.Choose(s.SessionGeneration, s.PositionVersion, "bad", 4));
            Assert.AreEqual(1, runner.AutoSaveRevision);
            Assert.IsTrue(runner.Choose(s.SessionGeneration, s.PositionVersion, "yes", 4)); Assert.AreEqual(2, runner.AutoSaveRevision);
            Assert.IsFalse(runner.Choose(s.SessionGeneration, s.PositionVersion, "yes", 4)); Assert.AreEqual(2, runner.AutoSaveRevision);
        }
        [Test]
        public void CrossChapterRestoreRetainsGlobalsAndSavedChapterLocals()
        {
            var a = new NovelChapter("a", 1, "exit", new[] { new NovelNode("exit", NovelNodeKind.ChapterExit) });
            var b = new NovelChapter("b", 1, "talk", new[] {
                new NovelNode("talk", NovelNodeKind.Dialogue, "end", commands: new[] {
                    new NovelCommand("say", NovelCommandKind.Say, "第二章", "line") }),
                new NovelNode("end", NovelNodeKind.Ending, endingId: "done") }, new[] { new NovelVariable("x", new NovelValue(0)) });
            var story = new NovelStory("multi", 1, "a", new[] { a, b }, new[] { new NovelVariable("x", new NovelValue(1)) },
                new[] { new NovelChapterExit("a", "exit", "b") });
            var runner = new NarrativeRunner(); Assert.IsTrue(runner.StartStory(story, Ready)); Advance(runner, 1);
            Assert.AreEqual(2, runner.AutoSaveRevision); var save = Capture(runner);
            save.Locals[0] = new NovelVariable("x", new NovelValue(20)); save.Globals[0] = new NovelVariable("x", new NovelValue(30));
            var restored = new NarrativeRunner(); Assert.IsTrue(restored.TryRestore(story, Ready, save, out var error), error);
            Assert.AreEqual("b", restored.Snapshot.ChapterId); Assert.AreEqual(20, restored.Snapshot.Variables["x"].Int);
            Assert.AreEqual(30, restored.Snapshot.GlobalVariables["x"].Int); Assert.AreEqual(0, restored.AutoSaveRevision);
        }
        [Test]
        public void ChoiceLegalSetTamperingIsRejected()
        {
            var runner = AtSay(Story()); Advance(runner, 1); Advance(runner, 2); Advance(runner, 3);
            var save = Capture(runner); save.Options.RemoveAt(0);
            Assert.IsFalse(new NarrativeRunner().TryRestore(Story(), Ready, save, out _));
        }
        [Test]
        public void StoreRestartAndFailedIndexCommitPreservePriorSuccess()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N"));
            var store = new NovelSaveStore(root); var save = Capture(AtSay(Story()));
            Assert.IsTrue(store.Save(0, save, out var error), error); long stamp = store.Slots.Single().SavedUtcTicks;
            store.BeforeCommit = path => { if (Path.GetFileName(path) == "index.json") throw new IOException("injected index failure"); };
            Assert.IsFalse(store.Save(0, save, out error)); StringAssert.Contains("injected", error);
            Assert.AreEqual(stamp, store.Slots.Single().SavedUtcTicks);
            var restarted = new NovelSaveStore(root); Assert.AreEqual(stamp, restarted.Slots.Single().SavedUtcTicks);
            Assert.IsTrue(restarted.Read(0, out var loaded, out error), error); Assert.AreEqual(save.CommandId, loaded.CommandId);
        }
        [Test]
        public void CorruptLatestDoesNotSelectEarlierSlotOrModifyFiles()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N"));
            var store = new NovelSaveStore(root); var save = Capture(AtSay(Story()));
            Assert.IsTrue(store.Save(0, save, out _)); Assert.IsTrue(store.Save(6, save, out _));
            string bad = Path.Combine(root, store.Slots.Single(s => s.Slot == 6).File); File.WriteAllText(bad, "corrupt");
            var restarted = new NovelSaveStore(root); Assert.AreEqual(6, restarted.Latest);
            Assert.IsFalse(restarted.Read(restarted.Latest, out _, out _)); Assert.AreEqual("corrupt", File.ReadAllText(bad));
            Assert.IsTrue(restarted.Read(0, out _, out _)); Assert.AreEqual(6, restarted.Latest);
        }
        [Test]
        public void CorruptIndexIsNotSilentlyResetOrOverwritten()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "index.json"), "broken"); var store = new NovelSaveStore(root);
            Assert.IsNotNull(store.IndexError); Assert.IsFalse(store.Save(0, Capture(AtSay(Story())), out _));
            Assert.AreEqual("broken", File.ReadAllText(Path.Combine(root, "index.json")));
        }
        [Test]
        public void AccountDataIsSeparateFromSlotRestoreAndIndexHasBackup()
        {
            string root = Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N")); var store = new NovelSaveStore(root);
            var save = Capture(AtSay(Story())); Assert.IsTrue(store.Save(0, save, out _)); Assert.IsTrue(store.Save(7, save, out _));
            var account = new NovelAccountData { TextSpeed = 51 }; account.ReadLines.Add("story/line/2");
            Assert.IsTrue(store.SaveAccount(account, out _)); Assert.IsTrue(store.Read(0, out _, out _));
            Assert.IsTrue(new NovelSaveStore(root).ReadAccount(out var loaded, out _));
            Assert.AreEqual(51, loaded.TextSpeed); CollectionAssert.AreEqual(account.ReadLines, loaded.ReadLines);
            Assert.IsTrue(File.Exists(Path.Combine(root, "index.json.bak")));
        }
    }
}
