using System;
using System.Linq;
using Game.Narrative.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Narrative.Tests
{
    // ActionId 契约：它是可选的等待句柄别名，留空时句柄回退到该步骤的指令 ID。
    // 指令 ID 已由校验器保证章节内唯一，所以回退不会破坏唯一性，也不会改变存档指纹。
    public sealed partial class NovelSessionTests
    {
        #region 内部参数
        private sealed class FixtureCatalog : INarrativeCatalog
        {
            public bool IsReady => true;
            public bool HasCharacter(string id) => true;
            public bool TryResolve(NovelCommandKind kind, string key, out string path) { path = key; return true; }
        }
        // 改动前（模板 0.9.0 / com.ember 0.14.10）在 Unity 中实测得到的样例剧情指纹。
        // 本批改动只动运行期句柄、校验与编辑器，指纹写的是原始字段，这两个值必须逐字节不变。
        private const string PRESENTATION_E0_FINGERPRINT = "7D8B67D3B0A1A4B7A0F54FB935254A6C8E04A78BA1E14A13847DB3F5BC93FC32";
        private const string M1_SAMPLE_FINGERPRINT = "FFBCEEA68A82479054CFC04386D9C9704216DD470D37C27B69DE2F8A0DE26AFB";
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void ResolveIdFallsBackToCommandIdOnlyWhenAliasIsBlank()
        {
            var blank = new NovelCommand("fade-actor", NovelCommandKind.Opacity, duration: 1, instanceId: "actor",
                targetKind: NovelTargetKind.Character, opacity: 0);
            Assert.IsTrue(string.IsNullOrEmpty(blank.ActionId));
            Assert.AreEqual("fade-actor", NovelActionHandle.ResolveId(blank));
            var named = new NovelCommand("move-actor", NovelCommandKind.Move, instanceId: "actor", actionId: "wan_walk_left");
            Assert.AreEqual("wan_walk_left", NovelActionHandle.ResolveId(named));
            // 纯空白等同于留空。
            Assert.AreEqual("flash-stage", NovelActionHandle.ResolveId(new NovelCommand("flash-stage", NovelCommandKind.Flash, actionId: "   ")));
            // 媒体路径保留原签名与调用点，但委托同一处真源，不再自带第二份回退规则。
            Assert.AreEqual(NovelActionHandle.ResolveId(blank), NovelMediaRules.ActionId(blank));
        }

        [Test]
        public void BlankActionIdActionCanBeWaitedThroughItsCommandId()
        {
            using var view = new ActionView();
            var fade = new NovelCommand("fade-one", NovelCommandKind.Opacity, duration: 2, instanceId: "one",
                targetKind: NovelTargetKind.Character, parallel: true, opacity: 0);
            using var session = ActionSession(view, ShowAction("one", NovelPortraitSlot.Left), fade, SayAction("line"),
                new NovelCommand("join", NovelCommandKind.WaitActions, waitActions: new[] { "fade-one" }), SayAction("done"));
            int frame = 0; PumpAction(session, "line", ref frame);
            // 句柄就是本步骤的指令 ID，而不是空的动作 ID。
            var handle = session.Actions.Single(a => a.Id == "fade-one");
            // 先让正文完整显示，再推进；否则第一次 Advance 只完成打字机，停在原地（旧写法曾因此失败）。
            session.Tick(1.5f, ++frame); session.Advance(++frame);
            PumpAction(session, "join", ref frame); session.Tick(0, ++frame);
            // 关键断言：留空回退后仍能找到句柄；旧契约下这里会抛「等待的动作尚未启动」。
            Assert.IsNull(session.Snapshot.Error, session.Snapshot.Error?.ToString());
            Assert.That(session.Snapshot.Wait.HasFlag(NarrativeWait.Actions));
            Assert.AreEqual(NovelActionStatus.Running, handle.Status);
            session.Tick(2.5f, ++frame);
            Assert.AreEqual("done", session.Snapshot.CommandId);
            Assert.AreEqual(NovelActionStatus.Completed, handle.Status);
        }

        [Test]
        public void CustomStepWrapRemapsCommandIdWaitsWhenActionIdsAreBlank()
        {
            var source = new[]
            {
                new NovelCommand("group-show", NovelCommandKind.Character, resourceKey: "alice_neutral", slot: NovelPortraitSlot.Left, instanceId: "actor"),
                new NovelCommand("group-move", NovelCommandKind.Move, instanceId: "actor", slot: NovelPortraitSlot.Center, duration: 1, parallel: true),
                new NovelCommand("group-join", NovelCommandKind.WaitActions, waitActions: new[] { "group-move" })
            };
            Assert.IsTrue(source.All(c => string.IsNullOrEmpty(c.ActionId)));
            var wrapped = NarrativeStepGroups.Wrap(source, "留空句柄", Color.cyan);
            // 留空必须保持留空，不能凭空塞一个哈希进去。
            Assert.IsTrue(wrapped.All(c => string.IsNullOrEmpty(c.ActionId)));
            Assert.IsEmpty(wrapped.Select(c => c.CommandId).Intersect(source.Select(c => c.CommandId)));
            var join = wrapped.Last();
            Assert.IsNotEmpty(join.WaitActions);
            // 等待项必须解析到包装后某个步骤的句柄：回退目标就是重建出来的新指令 ID。
            var move = wrapped.Single(c => NovelActionHandle.ResolveId(c) == join.WaitActions.Single());
            Assert.AreEqual(NovelCommandKind.Move, move.Kind);
            Assert.IsTrue(string.IsNullOrEmpty(move.ActionId));
            // freshIds 为假时沿用原指令 ID，且动作 ID 仍保持原样（留空）。
            var tagged = NarrativeStepGroups.Wrap(source, "原位包装", Color.green, false);
            CollectionAssert.AreEqual(source.Select(c => c.CommandId), tagged.Select(c => c.CommandId));
            Assert.AreEqual("group-move", tagged.Last().WaitActions.Single());
        }

        [TestCase("Assets/Game/Module/Narrative/Tests/Fixtures/PresentationE0/E0StoryFixture.asset", PRESENTATION_E0_FINGERPRINT)]
        [TestCase("Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset", M1_SAMPLE_FINGERPRINT)]
        public void SampleStoryFingerprintsStayByteIdentical(string path, string expected)
        {
            var asset = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(path);
            Assert.IsNotNull(asset, path);
            Assert.IsTrue(asset.TryReadDefinition(new FixtureCatalog(), out var story, out var errors), string.Join("\n", errors));
            Assert.AreEqual(expected, NovelCompatibility.Fingerprint(story));
        }

        [Test]
        public void ValidatorAcceptsBlankActionIdAndReportsResolvedCollisions()
        {
            var dialogue = (NarrativeDialogueSO)_story.Entry.Entry;
            // 留空不再判成“缺少动作 ID”；句柄回退到本步骤 ID，且能被等待组引用。
            JsonUtility.FromJsonOverwrite("{\"_commands\":[" +
                "{\"_commandId\":\"fade-actor\",\"_kind\":8,\"_targetKind\":2,\"_instanceId\":\"actor\",\"_duration\":1,\"_opacity\":0,\"_parallel\":1,\"_waitActions\":[]}," +
                "{\"_commandId\":\"join\",\"_kind\":9,\"_waitActions\":[\"fade-actor\"]}]}", dialogue);
            Assert.IsTrue(_story.TryReadDefinition(_tables, out var definition, out var issues), string.Join("\n", issues));
            Assert.AreEqual("fade-actor", NovelActionHandle.ResolveId(definition.Chapters[0].Nodes[0].Commands[0]));
            // 显式别名先注册，随后的留空动作回退到同名步骤 ID：按解析值判重，并指出它来自回退。
            JsonUtility.FromJsonOverwrite("{\"_commands\":[" +
                "{\"_commandId\":\"first\",\"_kind\":8,\"_targetKind\":2,\"_instanceId\":\"actor\",\"_duration\":1,\"_opacity\":0,\"_actionId\":\"shared\"}," +
                "{\"_commandId\":\"shared\",\"_kind\":8,\"_targetKind\":2,\"_instanceId\":\"actor\",\"_duration\":1,\"_opacity\":0}]}", dialogue);
            Assert.IsFalse(_story.TryReadDefinition(_tables, out _, out var collisions));
            Assert.That(collisions, Has.Some.Matches<NarrativeError>(e =>
                e.Code == "BadActionId" && e.CommandId == "shared" && e.Message.Contains("回退")));
            // 两个显式别名同名也照旧判重，且报的是第二条。
            JsonUtility.FromJsonOverwrite("{\"_commands\":[" +
                "{\"_commandId\":\"one\",\"_kind\":8,\"_targetKind\":2,\"_instanceId\":\"actor\",\"_duration\":1,\"_opacity\":0,\"_actionId\":\"dup\"}," +
                "{\"_commandId\":\"two\",\"_kind\":8,\"_targetKind\":2,\"_instanceId\":\"actor\",\"_duration\":1,\"_opacity\":0,\"_actionId\":\"dup\"}]}", dialogue);
            Assert.IsFalse(_story.TryReadDefinition(_tables, out _, out var duplicates));
            Assert.That(duplicates, Has.Some.Matches<NarrativeError>(e => e.Code == "BadActionId" && e.CommandId == "two"));
        }
        #endregion
    }
}
