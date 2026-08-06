// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using Sirenix.OdinInspector;

using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// UI 页面基类。
    /// 所有 UI 页面继承此类，获得完整的生命周期管理、调试面板、CanvasGroup 控制。
    ///
    /// <para>生命周期（两阶段）：</para>
    /// <code>
    /// Init → PlayShow → [Opened] → OnPause / OnResume → PlayHide → Cleanup
    /// </code>
    ///
    /// <para>子类只需关注 OnInitialize / OnShow / OnHide / OnCleanup 四个钩子：</para>
    /// <code>
    /// public class UIMainMenu : EmberPage
    /// {
    ///     protected override void OnInitialize(object args) { /* 填数据 */ }
    ///     protected override IEnumerator OnShow() { /* 打开动画, yield return */ }
    ///     protected override IEnumerator OnHide() { /* 关闭动画, yield return */ }
    ///     protected override void OnCleanup()    { /* 注销事件 */ }
    /// }
    /// </code>
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class EmberPage : MonoBehaviour, IUIView
    {
        private const string TAG = LogTags.UIManager;

        #region 编辑器面板参数

        [FoldoutGroup("调试面板", VisibleIf = "@UnityEngine.Application.isEditor")]
        [SerializeField]
        [LabelText("页面名称")]
        [Tooltip("仅用于调试显示")]
        private string _debugPageName;

        [FoldoutGroup("调试面板", VisibleIf = "@UnityEngine.Application.isEditor")]
        [Button("在 Scene 中预览", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        [EnableIf("@UnityEngine.Application.isPlaying == false")]
        private void DebugOpenSelf()
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            gameObject.SetActive(true);
            ((IUIView)this).Init(null);
            ((IUIView)this).PlayShow();
            _state = PageState.Opened;
        }

        [FoldoutGroup("调试面板", VisibleIf = "@UnityEngine.Application.isEditor")]
        [Button("隐藏此页面", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        [EnableIf("@UnityEngine.Application.isPlaying == false")]
        private void DebugCloseSelf()
        {
            gameObject.SetActive(false);
            _state = PageState.Unloaded;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private PageState _state = PageState.Unloaded;
        private PageDef _pageDef;
        private EmberPage _parentPage;
        private readonly List<EmberPage> _subPages = new List<EmberPage>();
        private CompositeDisposable _subscriptions;

        #endregion

        // --------------------------------------------------------

        #region 生命周期（MonoBehaviour）

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _subscriptions = new CompositeDisposable();
        }

        protected virtual void OnDestroy()
        {
            _subscriptions?.Dispose();
        }

        #endregion

        // --------------------------------------------------------

        #region IUIView 实现（密封——子类不能 override）

        /// <inheritdoc />
        void IUIView.Init(object args)
        {
            if (_state != PageState.Unloaded && _state != PageState.Closed)
            {
                EmberDebug.LogWarning(TAG, $"EmberPage.Init: '{name}' state={_state}, expected Unloaded/Closed.");
                return;
            }

            _state = PageState.Loaded;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(true);

            try
            {
                OnInitialize(args);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"EmberPage.Init '{name}' error: {ex}");
            }
        }

        /// <inheritdoc />
        void IUIView.PlayShow()
        {
            if (_state != PageState.Loaded)
            {
                EmberDebug.LogWarning(TAG, $"EmberPage.PlayShow: '{name}' state={_state}, expected Loaded.");
                return;
            }

            _state = PageState.Showing;

            // 如果子类返回 null（无动画），直接完成
            var routine = OnShow();
            if (routine == null)
            {
                CompleteShow();
            }
            else
            {
                StartCoroutine(PlayShowRoutine(routine));
            }
        }

        /// <inheritdoc />
        void IUIView.OnPause()
        {
            if (_state != PageState.Opened) return;
            _state = PageState.Paused;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            OnPaused();
        }

        /// <inheritdoc />
        void IUIView.OnResume()
        {
            if (_state != PageState.Paused) return;
            _state = PageState.Opened;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            OnResumed();
        }

        /// <inheritdoc />
        void IUIView.OnReopen(object args)
        {
            if (_state != PageState.Closed) return;
            _state = PageState.Loaded;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);
            OnReopened(args);

            ((IUIView)this).PlayShow();
        }

        /// <inheritdoc />
        void IUIView.PlayHide()
        {
            if (_state == PageState.Unloaded || _state == PageState.Closed)
                return;

            _state = PageState.Hiding;

            var routine = OnHide();
            if (routine == null)
            {
                CompleteHide();
            }
            else
            {
                StartCoroutine(PlayHideRoutine(routine));
            }
        }

        /// <inheritdoc />
        void IUIView.Cleanup()
        {
            if (_state == PageState.Unloaded) return;
            _state = PageState.Unloaded;

            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable();

            try
            {
                OnCleanup();
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"EmberPage.Cleanup '{name}' error: {ex}");
            }

            // 清理子页面
            foreach (var sub in _subPages)
            {
                if (sub != null && sub.gameObject != null)
                    Destroy(sub.gameObject);
            }
            _subPages.Clear();

            gameObject.SetActive(false);
        }

        /// <inheritdoc />
        bool IUIView.TryEscapeKeyClose()
        {
            // 先问子页面
            foreach (var sub in _subPages)
            {
                if (sub != null && ((IUIView)sub).TryEscapeKeyClose())
                    return true;
            }
            return OnEscapeKey();
        }

        /// <inheritdoc />
        bool IUIView.IsInitialized => _state >= PageState.Loaded;

        /// <inheritdoc />
        bool IUIView.IsOpened => _state == PageState.Opened;

        /// <inheritdoc />
        PageState IUIView.State => _state;

        #endregion

        // --------------------------------------------------------

        #region 子类可 override 的钩子（虚方法）

        /// <summary>初始化数据。只做数据操作，不要做动画。</summary>
        protected virtual void OnInitialize(object args) { }

        /// <summary>打开动画。返回 null 表示无动画。如果不为 null，框架通过协程等待完成后标记 Opened。</summary>
        protected virtual System.Collections.IEnumerator OnShow() { return null; }

        /// <summary>关闭动画。返回 null 表示无动画。</summary>
        protected virtual System.Collections.IEnumerator OnHide() { return null; }

        /// <summary>清理。注销事件、释放引用。</summary>
        protected virtual void OnCleanup() { }

        /// <summary>被遮挡时回调。</summary>
        protected virtual void OnPaused() { }

        /// <summary>重新可见时回调。</summary>
        protected virtual void OnResumed() { }

        /// <summary>已加载页面被重新打开。</summary>
        protected virtual void OnReopened(object args) { }

        /// <summary>返回键处理。返回 true 表示已消费（阻止冒泡）。默认返回 false。</summary>
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
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>页面名称（调试用）</summary>
        public string PageName
        {
            get => string.IsNullOrEmpty(_debugPageName) ? name : _debugPageName;
            set => _debugPageName = value;
        }

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

        /// <summary>CanvasGroup 引用</summary>
        public CanvasGroup CanvasGroup => _canvasGroup;

        /// <summary>RectTransform 引用</summary>
        public RectTransform RectTransform => _rectTransform;

        /// <summary>UniRx CompositeDisposable，绑定到此页面的生命周期</summary>
        public CompositeDisposable Subscriptions => _subscriptions;

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
            subPage._parentPage = null;
        }

        #endregion
    }
}
