using System;
using System.Collections;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Narrative.Tests
{
    public sealed class NarrativeGraphTests
    {
        #region 内部参数
        private string _folder;
        private string _layoutPath;
        private NarrativeChapterSO _chapter;
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        private static readonly Catalog READY = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private NarrativeNodeSO Node(NovelNodeKind kind) => NarrativeGraphModel.CreateNode(_chapter, kind);
        private static void Text(NarrativeDialogueSO node, string text)
        {
            NarrativeGraphModel.AddItem(node, "_commands");
            using var data = new SerializedObject(node);
            data.FindProperty("_commands").GetArrayElementAtIndex(0).FindPropertyRelative("_text").stringValue = text;
            data.ApplyModifiedProperties();
        }
        private static void Advance(NarrativeRunner runner, int frame)
        { var s = runner.Snapshot; runner.Advance(s.SessionGeneration, s.PositionVersion, frame); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            Assert.IsTrue(NarrativeGraphModel.IsTemplateActive());
            _folder = "Assets/Game/Module/Narrative/Tests/Temp_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Game/Module/Narrative/Tests", System.IO.Path.GetFileName(_folder));
            _chapter = ScriptableObject.CreateInstance<NarrativeChapterSO>();
            AssetDatabase.CreateAsset(_chapter, _folder + "/Chapter.asset");
            _layoutPath = NarrativeGraphModel.LAYOUT_ROOT + "/" + AssetDatabase.AssetPathToGUID(_folder + "/Chapter.asset") + ".asset";
        }
        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            if (!string.IsNullOrEmpty(_layoutPath)) AssetDatabase.DeleteAsset(_layoutPath);
        }
        [Test]
        public void AuthorBranchesSaveReloadAndRunBothRoutes_LayoutDoesNotChangeDefinition()
        {
            var intro = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Text(intro, "intro");
            var choice = (NarrativeChoiceSO)Node(NovelNodeKind.Choice);
            var a = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Text(a, "route a");
            var b = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Text(b, "route b");
            var merge = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Text(merge, "merge");
            var end = Node(NovelNodeKind.Ending);
            NarrativeGraphModel.AddItem(choice, "_options"); NarrativeGraphModel.AddItem(choice, "_options");
            using (var data = new SerializedObject(choice))
            {
                var options = data.FindProperty("_options");
                options.GetArrayElementAtIndex(0).FindPropertyRelative("_text").stringValue = "A";
                options.GetArrayElementAtIndex(1).FindPropertyRelative("_text").stringValue = "B"; data.ApplyModifiedProperties();
            }
            NarrativeGraphModel.Connect(_chapter, intro, "_next", choice);
            NarrativeGraphModel.Connect(_chapter, choice, NarrativeGraphModel.Ports(choice)[0], a);
            NarrativeGraphModel.Connect(_chapter, choice, NarrativeGraphModel.Ports(choice)[1], b);
            foreach (var node in new[] { a, b }) NarrativeGraphModel.Connect(_chapter, node, "_next", merge);
            NarrativeGraphModel.Connect(_chapter, merge, "_next", end);
            string before = EditorJsonUtility.ToJson(_chapter);
            var nodesBefore = _chapter.Nodes.ToDictionary(n => n, EditorJsonUtility.ToJson);
            NarrativeGraphModel.Move(_chapter, choice, new Vector2(712, 351));
            Assert.AreEqual(before, EditorJsonUtility.ToJson(_chapter));
            foreach (var pair in nodesBefore) Assert.AreEqual(pair.Value, EditorJsonUtility.ToJson(pair.Key));
            NarrativeGraphModel.Save(_chapter);
            AssetDatabase.ImportAsset(_folder + "/Chapter.asset", ImportAssetOptions.ForceUpdate);
            var reopened = AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>(_folder + "/Chapter.asset");
            Assert.AreEqual(new Vector2(712, 351), NarrativeGraphModel.GetLayout(reopened, false).GetPosition(choice.NodeId, 0));
            Assert.IsTrue(reopened.TryReadDefinition(READY, out var definition, out var errors), string.Join("\n", errors));
            foreach (int branch in new[] { 0, 1 })
            {
                var runner = new NarrativeRunner(); runner.Start(definition, READY);
                Advance(runner, 1); Advance(runner, 2);
                var s = runner.Snapshot;
                Assert.AreEqual(NarrativeState.AwaitingChoice, s.State);
                runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[branch].Id, 3);
                Assert.AreEqual(branch == 0 ? a.NodeId : b.NodeId, runner.Snapshot.NodeId);
                Advance(runner, 4); Advance(runner, 5);
                Assert.AreEqual(merge.NodeId, runner.Snapshot.NodeId);
                Advance(runner, 6); Advance(runner, 7); Assert.AreEqual(NarrativeState.Ended, runner.Snapshot.State);
            }
        }
        [Test]
        public void DuplicateRegeneratesIds_ReorderPreservesIds_RemoveChecksIncoming_UndoRestores()
        {
            var intro = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Text(intro, "one");
            NarrativeGraphModel.AddItem(intro, "_commands", 0);
            Assert.AreNotEqual(intro.Commands[0].CommandId, intro.Commands[1].CommandId);
            string first = intro.Commands[0].CommandId;
            using (var data = new SerializedObject(intro)) { data.FindProperty("_commands").MoveArrayElement(0, 1); data.ApplyModifiedProperties(); }
            Assert.AreEqual(first, intro.Commands[1].CommandId);
            var copy = (NarrativeDialogueSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Dialogue, intro);
            Assert.AreNotEqual(intro.NodeId, copy.NodeId);
            Assert.AreNotEqual(intro.Commands[0].CommandId, copy.Commands[0].CommandId);
            Assert.AreNotEqual(intro.Commands[0].LineId, copy.Commands[0].LineId);
            NarrativeGraphModel.Connect(_chapter, intro, "_next", copy);
            Assert.Throws<InvalidOperationException>(() => NarrativeGraphModel.Remove(_chapter, copy, false));
            NarrativeGraphModel.Remove(_chapter, copy, true); Undo.FlushUndoRecordObjects();
            Assert.IsNull(intro.Next); Assert.IsFalse(_chapter.Nodes.Contains(copy));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<NarrativeNodeSO>(AssetDatabase.GetAssetPath(copy)));
            Undo.PerformUndo();
            Assert.AreEqual(copy, intro.Next); Assert.IsTrue(_chapter.Nodes.Contains(copy));
            Undo.PerformRedo(); Assert.IsNull(intro.Next); Assert.IsFalse(_chapter.Nodes.Contains(copy));
        }
        [Test]
        public void ChoiceAndEndingCopiesGetNewIdentities_CrossChapterLinkRejected()
        {
            var choice = (NarrativeChoiceSO)Node(NovelNodeKind.Choice); NarrativeGraphModel.AddItem(choice, "_options");
            var copy = (NarrativeChoiceSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Choice, choice);
            Assert.AreNotEqual(choice.Options[0].OptionId, copy.Options[0].OptionId);
            var end = (NarrativeEndingSO)Node(NovelNodeKind.Ending);
            var endCopy = (NarrativeEndingSO)NarrativeGraphModel.CreateNode(_chapter, NovelNodeKind.Ending, end);
            Assert.AreNotEqual(end.EndingId, endCopy.EndingId);
            var foreign = ScriptableObject.CreateInstance<NarrativeEndingSO>();
            AssetDatabase.CreateAsset(foreign, _folder + "/Foreign.asset");
            Assert.Throws<InvalidOperationException>(() => NarrativeGraphModel.Connect(_chapter, choice, NarrativeGraphModel.Ports(choice)[0], foreign));
            Assert.IsNull(choice.Options[0].Target);
        }
        [Test]
        public void UndoNodeCreationPreservesRecoverableAsset_InspectorEditsAreGraphSource()
        {
            Undo.IncrementCurrentGroup();
            var a = (NarrativeDialogueSO)Node(NovelNodeKind.Dialogue); Undo.FlushUndoRecordObjects();
            string path = AssetDatabase.GetAssetPath(a);
            Undo.PerformUndo(); Assert.IsFalse(_chapter.Nodes.Contains(a)); Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<NarrativeNodeSO>(path));
            Undo.PerformRedo(); Assert.IsTrue(_chapter.Nodes.Contains(a));
            var b = Node(NovelNodeKind.Ending);
            using (var data = new SerializedObject(a)) { data.FindProperty("_next").objectReferenceValue = b; data.ApplyModifiedProperties(); }
            Assert.AreEqual(b, NarrativeGraphModel.Target(a, "_next"));
        }
        [Test]
        public void OldRegistrationCannotClearNewSource()
        {
            var old = NarrativeObservation.Register(new NarrativeRunner()); var current = new NarrativeRunner();
            using var fresh = NarrativeObservation.Register(current); old.Dispose(); Assert.AreSame(current, NarrativeObservation.Current);
        }
        #endregion
    }

    public sealed class NarrativeObservationPlayTests
    {
        #region 内部参数
        private sealed class Catalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        private sealed class Source : INarrativeDiagnostics
        {
            private readonly NarrativeRunner _runner;
            public int Subscribers { get; private set; }
            public Source(NarrativeRunner runner) { _runner = runner; }
            public NarrativeSnapshot Snapshot => _runner.Snapshot;
            public event Action Changed
            {
                add { _runner.Changed += value; Subscribers++; }
                remove { _runner.Changed -= value; Subscribers--; }
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static IEnumerator CheckPlayingWindow()
        {
            TestContext.WriteLine("Observation detail: entered window-check iterator");
            Assert.IsTrue(EditorApplication.isPlaying);
            var chapter = AssetDatabase.LoadAssetAtPath<NarrativeChapterSO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/Chapters/CH01_School/Chapter.asset");
            Assert.IsNotNull(chapter, "Play Mode 中 M1 章节必须可加载");
            TestContext.WriteLine("Observation detail: chapter loaded; capturing asset state");
            var before = chapter.Nodes.ToDictionary(n => n, EditorJsonUtility.ToJson);
            string chapterBefore = EditorJsonUtility.ToJson(chapter);
            var catalog = new Catalog();
            Assert.IsTrue(chapter.TryReadDefinition(catalog, out var definition, out _));
            TestContext.WriteLine("Observation detail: definition read; starting runner");
            var runner = new NarrativeRunner(); runner.Start(definition, catalog);
            var source = new Source(runner);
            IDisposable registration = NarrativeObservation.Register(source);
            int existingSubscribers = source.Subscribers;
            TestContext.WriteLine("Observation detail: creating first window");
            var window = ScriptableObject.CreateInstance<NarrativeGraphWindow>();
            TestContext.WriteLine("Observation detail: showing first window");
            window.Show();
            TestContext.WriteLine("Observation detail: binding chapter");
            window.ShowChapter(chapter);
            try
            {
                TestContext.WriteLine("Observation detail: awaiting first window frame");
                yield return null;
                TestContext.WriteLine("Observation detail: first window frame resumed");
                Assert.AreEqual("intro", window.ObservedSnapshot.NodeId);
                Assert.IsFalse(NarrativeGraphModel.CanEdit(chapter));
                Assert.Throws<InvalidOperationException>(() => NarrativeGraphModel.CreateNode(chapter, NovelNodeKind.Dialogue));
                window.FollowExecution = false; var browsing = chapter.Nodes.First(n => n.NodeId == "quiet"); window.SelectNode(browsing);
                CompleteTestPresentations(runner);
                var s = runner.Snapshot; runner.Advance(s.SessionGeneration, s.PositionVersion, 1);
                s = runner.Snapshot; runner.Advance(s.SessionGeneration, s.PositionVersion, 2);
                Assert.AreEqual(NarrativeState.AwaitingChoice, window.ObservedSnapshot.State);
                Assert.AreSame(browsing, window.SelectedNode);
                runner.Pause("settings"); runner.Pause("history");
                Assert.AreEqual(2, window.ObservedSnapshot.PauseReasons.Count);
                TestContext.WriteLine("Observation detail: closing paused window");
                window.Close();
                Assert.AreEqual(existingSubscribers, source.Subscribers, "关闭窗口必须解除运行器订阅");
                TestContext.WriteLine("Observation detail: reopening paused window");
                window = ScriptableObject.CreateInstance<NarrativeGraphWindow>(); window.Show();
                Assert.AreEqual(existingSubscribers + 1, source.Subscribers);
                Assert.AreEqual("choice", window.ObservedSnapshot.NodeId);
                Assert.AreEqual(2, window.ObservedSnapshot.PauseReasons.Count);
                window.LocateCurrent(true); Assert.AreEqual("choice", window.SelectedNode.NodeId);
                runner.Resume("settings"); runner.Resume("history"); s = runner.Snapshot;
                runner.Choose(s.SessionGeneration, s.PositionVersion, "ask_option", 3);
                Assert.AreEqual("ask", window.ObservedSnapshot.NodeId);
                TestContext.WriteLine("Observation detail: replacing session");
                var fresh = new NarrativeRunner(); fresh.Start(definition, catalog);
                var newRegistration = NarrativeObservation.Register(fresh); registration.Dispose(); registration = newRegistration;
                Assert.AreEqual(0, source.Subscribers, "换会话必须解除旧源订阅");
                runner.Cancel(); Assert.AreEqual("intro", window.ObservedSnapshot.NodeId);
                Assert.AreEqual(fresh.Snapshot.SessionGeneration, window.ObservedSnapshot.SessionGeneration);
                for (int frame = 10; frame < 40 && fresh.Snapshot.HasActiveSession; frame++)
                {
                    s = fresh.Snapshot;
                    if (s.State == NarrativeState.AwaitingChoice) fresh.Choose(s.SessionGeneration, s.PositionVersion, "leave_option", frame);
                    else if (s.Wait == NarrativeWait.Timer) fresh.Tick(s.SessionGeneration, 1, frame);
                    else if ((s.Wait & NarrativeWait.Presentation) != 0) fresh.CompletePresentation(s.SessionGeneration, s.PositionVersion);
                    else fresh.Advance(s.SessionGeneration, s.PositionVersion, frame);
                }
                TestContext.WriteLine("Observation detail: ending reached; checking unchanged assets");
                Assert.AreEqual("quiet", window.ObservedSnapshot.EndingId);
                foreach (var pair in before) Assert.AreEqual(pair.Value, EditorJsonUtility.ToJson(pair.Key));
                Assert.AreEqual(chapterBefore, EditorJsonUtility.ToJson(chapter));
                registration.Dispose(); Assert.IsNull(window.ObservedSnapshot);
                TestContext.WriteLine("Observation detail: checking fault observation");
                var fault = new NarrativeRunner(4);
                fault.Start(new NovelChapter(chapter.ChapterId, 1, "intro", new[] { new NovelNode("intro", NovelNodeKind.Dialogue, "intro") }, Array.Empty<NovelVariable>()), catalog);
                registration = NarrativeObservation.Register(fault);
                Assert.AreEqual(NarrativeState.Faulted, window.ObservedSnapshot.State);
                Assert.AreEqual("StepLimit", window.ObservedSnapshot.Error.Code);
                window.LocateCurrent(true); Assert.AreEqual("intro", window.SelectedNode.NodeId);
                TestContext.WriteLine("Observation detail: awaiting final window frame");
                yield return null;
                TestContext.WriteLine("Observation detail: final window frame resumed");
            }
            finally
            {
                TestContext.WriteLine("Observation detail: disposing registration and window");
                registration.Dispose(); if (window) window.Close();
                TestContext.WriteLine("Observation detail: cleanup completed");
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [UnityTest]
        public IEnumerator PlayModeWindowReopenFollowPauseReplacementAndExitDoNotWriteAssets()
        {
            TestContext.WriteLine("Observation: entering Play Mode");
            EditorApplication.isPaused = false;
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            EditorApplication.isPaused = false;
            bool background = Application.runInBackground;
            Application.runInBackground = true;
            try
            {
                TestContext.WriteLine("Observation: checking window lifecycle with background updates enabled");
                yield return null;
                // 闭包与窗口必须在域重载之后创建，不跨 EnterPlayMode 保留编译器生成的闭包。
                yield return CheckPlayingWindow();
            }
            finally { Application.runInBackground = background; }
            TestContext.WriteLine("Observation: exiting Play Mode");
            yield return NovelPlayModeScenes.ExitIfPlaying();
            var afterExit = ScriptableObject.CreateInstance<NarrativeGraphWindow>();
            try { Assert.IsNull(afterExit.ObservedSnapshot); }
            finally { Object.DestroyImmediate(afterExit); }
        }
        [UnityTest]
        public IEnumerator StoryOverviewObservesCrossChapterGlobalsAndRemainsReadOnly()
        {
            TestContext.WriteLine("Overview: entering Play Mode");
            EditorApplication.isPaused = false;
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            EditorApplication.isPaused = false;
            // ShowStory and runner notifications update the observed model synchronously.
            // This test checks that model, not a rendered frame or scheduled graph framing.
            TestContext.WriteLine("Overview: checking synchronous cross-chapter observation in Play Mode");
            CheckStoryOverview();
            TestContext.WriteLine("Overview: exiting Play Mode");
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }
        [UnityTearDown]
        public IEnumerator ExitAfterFailedObservationCheck()
        {
            // ExitPlayMode 必须由本方法直接 yield：在 [UnitySetUp]/[UnityTearDown] 里
            // 通过嵌套枚举器（包一层 helper）yield 退出指令会被框架拒绝，报
            // "Nested enumerators are not allowed to yield ExitPlayMode"。
            NovelPlayModeScenes.DiscardUnsavedScenes();
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            NovelPlayModeScenes.DiscardUnsavedScenes();
        }
        private static void CheckStoryOverview()
        {
            Assert.IsTrue(EditorApplication.isPlaying);
            var story = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset");
            var catalog = new Catalog(); Assert.IsTrue(story.TryReadDefinition(catalog, out var definition, out _));
            string before = JsonUtility.ToJson(story);
            var runner = new NarrativeRunner(); runner.StartStory(definition, catalog);
            using var registration = NarrativeObservation.Register(runner);
            var window = ScriptableObject.CreateInstance<NarrativeGraphWindow>(); window.Show(); window.ShowStory(story);
            try
            {
                Assert.IsTrue(window.IsOverview);
                CompleteTestPresentations(runner);
                var s = runner.Snapshot; runner.Advance(s.SessionGeneration, s.PositionVersion, 1);
                s = runner.Snapshot; runner.Advance(s.SessionGeneration, s.PositionVersion, 2);
                s = runner.Snapshot; runner.Choose(s.SessionGeneration, s.PositionVersion, s.Options[0].Id, 3);
                Assert.AreEqual("vn_m1_sample", window.ObservedSnapshot.ChapterId);
                Assert.IsTrue(window.ObservedSnapshot.GlobalVariables["visitSchool"].Bool);
                Assert.IsTrue(window.IsOverview, "总览跟随不能强制钻入章节");
                Assert.Throws<InvalidOperationException>(() => NarrativeStoryModel.AddRoute(story, 0));
                window.LocateCurrent(true); Assert.IsFalse(window.IsOverview); Assert.AreEqual("intro", window.SelectedNode.NodeId);
                Assert.AreEqual(before, JsonUtility.ToJson(story));
            }
            finally { window.Close(); }
        }
        // 仅测试宿主完成演出接口；不将图观察测试当作资源或音频验收。
        private static void CompleteTestPresentations(NarrativeRunner runner)
        {
            for (int i = 0; i < 20 && (runner.Snapshot.Wait & NarrativeWait.Presentation) != 0; i++)
            { var s = runner.Snapshot; runner.CompletePresentation(s.SessionGeneration, s.PositionVersion); }
        }
        #endregion
        // --------------------------------------------------------

    }
}
