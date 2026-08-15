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

        private readonly Queue<ShowRequest> _showQueue = new();
        private bool _isProcessingQueue;

        private readonly Dictionary<EUIPage, EUIPage> _parentPageMap = new(); // SubPage → ParentPage
        private readonly Dictionary<EUIPage, object> _returnValueMap = new();    // Page → ReturnValue

        // 预加载页面（对标 Burner Preload 机制）
        private readonly Dictionary<string, EUIPage> _preloadedPages = new(); // PrefabPath → Page

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
                    while (!loadDone || logic == null || !logic.IsTransitionReady)
                        await Cysharp.Threading.Tasks.UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);

                    // 三者就绪 → 状态机推进（新场景 Enter，新 UI 在 loading 底下加载）
                    onLoaded?.Invoke();

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
            _showQueue.Clear();
            _parentPageMap.Clear();
            _returnValueMap.Clear();
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
        /// 背景页现由开屏动画（EUIMainAnimationStarter）在 MainSceneReady 后加载，
        /// 开屏动画会 await 本方法，确保背景兜底 UI 就绪后才结束动画、进入 MainUI。
        /// </summary>
        public async UniTask SetBackgroundAsync(EUIPageDef pageDef)
        {
            if (pageDef == null) return;
            if (pageDef.PageType != PageType.Background)
            {
                EmberDebug.LogWarning(TAG, $"SetBackground 仅接受 Background 类型，收到: {pageDef.PageType}");
                return;
            }

            var tcs = new UniTaskCompletionSource();
            _uiManager.ResourceProvider.LoadPrefabAsync(pageDef.PrefabPath, prefab =>
            {
                if (prefab == null)
                {
                    EmberDebug.LogError(TAG, $"无法加载背景预制体: {pageDef.PrefabPath}");
                    tcs.TrySetResult();
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;

                var page = new EUIPage(instance);
                OnPageCreated?.Invoke(page);
                _context.SetBackground(page);
                _uiManager.OpenPage(page, pageDef, null, () =>
                {
                    EmberDebug.Log(TAG, $"背景已设置: {pageDef.PrefabPath}");
                    tcs.TrySetResult();
                });
            });

            await tcs.Task;
        }

        /// <summary>
        /// 关闭并移除当前背景页。
        /// </summary>
        public void ClearBackground()
        {
            _context.ClearBackground();
        }

        /// <summary>
        /// 预加载页面：加载 Prefab + Init，但不 PlayShow。
        /// 页面处于 Loaded 状态并缓存。后续 <see cref="ShowMainPage"/> 等调用同一 EUIPageDef 时
        /// 直接跳过加载进入 PlayShow，实现零延迟打开。
        /// 对标 Burner GamePage 预加载机制。
        /// </summary>
        /// <param name="pageDef">页面定义</param>
        /// <param name="args">传递给 Init 的参数</param>
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

                // 路由注册（同 RouteAndOpenPage 逻辑，但不调用 OpenPage）
                switch (pageDef.PageType)
                {
                    case PageType.MainPage:
                        _context.PushMainPage(page);
                        break;
                    case PageType.Popup:
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
            if (page == null) return;

            if (returnValue != null)
                _returnValueMap[page] = returnValue;

            // 先关子页面
            foreach (var subPage in page.SubPages)
            {
                ClosePage(subPage);
            }

            // 从上下文中移除
            switch (page.EUIPageDef.PageType)
            {
                case PageType.MainPage:
                    _context.PopMainPage(page);
                    break;
                case PageType.Popup:
                    _context.RemovePopup(page);
                    HideBgMaskForPopup(page);
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

            _uiManager.ClosePage(page);
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

            // 优先使用预加载页面（对标 Burner GamePage Preload 机制）
            if (_preloadedPages.TryGetValue(pageDef.PrefabPath, out var preloadedPage))
            {
                _preloadedPages.Remove(pageDef.PrefabPath);
                EmberDebug.Log(TAG, $"复用预加载页面: {pageDef.PrefabPath}");
                RouteAndOpenPage(preloadedPage, pageDef, req);
                return;
            }

            // 其次复用延迟销毁中的页面（对标 Burner GamePage 复用逻辑）
            var reusablePage = _uiManager.FindReusablePage(pageDef.PrefabPath);
            if (reusablePage != null)
            {
                RouteAndOpenPage(reusablePage, pageDef, req);
                return;
            }

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
        private void RouteAndOpenPage(EUIPage page, EUIPageDef pageDef, ShowRequest req)
        {
            Profiler.BeginSample("EUIManager.RouteAndOpenPage");
            // 扩展点：允许 uiextension 等外部包配置 Logic
            OnPageCreated?.Invoke(page);

            switch (pageDef.PageType)
            {
                case PageType.MainPage:
                    _context.PushMainPage(page);
                    break;

                case PageType.Popup:
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

            _uiManager.OpenPage(page, pageDef, req.Args, () =>
            {
                req.OnComplete?.Invoke(page);
            });
            Profiler.EndSample();
        }

        // ── BG Mask 管理 ──

        private readonly Dictionary<EUIPage, GameObject> _activeMasks = new();

        /// <summary>页面创建扩展点。uiextension 包在此 Hook 中配置 Logic 层。</summary>
        public static Action<EUIPage> OnPageCreated;

        private void ShowBgMaskForPopup(EUIPage popup)
        {
            var canvas = popup.Canvas;
            var sortingOrder = canvas ? canvas.sortingOrder : (int)popup.EUIPageDef.Layer;

            var mask = _uiManager.ShowBgMask(sortingOrder - 1, () =>
            {
                // 点击遮罩关闭 Popup
                ClosePage(popup);
            });

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
