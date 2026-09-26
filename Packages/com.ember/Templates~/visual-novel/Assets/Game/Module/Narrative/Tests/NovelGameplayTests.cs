using System.Collections;
using System.Linq;
using Ember.Core;
using Ember.UIExtension;
using Ember.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Narrative.Tests
{
    public sealed partial class NovelGameplayTests
    {
        [TestCase("EUINovelPortraitItem", NovelCommandKind.Character)]
        [TestCase("EUINovelBackgroundItem", NovelCommandKind.Background)]
        public void VisualHideKeepsSpriteUntilTransparentAndNeverShowsEmptyQuad(string prefabName, NovelCommandKind kind)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/" + prefabName + ".prefab");
            var root = Object.Instantiate(prefab);
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
            EUIItem item = null;
            try
            {
                Assert.IsTrue(EUIItemFactory.TryCreate(root, out item, out var error), error);
                item.Show();
                var picture = root.GetComponentInChildren<Image>(true);
                var apply = item.Logic.GetType().GetMethod("Apply");
                var show = new NovelCommand("show", kind);
                var hide = new NovelCommand("hide", kind, duration: 1, visualAction: NovelVisualAction.Hide);
                void Apply(NovelCommand command, Sprite image, float progress) =>
                    apply.Invoke(item.Logic, new object[] { command, image, progress });
                void AssertEmpty()
                {
                    Assert.IsNull(picture.sprite);
                    Assert.AreEqual(0, picture.color.a);
                    Assert.IsFalse(picture.enabled, "Empty Image must not render a white quad");
                }
                // Hiding a slot that has never been populated must remain invisible.
                Apply(hide, null, .1f); AssertEmpty();
                Apply(show, sprite, 1);
                Apply(hide, null, .25f);
                Assert.AreSame(sprite, picture.sprite);
                Assert.AreEqual(.75f, picture.color.a);
                Assert.IsTrue(picture.enabled);
                Apply(hide, null, .99f);
                Assert.AreSame(sprite, picture.sprite);
                Apply(hide, null, 1); AssertEmpty();
                // A later Hide restarts progress but must not revive the now-empty slot.
                Apply(hide, null, 0); AssertEmpty();
                Apply(hide, null, .5f); AssertEmpty();
                Apply(show, sprite, .5f);
                Assert.AreSame(sprite, picture.sprite);
                Assert.AreEqual(.5f, picture.color.a);
                Assert.IsTrue(picture.enabled, "A new Show must re-enable the image");
                Apply(hide, null, 1); AssertEmpty();
                Apply(show, null, .5f); AssertEmpty();
            }
            finally
            {
                item?.Dispose(); Object.DestroyImmediate(root);
                Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
            }
        }

        [TestCase(-1, false, false, 3)]
        [TestCase(6, true, false, 4)]
        [TestCase(7, true, false, 4)]
        [TestCase(0, true, true, 5)]
        public void MainMenuVisibilityAndLayoutFollowSlotKinds(int slot, bool resume, bool load, int count)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPanel.prefab");
            var root = Object.Instantiate(prefab);
            var page = new EUIPage(root);
            try
            {
                EUIBindingBridge.Attach(page, root.GetComponent<EUIBinding>());
                var store = new Game.NovelSave.NovelSaveStore(System.IO.Path.Combine(".utmp/visual-novel-m3/menu-tests", System.Guid.NewGuid().ToString("N")));
                if (slot >= 0) Assert.IsTrue(store.Save(slot, new NovelCheckpoint { Stop = NarrativeState.AwaitingAdvance }, out _));
                var module = new Game.NovelSave.NovelSaveModule();
                typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(module.Store)).SetValue(module, store);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                page.Logic.GetType().GetField("_novelSave", flags).SetValue(page.Logic, module);
                page.Logic.GetType().GetMethod("RefreshNovelSave", flags).Invoke(page.Logic, null);
                var group = (RectTransform)root.transform.Find("Animator/EUISafeArea/Center/MenuButtons");
                // 两个入口分工不同：「继续游戏」续读最新进度，有任意存档就出现；
                // 「读取存档」打开槽位页，只在存在手动槽（0–5）时出现。
                Assert.AreEqual(resume, group.Find("NovelContinue").gameObject.activeSelf);
                Assert.AreEqual(load, group.Find("NovelLoad").gameObject.activeSelf);
                var buttons = group.GetComponentsInChildren<Button>(true).Where(b => b.gameObject.activeSelf).ToArray();
                Assert.AreEqual(count, buttons.Length);
                CollectionAssert.AreEqual(new[] { "m_Btn_Start", "NovelContinue", "NovelLoad", "m_Btn_Settings", "NovelQuit" }
                    .Where(n => (n != "NovelContinue" || resume) && (n != "NovelLoad" || load)), buttons.Select(b => b.name));
                LayoutRebuilder.ForceRebuildLayoutImmediate(group);
                float contentHeight = buttons.Sum(b => LayoutUtility.GetPreferredHeight((RectTransform)b.transform));
                var layout = group.GetComponent<VerticalLayoutGroup>();
                Assert.AreEqual(contentHeight + (count - 1) * layout.spacing + layout.padding.vertical, group.rect.height, .1f,
                    "菜单高度应随可见按钮收缩，不能为隐藏的继续/读档按钮保留空行");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class DelayedReader : IEUIResourceProvider
        {
            internal IEUIResourceProvider Inner;
            internal System.Action<GameObject> Pending;
            internal string Path;
            internal int Released;
            public void LoadPrefabAsync(string path, System.Action<GameObject> callback)
            {
                if (path.Contains("EUINovelReaderPage")) { Path = path; Pending = callback; }
                else Inner.LoadPrefabAsync(path, callback);
            }
            public void Release(string path) { Released++; Inner.Release(path); }
            internal void Complete() { var callback = Pending; Pending = null; Inner.LoadPrefabAsync(Path, callback); }
        }

        /// <summary>
        /// 把「当前小说」临时钉到模板自带的验收样例 LastLight，并在 Dispose 时还原。
        ///
        /// 这些模板用例验证的是模板的读取 / 存档 / 演出链路，不是「消费项目此刻把哪一部小说设成了当前小说」。
        /// 消费项目把 <c>NarrativeLibrarySO.Current</c> 换成自己的小说之后，不钉住的话：
        /// 新游戏入口会打开另一部小说，而用例仍按 LastLight 的资源路径读指令表，
        /// 于是「规范示例的居中立绘应加载」这类断言必然失败——那是用例与当前小说耦合，不是模板缺陷。
        ///
        /// 还原时同时核对 <c>Library.asset</c> 的磁盘内容没有被写脏；资源引用的临时改写
        /// 只应存在于内存里。
        /// </summary>
        private sealed class PinnedSampleStory : System.IDisposable
        {
            private const string LIBRARY_ASSET = "Assets/GameResource/Resources/Config/Narrative/Library.asset";
            private readonly NarrativeLibrarySO _library;
            private readonly NarrativeStorySO _previous;
            private readonly string _before;
            internal NarrativeStorySO Story { get; }
            internal PinnedSampleStory()
            {
                _library = Resources.Load<NarrativeLibrarySO>(NarrativeLibrarySO.RESOURCE_PATH);
                Assert.IsNotNull(_library, "缺少小说入口配置：" + NarrativeLibrarySO.RESOURCE_PATH);
                Story = AssetDatabase.LoadAssetAtPath<NarrativeStorySO>(
                    "Assets/GameResource/Resources/Config/Narrative/LastLight/Story.asset");
                Assert.IsNotNull(Story, "缺少内置样例小说，无法在解耦的用例里钉住它");
                _previous = _library.Current;
                _before = System.IO.File.ReadAllText(LIBRARY_ASSET);
                SetCurrent(Story);
                Assert.AreSame(Story, _library.Current, "临时改写的当前小说没有生效");
            }
            public void Dispose()
            {
                if (!_library) return;
                SetCurrent(_previous);
                Assert.AreEqual(_before, System.IO.File.ReadAllText(LIBRARY_ASSET),
                    "用例只在内存里临时改写当前小说，不能把小说入口资产写脏");
            }
            private void SetCurrent(NarrativeStorySO story) => typeof(NarrativeLibrarySO)
                .GetField("_current", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_library, story);
        }

        /// <summary>背景是否已经落到首屏；未就位时由阅读页的不透明底板兜底（见 P4 的方案）。</summary>
        private static bool FirstScreenHasBackgroundOrOpaqueBackdrop(EUIBinding page, out bool backgroundApplied, out bool opaqueBackdrop)
        {
            var root = page.transform;
            backgroundApplied = root.Find("Background").GetComponentsInChildren<Image>(true).Any(image => image.sprite != null);
            var backdrop = root.Find("Backdrop");
            opaqueBackdrop = backdrop && backdrop.TryGetComponent(out Image plate) && plate.color.a >= 1f;
            return backgroundApplied || opaqueBackdrop;
        }

        private EUIBinding Page(string name) => GameLauncher.Instance.UIRoot
            .GetComponentsInChildren<EUIBinding>(true).FirstOrDefault(x => x.ClassName == name && x.gameObject.activeInHierarchy);
        private Button Button(EUIBinding page, string name) => page.GetComponentsInChildren<Button>(true)
            .First(x => x.name == name || x.name == "m_" + name);
        private bool NovelLoadInProgress => (bool)System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Game.UI.NovelSaveUI")).First(type => type != null)
            .GetProperty("IsLoading", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
        private NovelSession Session => NarrativeModule.TryGetInstance(out var module) ? module.Session : null;
        private IEnumerator Wait(System.Func<bool> ready, string message)
        {
            float deadline = Time.realtimeSinceStartup + 30;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(ready(), message + "；session=" + (Session?.Snapshot.State.ToString() ?? "null") +
                ", wait=" + Session?.Snapshot.Wait + ", pauses=" +
                (Session == null ? "" : string.Join(",", Session.Snapshot.PauseReasons)) +
                ", loading=" + NovelLoadInProgress + ", message=" +
                (EmberModuleCollector.Instance.TryGetModule(out Game.NovelSave.NovelSaveModule save) ? save.Message : ""));
        }

        /// <summary>
        /// 新开场是「瞬时黑幕 → 两句自动播放的内心独白 → 名字输入 → 一句话 → 黑幕散去 → 第一章章节卡」。
        /// 自动化用例按真实页面把它走完：输入页出现时替玩家填名字并确认，其余交给自动播放。
        /// </summary>
        private IEnumerator DriveOpeningToChapterCard(NovelSession session)
        {
            float deadline = Time.realtimeSinceStartup + 90;
            while (Time.realtimeSinceStartup < deadline)
            {
                var snapshot = session.Snapshot;
                if (snapshot.CommandId == "last_light_intro_e4_title" && snapshot.State == NarrativeState.AwaitingAdvance) yield break;
                var popup = Page("EUINovelNameInputPage");
                if (popup != null)
                {
                    var field = popup.GetComponentInChildren<TMPro.TMP_InputField>(true);
                    if (field != null) field.text = "自动测试";
                    Button(popup, "Btn_Confirm").onClick.Invoke();
                }
                yield return null;
            }
            Assert.Fail("开场未走到章节卡：" + session.Snapshot.CommandId + " state=" + session.Snapshot.State +
                " wait=" + session.Snapshot.Wait);
        }

        [UnityTest]
        public IEnumerator MainMenuContinueLoadsDirectlyAndLoadOpensChooser()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground; Application.runInBackground = true;
            try
            {
                // 与「当前小说是哪一部」解耦：本用例验证模板链路，临时钉住内置样例 LastLight。
                using var sample = new PinnedSampleStory();
                yield return Wait(() => GameLauncher.Instance && GameLauncher.Instance.UIRoot && Page("EUIMainPage"), "主菜单未就绪");
                Assert.IsTrue(EmberModuleCollector.Instance.TryGetModule(out Game.NovelSave.NovelSaveModule saves));
                string root = System.IO.Path.Combine(".utmp/visual-novel-m3/menu-flow-tests", System.Guid.NewGuid().ToString("N"));
                var store = new Game.NovelSave.NovelSaveStore(root);
                typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(saves.Store)).SetValue(saves, store);
                typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(saves.Account)).SetValue(saves, new Game.NovelSave.NovelAccountData());
                saves.ReportMessage("");
                var entryProvider = EUIViewEngine.Instance.ResourceProvider;
                var slowEntry = new DelayedReader { Inner = entryProvider };
                EUIViewEngine.Instance.ResourceProvider = slowEntry;
                try
                {
                    Button(Page("EUIMainPage"), "Btn_Start").onClick.Invoke();
                    yield return Wait(() => slowEntry.Pending != null, "未捕获新游戏阅读页加载");
                    var entryCover = EUIViewEngine.Instance.ActivePages.Single(p => p.Logic?.GetType().Name == "EUILoadingPage");
                    for (int frame = 0; frame < 30; frame++)
                    {
                        Assert.IsTrue(entryCover.IsOpened, "新游戏慢加载期间不能揭幕");
                        Assert.IsFalse(Session.IsReady);
                        yield return null;
                    }
                    slowEntry.Complete();
                    yield return Wait(() => !entryCover.IsOpened, "新游戏内容就绪后未揭幕");
                    Assert.IsTrue(Session.IsReady);
                    Assert.IsTrue(EUIViewEngine.Instance.ActivePages.Any(p => p.Logic?.GetType().Name == "EUINovelReaderPage" && p.IsOpened));
                    Assert.Contains(Session.Snapshot.State, new[] { NarrativeState.Revealing, NarrativeState.AwaitingAdvance });
                    Assert.AreEqual(NarrativeWait.None, Session.Snapshot.Wait &
                        (NarrativeWait.Resource | NarrativeWait.Presentation | NarrativeWait.Transition));
                    // P4：首屏允许以章节卡开场（此时背景指令排在它之后），但那时必须已有不透明底板兜底，
                    // 不能透出页面之后的画面。底板本身无条件是页面基色，所以这里始终要求它不透明。
                    Assert.IsTrue(FirstScreenHasBackgroundOrOpaqueBackdrop(Page("EUINovelReaderPage"), out bool entryBackground, out bool entryPlate),
                        "揭幕前首屏既没有背景，也没有不透明底板");
                    Assert.IsTrue(entryPlate, "阅读页必须始终有不透明底板：章节卡开场时它是唯一的底色（本次背景已就位=" + entryBackground + "）");
                }
                finally { EUIViewEngine.Instance.ResourceProvider = entryProvider; }
                yield return Wait(() => Session != null && Session.IsReady && Session.Snapshot.PauseReasons.Count == 0 && Page("EUINovelReaderPage"), "新游戏未进入阅读");
                Session.Advance(Time.frameCount);
                yield return Wait(() => Session.Snapshot.State == NarrativeState.AwaitingAdvance && !saves.IsBusy && store.Slots.Any(slot => slot.Slot == 7), "首个稳定点的自动存档尚未完成");
                string command = Session.Snapshot.CommandId;
                Assert.IsTrue(saves.Save(6)); yield return Wait(() => !saves.IsBusy && store.Latest == 6, "快速存档未完成");
                // Force a real reader load on the next entry so a slow provider can hold it unresolved.
                var reader = EUIViewEngine.Instance.ActivePages.First(p => p.Logic?.GetType().Name == "EUINovelReaderPage");
                typeof(EUIPage).GetField("_destroyDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(reader, 0f);
                GameLauncher.Instance.Fsm.TransitionTo<MainState>();
                yield return Wait(() => Page("EUIMainPage") && Session == null && !Page("EUILoadingPage"), "未返回菜单");
                // 两个入口分工不同：「继续游戏」续读最新进度（这里只有快速槽，所以它可见、读档不可见）。
                Assert.IsTrue(Button(Page("EUIMainPage"), "NovelContinue").gameObject.activeSelf);
                Assert.IsFalse(Button(Page("EUIMainPage"), "NovelLoad").gameObject.activeSelf);
                var provider = EUIViewEngine.Instance.ResourceProvider;
                var delayed = new DelayedReader { Inner = provider };
                EUIViewEngine.Instance.ResourceProvider = delayed;
                try
                {
                    // 「继续游戏」直接读最新档，不走槽位页。
                    Button(Page("EUIMainPage"), "NovelContinue").onClick.Invoke();
                    Assert.IsFalse(saves.IsBusy, "遮挡完成前不能开始读取");
                    yield return Wait(() => delayed.Pending != null, "未捕获继续游戏的阅读页加载");
                    var curtain = EUIViewEngine.Instance.ActivePages.Single(p => p.Logic?.GetType().Name == "EUILoadingPage");
                    for (int frame = 0; frame < 30; frame++)
                    {
                        Assert.IsTrue(curtain.IsOpened, "慢加载期间不能提前揭开遮挡");
                        Assert.IsTrue(curtain.Logic.SkipFakeProgress);
                        Assert.IsFalse(curtain.GameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(x => x.name == "m_Img_ProgressBar").gameObject.activeSelf);
                        Assert.AreEqual(1, EUIViewEngine.Instance.ActivePages.Count(p => p.Logic?.GetType().Name == "EUILoadingPage"));
                        Assert.IsNull(Page("EUINovelSavePage"), "继续游戏不应打开槽位页");
                        Assert.IsFalse(Session.IsReady);
                        yield return null;
                    }
                    delayed.Complete();
                    yield return Wait(() => !curtain.IsOpened, "恢复就绪后遮挡未开始退出");
                    Assert.IsTrue(Session.IsReady);
                    Assert.IsTrue(EUIViewEngine.Instance.ActivePages.Any(p => p.Logic?.GetType().Name == "EUINovelReaderPage" && p.IsOpened),
                        "遮挡开始退出前阅读页必须已经打开完成");
                    Assert.AreEqual("读档成功", saves.Message, "遮挡开始退出前必须发布恢复完成状态");
                    yield return Wait(() => Session?.IsReady == true && !Page("EUILoadingPage") && Session.Snapshot.PauseReasons.Count == 0, "继续后遮挡未结束");
                    // The status label falls back to the save module message after transient pauses clear.
                    yield return null;
                    Assert.AreEqual("读档成功", Page("EUINovelReaderPage").GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
                        .Single(t => t.name == "Status").text, "不能在揭幕后显示旧的读档中消息");
                }
                finally { EUIViewEngine.Instance.ResourceProvider = provider; }
                Assert.IsNotNull(Session); Assert.IsTrue(Session.IsReady); Assert.AreEqual(command, Session.Snapshot.CommandId);
                var old = Session;
                Button(Page("EUINovelReaderPage"), "QuickLoad").onClick.Invoke();
                Assert.AreSame(old, Session); Assert.IsFalse(old.IsDisposed, "遮挡前不能交换会话");
                yield return Wait(() => Page("EUILoadingPage"), "快速读档未显示遮挡");
                yield return Wait(() => Session != old && Session?.IsReady == true && !Page("EUILoadingPage") && Session.Snapshot.PauseReasons.Count == 0, "快速读档未完成");
                Assert.IsTrue(old.IsDisposed); Assert.IsNull(Page("EUINovelSavePage"));
                Assert.AreEqual("读档成功", saves.Message);
                Assert.AreEqual(command, Session.Snapshot.CommandId);

                // A corrupt quick save must uncover the original session and retain the damaged file.
                string payload = System.IO.Path.Combine(root, store.Slots.Single(s => s.Slot == 6).File);
                System.IO.File.WriteAllText(payload, "corrupt");
                old = Session;
                Button(Page("EUINovelReaderPage"), "QuickLoad").onClick.Invoke();
                yield return Wait(() => Page("EUILoadingPage"), "失败路径未显示遮挡");
                yield return Wait(() => !Page("EUILoadingPage") && Page("EUINovelSavePage"), "失败后未返回槽位页");
                Assert.AreSame(old, Session); Assert.IsFalse(old.IsDisposed);
                Assert.IsTrue(System.IO.File.Exists(payload)); Assert.AreEqual(6, store.Latest);
                Button(Page("EUINovelSavePage"), "Close").onClick.Invoke();
                yield return Wait(() => Session.Snapshot.PauseReasons.Count == 0, "失败关闭后未恢复输入");
                yield return Wait(() => !saves.IsBusy, "继续后仍忙碌");
                Assert.IsTrue(saves.Save(0)); yield return Wait(() => !saves.IsBusy && store.Latest == 0, "手动存档未完成");
                GameLauncher.Instance.Fsm.TransitionTo<MainState>();
                yield return Wait(() => Page("EUIMainPage") && Session == null && !Page("EUILoadingPage"), "未返回菜单");
                // 已经有手动槽 0，所以「继续游戏」与「读取存档」都应可见。
                Assert.IsTrue(Button(Page("EUIMainPage"), "NovelLoad").gameObject.activeSelf);
                Button(Page("EUIMainPage"), "NovelLoad").onClick.Invoke();
                yield return Wait(() => Page("EUINovelSavePage"), "读取存档未打开选择页");
                Assert.IsNull(Session, "打开选择页不应直接开始剧情");
                var slotPage = Page("EUINovelSavePage");
                var read = slotPage.GetComponentsInChildren<Button>().First(b => b.name == "Read" && b.interactable);
                read.onClick.Invoke();
                Button(slotPage, "Close").onClick.Invoke(); // cancellation before BeginLoad is dispatched
                yield return Wait(() => Page("EUILoadingPage"), "取消路径未进入遮挡");
                yield return Wait(() => !Page("EUILoadingPage") && !Page("EUINovelSavePage") &&
                    saves.Message?.Contains("取消") == true && Button(Page("EUIMainPage"), "NovelLoad").interactable, "取消后遮挡或输入未释放");
                Assert.IsNull(Session); Assert.IsFalse(saves.IsBusy);
                StringAssert.Contains("取消", saves.Message);
                Button(Page("EUIMainPage"), "NovelLoad").onClick.Invoke();
                yield return Wait(() => Page("EUINovelSavePage"), "取消后无法重新打开槽位页");
                Page("EUINovelSavePage").GetComponentsInChildren<Button>().First(b => b.name == "Read" && b.interactable).onClick.Invoke();
                yield return Wait(() => Page("EUILoadingPage"), "手动槽未进入遮挡");
                yield return Wait(() => Session?.IsReady == true && !Page("EUILoadingPage") && Session.Snapshot.PauseReasons.Count == 0, "手动槽恢复失败");
                Assert.AreEqual(command, Session.Snapshot.CommandId); Assert.IsNull(Page("EUINovelSavePage"));
                Assert.AreEqual("读档成功", saves.Message);
            }
            finally { Application.runInBackground = background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }

        [UnityTest]
        public IEnumerator RealMenuReaderBranchEndingAndRepeatedExit()
        {
            yield return NovelPlayModeScenes.EnterFrameworkScenePlayMode();
            bool background = Application.runInBackground;
            Application.runInBackground = true;
            try { yield return CheckGameplay(); }
            finally { Application.runInBackground = background; }
            yield return NovelPlayModeScenes.ExitIfPlaying();
        }
        [UnityTearDown]
        public IEnumerator ExitAfterFailedGameplayCheck()
        {
            // ExitPlayMode 必须由本方法直接 yield：在 [UnitySetUp]/[UnityTearDown] 里
            // 通过嵌套枚举器（包一层 helper）yield 退出指令会被框架拒绝，报
            // "Nested enumerators are not allowed to yield ExitPlayMode"。
            NovelPlayModeScenes.DiscardUnsavedScenes();
            if (EditorApplication.isPlayingOrWillChangePlaymode) yield return new ExitPlayMode();
            NovelPlayModeScenes.DiscardUnsavedScenes();
        }
        private IEnumerator CheckGameplay()
        {
            // 与「当前小说是哪一部」解耦：本用例按 LastLight 的资源路径读指令表并断言它的画面，
            // 所以先把当前小说临时钉到内置样例，结束时还原（见 PinnedSampleStory）。
            using var sample = new PinnedSampleStory();
            yield return Wait(() => GameLauncher.Instance && GameLauncher.Instance.UIRoot && Page("EUIMainPage"), "主菜单未就绪");
            // M3 自动存档/已读会在真实 Gameplay 中写盘，整合测试必须使用独立数据目录。
            Assert.IsTrue(EmberModuleCollector.Instance.TryGetModule(out Game.NovelSave.NovelSaveModule saves));
            string saveRoot = System.IO.Path.Combine(".utmp/visual-novel-m3/gameplay-tests", System.Guid.NewGuid().ToString("N"));
            typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(saves.Store)).SetValue(saves, new Game.NovelSave.NovelSaveStore(saveRoot));
            // 本用例要断言“首次点击补全文字”，固定低速以免首句在揭幕期间已显示完。
            typeof(Game.NovelSave.NovelSaveModule).GetProperty(nameof(saves.Account)).SetValue(saves, new Game.NovelSave.NovelAccountData { TextSpeed = 1 });
            var provider = EUIViewEngine.Instance.ResourceProvider;
            var delayed = new DelayedReader { Inner = provider };
            EUIViewEngine.Instance.ResourceProvider = delayed;
            try
            {
                Button(Page("EUIMainPage"), "Btn_Start").onClick.Invoke();
                yield return Wait(() => delayed.Pending != null && Session != null, "未捕获页面加载");
                var abandoned = Session;
                // 新游戏首屏未就绪时必须保持遮挡；不能等待尚未完成的阅读页加载自行揭幕。
                Assert.IsNotNull(Page("EUILoadingPage"), "阅读页慢加载期间必须保留遮罩");
                Assert.IsTrue(NovelLoadInProgress);
                GameLauncher.Instance.Fsm.TransitionTo<MainState>();
                yield return Wait(() => abandoned.IsDisposed, "取消会话应先释放小说资源");
                delayed.Complete();
                yield return Wait(() => Page("EUIMainPage") && Session == null && !NovelLoadInProgress && !Page("EUILoadingPage"), "取消慢加载后未完成返回菜单");
                yield return Wait(() => delayed.Released == 1, "迟到页面未释放");
                Assert.IsNull(Page("EUINovelReaderPage"));
                Assert.IsTrue(abandoned.IsDisposed); Assert.AreEqual(0, abandoned.OwnedResourceCount);
            }
            finally { EUIViewEngine.Instance.ResourceProvider = provider; }
            var assets = Resources.LoadAll<ScriptableObject>("Config/Narrative/LastLight");
            var before = assets.ToDictionary(x => x, JsonUtility.ToJson);
            var authoredCommands = assets.OfType<NarrativeDialogueSO>().SelectMany(x => x.Commands).ToDictionary(x => x.CommandId);
            for (int route = 0; route < 2; route++)
            {
                // 返回主菜单时，结局自动存档可能仍在写入；等待真实按钮恢复可用再发起下一轮。
                yield return Wait(() => !saves.IsBusy && !NovelLoadInProgress && Page("EUIMainPage") &&
                    Button(Page("EUIMainPage"), "Btn_Start").interactable &&
                    EUIViewEngine.Instance.ActivePages.Any(p => p.Logic?.GetType().Name == "EUIMainPage" && p.IsOpened),
                    "主菜单新游戏入口未恢复可用");
                Button(Page("EUIMainPage"), "Btn_Start").onClick.Invoke();
                // 开场是自动播放段落：玩家推进被锁住，所以这里等它自己走到稳定点，而不是靠点击补全文字。
                yield return Wait(() => Session != null && Session.Snapshot.State == NarrativeState.AwaitingAdvance && Session.Snapshot.PauseReasons.Count == 0 && !NovelLoadInProgress && !Page("EUILoadingPage") && Page("EUINovelReaderPage"), "阅读页面未开始对白");
                var session = Session;
                Assert.AreSame(session, NarrativeObservation.Current);
                var page = Page("EUINovelReaderPage");
                Assert.IsNotNull(page.transform.Find("Background").GetComponentInChildren<Image>(true).sprite, "示例背景应加载");
                Assert.IsNotNull(page.transform.Find("Center").GetComponentInChildren<Image>(true).sprite, "规范示例的居中立绘应加载（本用例已把当前小说钉到 LastLight）");
                string command = session.Snapshot.CommandId;
                Button(page, "Advance").onClick.Invoke(); Button(page, "Advance").onClick.Invoke();
                Assert.AreEqual(command, session.Snapshot.CommandId, "同帧双击不能跨句");
                Assert.AreEqual(NarrativeState.AwaitingAdvance, session.Snapshot.State);
                if (route == 0)
                {
                    System.IO.Directory.CreateDirectory(".utmp/visual-novel-m2");
                    ScreenCapture.CaptureScreenshot(".utmp/visual-novel-m2/reader.png");
                }
                Button(page, "Menu").onClick.Invoke();
                yield return Wait(() => Page("EUINovelReadingMenuPage"), "阅读菜单未打开");
                Button(Page("EUINovelReadingMenuPage"), "Settings").onClick.Invoke();
                yield return Wait(() => Page("EUISettingPage") && !Page("EUINovelReadingMenuPage") && session.Snapshot.PauseReasons.Count > 0, "系统设置未打开并暂停小说");
                var position = session.Snapshot.PositionVersion;
                for (int i = 0; i < 10; i++) { session.Advance(Time.frameCount); yield return null; }
                Assert.AreEqual(position, session.Snapshot.PositionVersion);
                GameLauncher.Instance.Fsm.Pop();
                yield return Wait(() => session.Snapshot.PauseReasons.Count == 0, "设置关闭未恢复小说");
                bool sawInner = false, sawDuo = false, sawEmptyStage = false, sawChoice = false;
                // Use the supported reading speed while still executing every action and real UI transition.
                // A frame count cannot bound authored seconds consistently on different editor frame rates.
                session.SetReadingMultiplier(3);
                float routeDeadline = Time.realtimeSinceStartup + 60;
                string lastCommand = session.Snapshot.CommandId;
                float progressDeadline = Time.realtimeSinceStartup + 15;
                while (Time.realtimeSinceStartup < routeDeadline && session.Snapshot.State != NarrativeState.Ended)
                {
                    Assert.AreNotEqual(NarrativeState.Faulted, session.Snapshot.State, session.Snapshot.Error?.ToString());
                    if (session.Snapshot.CommandId != lastCommand)
                    {
                        lastCommand = session.Snapshot.CommandId;
                        progressDeadline = Time.realtimeSinceStartup + 15;
                    }
                    if (Time.realtimeSinceStartup >= progressDeadline) Assert.Fail(
                        "剧情连续 15 秒未推进：" + session.Snapshot.CommandId + " state=" + session.Snapshot.State +
                        " wait=" + session.Snapshot.Wait + " pauses=" + string.Join(",", session.Snapshot.PauseReasons) +
                        " actions=" + string.Join(",", session.Actions.Select(a => a.Id + ":" + a.Status + ":" + a.Progress)));
                    if (session.Snapshot.State == NarrativeState.AwaitingChoice)
                    {
                        sawChoice = true;
                        var choices = page.GetComponentsInChildren<Button>().Where(x => x.name == "Select" && x.interactable).ToArray();
                        Assert.IsNotEmpty(choices);
                        choices[route == 0 ? 0 : choices.Length - 1].onClick.Invoke();
                    }
                    else if (session.Snapshot.CommandId == "sample_open_name" && Page("EUINovelNameInputPage") != null)
                    {
                        // 开局的名字输入是交互步骤。自动化用例仍然走真实的页面链路，
                        // 只是替玩家填名字并确认，并顺带验证名字确实写回了剧情变量。
                        var nameField = Page("EUINovelNameInputPage").GetComponentInChildren<TMPro.TMP_InputField>(true);
                        Assert.IsNotNull(nameField, "名字输入页应有输入框");
                        nameField.text = "自动测试";
                        Button(Page("EUINovelNameInputPage"), "Btn_Confirm").onClick.Invoke();
                        Assert.AreEqual("自动测试", session.Snapshot.GlobalVariables["playerName"].String,
                            "名字输入应写回 playerName 全局变量");
                    }
                    else if (session.Snapshot.State == NarrativeState.Revealing || session.Snapshot.State == NarrativeState.AwaitingAdvance)
                    {
                        var current = authoredCommands[session.Snapshot.CommandId];
                        sawInner |= current.CharacterId == "lastlight_inner";
                        var left = page.transform.Find("Left").GetComponentInChildren<Image>(true);
                        var right = page.transform.Find("Right").GetComponentInChildren<Image>(true);
                        var center = page.transform.Find("Center").GetComponentInChildren<Image>(true);
                        bool duo = left.sprite && right.sprite && left.color.a > .9f && right.color.a > .9f;
                        if (duo && !sawDuo && route == 0)
                        {
                            System.IO.Directory.CreateDirectory(".utmp/visual-novel-m5/sample-complete");
                            ScreenCapture.CaptureScreenshot(".utmp/visual-novel-m5/sample-complete/runtime-duo.png");
                        }
                        sawDuo |= duo;
                        sawEmptyStage |= (!left.sprite || left.color.a == 0) && (!right.sprite || right.color.a == 0) && (!center.sprite || center.color.a == 0);
                        Button(page, "Advance").onClick.Invoke();
                    }
                    yield return null;
                }
                Assert.AreEqual(NarrativeState.Ended, session.Snapshot.State,
                    "路线 " + route + " 超时：" + session.Snapshot.CommandId + " wait=" + session.Snapshot.Wait +
                    " actions=" + string.Join(",", session.Actions.Select(a => a.Id + ":" + a.Status + ":" + a.Progress)));
                Assert.AreEqual(route == 0 ? "last_light_heard" : "last_light_tomorrow", session.Snapshot.EndingId);
                Assert.IsTrue(sawInner && sawDuo && sawEmptyStage && sawChoice, "完整示例应覆盖内心独白、双人立绘、纯背景旁白和选择");
                Button(page, "Menu").onClick.Invoke();
                yield return Wait(() => Page("EUINovelReadingMenuPage"), "结局菜单未打开");
                Button(Page("EUINovelReadingMenuPage"), "ReturnMenu").onClick.Invoke();
                yield return Wait(() => Page("EUIMainPage") && Session == null, "未返回主菜单");
                Assert.IsTrue(session.IsDisposed); Assert.AreEqual(0, session.OwnedResourceCount);
                Assert.IsNull(NarrativeObservation.Current);
            }
            foreach (var pair in before) Assert.AreEqual(pair.Value, JsonUtility.ToJson(pair.Key), pair.Key.name);
        }
    }
}
