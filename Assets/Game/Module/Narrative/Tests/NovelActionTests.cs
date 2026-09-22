using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Ember.UIExtension;
using Ember.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        [Serializable] private sealed class ActionCommands { public List<NovelCommand> _commands; }
        private sealed class ActionResources : INovelResources
        {
            public NarrativeStorySO Story;
            public Sprite Sprite;
            public INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object =>
                new Lease<T> { IsDone = true, Asset = (typeof(T) == typeof(NarrativeStorySO) ? (UnityEngine.Object)Story : Sprite) as T };
        }
        private sealed partial class ActionView : INovelView, INovelOpacityView, INovelActorView, IDisposable
        {
            private readonly GameObject _root;
            private readonly EUIItem _item;
            public readonly Dictionary<(NovelTargetKind, NovelPortraitSlot), float> Alpha = new();
            public readonly Dictionary<string, NovelVisualState> Actors = new();
            public readonly Dictionary<string, Vector2> Gestures = new();
            public Vector2 NamedPosition(NovelPortraitSlot slot) => new(.26f + (int)slot * .24f, slot == NovelPortraitSlot.Center ? .395f : .335f);
            public void ApplyActor(NovelVisualState state, Vector2 gestureOffset, float gestureRotation)
            { Actors[state.InstanceId] = state; Gestures[state.InstanceId] = gestureOffset; }
            public void ResetActor(NovelPortraitSlot slot)
            { foreach (var id in Actors.Where(p => p.Value.Slot == slot).Select(p => p.Key).ToArray()) { Actors.Remove(id); Gestures.Remove(id); } }
            public int Writes;
            private float _stage = 1, _local = 1;
            public int TextLength => 10;
            public UnityEngine.UI.Image Picture => _root.GetComponentInChildren<UnityEngine.UI.Image>(true);
            public ActionView()
            {
                _root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelPortraitItem.prefab"));
                Assert.IsTrue(EUIItemFactory.TryCreate(_root, out _item, out var error), error); _item.Show();
            }
            public void Render(NarrativeSnapshot s, NovelCommand c, string speaker, int visible, string status) { }
            public void Visual(NovelCommand c, Sprite sprite, float progress) =>
                _item.Logic.GetType().GetMethod("Apply").Invoke(_item.Logic, new object[] { c, sprite, progress });
            public void SetOpacity(NovelTargetKind kind, NovelPortraitSlot slot, float value)
            {
                Writes++; Alpha[(kind, slot)] = value;
                _item.Logic.GetType().GetMethod("SetOpacity").Invoke(_item.Logic, new object[] { (kind == NovelTargetKind.Stage ? (_stage = value) : (_local = value)) * (kind == NovelTargetKind.Stage ? _local : _stage) });
            }
            public void ClearVisuals() { ClearScreen(); _item.Logic.GetType().GetMethod("Clear").Invoke(_item.Logic, null); }
            public void Dispose() { _item.Dispose(); UnityEngine.Object.DestroyImmediate(_root); }
        }
        private NovelCommand Fade(string id, string target, float alpha = 0, bool parallel = true, float delay = 0) =>
            new(id, NovelCommandKind.Opacity, duration: 2, instanceId: target, targetKind: NovelTargetKind.Character,
                actionId: id, parallel: parallel, delay: delay, opacity: alpha);
        private NovelCommand SayAction(string id) => new(id, NovelCommandKind.Say, text: "E0 测试", lineId: id);
        private NovelCommand ShowAction(string id, NovelPortraitSlot slot) =>
            new("show-" + id, NovelCommandKind.Character, resourceKey: "alice_neutral", slot: slot, instanceId: id);
        private NovelSession ActionSession(ActionView view, params NovelCommand[] commands)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = commands.ToList() }), _story.Entry.Entry);
            var texture = new Texture2D(2, 2); _assets.Add(texture);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero); _assets.Add(sprite);
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, new ActionResources { Story = _story, Sprite = sprite });
            session.AttachView(view); session.Tick(0, 0); return session;
        }
        private void PumpAction(NovelSession session, string id, ref int frame)
        {
            for (int i = 0; i < 30 && session.Snapshot.CommandId != id && session.Snapshot.Error == null; i++) session.Tick(0, ++frame);
            Assert.IsNull(session.Snapshot.Error, session.Snapshot.Error?.ToString()); Assert.AreEqual(id, session.Snapshot.CommandId);
        }

        [Test]
        public void E0ParallelDialoguePauseMultiplierAndRealImageFrames()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), ShowAction("two", NovelPortraitSlot.Right),
                Fade("a", "one"), Fade("b", "two"), SayAction("line"),
                new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "a", "b" }), SayAction("done"));
            int frame = 0; PumpAction(session, "line", ref frame);
            Assert.AreEqual(2, session.Snapshot.Actions.Count); Assert.AreEqual(1, view.Picture.color.a);
            session.Tick(.5f, ++frame); Assert.AreEqual(.75f, view.Picture.color.a, .001);
            session.Pause("menu"); session.Tick(10, ++frame); Assert.AreEqual(.75f, view.Picture.color.a, .001);
            session.Resume("menu"); session.SetReadingMultiplier(2); session.Tick(.25f, ++frame);
            Assert.AreEqual(.5f, view.Picture.color.a, .001);
            session.Advance(++frame); PumpAction(session, "join", ref frame); session.Tick(0, ++frame);
            Assert.That(session.Snapshot.Wait.HasFlag(NarrativeWait.Actions));
            session.Tick(.5f, ++frame); Assert.AreEqual("done", session.Snapshot.CommandId);
            Assert.AreEqual(0, view.Picture.color.a); Assert.IsNotNull(view.Picture.sprite);
            Assert.IsTrue(session.Actions.All(a => a.Status == NovelActionStatus.Completed));
        }

        [Test]
        public void E0TakeoverDelayAndDisposeCannotWriteAgain()
        {
            using var view = new ActionView();
            var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), Fade("old", "one"),
                new NovelCommand("time", NovelCommandKind.Wait, duration: .5f), Fade("new", "one", 1, delay: 1), SayAction("line"));
            int frame = 0; PumpAction(session, "time", ref frame); session.Tick(.5f, ++frame);
            PumpAction(session, "line", ref frame);
            var old = session.Actions.Single(a => a.Id == "old"); Assert.AreEqual(NovelActionStatus.Cancelled, old.Status);
            Assert.AreEqual(.75f, view.Picture.color.a, .001);
            session.Tick(.5f, ++frame); Assert.AreEqual(.75f, view.Picture.color.a, .001);
            session.Pause("pause"); session.Tick(100, ++frame); session.Resume("pause");
            session.Tick(1.5f, ++frame); Assert.AreEqual(.875f, view.Picture.color.a, .001);
            var current = session.Actions.Single(a => a.Id == "new"); session.Dispose();
            int writes = view.Writes; session.Tick(100, ++frame);
            Assert.AreEqual(writes, view.Writes); Assert.AreEqual(NovelActionStatus.Cancelled, current.Status);
            Assert.IsNull(view.Picture.sprite); Assert.AreEqual(0, session.OwnedResourceCount);
        }

        [Test]
        public void E0CaptureProjectsFinalStateAndRestoreWaitDoesNotReplay()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Right), Fade("a", "one", .2f), SayAction("line"),
                new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "a" }), SayAction("done"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Advance(++frame);
            Assert.IsTrue(session.TryCapture(out var save, out var error), error);
            Assert.AreEqual(.2f, save.Visuals.Single().Opacity); Assert.AreEqual(1, view.Picture.color.a);
            Assert.AreEqual("one", save.Visuals.Single().InstanceId); Assert.AreEqual(NovelActionStatus.Completed, save.Actions.Single().Status);
            using var restored = new NovelSession(save, () => _tables, new ActionResources { Story = _story, Sprite = view.Picture.sprite });
            restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady, restored.Snapshot.Error?.ToString());
            restored.CommitRestore(view); Assert.AreEqual(.2f, view.Picture.color.a, .001);
            restored.Advance(++frame); restored.Tick(0, ++frame); Assert.AreEqual("done", restored.Snapshot.CommandId);
            Assert.IsEmpty(restored.Snapshot.Actions);
        }

        [Test]
        public void E0SkipConvergesBeforeUnreadAndIdentityIgnoresReplaceSlot()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Right),
                new NovelCommand("replace", NovelCommandKind.Character, resourceKey: "alice_neutral", instanceId: "one", visualAction: NovelVisualAction.Replace),
                Fade("a", "one", .3f), SayAction("read"), SayAction("unread"));
            int frame = 0; PumpAction(session, "read", ref frame);
            session.ConfigureReading((s, line, rev) => line == "read", 32, 1, 1, 1, 1);
            session.SetReadMode(NarrativeReadMode.Skip); session.Tick(0, ++frame); session.Tick(0, ++frame);
            Assert.AreEqual("unread", session.Snapshot.CommandId); Assert.AreEqual(NarrativeReadMode.Manual, session.ReadMode);
            Assert.AreEqual(.3f, view.Alpha[(NovelTargetKind.Character, NovelPortraitSlot.Right)], .001);
        }

        [Test]
        public void E0LegacySaveDefaultsAndUnknownWaitFailClearly()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Advance(++frame);
            Assert.IsTrue(session.TryCapture(out var save, out _)); save.SchemaVersion = 1;
            save.Visuals[0].InstanceId = null; save.Visuals[0].Opacity = 0; save.Visuals[0].Scale = Vector3.zero;
            using var restored = new NovelSession(save, () => _tables, new ActionResources { Story = _story, Sprite = view.Picture.sprite });
            restored.Tick(0, ++frame); restored.Tick(0, ++frame); Assert.IsTrue(restored.RestoreReady);
            restored.CommitRestore(view); Assert.AreEqual(1, view.Picture.color.a);
            using var bad = ActionSession(view, new NovelCommand("bad", NovelCommandKind.WaitActions, waitActions: new[] { "never" }));
            bad.Tick(0, ++frame); Assert.AreEqual(NarrativeState.Faulted, bad.Snapshot.State);
            StringAssert.Contains("never", bad.Snapshot.Error.Message);
        }
    }
}
