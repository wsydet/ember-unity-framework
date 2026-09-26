using System;
using System.Collections;
using System.Linq;
using Ember.Core;
using Game.NovelSave;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    /// <summary>
    /// 名字输入节点（<c>NovelPlayerNameInputStep</c>）**前 / 中 / 后**的存档与读档契约。
    ///
    /// <para><b>示例剧情里的位置</b>（<c>CH01_intro</c>）：
    /// <c>sample_open_segment_on</c>（锁定玩家推进、切自动播放）→ <c>sample_open_line_1</c> →
    /// <c>sample_open_line_2</c>（**输入前**的稳定点）→ <c>sample_open_name</c>（**输入节点**）→
    /// <c>sample_open_line_3</c>（用 <c>{playerName}</c> 绑定的那句，**输入后**）→ <c>sample_open_segment_off</c>。</para>
    ///
    /// <para><b>为什么这些断言是这样的</b>（都按源码核实，不靠猜）：
    /// <list type="number">
    /// <item>保存门控有两层：<c>EUINovelSavePage.Refresh</c> 只在
    /// <c>AwaitingAdvance</c>/<c>AwaitingChoice</c> 允许保存；<c>NarrativeRunner.TryCapture</c>
    /// 自己也会拒绝非稳定点，文案是「请等待当前句完整显示或选项准备完成后保存」。
    /// 输入节点等待期间状态是 <c>Executing</c> + <c>Wait.CustomStep</c>，两层都会挡住保存。</item>
    /// <item>恢复是「原地回到存档时的 stop」（<c>NarrativeRunner.TryRestore</c>），不重放本章节，
    /// 所以「输入前」的档读回来停在 <c>sample_open_line_2</c>，推进后才会再次进入输入节点。</item>
    /// <item>名字写进**全局变量** <c>playerName</c>，全局变量会进存档。</item>
    /// <item>输入页 <c>OnClose</c> 一定会 <c>Cancel()</c> 写回默认名，因此任何关闭路径
    /// （包括读档拆掉旧会话）都不会让剧情卡在等待步骤。</item>
    /// <item>开场段落（<c>NovelOpeningSegmentSO</c>）的推进锁与自动播放**不进存档**，
    /// 所以读档回到段落内部后是手动阅读、没有锁：用例里用显式 <c>Advance</c> 继续。</item>
    /// </list></para>
    ///
    /// <para><b>手动运行方式</b>：Window → General → Test Runner → PlayMode →
    /// 过滤 <c>NameInput</c> → Run。用例会把存档目录换到 <c>.utmp/name-input-saves/&lt;guid&gt;</c>，
    /// 不会碰你本机的真实存档。</para>
    /// </summary>
    public sealed partial class NovelGameplayTests
    {
        #region 内部参数

        private const string NAME_INPUT_PAGE = "EUINovelNameInputPage";
        /// <summary>输入节点之前那一句：可保存的稳定点。</summary>
        private const string NAME_BEFORE_COMMAND = "sample_open_line_2";
        /// <summary>输入节点本身（自定义步骤）。</summary>
        private const string NAME_STEP_COMMAND = "sample_open_name";
        /// <summary>输入节点之后、用 {playerName} 绑定的那一句。</summary>
        private const string NAME_AFTER_COMMAND = "sample_open_line_3";
        /// <summary>剧情变量 playerName 的初始值，也是输入步骤的 _defaultName。</summary>
        private const string DEFAULT_NAME = "旅人";

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 把存档目录换成临时目录（不污染本机存档），并把文字速度调到最快以缩短用例时长。
        /// 自动间隔保持小值；开场段落自己会用固定间隔覆盖它。
        /// </summary>
        private NovelSaveModule IsolateSaveStore()
        {
            Assert.IsTrue(EmberModuleCollector.Instance.TryGetModule(out NovelSaveModule saves), "存档模块未装配");
            typeof(NovelSaveModule).GetProperty(nameof(saves.Store)).SetValue(saves,
                new NovelSaveStore(System.IO.Path.Combine(".utmp/name-input-saves", Guid.NewGuid().ToString("N"))));
            typeof(NovelSaveModule).GetProperty(nameof(saves.Account)).SetValue(saves,
                new NovelAccountData { TextSpeed = 200, AutoInterval = 1f });
            return saves;
        }

        private IEnumerator StartNewGameAndWaitForReader()
        {
            yield return Wait(() => Page("EUIMainPage"), "主菜单未就绪");
            Button(Page("EUIMainPage"), "Btn_Start").onClick.Invoke();
            yield return Wait(() => Session?.IsReady == true && Page("EUINovelReaderPage") != null &&
                Session.Snapshot.PauseReasons.Count == 0 && !NovelLoadInProgress, "新游戏未进入阅读页");
        }

        /// <summary>等到剧情停在某个指定指令的「已完整显示」稳定点。</summary>
        private IEnumerator WaitStableCommand(string commandId)
        {
            yield return Wait(() => Session?.Snapshot.CommandId == commandId &&
                Session.Snapshot.State == NarrativeState.AwaitingAdvance &&
                Session.Snapshot.PauseReasons.Count == 0, "未停在稳定点 " + commandId);
        }

        private IEnumerator WaitNameInputPageOpen()
        {
            yield return Wait(() => Page(NAME_INPUT_PAGE) != null, "名字输入页未打开");
        }

        /// <summary>阅读菜单 → 存读档页（真实 UI 路径：菜单的「存读档」会先关菜单再开存档页）。</summary>
        private IEnumerator OpenSavePageFromMenu()
        {
            var reader = Page("EUINovelReaderPage");
            Assert.IsNotNull(reader, "阅读页缺失");
            Button(reader, "Menu").onClick.Invoke();
            yield return Wait(() => Page("EUINovelReadingMenuPage"), "阅读菜单未打开");
            Button(Page("EUINovelReadingMenuPage"), "Saves").onClick.Invoke();
            yield return Wait(() => Page("EUINovelSavePage") != null && Page("EUINovelReadingMenuPage") == null, "存读档页未打开");
        }

        /// <summary>槽位 i 的第 slot 个按钮（槽位条目是运行时实例化的，按层级顺序与槽位号一致）。</summary>
        private Button SlotButton(int slot, string name)
        {
            var page = Page("EUINovelSavePage");
            Assert.IsNotNull(page, "存读档页未打开");
            return page.GetComponentsInChildren<Button>(true)
                .Where(x => x.name == name || x.name == "m_" + name).ElementAt(slot);
        }

        private IEnumerator CloseSavePage()
        {
            Button(Page("EUINovelSavePage"), "Close").onClick.Invoke();
            yield return Wait(() => Page("EUINovelSavePage") == null, "存读档页未关闭");
        }

        /// <summary>在输入页里填名字并确认（留空/纯空白会走默认名）。</summary>
        private IEnumerator SubmitName(string name)
        {
            var popup = Page(NAME_INPUT_PAGE);
            Assert.IsNotNull(popup, "名字输入页未打开");
            var field = popup.GetComponentInChildren<TMPro.TMP_InputField>(true);
            Assert.IsNotNull(field, "名字输入页缺少输入框");
            field.text = name;
            Button(popup, "Btn_Confirm").onClick.Invoke();
            yield return Wait(() => Page(NAME_INPUT_PAGE) == null, "名字输入页未关闭");
        }

        private string PlayerName()
        {
            var snapshot = Session?.Snapshot;
            Assert.IsNotNull(snapshot, "会话缺失");
            Assert.IsTrue(snapshot.GlobalVariables.ContainsKey("playerName"), "剧情未声明 playerName 全局变量");
            return snapshot.GlobalVariables["playerName"].String;
        }

        /// <summary>阅读页正文控件的当前文本（按节点名取，不依赖安全区路径）。</summary>
        private string ReaderBodyText()
        {
            var reader = Page("EUINovelReaderPage");
            Assert.IsNotNull(reader, "阅读页缺失");
            var body = reader.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(t => t.name == "Body");
            Assert.IsNotNull(body, "阅读页正文控件未找到");
            return body.text;
        }

        private IEnumerator WaitLoadFinished(NovelSession previous)
        {
            yield return Wait(() => !NovelLoadInProgress && Session != null && !ReferenceEquals(Session, previous) &&
                Session.Snapshot.State == NarrativeState.AwaitingAdvance &&
                Session.Snapshot.PauseReasons.Count == 0, "读档未完成");
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// A：**输入前**存档 → 读回。
        /// <para>前置：新游戏走到 <c>sample_open_line_2</c>（输入节点前一句，稳定点）。</para>
        /// <para>操作：存档到手动槽 0 → 走到输入节点输入「甲」→ 走到输入后的那句 → 读回槽 0。</para>
        /// <para>预期：读回后停在输入前那句、**不**弹输入页、名字回到默认值（不能残留「甲」）；
        /// 再推进一次必须**重新询问**，并能写入新名字「乙」。</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NameInputSaveBeforeRestoresToTheLineAndAsksAgain()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground; Application.runInBackground = true;
            try
            {
                var saves = IsolateSaveStore();
                yield return StartNewGameAndWaitForReader();
                yield return WaitStableCommand(NAME_BEFORE_COMMAND);
                Assert.IsNull(Page(NAME_INPUT_PAGE), "输入节点之前不应已经弹出输入页");

                // 停下自动播放，避免存档操作期间剧情自己往前走（开场段落锁着玩家推进，只能靠切模式停）。
                Session.SetReadMode(NarrativeReadMode.Manual);

                yield return OpenSavePageFromMenu();
                Assert.IsTrue(SlotButton(0, "Write").interactable, "稳定点上的手动槽应可保存");
                SlotButton(0, "Write").onClick.Invoke();
                yield return Wait(() => saves.Store.Slots.Any(s => s.Slot == 0), "输入前的存档未落盘");
                yield return Wait(() => !saves.IsBusy, "存档模块未回到空闲");
                yield return CloseSavePage();

                // 放开自动播放，让剧情自己走进输入节点。
                Session.SetReadMode(NarrativeReadMode.Auto);
                yield return WaitNameInputPageOpen();
                Assert.AreEqual(NAME_STEP_COMMAND, Session.Snapshot.CommandId, "输入页应对应输入节点");
                yield return SubmitName("甲");

                yield return WaitStableCommand(NAME_AFTER_COMMAND);
                Assert.AreEqual("甲", PlayerName(), "输入的名字应写回 playerName");
                Assert.IsTrue(ReaderBodyText().Contains("甲"), "输入后的正文应显示玩家输入的名字");

                // 读回「输入前」的档。
                var previous = Session;
                yield return OpenSavePageFromMenu();
                Assert.IsTrue(SlotButton(0, "Read").interactable, "有档的槽位读取按钮应可用");
                SlotButton(0, "Read").onClick.Invoke();
                yield return WaitLoadFinished(previous);

                Assert.IsNull(Page(NAME_INPUT_PAGE), "读回输入前的档后不应停在已经询问过的输入页");
                Assert.AreEqual(NAME_BEFORE_COMMAND, Session.Snapshot.CommandId, "应回到输入节点前那一句");
                Assert.AreEqual(DEFAULT_NAME, PlayerName(), "输入前的档里名字应还是默认值，不能残留「甲」");

                // 预期：推进一次必须重新询问。
                Session.Advance(Time.frameCount);
                yield return WaitNameInputPageOpen();
                yield return SubmitName("乙");
                Assert.AreEqual("乙", PlayerName(), "重新询问后写入的新名字应生效");
            }
            finally { Application.runInBackground = background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        /// <summary>
        /// B：**输入页打开期间**的保存与读档。
        /// <para>前置：先按 A 的路径在输入前存好手动槽 0，再走到输入页停下。</para>
        /// <para>操作与预期：
        /// ① 存档页里所有保存按钮必须**禁用**，有档槽位的读取按钮仍可用；
        /// ② 程序化 <c>NovelSaveModule.Save(6)</c> 必须被拒（文案来自「请等待当前句完整显示」这条门控）、
        ///    **不写盘**且不改动既有存档；
        /// ③ 此时读回槽 0：输入页必须**干净关闭**、不残留暂停、剧情回到输入前那句；
        /// ④ 再推进一次必须能**再次**询问（不会被永久跳过）；
        /// ⑤ 这一步留空点确定：写默认名，并且剧情必须继续（不能卡在等待步骤）。</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NameInputOpenRefusesSaveAndLoadClosesInputPageCleanly()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground; Application.runInBackground = true;
            try
            {
                var saves = IsolateSaveStore();
                yield return StartNewGameAndWaitForReader();
                yield return WaitStableCommand(NAME_BEFORE_COMMAND);

                Session.SetReadMode(NarrativeReadMode.Manual);
                yield return OpenSavePageFromMenu();
                SlotButton(0, "Write").onClick.Invoke();
                yield return Wait(() => saves.Store.Slots.Any(s => s.Slot == 0), "输入前的存档未落盘");
                yield return Wait(() => !saves.IsBusy, "存档模块未回到空闲");
                yield return CloseSavePage();
                Session.SetReadMode(NarrativeReadMode.Auto);

                yield return WaitNameInputPageOpen();
                var session = Session;
                Assert.AreEqual(NarrativeState.Executing, session.Snapshot.State, "输入期间会话应停在执行态而不是稳定点");
                Assert.IsTrue((session.Snapshot.Wait & NarrativeWait.CustomStep) != 0, "输入期间应等待自定义节点");

                // ① 输入期间：保存按钮全部禁用，读取按钮不受影响。
                yield return OpenSavePageFromMenu();
                for (int slot = 0; slot < 7; slot++)
                    Assert.IsFalse(SlotButton(slot, "Write").interactable, "输入期间槽位 " + slot + " 的保存按钮必须禁用");
                Assert.IsTrue(SlotButton(0, "Read").interactable, "读取按钮不该跟着一起禁用");

                // 必须给出明确提醒，而不是只把按钮变灰。
                Assert.IsTrue(NovelLocalization.TryGetContent("ui.save.InputPending", out string inputPending),
                    "配表缺少「输入期间不可保存」的提醒文案");
                var feedback = Page("EUINovelSavePage").GetComponentsInChildren<TMPro.TMP_Text>(true)
                    .FirstOrDefault(t => t.name == "Feedback");
                Assert.IsNotNull(feedback, "存读档页缺少反馈文本");
                StringAssert.Contains(inputPending, feedback.text, "输入期间打开存读档页必须显示提醒文案");

                // ② 程序化保存同样必须被拒，并且不写盘、不动既有存档。
                long slot0Ticks = saves.Store.Slots.Single(s => s.Slot == 0).SavedUtcTicks;
                Assert.IsFalse(saves.Save(6), "输入期间快捷保存必须被拒绝");
                Assert.IsNotEmpty(saves.Message, "被拒绝的保存必须给出提示");
                StringAssert.Contains("请等待", saves.Message, "拒绝理由应来自「当前句未完整显示」这条门控，而不是别的原因");
                Assert.IsFalse(saves.Store.Slots.Any(s => s.Slot == 6), "被拒绝的保存不能写盘");
                Assert.AreEqual(slot0Ticks, saves.Store.Slots.Single(s => s.Slot == 0).SavedUtcTicks, "被拒绝的保存不能改动既有存档");

                // ③ 输入页开着时读档：必须干净关掉输入页、回到输入前那句。
                SlotButton(0, "Read").onClick.Invoke();
                yield return WaitLoadFinished(session);
                Assert.IsNull(Page(NAME_INPUT_PAGE), "读档后输入页必须已经关闭，不能残留悬浮弹窗");
                Assert.AreEqual(NAME_BEFORE_COMMAND, Session.Snapshot.CommandId, "应回到输入前那句");
                Assert.AreEqual(DEFAULT_NAME, PlayerName(), "读回输入前的档，名字应还是默认值");
                Assert.IsEmpty(Session.Snapshot.PauseReasons, "读档后不应残留暂停");

                // ④ 再次询问：不会被永久跳过。
                Session.Advance(Time.frameCount);
                yield return WaitNameInputPageOpen();

                // ⑤ 留空确定 → 默认名，且剧情必须继续。
                yield return SubmitName("   ");
                Assert.AreEqual(DEFAULT_NAME, PlayerName(), "留空必须写默认名，不能写空白");
                yield return Wait(() => Session?.Snapshot.CommandId == NAME_AFTER_COMMAND &&
                    Session.Snapshot.State == NarrativeState.AwaitingAdvance, "留空确定后剧情没有继续（卡在等待步骤）");
                Assert.IsTrue(ReaderBodyText().Contains(DEFAULT_NAME), "留空时正文应显示默认名");
            }
            finally { Application.runInBackground = background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        /// <summary>
        /// C：**输入后**存档 → 读回。
        /// <para>前置：走到输入节点输入「小雨」，停在输入后那句 <c>sample_open_line_3</c>。</para>
        /// <para>操作：存档到槽 0 → 继续走到更远的章节卡 → 读回槽 0。</para>
        /// <para>预期：读回后停在保存时那句、名字仍是「小雨」、正文显示「小雨」、
        /// **不再询问名字**（输入页不得出现）。</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NameInputSaveAfterKeepsEnteredNameAndDoesNotAskAgain()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground; Application.runInBackground = true;
            try
            {
                var saves = IsolateSaveStore();
                yield return StartNewGameAndWaitForReader();
                yield return WaitNameInputPageOpen();
                yield return SubmitName("小雨");
                yield return WaitStableCommand(NAME_AFTER_COMMAND);
                Assert.AreEqual("小雨", PlayerName());

                Session.SetReadMode(NarrativeReadMode.Manual);
                yield return OpenSavePageFromMenu();
                Assert.IsTrue(SlotButton(0, "Write").interactable, "输入后的稳定点应可保存");
                SlotButton(0, "Write").onClick.Invoke();
                yield return Wait(() => saves.Store.Slots.Any(s => s.Slot == 0), "输入后的存档未落盘");
                yield return Wait(() => !saves.IsBusy, "存档模块未回到空闲");
                yield return CloseSavePage();

                // 继续走到更远处（章节卡），确保后面的读档确实发生了回退。
                Session.SetReadMode(NarrativeReadMode.Auto);
                yield return DriveOpeningToChapterCard(Session);
                Assert.AreNotEqual(NAME_AFTER_COMMAND, Session.Snapshot.CommandId, "应已经离开保存时那句");

                var previous = Session;
                yield return OpenSavePageFromMenu();
                SlotButton(0, "Read").onClick.Invoke();
                yield return WaitLoadFinished(previous);

                Assert.AreEqual(NAME_AFTER_COMMAND, Session.Snapshot.CommandId, "应回到保存时那一句");
                Assert.IsNull(Page(NAME_INPUT_PAGE), "输入后的档读回来不得再问一次名字");
                Assert.AreEqual("小雨", PlayerName(), "读档必须保留当时输入的名字");
                Assert.IsTrue(ReaderBodyText().Contains("小雨"), "读档后的正文应显示存档里的名字");
            }
            finally { Application.runInBackground = background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        #endregion
    }
}
