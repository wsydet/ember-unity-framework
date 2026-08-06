// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Core;
using Ember.Basic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 管理器（框架层引擎）。
    ///
    /// 管理 Canvas 层级、页面生命周期、安全遍历、Update 分发。
    /// 不关心"打开什么/何时打开"——那是 <see cref="EmberUIPageRouter"/> 的职责。
    ///
    /// <para>初始化时自动隐藏 UIRoot 下所有子节点（编辑时放的预览节点），
    /// 后续所有 UI 页面由 Instantiate 动态创建。</para>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.UI)]
    public class EmberUIManager : EmberMonoSingleton<EmberUIManager>, IEmberManager
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        // Canvas 层管理
        private readonly Dictionary<int, Canvas> _layerCanvases = new();
        private Transform _uiRoot;
        private Camera _uiCamera;

        // 页面追踪
        private readonly List<EmberPage> _activePages = new();
        private readonly Queue<Action> _pendingOperations = new();
        private readonly Queue<Action> _nextFrameCallbacks = new();
        private bool _isProcessingOperations;
        private bool _initialized;

        // 上下文
        private EmberPageContext _pageContext;
        private EmberBgMaskPool _bgMaskPool;

        // 资源 & 过渡
        private IUIResourceProvider _resourceProvider;
        private IUITransitionHandler _transitionHandler;

        // Frame Time Budget
        private const int TimeBudgetMs = 10;
        private readonly System.Diagnostics.Stopwatch _loadTimer = new System.Diagnostics.Stopwatch();

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            // EmberMonoSingleton 自动注册，此处仅做日志
        }

        private void Update()
        {
            ProcessPendingOperations();
            ProcessNextFrameCallbacks();
        }

        private void LateUpdate()
        {
            // 如果活跃页面需要 LateUpdate 驱动
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        // ── 属性 ──

        /// <summary>页面上下文（MainPage + Popup 栈关系）</summary>
        public EmberPageContext PageContext => _pageContext;

        /// <summary>资源加载提供者</summary>
        public IUIResourceProvider ResourceProvider
        {
            get => _resourceProvider;
            set => _resourceProvider = value;
        }

        /// <summary>过渡动画处理器</summary>
        public IUITransitionHandler TransitionHandler
        {
            get => _transitionHandler;
            set => _transitionHandler = value;
        }

        /// <summary>UI 根节点</summary>
        public Transform UIRoot => _uiRoot;

        /// <summary>UI 相机</summary>
        public Camera UICamera => _uiCamera;

        // ── IEmberManager ──

        void IEmberManager.Init()
        {
            if (_initialized) return;

            var launcher = GameLauncher.Instance;
            _uiRoot = launcher.UIRoot?.transform;
            _uiCamera = launcher.UICamera;

            if (_uiRoot == null)
            {
                EmberDebug.LogError(TAG, "UIRoot 为空，EmberUIManager 无法初始化。");
                return;
            }

            // 初始化时隐藏 UIRoot 下所有子节点（编辑时放的预览节点）
            foreach (Transform child in _uiRoot)
            {
                child.gameObject.SetActive(false);
            }

            // 默认实现
            _resourceProvider ??= new DefaultUIResourceProvider();
            _transitionHandler ??= new DefaultUITransitionHandler();

            _pageContext = new EmberPageContext(this);
            _bgMaskPool = new EmberBgMaskPool(_uiRoot);

            _initialized = true;
            EmberEventBus.OnNext(EmberUIEvents.UIManagerReady);
            EmberDebug.LogInit(TAG, "EmberUIManager 初始化完成。");
        }

        void IEmberManager.Destroy()
        {
            Shutdown();
        }

        // ── 页面生命周期（由 PageRouter 调用） ──

        /// <summary>
        /// 打开一个页面。完整流程：Init → PlayShow → [Opened]。
        /// </summary>
        /// <param name="page">已实例化的页面</param>
        /// <param name="pageDef">页面定义</param>
        /// <param name="args">传递给 Init 的参数</param>
        /// <param name="onComplete">完成回调</param>
        public void OpenPage(EmberPage page, PageDef pageDef, object args, Action onComplete = null)
        {
            EnsureLayerCanvas(pageDef.Layer);

            page.PageDef = pageDef;
            page.transform.SetParent(_uiRoot, false);

            var canvas = page.GetComponent<Canvas>();
            if (!canvas)
                canvas = page.gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.overrideSorting = true;

            // 添加到追踪列表
            if (!_activePages.Contains(page))
                _activePages.Add(page);

            // Phase 1: Init
            EnqueuePageOperation(() =>
            {
                ((IUIView)page).Init(args);

                // Phase 2: PlayShow
                EnqueuePageOperation(() =>
                {
                    ((IUIView)page).PlayShow();

                    // 下一帧完成 Show
                    _nextFrameCallbacks.Enqueue(() =>
                    {
                        EmberUIObserver.NotifyOpened(pageDef, args);
                        EmberDebug.LogEvent(TAG, $"页面已打开: {pageDef}");
                        onComplete?.Invoke();
                    });
                });
            });
        }

        /// <summary>
        /// 关闭一个页面。流程：PlayHide → Cleanup → Destroy。
        /// </summary>
        public void ClosePage(EmberPage page, Action onComplete = null)
        {
            ClosePageInternal(page, onComplete);
        }

        internal void ClosePageInternal(EmberPage page, Action onComplete = null)
        {
            if (page == null) return;

            var pageDef = page.PageDef;

            EnqueuePageOperation(() =>
            {
                ((IUIView)page).PlayHide();

                // 下一帧完成 Hide
                _nextFrameCallbacks.Enqueue(() =>
                {
                    if (page != null)
                    {
                        ((IUIView)page).Cleanup();
                        _activePages.Remove(page);
                        EmberUIObserver.NotifyClosed(pageDef, null);
                        EmberDebug.LogCleanup(TAG, $"页面已关闭: {pageDef}");
                        if (page.gameObject != null)
                            Destroy(page.gameObject);
                        onComplete?.Invoke();
                    }
                });
            });
        }

        /// <summary>
        /// 暂停一个页面。
        /// </summary>
        public void PausePage(EmberPage page)
        {
            EnqueuePageOperation(() =>
            {
                ((IUIView)page).OnPause();
                EmberUIObserver.NotifyPaused(page.PageDef);
            });
        }

        /// <summary>
        /// 恢复一个页面。
        /// </summary>
        public void ResumePage(EmberPage page)
        {
            EnqueuePageOperation(() =>
            {
                ((IUIView)page).OnResume();
                EmberUIObserver.NotifyResumed(page.PageDef);
            });
        }

        /// <summary>
        /// 重新打开已关闭的页面（OnReopen → PlayShow）。
        /// </summary>
        public void ReopenPage(EmberPage page, object args, Action onComplete = null)
        {
            EnqueuePageOperation(() =>
            {
                ((IUIView)page).OnReopen(args);
                EmberUIObserver.NotifyReopened(page.PageDef, args);

                ((IUIView)page).PlayShow();
                _nextFrameCallbacks.Enqueue(() =>
                {
                    EmberUIObserver.NotifyOpened(page.PageDef, args);
                    onComplete?.Invoke();
                });
            });
        }

        // ── Canvas 层管理 ──

        /// <summary>
        /// 确保指定层级的 Canvas 已创建。
        /// </summary>
        public Canvas EnsureLayerCanvas(int layer)
        {
            if (_layerCanvases.TryGetValue(layer, out var canvas))
                return canvas;

            var go = new GameObject($"UI_Layer_{layer}");
            go.transform.SetParent(_uiRoot);

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.sortingOrder = layer;

            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            _layerCanvases[layer] = canvas;
            return canvas;
        }

        // ── BG Mask ──

        /// <summary>显示背景遮罩</summary>
        public GameObject ShowBgMask(int sortingOrder, Action onClick)
        {
            return _bgMaskPool.Get(sortingOrder, onClick);
        }

        /// <summary>隐藏背景遮罩</summary>
        public void HideBgMask(GameObject mask)
        {
            _bgMaskPool.Return(mask);
        }

        // ── 安全操作队列 ──

        /// <summary>
        /// 将页面操作加入队列，避免在遍历 _activePages 时修改集合。
        /// </summary>
        public void EnqueuePageOperation(Action op)
        {
            _pendingOperations.Enqueue(op);
        }

        // ── 查询 ──

        /// <summary>活跃页面列表（运行时）</summary>
        public IReadOnlyList<EmberPage> ActivePages => _activePages;

        /// <summary>按层级从高到低查找返回键处理者</summary>
        public bool HandleEscapeKey()
        {
            bool handled = false;
            _pageContext.ForEachVisiblePage(page =>
            {
                handled = ((IUIView)page).TryEscapeKeyClose();
                return handled; // true = 停止遍历
            });
            return handled;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void ProcessPendingOperations()
        {
            if (_pendingOperations.Count == 0) return;

            _isProcessingOperations = true;
            _loadTimer.Restart();

            while (_pendingOperations.Count > 0)
            {
                var op = _pendingOperations.Dequeue();
                op?.Invoke();

                if (_loadTimer.ElapsedMilliseconds > TimeBudgetMs && _pendingOperations.Count > 0)
                    break;
            }

            _isProcessingOperations = false;
        }

        private void ProcessNextFrameCallbacks()
        {
            if (_nextFrameCallbacks.Count == 0) return;

            var count = _nextFrameCallbacks.Count;
            for (int i = 0; i < count; i++)
            {
                _nextFrameCallbacks.Dequeue()?.Invoke();
            }
        }

        private void Shutdown()
        {
            _bgMaskPool?.Clear();
            _pageContext?.CloseAll();
            _activePages.Clear();
            _pendingOperations.Clear();
            _layerCanvases.Clear();

            EmberEventBus.OnNext(EmberUIEvents.UIManagerShutdown);
            EmberDebug.LogShutdown(TAG, "EmberUIManager 已关闭。");
            _initialized = false;
        }

        protected override void OnDestroy()
        {
            Shutdown();
        }

        #endregion
    }
}
