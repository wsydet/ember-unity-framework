// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using Ember.Basic;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 逻辑层基类 —— 非 MonoBehaviour 的纯 C# 类。
    /// 每个 EUIPage 持有一个 EUILogic 实例，负责所有业务逻辑。
    /// MonoBehaviour 生命周期由 EUIPage 桥接到此类。
    ///
    /// <para>架构：</para>
    /// <code>
    /// EUIPage (MonoBehaviour on prefab)   ← 表现层（框架管理）
    ///   └── EUILogic (plain class)      ← 逻辑层（生成的代码 + 手写逻辑）
    ///         ├── ControlMap                ← 控件引用字典（框架填充）
    ///         └── OnBind / OnInit / OnOpen / OnShow / OnHide / OnClose / OnDispose
    /// </code>
    ///
    /// <para>生成的代码示例：</para>
    /// <code>
    /// public partial class UIMainMenu : EUILogic
    /// {
    ///     private Button _btnStart;
    ///
    ///     public override void OnBind()
    ///     {
    ///         base.OnBind();
    ///         _btnStart = ControlMap["_btnStart"] as Button;
    ///     }
    ///
    ///     public override void OnInit()
    ///     {
    ///         _btnStart.onClick.AddListener(() =&gt; OnClickStart());
    ///     }
    /// }
    /// </code>
    /// </summary>
    public class EUILogic
    {
        #region 内部参数

        private const string TAG = LogTags.UIManager;

        /// <summary>控件引用字典（由框架在 OnBind 前填充）</summary>
        public Dictionary<string, Component> ControlMap { get; set; }

        /// <summary>所属 EUIPage（MonoBehaviour）</summary>
        public EUIPage Page { get; set; }

        /// <summary>自定义页面参数（由 EUIBindingBridge 从 EUIBinding._pageSettings 注入）</summary>
        public object CustomSettings { get; set; }

        /// <summary>
        /// 是否需要每帧 Update（默认 false）。
        /// </summary>
        /// <para><b>两种开启方式：</b>
        /// <list type="bullet">
        ///   <item><b>静态开启</b>：<c>public override bool NeedUpdate =&gt; true;</c>（页面始终逐帧更新，对标 Burner 风格）；</item>
        ///   <item><b>动态开关</b>：子类内 <c>NeedUpdate = true / false;</c>（protected setter，如 Loading 只在假进度期间开启）。</item>
        /// </list>
        /// </para>
        /// <para><b>注意：</b>override 成 get-only 的页面不能再动态 set；
        /// 既要动态开关又要 override 的页面可 override 到私有字段：
        /// <c>private bool _x; public override bool NeedUpdate =&gt; _x;</c></para>
        public virtual bool NeedUpdate
        {
            get => _needUpdate;
            protected set => _needUpdate = value;
        }

        private bool _needUpdate;

        /// <summary>
        /// Loading 过渡就绪标志。Loading 页面 override 为 true 表示假进度已完成。
        /// <see cref="EUIManager.TransitionSceneWithLoading"/> 轮询此属性。
        /// </summary>
        public virtual bool IsTransitionReady => true;

        /// <summary>
        /// 跳过假进度（快速转场用）。框架/业务在显示 Loading 后置 true：
        /// Loading 逻辑应据此跳过假进度与进度显示，<see cref="IsTransitionReady"/> 直接以真实加载进度为准。
        /// </summary>
        public bool SkipFakeProgress { get; set; }

        // ── 安全区便捷访问（对标 Burner HasSafeArea / SafeAreaRoot） ──

        /// <summary>
        /// 页面是否挂有安全区组件且存在有效安全区。
        /// 懒加载发现（<c>GetComponentInChildren《IEmberSafeAreaProvider》</c> 仅执行一次并缓存）。
        /// </summary>
        public bool HasSafeArea => SafeAreaProvider?.HasSafeArea ?? false;

        /// <summary>
        /// 安全区内容容器（页面内 EUISafeArea 组件所在的 RectTransform）。
        /// 页面未挂安全区组件时返回 null。
        /// </summary>
        public RectTransform SafeAreaRoot => SafeAreaProvider?.SafeAreaRoot;

        private IEmberSafeAreaProvider _safeAreaProvider;
        private bool _safeAreaSearched;

        private IEmberSafeAreaProvider SafeAreaProvider
        {
            get
            {
                if (!_safeAreaSearched)
                {
                    _safeAreaSearched = true;
                    if (Page != null)
                    {
                        _safeAreaProvider = Page.GameObject.GetComponentInChildren<IEmberSafeAreaProvider>(true);
                        // 框架自动订阅安全区变化（对标 Burner：基类注册 SafeAreaChanged，子类覆写 OnSafeAreaChanged）
                        if (_safeAreaProvider != null)
                            _safeAreaProvider.SafeAreaChanged += OnSafeAreaChanged;
                    }
                }
                return _safeAreaProvider;
            }
        }

        /// <summary>
        /// 注册可销毁对象（IDisposable），在 <see cref="OnDispose"/> 时自动清理。
        /// 适用于 EmberEventBus.Subscribe 返回值、UniRx 订阅等。
        /// 对标 Burner GameUIBase.AddEvent + RemoveAllEvents。
        /// </summary>
        /// <example>
        /// <code>
        /// public override void OnInit()
        /// {
        ///     TrackDisposable(EmberEventBus.On(MyEvents.Foo, OnFoo));
        ///     TrackDisposable(Observable.EveryUpdate().Subscribe(_ => Tick()));
        /// }
        /// // OnDispose 中无需手动注销——框架自动清理
        /// </code>
        /// </example>
        public void TrackDisposable(System.IDisposable disposable)
        {
            if (disposable == null) return;
            if (_trackedDisposables == null)
                _trackedDisposables = new List<System.IDisposable>();
            _trackedDisposables.Add(disposable);
        }

        private List<System.IDisposable> _trackedDisposables;

        // 嵌套子 Logic（对标 Burner behaviours 列表，子 UIBinding 独立管理）
        private readonly List<EUILogic> _childLogics = new();

        #endregion

        // --------------------------------------------------------

        #region 子 Logic 管理（嵌套 UIBinding）

        /// <summary>注册子 Logic（由 EUIBindingBridge 在发现嵌套 UIBinding 时调用）</summary>
        public void RegisterChildLogic(EUILogic child)
        {
            if (child != null && !_childLogics.Contains(child))
                _childLogics.Add(child);
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期钩子（子类 override）

        // ═══════════════════════════════════════════════════════════
        // 注意：EUILogic 的基础生命周期钩子只处理业务逻辑，不要在这里写动画。
        // 普通 UI 在 EUIBinding 中选择唯一过渡模式；Custom 模式才使用 OnCustomEnter/OnCustomExit。
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 开始加载 —— 在整个生命周期中<b>最早调用且仅调用一次</b>（对标 Burner OnBeginLoad）。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.CreateLogic"/> 中，Logic 实例刚创建、<see cref="OnBind"/> 之前。</para>
        /// <para><b>在这里做：</b>加载开始前的轻量准备（如记录时间、设置加载态）。</para>
        public virtual void OnBeginLoad() { }

        /// <summary>
        /// 绑定 UI 控件引用 —— 在整个生命周期中<b>最早调用且仅调用一次</b>。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.CreateLogic"/> 中，ControlMap 填充完毕后立即调用。</para>
        /// <para><b>在这里做：</b>从 ControlMap 取出控件引用赋值给私有字段。</para>
        /// <para><b>不要在这里做：</b>注册事件、设初始值（那是 <see cref="OnInit"/> 的事）。</para>
        public virtual void OnBind() { }

        /// <summary>
        /// 预加载 —— <b>预加载时调用，先于 OnInit</b>（对标 Burner OnPreload）。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Preload"/>（EUIManager.PreloadPage）时。
        /// OnInit/OnOpen 不会在预加载时执行，而是延后到页面真正打开时。</para>
        /// <para><b>在这里做：</b>预加载阶段的准备工作（预热数据、提前订阅等）。</para>
        /// <param name="param">预加载时传入的参数</param>
        /// <param name="isOpen">是否在打开请求中触发（Ember 当前 PreloadPage 为纯预加载，恒为 false）</param>
        public virtual void OnPreload(object param, bool isOpen) { }

        /// <summary>
        /// 重置为默认状态 —— <b>打开（初始化之前）与关闭（OnClose 之后）各调用一次</b>。
        /// </summary>
        /// <para><b>触发时机：</b>
        /// <list type="number">
        ///   <item><b>打开时</b>：<see cref="EUIPage.Init"/> 中、<see cref="OnInit"/> 之前——保证初始化从干净默认状态开始（绝不依赖上一次运行残留）；</item>
        ///   <item><b>关闭时</b>：<see cref="EUIPage.Cleanup"/> 中、<see cref="OnClose"/> 之后——清理显示状态，为下次复用/干净关闭做准备。</item>
        /// </list>
        /// </para>
        /// <para><b>约定：</b>所有由面板参数驱动的显示开关（进度条、功能开关等）都遵循
        /// 「先恢复默认（OnResetDefault），再按参数打开（OnInit/OnShow）」两步。</para>
        /// <para><b>在这里做：</b>关闭所有按参数显示的 UI 元素、复位内部状态字段到默认值。</para>
        public virtual void OnResetDefault() { }

        /// <summary>
        /// 初始化业务数据 —— 在 <b>OnBind 之后、OnOpen 之前</b>调用，整个生命周期只调用一次。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Init"/> 中，在 <see cref="OnResetDefault"/> 之后、OnOpen 之前。
        /// 此时 ControlMap 已就绪、控件引用可用，页面尚未可见（α=0）。</para>
        /// <para><b>在这里做：</b>注册按钮事件（onClick.AddListener）、设置初始默认值。</para>
        /// <para><b>不要在这里做：</b>播放过渡动画（由 EUIBinding 选定的过渡模式负责）、
        /// 处理打开参数（那是 <see cref="OnOpen"/> 的事）。</para>
        public virtual void OnInit() { }

        /// <summary>
        /// 页面打开时接收参数 —— 在 <b>OnInit 之后、OnReset 之前</b>调用，每次打开都会触发。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Init"/> 中 OnInit 之后，
        /// 以及 <see cref="EUIPage.OnReopen"/> 时。</para>
        /// <para><b>在这里做：</b>根据传入参数切换页面状态（如从哪个入口进来的、携带的初始数据）。</para>
        /// <para><b>不要在这里做：</b>注册事件（那是 <see cref="OnInit"/> 的事）、
        /// 播放过渡动画（由 EUIBinding 选定的过渡模式负责）。</para>
        /// <param name="param">打开时传入的参数（来自 EUIManager.ShowMainPage/ShowPopup 的 args）</param>
        public virtual void OnOpen(object param) { }

        /// <summary>
        /// 已显示页面再次 Show 的数据刷新 —— 页面处于 Opened/Paused/ViewHidden 时再次 Show 触发（对标 Burner OnReopen）。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.OnReopen"/> 的可见分支：不重放 OnOpen/OnShow，仅刷新数据。</para>
        /// <para><b>在这里做：</b>根据新参数刷新页面内容。</para>
        /// <param name="param">再次 Show 时传入的参数</param>
        public virtual void OnReopen(object param) { }

        /// <summary>
        /// 页面即将可见（业务逻辑） —— 在 <b>PlayShow 阶段、打开动画之前</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayShow"/> 中，在打开过渡<b>之前</b>。
        /// 每次页面从不可见变为可见时都会触发。</para>
        /// <para><b>在这里做：</b>刷新数据（可能已被其他页面修改）、更新 UI 显示。</para>
        /// <para><b>⚠️ 不要在这里写动画！</b>预设/Animator 由框架驱动；
        /// 自定义过渡写在 <see cref="OnCustomEnter"/> 中。</para>
        public virtual void OnShow() { }

        /// <summary>
        /// 页面被其他页面遮挡 —— <b>页面不会被销毁</b>，只是暂时不可见。
        /// </summary>
        /// <para><b>触发时机：</b>上方 Push 了新的 MainPage 或 Popup。State: Opened → Paused。</para>
        /// <para><b>在这里做：</b>暂停计时器、关闭实时刷新、静音页面专属音频。</para>
        public virtual void OnPause() { }

        /// <summary>
        /// 页面恢复可见 —— 上方遮挡的页面被关闭。
        /// </summary>
        /// <para><b>触发时机：</b>上方的 Popup 或 MainPage 被 Pop。State: Paused → Opened。</para>
        /// <para><b>在这里做：</b>恢复计时器、重新开启实时刷新、恢复音频。</para>
        public virtual void OnResume() { }

        /// <summary>
        /// 页面即将隐藏（业务逻辑） —— 在 <b>PlayHide 阶段、关闭动画之前</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayHide"/> 中，在关闭过渡<b>之前</b>。</para>
        /// <para><b>在这里做：</b>停止实时刷新、停止音频、准备关闭。</para>
        /// <para><b>⚠️ 不要在这里写动画！</b>预设/Animator 由框架驱动；
        /// 自定义过渡写在 <see cref="OnCustomExit"/> 中。</para>
        public virtual void OnHide() { }

        /// <summary>
        /// 页面关闭 —— 在 <b>Cleanup 阶段、OnReset 之前</b>调用。页面即将被销毁或回池。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Cleanup"/> 中，在 OnReset 和 OnDispose 之前。
        /// 此时 UI 状态还在，可以读取控件当前值。</para>
        /// <para><b>在这里做：</b>持久化用户输入、保存设置、上报统计数据。</para>
        /// <para><b>不要在这里做：</b>注销事件（那是 <see cref="OnDispose"/> 的事）。</para>
        public virtual void OnClose() { }

        /// <summary>
        /// 重置 UI 状态到默认值 —— 在两个时机调用。
        /// <list type="number">
        ///   <item><b>打开时</b>：Init 结束后（OnInit + OnOpen 之后），保证每次打开是干净状态</item>
        ///   <item><b>关闭时</b>：<see cref="OnClose"/> 之后、<see cref="OnDispose"/> 之前，保证下次打开是干净状态</item>
        /// </list>
        /// </summary>
        /// <para><b>在这里做：</b>清空输入框、重置 Toggle/Slider 到默认值、清空临时列表。</para>
        public virtual void OnReset() { }

        /// <summary>
        /// 页面销毁 / 回池 —— <b>最后一次调用</b>，此后 Logic 实例被置 null。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Cleanup"/> 末尾，在 OnClose 和 OnReset 之后。
        /// 此后 GameObject 被 Destroy，Logic 实例被释放。</para>
        /// <para><b>在这里做：</b>注销所有事件（onClick.RemoveListener）、解除引用、清理订阅。</para>
        /// <para><b>⚠️ 这是防止内存泄漏的最后防线，务必在此清理干净。</b></para>
        public virtual void OnDispose() { }

        /// <summary>
        /// 自定义 Update —— 仅在 <see cref="NeedUpdate"/> 设为 true 时每帧调用。
        /// </summary>
        /// <para><b>注意：</b>默认不启用，避免空转开销。需要逐帧更新时在 OnInit 中设置 NeedUpdate = true。</para>
        public virtual void OnUpdate() { }

        /// <summary>自定义 LateUpdate。同样需要 NeedUpdate = true。</summary>
        public virtual void OnLateUpdate() { }

        /// <summary>
        /// 自定义进入过渡 —— 普通 UI 选择 CustomCode 时调用；
        /// Loading 方块特殊链路勾选自定义阶段时也会调用。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayShow"/> 中，<see cref="OnShow"/> 之后。
        /// 普通 UI 此时页面根 CanvasGroup α=1，可安全编写位移、缩放、旋转等动画；
        /// 需要渐入时由本方法自行在首次 await 前将 Alpha 设为 0。</para>
        /// <para><b>返回值：</b>返回 UniTask，框架 await 动画完成后再调 CompleteShow。
        /// 返回 <see cref="UniTask.CompletedTask"/> 则跳过动画直接完成。</para>
        /// <example>
        /// <code>
        /// public override async UniTask OnCustomEnter()
        /// {
        ///     var t = 0f;
        ///     while (t &lt; 0.5f)
        ///     {
        ///         t += Time.deltaTime;
        ///         Page.RectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / 0.5f);
        ///         await UniTask.Yield();
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual UniTask OnCustomEnter() => UniTask.CompletedTask;

        /// <summary>
        /// 自定义退出过渡 —— 调用条件与 <see cref="OnCustomEnter"/> 相同。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayHide"/> 中，<see cref="OnHide"/> 之后。</para>
        /// <para><b>返回值：</b>返回 UniTask，返回 <see cref="UniTask.CompletedTask"/> 则跳过动画直接完成。</para>
        public virtual UniTask OnCustomExit() => UniTask.CompletedTask;

        /// <summary>
        /// 展示进度的平滑收尾时长（秒）。Loading 页面 override 此属性，
        /// <see cref="EUIManager.TransitionSceneWithLoading"/> 在关闭页面前等待此时长。
        /// 默认 0，表示无需额外等待。
        /// </summary>
        public virtual float SmoothTailDuration => 0f;

        /// <summary>
        /// 是否自动创建可点击遮罩（仅 Popup 生效，对标 Burner AutoCreateClickableMask）。
        /// </summary>
        /// <para><b>默认：</b>true —— Popup 打开时框架自动创建半透明遮罩并拦截点击，点击默认关闭本弹窗。</para>
        /// <para><b>override 为 false：</b>不创建遮罩（点击可穿透到下层页面，慎用于非模态场景）。</para>
        protected virtual bool AutoCreateClickableMask => true;

        /// <summary>
        /// 点击遮罩时的回调（仅 Popup 生效，对标 Burner OnClickMask）。
        /// </summary>
        /// <para><b>默认实现：</b>关闭本页面（<see cref="EUIManager.ClosePage(EUIPage, object)"/>）。</para>
        /// <para><b>override 为空方法：</b>保留遮罩拦截，但不允许点击遮罩关闭。</para>
        /// <para><b>override 自定义：</b>替换点击行为；需要关闭时调用 <c>base.OnClickMask()</c>。</para>
        protected virtual void OnClickMask()
        {
            // 数据层开关（EUIBinding.clickMaskToClose=false，注入到 Page.ClickMaskToClose）时默认不关闭；
            // override 本方法自定义行为时优先于此开关（用户代码优先）。
            if (Page == null || !Page.ClickMaskToClose) return;
            if (EUIManager.Instance != null)
                EUIManager.Instance.ClosePage(Page);
        }

        /// <summary>
        /// 安全区变化回调 —— 页面内挂有安全区组件（<see cref="IEmberSafeAreaProvider"/>）时，
        /// 设备旋转/安全区变化后自动调用（框架自动订阅，对标 Burner OnSafeAreaChanged）。
        /// </summary>
        /// <para><b>在这里做：</b>刷新受安全区影响的布局。
        /// 可交互内容是否进安全区是 prefab 责任，逻辑只处理安全区更新后的布局刷新。</para>
        protected virtual void OnSafeAreaChanged() { }

        #endregion

        // --------------------------------------------------------

        #region 广播方法（由 EUIPage 调用，内部先调 virtual hook 再递归子 Logic）

        internal void BroadcastPreload(object args, bool isOpen)
        {
            OnPreload(args, isOpen);
            foreach (var child in _childLogics)
                child.BroadcastPreload(args, isOpen);
        }

        internal void BroadcastResetDefault()
        {
            OnResetDefault();
            foreach (var child in _childLogics)
                child.BroadcastResetDefault();
        }

        internal void BroadcastInit()
        {
            OnInit();
            foreach (var child in _childLogics)
                child.BroadcastInit();
        }

        internal void BroadcastOpen(object args)
        {
            OnOpen(args);
            foreach (var child in _childLogics)
                child.BroadcastOpen(args);
        }

        internal void BroadcastReopen(object args)
        {
            OnReopen(args);
            foreach (var child in _childLogics)
                child.BroadcastReopen(args);
        }

        internal void BroadcastShow()
        {
            OnShow();
            foreach (var child in _childLogics)
                child.BroadcastShow();
        }

        internal void BroadcastPause()
        {
            OnPause();
            foreach (var child in _childLogics)
                child.BroadcastPause();
        }

        internal void BroadcastResume()
        {
            OnResume();
            foreach (var child in _childLogics)
                child.BroadcastResume();
        }

        internal void BroadcastHide()
        {
            OnHide();
            foreach (var child in _childLogics)
                child.BroadcastHide();
        }

        internal void BroadcastClose()
        {
            OnClose();
            foreach (var child in _childLogics)
                child.BroadcastClose();
        }

        internal void BroadcastReset()
        {
            OnReset();
            foreach (var child in _childLogics)
                child.BroadcastReset();
        }

        internal void BroadcastDispose()
        {
            foreach (var child in _childLogics)
                child.BroadcastDispose();
            _childLogics.Clear();

            // 自动清理所有 TrackDisposable 注册的资源（对标 Burner RemoveAllEvents）
            if (_trackedDisposables != null)
            {
                foreach (var d in _trackedDisposables)
                {
                    try { d.Dispose(); }
                    catch (System.Exception ex) { EmberDebug.LogError(TAG, $"TrackDisposable 清理异常: {ex}"); }
                }
                _trackedDisposables.Clear();
                _trackedDisposables = null;
            }

            // 注销安全区事件订阅（与 SafeAreaProvider 发现时的订阅对称；字段复位支持实例复用）
            if (_safeAreaProvider != null)
            {
                _safeAreaProvider.SafeAreaChanged -= OnSafeAreaChanged;
                _safeAreaProvider = null;
            }
            _safeAreaSearched = false;

            OnDispose();
        }

        internal void BroadcastUpdate()
        {
            if (NeedUpdate)
                OnUpdate();
            foreach (var child in _childLogics)
                child.BroadcastUpdate();
        }

        /// <summary>遮罩创建开关（internal 桥，供 EUIManager 读取 protected virtual 配置）</summary>
        internal bool ShouldCreateClickableMask => AutoCreateClickableMask;

        /// <summary>遮罩点击入口（internal 桥，供 EUIManager 转发点击事件到 protected virtual 钩子）</summary>
        internal void NotifyClickMask() => OnClickMask();

        #endregion
    }
}
