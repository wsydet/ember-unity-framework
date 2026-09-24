using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Game.NovelSave;
using UnityEngine.TestTools;
using Ember.Table;
using Game.Table.Generated;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed class Lease<T> : INovelAssetLease<T> where T : UnityEngine.Object
        {
            public bool IsDone { get; set; }
            public T Asset { get; set; }
            public string Error { get; set; }
            public bool Disposed;
            public void Dispose() { Disposed = true; }
        }
        private sealed class ResourcesFake : INovelResources
        {
            public readonly Lease<NarrativeStorySO> Story = new();
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object => (INovelAssetLease<T>)(object)Story;
        }
        private sealed class MissingPresentationResources : INovelResources
        {
            public NarrativeStorySO Story;
            public int PresentationLoads;
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
            {
                if (typeof(T) == typeof(NarrativeStorySO)) return new Lease<T> { Asset = Story as T, IsDone = true };
                PresentationLoads++; return new Lease<T> { IsDone = true, Error = "missing presentation asset" };
            }
        }
        private sealed class View : INovelView
        {
            public int TextLength => 10;
            public int Visible, Clears;
            public float Progress;
            public void Render(NarrativeSnapshot snapshot, NovelCommand command, string speaker, int visibleCharacters, string status) { Visible = visibleCharacters; }
            public void Visual(NovelCommand command, Sprite sprite, float progress) { Progress = progress; }
            public void ClearVisuals() { Clears++; }
        }
        private readonly List<UnityEngine.Object> _assets = new();
        private EmberTableEngine _engine;
        private NarrativeTableCatalog _tables;
        private NarrativeStorySO _story;

        [SetUp]
        public void SetUp()
        {
            _engine = new EmberTableEngine(); var catalog = GameTables.CreateCatalog();
            var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries) bytes.Add(entry.TableId, Resources.Load<TextAsset>(entry.ResourcePath).bytes);
            Assert.IsTrue(_engine.Load(catalog, bytes).Succeeded); _tables = new NarrativeTableCatalog(_engine.Database);
            _story = Create<NarrativeStorySO>(); var chapter = Create<NarrativeChapterSO>();
            var talk = Create<NarrativeDialogueSO>(); var ending = Create<NarrativeEndingSO>();
            Set(chapter, "_chapterId", "session_test"); Set(talk, "_chapterId", "session_test"); Set(ending, "_chapterId", "session_test");
            Set(talk, "_nodeId", "talk"); Set(ending, "_nodeId", "end"); Set(ending, "_endingId", "finished");
            Ref(talk, "_next", ending); Ref(chapter, "_entry", talk); Ref(_story, "_entry", chapter);
            List(chapter, "_nodes", talk, ending); List(_story, "_chapters", chapter);
            JsonUtility.FromJsonOverwrite("{\"_commands\":[{\"_commandId\":\"say1\",\"_kind\":0,\"_lineId\":\"line1\",\"_textRevision\":1,\"_text\":\"1234567890\"},{\"_commandId\":\"hide\",\"_kind\":4,\"_visualAction\":2,\"_duration\":1},{\"_commandId\":\"say2\",\"_kind\":0,\"_lineId\":\"line2\",\"_textRevision\":1,\"_text\":\"第二句\"}]}", talk);
        }
        private T Create<T>() where T : ScriptableObject { var a = ScriptableObject.CreateInstance<T>(); _assets.Add(a); return a; }
        private void Set(UnityEngine.Object obj, string field, string value) { var s=new SerializedObject(obj); s.FindProperty(field).stringValue=value; s.ApplyModifiedPropertiesWithoutUndo(); }
        private void Ref(UnityEngine.Object obj, string field, UnityEngine.Object value) { var s=new SerializedObject(obj); s.FindProperty(field).objectReferenceValue=value; s.ApplyModifiedPropertiesWithoutUndo(); }
        private void List(UnityEngine.Object obj, string field, params UnityEngine.Object[] values)
        { var s=new SerializedObject(obj); var p=s.FindProperty(field); p.arraySize=values.Length; for(int i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; s.ApplyModifiedPropertiesWithoutUndo(); }
        [TearDown]
        public void TearDown() { _engine.Dispose(); foreach(var a in _assets) UnityEngine.Object.DestroyImmediate(a); _assets.Clear(); }

        [Test]
        public void DeliverySampleResolvesExportedTablesAndTypedResources()
        {
            var story = Resources.Load<NarrativeStorySO>("Config/Narrative/LastLight/Story");
            Assert.IsNotNull(story);
            var type = typeof(Game.Narrative.Editor.NarrativeGraphWindow).Assembly.GetType("Game.Narrative.Editor.NarrativeAssetValidation");
            var validate = type.GetMethod("Validate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var issues = (IReadOnlyList<NarrativeError>)validate.Invoke(null, new object[] { story, null, _tables });
            Assert.IsEmpty(issues, string.Join("\n", issues));
        }

        [Test]
        public void EmptyVoiceIsValidButBgmCannotBeSelectedAsVoice()
        {
            Assert.IsTrue(_story.TryReadDefinition(_tables, out _, out _));
            var dialogue = (NarrativeDialogueSO)_story.Entry.Entry;
            using var data = new SerializedObject(dialogue);
            data.FindProperty("_commands").GetArrayElementAtIndex(0).FindPropertyRelative("_resourceKey").stringValue = "quiet_afternoon";
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(_story.TryReadDefinition(_tables, out _, out var issues));
            Assert.That(issues, Has.Some.Matches<NarrativeError>(e => e.Code == "MissingResourceKey" && e.CommandId == "say1"));
        }

        [Test]
        public void AllReadyGatesMustOpenBeforeFirstCommand()
        {
            var resources=new ResourcesFake(); NarrativeTableCatalog ready=null;
            using var session=new NovelSession(new NovelNewGameRequest(),()=>ready,resources);
            session.Tick(1,1); Assert.AreEqual(NarrativeState.Preparing,session.Snapshot.State);
            resources.Story.Asset=_story; resources.Story.IsDone=true;
            session.Tick(1,2); Assert.IsFalse(session.IsReady);
            ready=_tables; session.Tick(1,3); Assert.IsFalse(session.IsReady);
            session.AttachView(new View()); session.Tick(0,4);
            Assert.IsTrue(session.IsReady); Assert.AreEqual("say1",session.Snapshot.CommandId);
        }

        [Test]
        public void RapidClickAndNestedPauseCannotCrossLineOrAdvanceTransition()
        {
            var resources=new ResourcesFake(); resources.Story.Asset=_story; resources.Story.IsDone=true;
            using var session=new NovelSession(new NovelNewGameRequest(),()=>_tables,resources);
            var view=new View(); session.AttachView(view); session.Tick(0,0);
            session.Advance(1); session.Advance(1);
            Assert.AreEqual("say1",session.Snapshot.CommandId); Assert.AreEqual(NarrativeState.AwaitingAdvance,session.Snapshot.State);
            session.Pause("settings"); session.Pause("confirm"); session.Advance(2); session.Tick(10,2);
            session.Resume("confirm"); session.Advance(3); Assert.AreEqual("say1",session.Snapshot.CommandId);
            session.Resume("settings"); session.Advance(4); session.Tick(.25f,5);
            Assert.AreEqual(.25f,view.Progress,.001f);
            Assert.IsTrue((session.Snapshot.Wait & NarrativeWait.Transition)!=0);
            session.Pause("settings"); session.Tick(10,6); Assert.AreEqual(.25f,view.Progress,.001f);
            session.Resume("settings"); session.Tick(.75f,7); Assert.AreEqual("say2",session.Snapshot.CommandId);
            Assert.AreEqual(0,view.Visible,"下一句不能闪现上一句的完整可见数");
        }

        [Test]
        public void ExitWhileLoadingAndLateCompletionCannotAffectReplacement()
        {
            var oldResources=new ResourcesFake(); var old=new NovelSession(new NovelNewGameRequest(),()=>_tables,oldResources);
            var oldView=new View(); old.AttachView(oldView); old.Dispose(); old.Dispose();
            Assert.IsTrue(oldResources.Story.Disposed); Assert.AreEqual(0,old.OwnedResourceCount);
            using var next=new NovelSession(new NovelNewGameRequest(),()=>_tables,new ResourcesFake());
            oldResources.Story.Asset=_story; oldResources.Story.IsDone=true;
            old.Tick(10,100); old.Advance(100);
            Assert.AreEqual(NarrativeState.Cancelled,old.Snapshot.State);
            Assert.AreEqual(NarrativeState.Preparing,next.Snapshot.State);
            Assert.AreNotEqual(old.Snapshot.SessionGeneration,next.Snapshot.SessionGeneration);
            Assert.AreEqual(1,oldView.Clears);
        }

        [Test]
        public void RepeatedSessionsLeaveDefinitionUnchangedAndReleaseAllLeases()
        {
            var before=new Dictionary<UnityEngine.Object,string>(); foreach(var asset in _assets) before.Add(asset,JsonUtility.ToJson(asset));
            for(int pass=0;pass<4;pass++)
            {
                var resources=new ResourcesFake(); resources.Story.Asset=_story; resources.Story.IsDone=true;
                var session=new NovelSession(new NovelNewGameRequest(),()=>_tables,resources); session.AttachView(new View());
                for(int frame=0;frame<20 && session.Snapshot.State!=NarrativeState.Ended;frame++) { session.Tick(.5f,frame); session.Advance(frame); }
                Assert.AreEqual(NarrativeState.Ended,session.Snapshot.State); session.Dispose();
                Assert.IsTrue(resources.Story.Disposed); Assert.AreEqual(0,session.OwnedResourceCount);
            }
            foreach(var pair in before) Assert.AreEqual(pair.Value,JsonUtility.ToJson(pair.Key));
        }

        [Test]
        public void PreparationFailureIsVisibleAndDoesNotStartRunner()
        {
            var resources=new ResourcesFake(); resources.Story.IsDone=true; resources.Story.Error="missing story";
            using var session=new NovelSession(new NovelNewGameRequest(),()=>_tables,resources); session.AttachView(new View()); session.Tick(0,0);
            Assert.AreEqual(NarrativeState.Faulted,session.Snapshot.State); Assert.IsFalse(session.IsReady);
            StringAssert.Contains("missing story",session.Snapshot.Error.Message);
        }

        [Test]
        public void CandidatePreparesWithoutTouchingOriginalViewAndRestoresHistoryOnlyOnce()
        {
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            using var original = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            var view = new View(); original.AttachView(view); original.Tick(0, 0); original.Advance(1);
            Assert.IsTrue(original.TryCapture(out var save, out var error), error); Assert.AreEqual(1, save.History.Count);
            original.Pause("load");
            var candidateResources = new ResourcesFake(); candidateResources.Story.Asset = _story; candidateResources.Story.IsDone = true;
            using var candidate = new NovelSession(save, () => _tables, candidateResources);
            Assert.AreEqual(NarrativeState.Restoring, candidate.Snapshot.State);
            candidate.Tick(0, 2); candidate.Tick(0, 3); Assert.IsTrue(candidate.RestoreReady);
            Assert.AreEqual(0, view.Clears); Assert.IsFalse(original.IsDisposed);
            original.Dispose(); candidate.CommitRestore(view);
            candidate.Tick(1, 4); Assert.IsTrue(candidate.TryCapture(out var restored, out error), error);
            Assert.AreEqual(1, restored.History.Count); Assert.AreEqual(NarrativeState.AwaitingAdvance, candidate.Snapshot.State);
            candidate.Advance(5); Assert.AreEqual("hide", candidate.Snapshot.CommandId);
        }

        [UnityTest]
        public IEnumerator CancelDuringFileReadCannotCommitLateCandidate()
        {
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            using var original = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            var view = new View(); original.AttachView(view); original.Tick(0, 0); original.Advance(1);
            Assert.IsTrue(original.TryCapture(out var checkpoint, out _));
            var store = new NovelSaveStore(System.IO.Path.Combine(".utmp/visual-novel-m3/tests", Guid.NewGuid().ToString("N")));
            Assert.IsTrue(store.Save(0, checkpoint, out _));
            var module = new NovelSaveModule();
            typeof(NovelSaveModule).GetProperty(nameof(module.Store)).SetValue(module, store);
            module.Track(original); bool committed = false;
            try
            {
                Assert.IsTrue(module.BeginLoad(0, () => _tables, _ => committed = true));
                var read = (Task)typeof(NovelSaveModule).GetField("_loadTask", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(module);
                CollectionAssert.Contains(original.Snapshot.PauseReasons, "RestoreTransaction");
                module.CancelLoad(); module.Track(null);
                Assert.IsFalse(module.IsBusy); Assert.IsEmpty(original.Snapshot.PauseReasons);
                float deadline = Time.realtimeSinceStartup + 10;
                while (!read.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(read.IsCompleted); module.Update();
                Assert.IsFalse(committed); Assert.IsFalse(original.IsDisposed);
                Assert.AreEqual("say1", original.Snapshot.CommandId); Assert.AreEqual(0, view.Clears);
            }
            finally { ((Ember.Core.IEmberModule)module).OnDestroy(); }
        }

        [Test]
        public void CandidateFailureAndCancellationDoNotDisposeOriginalOrConsumeLateResource()
        {
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            using var original = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            var view = new View(); original.AttachView(view); original.Tick(0, 0); original.Advance(1);
            Assert.IsTrue(original.TryCapture(out var save, out _));
            var pending = new ResourcesFake(); var candidate = new NovelSession(save, () => _tables, pending);
            candidate.Tick(0, 2); candidate.Dispose(); pending.Story.Asset = _story; pending.Story.IsDone = true; candidate.Tick(0, 3);
            Assert.IsFalse(candidate.RestoreReady); Assert.IsTrue(pending.Story.Disposed); Assert.AreEqual(0, view.Clears);
            var missing = new ResourcesFake(); missing.Story.IsDone = true; missing.Story.Error = "missing saved story";
            using var failed = new NovelSession(save, () => _tables, missing); failed.Tick(0, 4);
            Assert.AreEqual(NarrativeState.Faulted, failed.Snapshot.State); Assert.IsFalse(original.IsDisposed);
            Assert.AreEqual("say1", original.Snapshot.CommandId); Assert.AreEqual(0, view.Clears);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void MissingSavedVisualOrBgmFailsBeforeTouchingOriginal(bool audio)
        {
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            using var original = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            var view = new View(); original.AttachView(view); original.Tick(0, 0); original.Advance(1);
            Assert.IsTrue(original.TryCapture(out var save, out _));
            if (audio) save.BgmKey = "quiet_afternoon";
            else save.Visuals.Add(new NovelVisualState { Kind = NovelCommandKind.Background, Key = "campus", InstanceId = "background" });
            var missing = new MissingPresentationResources { Story = _story };
            using var candidate = new NovelSession(save, () => _tables, missing);
            candidate.Tick(0, 2); candidate.Tick(0, 3);
            Assert.AreEqual(NarrativeState.Faulted, candidate.Snapshot.State); Assert.AreEqual(1, missing.PresentationLoads);
            Assert.AreEqual(0, view.Clears); Assert.IsFalse(original.IsDisposed);
        }
    }
}
