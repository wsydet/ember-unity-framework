using System.Collections.Generic;
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

        /// <summary>
        /// 归一化入场（<c>PositionMode = Normalized</c>）的人物 <c>NamedSlot</c> 恒为 -1，
        /// 空实例 ID 的 Hide 只按命名位置找会找不到它，于是整条指令什么也不做、也没有任何提示，
        /// 立绘就永久留在台上。这里要求它退化为隐藏该渲染槽上的实例，并留下诊断。
        /// </summary>
        [Test]
        public void E6EmptyInstanceIdHideReachesANormalizedEntryInsteadOfSilentlyDoingNothing()
        {
            var warnings = new System.Collections.Generic.List<string>();
            Application.LogCallback handler = (condition, _, type) => { if (type == LogType.Warning) warnings.Add(condition); };
            Application.logMessageReceived += handler;
            try
            {
                using var view = new ActionView();
                using var session = ActionSession(view,
                    new NovelCommand("show", NovelCommandKind.Character, resourceKey: "alice_neutral",
                        slot: NovelPortraitSlot.Center, instanceId: "mother",
                        positionMode: NovelPositionMode.Normalized, position: new Vector2(.5f, .38f)),
                    new NovelCommand("hide", NovelCommandKind.Character, slot: NovelPortraitSlot.Center,
                        visualAction: NovelVisualAction.Hide, duration: .4f),
                    SayAction("line"));
                int frame = 0; PumpAction(session, "hide", ref frame);
                Assert.IsTrue(view.Actors.ContainsKey("mother"), "归一化入场的人物应该已经在场上");
                Assert.AreEqual(-1, view.Actors["mother"].NamedSlot, "归一化入场不认领命名位置");
                session.Tick(1, ++frame); PumpAction(session, "line", ref frame);
                Assert.IsFalse(view.Actors.ContainsKey("mother"),
                    "空实例 ID 的 Hide 必须退化到该渲染槽上的实例，不能静默失效");
                Assert.IsTrue(warnings.Exists(w => w.Contains("空实例 ID 的立绘隐藏")),
                    "退化匹配必须留下诊断，否则作者永远不知道自己在依赖槽位语义");
            }
            finally { Application.logMessageReceived -= handler; }
        }

        /// <summary>槽位上什么都没有时仍然是安全的空操作，但必须报出来，不能完全静默。</summary>
        [Test]
        public void E6EmptyInstanceIdHideOnAnEmptySlotWarnsInsteadOfFailingSilently()
        {
            var warnings = new System.Collections.Generic.List<string>();
            Application.LogCallback handler = (condition, _, type) => { if (type == LogType.Warning) warnings.Add(condition); };
            Application.logMessageReceived += handler;
            try
            {
                using var view = new ActionView();
                using var session = ActionSession(view,
                    new NovelCommand("hide", NovelCommandKind.Character, slot: NovelPortraitSlot.Right,
                        visualAction: NovelVisualAction.Hide),
                    SayAction("line"));
                int frame = 0; PumpAction(session, "line", ref frame);
                Assert.IsEmpty(view.Actors);
                Assert.IsTrue(warnings.Exists(w => w.Contains("没有隐藏任何实例")),
                    "空槽位上的空 ID 隐藏要保持安全，但必须给出诊断");
            }
            finally { Application.logMessageReceived -= handler; }
        }

        /// <summary>
        /// 同节点内的空实例 ID 立绘隐藏要在编辑期被报出来（编写提示，不阻断运行）。
        /// 只做同节点顺序分析：跨节点延续的实例状态交给运行期诊断，避免对分支内容误报。
        /// </summary>
        [Test]
        public void E6EditorHintsReportSlotAddressedHideThatReliesOnDegradation()
        {
            JsonUtility.FromJsonOverwrite("{\"_commands\":[" +
                "{\"_commandId\":\"show\",\"_kind\":4,\"_resourceKey\":\"alice_neutral\",\"_instanceId\":\"mother\",\"_slot\":1,\"_positionMode\":1}," +
                "{\"_commandId\":\"hide\",\"_kind\":4,\"_visualAction\":2,\"_instanceId\":\"\",\"_slot\":1,\"_duration\":0.4}]}", ReadingTalk);
            var validation = typeof(Game.Narrative.Editor.NarrativeGraphWindow).Assembly
                .GetType("Game.Narrative.Editor.NarrativeAssetValidation");
            var validate = validation.GetMethod("ValidateHints",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var hints = (IReadOnlyList<NarrativeError>)validate.Invoke(null, new object[] { _story, null });
            Assert.IsTrue(hints.Any(h => h.Code == "AmbiguousSlotHide" && h.CommandId == "hide"),
                "空 ID 的隐藏落在归一化入场的实例上时必须给出编写提示：" +
                string.Join(" / ", hints.Select(h => h.ToString())));
            // 提示不能进校验错误通道：TryReadDefinition 只要有一条错误就拒绝运行整章。
            Assert.IsTrue(_story.TryReadDefinition(_tables, out _, out _),
                "编写提示不能把这条剧情判成不可运行");
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
