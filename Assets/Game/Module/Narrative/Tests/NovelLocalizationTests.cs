using System;
using System.Linq;
using Ember.Core;
using Ember.UI;
using Ember.UIExtension;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    // 多语言与皮肤的契约测试。全部走真实配表（SetUp 里已由 GameTables 烘焙产物装配 _tables），
    // 不伪造表数据，因此这些用例同时也在校验配表本身可用。
    // 指纹基线常量与 FixtureCatalog 声明在同一个 partial 类的 NovelActionHandleTests.cs 里，这里直接复用。
    public sealed partial class NovelSessionTests
    {
        #region 内部方法
        private void InstallLocalization()
        {
            NovelLanguageSettings.SetPreview(string.Empty);
            NovelLocalization.Install(_tables);
        }

        private static void RemoveLocalization()
        {
            NovelLocalization.Uninstall();
            NovelSkin.Uninstall();
            NovelLanguageSettings.SetPreview(string.Empty);
        }

        /// <summary>把首句改成带指定 Key 的台词，读定义并启动 runner，返回当前显示文本。</summary>
        private string RunFirstLine(string textKey, string language)
        {
            var dialogue = (NarrativeDialogueSO)_story.Entry.Entry;
            JsonUtility.FromJsonOverwrite("{\"_commands\":[{\"_commandId\":\"say1\",\"_kind\":0,\"_lineId\":\"line1\"," +
                "\"_textRevision\":1,\"_text\":\"原文台词\",\"_textKey\":\"" + textKey + "\"}]}", dialogue);
            Assert.IsTrue(_story.TryReadDefinition(_tables, out var definition, out var issues), string.Join("\n", issues));
            NovelLanguageSettings.SetPreview(language);
            var runner = new NarrativeRunner();
            Assert.IsTrue(runner.StartStory(definition, _tables), runner.Snapshot.Error?.ToString());
            return runner.CurrentCommand?.Text;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [Test]
        public void LocalizationResolvesLanguagesAndFallsBackToSource()
        {
            InstallLocalization();
            try
            {
                Assert.IsTrue(_tables.LocalizationReady, "配表里缺少多语言三张表");
                var localizer = NovelLocalization.Localizer;
                Assert.AreEqual("zh_Hans", localizer.SourceLanguage);
                CollectionAssert.Contains(localizer.Languages, "zh_Hant");
                CollectionAssert.Contains(localizer.Languages, "ja");
                CollectionAssert.Contains(localizer.Languages, "en");

                // 四语言各自取到自己那一列。
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "zh_Hans", out string zh));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "zh_Hant", out string hant));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "ja", out string ja));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "en", out string en));
                Assert.AreNotEqual(zh, hant); Assert.AreNotEqual(zh, ja);
                Assert.AreNotEqual(zh, en); Assert.AreNotEqual(ja, en);

                // 某语言列留空 → 回退源语言，而不是空白。
                // ui.reader.Choice.Label 是刻意留空三列的回退探针：它已经不挂任何控件，
                // 专门留给这条断言用——补其它 UI 译文时不要顺手把它的 zh_Hant/ja/en 填上。
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.reader.Choice.Label", "ja", out string fallback));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.reader.Choice.Label", "zh_Hans", out string source));
                Assert.AreEqual(source, fallback);

                // 查不到的 Key 返回 false，由调用方回退原文。
                Assert.IsFalse(NovelLocalization.TryGetContent("nope.missing.key", out _));
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void LocalizationResolvesCharacterNamesAndFallsBack()
        {
            InstallLocalization();
            try
            {
                // 内容表里有 character.<角色键> → 用表里的名字。
                Assert.IsTrue(NovelLocalization.TryGetCharacterName("lastlight_wan", out string known));
                Assert.IsFalse(string.IsNullOrWhiteSpace(known));
                Assert.AreEqual(known, NovelLocalization.CharacterName("lastlight_wan", "不该被用到"));
                // 没有条目 → 用调用方给的回退值（通常是 novel_characters.displayName）。
                Assert.IsFalse(NovelLocalization.TryGetCharacterName("__no_such_character__", out _));
                Assert.AreEqual("回退名", NovelLocalization.CharacterName("__no_such_character__", "回退名"));
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void LocalizationNeverChangesStoryFingerprint()
        {
            var asset = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(
                "Assets/Game/Module/Narrative/Tests/Fixtures/PresentationE0/E0StoryFixture.asset");
            Assert.IsNotNull(asset);
            Assert.IsTrue(asset.TryReadDefinition(new FixtureCatalog(), out var story, out var errors), string.Join("\n", errors));
            string baseline = NovelCompatibility.Fingerprint(story);
            Assert.AreEqual(PRESENTATION_E0_FINGERPRINT, baseline, "改动前的实测基线必须保持不变");

            InstallLocalization();
            try
            {
                // 装上多语言、并且逐个语言切换，指纹都不允许变——存档兼容性就靠这一条。
                foreach (string language in new[] { "zh_Hant", "ja", "en", "zh_Hans" })
                {
                    NovelLanguageSettings.SetPreview(language);
                    Assert.AreEqual(baseline, NovelCompatibility.Fingerprint(story), "切到 " + language + " 后指纹变了");
                }
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void KeyFieldsRoundTripAndOldDataStaysCompatible()
        {
            var command = new NovelCommand("say1", NovelCommandKind.Say, "原文", "line1", textKey: "text.probe.1");
            Assert.AreEqual("text.probe.1", command.TextKey);
            var roundtrip = JsonUtility.FromJson<NovelCommand>(JsonUtility.ToJson(command));
            Assert.AreEqual("text.probe.1", roundtrip.TextKey, "JsonUtility 往返必须保留 Key");
            // 旧数据没有这个字段：反序列化后必须是空 Key，行为与接入前一致。
            var legacy = JsonUtility.FromJson<NovelCommand>("{\"_commandId\":\"old\",\"_kind\":0,\"_lineId\":\"l\",\"_text\":\"旧台词\"}");
            Assert.IsTrue(string.IsNullOrEmpty(legacy.TextKey));
            Assert.AreEqual("旧台词", legacy.Text);

            InstallLocalization();
            try
            {
                // 章节显示名：填 Key 取译文，留空回退 DisplayName。
                var chapter = Create<NarrativeChapterSO>();
                Set(chapter, "_displayName", "教室");
                Set(chapter, "_displayNameKey", "ui.main.Start");
                string localized = chapter.LocalizedDisplayName;
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", out string expected));
                Assert.AreEqual(expected, localized);
                Set(chapter, "_displayNameKey", string.Empty);
                Assert.AreEqual("教室", chapter.LocalizedDisplayName);
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void RunnerResolvesTextKeyPerLanguageAndFallsBack()
        {
            InstallLocalization();
            try
            {
                // 未设置语言 → 源语言列。
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "zh_Hans", out string zh));
                Assert.AreEqual(zh, RunFirstLine("ui.main.Start", string.Empty));
                // 同一份定义换语言 → 文本副本按语言重建（语言必须进缓存键）。
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "en", out string en));
                Assert.AreEqual(en, RunFirstLine("ui.main.Start", "en"));
                Assert.AreNotEqual(zh, en);
                // 缺条目的 Key → 回退资产里的原文，不报错。
                Assert.AreEqual("原文台词", RunFirstLine("nope.missing.key", "en"));
                // 没有 Key → 完全保持原文。
                Assert.AreEqual("原文台词", RunFirstLine(string.Empty, "en"));
            }
            finally { RemoveLocalization(); }
        }

        /// <summary>
        /// 会话进行中切语言的三条契约：当前句按新语言重建、同一条 Say 的显示进度不重置、
        /// 历史回看跟着当前语言走。外加框架广播点的唯一定义——发布一次就播报一次 LanguageChanged。
        /// </summary>
        [Test]
        public void LanguageSwitchMidSessionRefreshesVisibleTextHistoryAndBroadcast()
        {
            InstallLocalization();
            try
            {
                // 挂一个四语言都齐全的真实表项，这样断言不依赖某个语言的翻译进度。
                Commands(new NovelCommand("line", NovelCommandKind.Say, "源语言台词", "line", textKey: "ui.main.Start"));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "zh_Hans", out string zh));
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.main.Start", "en", out string en));
                Assert.AreNotEqual(zh, en);

                NovelLanguageSettings.SetPreview("zh_Hans");
                using var session = ReadingSession(out _, out _);
                var view = new TextView { Capacity = 100 };
                session.AttachView(view);
                Assert.AreEqual(zh, view.Command.Text, "源语言下应按源语言列显示");

                session.Tick(.2f, 1);
                int visibleBefore = view.Visible;
                Assert.Greater(visibleBefore, 0, "先让打字机推进一段，后面的「进度不被重置」才有意义");

                NovelLanguageSettings.SetPreview("en");
                session.RefreshLocalization();
                Assert.AreEqual(en, view.Command.Text, "切语言后当前句必须按新语言重建");
                Assert.AreEqual(visibleBefore, view.Visible, "同一条 Say 只换文本，显示进度不能被重置");

                for (int frame = 2; frame <= 8 && session.Snapshot.State != NarrativeState.AwaitingAdvance; frame++) session.Advance(frame);
                Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
                var entry = session.History[0];
                Assert.IsTrue(entry.TextKey == "ui.main.Start", "历史条目要记住 Key 才能回看时重解析");
                Assert.AreEqual(en, NovelLocalization.HistoryText(entry), "历史回看应跟着当前语言");
                NovelLanguageSettings.SetPreview("zh_Hans");
                Assert.AreEqual(zh, NovelLocalization.HistoryText(entry), "再切回来也要能重解析");

                // 广播点：唯一发布处先重刷 TMPEx，再播报新语言标识。
                int fired = 0; string payload = null;
                using (EmberEventBus.Subscribe<string>(EmberBroadcastEvent.LanguageChanged, code => { fired++; payload = code; }))
                    TextLocalization.PublishLanguageChanged("ja");
                Assert.AreEqual(1, fired);
                Assert.AreEqual("ja", payload);
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void SkinAppliesSpriteOverridesOnlyForMatchingPageAndSkin()
        {
            NovelLanguageSettings.SetPreview(string.Empty);
            NovelSkin.Install(_tables);
            try
            {
                Assert.IsTrue(_tables.SkinsReady, "配表里缺少皮肤三张表");

                // 阅读页：控件 History 下的子节点 SkinIcon。
                var readerRoot = new GameObject("EUINovelReaderPage", typeof(RectTransform)); _assets.Add(readerRoot);
                var history = new GameObject("History", typeof(RectTransform), typeof(Image));
                history.transform.SetParent(readerRoot.transform, false);
                var icon = new GameObject("SkinIcon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(history.transform, false);
                var iconImage = icon.GetComponent<Image>();
                var readerLogic = new EUILogic { ControlMap = new System.Collections.Generic.Dictionary<string, Component>() };
                readerLogic.ControlMap["History"] = history.GetComponent<RectTransform>();

                int applied = NovelSkin.Apply(readerLogic, "EUINovelReaderPage", "lastlight_alt");
                Assert.AreEqual(1, applied, "演示皮肤应命中阅读页的 History/SkinIcon");
                Assert.IsNotNull(iconImage.sprite);
                Assert.AreEqual("return", iconImage.sprite.name);

                // 页面不匹配 → 一行都不该命中。
                Assert.AreEqual(0, NovelSkin.Apply(readerLogic, "EUINovelSavePage", "lastlight_alt"));
                // 皮肤不匹配（默认皮肤没有覆盖行）→ 保持 Prefab 外观。
                iconImage.sprite = null;
                Assert.AreEqual(0, NovelSkin.Apply(readerLogic, "EUINovelReaderPage", "lastlight_default"));
                Assert.IsNull(iconImage.sprite);
                // 页面名不认识 → 不越界套用。
                Assert.AreEqual(0, NovelSkin.Apply(readerLogic, "EUINovelReaderPage", "__no_such_skin__"));
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void SkinResolutionReturnsNothingForUnassignedStory()
        {
            NovelLanguageSettings.SetPreview(string.Empty);
            NovelSkin.Install(_tables);
            try
            {
                // 没有被 novel_story_skin 赋值的剧情不套皮肤。
                Assert.IsFalse(_tables.TryGetSkinId("__no_such_story__", out string skinId));
                Assert.IsNull(skinId);
                // 没有装配皮肤配表时整体降级，不抛异常。
                NovelSkin.Uninstall();
                var logic = new EUILogic { ControlMap = new System.Collections.Generic.Dictionary<string, Component>() };
                Assert.AreEqual(0, NovelSkin.Apply(logic, "EUINovelReaderPage", "lastlight_alt"));
                Assert.IsFalse(NovelSkin.IsInstalled);
            }
            finally { RemoveLocalization(); }
        }

        [Test]
        public void SampleStoryFingerprintsStillMatchThePreChangeBaseline()
        {
            // 与 NovelActionHandleTests 的同名断言互为冗余：任何一侧被改动都会立刻暴露。
            var e0 = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(
                "Assets/Game/Module/Narrative/Tests/Fixtures/PresentationE0/E0StoryFixture.asset");
            Assert.IsTrue(e0.TryReadDefinition(new FixtureCatalog(), out var e0Story, out _));
            Assert.AreEqual(PRESENTATION_E0_FINGERPRINT, NovelCompatibility.Fingerprint(e0Story));

            var m1 = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(
                "Assets/Game/Module/Narrative/Tests/Fixtures/M1Sample/M1StoryFixture.asset");
            Assert.IsTrue(m1.TryReadDefinition(new FixtureCatalog(), out var m1Story, out _));
            Assert.AreEqual(M1_SAMPLE_FINGERPRINT, NovelCompatibility.Fingerprint(m1Story));
        }
        #endregion
    }
}
