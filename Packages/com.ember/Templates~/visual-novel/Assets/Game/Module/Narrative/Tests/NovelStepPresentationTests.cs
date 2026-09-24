using System;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelSessionTests
    {
        [Test]
        public void TitleFadeWaitsForClickExitAndPausesWithoutDoubleAdvance()
        {
            Commands(new NovelCommand("title", NovelCommandKind.Say, "第一章", "title", textMode: NovelTextMode.Title,
                textReveal: NovelTextReveal.Fade, textFadeDuration: 1, titleExitDuration: .5f, textEase: NovelEase.Linear), SayAction("next"));
            using var session = ReadingSession(out _, out _);
            var view = new TextView { Capacity = 100 }; session.AttachView(view);
            session.Tick(0, 1); session.Tick(.25f, 2);
            Assert.That(view.TextAlpha, Is.InRange(.01f, .9f));
            Assert.AreEqual("title", session.Snapshot.CommandId);
            session.Advance(3); Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
            Assert.AreEqual(1, view.TextAlpha);
            session.Advance(4); Assert.IsTrue(session.TextTransitionActive);
            Assert.IsFalse(session.TryCapture(out _, out var error)); StringAssert.Contains("渐隐", error);
            session.Pause("menu"); session.Tick(10, 5); Assert.AreEqual(1, view.CardAlpha);
            session.Resume("menu"); session.Tick(.25f, 6); Assert.AreEqual(.5f, view.CardAlpha, .001);
            session.Advance(7); Assert.AreEqual("title", session.Snapshot.CommandId);
            session.Tick(.25f, 8); Assert.AreEqual("next", session.Snapshot.CommandId);
            Assert.AreEqual(1, view.CardAlpha);
        }

        [Test]
        public void HideAllIncludesActorsMovedOutOfNamedSlotsAndReleasesTheirActions()
        {
            using var view = new ActionView();
            using var session = ActionSession(view, ShowAction("actor", NovelPortraitSlot.Left),
                new NovelCommand("free", NovelCommandKind.Move, instanceId: "actor", actionId: "free", positionMode: NovelPositionMode.Normalized,
                    position: new Vector2(.5f, .4f), duration: 0),
                new NovelCommand("clear", NovelCommandKind.HideAllCharacters, duration: 1), SayAction("done"));
            int frame = 0; PumpAction(session, "clear", ref frame); session.Tick(.5f, ++frame);
            Assert.AreEqual(.5f, view.Alpha[(NovelTargetKind.Character, NovelPortraitSlot.Left)], .001);
            session.Pause("test"); session.Tick(10, ++frame); Assert.AreEqual("clear", session.Snapshot.CommandId);
            session.Resume("test"); session.Tick(.5f, ++frame);
            Assert.IsEmpty(view.Actors); Assert.AreEqual("done", session.Snapshot.CommandId);
        }

        [Test]
        public void CustomStepCopiesRemapWaitsWithoutChangingSourceAndRejectExternalReferences()
        {
            var source = NarrativePresentationPresets.Build(NarrativePresetKind.Impact, "hit", "actor", null, NovelPortraitSlot.Left, .5f, 1);
            var a = NarrativeStepGroups.Wrap(source, "受击", Color.cyan);
            var b = NarrativeStepGroups.Wrap(source, "受击", Color.cyan);
            Assert.IsEmpty(a.Select(c => c.CommandId).Intersect(b.Select(c => c.CommandId)));
            Assert.AreNotEqual(a[0].StepGroupId, b[0].StepGroupId);
            foreach (string wait in a.Last().WaitActions) Assert.IsTrue(a.Any(c => c.ActionId == wait));
            Assert.AreEqual("hit-shake", source[0].CommandId); Assert.AreEqual("actor", a[0].InstanceId);
            Assert.Throws<ArgumentException>(() => NarrativeStepGroups.Wrap(new[] { source.Last() }, "错误范围", Color.white));
            var tagged = NarrativeStepGroups.Wrap(source, "原位包装", Color.green, false);
            CollectionAssert.AreEqual(source.Select(c => c.CommandId), tagged.Select(c => c.CommandId));
        }

        [Test]
        public void LegacyJsonGetsTextDefaultsButNewInvalidValuesStayInvalid()
        {
            var legacy = JsonUtility.FromJson<NovelCommand>("{\"_commandId\":\"old\",\"_kind\":0,\"_lineId\":\"line\",\"_text\":\"text\"}");
            Assert.AreEqual(1, legacy.TextSpeedMultiplier);
            Assert.AreEqual(.8f, legacy.TextFadeDuration);
            Assert.IsNull(NovelTextRules.Validate(legacy));
            var invalid = new NovelCommand("new", NovelCommandKind.Say, "text", "line", textSpeedMultiplier: 0);
            var copy = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(invalid));
            Assert.AreEqual(0, copy.TextSpeedMultiplier);
            Assert.IsNotNull(NovelTextRules.Validate(copy));
        }

        [TestCase(-1, 1)]
        [TestCase(1, 0)]
        [TestCase(31, 1)]
        public void InvalidTextAnimationParametersAreRejected(float duration, float speed)
        {
            var command = new NovelCommand("title", NovelCommandKind.Say, "标题", "title", textFadeDuration: duration, textSpeedMultiplier: speed);
            Assert.IsNotNull(NovelTextRules.Validate(command));
        }
    }
}
