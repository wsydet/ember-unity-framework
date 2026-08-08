// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Cysharp.Threading.Tasks;

using Ember.Basic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 页面包装类 —— 纯 C# 类，非 MonoBehaviour。
    /// 包装一个已实例化的预制体 GameObject，管理其 Canvas/CanvasGroup 生命周期。
    ///
    /// <para>架构（对标 Burner GamePage）：</para>
    /// <code>
    /// 预制体上只有: Canvas + 子控件 + EmberUIBinding
    /// 运行时创建:   EmberPage (此类) → EmberUILogic (生成的数据类)
    /// </code>
    ///
    /// <para><b>自研生命周期（完整流程）：</b></para>
    /// <code>
    /// // ── 构造阶段 ──
    /// new EmberPage(go)                     // 包装已实例化的预制体
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
    ///   <item><b>IUIView</b> — 契约：定义生命周期方法签名，<see cref="EmberUIManager"/> 只认这个接口</item>
    ///   <item><b>EmberPage</b> — 框架实现：管理 GameObject/Canvas/CanvasGroup，驱动 Logic 钩子，
    ///        提供 protected virtual 动画钩子（<see cref="OnShow"/> / <see cref="OnHide"/> 等）供子类 override</item>
    ///   <item><b>EmberUILogic</b> — 业务基类：用户继承后 override 钩子方法，写具体业务逻辑，
    ///        不需要知道框架流程怎么走的</item>
    /// </list>
    /// </summary>
    public class EmberPage : IUIView
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        private readonly GameObject _gameObject;
        private readonly CanvasGroup _canvasGroup;
        private readonly Canvas _canvas;
        private readonly RectTransform _rectTransform;

        private PageState _state = PageState.Unloaded;
        private PageDef _pageDef;
        private EmberPage _parentPage;
        private readonly List<EmberPage> _subPages = new List<EmberPage>();
        private EmberUILogic _logic;
        private string _logicTypeName;

        // 预设渐入渐出（由 EmberUIBindingBridge 在页面创建时注入）
        private bool _usePresetFade;
        private float _presetFadeInTime;
        private float _presetFadeOutTime;

        // 动画完成回调（由 EmberUIManager 在调用 PlayShow/PlayHide 前注入）
        private Action _onShowComplete;
        private object _showArgs;
        private Action _onHideComplete;

        // 延迟销毁（对标 Burner GamePage）
        private bool _autoDestroy = true;
        private float _destroyDelay = 30f;
        private bool _isClosing;
        private float _closeTime;

        // 子页面的子页面（递归追踪）
        private readonly List<EmberPage> _subPagesLinear = new List<EmberPage>();

        #endregion

        // --------------------------------------------------------

        #region 构造 & 销毁

        /// <summary>
        /// 创建一个页面包装实例。
        /// </summary>
        /// <param name="gameObject">已实例化的预制体根节点</param>
        public EmberPage(GameObject gameObject)
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
        public void CreateLogic(Action<Dictionary<string, Component>, EmberUILogic> populateControlMap)
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

                if (type != null && typeof(EmberUILogic).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    _logic = (EmberUILogic)Activator.CreateInstance(type);
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

        #region IUIView 实现

        /// <inheritdoc />
        public void Init(object args)
        {
            if (_state != PageState.Unloaded && _state != PageState.Closed)
            {
                EmberDebug.LogWarning(TAG, $"EmberPage.Init: '{Name}' state={_state}, expected Unloaded/Closed.");
                return;
            }

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
                EmberDebug.LogError(TAG, $"EmberPage.Init '{Name}' error: {ex}");
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
        }

        /// <inheritdoc />
        public void PlayShow()
        {
            if (_state != PageState.Loaded)
            {
                EmberDebug.LogWarning(TAG, $"EmberPage.PlayShow: '{Name}' state={_state}, expected Loaded.");
                return;
            }

            _state = PageState.Showing;

            try { _logic?.BroadcastShow(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnShow '{Name}': {ex}"); }

            if (_usePresetFade)
            {
                // 预设渐入动画（UniTask），跳过子类 OnShow() virtual
                EmberUIManager.Instance.StartPageCoroutine(
                    PresetFadeInAsync().ToCoroutine());
            }
            else
            {
                var routine = OnShow();
                if (routine == null)
                {
                    CompleteShow();
                }
                else
                {
                    // 协程需要通过外部驱动（EmberUIManager 持有 MonoBehaviour 启动协程）
                    EmberUIManager.Instance.StartPageCoroutine(PlayShowRoutine(routine));
                }
            }
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

            _state = PageState.Hiding;
            try { _logic?.BroadcastHide(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"Logic.OnHide '{Name}': {ex}"); }

            if (_usePresetFade)
            {
                // 预设渐出动画（UniTask），跳过子类 OnHide() virtual
                EmberUIManager.Instance.StartPageCoroutine(
                    PresetFadeOutAsync().ToCoroutine());
            }
            else
            {
                var routine = OnHide();
                if (routine == null)
                {
                    CompleteHide();
                }
                else
                {
                    EmberUIManager.Instance.StartPageCoroutine(PlayHideRoutine(routine));
                }
            }
        }

        /// <inheritdoc />
        public void Cleanup()
        {
            if (_state == PageState.Unloaded) return;
            _state = PageState.Unloaded;

            try { OnCleanup(); }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"EmberPage.Cleanup '{Name}' error: {ex}"); }

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
            _subPagesLinear.Clear();

            _gameObject.SetActive(false);
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
        /// 初始化数据 —— 在 <see cref="Init"/> 的数据阶段调用，<b>早于 <see cref="EmberUILogic.OnInit"/> 和 <see cref="EmberUILogic.OnOpen"/> 和 <see cref="EmberUILogic.OnReset"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>页面首次打开（State=Unloaded）或重新打开已关闭页面（State=Closed）时，
        /// 由 <see cref="Init"/> 调用。此时 CanvasGroup α=0，页面不可交互。</para>
        /// <para><b>职责：</b>框架层自定义初始化数据。只做数据操作，不要做动画。
        /// 动画逻辑请 override <see cref="OnShow"/>。</para>
        /// <para><b>业务层对应：</b><see cref="EmberUILogic.OnInit"/> + <see cref="EmberUILogic.OnOpen"/> + <see cref="EmberUILogic.OnReset"/>，
        /// 业务初始化应写在 EmberUILogic 子类中，不要写在这里。</para>
        /// <param name="args">打开时传入的参数（来自 EmberUIPageRouter.ShowMainPage/ShowPopup 的 args）</param>
        protected virtual void OnInitialize(object args) { }

        /// <summary>
        /// 打开动画协程 —— 在 <see cref="PlayShow"/> 中、<b><see cref="EmberUILogic.OnShow"/> 之后</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b>在 Init 完成（State=Loaded）之后，由 <see cref="PlayShow"/> 调用。
        /// 此时数据已就绪（Logic.OnInit/OnOpen 已执行完），CanvasGroup α=0。</para>
        /// <para><b>职责：</b>播放打开动画（fade in、slide in 等）。框架通过 <see cref="EmberUIManager.StartPageCoroutine"/> 驱动此协程，
        /// 动画结束后自动调用 <see cref="CompleteShow"/>（α=1, 可交互, State→Opened）。</para>
        /// <para><b>返回值：</b></para>
        /// <list type="bullet">
        ///   <item>返回 <c>null</c> — 无动画，框架立即调用 <see cref="CompleteShow"/> 完成打开</item>
        ///   <item>返回 <c>IEnumerator</c> — 框架启动协程等待动画完成后再完成打开</item>
        /// </list>
        /// <para><b>注意：</b>这是 EmberPage 子类的 override 点，不是 EmberUILogic 的。
        /// <b>业务逻辑</b>（如刷新数据）应写在 <see cref="EmberUILogic.OnShow"/> 中，不要写在这里。</para>
        /// <para><b>示例：</b></para>
        /// <code>
        /// public class MyFancyPage : EmberPage
        /// {
        ///     protected override IEnumerator OnShow()
        ///     {
        ///         // 播放 DOTween / Animator 动画
        ///         yield return CanvasGroup.DOFade(1f, 0.3f).WaitForCompletion();
        ///     }
        /// }
        /// </code>
        protected virtual System.Collections.IEnumerator OnShow() { return null; }

        /// <summary>
        /// 关闭动画协程 —— 在 <see cref="PlayHide"/> 中、<b><see cref="EmberUILogic.OnHide"/> 之后</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b>页面被关闭时，在 <see cref="PlayHide"/> 中调用。此时 State=Hiding，Logic.OnHide 已执行完。</para>
        /// <para><b>职责：</b>播放关闭动画（fade out、slide out 等）。动画结束后自动调用 <see cref="CompleteHide"/>
        /// （α=0, 不可交互, State→Closed），随后框架调用 <see cref="Cleanup"/> 执行数据清理。</para>
        /// <para><b>返回值：</b></para>
        /// <list type="bullet">
        ///   <item>返回 <c>null</c> — 无动画，框架立即调用 <see cref="CompleteHide"/> 然后进入 <see cref="Cleanup"/></item>
        ///   <item>返回 <c>IEnumerator</c> — 框架启动协程等待动画完成后再进入 Cleanup</item>
        /// </list>
        /// <para><b>示例：</b></para>
        /// <code>
        /// public class MyFancyPage : EmberPage
        /// {
        ///     protected override IEnumerator OnHide()
        ///     {
        ///         yield return CanvasGroup.DOFade(0f, 0.2f).WaitForCompletion();
        ///     }
        /// }
        /// </code>
        protected virtual System.Collections.IEnumerator OnHide() { return null; }

        /// <summary>
        /// 清理 —— 在 <see cref="Cleanup"/> 中、<b>最早调用</b>（早于 <see cref="EmberUILogic.OnClose"/>、<see cref="EmberUILogic.OnReset"/>、<see cref="EmberUILogic.OnDispose"/>）。
        /// </summary>
        /// <para><b>触发时机：</b>PlayHide 动画完成后（或无需动画时立即），在 Cleanup 中<b>第一顺位</b>调用。
        /// 此时页面 State 刚从 Hiding/Closed 切换，GameObject 仍然存在。</para>
        /// <para><b>职责：</b>框架层自定义清理。注销页面级事件、释放框架层引用。
        /// 业务层的清理应写在 <see cref="EmberUILogic.OnDispose"/> 中，不要写在这里。</para>
        /// <para><b>注意：</b>在此方法之后，Logic.OnClose → Logic.OnReset → Logic.OnDispose 依次执行，然后子页面递归清理，最后 SetActive(false)。</para>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// 被遮挡时回调 —— 在 <see cref="IUIView.OnPause"/> 中、<b>早于 <see cref="EmberUILogic.OnPause"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>另一个页面 Push 到上方时（如 MainPage 上弹出 Popup、新 MainPage 替换当前）。
        /// 被遮挡的页面不会被销毁，State 从 Opened 切换为 Paused。</para>
        /// <para><b>职责：</b>框架层在页面被遮挡时的处理。业务层处理请用 <see cref="EmberUILogic.OnPause"/>。</para>
        protected virtual void OnPaused() { }

        /// <summary>
        /// 重新可见时回调 —— 在 <see cref="IUIView.OnResume"/> 中、<b>早于 <see cref="EmberUILogic.OnResume"/></b>。
        /// </summary>
        /// <para><b>触发时机：</b>上方遮挡的页面关闭后，当前页面重新回到栈顶。State 从 Paused 恢复为 Opened。</para>
        /// <para><b>职责：</b>框架层在页面恢复时的处理。业务层处理请用 <see cref="EmberUILogic.OnResume"/>。</para>
        protected virtual void OnResumed() { }

        /// <summary>
        /// 已加载页面被重新打开 —— 在 <see cref="IUIView.OnReopen"/> 中调用。
        /// </summary>
        /// <para><b>触发时机：</b>State=Closed 的页面被 <see cref="EmberUIManager.ReopenPage"/> 重新打开时。
        /// 与 <see cref="OnInitialize"/> 互斥：已加载的页面走 OnReopen，不重新走 Init 流程。</para>
        /// <para><b>职责：</b>恢复页面状态。框架随后自动调用 Logic.OnOpen + PlayShow。</para>
        /// <param name="args">重新打开时传入的参数</param>
        protected virtual void OnReopened(object args) { }

        /// <summary>
        /// 返回键处理 —— 在 <see cref="TryEscapeKeyClose"/> 中、<b>子页面均未处理时</b>最后调用。
        /// </summary>
        /// <para><b>触发时机：</b>用户按 ESC / Android 返回键时，
        /// <see cref="EmberUIManager.HandleEscapeKey"/> 从 TopMost → Popup → MainPage 逐层调用每个页面的 TryEscapeKeyClose。
        /// 每个页面先递归询问自己的 SubPage，若子页面均未处理，才调用此方法。</para>
        /// <para><b>返回值：</b>return true 表示已处理（阻止事件继续向更低层冒泡），return false 表示未处理。</para>
        /// <para><b>典型用法：</b>Popup 页面 override 此方法 return true 关闭自己，实现"按返回键关闭弹窗"。</para>
        protected virtual bool OnEscapeKey() { return false; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private System.Collections.IEnumerator PlayShowRoutine(System.Collections.IEnumerator routine)
        {
            yield return routine;
            CompleteShow();
        }

        private void CompleteShow()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _state = PageState.Opened;

            // 动画真正结束 → 播报事件 + 日志 + 回调
            if (_pageDef != null)
                EmberUIObserver.NotifyOpened(_pageDef, _showArgs);
            EmberDebug.LogEvent(TAG, $"页面已打开: {_pageDef}");
            _onShowComplete?.Invoke();
            _onShowComplete = null;
            _showArgs = null;
        }

        private System.Collections.IEnumerator PlayHideRoutine(System.Collections.IEnumerator routine)
        {
            yield return routine;
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
                EmberUIObserver.NotifyClosed(_pageDef, null);
            EmberDebug.LogCleanup(TAG, $"页面已关闭: {_pageDef}");
            _onHideComplete?.Invoke();
            _onHideComplete = null;
        }

        /// <summary>
        /// 预设渐入动画（UniTask）—— CanvasGroup alpha 从 0 线性过渡到 1。
        /// 仅当 <see cref="_usePresetFade"/> 为 true 时由 <see cref="PlayShow"/> 调用。
        /// </summary>
        private async UniTask PresetFadeInAsync()
        {
            float elapsed = 0f;
            while (elapsed < _presetFadeInTime)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Clamp01(elapsed / _presetFadeInTime);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            CompleteShow();
        }

        /// <summary>
        /// 预设渐出动画（UniTask）—— CanvasGroup alpha 从 1 线性过渡到 0。
        /// 仅当 <see cref="_usePresetFade"/> 为 true 时由 <see cref="PlayHide"/> 调用。
        /// </summary>
        private async UniTask PresetFadeOutAsync()
        {
            float elapsed = 0f;
            while (elapsed < _presetFadeOutTime)
            {
                elapsed += Time.deltaTime;
                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / _presetFadeOutTime));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            CompleteHide();
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
        public PageDef PageDef
        {
            get => _pageDef;
            internal set => _pageDef = value;
        }

        /// <summary>父页面（SubPage 时非空）</summary>
        public EmberPage ParentPage
        {
            get => _parentPage;
            internal set => _parentPage = value;
        }

        /// <summary>子页面列表</summary>
        public IReadOnlyList<EmberPage> SubPages => _subPages;

        /// <summary>逻辑层实例</summary>
        public EmberUILogic Logic => _logic;

        /// <summary>逻辑层类型全名</summary>
        public string LogicTypeName
        {
            get => _logicTypeName;
            set => _logicTypeName = value;
        }

        /// <summary>注册子页面</summary>
        internal void RegisterSubPage(EmberPage subPage)
        {
            if (!_subPages.Contains(subPage))
                _subPages.Add(subPage);
            subPage._parentPage = this;
        }

        /// <summary>注销子页面</summary>
        internal void UnregisterSubPage(EmberPage subPage)
        {
            _subPages.Remove(subPage);
            _subPagesLinear.Remove(subPage);
            subPage._parentPage = null;
        }

        /// <summary>
        /// 注入预设渐入渐出配置。由 <see cref="EmberUIBindingBridge"/> 在页面创建时调用。
        /// 启用后 <see cref="PlayShow"/> / <see cref="PlayHide"/> 使用 UniTask alpha 渐变，跳过子类 override 的 OnShow/OnHide。
        /// </summary>
        /// <param name="enabled">是否启用预设渐入渐出</param>
        /// <param name="fadeInTime">渐入持续时间（秒）</param>
        /// <param name="fadeOutTime">渐出持续时间（秒）</param>
        public void SetPresetFade(bool enabled, float fadeInTime, float fadeOutTime)
        {
            _usePresetFade = enabled;
            _presetFadeInTime = fadeInTime;
            _presetFadeOutTime = fadeOutTime;
        }

        /// <summary>
        /// 注入打开动画完成回调。由 <see cref="EmberUIManager"/> 在调用 PlayShow 前设置，
        /// 在 <see cref="CompleteShow"/> 中（动画真正结束时）执行。
        /// </summary>
        internal void SetShowCallback(Action onComplete, object args)
        {
            _onShowComplete = onComplete;
            _showArgs = args;
        }

        /// <summary>
        /// 注入关闭动画完成回调。由 <see cref="EmberUIManager"/> 在调用 PlayHide 前设置，
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
