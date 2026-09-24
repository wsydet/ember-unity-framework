using System;
using System.Linq;
using Game.UI.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed class TextView : INovelView, INovelTextView
        {
            public int Capacity = 4, Start, Visible, Clears;
            public bool StoryVisible = true;
            public NovelCommand Command;
            public int TextLength => NovelTextRules.Length(Command?.Text);
            public int PrepareText(NovelCommand command, int start)
            { Command = command; Start = start; return Math.Min(TextLength, start + Capacity); }
            public void SetStoryDialogueVisible(bool visible) { StoryVisible = visible; }
            public void ShowText(string speaker, int visibleCharacters) { Visible = visibleCharacters; }
            public void Render(NarrativeSnapshot snapshot, NovelCommand command, string speaker, int visibleCharacters, string status)
            { ShowText(speaker, visibleCharacters); }
            public void Visual(NovelCommand command, Sprite sprite, float progress) { }
            public void ClearVisuals() { Clears++; Command = null; }
        }

        [Test]
        public void E4ClockSplitsTimeAtPauseSpeedAndInstantBoundaries()
        {
            var clock = new NovelTextClock();
            var beats = new[] { new NovelTextBeat(2, .5f, 2, 3), new NovelTextBeat(6, .25f, .5f) };
            clock.Reset(beats);
            Assert.AreEqual(2, clock.Tick(.2f, 10, 10), .0001);
            Assert.IsTrue(clock.Pausing);
            Assert.AreEqual(2, clock.Tick(.4f, 10, 10), .0001);
            Assert.AreEqual(6, clock.Tick(.15f, 10, 10), .0001);
            Assert.AreEqual(7, clock.Tick(.45f, 10, 10), .0001);
            var whole = new NovelTextClock(); whole.Reset(beats);
            Assert.AreEqual(clock.Visible, whole.Tick(1.2f, 10, 10), .0001, "Frame partition must not discard boundary time.");
            clock.Reset(beats); clock.Tick(.2f, 10, 10); clock.Complete(4);
            Assert.IsFalse(clock.Pausing);
            Assert.AreEqual(5, clock.Tick(0, 10, 10), .0001, "Instant range continues across a page without repeating its pause.");
            clock.Reset(null); Assert.AreEqual(1, clock.Tick(.1f, 10, 10), .0001);
        }

        [Test]
        public void E4ScalarOffsetsDoNotSplitSurrogatePairsAndTextStaysLiteral()
        {
            const string text = "甲🌙乙\r\n<慢>";
            Assert.AreEqual(8, NovelTextRules.Length(text));
            Assert.AreEqual("🌙乙\r\n<慢>", NovelTextRules.Slice(text, 1));
            Assert.AreEqual("乙\r\n<慢>", NovelTextRules.Slice(text, 2));
            Assert.AreEqual(3, NovelTextRules.Utf16Index(text, 2));
            Assert.AreEqual(2, NovelTextRules.Length(text, 3));
        }

        [TestCase(-1, 0, 1, 0)]
        [TestCase(3, 0, 1, 0)]
        [TestCase(0, -1, 1, 0)]
        [TestCase(0, 61, 1, 0)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(0, 0, 21, 0)]
        [TestCase(0, 0, 1, 4)]
        public void E4InvalidBeatStopsAtValidation(int at, float pause, float speed, int instant)
        {
            Commands(new NovelCommand("line", NovelCommandKind.Say, "abc", "line",
                textBeats: new[] { new NovelTextBeat(at, pause, speed, instant) }));
            Assert.IsFalse(_story.TryReadDefinition(_tables, out _, out var issues));
            Assert.That(issues, Has.Some.Matches<NarrativeError>(e => e.Code == "BadText" && e.CommandId == "line"));
        }

        [Test]
        public void E4DuplicateOrNonFiniteBeatsAndUnknownModeAreRejected()
        {
            foreach (var beats in new[] { new[] { new NovelTextBeat(1), new NovelTextBeat(1) },
                new[] { new NovelTextBeat(0, float.NaN) }, new[] { new NovelTextBeat(0, speed: float.PositiveInfinity) } })
                Assert.IsNotNull(NovelTextRules.Validate(new NovelCommand("line", NovelCommandKind.Say, "abc", "line", textBeats: beats)));
            Assert.IsNotNull(NovelTextRules.Validate(new NovelCommand("line", NovelCommandKind.Say, "abc", "line", textMode: (NovelTextMode)99)));
        }

        [Test]
        public void E4PagesNeedCompleteThenAdvanceAndOnlyFinalPageIsStable()
        {
            using var s = ReadingSession(out _, out _);
            var view = new TextView(); s.AttachView(view);
            s.Advance(1); Assert.AreEqual(4, view.Visible); Assert.AreEqual(0, s.TextPageStart);
            s.Advance(1); Assert.AreEqual(0, s.TextPageStart, "Same frame cannot complete and turn a page.");
            Assert.AreEqual(NarrativeState.Revealing, s.Snapshot.State); Assert.IsEmpty(s.History);
            Assert.IsFalse(s.TryCapture(out _, out _));
            Assert.That(s.Snapshot.Wait.HasFlag(NarrativeWait.Advance));
            s.Advance(2); Assert.AreEqual(4, s.TextPageStart);
            s.Advance(3); Assert.AreEqual(8, view.Visible); Assert.IsEmpty(s.History);
            s.Advance(4); Assert.AreEqual(8, s.TextPageStart);
            s.Advance(5); Assert.AreEqual(NarrativeState.AwaitingAdvance, s.Snapshot.State);
            Assert.AreEqual("1234567890", s.History.Single().Text);
            Assert.IsTrue(s.TryCapture(out _, out var error), error);
            s.Advance(6); Assert.AreEqual("hide", s.Snapshot.CommandId);
        }

        [Test]
        public void E4PauseAndMultiplierApplyToStructuredPauseWithoutDiscardingItsRemainder()
        {
            Commands(new NovelCommand("line", NovelCommandKind.Say, "0123456789", "line",
                textBeats: new[] { new NovelTextBeat(2, 1, 2) }));
            using var s = ReadingSession(out _, out _); var view = new TextView { Capacity = 20 }; s.AttachView(view);
            s.Tick(.2f, 1); Assert.AreEqual(2, view.Visible);
            using (s.AcquirePause("settings")) { s.Tick(100, 2); Assert.AreEqual(2, view.Visible); }
            s.SetReadingMultiplier(2); s.Tick(.25f, 3); Assert.AreEqual(2, view.Visible);
            s.Tick(.25f, 4); Assert.AreEqual(2, view.Visible);
            s.Tick(.1f, 5); Assert.AreEqual(6, view.Visible);
            s.Advance(6); Assert.AreEqual(NarrativeState.AwaitingAdvance, s.Snapshot.State);
        }

        [Test]
        public void E4AutoTurnsPagesDuringOneVoiceButFinalAdvanceWaitsForCompletion()
        {
            VoiceTable(); Commands(new NovelCommand("line", NovelCommandKind.Say, "0123456789", "line", resourceKey: "test_voice",
                textBeats: new[] { new NovelTextBeat(1, .2f, 1, 2) }));
            using var s = ReadingSession(out var resources, out var audio);
            resources.Clip.Asset = ReadingClip(); resources.Clip.IsDone = true;
            s.AttachView(new TextView()); s.SetReadMode(NarrativeReadMode.Auto);
            int frame = 1;
            for (; frame < 100 && s.Snapshot.State != NarrativeState.AwaitingAdvance; frame++) s.Tick(.2f, frame);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, s.Snapshot.State);
            Assert.AreEqual(8, s.TextPageStart); Assert.AreEqual(1, audio.Voices); Assert.AreEqual(1, resources.AudioLoads);
            s.Tick(100, frame++); Assert.AreEqual("line", s.Snapshot.CommandId);
            audio.Finish(); s.Tick(1.1f, frame++); Assert.AreEqual(NarrativeState.Ended, s.Snapshot.State);
        }

        [Test]
        public void E4ReadSkipCompletesAllPagesAndStopsBeforeUnreadText()
        {
            Commands(new NovelCommand("read", NovelCommandKind.Say, "0123456789", "read",
                textBeats: new[] { new NovelTextBeat(0, 60) }),
                new NovelCommand("unread", NovelCommandKind.Say, "下一句", "unread"));
            using var s = ReadingSession(out _, out _, (_, line, _) => line == "read");
            s.AttachView(new TextView()); s.SetReadMode(NarrativeReadMode.Skip); s.Tick(0, 1);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, s.Snapshot.State); Assert.AreEqual(8, s.TextPageStart);
            s.Tick(0, 2); Assert.AreEqual("unread", s.Snapshot.CommandId); Assert.AreEqual(NarrativeReadMode.Manual, s.ReadMode);
            Assert.AreEqual(1, s.History.Count); Assert.AreEqual(NarrativeState.Revealing, s.Snapshot.State);
        }

        [Test]
        public void E4StoryHiddenDoesNotPauseAndNextSayRestoresItIndependentlyOfPlayerHide()
        {
            Commands(new NovelCommand("hidden", NovelCommandKind.DialogueVisibility, dialogueVisible: false),
                new NovelCommand("silence", NovelCommandKind.Wait, duration: 1),
                new NovelCommand("line", NovelCommandKind.Say, "正文", "line", textMode: NovelTextMode.Title));
            using var s = ReadingSession(out _, out _); var view = new TextView(); s.AttachView(view);
            s.Tick(0, 1); Assert.IsFalse(view.StoryVisible); Assert.IsFalse(s.DialogueHidden); Assert.IsEmpty(s.Snapshot.PauseReasons);
            s.SetDialogueHidden(true); s.Tick(10, 2); Assert.AreEqual("silence", s.Snapshot.CommandId);
            s.Advance(3); s.Tick(10, 3); s.Tick(1.1f, 4);
            Assert.AreEqual("line", s.Snapshot.CommandId); Assert.IsTrue(view.StoryVisible); Assert.IsFalse(s.DialogueHidden);
            Assert.AreEqual(NovelTextMode.Title, view.Command.TextMode);
        }

        [Test]
        public void E4HiddenTerminalPresentationRestoresVisibilityAndDisposeResetsView()
        {
            Commands(new NovelCommand("hidden", NovelCommandKind.DialogueVisibility, dialogueVisible: false));
            var s = ReadingSession(out _, out var audio); var view = new TextView(); s.AttachView(view);
            s.Tick(0, 1); Assert.AreEqual(NarrativeState.Ended, s.Snapshot.State); Assert.IsTrue(view.StoryVisible);
            s.Dispose(); s.Dispose(); Assert.AreEqual(1, view.Clears); Assert.IsTrue(audio.Disposed);
        }

        [Test]
        public void E4StableRestoreReflowsToFinalPageWithoutReplayingBeatsVoiceOrHistory()
        {
            VoiceTable(); Commands(new NovelCommand("line", NovelCommandKind.Say, "0123456789", "line", resourceKey: "test_voice",
                textMode: NovelTextMode.FullScreen, textBeats: new[] { new NovelTextBeat(0, 60) }));
            using var original = ReadingSession(out _, out _); original.AttachView(new TextView());
            for (int frame = 1; frame <= 5; frame++) original.Advance(frame);
            Assert.IsTrue(original.TryCapture(out var saved, out var error), error);
            var resources = new ReadingResources { Story = _story }; var audio = new ReadingAudio();
            using var restored = new NovelSession(saved, () => _tables, resources, audio);
            restored.Tick(0, 6); restored.Tick(0, 7); Assert.IsTrue(restored.RestoreReady);
            var view = new TextView { Capacity = 3 }; restored.CommitRestore(view); restored.Tick(100, 8);
            Assert.AreEqual(9, restored.TextPageStart); Assert.AreEqual(int.MaxValue, view.Visible);
            Assert.AreEqual(NovelTextMode.FullScreen, view.Command.TextMode); Assert.AreEqual(1, restored.History.Count);
            Assert.AreEqual(0, resources.AudioLoads); Assert.AreEqual(0, audio.Voices);
            restored.Advance(9); Assert.AreEqual(NarrativeState.Ended, restored.Snapshot.State);
        }

        [Test]
        public void E4ReflowDoesNotLoseTextOrDuplicateHistory()
        {
            using var s = ReadingSession(out _, out _); var view = new TextView(); s.AttachView(view);
            s.Advance(1); s.Advance(2); s.Tick(.1f, 3);
            view.Capacity = 2; s.Tick(0, 4);
            for (int frame = 5; frame < 25 && s.Snapshot.State != NarrativeState.AwaitingAdvance; frame++) s.Advance(frame);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, s.Snapshot.State); Assert.AreEqual(1, s.History.Count);
            Assert.AreEqual("1234567890", s.History[0].Text);
        }

        [Test]
        public void E4SemanticsAndEditorCopyIncludeBeatsAndMode()
        {
            Commands(new NovelCommand("line", NovelCommandKind.Say, "literal <pause>", "line"));
            Assert.IsTrue(_story.TryReadDefinition(_tables, out var original, out _));
            string before = NovelCompatibility.Fingerprint(original);
            Commands(new NovelCommand("line", NovelCommandKind.Say, "literal <pause>", "line", textMode: NovelTextMode.Title,
                textBeats: new[] { new NovelTextBeat(3, .5f) }));
            Assert.IsTrue(_story.TryReadDefinition(_tables, out var changed, out _));
            Assert.AreNotEqual(before, NovelCompatibility.Fingerprint(changed));
            using var copy = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
            Assert.AreEqual(NovelTextMode.Title, copy.Dialogue.Commands[0].TextMode);
            Assert.AreEqual(.5f, copy.Dialogue.Commands[0].TextBeats[0].Pause);
            Assert.AreEqual("literal <pause>", copy.Dialogue.Commands[0].Text);
        }

        [Test]
        public void E4HistoryKeepsLiteralMarkupAndDoesNotInterpretTextAsFormatting()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelHistoryPage.prefab");
            Assert.IsNotNull(prefab);
            var root = UnityEngine.Object.Instantiate(prefab);
            Ember.UI.EUIPage page = null;
            try
            {
                page = new Ember.UI.EUIPage(root);
                Ember.UIExtension.EUIBindingBridge.Attach(page, root.GetComponent<Ember.UIExtension.EUIBinding>());
                page.Logic.OnInit();
                var method = page.Logic.GetType().GetMethod("ShowHistory", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                const string literal = "<color=red>原文</color> </noparse>";
                method.Invoke(page.Logic, new object[] { new[] { new NovelHistoryEntry { Text = literal } } });
                var entries = (TMP_Text)page.Logic.ControlMap["Entries"];
                entries.ForceMeshUpdate(true);
                Assert.That(entries.GetParsedText(), Does.Contain(literal));
            }
            finally { page?.Logic.OnDispose(); UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        public void E4FormalAndEditorTextSharePaginationRestoreLayoutAndLeavePrefabUntouched(int width, int height)
        {
            const string path = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
            var before = AssetDatabase.GetAssetDependencyHash(path);
            var preview = new PreviewRenderUtility(); var host = new GameObject("E4 text preview"); preview.AddSingleGO(host);
            NovelPlaybackView view = null; NovelSession session = null;
            try
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), host.transform);
                view = new NovelPlaybackView(root, preview.camera, new Vector2(width, height));
                var body = root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Body");
                var speaker = root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Speaker");
                var originalMin = body.rectTransform.anchorMin; var originalMax = body.rectTransform.anchorMax;
                var dialogue = (RectTransform)body.transform.parent;
                var dialogueParent = dialogue.parent; int dialogueSibling = dialogue.GetSiblingIndex();
                var advance = (RectTransform)root.transform.Find("ReadingShading/Advance");
                var advanceMin = advance.anchorMin; var advanceMax = advance.anchorMax;
                string longText = string.Concat(Enumerable.Repeat("窗外的灯亮起来。你把尚未寄出的信轻轻放在桌上。\n", 60));
                var longCommand = new NovelCommand("long", NovelCommandKind.Say, longText, "long", textMode: NovelTextMode.FullScreen);
                int end = view.PrepareText(longCommand, 0);
                Assert.Greater(end, 0); Assert.Less(end, NovelTextRules.Length(longText));
                view.ShowText("旁白", int.MaxValue); view.Flush(); Assert.IsFalse(speaker.gameObject.activeSelf);
                Assert.AreEqual(TextOverflowModes.Page, body.overflowMode); Assert.IsFalse(body.richText);
                var formal = (INovelView)typeof(NovelPlaybackView).GetField("_visual", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
                var baseFont = formal.GetType().GetField("_bodyFontSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                float normalFont = (float)baseFont.GetValue(formal);
                baseFont.SetValue(formal, normalFont * 1.2f);
                Assert.Less(view.PrepareText(longCommand, 0), end, "A larger font must invalidate measured page boundaries.");
                baseFont.SetValue(formal, normalFont);
                var ordinary = new NovelCommand("line", NovelCommandKind.Say, "正文", "line");
                view.PrepareText(ordinary, 0); view.ShowText("林晚", int.MaxValue);
                Assert.AreEqual(originalMin, body.rectTransform.anchorMin); Assert.AreEqual(originalMax, body.rectTransform.anchorMax);
                Assert.IsTrue(speaker.gameObject.activeSelf);
                Commands(new NovelCommand("title", NovelCommandKind.Say, "最后一盏灯", "title", textMode: NovelTextMode.Title), longCommand, ordinary);
                using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story);
                session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
                session.AttachView(view); session.Tick(0, 0);
                Assert.AreEqual(TextAlignmentOptions.Center, body.alignment);
                formal.Render(session.Snapshot, data.Dialogue.Commands[0], "", 2, "");
                Assert.AreEqual("最后一盏灯", body.text); Assert.AreEqual(2, body.maxVisibleCharacters);
                view.Flush();
                var screenCorners = new Vector3[4]; var dialogueCorners = new Vector3[4];
                ((RectTransform)root.transform).GetWorldCorners(screenCorners); dialogue.GetWorldCorners(dialogueCorners);
                for (int corner = 0; corner < 4; corner++)
                    Assert.Less(Vector3.Distance(screenCorners[corner], dialogueCorners[corner]), .01f, "Chapter background must fill the entire canvas.");
                Assert.AreEqual(1f, dialogue.GetComponent<UnityEngine.UI.Image>().color.a);
                Assert.AreEqual(Vector2.zero, advance.anchorMin); Assert.AreEqual(Vector2.one, advance.anchorMax);
                Assert.IsFalse(root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ReadingControls").gameObject.activeSelf);
                Assert.Less(Vector3.Distance(body.rectTransform.TransformPoint(body.rectTransform.rect.center),
                    ((RectTransform)root.transform).TransformPoint(((RectTransform)root.transform).rect.center)), .01f);
                Assert.IsFalse(body.raycastTarget);
                int frame = 1;
                for (; frame < 2000 && session.Snapshot.State != NarrativeState.Ended; frame++)
                { session.Advance(frame); session.Tick(.01f, frame); view.Flush(); }
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString());
                Assert.AreEqual(3, session.History.Count); Assert.AreEqual(longText, session.History[1].Text);
                Assert.AreSame(dialogueParent, dialogue.parent); Assert.AreEqual(dialogueSibling, dialogue.GetSiblingIndex());
                Assert.AreEqual(advanceMin, advance.anchorMin); Assert.AreEqual(advanceMax, advance.anchorMax);
                Assert.AreEqual(originalMin, body.rectTransform.anchorMin); Assert.AreEqual(originalMax, body.rectTransform.anchorMax);
                session.Dispose(); session = null; view.Dispose(); view.Dispose(); view = null;
                Assert.AreEqual(before, AssetDatabase.GetAssetDependencyHash(path));
            }
            finally { session?.Dispose(); view?.Dispose(); preview.Cleanup(); }
        }
    }
}
