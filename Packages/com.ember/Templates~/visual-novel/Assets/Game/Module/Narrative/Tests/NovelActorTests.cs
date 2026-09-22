using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        private NovelCommand MoveActor(string id, NovelPortraitSlot slot, float duration = 2) =>
            new(id, NovelCommandKind.Move, instanceId: "actor", actionId: id, slot: slot, duration: duration, parallel: true);

        [Test]
        public void E1MoveAndScaleAreIndependentPauseAndCaptureFinalPose()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                MoveActor("move", NovelPortraitSlot.Left),
                new NovelCommand("scale", NovelCommandKind.Scale, instanceId: "actor", actionId: "scale", duration: 2,
                    parallel: true, scale: new Vector2(.5f, .5f)), SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame);
            session.Tick(1, ++frame);
            Assert.AreEqual(-.12f, view.Actors["actor"].Offset.x, .001f);
            Assert.AreEqual(.75f, view.Actors["actor"].Scale.x, .001f);
            session.Pause("menu"); session.Tick(10, ++frame);
            Assert.AreEqual(-.12f, view.Actors["actor"].Offset.x, .001f);
            session.Resume("menu");
            if (session.Snapshot.State == NarrativeState.Revealing) session.Advance(++frame);
            Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
            Assert.IsTrue(session.TryCapture(out var save, out var error), error);
            var actor = save.Visuals.Single();
            Assert.AreEqual("actor", actor.InstanceId); Assert.AreEqual((int)NovelPortraitSlot.Left, actor.NamedSlot);
            Assert.AreEqual(-.24f, actor.Offset.x, .001f); Assert.AreEqual(.5f, actor.Scale.x, .001f);
            Assert.AreEqual(-.12f, view.Actors["actor"].Offset.x, .001f, "Capture must not advance the live actor");
            session.SetReadingMultiplier(2); session.Tick(.5f, ++frame);
            Assert.IsTrue(session.Actions.All(a => a.Status == NovelActionStatus.Completed));
        }

        [Test]
        public void E1ExpressionPreservesMovingIdentityAndTakeoverStartsAtCurrentPose()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                MoveActor("old", NovelPortraitSlot.Left), new NovelCommand("timer", NovelCommandKind.Wait, duration: 1),
                new NovelCommand("expression", NovelCommandKind.Character, instanceId: "actor", resourceKey: "alice_smile",
                    visualAction: NovelVisualAction.Replace), MoveActor("new", NovelPortraitSlot.Right), SayAction("line"));
            int frame = 0; PumpAction(session, "timer", ref frame); session.Tick(1, ++frame);
            PumpAction(session, "line", ref frame);
            Assert.AreEqual(NovelActionStatus.Cancelled, session.Actions.Single(a => a.Id == "old").Status);
            Assert.AreEqual("alice_smile", view.Actors["actor"].Key);
            Assert.AreEqual(-.12f, view.Actors["actor"].Offset.x, .001f);
            session.Tick(1, ++frame); Assert.AreEqual(.06f, view.Actors["actor"].Offset.x, .001f);
            session.Tick(1, ++frame); Assert.AreEqual(.24f, view.Actors["actor"].Offset.x, .001f);
            Assert.AreEqual(NovelPortraitSlot.Center, view.Actors["actor"].Slot, "Physical render slot must not become identity");
        }

        [Test]
        public void E1GestureReturnsToBaselineWithoutMovingActor()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                new NovelCommand("jump", NovelCommandKind.Gesture, instanceId: "actor", actionId: "jump", duration: 2, parallel: true),
                SayAction("line"));
            int frame = 0; PumpAction(session, "line", ref frame); session.Tick(1, ++frame);
            Assert.AreEqual(.04f, view.Gestures["actor"].y, .001f);
            Assert.AreEqual(Vector2.zero, view.Actors["actor"].Offset);
            session.Tick(1, ++frame); Assert.AreEqual(Vector2.zero, view.Gestures["actor"]);
        }

        [Test]
        public void E1OccupiedNamedDestinationReportsError()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                ShowAction("other", NovelPortraitSlot.Left), MoveActor("conflict", NovelPortraitSlot.Left), SayAction("line"));
            for (int frame = 1; frame < 30 && session.Snapshot.Error == null; frame++) session.Tick(0, frame);
            Assert.IsNotNull(session.Snapshot.Error);
        }

        [Test]
        public void E1EmptyLogicalSlotHideKeepsDurationAndDoesNotHideMovedActor()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Center),
                MoveActor("move", NovelPortraitSlot.Left, 0),
                new NovelCommand("empty", NovelCommandKind.Character, slot: NovelPortraitSlot.Center,
                    visualAction: NovelVisualAction.Hide, duration: 1), SayAction("line"));
            int frame = 0; PumpAction(session, "empty", ref frame); session.Tick(.25f, ++frame);
            Assert.AreEqual("empty", session.Snapshot.CommandId);
            Assert.IsTrue(session.Snapshot.Wait.HasFlag(NarrativeWait.Transition));
            Assert.IsNotNull(view.Picture.sprite);
            Assert.AreEqual(1, view.Picture.color.a, .001f);
            Assert.AreEqual(-.24f, view.Actors["actor"].Offset.x, .001f);
            session.Tick(.75f, ++frame); PumpAction(session, "line", ref frame);
            Assert.IsTrue(view.Actors.ContainsKey("actor")); Assert.IsNotNull(view.Picture.sprite);
        }

        [Test]
        public void E1InvalidParametersAreRejected()
        {
            Assert.IsNotNull(NovelActorRules.Validate(new NovelCommand("scale", NovelCommandKind.Scale,
                instanceId: "actor", actionId: "scale", scale: Vector2.zero)));
            Assert.IsNotNull(NovelActorRules.Validate(new NovelCommand("exit", NovelCommandKind.Move,
                instanceId: "actor", actionId: "exit", exitAfterMove: true)));
            Assert.IsNotNull(NovelActorRules.Validate(new NovelCommand("manual", NovelCommandKind.Emphasis,
                emphasisMode: NovelEmphasisMode.Manual)));
            Assert.IsNull(NovelActorRules.Validate(new NovelCommand("entry", NovelCommandKind.Character,
                instanceId: "actor", positionMode: NovelPositionMode.Normalized, position: new Vector2(-2, 0))));
        }
    }
}
