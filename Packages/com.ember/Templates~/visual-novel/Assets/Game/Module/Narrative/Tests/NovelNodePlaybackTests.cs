using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void EditorPlaybackOpeningKeepsAutoAfterNameSubmissionAndStageClick(bool automatic)
        {
            JsonUtility.FromJsonOverwrite(GLOBALS_JSON, _story);
            var begin = Create<NovelOpeningSegmentSO>();
            var end = Create<NovelOpeningSegmentSO>();
            JsonUtility.FromJsonOverwrite("{\"_mode\":1}", end);
            var input = ScriptableObject.CreateInstance("NovelPlayerNameInputStep") as NovelCustomStepSO;
            var window = ScriptableObject.CreateInstance<NovelNodePlaybackWindow>();
            var host = new GameObject("name preview regression");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(NovelNodePlaybackWindow);
            try
            {
                List(_story, "_customSteps", begin, end, input);
                using var session = StepSession(new View(), null,
                    StepCommand("begin", begin.ScriptId),
                    new NovelCommand("before", NovelCommandKind.Say, text: "输入前", lineId: "before"),
                    StepCommand("name", input.ScriptId),
                    new NovelCommand("after", NovelCommandKind.Say, text: "输入后", lineId: "after"),
                    StepCommand("end", end.ScriptId));
                type.GetField("_session", flags).SetValue(window, session);
                type.GetField("_audio", flags).SetValue(window, new NovelPlaybackAudio(host.transform));
                type.GetField("_automatic", flags).SetValue(window, automatic);
                var update = type.GetMethod("UpdatePlayback", flags);
                void Tick()
                {
                    type.GetField("_lastTime", flags).SetValue(window, EditorApplication.timeSinceStartup - .1);
                    update.Invoke(window, null);
                }
                for (int i = 0; i < 500 && session.PreviewCustomStepState is not INovelPreviewTextInput; i++) Tick();
                var request = session.PreviewCustomStepState as INovelPreviewTextInput;
                Assert.IsNotNull(request, session.Snapshot.Error?.ToString());
                Assert.IsTrue(session.IsInputLocked);
                request.Submit("林晚");
                type.GetMethod("Advance", flags).Invoke(window, null);
                for (int i = 0; i < 500 && session.Snapshot.State != NarrativeState.Ended; i++) Tick();
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString());
                Assert.IsFalse(session.IsInputLocked);
                Assert.AreEqual("林晚", session.Snapshot.GlobalVariables["playerName"].String);
                Assert.AreEqual(automatic, type.GetField("_automatic", flags).GetValue(window));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(host);
                if (input) UnityEngine.Object.DestroyImmediate(input);
            }
        }

        [Test]
        public void EditorPlaybackDrainsParallelTailPausesAndNeverFollowsOriginalNext()
        {
            Commands(new NovelCommand("tail", NovelCommandKind.Opacity, targetKind: NovelTargetKind.Stage,
                actionId: "fade", duration: 2, parallel: true, opacity: .25f));
            string originalNode = EditorJsonUtility.ToJson(ReadingTalk);
            string originalStory = EditorJsonUtility.ToJson(_story);
            using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
            using var view = new ActionView();
            using var session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
            session.AttachView(view);
            int frame = 0;
            for (; frame < 20 && session.Snapshot.CommandId != data.DrainCommandId; frame++) session.Tick(.01f, frame);
            Assert.AreEqual(data.DrainCommandId, session.Snapshot.CommandId, session.Snapshot.Error?.ToString());
            Assert.AreNotEqual(NarrativeState.Ended, session.Snapshot.State);
            float alpha = view.Alpha[(NovelTargetKind.Stage, default)];
            session.Pause("test"); session.Tick(10, ++frame);
            Assert.AreEqual(alpha, view.Alpha[(NovelTargetKind.Stage, default)]);
            session.Resume("test");
            for (int i = 0; i < 100 && session.Snapshot.State != NarrativeState.Ended; i++) session.Tick(.05f, ++frame);
            Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString());
            Assert.AreEqual(.25f, view.Alpha[(NovelTargetKind.Stage, default)], .0001);
            Assert.AreEqual(NovelActionStatus.Completed, session.Actions.Single().Status);
            Assert.AreNotEqual("finished", session.Snapshot.EndingId);
            Assert.AreEqual(originalNode, EditorJsonUtility.ToJson(ReadingTalk));
            Assert.AreEqual(originalStory, EditorJsonUtility.ToJson(_story));
        }

        [Test]
        public void EditorPlaybackVariableOverridesAndEditsStayInTemporaryStory()
        {
            JsonUtility.FromJsonOverwrite("{\"_variables\":[{\"_id\":\"score\",\"_value\":{\"_type\":1,\"_int\":1}}]}", _story.Entry);
            Commands(new NovelCommand("read", NovelCommandKind.Say, text: "original", lineId: "read"));
            using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story, variables: new[]
            { new NovelPlaybackVariable { Id = "score", Scope = NovelVariableScope.Chapter, Value = new NovelValue(7) } });
            // Simulate editing the source while audition runs; the copied command must not change.
            Commands(new NovelCommand("read", NovelCommandKind.Say, text: "edited", lineId: "read"));
            using var session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
            session.AttachView(new View()); session.Tick(0, 1);
            Assert.AreEqual(7, session.Snapshot.Variables["score"].Int);
            Assert.AreEqual(1, _story.Entry.Variables[0].Value.Int);
            Assert.AreEqual("original", data.Dialogue.Commands[0].Text);
            Assert.AreEqual("edited", ReadingTalk.Commands[0].Text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EditorPlaybackMissingActorReportsCommandAndExplicitSetupResolvesIt(bool useSetup)
        {
            Commands(new NovelCommand("move", NovelCommandKind.Move, instanceId: "actor", actionId: "moving",
                slot: NovelPortraitSlot.Right, duration: .1f));
            var initial = useSetup ? new[] { ShowAction("actor", NovelPortraitSlot.Center) } : Array.Empty<NovelCommand>();
            using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story, initial);
            using var view = new ActionView();
            using var session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
            session.AttachView(view);
            for (int i = 0; i < 100 && session.Snapshot.State != NarrativeState.Ended && session.Snapshot.State != NarrativeState.Faulted; i++) session.Tick(.02f, i);
            Assert.AreEqual(useSetup ? NarrativeState.Ended : NarrativeState.Faulted, session.Snapshot.State, session.Snapshot.Error?.ToString());
            if (!useSetup) Assert.AreEqual("move", session.Snapshot.Error.CommandId);
        }

        [Test]
        public void EditorPlaybackMutedVoiceStillWaitsAndDisposalReleasesAllAudio()
        {
            var preview = new PreviewRenderUtility();
            var host = new GameObject("preview audio test"); preview.AddSingleGO(host);
            var clip = AudioClip.Create("preview clock", 44100, 1, 44100, false);
            using var audio = new NovelPlaybackAudio(host.transform);
            try
            {
                audio.SetMuted(true);
                audio.Play(NovelCommandKind.BGM, clip); audio.Play(NovelCommandKind.SFX, clip); audio.Play(NovelCommandKind.Voice, clip);
                Assert.AreEqual(3, audio.ActiveCount);
                Assert.IsTrue(host.GetComponentsInChildren<AudioSource>().All(a => a.volume == 0));
                audio.AdvanceTime(.4f); Assert.IsTrue(audio.VoicePlaying);
                audio.SetPaused(true); audio.AdvanceTime(5); Assert.IsTrue(audio.VoicePlaying);
                audio.SetPaused(false); audio.AdvanceTime(.61f);
                Assert.IsFalse(audio.VoicePlaying); Assert.AreEqual(1, audio.ActiveCount);
                audio.Dispose(); audio.Dispose();
                Assert.AreEqual(0, audio.ActiveCount); Assert.IsEmpty(host.GetComponentsInChildren<AudioSource>());
            }
            finally { audio.Dispose(); UnityEngine.Object.DestroyImmediate(clip); preview.Cleanup(); }
        }

        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        public void EditorPlaybackFormalViewRunsE2AndRepeatedDisposalLeavesPrefabUnchanged(int width, int height)
        {
            Commands(ShowAction("actor", NovelPortraitSlot.Center),
                new NovelCommand("blend", NovelCommandKind.CrossFade, targetKind: NovelTargetKind.Character,
                    instanceId: "actor", resourceKey: "lastlight_wan_smile", actionId: "blend", duration: 1, parallel: true),
                new NovelCommand("shake", NovelCommandKind.Shake, targetKind: NovelTargetKind.Stage, actionId: "shake", duration: 1, parallel: true),
                SayAction("last"));
            const string path = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
            var before = AssetDatabase.GetAssetDependencyHash(path);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var preview = new PreviewRenderUtility();
                var host = new GameObject("node view test"); preview.AddSingleGO(host);
                NovelPlaybackView view = null;
                NovelSession session = null;
                using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
                try
                {
                    var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), host.transform);
                    view = new NovelPlaybackView(root, preview.camera, new Vector2(width, height));
                    session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
                    session.AttachView(view); session.ConfigureReading(null, 200, 0, 0, 0, 0);
                    bool middle = false;
                    for (int i = 0; i < 400 && session.Snapshot.State != NarrativeState.Ended; i++)
                    {
                        session.Tick(.02f, i); view.Flush();
                        // Match the window: changing modes resets the automatic reading timer.
                        if (session.IsReady && session.Snapshot.HasActiveSession && session.ReadMode != NarrativeReadMode.Auto)
                            session.SetReadMode(NarrativeReadMode.Auto);
                        var picture = root.transform.Find("Center/Picture").GetComponent<Image>();
                        var blend = root.transform.Find("Center/CrossFade/Picture").GetComponent<Image>();
                        middle |= picture.enabled && blend.enabled && picture.color.a > .1f && blend.color.a > .1f;
                    }
                    Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString());
                    Assert.IsTrue(middle, "必须观察到正式双图过渡中间帧。");
                    Assert.IsTrue(root.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text == "E0 测试"));
                    session.Dispose(); session = null;
                    Assert.IsFalse(root.transform.Find("Center/Picture").GetComponent<Image>().enabled);
                    view.Dispose(); view.Dispose(); view = null;
                    Assert.IsNull(root.transform.Find("Cover"));
                    Assert.IsNull(root.transform.Find("Center/CrossFade"));
                }
                finally { session?.Dispose(); view?.Dispose(); preview.Cleanup(); }
            }
            Assert.AreEqual(before, AssetDatabase.GetAssetDependencyHash(path));
        }
    }
}
