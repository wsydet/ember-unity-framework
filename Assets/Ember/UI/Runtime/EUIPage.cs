// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Cysharp.Threading.Tasks;

using Ember.Basic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 页面包装类 —— 纯 C# 类，非 MonoBehaviour。
    /// 包装一个已实例化的预制体 GameObject，管理其 Canvas/CanvasGroup 生命周期。
    ///
    /// <para>架构（对标 Burner GamePage）：</para>
    /// <code>
    /// 预制体上只有: Canvas + 子控件 + EUIBinding
    /// 运行时创建:   EUIPage (此类) → EUILogic (生成的数据类)
    /// </code>
    ///
    /// <para><b>自研生命周期（完整流程）：</b></para>
    /// <code>
    /// // ── 构造阶段 ──
    /// new EUIPage(go)                     // 包装已实例化的预制体
    ///   → CreateLogic(populateControlMap)    // 反射创建 Logic + OnBind
    ///
    /// // ── 打开流程 ──
    /// Init(args)                             // 数据阶段：填文字、设图片、注册事件
    ///   → OnInitialize(args)                 //   [子类 override] 框架层自定义初始化
    ///   → Logic.OnInit()                     //   [用户 override] 注册事件、设初始值
    ///   → Logic.OnOpen(args)                 //   [用户 override] 携带打开参数
    ///   → Logic.OnReset()                    //   [用户 override] 重置 UI 到默认状态
    ///   → PlayShow()                         // 表现阶段：播放打开动画
    ///       → Logic.OnShow()                 //   [用户 override] 页面即将可见
    ///       → OnShow()                       //   [子类 override] 打开动画协程（返回 null = 无动画）
    ///           → CompleteShow()             // 动画结束：α=1, 可交互, State→Opened
    ///
    /// // ── 运行时 ──
    /// OnPause()                              // 被其他页面遮挡
    ///   → OnPaused()                         //   [子类 override]
    ///   → Logic.OnPause()                    //   [用户 override]
    /// OnResume()                             // 重新回到顶层
    ///   → OnResumed()                        //   [子类 override]
    ///   → Logic.OnResume()                   //   [用户 override]
    ///
    /// // ── 关闭流程 ──
    /// PlayHide()                             // 表现阶段：播放关闭动画
    ///   → Logic.OnHide()                     //   [用户 override] 页面即将隐藏
    ///   → OnHide()                           //   [子类 override] 关闭动画协程（返回 null = 无动画）
    ///       → CompleteHide()                 // 动画结束：α=0, 不可交互, State→Closed
    ///   → Cleanup()                          // 数据阶段：注销事件、释放引用
    ///       → OnCleanup()                    //   [子类 override] 框架层清理
    ///       → Logic.OnClose()                //   [用户 override] 关闭时持久化（此时 UI 状态还在）
    ///       → Logic.OnReset()                //   [用户 override] 重置到默认状态
    ///       → Logic.OnDispose()              //   [用户 override] 注销事件、释放引用
    ///       → Destroy(gameObject)            // 框架销毁 GameObject
    ///
    /// // ── 复用路径 ──
    /// OnReopen(args)                         // 已关闭页面重新打开（跳过 Init）
    ///   → OnReopened(args)                   //   [子类 override]
    ///   → Logic.OnOpen(args)                 //   [用户 override]
    ///   → PlayShow()                         // 直接进入 Show 流程
    ///
    /// // ── 输入 ──
    /// TryEscapeKeyClose()                    // 返回键处理（递归询问子页面）
    ///   → OnEscapeKey()                      //   [子类 override] return true 阻止冒泡
    /// </code>
    ///
    /// <para><b>三层分工：</b></para>
    /// <list type="bullet">
    ///   <item><b>IEUIView</b> — 契约：定义生命周期方法签名，<see cref="EUIViewEngine"/> 只认这个接口</item>
    ///   <item><b>EUIPage</b> — 框架实现：管理 GameObject/Canvas/CanvasGroup，驱动 Logic 钩子，
    ///        提供 protected virtual 动画钩子（<see cref="OnShow"/> / <see cref="OnHide"/> 等）供子类 override</item>
    ///   <item><b>EUILogic</b> — 业务基类：用户继承后 override 钩子方法，写具体业务逻辑，
    ///        不需要知道框架流程怎么走的</item>
    /// </list>
    /// </summary>
    public class EUIPage : IEUIView
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        private readonly GameObject _gameObject;
        private readonly CanvasGroup _canvasGroup;
        private readonly Canvas _canvas;
        private readonly RectTransform _rectTransform;

        private PageState _state = PageState.Unloaded;
        private EUIPageDef _pageDef;
        private EUIPage _parentPage;
        private readonly List<EUIPage> _subPages = new List<EUIPage>();
        private EUILogic _logic;
        private string _logicTypeName;

        // 过渡动画配置（由 EUIBindingBridge 在页面创建时注入）
        private bool _usePresetFade;
        private bool _useCustomTransition;
        private float _transitionInTime;
        private float _transitionOutTime;

        // 动画完成回调（由 EUIViewEngine 在调用 PlayShow/PlayHide 前注入）
        private Action _onShowComplete;
        private object _showArgs;
        private Action _onHideComplete;

        // 延迟销毁（对标 Burner GamePage）
        private bool _autoDestroy = true;
        private float _destroyDelay = 30f;
        private bool _isClosing;
        private float _closeTime;

        // 加载计时（对标 Burner PageLoadTiming）
        private LoadTiming _loadTiming;
        private float _initStartTime;
        private float _showStartTime;

        // SubPage 排序（对标 Burner SubPageOrderGrowStep）
        private const int SubPageOrderGrowStep = 50;

        // 挂起操作（对标 Burner PageTargetState）
        private PagePendingOp _pendingOp;
        private object _pendingOpArgs;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        /// <summary>
        /// 页面处于过渡状态时（Showing/Hiding），收到的操作挂起到此枚举。
        /// </summary>
        internal enum PagePendingOp
        {
            None,
            Close,
            Reopen,
        }

        /// <summary>
        /// 页面加载耗时数据（对标 Burner PageLoadTiming）。
        /// 仅首次打开时记录，后续 Reopen 不更新。
        /// </summary>
        public struct LoadTiming
        {
            /// <summary>Prefab 加载耗时（ms）。Ember 中 Prefab 在 Router 层同步 Instantiate，此项为 0。</summary>
            public float AssetLoadMs;
            /// <summary>Init 阶段耗时（ms）：OnInitialize + Logic.OnInit + OnOpen + OnReset</summary>
            public float InitMs;
            /// <summary>PlayShow 阶段耗时（ms）：Logic.OnShow + OnShow 动画</summary>
            public float ShowMs;
            /// <summary>总耗时（ms）</summary>
            public float TotalMs;
            /// <summary>是否首次打开（false = 本次是 Reopen）</summary>
            public bool IsFirstOpen;
        }

        #endregion

        // --------------------------------------------------------

        #region 构造 & 销毁

        /// <summary>
        /// 创建一个页面包装实例。
        /// </summary>
        /// <param name="gameObject">已实例化的预制体根节点</param>
        public EUIPage(GameObject gameObject)
        {
            _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            _canvasGroup = _gameObject.GetComponent<CanvasGroup>();
            _canvas = _gameObject.GetComponent<Canvas>();
            _rectTransform = _gameObject.GetComponent<RectTransform>();

            if (_canvasGroup == null)
                _canvasGroup = _gameObject.AddComponent<CanvasGroup>();
            if (_canvas == null)
                _canvas = _gameObject.AddComponent<Canvas>();
        }

        /// <summary>
        /// 创建逻辑层实例。Caller 负责填充 ControlMap。
        /// </summary>
        /// <param name="populateControlMap">填充 ControlMap 的回调（接收 ControlMap 和当前 Logic 实例，供子 UIBinding 注册）</param>
        public void CreateLogic(Action<Dictionary<string, Component>, EUILogic> populateControlMap)
        {
            if (string.IsNullOrEmpty(_logicTypeName)) return;

            try
            {
                Type type = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(_logicTypeName);
                    if (type != null) break;
                }

                if (type != null && typeof(EUILogic).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    _logic = (EUILogic)Activator.CreateInstance(type);
                    _logic.Page = this;
                    _logic.ControlMap = new Dictionary<string, Component>();
                    populateControlMap?.Invoke(_logic.ControlMap, _logic);
                    _logic.OnBind();
                }
                else
                {
                    EmberDebug.LogWarning(TAG, $"Logic type '{_logicTypeName}' not found for '{_gameObject.name}'.");
                }
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"Logic creation failed for '{_gameObject.name}': {ex}");
            }
        }

        #endregion

        // --------------------------------------------------------

        #region IEUIView 实现

        /// <inheritdoc />
        public void Init(object args)
        {
            if (_state != PageState.Unloaded && _state != PageState.Closed)
            {
                EmberDebug.LogWarning(TAG, $"EUIPage.Init: '{Name}' state={_state}, expected Unloaded/Closed.");
                return;
            }

            Profiler.BeginSample("EUIPage.Init");
            _initStartTime = Time.realtimeSinceStartup;
            _loadTiming.IsFirstOpen = true;
            _state = PageState.Loaded;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _gameObject.SetActive(true);

            try
            {
                OnInitialize(args);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"EUIPage.Init '{Name}' error: {ex}");
            }

            // 通知逻辑层
            if (_logic != null)
            {
                try { _logic.BroadcastInit(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnInit '{Name}': {ex}"); }

                try { _logic.BroadcastOpen(args); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnOpen '{Name}': {ex}"); }

                try { _logic.BroadcastReset(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnReset '{Name}': {ex}"); }
            }

            _loadTiming.InitMs = (Time.realtimeSinceStartup - _initStartTime) * 1000f;
            Profiler.EndSample();
        }

        /// <inheritdoc />
        public void PlayShow()
        {
            if (_state != PageState.Loaded)
            {
                EmberDebug.LogWarning(TAG, $"EUIPage.PlayShow: '{Name}' state={_state}, expected Loaded.");
                return;
            }

            Profiler.BeginSample("EUIPage.PlayShow");
            _showStartTime = Time.realtimeSinceStartup;
            _state = PageState.Showing;

            try { _logic?.BroadcastShow(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnShow '{Name}': {ex}"); }

            if (!_usePresetFade && !_useCustomTransition)
            {
                CompleteShow();
            }
            else
            {
                RunShowAnimationSequence().Forget();
            }
            Profiler.EndSample();
        }

        /// <inheritdoc />
        public void OnPause()
        {
            if (_state != PageState.Opened) return;
            _state = PageState.Paused;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            OnPaused();
            try { _logic?.BroadcastPause(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnPause '{Name}': {ex}"); }
        }

        /// <inheritdoc />
        public void OnResume()
        {
            if (_state != PageState.Paused) return;
            _state = PageState.Opened;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            OnResumed();
            try { _logic?.BroadcastResume(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnResume '{Name}': {ex}"); }
        }

        /// <inheritdoc />
        public void OnReopen(object args)
        {
            if (_state != PageState.Closed) return;
            _state = PageState.Loaded;
            _canvasGroup.alpha = 0f;
            _gameObject.SetActive(true);
            OnReopened(args);

            try { _logic?.BroadcastOpen(args); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnOpen '{Name}': {ex}"); }

            PlayShow();
        }

        /// <inheritdoc />
        public void PlayHide()
        {
            if (_state == PageState.Unloaded || _state == PageState.Closed)
                return;

            Profiler.BeginSample("EUIPage.PlayHide");
            _state = PageState.Hiding;
            try { _logic?.BroadcastHide(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnHide '{Name}': {ex}"); }

            if (!_usePresetFade && !_useCustomTransition)
            {
                CompleteHide();
            }
            else
            {
                RunHideAnimationSequence().Forget();
            }
            Profiler.EndSample();
        }

        /// <inheritdoc />
        public void Cleanup()
        {
            if (_state == PageState.Unloaded) return;
            Profiler.BeginSample("EUIPage.Cleanup");
            _state = PageState.Unloaded;

            try { OnCleanup(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"EUIPage.Cleanup '{Name}' error: {ex}"); }

            // 清理逻辑层
            if (_logic != null)
            {
                try { _logic.BroadcastClose(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnClose '{Name}': {ex}"); }
                try { _logic.BroadcastReset(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnReset '{Name}': {ex}"); }
                try { _logic.BroadcastDispose(); }
                catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnDispose '{Name}': {ex}"); }
                _logic = null;
            }

            // 清理子页面
            foreach (var sub in _subPages.ToArray())
            {
                if (sub != null)
                    sub.Dispose();
            }
            _subPages.Clear();

            _gameObject.SetActive(false);
            Profiler.EndSample();
        }

        /// <inheritdoc />
        public bool TryEscapeKeyClose()
        {
            foreach (var sub in _subPages)
            {
                if (sub != null && sub.TryEscapeKeyClose())
                    return true;
            }
            return OnEscapeKey();
        }

        /// <inheritdoc />
        public bool IsInitialized => _state >= PageState.Loaded;

        /// <inheritdoc />
        public bool IsOpened => _state == PageState.Opened;

        /// <inheritdoc />
        public PageState State => _state;

        #endregion

        // --------------------------------------------------------

        #region 子类可 override 的钩子

        /// <summary>
        /// 初始化数据 —— 在 <see cref="Init"/> 的数据阶段调用，<b>早于 <see cref="EUILogic.OnInit"/> 和 <see cref="EUILogic.OnOpen"/> 和 <see cref="EUILogic.OnReset"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>页面首次打开（State=Unloaded）或重新打开已关闭页面（State=Closed）时，
        /// 由 <see cref="Init"/> 调用。此时 CanvasGroup α=0，页面不可交互。</para>
        /// <para><b>职责：</b>框架层自定义初始化数据。只做数据操作，不要做动画。
        /// 动画逻辑请 override <see cref="OnShow"/>。</para>
        /// <para><b>业务层对应：</b><see cref="EUILogic.OnInit"/> + <see cref="EUILogic.OnOpen"/> + <see cref="EUILogic.OnReset"/>，
        /// 业务初始化应写在 EUILogic 子类中，不要写在这里。</para>
        /// <param name="args">打开时传入的参数（来自 EUIManager.ShowMainPage/ShowPopup 的 args）</param>
        protected virtual void OnInitialize(object args) { }

        /// <summary>
        /// 打开动画 —— 在 <see cref="PlayShow"/> 中、<b><see cref="EUILogic.OnShow"/> 之后</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b>在 Init 完成（State=Loaded）之后，由 <see cref="PlayShow"/> 调用。
        /// 此时数据已就绪（Logic.OnInit/OnOpen 已执行完），CanvasGroup α=0。</para>
        /// <para><b>职责：</b>播放打开动画（fade in、slide in 等）。框架 await 此 UniTask，
        /// 动画结束后自动调用 <see cref="CompleteShow"/>（α=1, 可交互, State→Opened）。</para>
        /// <para><b>注意：</b>这是 EUIPage 子类的 override 点，不是 EUILogic 的。
        /// <b>业务逻辑</b>（如刷新数据）应写在 <see cref="EUILogic.OnShow"/> 中，不要写在这里。</para>
        /// <para><b>示例：</b></para>
        /// <code>
        /// public class MyFancyPage : EUIPage
        /// {
        ///     protected override async UniTask OnShow()
        ///     {
        ///         await CanvasGroup.DOFade(1f, 0.3f).ToUniTask();
        ///     }
        /// }
        /// </code>
        protected virtual UniTask OnShow() => UniTask.CompletedTask;

        /// <summary>
        /// 关闭动画 —— 在 <see cref="PlayHide"/> 中、<b><see cref="EUILogic.OnHide"/> 之后</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b>页面被关闭时，在 <see cref="PlayHide"/> 中调用。此时 State=Hiding，Logic.OnHide 已执行完。</para>
        /// <para><b>职责：</b>播放关闭动画（fade out、slide out 等）。框架 await 动画完成后再调 <see cref="CompleteHide"/>
        /// （α=0, 不可交互, State→Closed），随后进入 <see cref="Cleanup"/>。</para>
        /// <para><b>示例：</b></para>
        /// <code>
        /// public class MyFancyPage : EUIPage
        /// {
        ///     protected override async UniTask OnHide()
        ///     {
        ///         await CanvasGroup.DOFade(0f, 0.2f).ToUniTask();
        ///     }
        /// }
        /// </code>
        protected virtual UniTask OnHide() => UniTask.CompletedTask;

        /// <summary>
        /// 清理 —— 在 <see cref="Cleanup"/> 中、<b>最早调用</b>（早于 <see cref="EUILogic.OnClose"/>、<see cref="EUILogic.OnReset"/>、<see cref="EUILogic.OnDispose"/>）。
        /// </summary>
        /// <para><b>触发时机：</b>PlayHide 动画完成后（或无需动画时立即），在 Cleanup 中<b>第一顺位</b>调用。
        /// 此时页面 State 刚从 Hiding/Closed 切换，GameObject 仍然存在。</para>
        /// <para><b>职责：</b>框架层自定义清理。注销页面级事件、释放框架层引用。
        /// 业务层的清理应写在 <see cref="EUILogic.OnDispose"/> 中，不要写在这里。</para>
        /// <para><b>注意：</b>在此方法之后，Logic.OnClose → Logic.OnReset → Logic.OnDispose 依次执行，然后子页面递归清理，最后 SetActive(false)。</para>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// 被遮挡时回调 —— 在 <see cref="IEUIView.OnPause"/> 中、<b>早于 <see cref="EUILogic.OnPause"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>另一个页面 Push 到上方时（如 MainPage 上弹出 Popup、新 MainPage 替换当前）。
        /// 被遮挡的页面不会被销毁，State 从 Opened 切换为 Paused。</para>
        /// <para><b>职责：</b>框架层在页面被遮挡时的处理。业务层处理请用 <see cref="EUILogic.OnPause"/>。</para>
        protected virtual void OnPaused() { }

        /// <summary>
        /// 重新可见时回调 —— 在 <see cref="IEUIView.OnResume"/> 中、<b>早于 <see cref="EUILogic.OnResume"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>上方遮挡的页面关闭后，当前页面重新回到栈顶。State 从 Paused 恢复为 Opened。</para>
        /// <para><b>职责：</b>框架层在页面恢复时的处理。业务层处理请用 <see cref="EUILogic.OnResume"/>。</para>
        protected virtual void OnResumed() { }

        /// <summary>
        /// 已加载页面被重新打开 —— 在 <see cref="IEUIView.OnReopen"/> 中调用。
        /// </summary>
        /// <para><b>触发时机：</b>State=Closed 的页面被 <see cref="EUIViewEngine.ReopenPage"/> 重新打开时。
        /// 与 <see cref="OnInitialize"/> 互斥：已加载的页面走 OnReopen，不重新走 Init 流程。</para>
        /// <para><b>职责：</b>恢复页面状态。框架随后自动调用 Logic.OnOpen + PlayShow。</para>
        /// <param name="args">重新打开时传入的参数</param>
        protected virtual void OnReopened(object args) { }

        /// <summary>
        /// 返回键处理 —— 在 <see cref="TryEscapeKeyClose"/> 中、<b>子页面均未处理时</b>最后调用。
        /// </summary>
        /// <para><b>触发时机：</b>用户按 ESC / Android 返回键时，
        /// <see cref="EUIViewEngine.HandleEscapeKey"/> 从 TopMost → Popup → MainPage 逐层调用每个页面的 TryEscapeKeyClose。
        /// 每个页面先递归询问自己的 SubPage，若子页面均未处理，才调用此方法。</para>
        /// <para><b>返回值：</b>return true 表示已处理（阻止事件继续向更低层冒泡），return false 表示未处理。</para>
        /// <para><b>典型用法：</b>Popup 页面 override 此方法 return true 关闭自己，实现"按返回键关闭弹窗"。</para>
        protected virtual bool OnEscapeKey() { return false; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private async UniTask RunShowAnimationSequence()
        {
            if (_usePresetFade)
                await EUIViewEngine.Instance.TransitionHandler.PlayShowAsync(_gameObject, _transitionInTime);
            if (_useCustomTransition)
                await (_logic?.OnCustomEnter() ?? UniTask.CompletedTask);
            CompleteShow();
        }

        private void CompleteShow()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _state = PageState.Opened;

            // 加载计时（对标 Burner PageLoadTiming）
            if (_loadTiming.IsFirstOpen)
            {
                _loadTiming.ShowMs = (Time.realtimeSinceStartup - _showStartTime) * 1000f;
                _loadTiming.TotalMs = _loadTiming.InitMs + _loadTiming.ShowMs;
                EmberDebug.LogInit(TAG, $"页面加载完成: {Name} init={_loadTiming.InitMs:F1}ms show={_loadTiming.ShowMs:F1}ms total={_loadTiming.TotalMs:F1}ms");
            }

            // 动画真正结束 → 播报事件 + 日志 + 回调
            if (_pageDef != null)
                EUIObserver.NotifyOpened(_pageDef, _showArgs);
            EmberDebug.LogEvent(TAG, $"页面已打开: {_pageDef}");
            _onShowComplete?.Invoke();
            _onShowComplete = null;
            _showArgs = null;

            // 过渡完成 → 执行挂起操作（对标 Burner ExecutePendingOperationIfAny）
            FlushPendingOp();
        }

        private async UniTask RunHideAnimationSequence()
        {
            if (_usePresetFade)
                await EUIViewEngine.Instance.TransitionHandler.PlayHideAsync(_gameObject, _transitionOutTime);
            if (_useCustomTransition)
                await (_logic?.OnCustomExit() ?? UniTask.CompletedTask);
            CompleteHide();
        }

        private void CompleteHide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _state = PageState.Closed;

            // 动画真正结束 → 播报事件 + 日志 + 回调（回调中包含 Cleanup + Destroy 调度）
            if (_pageDef != null)
                EUIObserver.NotifyClosed(_pageDef, null);
            EmberDebug.LogCleanup(TAG, $"页面已关闭: {_pageDef}");
            _onHideComplete?.Invoke();
            _onHideComplete = null;

            // 过渡完成 → 执行挂起操作（对标 Burner ExecutePendingOperationIfAny）
            FlushPendingOp();
        }

        internal void Dispose()
        {
            if (_gameObject != null)
                UnityEngine.Object.Destroy(_gameObject);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>页面名称</summary>
        public string Name => _gameObject ? _gameObject.name : "(destroyed)";

        /// <summary>包装的 GameObject</summary>
        public GameObject GameObject => _gameObject;

        /// <summary>页面 Transform</summary>
        public Transform Transform => _gameObject.transform;

        /// <summary>CanvasGroup 引用</summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>Canvas 引用</summary>
        public Canvas Canvas => _canvas;

        /// <summary>RectTransform 引用</summary>
        public RectTransform RectTransform => _rectTransform;

        /// <summary>页面定义元数据</summary>
        public EUIPageDef EUIPageDef
        {
            get => _pageDef;
            internal set => _pageDef = value;
        }

        /// <summary>父页面（SubPage 时非空）</summary>
        public EUIPage ParentPage
        {
            get => _parentPage;
            internal set => _parentPage = value;
        }

        /// <summary>子页面列表</summary>
        public IReadOnlyList<EUIPage> SubPages => _subPages;

        /// <summary>页面加载耗时数据（仅首次打开时有效）</summary>
        public LoadTiming LoadTimingData => _loadTiming;

        /// <summary>逻辑层实例</summary>
        public EUILogic Logic => _logic;

        /// <summary>逻辑层类型全名</summary>
        public string LogicTypeName
        {
            get => _logicTypeName;
            set => _logicTypeName = value;
        }

        /// <summary>注册子页面</summary>
        internal void RegisterSubPage(EUIPage subPage)
        {
            if (!_subPages.Contains(subPage))
                _subPages.Add(subPage);
            subPage._parentPage = this;
        }

        /// <summary>注销子页面</summary>
        internal void UnregisterSubPage(EUIPage subPage)
        {
            _subPages.Remove(subPage);
            subPage._parentPage = null;
        }

        /// <summary>
        /// 计算新 SubPage 的 Canvas.sortingOrder。
        /// 规则：找到顶层非 SubPage 祖先，在已有子页面最大 sortingOrder 基础上递增 <see cref="SubPageOrderGrowStep"/>。
        /// </summary>
        /// <returns>新 SubPage 应使用的 sortingOrder</returns>
        internal int GetNextSubPageSortingOrder()
        {
            // 找到顶层非 SubPage 页面
            var root = this;
            while (root._parentPage != null && root._parentPage.EUIPageDef?.PageType == PageType.SubPage)
                root = root._parentPage;

            int baseOrder = root._canvas ? root._canvas.sortingOrder : 0;

            // 遍历 root 的所有已有子页面，找到最大 sortingOrder
            int maxSubOrder = baseOrder;
            if (root._subPages.Count > 0)
            {
                foreach (var sub in root._subPages)
                {
                    if (sub != this && sub._canvas)
                        maxSubOrder = Math.Max(maxSubOrder, sub._canvas.sortingOrder);
                }
            }

            return maxSubOrder + SubPageOrderGrowStep;
        }

        /// <summary>
        /// 页面处于过渡状态（Showing/Hiding）时，将操作挂起。
        /// 过渡完成后由 <see cref="CompleteShow"/> 或 <see cref="CompleteHide"/> 重放。
        /// </summary>
        internal bool TryQueuePendingOp(PagePendingOp op, object args)
        {
            if (_state != PageState.Showing && _state != PageState.Hiding)
                return false;

            _pendingOp = op;
            _pendingOpArgs = args;
            EmberDebug.LogWarning(TAG, $"页面 '{Name}' 处于 {_state}，操作 '{op}' 已挂起。");
            return true;
        }

        /// <summary>当前挂起的操作类型</summary>
        internal PagePendingOp PendingOp => _pendingOp;

        /// <summary>清除挂起操作</summary>
        internal void ClearPendingOp()
        {
            _pendingOp = PagePendingOp.None;
            _pendingOpArgs = null;
        }

        /// <summary>
        /// 过渡状态结束后，重放挂起的操作。
        /// 只做日志记录，实际操作由 <see cref="EUIViewEngine"/> 通过
        /// <see cref="PendingOp"/> 属性检查后调度执行。
        /// </summary>
        private void FlushPendingOp()
        {
            if (_pendingOp == PagePendingOp.None) return;

            EmberDebug.Log(TAG, $"页面 '{Name}' 完成过渡，挂起操作 '{_pendingOp}' 等待调度。");
            // _pendingOp 保留，由 EUIViewEngine 的 CompleteShow/CompleteHide 回调检查并调度
        }

        /// <summary>
        /// 注入过渡动画配置。由 <see cref="EUIBindingBridge"/> 在页面创建时调用。
        /// <see cref="PlayShow"/> / <see cref="PlayHide"/> 根据两个独立开关决定动画链：
        /// <list type="bullet">
        ///   <item>仅 <b>Preset</b>：全局 Handler 的 CanvasGroup alpha 渐变</item>
        ///   <item>仅 <b>Custom</b>：<see cref="EUILogic.OnCustomEnter"/> / <see cref="EUILogic.OnCustomExit"/></item>
        ///   <item><b>Preset + Custom</b>：先播全局预设，再播自定义（叠加）</item>
        ///   <item>都不勾：无动画，立即完成</item>
        /// </list>
        /// </summary>
        /// <param name="usePreset">是否启用全局预设过渡动画</param>
        /// <param name="useCustom">是否启用自定义过渡动画</param>
        /// <param name="inTime">进入动画时长（秒），预设使用</param>
        /// <param name="outTime">退出动画时长（秒），预设使用</param>
        public void SetTransition(bool usePreset, bool useCustom, float inTime, float outTime)
        {
            _usePresetFade = usePreset;
            _useCustomTransition = useCustom;
            _transitionInTime = inTime;
            _transitionOutTime = outTime;
        }

        /// <summary>
        /// 注入打开动画完成回调。由 <see cref="EUIViewEngine"/> 在调用 PlayShow 前设置，
        /// 在 <see cref="CompleteShow"/> 中（动画真正结束时）执行。
        /// </summary>
        internal void SetShowCallback(Action onComplete, object args)
        {
            _onShowComplete = onComplete;
            _showArgs = args;
        }

        /// <summary>
        /// 注入关闭动画完成回调。由 <see cref="EUIViewEngine"/> 在调用 PlayHide 前设置，
        /// 在 <see cref="CompleteHide"/> 中（动画真正结束时）执行，回调内负责调度 Cleanup + Destroy。
        /// </summary>
        internal void SetHideCallback(Action onComplete)
        {
            _onHideComplete = onComplete;
        }

        /// <summary>获取组件（转发到 GameObject）</summary>
        public T GetComponent<T>() where T : Component
        {
            return _gameObject ? _gameObject.GetComponent<T>() : null;
        }

        // ── 延迟销毁（对标 Burner GamePage） ──

        /// <summary>是否处于延迟销毁等待中</summary>
        internal bool IsClosing => _isClosing;

        /// <summary>预制体路径（用于延迟销毁复用查找）</summary>
        internal string PrefabPath => _pageDef?.PrefabPath;

        /// <summary>关闭后是否自动销毁（false = 常驻页面）</summary>
        internal bool AutoDestroy => _autoDestroy;

        /// <summary>延迟销毁等待时间（秒）</summary>
        internal float DestroyDelay => _destroyDelay;

        /// <summary>
        /// 进入延迟销毁状态：隐藏 GameObject，记录时间戳，等待 DestroyDelay 秒后真正销毁。
        /// </summary>
        internal void EnterClosingState()
        {
            _isClosing = true;
            _closeTime = Time.realtimeSinceStartup;
            if (_gameObject != null)
                _gameObject.SetActive(false);
        }

        /// <summary>
        /// 退出延迟销毁状态：GameObject 重新激活，准备被复用。
        /// </summary>
        internal void CancelClosing()
        {
            _isClosing = false;
            if (_gameObject != null)
                _gameObject.SetActive(true);
        }

        /// <summary>
        /// 检查延迟是否到期，到期则应执行真正销毁。
        /// </summary>
        internal bool ShouldDisposeNow()
        {
            return _isClosing && _autoDestroy
                && (Time.realtimeSinceStartup - _closeTime) > _destroyDelay;
        }

        /// <summary>
        /// 强制立即销毁 GameObject（跳过延迟）。
        /// </summary>
        internal void ForceDispose()
        {
            _isClosing = false;
            if (_gameObject != null)
                UnityEngine.Object.Destroy(_gameObject);
        }

        #endregion
    }
}
