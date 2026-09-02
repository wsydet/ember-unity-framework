// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using Ember.Core;
using Ember.Basic;

using UnityEngine;
using UnityEngine.Profiling;

namespace Ember.UI
{
    /// <summary>
    /// UI 管理器（应用层）—— 开发者与 UI 系统的唯一入口。
    ///
    /// 管理"打开什么/何时打开"的逻辑：路由分发、显示队列、父子页面追踪、
    /// BG Mask 自动管理、跨场景 Loading 过渡。
    ///
    /// <para>依赖 <see cref="EUIViewEngine"/> 执行底层页面生命周期操作。</para>
    ///
    /// <para>使用示例：</para>
    /// <code>
    /// EUIManager.Instance.ShowMainPage(GamePages.MainMenu);
    /// EUIManager.Instance.ShowPopup(GamePages.Settings);
    /// EUIManager.Instance.CloseTopPopup();
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.UI + 1)]
    public class EUIManager : EmberSingleton<EUIManager>, IEmberManager, IEmberUpdate
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        private EUIViewEngine _uiManager;
        private EUIPageContext _context => _uiManager.PageContext;

        /// <summary>
        /// Popup 遮罩颜色（全局静态配置，默认半透明黑 α=0.5，对标 Burner GameUIConst.UIBGMaskColor）。
        /// 建议在首次打开 Popup 前设置；修改后对之后创建/复用的遮罩立即生效。
        /// </summary>
        public static Color PopupMaskColor { get; set; } = new Color(0f, 0f, 0f, 0.5f);

        private readonly Queue<ShowRequest> _showQueue = new();
        private bool _isProcessingQueue;

        private readonly Dictionary<EUIPage, EUIPage> _parentPageMap = new(); // SubPage → ParentPage
        private readonly Dictionary<EUIPage, object> _returnValueMap = new();    // Page → ReturnValue
        private readonly HashSet<EUIPage> _closeRequestedPages = new(); // 防止退出过渡期间重复关闭

        // 预加载页面（对标 Burner Preload 机制）
        private readonly Dictionary<string, EUIPage> _preloadedPages = new(); // PrefabPath → Page

        // Background 使用独立的幂等加载通道：同路径共享完成源，不同路径由最后一次请求获得写入权。
        private BackgroundRequest _backgroundRequest;
        private int _backgroundRequestVersion;

        private bool _initialized;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        private struct ShowRequest
        {
            public EUIPageDef EUIPageDef;
            public object Args;
            public Action<EUIPage> OnComplete;
            public EUIPage ParentPage; // SubPage 时非空
        }

        private sealed class BackgroundRequest
        {
            public int Version;
            public string PrefabPath;
            public UniTaskCompletionSource Completion;
        }

        #endregion

        // --------------------------------------------------------

        #region IEmberManager

        /// <summary>全局默认 Loading 页面。设置后所有跨场景 TransitionTo 自动显示。</summary>
        public static EUIPageDef DefaultLoadingPageDef { get; set; }

        void IEmberManager.Init()
        {
            if (_initialized) return;

            _uiManager = EUIViewEngine.Instance;
            _initialized = true;

            // 注册场景加载拦截器：有 Loading 页面时自动使用
            // 流程：Show loading → 5011 事件 → 状态机 LoadScene → 轮询完成 → Proceed → 关 loading
            Ember.Scene.SceneCoordinator.InterceptSceneLoad = (sceneName, fromScene, onLoaded) =>
            {
                if (DefaultLoadingPageDef == null) return false;

                var sceneMgr = Ember.Scene.EmberSceneManager.Instance;
                if (sceneMgr == null || sceneMgr.IsLoading) return false;

                EUIPage loadingPage = null;
                var loadDone = false;
                var closed = false;

                // ── 5011: 渐入完成 → 状态机开始加载场景 ──
                Action onFadeInComplete = null;
                onFadeInComplete = () =>
                {
                    EmberEventBus.Unsubscribe(EUIEvents.LoadingFadeInComplete, onFadeInComplete);
                    sceneMgr.LoadSceneAsync(sceneName, () =>
                    {
                        if (!string.IsNullOrEmpty(fromScene) && sceneMgr.IsSceneLoaded(fromScene))
                            sceneMgr.UnloadSceneAsync(fromScene);
                        loadDone = true;
                    });
                };
                EmberEventBus.Subscribe(EUIEvents.LoadingFadeInComplete, onFadeInComplete);

                // ── 5013: 渐出完成 → 整个过程结束 ──
                Action onFadeOutComplete = null;
                onFadeOutComplete = () =>
                {
                    EmberEventBus.Unsubscribe(EUIEvents.LoadingFadeOutComplete, onFadeOutComplete);
                    closed = true;
                };
                EmberEventBus.Subscribe(EUIEvents.LoadingFadeOutComplete, onFadeOutComplete);

                // 显示 loading 页面
                ShowTopMost(DefaultLoadingPageDef, onComplete: page => loadingPage = page);

                // 轮询：等 loading 页创建、假进度完成、场景加载完成
                WrapAsync();
                async void WrapAsync()
                {
                    while (loadingPage == null)
                        await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

                    var logic = loadingPage.Logic;

                    // 快速转场（真实加载即就绪，跳过假进度）：由顶层状态机置 EmberStateMachine.QuickSceneLoad = true
                    if (logic != null && Ember.Core.EmberStateMachine.QuickSceneLoad)
                        logic.SkipFakeProgress = true;
                    Ember.Core.EmberStateMachine.QuickSceneLoad = false;

                    while (!loadDone || logic == null || !logic.IsTransitionReady)
                        await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

                    // 三者就绪 → 进入目标状态（await PrepareEnterAsync + Proceed），新 UI 在 loading 底下加载
                    if (onLoaded != null)
                        await onLoaded();

                    // 等 2 帧让新 UI 完成 ProcessShowQueue + PlayShow
                    await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
                    await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

                    // 关闭 loading → PlayHide → OnCustomExit 播 5012(float) + 5013
                    ClosePage(loadingPage);

                    while (!closed)
                        await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
                }

                return true;
            };

            EmberEventBus.OnNext(EUIEvents.UIManagerReady);
            EmberDebug.LogInit(TAG, "EUIManager 初始化完成。");
        }

        void IEmberManager.Destroy()
        {
            Ember.Scene.SceneCoordinator.InterceptSceneLoad = null;
            InvalidateBackgroundRequest();
            _showQueue.Clear();
            _parentPageMap.Clear();
            _returnValueMap.Clear();
            _closeRequestedPages.Clear();
            _preloadedPages.Clear();
            _initialized = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法（路由 API）

        /// <summary>
        /// 显示主页面。替换当前 MainPage。
        /// </summary>
        public void ShowMainPage(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示弹窗。叠加在当前 MainPage 之上，自动创建 BG Mask。
        /// </summary>
        public void ShowPopup(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示置顶弹窗。高于所有 Popup（如 Loading、全局提示）。
        /// </summary>
        public void ShowTopMost(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示子页面。嵌入父页面的指定区域，父关子关。
        /// </summary>
        public void ShowSubPage(EUIPageDef pageDef, EUIPage parentPage, object args = null, Action<EUIPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete, parentPage);
        }

        /// <summary>
        /// 仅隐藏页面视图（α=0、不可交互；不销毁、不清逻辑，数据保留）。
        /// 触发 Logic.OnHide。与 <see cref="ShowPageViewOnly"/> 配对，对标 Burner HidePage(renderOnly=true)。
        /// </summary>
        public void HidePageViewOnly(EUIPageDef pageDef)
        {
            var page = _context.FindOpenedPage(pageDef);
            if (page == null)
            {
                EmberDebug.LogWarning(TAG, $"HidePageViewOnly: 未找到已显示的页面 {pageDef?.PrefabPath}");
                return;
            }
            _uiManager.EnqueuePageOperation(() => ((IEUIView)page).HideViewOnly());
        }

        /// <summary>
        /// 恢复视图可见（HidePageViewOnly 的逆操作；触发 Logic.OnShow 刷新显示）。
        /// </summary>
        public void ShowPageViewOnly(EUIPageDef pageDef)
        {
            var page = _context.FindOpenedPage(pageDef);
            if (page == null)
            {
                EmberDebug.LogWarning(TAG, $"ShowPageViewOnly: 未找到视图隐藏的页面 {pageDef?.PrefabPath}");
                return;
            }
            _uiManager.EnqueuePageOperation(() => ((IEUIView)page).RestoreViewOnly());
        }

        /// <summary>
        /// 设置背景页（单槽位）。仅 <see cref="PageType.Background"/> 类型有效。
        /// 不走 ShowQueue，直接加载并打开，sortingOrder 固定为 0。
        /// 不等待加载完成；需要等背景就绪再继续的场景用 <see cref="SetBackgroundAsync"/>。
        /// </summary>
        public void SetBackground(EUIPageDef pageDef)
        {
            SetBackgroundAsync(pageDef).Forget();
        }

        /// <summary>
        /// 设置背景页并等待其加载+显示完成（异步版本）。
        /// 同路径已显示时直接复用，加载中时共享同一完成源；
        /// 不同路径并发请求以最后一次为准，过期回调不会覆盖当前背景。
        /// </summary>
        public async UniTask SetBackgroundAsync(EUIPageDef pageDef)
        {
            if (pageDef == null) return;
            if (pageDef.PageType != PageType.Background)
            {
                EmberDebug.LogWarning(TAG, $"SetBackground 仅接受 Background 类型，收到: {pageDef.PageType}");
                return;
            }

            var prefabPath = pageDef.PrefabPath;
            if (string.IsNullOrEmpty(prefabPath))
            {
                EmberDebug.LogError(TAG, "SetBackground 的 PrefabPath 不能为空。");
                return;
            }

            // 同路径正在加载/显示：所有调用者共享同一个显示完成时点。
            var pendingRequest = _backgroundRequest;
            if (pendingRequest != null
                && string.Equals(pendingRequest.PrefabPath, prefabPath, StringComparison.Ordinal))
            {
                await pendingRequest.Completion.Task;
                return;
            }

            // 当前已是目标背景：取消可能存在的其他路径请求，不再重复 Instantiate。
            var currentBackground = _context.BackgroundPage;
            if (IsSameBackground(currentBackground, prefabPath))
            {
                InvalidateBackgroundRequest();
                return;
            }

            InvalidateBackgroundRequest();
            var request = new BackgroundRequest
            {
                Version = ++_backgroundRequestVersion,
                PrefabPath = prefabPath,
                Completion = new UniTaskCompletionSource(),
            };
            _backgroundRequest = request;

            StartBackgroundRequest(request, pageDef);
            await request.Completion.Task;
        }

        /// <summary>
        /// 关闭并移除当前背景页。
        /// </summary>
        public void ClearBackground()
        {
            InvalidateBackgroundRequest();
            _context.ClearBackground();
        }

        /// <summary>
        /// 预加载页面：加载 Prefab + 准备 Logic（触发 OnPreload），但不执行 Init、不 PlayShow。
        /// 页面处于 Loaded 状态并缓存。后续 <see cref="ShowMainPage"/> 等调用同一 EUIPageDef 时
        /// 补跑 Init（OnInit/OnOpen/OnReset 使用真实打开参数）再 PlayShow，实现零延迟打开。
        /// 对标 Burner GamePage 预加载机制。
        /// </summary>
        /// <param name="pageDef">页面定义</param>
        /// <param name="args">传递给 OnPreload 的参数（Init 的参数在真正打开时另行传入）</param>
        /// <param name="onComplete">预加载完成回调</param>
        public void PreloadPage(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            var prefabPath = pageDef.PrefabPath;

            // 已预加载则直接返回
            if (_preloadedPages.ContainsKey(prefabPath))
            {
                onComplete?.Invoke(_preloadedPages[prefabPath]);
                return;
            }

            EUIObserver.NotifyLoadStarted(pageDef);
            _uiManager.ResourceProvider.LoadPrefabAsync(prefabPath, prefab =>
            {
                if (prefab == null)
                {
                    EmberDebug.LogError(TAG, $"预加载失败: {prefabPath}");
                    onComplete?.Invoke(null);
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;

                var page = new EUIPage(instance);
                OnPageCreated?.Invoke(page);
                // 提前绑定定义：路由注册在 InitPageOnly 之前执行，保持一致
                page.EUIPageDef = pageDef;

                // 路由注册（同 RouteAndOpenPage 逻辑，但不调用 OpenPage）
                switch (pageDef.PageType)
                {
                    case PageType.MainPage:
                        _context.PushMainPage(page);
                        break;
                    case PageType.Popup:
                    case PageType.FullScreenPopup:
                        _context.AddPopup(page);
                        break;
                    case PageType.TopMost:
                        _context.AddTopMost(page);
                        break;
                    case PageType.Overlay:
                        _context.AddOverlay(page);
                        break;
                }

                // 仅 Init，不 PlayShow
                _uiManager.InitPageOnly(page, pageDef, args, () =>
                {
                    _preloadedPages[prefabPath] = page;
                    EmberDebug.LogInit(TAG, $"预加载完成: {prefabPath}");
                    onComplete?.Invoke(page);
                });
            });
        }

        /// <summary>
        /// 关闭指定页面。
        /// </summary>
        public void ClosePage(EUIPage page, object returnValue = null)
        {
            ClosePageCore(page, returnValue, true);
        }

        /// <summary>
        /// 关闭页面核心流程。restoreUnderlying=false 用于主页面整组关闭 Popup，
        /// 避免每个 Popup 退出时又恢复同样正在关闭的下层页面。
        /// </summary>
        private void ClosePageCore(EUIPage page, object returnValue, bool restoreUnderlying)
        {
            if (page == null) return;
            if (!_closeRequestedPages.Add(page)) return;

            if (returnValue != null)
                _returnValueMap[page] = returnValue;

            // 先关子页面
            foreach (var subPage in page.SubPages)
            {
                ClosePage(subPage);
            }

            EUIPage pageToResume = null;
            bool resumeMainPage = false;
            bool hidePopupMask = false;

            // 立即从路由上下文移除，但遮罩清理与下层恢复延后到退出过渡真正完成。
            switch (page.EUIPageDef.PageType)
            {
                case PageType.MainPage:
                    // 先快照并关闭该 MainPage 持有的 Popup。主页面整组关闭时不逐层恢复下方 Popup。
                    var popups = new List<EUIPage>(_context.GetPopups(page));
                    for (int i = popups.Count - 1; i >= 0; i--)
                        ClosePageCore(popups[i], null, false);

                    pageToResume = _context.PopMainPage(page);
                    resumeMainPage = true;
                    break;
                case PageType.Popup:
                case PageType.FullScreenPopup:
                    pageToResume = _context.RemovePopup(page);
                    hidePopupMask = true;
                    break;
                case PageType.TopMost:
                    _context.RemoveTopMost(page);
                    break;
                case PageType.SubPage:
                    {
                        var parent = page.ParentPage;
                        if (parent != null)
                        {
                            parent.UnregisterSubPage(page);
                            _parentPageMap.Remove(page);
                        }
                        break;
                    }
                case PageType.Overlay:
                    _context.RemoveOverlay(page);
                    break;
                case PageType.FreePage:
                    _context.RemoveFreePage(page);
                    break;
                case PageType.Background:
                    _context.ClearBackground();
                    break;
            }

            _uiManager.ClosePageInternal(
                page,
                onComplete: () => _closeRequestedPages.Remove(page),
                onTransitionComplete: () =>
                {
                    if (hidePopupMask)
                        HideBgMaskForPopup(page);

                    if (!restoreUnderlying || pageToResume == null)
                        return;

                    if (resumeMainPage)
                        _context.ResumeMainPageAfterClose(pageToResume);
                    else
                        _context.ResumePageAfterPopupClose(pageToResume);
                });
        }

        /// <summary>
        /// 关闭最顶层 Popup。
        /// </summary>
        public void CloseTopPopup()
        {
            var top = _context.GetTopPopup();
            if (top != null) ClosePage(top);
        }

        /// <summary>
        /// 关闭所有 Popup。
        /// </summary>
        public void CloseAllPopups()
        {
            while (_context.HasPopup())
            {
                CloseTopPopup();
            }
        }

        /// <summary>
        /// 按 EUIPageDef 关闭匹配的页面（MainPage、Popup、TopMost 等均可）。
        /// 遍历 MainPage 栈和 TopMost 列表查找匹配项。
        /// </summary>
        public void ClosePageByDef(EUIPageDef pageDef)
        {
            if (pageDef == null) return;

            // 查 TopMost
            var topMost = _context.FindTopMostByPath(pageDef.PrefabPath);
            if (topMost != null)
            {
                ClosePage(topMost);
                return;
            }

            // 查 MainPage
            var mainPage = _context.FindMainPageByPath(pageDef.PrefabPath);
            if (mainPage != null)
                ClosePage(mainPage);
        }

        /// <summary>
        /// 获取页面的回传值。
        /// </summary>
        public object GetReturnValue(EUIPage page)
        {
            _returnValueMap.TryGetValue(page, out var val);
            _returnValueMap.Remove(page);
            return val;
        }

        /// <summary>
        /// 每帧由 <see cref="EmberUpdateManager"/> 驱动，处理 Show 请求队列 + 页面 Update。
        /// </summary>
        void IEmberUpdate.Update()
        {
            ProcessShowQueue();
            _uiManager.BroadcastPageUpdate();
        }

        /// <summary>
        /// Loading 页面生命周期管理。显示 loading → 等待 IsTransitionReady → 关闭。
        /// 不负责场景加载和状态机切换，仅管理 loading 页面的打开和关闭。
        /// 返回的 UniTask 在 loading 完全关闭（LoadingFadeOutComplete）后完成。
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask RunLoadingPage(EUIPageDef loadingPageDef)
        {
            EUIPage loadingPage = null;
            var closed = false;

            Action onFadeOutComplete = null;
            onFadeOutComplete = () =>
            {
                EmberEventBus.Unsubscribe(EUIEvents.LoadingFadeOutComplete, onFadeOutComplete);
                closed = true;
            };
            EmberEventBus.Subscribe(EUIEvents.LoadingFadeOutComplete, onFadeOutComplete);

            ShowTopMost(loadingPageDef, onComplete: page => loadingPage = page);

            // 等待 loading 页面创建完成
            while (loadingPage == null)
                await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

            // 轮询假进度完成
            var logic = loadingPage.Logic;
            while (logic == null || !logic.IsTransitionReady)
                await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

            // 关闭 loading → PlayHide → OnCustomExit 播 LoadingFadeOutStart(float) + LoadingFadeOutComplete
            ClosePage(loadingPage);

            // 等待渐出完成
            while (!closed)
                await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void EnqueueShow(EUIPageDef pageDef, object args, Action<EUIPage> onComplete, EUIPage parentPage = null)
        {
            // SubPage 必须经 ShowSubPage 带父页面打开（对标 Burner：禁止全局入口打开 SubPage），否则会成为无归属的孤儿页
            if (pageDef != null && pageDef.PageType == PageType.SubPage && parentPage == null)
            {
                EmberDebug.LogWarning(TAG, $"SubPage [{pageDef.PrefabPath}] 必须通过 ShowSubPage(def, parentPage, ...) 打开，本次请求已拒绝。");
                return;
            }

            _showQueue.Enqueue(new ShowRequest
            {
                EUIPageDef = pageDef,
                Args = args,
                OnComplete = onComplete,
                ParentPage = parentPage,
            });
        }

        private void ProcessShowQueue()
        {
            if (_showQueue.Count == 0 || _isProcessingQueue) return;

            _isProcessingQueue = true;
            while (_showQueue.Count > 0)
            {
                var req = _showQueue.Dequeue();
                ProcessShowRequest(req);
            }
            _isProcessingQueue = false;
        }

        private void ProcessShowRequest(ShowRequest req)
        {
            Profiler.BeginSample("EUIManager.ProcessShowRequest");
            var pageDef = req.EUIPageDef;

            // 已显示（含视图隐藏）页面再次 Show：走数据刷新路径（对标 Burner 可见分支 OnReopen），不再开新实例
            var openedPage = _context.FindOpenedPage(pageDef);
            if (openedPage != null)
            {
                EmberDebug.Log(TAG, $"页面已显示，刷新数据: {pageDef.PrefabPath}");
                _uiManager.ReopenPage(openedPage, req.Args, () => req.OnComplete?.Invoke(openedPage));
                Profiler.EndSample();
                return;
            }

            // 优先使用预加载页面（对标 Burner GamePage Preload 机制）
            if (_preloadedPages.TryGetValue(pageDef.PrefabPath, out var preloadedPage))
            {
                _preloadedPages.Remove(pageDef.PrefabPath);
                EmberDebug.Log(TAG, $"复用预加载页面: {pageDef.PrefabPath}");
                RouteAndOpenPage(preloadedPage, pageDef, req, true);
                Profiler.EndSample();
                return;
            }

            // 其次复用延迟销毁中的页面（对标 Burner GamePage 复用逻辑）
            var reusablePage = _uiManager.FindReusablePage(pageDef.PrefabPath);
            if (reusablePage != null)
            {
                RouteAndOpenPage(reusablePage, pageDef, req);
                Profiler.EndSample();
                return;
            }

            EUIObserver.NotifyLoadStarted(pageDef);
            _uiManager.ResourceProvider.LoadPrefabAsync(pageDef.PrefabPath, prefab =>
            {
                if (prefab == null)
                {
                    EmberDebug.LogError(TAG, $"无法加载预制体: {pageDef.PrefabPath}");
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;

                var page = new EUIPage(instance);
                RouteAndOpenPage(page, pageDef, req);
            });
            Profiler.EndSample();
        }

        /// <summary>
        /// 执行路由分发 + 打开页面（新建和复用共用）。
        /// </summary>
        /// <param name="alreadyRouted">预加载页已在 PreloadPage 中注册过路由，跳过二次注册（否则会重复压栈）</param>
        private void RouteAndOpenPage(EUIPage page, EUIPageDef pageDef, ShowRequest req, bool alreadyRouted = false)
        {
            Profiler.BeginSample("EUIManager.RouteAndOpenPage");
            // 扩展点：允许 uiextension 等外部包配置 Logic（CreateLogic 内部防重，重复调用安全）
            OnPageCreated?.Invoke(page);

            if (!alreadyRouted)
            {
                // 提前绑定定义：路由分发的 switch（如 FreePage 的 AddFreePage）需要在 OpenPage 之前读取 EUIPageDef
                page.EUIPageDef = pageDef;

                switch (pageDef.PageType)
                {
                case PageType.MainPage:
                    _context.PushMainPage(page);
                    break;

                case PageType.Popup:
                case PageType.FullScreenPopup:
                    _context.AddPopup(page);
                    ShowBgMaskForPopup(page);
                    break;

                case PageType.TopMost:
                    _context.AddTopMost(page);
                    break;

                case PageType.SubPage:
                    {
                        var parent = req.ParentPage;
                        if (parent != null)
                        {
                            parent.RegisterSubPage(page);
                            _parentPageMap[page] = parent;

                            // 设置 SubPage 排序（对标 Burner SubPageOrderGrowStep=50）
                            // 必须在 OpenPage 之前设置，因为 OpenPage 只设置 overrideSorting=true，
                            // 不覆盖 sortingOrder 的具体值
                            var sortingOrder = parent.GetNextSubPageSortingOrder();
                            var canvas = page.Canvas;
                            if (canvas)
                                canvas.sortingOrder = sortingOrder;
                        }
                        break;
                    }

                case PageType.Overlay:
                    _context.AddOverlay(page);
                    break;

                case PageType.FreePage:
                    _context.AddFreePage(page);
                    break;

                case PageType.Background:
                    _context.SetBackground(page);
                    break;
                }
            }

            _uiManager.OpenPage(page, pageDef, req.Args, () =>
            {
                req.OnComplete?.Invoke(page);
            });
            Profiler.EndSample();
        }

        /// <summary>
        /// 启动当前 Background 请求。优先消费已完成的预加载页和延迟销毁页，
        /// 仅在无可复用实例时加载并实例化 Prefab。
        /// </summary>
        private void StartBackgroundRequest(BackgroundRequest request, EUIPageDef pageDef)
        {
            if (!IsCurrentBackgroundRequest(request)) return;

            if (_preloadedPages.TryGetValue(request.PrefabPath, out var preloadedPage))
            {
                _preloadedPages.Remove(request.PrefabPath);
                if (preloadedPage != null && preloadedPage.GameObject != null)
                {
                    EmberDebug.Log(TAG, $"复用预加载背景页: {request.PrefabPath}");
                    OpenBackgroundPage(request, pageDef, preloadedPage);
                    return;
                }
            }

            var reusablePage = _uiManager.FindReusablePage(request.PrefabPath);
            if (reusablePage != null && reusablePage.GameObject != null)
            {
                OpenBackgroundPage(request, pageDef, reusablePage);
                return;
            }

            try
            {
                _uiManager.ResourceProvider.LoadPrefabAsync(request.PrefabPath, prefab =>
                {
                    // ClearBackground 或后来的不同路径请求已使本请求过期：
                    // 在 Instantiate 前终止，不允许旧回调重新写入 Background 槽位。
                    if (!IsCurrentBackgroundRequest(request)) return;

                    if (prefab == null)
                    {
                        FailBackgroundRequest(request, $"无法加载背景预制体: {request.PrefabPath}");
                        return;
                    }

                    GameObject instance = null;
                    try
                    {
                        instance = UnityEngine.Object.Instantiate(prefab);
                        instance.name = prefab.name;

                        // Instantiate 不会跨帧，但保留二次代次校验，避免自定义扩展在创建链中重入。
                        if (!IsCurrentBackgroundRequest(request))
                        {
                            UnityEngine.Object.Destroy(instance);
                            return;
                        }

                        OpenBackgroundPage(request, pageDef, new EUIPage(instance));
                    }
                    catch (Exception ex)
                    {
                        if (instance != null)
                            UnityEngine.Object.Destroy(instance);
                        FailBackgroundRequest(request, $"创建背景页失败: {request.PrefabPath}\n{ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                FailBackgroundRequest(request, $"加载背景页失败: {request.PrefabPath}\n{ex}");
            }
        }

        /// <summary>将已创建或复用的页面接入 Background 槽位，并在显示过渡真正完成后解锁所有等待者。</summary>
        private void OpenBackgroundPage(BackgroundRequest request, EUIPageDef pageDef, EUIPage page)
        {
            if (!IsCurrentBackgroundRequest(request)) return;

            try
            {
                OnPageCreated?.Invoke(page);
                if (!IsCurrentBackgroundRequest(request)) return;

                // PreloadPage 不会为 Background 注册路由，因此消费预加载页时也必须在此显式设置槽位。
                _context.SetBackground(page);
                _uiManager.OpenPage(page, pageDef, null, () =>
                {
                    if (!IsCurrentBackgroundRequest(request)) return;

                    EmberDebug.Log(TAG, $"背景已设置: {request.PrefabPath}");
                    CompleteBackgroundRequest(request);
                });
            }
            catch (Exception ex)
            {
                FailBackgroundRequest(request, $"打开背景页失败: {request.PrefabPath}\n{ex}");
            }
        }

        private bool IsCurrentBackgroundRequest(BackgroundRequest request)
        {
            return request != null
                && ReferenceEquals(_backgroundRequest, request)
                && request.Version == _backgroundRequestVersion;
        }

        private static bool IsSameBackground(EUIPage page, string prefabPath)
        {
            return page != null
                && page.GameObject != null
                && page.EUIPageDef != null
                && string.Equals(page.EUIPageDef.PrefabPath, prefabPath, StringComparison.Ordinal);
        }

        private void CompleteBackgroundRequest(BackgroundRequest request)
        {
            if (!IsCurrentBackgroundRequest(request)) return;

            _backgroundRequest = null;
            request.Completion.TrySetResult();
        }

        private void FailBackgroundRequest(BackgroundRequest request, string message)
        {
            if (!IsCurrentBackgroundRequest(request)) return;

            EmberDebug.LogError(TAG, message);
            _backgroundRequest = null;
            request.Completion.TrySetResult();
        }

        /// <summary>
        /// 使当前 Background 请求失效并释放等待者。已发出的资源请求可继续回调，
        /// 但回调的代次校验会在 Instantiate 之前拒绝其结果。
        /// </summary>
        private void InvalidateBackgroundRequest()
        {
            var request = _backgroundRequest;
            _backgroundRequest = null;
            _backgroundRequestVersion++;
            request?.Completion.TrySetResult();
        }

        // ── BG Mask 管理 ──

        private readonly Dictionary<EUIPage, GameObject> _activeMasks = new();

        /// <summary>页面创建扩展点。uiextension 包在此 Hook 中配置 Logic 层。</summary>
        public static Action<EUIPage> OnPageCreated;

        private void ShowBgMaskForPopup(EUIPage popup)
        {
            var logic = popup.Logic;

            // 数据层开关（EUIBinding.useMask，注入到 EUIPage.UseMask）或代码层开关
            // （页面 override AutoCreateClickableMask=false）任一为 false → 不创建遮罩
            if (!popup.UseMask || (logic != null && !logic.ShouldCreateClickableMask))
                return;

            var canvas = popup.Canvas;
            var sortingOrder = canvas ? canvas.sortingOrder : (int)popup.EUIPageDef.Layer;

            var mask = _uiManager.ShowBgMask(sortingOrder, () =>
            {
                // 点击遮罩：优先转发给页面 Logic（默认实现按 ClickMaskToClose 开关关闭 Popup，可 override 定制），
                // 无 Logic 时按数据层开关兜底
                if (popup.Logic != null)
                    popup.Logic.NotifyClickMask();
                else if (popup.ClickMaskToClose)
                    ClosePage(popup);
            }, popup.MaskColorOverride ?? PopupMaskColor, popup.GameObject.layer);

            if (mask != null)
                _activeMasks[popup] = mask;
        }

        private void HideBgMaskForPopup(EUIPage popup)
        {
            if (_activeMasks.TryGetValue(popup, out var mask))
            {
                _uiManager.HideBgMask(mask);
                _activeMasks.Remove(popup);
            }
        }

        #endregion
    }
}
