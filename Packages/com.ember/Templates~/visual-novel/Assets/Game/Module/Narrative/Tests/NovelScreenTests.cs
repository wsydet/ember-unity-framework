using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private sealed partial class ActionView : INovelScreenView
        {
            public readonly Dictionary<(NovelTargetKind, NovelPortraitSlot), Vector2> Shakes = new();
            public Color Cover;
            public bool WholeReader, Blending;
            public float BlendProgress;
            public Sprite NextSprite;
            public void SetShake(NovelTargetKind kind, NovelPortraitSlot slot, Vector2 offset) => Shakes[(kind, slot)] = offset;
            public void SetCover(Color color, bool wholeReader) { Cover = color; WholeReader = wholeReader; }
            public void BeginCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, Sprite sprite) { Blending = true; BlendProgress = 0; NextSprite = sprite; }
            public void SetCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, float progress) => BlendProgress = progress;
            public void EndCrossFade(NovelTargetKind kind, NovelPortraitSlot slot) { Blending = false; BlendProgress = 1; }
            private void ClearScreen() { CameraOffset = Vector2.zero; CameraZoom = 1; WipeProgress = 0; Shakes.Clear(); Cover = Color.clear; Blending = false; NextSprite = null; }
        }
        private sealed class ScreenResources : INovelResources
        {
            public NarrativeStorySO Story;
            public readonly Dictionary<string, Lease<Sprite>> Sprites = new();
            public INovelAssetLease<T> Load<T>(string path) where T : Object => typeof(T) == typeof(NarrativeStorySO) ?
                (INovelAssetLease<T>)(object)new Lease<NarrativeStorySO> { Asset = Story, IsDone = true } :
                (INovelAssetLease<T>)(object)Sprites[path];
        }
        private NovelCommand Shake(string id, NovelTargetKind kind = NovelTargetKind.Character, float delay = 0) =>
            new(id, NovelCommandKind.Shake, instanceId: "actor", targetKind: kind, actionId: id, duration: 2,
                parallel: true, frequency: 2, decay: false, delay: delay);
        private NovelCommand Blend(string id, string key, NovelTargetKind kind = NovelTargetKind.Character) =>
            new(id, NovelCommandKind.CrossFade, instanceId: "actor", targetKind: kind, resourceKey: key, actionId: id, duration: 2, parallel: true);

        [Test]
        public void E2ShakeAddsToMovePausePreferenceMultiplierAndCapture()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center), MoveActor("move", NovelPortraitSlot.Left),
                Shake("actor-shake"), Shake("stage-shake", NovelTargetKind.Stage), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Tick(.125f, ++frame);
            var key = (NovelTargetKind.Character, NovelPortraitSlot.Center);
            Assert.AreEqual(.02f, view.Shakes[key].x, .0001);
            Assert.AreEqual(-.015f, view.Actors["actor"].Offset.x, .0001);
            session.Pause("menu"); session.Tick(20, ++frame); Assert.AreEqual(.02f, view.Shakes[key].x, .0001);
            session.ConfigureScreenEffects(NovelEffectPreference.Off, NovelEffectPreference.Normal);
            Assert.AreEqual(Vector2.zero, view.Shakes[key]);
            session.Resume("menu"); session.Advance(++frame);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            Assert.AreEqual(1, checkpoint.StageOpacity, "Stage Shake must not overwrite opacity in the checkpoint");
            Assert.AreEqual(-.24f, checkpoint.Visuals.Single().Offset.x, .0001);
            Assert.AreEqual(-.015f, view.Actors["actor"].Offset.x, .0001, "Capture cannot advance live animation");
            session.SetReadingMultiplier(2); session.Tick(1, ++frame);
            Assert.IsTrue(session.Actions.All(a => a.IsFinished)); Assert.AreEqual(Vector2.zero, view.Shakes[key]);
            Assert.AreEqual(-.24f, view.Actors["actor"].Offset.x, .0001);
        }

        [Test]
        public void E2ShakeTakeoverClearsOffsetDuringNewDelayAndWaitUnblocks()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Left), Shake("old"),
                new NovelCommand("timer", NovelCommandKind.Wait, duration: .125f), Shake("new", delay: 1),
                new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "old", "new" }), SayAction("done"));
            int frame = 0; PumpAction(session, "timer", ref frame); session.Tick(.125f, ++frame);
            Assert.Greater(view.Shakes[(NovelTargetKind.Character, NovelPortraitSlot.Left)].x, 0);
            PumpAction(session, "join", ref frame);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "old").Status);
            Assert.AreEqual(Vector2.zero, view.Shakes[(NovelTargetKind.Character, NovelPortraitSlot.Left)]);
            session.Tick(3, ++frame); PumpAction(session, "done", ref frame);
            session.Dispose(); Assert.IsEmpty(view.Shakes); Assert.AreEqual(0, session.OwnedResourceCount);
        }

        [Test]
        public void E2CoverHoldFlashOffAndRestoreStableCover()
        {
            using var view = new ActionView();
            using var session = ActionSession(view,
                new NovelCommand("cover", NovelCommandKind.Cover, actionId: "cover", color: Color.black, opacity: .3f),
                new NovelCommand("flash", NovelCommandKind.Flash, actionId: "flash", color: Color.white, duration: 2, hold: 1, parallel: true, wholeReader: true), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Tick(1, ++frame);
            Assert.AreEqual(Color.white, view.Cover); Assert.IsTrue(view.WholeReader);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            Assert.AreEqual(.3f, checkpoint.CoverColor.a); Assert.AreEqual(0, checkpoint.CoverColor.r); Assert.IsFalse(checkpoint.CoverWholeReader);
            session.Pause("menu"); session.Tick(10, ++frame); Assert.AreEqual(Color.white, view.Cover);
            session.ConfigureScreenEffects(NovelEffectPreference.Normal, NovelEffectPreference.Off);
            Assert.AreEqual(.3f, view.Cover.a); Assert.IsFalse(view.WholeReader);
            session.Resume("menu"); session.Tick(.5f, ++frame); Assert.IsFalse(session.Actions.Single(a => a.Id == "flash").IsFinished);
            session.Tick(1.5f, ++frame); Assert.AreEqual(.3f, view.Cover.a); Assert.AreEqual(0, view.Cover.r);
            using var restored = new NovelSession(checkpoint, () => _tables, new ActionResources { Story = _story });
            restored.Tick(0, ++frame); restored.Tick(0, ++frame);
            Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString()); session.Dispose(); restored.CommitRestore(view);
            Assert.AreEqual(.3f, view.Cover.a); Assert.IsFalse(view.WholeReader); Assert.IsTrue(restored.Actions.All(a => a.IsFinished));
        }

        [Test]
        public void E2CoverTakeoverAndSkipKeepDeterministicFinalState()
        {
            using var view = new ActionView();
            using var session = ActionSession(view,
                new NovelCommand("old", NovelCommandKind.Cover, actionId: "old", color: Color.white, duration: 2, parallel: true),
                new NovelCommand("time", NovelCommandKind.Wait, duration: 1),
                new NovelCommand("new", NovelCommandKind.Cover, actionId: "new", color: Color.white, opacity: 0, duration: 2, hold: 1, parallel: true),
                new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "old", "new" }), SayAction("done"));
            int frame = 0; PumpAction(session, "time", ref frame); session.Tick(1, ++frame);
            Assert.AreEqual(.5f, view.Cover.a, .001); PumpAction(session, "join", ref frame);
            Assert.AreEqual(.5f, view.Cover.a, .001); Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "old").Status);
            session.Tick(2, ++frame); Assert.AreEqual(0, view.Cover.a); Assert.IsFalse(session.Actions.Single(a => a.Id == "new").IsFinished);
            session.SetReadMode(NarrativeReadMode.Skip); session.Tick(0, ++frame); PumpAction(session, "done", ref frame);
            Assert.AreEqual(0, view.Cover.a); Assert.IsTrue(session.Actions.All(a => a.IsFinished));
        }

        private NovelSession ScreenSession(ActionView view, ScreenResources resources, params NovelCommand[] commands)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = commands.ToList() }), _story.Entry.Entry);
            resources.Story = _story;
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            session.AttachView(view); session.Tick(0, 0); return session;
        }
        private Lease<Sprite> ScreenSprite()
        {
            var texture = new Texture2D(2, 2); _assets.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); _assets.Add(sprite);
            return new Lease<Sprite> { Asset = sprite, IsDone = true };
        }

        [Test]
        public void E2CrossFadeKeepsBothLeasesUntilEndAndCaptureUsesDestination()
        {
            Assert.IsTrue(_tables.TryResolve(NovelCommandKind.Character, "alice_neutral", out var oldPath));
            Assert.IsTrue(_tables.TryResolve(NovelCommandKind.Character, "lin_neutral", out var newPath));
            var resources = new ScreenResources(); var old = ScreenSprite(); var next = ScreenSprite();
            resources.Sprites[oldPath] = old; resources.Sprites[newPath] = next;
            using var view = new ActionView();
            using var session = ScreenSession(view, resources, ShowAction("actor", NovelPortraitSlot.Left),
                Blend("blend", "lin_neutral"), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Tick(1, ++frame);
            Assert.IsTrue(view.Blending); Assert.AreEqual(.5f, view.BlendProgress); Assert.IsFalse(old.Disposed); Assert.IsFalse(next.Disposed);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error); Assert.AreEqual("lin_neutral", checkpoint.Visuals.Single().Key);
            Assert.IsTrue(view.Blending); session.Tick(1, ++frame);
            Assert.IsFalse(view.Blending); Assert.IsTrue(old.Disposed); Assert.IsFalse(next.Disposed);
            session.Dispose(); Assert.IsTrue(next.Disposed);
        }

        [Test]
        public void E2MissingBlendKeepsOldPictureAndDisposeReleasesPendingLoad()
        {
            _tables.TryResolve(NovelCommandKind.Character, "alice_neutral", out var oldPath);
            _tables.TryResolve(NovelCommandKind.Character, "lin_neutral", out var newPath);
            var resources = new ScreenResources(); var old = ScreenSprite(); var next = new Lease<Sprite>();
            resources.Sprites[oldPath] = old; resources.Sprites[newPath] = next;
            using var view = new ActionView();
            using var session = ScreenSession(view, resources, ShowAction("actor", NovelPortraitSlot.Left), Blend("missing", "lin_neutral"), SayAction("line"));
            int frame = 0; PumpAction(session, "missing", ref frame); session.Tick(0, ++frame);
            Assert.IsTrue(session.Snapshot.Wait.HasFlag(NarrativeWait.Resource)); Assert.IsFalse(view.Blending);
            Assert.IsFalse(old.Disposed); next.Error = "missing"; next.IsDone = true; session.Tick(0, ++frame);
            Assert.AreEqual(NarrativeState.Faulted, session.Snapshot.State); Assert.AreSame(old.Asset, view.Picture.sprite);
            StringAssert.Contains("lin_neutral", session.Snapshot.Error.Message);
            session.Dispose(); Assert.IsTrue(next.Disposed); Assert.IsTrue(old.Disposed);
        }

        [Test]
        public void E2ValidationAndFingerprintsIncludeScreenParameters()
        {
            Assert.IsNotNull(NovelScreenRules.Validate(new NovelCommand("s", NovelCommandKind.Shake, actionId: "s", direction: Vector2.zero)));
            Assert.IsNotNull(NovelScreenRules.Validate(new NovelCommand("s", NovelCommandKind.Shake, actionId: "s", frequency: float.NaN)));
            Assert.IsNotNull(NovelScreenRules.Validate(new NovelCommand("c", NovelCommandKind.Cover, actionId: "c", hold: float.PositiveInfinity)));
            Assert.IsNotNull(NovelScreenRules.Validate(new NovelCommand("x", NovelCommandKind.CrossFade, actionId: "x")));
            Assert.IsFalse(NovelScreenRules.ValidColor(new Color(float.NaN, 0, 0, 0)));
            string Fingerprint(float frequency)
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = new List<NovelCommand> {
                    new("s", NovelCommandKind.Shake, actionId: "s", frequency: frequency), SayAction("line") } }), _story.Entry.Entry);
                Assert.IsTrue(_story.TryReadDefinition(_tables, out var definition, out var errors), string.Join("\n", errors));
                return NovelCompatibility.Fingerprint(definition);
            }
            Assert.AreNotEqual(Fingerprint(2), Fingerprint(3), "Stage actions with no instance ID must still affect compatibility");
        }

        [Test]
        public void E2NormalReplaceCancelsCrossFadeWhileMoveKeepsRunning()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center), MoveActor("move", NovelPortraitSlot.Left, 4),
                Blend("old", "lin_neutral"), new NovelCommand("time", NovelCommandKind.Wait, duration: .5f),
                new NovelCommand("replace", NovelCommandKind.Character, instanceId: "actor", resourceKey: "alice_neutral", visualAction: NovelVisualAction.Replace),
                SayAction("line"));
            int frame = 0; PumpAction(session, "time", ref frame); session.Tick(.5f, ++frame); PumpAction(session, "line", ref frame);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "old").Status);
            Assert.IsFalse(view.Blending); Assert.AreEqual("alice_neutral", view.Actors["actor"].Key);
            Assert.IsFalse(session.Actions.Single(a => a.Id == "move").IsFinished);
            session.Tick(3.5f, ++frame); Assert.AreEqual(-.24f, view.Actors["actor"].Offset.x, .0001);
        }

        [Test]
        public void E2OldSchemaDefaultsToNoCoverAndInvalidNewCoverIsRejected()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Advance(++frame);
            Assert.IsTrue(session.TryCapture(out var checkpoint, out var error), error);
            checkpoint.SchemaVersion = 3; checkpoint.CoverColor = Color.white; checkpoint.CoverWholeReader = true;
            using (var restored = new NovelSession(checkpoint, () => _tables, new ActionResources { Story = _story }))
            {
                restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady);
                restored.CommitRestore(view); Assert.AreEqual(Color.clear, view.Cover); Assert.IsFalse(view.WholeReader);
            }
            checkpoint.SchemaVersion = 4; checkpoint.CoverColor = new Color(0, 0, 0, 2);
            using var invalid = new NovelSession(checkpoint, () => _tables, new ActionResources { Story = _story });
            invalid.Tick(0, ++frame); Assert.AreEqual(NarrativeState.Faulted, invalid.Snapshot.State); Assert.IsFalse(invalid.RestoreReady);
        }
    }
}
