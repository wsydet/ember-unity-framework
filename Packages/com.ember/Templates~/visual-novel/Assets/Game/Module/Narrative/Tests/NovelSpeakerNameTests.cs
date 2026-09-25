using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Narrative.Tests
{
    // 「角色称呼揭晓前显示占位名」契约：_speakerNameKey 只覆盖这一句显示的说话人名字。
    // 它不换真实角色键，所以不进剧情指纹、不做废既有存档，也不影响 Emphasis Auto 的角色匹配。
    // 全部走真实配表与真实会话，不伪造表数据。
    public sealed partial class NovelSessionTests
    {
        #region 内部参数
        private const string UNKNOWN_SPEAKER = "speaker.unknown";
        private const string MISSING_SPEAKER = "speaker.__no_such_key__";
        private const string WAN = "lastlight_wan";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        /// <summary>未知说话人 → 自我介绍 → 真名：两句共用真实角色键，只有首句带称呼 Key。</summary>
        private static NovelCommand[] SpeakerLines(string firstSpeakerNameKey) => new[]
        {
            new NovelCommand("say-unknown", NovelCommandKind.Say, "……你是谁？", "line-unknown",
                characterId: WAN, speakerNameKey: firstSpeakerNameKey),
            new NovelCommand("say-known", NovelCommandKind.Say, "我是林晚。", "line-known", characterId: WAN)
        };
        private static NovelStory SpeakerStory(string firstSpeakerNameKey) => new NovelStory("speaker", 1, "chapter",
            new[]
            {
                new NovelChapter("chapter", 1, "talk", new[]
                {
                    new NovelNode("talk", NovelNodeKind.Dialogue, "end", commands: SpeakerLines(firstSpeakerNameKey)),
                    new NovelNode("end", NovelNodeKind.Ending, endingId: "done")
                })
            });
        /// <summary>把样例装进会话用的对话节点，返回按正式配表与多语言启动的会话。</summary>
        private NovelSession SpeakerSession(View view, string firstSpeakerNameKey)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ActionCommands { _commands = SpeakerLines(firstSpeakerNameKey).ToList() }), _story.Entry.Entry);
            var resources = new ResourcesFake(); resources.Story.Asset = _story; resources.Story.IsDone = true;
            var session = new NovelSession(new NovelNewGameRequest(), () => _tables, resources);
            session.AttachView(view); session.Tick(0, 0); return session;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void SpeakerNameKeyOverridesTheLineAndFallsBackToTheCharacterName()
        {
            InstallLocalization();
            try
            {
                // ① 覆盖 Key 命中内容表 → 显示译文，而不是角色名。
                Assert.IsTrue(NovelLocalization.TryGetContent(UNKNOWN_SPEAKER, "zh_Hans", out string unknown), "内容表里应有 " + UNKNOWN_SPEAKER);
                Assert.IsFalse(string.IsNullOrWhiteSpace(unknown));
                Assert.IsTrue(NovelLocalization.TryGetCharacterName(WAN, out string realName));
                Assert.AreNotEqual(realName, unknown);
                Assert.AreEqual(unknown, NovelLocalization.SpeakerName(WAN, UNKNOWN_SPEAKER, "不该被用到"));

                // 称呼 Key 是内容文案，所以它跟着语言变：？？？ 也要能翻译。
                Assert.IsTrue(NovelLocalization.TryGetContent(UNKNOWN_SPEAKER, "en", out string unknownEn));
                NovelLanguageSettings.SetPreview("en");
                Assert.AreEqual(unknownEn, NovelLocalization.SpeakerName(WAN, UNKNOWN_SPEAKER, "不该被用到"));
                NovelLanguageSettings.SetPreview(string.Empty);

                // ② Key 未命中或留空 → 按原回退链显示角色名，不显示空白、不报错。
                Assert.IsFalse(NovelLocalization.TryGetContent(MISSING_SPEAKER, out _));
                Assert.AreEqual(realName, NovelLocalization.SpeakerName(WAN, MISSING_SPEAKER, "回退名"));
                Assert.AreEqual(realName, NovelLocalization.SpeakerName(WAN, null, "回退名"));
                Assert.AreEqual(realName, NovelLocalization.SpeakerName(WAN, string.Empty, "回退名"));

                // 没有装配多语言配表时整体退化为角色名，行为与接入前完全一致。
                NovelLocalization.Uninstall();
                Assert.AreEqual("回退名", NovelLocalization.SpeakerName(WAN, UNKNOWN_SPEAKER, "回退名"));
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void SpeakerNameKeyRoundTripsWithoutTouchingTheCharacterKey()
        {
            var command = new NovelCommand("say1", NovelCommandKind.Say, "……你是谁？", "line1",
                characterId: WAN, speakerNameKey: UNKNOWN_SPEAKER);
            Assert.AreEqual(UNKNOWN_SPEAKER, command.SpeakerNameKey);
            Assert.AreEqual(WAN, command.CharacterId, "称呼 Key 不替换真实角色键");
            var roundtrip = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(command));
            Assert.AreEqual(UNKNOWN_SPEAKER, roundtrip.SpeakerNameKey, "JsonUtility 往返必须保留称呼 Key");
            Assert.AreEqual(WAN, roundtrip.CharacterId);

            // 旧资产没有这个字段：反序列化后为空，行为与接入前一致。
            var legacy = JsonUtility.FromJson<NovelCommand>(
                "{\"_commandId\":\"old\",\"_kind\":0,\"_lineId\":\"l\",\"_text\":\"旧台词\",\"_characterId\":\"" + WAN + "\"}");
            Assert.IsTrue(string.IsNullOrEmpty(legacy.SpeakerNameKey));
            Assert.AreEqual(WAN, legacy.CharacterId);

            // 运行期文本副本（接多语言的剧集走 WithResolvedText）必须继承称呼 Key，否则占位名会丢。
            NovelLanguageSettings.SetPreview(string.Empty);
            var localized = SpeakerStory(UNKNOWN_SPEAKER);
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(localized, _tables), runner.Snapshot.Error?.ToString());
            Assert.AreEqual(UNKNOWN_SPEAKER, runner.CurrentCommand.SpeakerNameKey);
        }

        [Test]
        public void SpeakerNameKeyNeverChangesTheStoryFingerprint()
        {
            // 只改称呼 Key 的剧情指纹必须逐字节一致：否则 NarrativeRunner.Checkpoint 会把旧档
            // 判成「剧情语义已变化」。同 _text，它是表现层字段。
            Assert.AreEqual(NovelCompatibility.Fingerprint(SpeakerStory(null)),
                NovelCompatibility.Fingerprint(SpeakerStory(UNKNOWN_SPEAKER)));
        }

        [Test]
        public void UnknownSpeakerShowsPlaceholderThenTheRealNameAndKeepsHistory()
        {
            InstallLocalization();
            try
            {
                Assert.IsTrue(NovelLocalization.TryGetContent(UNKNOWN_SPEAKER, out string unknown));
                Assert.IsTrue(NovelLocalization.TryGetCharacterName(WAN, out string realName), "角色名走 character.<角色键>");
                var view = new View();
                using var session = SpeakerSession(view, UNKNOWN_SPEAKER);
                int frame = 0;

                // 揭晓前：显示占位名，不显示真名。
                Assert.AreEqual("say-unknown", session.Snapshot.CommandId);
                Assert.AreEqual(unknown, view.Speaker);

                // 首句完整显示即为稳定点：写入历史后才推进到下一句。
                session.Tick(10, ++frame);
                Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
                var history = session.History;
                Assert.AreEqual(1, history.Count);
                Assert.AreEqual(UNKNOWN_SPEAKER, history[0].SpeakerNameKey, "历史条目要带上覆盖 Key");
                Assert.AreEqual(unknown, history[0].Speaker, "回看旧句仍是占位名，不会因为后来揭晓而成真名");

                // 揭晓后：同角色键的第二句显示真名。
                session.Advance(++frame); session.Tick(0, ++frame);
                Assert.AreEqual("say-known", session.Snapshot.CommandId);
                Assert.AreEqual(realName, view.Speaker);
                session.Tick(10, ++frame);
                history = session.History;
                Assert.AreEqual(2, history.Count);
                // JsonUtility 把 null 字符串字段读写为空串，两者都表示「没填」。
                Assert.IsTrue(string.IsNullOrEmpty(history[1].SpeakerNameKey), "没填称呼 Key 的句子保持空");
                Assert.AreEqual(realName, history[1].Speaker);

                // 存档：本能力不推进 SchemaVersion，历史带回称呼 Key。
                Assert.IsTrue(session.TryCapture(out var save, out var error), error);
                Assert.AreEqual(7, NovelCheckpoint.CurrentSchemaVersion, "称呼 Key 不需要新的存档版本");
                Assert.AreEqual(NovelCheckpoint.CurrentSchemaVersion, save.SchemaVersion);
                Assert.AreEqual(UNKNOWN_SPEAKER, save.History[0].SpeakerNameKey);

                // 旧档没有 SpeakerNameKey 字段：JsonUtility 读回来是空值（与没填等价），
                // 仍然能恢复，解析退回原回退链上的角色名。
                save.History[0].SpeakerNameKey = null;
                var restoredView = new View();
                var restoreResources = new ResourcesFake(); restoreResources.Story.Asset = _story; restoreResources.Story.IsDone = true;
                using var candidate = new NovelSession(save, () => _tables, restoreResources);
                for (int i = 0; i < 4 && !candidate.RestoreReady; i++) candidate.Tick(0, ++frame);
                Assert.IsTrue(candidate.RestoreReady, candidate.Snapshot.Error?.ToString());
                session.Dispose();
                candidate.CommitRestore(restoredView);
                Assert.AreEqual(2, candidate.History.Count);
                Assert.IsTrue(string.IsNullOrEmpty(candidate.History[0].SpeakerNameKey));
                Assert.AreEqual(realName, candidate.History[0].Speaker, "旧档缺字段时按原回退链显示角色名");
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void EmphasisAutoKeepsMatchingTheRealCharacterKeyWhenTheNameIsOverridden()
        {
            InstallLocalization();
            try
            {
                using var view = new ActionView();
                using var session = ActionSession(view,
                    new NovelCommand("show-wan", NovelCommandKind.Character, resourceKey: "lastlight_alice",
                        slot: NovelPortraitSlot.Left, instanceId: "wan"),
                    new NovelCommand("show-lin", NovelCommandKind.Character, resourceKey: "lin_neutral",
                        slot: NovelPortraitSlot.Right, instanceId: "lin"),
                    new NovelCommand("emphasis", NovelCommandKind.Emphasis, emphasisMode: NovelEmphasisMode.Auto, dimFactor: .3f),
                    new NovelCommand("say-unknown", NovelCommandKind.Say, "……你是谁？", "line-unknown",
                        characterId: WAN, speakerNameKey: UNKNOWN_SPEAKER),
                    new NovelCommand("say-known", NovelCommandKind.Say, "我是林晚。", "line-known", characterId: WAN));
                int frame = 0;
                PumpAction(session, "say-unknown", ref frame);
                // 立绘身份来自角色表；强调 Auto 仍按对白角色键匹配，称呼 Key 完全不参与。
                Assert.AreEqual(WAN, view.Actors["wan"].CharacterId);
                Assert.AreEqual(1f, view.Actors["wan"].Brightness, .001f, "说话人不能被自己的占位名挡掉");
                Assert.AreEqual(.3f, view.Actors["lin"].Brightness, .001f, "非说话人变暗，说明 Auto 确实在按角色键匹配");
            }
            finally { RemoveLocalization(); }
        }
        #endregion
    }
}
