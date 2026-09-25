using System;
using System.Linq;
using Game.Narrative.Editor;
using Game.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed partial class ActionView : INovelCameraView, INovelWipeView
        {
            public Vector2 CameraOffset;
            public float CameraZoom = 1, WipeProgress;
            public NovelWipeDirection WipeDirection;
            public void SetCamera(Vector2 offset, float zoom) { CameraOffset = offset; CameraZoom = zoom; Writes++; }
            public void SetWipe(float progress, NovelWipeDirection direction) { WipeProgress = progress; WipeDirection = direction; }
        }
        private NovelCommand CameraAction(string id, float zoom = 1.4f, float delay = 0) =>
            new(id, NovelCommandKind.Camera, actionId: id, cameraZoom: zoom, duration: 2, delay: delay, parallel: true);
        private NovelCommand WipeAction(string id, string key, float delay = 0) =>
            new(id, NovelCommandKind.Wipe, actionId: id, resourceKey: key, targetKind: NovelTargetKind.Background,
                duration: 2, delay: delay, parallel: true, wipeDirection: NovelWipeDirection.RightToLeft);

        [Test]
        public void E5CameraDelayPauseMultiplierComposesWithMoveAndShake()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                MoveActor("move", NovelPortraitSlot.Left), Shake("shake"), CameraAction("camera", delay: .5f), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            session.Tick(.5f, ++frame); Assert.AreEqual(1, view.CameraZoom);
            session.Pause("menu"); session.Tick(10, ++frame); Assert.AreEqual(1, view.CameraZoom);
            session.Resume("menu"); session.SetReadingMultiplier(2); session.Tick(.25f, ++frame);
            Assert.AreEqual(1.1f, view.CameraZoom, .0001);
            Assert.AreEqual(-.12f, view.Actors["actor"].Offset.x, .0001);
            session.Tick(1, ++frame); Assert.AreEqual(1.4f, view.CameraZoom, .0001);
            Assert.AreEqual(Vector2.zero, view.Shakes[(NovelTargetKind.Character, NovelPortraitSlot.Center)]);
            Assert.IsTrue(session.Actions.All(a => a.IsFinished));
            session.Dispose(); Assert.AreEqual(1, view.CameraZoom); Assert.AreEqual(0, session.OwnedResourceCount);
        }

        [Test]
        public void E5CameraTakeoverStartsAtCurrentValueAndWaitIncludesCancelledAction()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, CameraAction("old"), new NovelCommand("timer", NovelCommandKind.Wait, duration: 1),
                CameraAction("new", 1), new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "old", "new" }), SayAction("done"));
            int frame = 0; PumpAction(session, "timer", ref frame); session.Tick(1, ++frame); PumpAction(session, "join", ref frame);
            Assert.AreEqual(1.2f, view.CameraZoom, .0001);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "old").Status);
            session.Tick(1, ++frame); Assert.AreEqual(1.1f, view.CameraZoom, .0001);
            session.Tick(1, ++frame); PumpAction(session, "done", ref frame); Assert.AreEqual(1, view.CameraZoom);
        }

        [TestCase(NovelCheckpoint.CurrentSchemaVersion, 1.4f)]
        [TestCase(5, 1f)]
        public void E5CaptureProjectsCameraWithoutAdvancingLiveAndOldSchemaRestoresNeutral(int schema, float expected)
        {
            using var view = new ActionView();
            using var session = ActionSession(view, CameraAction("camera"), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Tick(.5f, ++frame);
            Assert.IsTrue(session.TryCapture(out var save, out var error), error);
            Assert.AreEqual(NovelCheckpoint.CurrentSchemaVersion, save.SchemaVersion); Assert.AreEqual(1.4f, save.CameraZoom); Assert.AreEqual(1.1f, view.CameraZoom, .0001);
            save.SchemaVersion = schema;
            using var restored = new NovelSession(save, () => _tables, new ActionResources { Story = _story });
            restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
            session.Dispose(); restored.CommitRestore(view); Assert.AreEqual(expected, view.CameraZoom, .0001);
            restored.Tick(20, ++frame); Assert.AreEqual(expected, view.CameraZoom, .0001, "Restore must not replay the camera event");
        }

        [Test]
        public void E5InvalidCameraCheckpointIsRejectedBeforeTouchingLiveView()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, SayAction("line")); int frame = 0; PumpAction(session, "line", ref frame); session.Advance(++frame);
            Assert.IsTrue(session.TryCapture(out var save, out var error), error); save.CameraOffset = Vector2.one;
            using var restored = new NovelSession(save, () => _tables, new ActionResources { Story = _story });
            restored.Tick(0, ++frame); restored.Tick(0, ++frame);
            Assert.IsFalse(restored.RestoreReady); Assert.IsNotNull(restored.Snapshot.Error); Assert.AreEqual(1, view.CameraZoom);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void E5WipeOwnsBothLeasesDuringLoadAndDelayAndReleasesOnFinishOrFailure(bool fail)
        {
            Assert.IsTrue(_tables.TryResolve(NovelCommandKind.Background, "lastlight_rooftop", out var oldPath));
            Assert.IsTrue(_tables.TryResolve(NovelCommandKind.Background, "lastlight_rooftop_night", out var newPath));
            var resources = new ScreenResources(); var old = ScreenSprite(); var next = ScreenSprite(); next.IsDone = false;
            resources.Sprites[oldPath] = old; resources.Sprites[newPath] = next;
            using var view = new ActionView();
            using var session = ScreenSession(view, resources,
                new NovelCommand("bg", NovelCommandKind.Background, resourceKey: "lastlight_rooftop"),
                WipeAction("wipe", "lastlight_rooftop_night", .5f), SayAction("line"));
            int frame = 0; PumpAction(session, "wipe", ref frame); session.Tick(0, ++frame);
            Assert.IsFalse(view.Blending); Assert.IsFalse(old.Disposed); Assert.IsFalse(next.Disposed);
            next.IsDone = true; if (fail) { next.Asset = null; next.Error = "missing wipe image"; }
            if (fail)
            { session.Tick(0, ++frame); Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State); Assert.IsFalse(view.Blending); Assert.IsFalse(old.Disposed); }
            else
            {
                PumpAction(session, "line", ref frame); Assert.IsTrue(view.Blending); session.Tick(.5f, ++frame); Assert.AreEqual(0, view.WipeProgress);
                session.Tick(1, ++frame); Assert.AreEqual(.5f, view.WipeProgress, .0001); Assert.IsFalse(old.Disposed);
                Assert.AreEqual(NovelWipeDirection.RightToLeft, view.WipeDirection);
                Assert.IsTrue(session.TryCapture(out var save, out var error), error);
                Assert.AreEqual("lastlight_rooftop_night", save.Visuals.Single().Key);
                session.Tick(1, ++frame); Assert.IsFalse(view.Blending); Assert.IsTrue(old.Disposed); Assert.IsFalse(next.Disposed);
                using var restored = new NovelSession(save, () => _tables, new ActionResources { Story = _story, Sprite = next.Asset });
                restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
                session.Dispose(); restored.CommitRestore(view); restored.Tick(.5f, ++frame);
                Assert.IsFalse(view.Blending, "Restoring must show the destination without replaying the wipe");
                Assert.AreSame(next.Asset, view.Picture.sprite);
            }
            session.Dispose(); Assert.IsTrue(old.Disposed); Assert.IsTrue(next.Disposed); Assert.AreEqual(0, session.OwnedResourceCount);
        }

        [Test]
        public void E5WipeAndCrossFadeShareImageOwnershipAndSkipSettlesDestination()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, new NovelCommand("bg", NovelCommandKind.Background, resourceKey: "lastlight_rooftop"),
                WipeAction("wipe", "lastlight_rooftop_night"), new NovelCommand("timer", NovelCommandKind.Wait, duration: .5f),
                new NovelCommand("blend", NovelCommandKind.CrossFade, actionId: "blend", resourceKey: "lastlight_rooftop",
                    targetKind: NovelTargetKind.Background, duration: 2, parallel: true), CameraAction("camera"), SayAction("line"));
            int frame = 0; PumpAction(session, "timer", ref frame); session.Tick(.5f, ++frame); PumpAction(session, "line", ref frame);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "wipe").Status);
            session.ConfigureReading((story, line, revision) => true, 30, 0, 1, 1, 1);
            session.SetReadMode(NarrativeReadMode.Skip); session.Tick(.01f, ++frame);
            Assert.IsFalse(view.Blending); Assert.AreEqual(1.4f, view.CameraZoom); Assert.IsTrue(session.Actions.All(a => a.IsFinished));
        }

        [Test]
        public void E5PresetExpansionProducesEditableIndependentCommandsAndMatchingWaits()
        {
            foreach (NarrativePresetKind kind in Enum.GetValues(typeof(NarrativePresetKind)))
            {
                var a = NarrativePresentationPresets.Build(kind, "first", "actor", "alice_neutral", NovelPortraitSlot.Left, 1, 1);
                var b = NarrativePresentationPresets.Build(kind, "second", "actor", "alice_neutral", NovelPortraitSlot.Left, 2, 2);
                Assert.AreEqual(a.Where(c => !string.IsNullOrEmpty(c.ActionId)).Select(c => c.ActionId), a.Last().WaitActions);
                Assert.IsFalse(a.Select(c => c.CommandId).Intersect(b.Select(c => c.CommandId)).Any());
                Assert.IsTrue(a.Where(c => !string.IsNullOrEmpty(c.ActionId)).All(c => c.Parallel && c.Duration == 1));
                foreach (var c in a) { Assert.IsNull(NovelCameraRules.Validate(c)); Assert.IsNull(NovelActorRules.Validate(c)); Assert.IsNull(NovelScreenRules.Validate(c)); }
            }
            Assert.Throws<ArgumentException>(() => NarrativePresentationPresets.Build(NarrativePresetKind.Entrance, "p", "", "portrait", default, 1, 1));
            Assert.Throws<ArgumentException>(() => NarrativePresentationPresets.Build(NarrativePresetKind.Memory, "p", "", "", default, float.NaN, 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void E5PreviewDrainsCameraTailAndLeavesSourceUntouched(bool wipe)
        {
            Commands(wipe ? new[] { new NovelCommand("bg", NovelCommandKind.Background, resourceKey: "lastlight_rooftop"),
                WipeAction("wipe", "lastlight_rooftop_night"), CameraAction("camera") } : new[] { CameraAction("camera") });
            string before = EditorJsonUtility.ToJson(ReadingTalk);
            using var data = new NovelPlaybackStory(ReadingTalk, _story.Entry, _story); using var view = new ActionView();
            using var session = new NovelSession(new NovelNewGameRequest("preview"), () => _tables, data, new ReadingAudio());
            session.AttachView(view); int frame = 0;
            for (; frame < 30 && session.Snapshot.CommandId != data.DrainCommandId; frame++) session.Tick(0, frame);
            Assert.AreEqual(data.DrainCommandId, session.Snapshot.CommandId); session.Pause("preview"); session.Tick(20, ++frame); Assert.AreEqual(1, view.CameraZoom);
            session.Resume("preview");
            for (int i = 0; i < 100 && session.Snapshot.State != NarrativeState.Ended; i++) session.Tick(.05f, ++frame);
            Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State, session.Snapshot.Error?.ToString()); Assert.AreEqual(1.4f, view.CameraZoom);
            Assert.AreEqual(before, EditorJsonUtility.ToJson(ReadingTalk)); session.Dispose(); Assert.AreEqual(1, view.CameraZoom);
        }

        [TestCase(1920, 1080)]
        [TestCase(1440, 1080)]
        public void E5FormalPreviewCameraKeepsReaderFixedWipeRestoresStyleAndCleanupResetsStage(int width, int height)
        {
            const string path = "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab";
            var hash = AssetDatabase.GetAssetDependencyHash(path); var preview = new PreviewRenderUtility();
            var host = new GameObject("E5 preview test"); preview.AddSingleGO(host); NovelPlaybackView view = null;
            try
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), host.transform);
                view = new NovelPlaybackView(root, preview.camera, new Vector2(width, height)); view.Flush();
                var bg = (RectTransform)root.transform.Find("Background"); var actor = (RectTransform)root.transform.Find("Center");
                var text = root.GetComponentsInChildren<TMPro.TMP_Text>(true).First().rectTransform;
                var position = text.position; var scale = text.lossyScale; var bgMin = bg.anchorMin; var bgScale = bg.localScale;
                var actorMin = actor.anchorMin; var actorScale = actor.localScale;
                var originalCorners = new Vector3[4]; bg.GetWorldCorners(originalCorners);
                ((INovelCameraView)view).SetCamera(new Vector2(.1f, 0), 1.4f); view.Flush();
                Assert.AreEqual(position, text.position); Assert.AreEqual(scale, text.lossyScale);
                var corners = new Vector3[4]; bg.GetWorldCorners(corners);
                Assert.LessOrEqual(corners[0].x, originalCorners[0].x); Assert.LessOrEqual(corners[0].y, originalCorners[0].y);
                Assert.GreaterOrEqual(corners[2].x, originalCorners[2].x); Assert.GreaterOrEqual(corners[2].y, originalCorners[2].y);
                ((INovelActorView)view).ApplyActor(new NovelVisualState { Kind = NovelCommandKind.Character, InstanceId = "actor",
                    Slot = NovelPortraitSlot.Center, Offset = new Vector2(.05f, 0), Scale = Vector3.one, Brightness = 1 }, Vector2.zero, 0);
                Assert.AreEqual(actorMin.x + .17f, actor.anchorMin.x, .0001, "Actor offset must be scaled by camera zoom");
                Assert.AreEqual(bgScale.x * 1.4f, bg.localScale.x, .0001); Assert.AreEqual(actorScale.x * 1.4f, actor.localScale.x, .0001);
                ((INovelScreenView)view).SetShake(NovelTargetKind.Stage, default, new Vector2(.01f, 0));
                Assert.AreEqual(bgMin.x + .11f, bg.anchorMin.x, .0001);
                var image = root.transform.Find("Background/CrossFade/Picture").GetComponent<Image>();
                image.type = Image.Type.Sliced; var sprite = ScreenSprite().Asset;
                view.Visual(new NovelCommand("bg", NovelCommandKind.Background), sprite, 1);
                ((INovelScreenView)view).BeginCrossFade(NovelTargetKind.Background, default, sprite);
                ((INovelWipeView)view).SetWipe(.5f, NovelWipeDirection.TopToBottom);
                Assert.AreEqual(Image.Type.Filled, image.type); Assert.AreEqual(Image.FillMethod.Vertical, image.fillMethod); Assert.AreEqual(1, image.fillOrigin); Assert.AreEqual(.5f, image.fillAmount);
                ((INovelScreenView)view).EndCrossFade(NovelTargetKind.Background, default); Assert.AreEqual(Image.Type.Sliced, image.type);
                view.ClearVisuals(); Assert.AreEqual(bgMin, bg.anchorMin); Assert.AreEqual(bgScale, bg.localScale);
                Assert.AreEqual(actorMin, actor.anchorMin); Assert.AreEqual(actorScale, actor.localScale);
                view.Dispose(); view = null; Assert.IsNull(root.transform.Find("Background/CrossFade"));
            }
            finally { view?.Dispose(); preview.Cleanup(); }
            Assert.AreEqual(hash, AssetDatabase.GetAssetDependencyHash(path));
        }

        [Test]
        public void E5CompatibilityChangesWithCameraAndWipeDirectionAndInvalidDirectionIsRejected()
        {
            string Fingerprint(float zoom, NovelWipeDirection direction)
            {
                Commands(new NovelCommand("camera", NovelCommandKind.Camera, actionId: "camera", cameraZoom: zoom),
                    new NovelCommand("bg", NovelCommandKind.Background, resourceKey: "lastlight_rooftop"),
                    new NovelCommand("wipe", NovelCommandKind.Wipe, actionId: "wipe", targetKind: NovelTargetKind.Background,
                        resourceKey: "lastlight_rooftop_night", wipeDirection: direction), SayAction("line"));
                Assert.IsTrue(_story.TryReadDefinition(_tables, out var definition, out var errors), string.Join("\n", errors));
                return NovelCompatibility.Fingerprint(definition);
            }
            string baseline = Fingerprint(1.2f, NovelWipeDirection.LeftToRight);
            Assert.AreNotEqual(baseline, Fingerprint(1.3f, NovelWipeDirection.LeftToRight));
            Assert.AreNotEqual(baseline, Fingerprint(1.2f, NovelWipeDirection.RightToLeft));
            Assert.IsNotNull(NovelScreenRules.Validate(new NovelCommand("wipe", NovelCommandKind.Wipe, actionId: "wipe",
                targetKind: NovelTargetKind.Background, wipeDirection: (NovelWipeDirection)99)));
        }

        [TestCase(1f, 0f, true)]
        [TestCase(1.4f, .2f, true)]
        [TestCase(1.4f, .21f, false)]
        [TestCase(.9f, 0f, false)]
        [TestCase(3.1f, 0f, false)]
        public void E5CameraBoundsKeepFullRectBackgroundCoveringViewport(float zoom, float x, bool valid)
        {
            Assert.AreEqual(valid, NovelCameraRules.Valid(new Vector2(x, 0), zoom));
            if (valid) { Assert.LessOrEqual(.5f + x - zoom * .5f, .00001f); Assert.GreaterOrEqual(.5f + x + zoom * .5f, .99999f); }
            Assert.IsFalse(NovelCameraRules.Valid(new Vector2(float.NaN, 0), 1));
        }
    }
}
