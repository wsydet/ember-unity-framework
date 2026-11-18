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
    public partial class EUIManager : EmberSingleton<EUIManager>, IEmberManager, IEmberUpdate
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        private EUIViewEngine _viewEngine;
        private EUIModalInputScope _modalInput;
        private readonly List<EUIPage> _maskSnapshot = new();
        private EUIPageContext _context => _viewEngine.PageContext;

        /// <summary>
        /// Popup 遮罩颜色（全局静态配置，默认半透明黑 α=0.5，对标 Burner GameUIConst.UIBGMaskColor）。
        /// 建议在首次打开 Popup 前设置；修改后对之后创建/复用的遮罩立即生效。
        /// </summary>
        public static Color PopupMaskColor { get; set; } = new Color(0f, 0f, 0f, 0.5f);
        public static EUIMaskEffect PopupMaskEffect { get; set; } = EUIMaskEffect.Blur;

        private readonly Queue<ShowRequest> _showQueue = new();
        private readonly List<ShowPresentation> _presentations = new();
        private bool _isProcessingQueue;

        private readonly Dictionary<EUIPage, EUIPage> _parentPageMap = new(); // SubPage → ParentPage
        private readonly Dictionary<EUIPage, object> _returnValueMap = new();    // Page → ReturnValue
        private readonly HashSet<EUIPage> _closeRequestedPages = new(); // 防止退出过渡期间重复关闭

        // 预加载页面（对标 Burner Preload 机制）
        private readonly Dictionary<string, PreloadRequest> _preloads = new();

        // Background 使用独立的幂等加载通道：同路径共享完成源，不同路径由最后一次请求获得写入权。
        private BackgroundRequest _backgroundRequest;
        private int _backgroundRequestVersion;

        private bool _initialized;
        private bool _sessionStopped;
        private IDisposable _lifetimeAbort;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        private struct ShowRequest
        {
            public EUIPageDef EUIPageDef;
            public object Args;
            public Action<EUIPage> OnComplete;
            public Func<bool> IsCurrent;
            public EUIPage ParentPage; // SubPage 时非空
        }

        private sealed class ShowPresentation
        {
            public readonly List<ShowRequest> Requests = new();
            public int InitialRequestIndex;
            public EUIPage Page;
            public EUIPage PopupOwner;
        }

        private sealed class PreloadRequest
        {
            public string Path;
            public IEUIResourceProvider Provider;
            public EUIPage Page;
            public bool Ready;
            public readonly List<Action<EUIPage>> Callbacks = new();
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

            _viewEngine = EUIViewEngine.Instance;
            _modalInput = _viewEngine.UIRoot.gameObject.AddComponent<EUIModalInputScope>();
            _modalInput.Initialize(_viewEngine.ActivePages, _activeMasks, SynchronizeModalMasks);
            _viewEngine.PreparePagePresentation = PreparePagePresentationAsync;
            _initialized = true;
            _sessionStopped = false;
            _lifetimeAbort = GameLauncher.CurrentLifetime?.RegisterAbort(StopSessionPresentation);

            // UI owns only the curtain. SceneCoordinator owns load/prepare/commit/unload.
            Ember.Scene.SceneCoordinator.WrapTransitionAsync = RunSceneTransitionWithLoadingAsync;

            EmberEventBus.OnNext(EUIEvents.UIManagerReady);
            EmberDebug.LogInit(TAG, "EUIManager 初始化完成。");
        }

        void IEmberManager.Destroy()
        {
            StopSessionPresentation();
            _lifetimeAbort?.Dispose();
            Ember.Scene.SceneCoordinator.WrapTransitionAsync = null;
            InvalidateBackgroundRequest();
            _showQueue.Clear();
            _parentPageMap.Clear();
            _returnValueMap.Clear();
            _closeRequestedPages.Clear();
            _initialized = false;
            _viewEngine.PreparePagePresentation = null;
            if (_modalInput) UnityEngine.Object.Destroy(_modalInput);
            _modalInput = null;
        }

        private void StopSessionPresentation()
        {
            _sessionStopped = true;
            _presentations.Clear();
            foreach (var request in new List<PreloadRequest>(_preloads.Values))
                DiscardPreload(request, notify: false);
            _curtainSession?.Dispose();
            InvalidateBackgroundRequest();
            _showQueue.Clear();
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法（路由 API）

        /// <summary>
        /// 显示主页面。替换当前 MainPage。
        /// </summary>
        public void ShowMainPage(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null, Func<bool> isCurrent = null)
        {
            EnqueueShow(pageDef, args, onComplete, isCurrent: isCurrent);
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
        /// 显示覆盖层页面。不参与 MainPage/Popup 栈，排序由 EUIPageDef.OverlaySortingOrder 决定。
        /// </summary>
        public void ShowOverlay(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            if (pageDef == null || pageDef.PageType != PageType.Overlay)
            {
                EmberDebug.LogWarning(TAG, "ShowOverlay 仅接受 Overlay 页面定义。");
                onComplete?.Invoke(null);
                return;
            }

            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示独立页面。不参与 MainPage/Popup 栈，排序由 EUIPageDef.FreePageSortingOrder 决定。
        /// </summary>
        public void ShowFreePage(EUIPageDef pageDef, object args = null, Action<EUIPage> onComplete = null)
        {
            if (pageDef == null || pageDef.PageType != PageType.FreePage)
            {
                EmberDebug.LogWarning(TAG, "ShowFreePage 仅接受 FreePage 页面定义。");
                onComplete?.Invoke(null);
                return;
            }

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
            _viewEngine.EnqueuePageOperation(() => ((IEUIView)page).HideViewOnly());
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
            _viewEngine.EnqueuePageOperation(() => ((IEUIView)page).RestoreViewOnly());
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
            if (pageDef == null || !_initialized || _sessionStopped)
            {
                onComplete?.Invoke(null);
                return;
            }

            var prefabPath = pageDef.PrefabPath;
            if (_preloads.TryGetValue(prefabPath, out var existing))
            {
                if (!existing.Ready)
                {
                    if (onComplete != null) existing.Callbacks.Add(onComplete);
                    return;
                }

                if (existing.Page.GameObject && !existing.Page.IsDisposed)
                {
                    onComplete?.Invoke(existing.Page);
                    return;
                }
                DiscardPreload(existing);
            }

            var request = new PreloadRequest { Path = prefabPath, Provider = _viewEngine.ResourceProvider };
            if (onComplete != null) request.Callbacks.Add(onComplete);
            _preloads.Add(prefabPath, request);
            EUIObserver.NotifyLoadStarted(pageDef);
            bool callbackReceived = false;
            try
            {
                request.Provider.LoadPrefabAsync(prefabPath, prefab =>
                {
                    if (callbackReceived) return;
                    callbackReceived = true;
                    if (!IsCurrentPreload(request))
                    {
                        if (prefab) request.Provider.Release(prefabPath);
                        return;
                    }
                    if (!prefab)
                    {
                        EmberDebug.LogError(TAG, $"预加载失败: {prefabPath}");
                        DiscardPreload(request);
                        return;
                    }
                    if (_viewEngine.HasPresentationFor(prefabPath))
                    {
                        request.Provider.Release(prefabPath);
                        DiscardPreload(request);
                        return;
                    }

                    request.Page = new EUIPage(UnityEngine.Object.Instantiate(prefab)) { EUIPageDef = pageDef };
                    request.Page.GameObject.name = prefab.name;
                    try
                    {
                        OnPageCreated?.Invoke(request.Page);
                        if (!IsCurrentPreload(request)) return;
                        _viewEngine.InitPageOnly(request.Page, pageDef, args, () =>
                        {
                            if (!IsCurrentPreload(request)) return;
                            request.Ready = true;
                            EmberDebug.LogInit(TAG, $"预加载完成: {prefabPath}");
                            CompletePreload(request, request.Page);
                        });
                    }
                    catch
                    {
                        if (IsCurrentPreload(request)) DiscardPreload(request);
                        throw;
                    }
                });
            }
            catch
            {
                if (IsCurrentPreload(request)) DiscardPreload(request);
                throw;
            }
        }

        private bool IsCurrentPreload(PreloadRequest request)
            => _preloads.TryGetValue(request.Path, out var current) && current == request;

        private void CompletePreload(PreloadRequest request, EUIPage page)
        {
            var callbacks = request.Callbacks.ToArray();
            request.Callbacks.Clear();
            foreach (var callback in callbacks)
            {
                try { callback(page != null && !page.IsDisposed ? page : null); }
                catch (Exception error) { EmberDebug.LogError(TAG, $"预加载回调异常 '{request.Path}': {error}"); }
            }
        }

        private void DiscardPreload(PreloadRequest request, bool notify = true)
        {
            _preloads.Remove(request.Path);
            if (request.Page != null)
            {
                try { _viewEngine.DiscardPreloadedPage(request.Page); }
                finally { request.Provider.Release(request.Path); }
            }
            if (notify) CompletePreload(request, null);
        }

        private EUIPage TakePreloadedPage(string path, out IEUIResourceProvider provider)
        {
            provider = null;
            if (!_preloads.TryGetValue(path, out var request)) return null;
            if (!request.Ready || request.Page.IsDisposed || !request.Page.GameObject)
            {
                DiscardPreload(request);
                return null;
            }

            _preloads.Remove(path);
            provider = request.Provider;
            return request.Page;
        }

        /// <summary>
        /// 关闭指定页面。
        /// </summary>
        public void ClosePage(EUIPage page, object returnValue = null)
        {
            if (page != null && page == _curtainPage && _curtainSession != null)
            {
                _curtainSession.Cancel();
                return;
            }
            ClosePageCore(page, returnValue, true);
        }

        /// <summary>
        /// 关闭页面核心流程。restoreUnderlying=false 用于主页面整组关闭 Popup，
        /// 避免每个 Popup 退出时又恢复同样正在关闭的下层页面。
        /// </summary>
        private void ClosePageCore(
            EUIPage page,
            object returnValue,
            bool restoreUnderlying,
            Action onPageTransitionComplete = null, Action onPageClosed = null)
        {
            if (page == null) return;
            if (page.PrefabPath != null && _preloads.TryGetValue(page.PrefabPath, out var preload) &&
                preload.Page == page)
            {
                DiscardPreload(preload);
                onPageTransitionComplete?.Invoke();
                onPageClosed?.Invoke();
                return;
            }
            if (page.IsDisposed || !_closeRequestedPages.Add(page)) return;

            if (returnValue != null)
                _returnValueMap[page] = returnValue;

            // 先关子页面
            foreach (var subPage in new List<EUIPage>(page.SubPages))
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

            _viewEngine.ClosePageInternal(
                page,
                onComplete: () => { _closeRequestedPages.Remove(page); onPageClosed?.Invoke(); },
                onTransitionComplete: () =>
                {
                    if (resumeMainPage)
                        _context.FinishMainPageClose(page);
                    if (hidePopupMask)
                    {
                        _context.FinishPopupClose(page);
                        HideBgMaskForPopup(page);
                    }

                    if (restoreUnderlying && pageToResume != null)
                    {
                        if (resumeMainPage)
                            _context.ResumeMainPageAfterClose(pageToResume);
                        else
                            _context.ResumePageAfterPopupClose(pageToResume);
                    }

                    onPageTransitionComplete?.Invoke();
                });

            CancelPresentations(page);
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
            var popups = new List<EUIPage>(_context.GetPopups(_context.CurrentMainPage));
            for (int i = popups.Count - 1; i >= 0; i--)
                ClosePageCore(popups[i], null, false);
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
            CheckDestroyedPresentations();
            ProcessShowQueue();
            _viewEngine.BroadcastPageUpdate();
        }

        /// <summary>
        /// Loading 页面生命周期管理。显示 loading → 等待 IsTransitionReady → 关闭。
        /// 不负责场景加载和状态机切换，仅管理 loading 页面的打开和关闭。
        /// 返回的 UniTask 在 loading 完全关闭（LoadingFadeOutComplete）后完成。
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask RunLoadingPage(EUIPageDef loadingPageDef)
        {
            var curtain = await CoverAsync(loadingPageDef);
            try
            {
                var logic = _curtainPage.Logic;
                if (logic == null) throw new InvalidOperationException("Loading page has no readiness presenter.");
                await UniTask.WaitUntil(() => curtain.IsReleased || logic.IsTransitionReady);
                await curtain.RevealAsync();
            }
            finally { curtain.Dispose(); }
        }

        /// <summary>
        /// Waits for the Loading enter animation, runs an asynchronous operation under the curtain,
        /// then closes it. Scene transitions inside the operation share this curtain. The operation
        /// must await its destination content, not merely dispatch a state transition. Args configure
        /// the page's display mode in OnOpen. Concurrent Loading owners are rejected.
        /// </summary>
        [HasGC]
        public UniTask RunWithLoadingAsync(EUIPageDef loadingPageDef, object args,
            Func<UniTask> operation, System.Threading.CancellationToken cancellationToken = default)
            => RunWithLoadingCoreAsync(loadingPageDef, args, operation, cancellationToken, null);

        private async UniTask RunWithLoadingCoreAsync(EUIPageDef loadingPageDef, object args,
            Func<UniTask> operation, System.Threading.CancellationToken cancellationToken, Func<bool> commitStarted,
            EUICurtainOptions options = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            var curtain = await CoverAsync(loadingPageDef, args, options, cancellationToken);
            try
            {
                await operation();
                if (curtain.IsReleased) throw new InvalidOperationException("Loading owner expired.");
                var token = commitStarted?.Invoke() == true ? default : cancellationToken;
                if (_curtainPage?.Logic != null)
                {
                    var logic = _curtainPage.Logic;
                    logic.LoadingWorkCompleted = true;
                    await UniTask.WaitUntil(() => curtain.IsReleased || logic.IsTransitionReady, cancellationToken: token);
                }
                await UniTask.NextFrame(cancellationToken: token);
                await curtain.RevealAsync(token);
            }
            finally { curtain.Dispose(); }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void EnqueueShow(EUIPageDef pageDef, object args, Action<EUIPage> onComplete, EUIPage parentPage = null, Func<bool> isCurrent = null)
        {
            if (_sessionStopped) throw new OperationCanceledException("UI 会话已停止。");
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
                IsCurrent = isCurrent,
                ParentPage = parentPage,
            });
        }

        private void ProcessShowQueue()
        {
            if (_showQueue.Count == 0 || _isProcessingQueue) return;

            _isProcessingQueue = true;
            try
            {
                while (_showQueue.Count > 0)
                {
                    var req = _showQueue.Dequeue();
                    try { ProcessShowRequest(req); }
                    catch (Exception error)
                    {
                        if (req.Args is EUICurtainOpenArgs curtain) curtain.Session.Fail(error);
                        else throw;
                    }
                }
            }
            finally { _isProcessingQueue = false; }
        }

        private void ProcessShowRequest(ShowRequest req)
        {
            if (!IsShowCurrent(req)) return;
            var pageDef = req.EUIPageDef;
            if ((req.Args is not EUICurtainOpenArgs && pageDef.PrefabPath == _curtainPrefabPath) ||
                (req.ParentPage != null && !CanPresent(req.ParentPage)))
            {
                NotifyShowComplete(req, null);
                return;
            }

            var openedPage = FindVisiblePresentation(req);
            if (openedPage != null)
            {
                _viewEngine.ReopenPage(openedPage, req.Args, () => req.OnComplete?.Invoke(openedPage));
                return;
            }

            var pending = FindPresentation(req);
            if (pending != null)
            {
                pending.Requests.Add(req);
                return;
            }

            var presentation = new ShowPresentation();
            presentation.Requests.Add(req);
            _presentations.Add(presentation);
            Profiler.BeginSample("EUIManager.ProcessShowRequest");
            try
            {
                var page = TakePreloadedPage(pageDef.PrefabPath, out var provider);
                if (page != null)
                {
                    EmberDebug.Log(TAG, $"复用预加载页面: {pageDef.PrefabPath}");
                    RouteAndOpenPage(presentation, page, req, provider);
                    return;
                }

                page = _viewEngine.FindReusablePage(pageDef.PrefabPath);
                if (page != null)
                {
                    RouteAndOpenPage(presentation, page, req);
                    return;
                }

                LoadPresentation(presentation, pageDef);
            }
            catch
            {
                CompletePresentation(presentation, null);
                throw;
            }
            finally { Profiler.EndSample(); }
        }

        private static bool CanPresent(EUIPage page) => !page.IsDisposed && !page.CloseRequested && page.GameObject;

        private bool IsShowCurrent(ShowRequest request)
            => _initialized && !_sessionStopped && (request.IsCurrent == null || request.IsCurrent());

        private EUIPage FindVisiblePresentation(ShowRequest request)
        {
            if (request.EUIPageDef.PageType == PageType.SubPage || request.Args is EUICurtainOpenArgs) return null;
            var page = _context.FindOpenedPage(request.EUIPageDef);
            return page != null && CanPresent(page) ? page : null;
        }

        private ShowPresentation FindPresentation(ShowRequest request, ShowPresentation except = null)
        {
            var definition = request.EUIPageDef;
            var curtain = (request.Args as EUICurtainOpenArgs)?.Session;
            foreach (var candidate in _presentations)
            {
                if (candidate == except) continue;
                var first = candidate.Requests[0];
                if (first.EUIPageDef.PageType != definition.PageType || first.EUIPageDef.PrefabPath != definition.PrefabPath ||
                    first.ParentPage != request.ParentPage || (first.Args as EUICurtainOpenArgs)?.Session != curtain)
                    continue;

                // A Popup without a view will join the current Main when its resource arrives.
                if (candidate.Page != null && (definition.PageType == PageType.Popup || definition.PageType == PageType.FullScreenPopup) &&
                    candidate.PopupOwner != _context.CurrentMainPage)
                    continue;
                return candidate;
            }

            return null;
        }

        private void LoadPresentation(ShowPresentation presentation, EUIPageDef definition)
        {
            EUIObserver.NotifyLoadStarted(definition);
            var path = definition.PrefabPath;
            var provider = _viewEngine.ResourceProvider;
            bool callbackReceived = false;
            provider.LoadPrefabAsync(path, prefab =>
            {
                if (callbackReceived) return;
                callbackReceived = true;
                try
                {
                    int first = _presentations.Contains(presentation)
                        ? presentation.Requests.FindIndex(IsShowCurrent) : -1;
                    if (first < 0)
                    {
                        if (prefab) provider.Release(path);
                        CompletePresentation(presentation, null);
                        return;
                    }

                    presentation.InitialRequestIndex = first;
                    var request = presentation.Requests[first];
                    var pending = FindPresentation(request, presentation);
                    var existing = FindVisiblePresentation(request);
                    if (existing != null || pending != null)
                    {
                        if (prefab) provider.Release(path);
                        if (existing != null) CompletePresentation(presentation, existing, openedHere: false);
                        else if (_presentations.Remove(presentation)) pending.Requests.AddRange(presentation.Requests);
                        return;
                    }

                    if (!prefab)
                    {
                        EmberDebug.LogError(TAG, $"无法加载预制体: {path}");
                        CompletePresentation(presentation, null);
                        return;
                    }

                    var instance = UnityEngine.Object.Instantiate(prefab);
                    instance.name = prefab.name;
                    RouteAndOpenPage(presentation, new EUIPage(instance), request, provider);
                }
                catch (Exception error)
                {
                    CompletePresentation(presentation, null);
                    if (presentation.Requests[0].Args is EUICurtainOpenArgs curtain) curtain.Session.Fail(error);
                    else throw;
                }
            });
        }

        private void CompletePresentation(ShowPresentation presentation, EUIPage page, bool openedHere = true)
        {
            if (!_presentations.Remove(presentation)) return;
            for (int i = presentation.InitialRequestIndex; i < presentation.Requests.Count; i++)
            {
                var request = presentation.Requests[i];
                if (!IsShowCurrent(request)) continue;

                EUIPage result = null;
                try
                {
                    if (page != null && CanPresent(page) &&
                        ((openedHere && i == presentation.InitialRequestIndex) ||
                         _viewEngine.RefreshOpenedPage(page, request.Args)))
                        result = page;
                }
                catch (Exception error)
                {
                    EmberDebug.LogError(TAG, $"页面刷新异常 '{request.EUIPageDef}': {error}");
                }
                NotifyShowComplete(request, result);
            }
        }

        private static void NotifyShowComplete(ShowRequest request, EUIPage page)
        {
            try { request.OnComplete?.Invoke(page); }
            catch (Exception error)
            {
                EmberDebug.LogError(TAG, $"页面完成回调异常 '{request.EUIPageDef}': {error}");
            }
        }

        private void CancelPresentations(EUIPage page)
        {
            foreach (var presentation in _presentations.FindAll(p => p.Page == page || p.Requests[0].ParentPage == page))
                CompletePresentation(presentation, null);
        }

        private void CheckDestroyedPresentations()
        {
            if (_presentations.Count == 0) return;
            for (int i = _presentations.Count - 1; i >= 0; i--)
            {
                var presentation = _presentations[i];
                var page = presentation.Page;
                if (page == null || CanPresent(page)) continue;
                // Complete may synchronously invoke a callback that closes a different page.
                AbortPresentation(presentation);
                i = _presentations.Count;
            }
        }

        private void AbortPresentation(ShowPresentation presentation)
        {
            if (!_presentations.Contains(presentation)) return;
            var page = presentation.Page;
            if (presentation.Requests[0].Args is EUICurtainOpenArgs curtain)
                curtain.Session.Cancel();
            else if (!page.IsDisposed && !page.CloseRequested)
                ClosePage(page);
            CompletePresentation(presentation, null);
        }

        /// <summary>
        /// 执行路由分发 + 打开页面（新建和复用共用）。
        /// </summary>
        private void RouteAndOpenPage(ShowPresentation presentation, EUIPage page, ShowRequest req,
            IEUIResourceProvider acquiredProvider = null)
        {
            var pageDef = req.EUIPageDef;
            presentation.Page = page;
            Profiler.BeginSample("EUIManager.RouteAndOpenPage");
            try
            {
                var curtain = (req.Args as EUICurtainOpenArgs)?.Session;
                page.EUIPageDef = pageDef;
                curtain?.PageAttached?.Invoke(page);
                // 扩展点：允许 uiextension 等外部包配置 Logic（CreateLogic 内部防重，重复调用安全）
                OnPageCreated?.Invoke(page);
                // 扩展回调可能已取消遮幕或关闭本次展示。
                if (curtain?.IsReleased == true || page.CloseRequested || page.IsDisposed)
                {
                    CompletePresentation(presentation, null);
                    return;
                }

                // Show 的加载也可能早于稍后发起的同路径预加载。
                if (_preloads.TryGetValue(pageDef.PrefabPath, out var unusedPreload))
                    DiscardPreload(unusedPreload);
                if (curtain?.IsReleased == true || page.CloseRequested || page.IsDisposed)
                {
                    CompletePresentation(presentation, null);
                    return;
                }

                switch (pageDef.PageType)
                {
                    case PageType.MainPage:
                        _context.PushMainPage(page);
                        break;

                    case PageType.Popup:
                    case PageType.FullScreenPopup:
                        presentation.PopupOwner = _context.CurrentMainPage;
                        _context.AddPopup(page);
                        break;

                    case PageType.TopMost:
                        if (!_context.TryAddTopMost(page))
                        {
                            RejectPageOrder(presentation, page, req, acquiredProvider, curtain);
                            return;
                        }
                        break;

                    case PageType.SubPage:
                        {
                            var parent = req.ParentPage;
                            if (parent != null)
                            {
                                if (parent.CloseRequested || parent.IsDisposed || !parent.GameObject ||
                                    !_context.TrySetSubPageOrder(page, parent))
                                {
                                    RejectPageOrder(presentation, page, req, acquiredProvider, curtain);
                                    return;
                                }
                                parent.RegisterSubPage(page);
                                _parentPageMap[page] = parent;
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

                _viewEngine.OpenPage(page, pageDef, req.Args,
                    () => CompletePresentation(presentation, page),
                    () => AbortPresentation(presentation));
            }
            finally { Profiler.EndSample(); }
        }

        private void RejectPageOrder(ShowPresentation presentation, EUIPage page, ShowRequest request,
            IEUIResourceProvider provider, EUICurtainSession curtain)
        {
            string message = $"页面排序区间已满或父页面已关闭: {request.EUIPageDef.PrefabPath}";
            if (curtain != null)
                curtain.Fail(new InvalidOperationException(message));
            else
                _viewEngine.DiscardPreloadedPage(page);
            provider?.Release(request.EUIPageDef.PrefabPath);
            EmberDebug.LogWarning(TAG, message);
            CompletePresentation(presentation, null);
        }

        /// <summary>
        /// 启动当前 Background 请求。优先消费已完成的预加载页和延迟销毁页，
        /// 仅在无可复用实例时加载并实例化 Prefab。
        /// </summary>
        private void StartBackgroundRequest(BackgroundRequest request, EUIPageDef pageDef)
        {
            if (!IsCurrentBackgroundRequest(request)) return;

            var preloadedPage = TakePreloadedPage(request.PrefabPath, out _);
            if (preloadedPage != null)
            {
                EmberDebug.Log(TAG, $"复用预加载背景页: {request.PrefabPath}");
                OpenBackgroundPage(request, pageDef, preloadedPage);
                return;
            }

            var reusablePage = _viewEngine.FindReusablePage(request.PrefabPath);
            if (reusablePage != null && reusablePage.GameObject != null)
            {
                OpenBackgroundPage(request, pageDef, reusablePage);
                return;
            }

            try
            {
                _viewEngine.ResourceProvider.LoadPrefabAsync(request.PrefabPath, prefab =>
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

                if (_preloads.TryGetValue(request.PrefabPath, out var unusedPreload))
                    DiscardPreload(unusedPreload);
                if (!IsCurrentBackgroundRequest(request)) return;
                _context.SetBackground(page);
                _viewEngine.OpenPage(page, pageDef, null, () =>
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

            var mask = _viewEngine.ShowBgMask(sortingOrder, () =>
            {
                // 点击遮罩：优先转发给页面 Logic（默认实现按 ClickMaskToClose 开关关闭 Popup，可 override 定制），
                // 无 Logic 时按数据层开关兜底
                if (popup.Logic != null)
                    popup.Logic.NotifyClickMask();
                else if (popup.ClickMaskToClose)
                    ClosePage(popup);
            }, popup.MaskColorOverride ?? PopupMaskColor, popup.GameObject.layer, popup.Canvas,
                EUIBackdrop.ResolveEffect(popup.MaskEffectOverride, PopupMaskEffect),
                popup.BackdropSettingsOverride ? popup.BackdropSettingsOverride : EUIBackdrop.DefaultSettings);

            if (mask != null)
                _activeMasks[popup] = mask;
        }

        private void HideBgMaskForPopup(EUIPage popup, bool refreshInput = true)
        {
            if (_activeMasks.TryGetValue(popup, out var mask))
            {
                _viewEngine.HideBgMask(mask);
                _activeMasks.Remove(popup);
            }
            if (refreshInput && _modalInput) _modalInput.Refresh();
        }

        private void SynchronizeModalMasks()
        {
            _context.SynchronizePopupInteraction();
            _maskSnapshot.Clear();
            foreach (var pair in _activeMasks) _maskSnapshot.Add(pair.Key);
            foreach (var page in _maskSnapshot)
            {
                if (!page.GameObject) { ClosePage(page); HideBgMaskForPopup(page, false); continue; }
                if (page.IsDisposed) { HideBgMaskForPopup(page, false); continue; }
                var mask = _activeMasks[page];
                if (!mask) { _activeMasks.Remove(page); continue; }
                bool visible = page.GameObject.activeInHierarchy && page.State != PageState.ViewHidden;
                if (mask.activeSelf == visible) continue;
                if (!visible) mask.SetActive(false);
                else { HideBgMaskForPopup(page, false); ShowBgMaskForPopup(page); }
            }
        }

        private async UniTask<bool> PreparePagePresentationAsync(EUIPage page)
        {
            EUIBackdrop.MarkOrderDirty();
            var type = page.EUIPageDef?.PageType;
            if (type != PageType.Popup && type != PageType.FullScreenPopup) return true;
            if (page.IsDisposed || _closeRequestedPages.Contains(page)) return false;
            HideBgMaskForPopup(page, false);
            ShowBgMaskForPopup(page);
            if (_modalInput) _modalInput.Refresh();
            EUIBackdropMaskSession session = null;
            if (_activeMasks.TryGetValue(page, out var mask) && mask) session = mask.GetComponent<EUIBackdropMaskSession>();
            var lease = session ? session.Lease : null;
            if (lease != null)
            {
                // Keep the lower view rendering until its first real capture, before fullscreen culling.
                for (int i = 0; i < 2 && !lease.IsReady && !lease.IsDisposed; i++) await UniTask.NextFrame();
                if (page.IsDisposed || _closeRequestedPages.Contains(page) || lease.IsDisposed) return false;
            }
            if (type == PageType.FullScreenPopup)
            {
                lease?.Freeze();
                _context.CommitFullScreenPopup(page);
            }
            return !page.IsDisposed && !_closeRequestedPages.Contains(page);
        }

        /// <summary>显式刷新已打开 Popup 的配置；全局配置只影响后续展示。</summary>
        public void RefreshBackdrop(EUIPage page)
        {
            if (page == null || page.IsDisposed) return;
            HideBgMaskForPopup(page, false); ShowBgMaskForPopup(page);
            if (_modalInput) _modalInput.Refresh();
            EUIBackdrop.MarkOrderDirty();
        }

        #endregion
    }
}
