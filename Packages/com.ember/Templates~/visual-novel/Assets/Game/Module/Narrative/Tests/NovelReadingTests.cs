using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ember.Table;
using Game.Table;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed class ReadingAudio : INovelAudio
        {
            public bool VoicePlaying { get; private set; }
            public bool Paused, Disposed;
            public int Voices, Effects, Music;
            public float VoiceVolume;
            public event Action VoiceCompleted;
            public void Play(NovelCommandKind kind, AudioClip clip)
            {
                if (kind == NovelCommandKind.Voice) { Voices++; VoicePlaying = true; }
                else if (kind == NovelCommandKind.SFX) Effects++; else Music++;
            }
            public void Finish() { if (Paused || !VoicePlaying) return; VoicePlaying = false; VoiceCompleted?.Invoke(); }
            public void Tick() { }
            public void StopVoice() { VoicePlaying = false; }
            public void SetPaused(bool paused) { Paused = paused; }
            public void SetVolumes(float bgm, float sfx, float voice) { VoiceVolume = voice; }
            public void Dispose() { StopVoice(); Disposed = true; }
        }
        private sealed class ReadingResources : INovelResources
        {
            public NarrativeStorySO Story;
            public readonly Lease<AudioClip> Clip = new();
            public int AudioLoads;
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
            {
                if (typeof(T) == typeof(NarrativeStorySO)) return new Lease<T> { Asset = Story as T, IsDone = true };
                AudioLoads++; return (INovelAssetLease<T>)(object)Clip;
            }
        }
        private NarrativeDialogueSO ReadingTalk => (NarrativeDialogueSO)_story.Entry.Entry;
        private void VoiceTable()
        {
            Assert.IsTrue(_engine.Database.TryGetTable<NovelAudioRow>("novel_audio", out var table));
            // Per-test database only; no CSV, ETBL, SO or developer account is changed.
            var rows = (Dictionary<string, NovelAudioRow>)typeof(EmberTable<NovelAudioRow>)
                .GetField("_byPrimaryKey", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(table);
            rows.Add("test_voice", new NovelAudioRow("test_voice", "Voice", "test/voice"));
        }
        private void Commands(params NovelCommand[] commands)
        {
            typeof(NarrativeDialogueSO).GetField("_commands", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ReadingTalk, new List<NovelCommand>(commands));
        }
        private NovelSession ReadingSession(out ReadingResources resources, out ReadingAudio audio, Func<string,string,int,bool> read = null)
        {
            resources = new ReadingResources { Story = _story }; audio = new ReadingAudio();
            var s = new NovelSession(new NovelNewGameRequest(), () => _tables, resources, audio);
            s.ConfigureReading(read, 10, 1, .5f, .6f, .7f); s.AttachView(new View()); s.Tick(0,0); return s;
        }
        private AudioClip ReadingClip()
        { var clip = AudioClip.Create("M4 test", 22050, 1, 22050, false); _assets.Add(clip); return clip; }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ReadingMultiplierScalesTextAndAutoDelay(int multiplier)
        {
            using var session = ReadingSession(out _, out _);
            session.SetReadingMultiplier(multiplier); session.SetReadMode(NarrativeReadMode.Auto);
            session.Tick(.99f / multiplier, 1);
            Assert.AreEqual(NarrativeState.Revealing, session.Snapshot.State);
            session.Tick(.02f / multiplier, 2);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
            session.Tick(1.49f / multiplier, 3); Assert.AreEqual("say1", session.Snapshot.CommandId);
            session.Tick(.02f / multiplier, 4); Assert.AreNotEqual("say1", session.Snapshot.CommandId);
        }

        [Test]
        public void RestoringHiddenUIConsumesAllAdvanceCallsInSameFrame()
        {
            using var session = ReadingSession(out _, out _);
            session.Advance(1); session.SetDialogueHidden(true);
            session.Advance(2); session.Advance(2);
            Assert.IsFalse(session.DialogueHidden); Assert.AreEqual("say1", session.Snapshot.CommandId);
        }

        [Test]
        public void M4AutoWaitsForTextThenIntervalAndNestedSameNameOwners()
        {
            using var s = ReadingSession(out _, out var audio);
            s.SetReadMode(NarrativeReadMode.Auto); s.Tick(.5f,1);
            Assert.AreEqual(NarrativeState.Revealing,s.Snapshot.State);
            s.Tick(.5f,2); Assert.AreEqual(NarrativeState.AwaitingAdvance,s.Snapshot.State);
            Assert.AreEqual(NarrativeWait.Timer,s.Snapshot.Wait);
            var first=s.AcquirePause("popup"); var second=s.AcquirePause("popup");
            Assert.AreEqual(2,s.Snapshot.PauseReasons.Count); first.Dispose(); first.Dispose();
            s.Tick(100,3); Assert.AreEqual("say1",s.Snapshot.CommandId); Assert.IsTrue(audio.Paused);
            second.Dispose(); s.Tick(1.4f,4); Assert.AreEqual("say1",s.Snapshot.CommandId);
            s.Tick(.11f,5); Assert.AreEqual("hide",s.Snapshot.CommandId);
            Assert.AreEqual(1,s.History.Count);
        }

        [Test]
        public void M4SkipStopsOnUnreadBeforeCompletionCanMarkItRead()
        {
            var read = new HashSet<string> { "line1" };
            using var s=ReadingSession(out _,out _, (_,line,revision)=>revision==1&&read.Contains(line));
            s.LineRead += h=>read.Add(h.LineId); s.SetReadMode(NarrativeReadMode.Skip);
            s.Tick(0,1); s.Tick(0,2); s.Tick(0,3);
            Assert.AreEqual("say2",s.Snapshot.CommandId); Assert.AreEqual(NarrativeReadMode.Manual,s.Snapshot.ReadMode);
            Assert.AreEqual(NarrativeState.Revealing,s.Snapshot.State); Assert.IsFalse(read.Contains("line2"));
            s.Tick(2,4); Assert.IsTrue(read.Contains("line2")); s.Tick(100,5);
            Assert.AreEqual("say2",s.Snapshot.CommandId,"本次刚标已读不能重新激活快进");
        }

        [Test]
        public void M4SkipProcessesWaitVisualsAndVariablesButDoesNotLoadEffectsOrVoice()
        {
            VoiceTable();
            typeof(NarrativeChapterSO).GetField("_variables", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_story.Entry, new List<NovelVariable> { new("count",new NovelValue(0)) });
            Commands(new NovelCommand("say1",NovelCommandKind.Say,"one","line1"),
                new NovelCommand("assign",NovelCommandKind.SetVariable,variableId:"count",value:new NovelValue(42)),
                new NovelCommand("wait",NovelCommandKind.Wait,duration:600),
                new NovelCommand("hide",NovelCommandKind.Character,duration:600,visualAction:NovelVisualAction.Hide),
                new NovelCommand("sfx",NovelCommandKind.SFX,resourceKey:"choice_chime"),
                new NovelCommand("voice",NovelCommandKind.Voice,resourceKey:"test_voice"),
                new NovelCommand("bgm",NovelCommandKind.BGM,resourceKey:"quiet_afternoon"),
                new NovelCommand("say2",NovelCommandKind.Say,"two","line2"));
            using var s=ReadingSession(out var resources,out var audio,(_,line,revision)=>line=="line1");
            resources.Clip.Asset=ReadingClip(); resources.Clip.IsDone=true;
            s.SetReadMode(NarrativeReadMode.Skip);
            for(int frame=1;frame<12;frame++) s.Tick(0,frame);
            Assert.AreEqual("say2",s.Snapshot.CommandId); Assert.AreEqual(NarrativeReadMode.Manual,s.ReadMode);
            Assert.AreEqual(0,audio.Effects); Assert.AreEqual(0,audio.Voices); Assert.AreEqual(1,audio.Music);
            Assert.AreEqual(1,resources.AudioLoads,"仅 BGM 加载资源");
            Assert.AreEqual(42,s.Snapshot.Variables["count"].Int);
        }

        [TestCase(NarrativeReadMode.Auto)]
        [TestCase(NarrativeReadMode.Skip)]
        public void M4ReadingModesStopAtChoice(NarrativeReadMode mode)
        {
            var choice=Create<NarrativeChoiceSO>(); Set(choice,"_chapterId","session_test"); Set(choice,"_nodeId","choice");
            var end=_story.Entry.Nodes.Last();
            JsonUtility.FromJsonOverwrite("{\"_options\":[{\"_optionId\":\"pick\",\"_text\":\"choose\",\"_condition\":{\"_predicates\":[]}}]}",choice);
            var data=new SerializedObject(choice); data.FindProperty("_options").GetArrayElementAtIndex(0).FindPropertyRelative("_target").objectReferenceValue=end;
            data.ApplyModifiedPropertiesWithoutUndo(); Ref(ReadingTalk,"_next",choice); List(_story.Entry,"_nodes",ReadingTalk,choice,end);
            using var s=ReadingSession(out _,out _,(_,line,revision)=>true); s.SetReadMode(mode);
            for(int frame=1;frame<15;frame++) s.Tick(10,frame);
            Assert.AreEqual(NarrativeState.AwaitingChoice,s.Snapshot.State); Assert.AreEqual(NarrativeReadMode.Manual,s.Snapshot.ReadMode);
            Assert.AreEqual("pick",s.Snapshot.Options.Single().Id);
        }

        [Test]
        public void M4StandaloneVoiceWaitsAndSkipCancelsAlreadyPendingLoad()
        {
            VoiceTable(); Commands(new NovelCommand("voice",NovelCommandKind.Voice,resourceKey:"test_voice"),
                new NovelCommand("say1",NovelCommandKind.Say,"one","line1"));
            using var s=ReadingSession(out var resources,out var audio,(_,line,revision)=>false);
            s.Tick(0,1); Assert.IsTrue((s.Snapshot.Wait&NarrativeWait.Resource)!=0);
            s.SetReadMode(NarrativeReadMode.Skip); s.Tick(0,2);
            resources.Clip.Asset=ReadingClip(); resources.Clip.IsDone=true; s.Tick(0,3);
            Assert.AreEqual("say1",s.Snapshot.CommandId); Assert.AreEqual(0,audio.Voices);
            Assert.AreEqual(NarrativeReadMode.Manual,s.ReadMode);
        }

        [Test]
        public void M4StandaloneVoiceOnlyCompletesFromPlaybackAndExplicitStop()
        {
            VoiceTable(); Commands(new NovelCommand("voice",NovelCommandKind.Voice,resourceKey:"test_voice"),
                new NovelCommand("say1",NovelCommandKind.Say,"one","line1"));
            using var s=ReadingSession(out var resources,out var audio);
            resources.Clip.Asset=ReadingClip(); resources.Clip.IsDone=true; s.Tick(0,1); s.Tick(0,2);
            Assert.AreEqual(NarrativeWait.Presentation|NarrativeWait.Voice,s.Snapshot.Wait);
            s.Tick(100,3); Assert.AreEqual("voice",s.Snapshot.CommandId,"不能用剧情计时代替播放完成");
            s.StopVoice(); s.Tick(0,4); Assert.AreEqual("say1",s.Snapshot.CommandId); Assert.AreEqual(1,audio.Voices);
        }

        [Test]
        public void M4HiddenDialogueFreezesModeAndFirstAdvanceOnlyRestoresUI()
        {
            using var s=ReadingSession(out _,out _); s.Advance(1); s.SetReadMode(NarrativeReadMode.Auto);
            s.SetDialogueHidden(true); s.Tick(100,2);
            Assert.IsTrue(s.DialogueHidden); Assert.AreEqual(NarrativeReadMode.Auto,s.Snapshot.ReadMode);
            s.Advance(3); s.Tick(100,3);
            Assert.IsFalse(s.DialogueHidden); Assert.AreEqual("say1",s.Snapshot.CommandId);
        }

        [Test]
        public void M4AutoWaitsForVoiceResourceAndNaturalCompletionThenInterval()
        {
            VoiceTable(); Commands(new NovelCommand("say1",NovelCommandKind.Say,"one","line1",resourceKey:"test_voice"),
                new NovelCommand("say2",NovelCommandKind.Say,"two","line2"));
            using var s=ReadingSession(out var resources,out var audio); s.SetReadMode(NarrativeReadMode.Auto);
            s.Tick(5,1); s.Tick(5,2);
            Assert.AreEqual("say1",s.Snapshot.CommandId); Assert.IsTrue((s.Snapshot.Wait&NarrativeWait.Resource)!=0);
            resources.Clip.Asset=ReadingClip(); resources.Clip.IsDone=true; s.Tick(5,3);
            Assert.AreEqual(1,audio.Voices); Assert.IsTrue((s.Snapshot.Wait&NarrativeWait.Voice)!=0);
            using(var pause=s.AcquirePause("history")) { audio.Finish(); s.Tick(100,4); Assert.IsTrue(audio.VoicePlaying); }
            audio.Finish(); s.Tick(.5f,5); Assert.AreEqual("say1",s.Snapshot.CommandId);
            s.Tick(.6f,6); Assert.AreEqual("say2",s.Snapshot.CommandId);
            Assert.IsFalse(audio.VoicePlaying);
        }

        [Test]
        public void M4JumpAndDisposeRejectLateVoiceLoadAndNotifications()
        {
            VoiceTable(); Commands(new NovelCommand("say1",NovelCommandKind.Say,"one","line1",resourceKey:"test_voice"),
                new NovelCommand("say2",NovelCommandKind.Say,"two","line2"));
            var s=ReadingSession(out var resources,out var audio); s.Tick(0,1); s.Advance(2); s.Advance(3);
            resources.Clip.Asset=ReadingClip(); resources.Clip.IsDone=true; s.Tick(0,4);
            Assert.AreEqual(0,audio.Voices); Assert.AreEqual("say2",s.Snapshot.CommandId);
            int changed=0; s.Changed+=()=>changed++; s.Dispose(); int final=changed;
            audio.Finish(); s.Tick(100,5); Assert.AreEqual(final,changed); Assert.IsTrue(resources.Clip.Disposed);
        }

        [Test]
        public void M4RestoreIsManualDoesNotReplayVoiceOrAppendHistoryOrRollbackReadSet()
        {
            VoiceTable(); Commands(new NovelCommand("say1",NovelCommandKind.Say,"one","line1",resourceKey:"test_voice"));
            var read=new HashSet<string>();
            using var original=ReadingSession(out _,out _,(_,line,revision)=>read.Contains(line));
            original.LineRead+=entry=>read.Add(entry.LineId); original.Advance(1);
            Assert.IsTrue(original.TryCapture(out var checkpoint,out var error),error); read.Add("future-line");
            var audio=new ReadingAudio(); var resources=new ReadingResources { Story=_story };
            using var restored=new NovelSession(checkpoint,()=>_tables,resources,audio);
            restored.ConfigureReading((_,line,revision)=>read.Contains(line),30,0,1,1,.4f);
            restored.Tick(0,2); restored.Tick(0,3); restored.CommitRestore(new View()); restored.Tick(10,4);
            Assert.AreEqual(NarrativeReadMode.Manual,restored.ReadMode); Assert.AreEqual(1,restored.History.Count);
            Assert.AreEqual(0,audio.Voices); Assert.AreEqual(0,resources.AudioLoads); Assert.IsTrue(read.Contains("future-line"));
            Assert.AreEqual(.4f,audio.VoiceVolume);
        }
    }
}
