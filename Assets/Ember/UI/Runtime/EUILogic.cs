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

        /// <summary>控件引用字典（由框架在 OnBind 前填充）</summary>
        public Dictionary<string, Component> ControlMap { get; set; }

        /// <summary>所属 EUIPage（MonoBehaviour）</summary>
        public EUIPage Page { get; set; }

        /// <summary>自定义页面参数（由 EUIBindingBridge 从 EUIBinding._pageSettings 注入）</summary>
        public object CustomSettings { get; set; }

        /// <summary>是否需要每帧 Update（默认 false）</summary>
        public bool NeedUpdate { get; set; }

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
        // 注意：EUILogic 的所有钩子都是纯业务逻辑，不要在这里写动画。
        // 动画写在 EUIPage 子类的 OnShow() / OnHide() virtual 方法中，
        // 或者在 Inspector 中勾选 UIBinding 的"启用预设渐入渐出"。
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 绑定 UI 控件引用 —— 在整个生命周期中<b>最早调用且仅调用一次</b>。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.CreateLogic"/> 中，ControlMap 填充完毕后立即调用。</para>
        /// <para><b>在这里做：</b>从 ControlMap 取出控件引用赋值给私有字段。</para>
        /// <para><b>不要在这里做：</b>注册事件、设初始值（那是 <see cref="OnInit"/> 的事）。</para>
        public virtual void OnBind() { }

        /// <summary>
        /// 初始化业务数据 —— 在 <b>OnBind 之后、OnOpen 之前</b>调用，整个生命周期只调用一次。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Init"/> 中，在 OnBind 之后、OnOpen 之前。
        /// 此时 ControlMap 已就绪、控件引用可用，页面尚未可见（α=0）。</para>
        /// <para><b>在这里做：</b>注册按钮事件（onClick.AddListener）、设置初始默认值。</para>
        /// <para><b>不要在这里做：</b>播放动画（那是 <see cref="EUIPage.OnShow()"/> 的事）、
        /// 处理打开参数（那是 <see cref="OnOpen"/> 的事）。</para>
        public virtual void OnInit() { }

        /// <summary>
        /// 页面打开时接收参数 —— 在 <b>OnInit 之后、OnReset 之前</b>调用，每次打开都会触发。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.Init"/> 中 OnInit 之后，
        /// 以及 <see cref="EUIPage.OnReopen"/> 时。</para>
        /// <para><b>在这里做：</b>根据传入参数切换页面状态（如从哪个入口进来的、携带的初始数据）。</para>
        /// <para><b>不要在这里做：</b>注册事件（那是 <see cref="OnInit"/> 的事）、
        /// 播放动画（那是 <see cref="EUIPage.OnShow()"/> 的事）。</para>
        /// <param name="param">打开时传入的参数（来自 EUIManager.ShowMainPage/ShowPopup 的 args）</param>
        public virtual void OnOpen(object param) { }

        /// <summary>
        /// 页面即将可见（业务逻辑） —— 在 <b>PlayShow 阶段、打开动画之前</b>调用。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayShow"/> 中，在调用 <see cref="EUIPage.OnShow()"/>
        /// （打开动画）<b>之前</b>。每次页面从不可见变为可见时都会触发。</para>
        /// <para><b>在这里做：</b>刷新数据（可能已被其他页面修改）、更新 UI 显示。</para>
        /// <para><b>⚠️ 不要在这里写动画！</b>打开动画写在：
        /// <list type="bullet">
        ///   <item><b>预设方式</b>：Inspector 中 UIBinding 勾选"启用预设渐入渐出"，设时间即可</item>
        ///   <item><b>自定义方式</b>：创建 <see cref="EUIPage"/> 子类，override <see cref="EUIPage.OnShow()"/>，
        ///       返回 <c>IEnumerator</c> 协程（非 EUILogic 子类）。参见 <see cref="EUIPage.OnShow()"/> 的文档和示例</item>
        /// </list>
        /// </para>
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
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayHide"/> 中，在调用 <see cref="EUIPage.OnHide()"/>
        /// （关闭动画）<b>之前</b>。</para>
        /// <para><b>在这里做：</b>停止实时刷新、停止音频、准备关闭。</para>
        /// <para><b>⚠️ 不要在这里写动画！</b>关闭动画写在：
        /// <list type="bullet">
        ///   <item><b>预设方式</b>：Inspector 中 UIBinding 勾选"启用预设渐入渐出"，设时间即可</item>
        ///   <item><b>自定义方式</b>：创建 <see cref="EUIPage"/> 子类，override <see cref="EUIPage.OnHide()"/>，
        ///       返回 <c>IEnumerator</c> 协程（非 EUILogic 子类）。参见 <see cref="EUIPage.OnHide()"/> 的文档和示例</item>
        /// </list>
        /// </para>
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
        /// 自定义进入动画 —— 仅当 EUIBinding 的过渡模式设为 Custom 时调用。
        /// </summary>
        /// <para><b>触发时机：</b><see cref="EUIPage.PlayShow"/> 中，<see cref="OnShow"/> 之后。
        /// 此时 CanvasGroup α=0，编写自定义动画（位移、缩放、旋转等）。</para>
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
        /// 自定义退出动画 —— 仅当 EUIBinding 的过渡模式设为 Custom 时调用。
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

        #endregion

        // --------------------------------------------------------

        #region 广播方法（由 EUIPage 调用，内部先调 virtual hook 再递归子 Logic）

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
                    catch (System.Exception ex) { EmberDebug.LogError("EUILogic", $"TrackDisposable 清理异常: {ex}"); }
                }
                _trackedDisposables.Clear();
                _trackedDisposables = null;
            }

            OnDispose();
        }

        internal void BroadcastUpdate()
        {
            if (NeedUpdate)
                OnUpdate();
            foreach (var child in _childLogics)
                child.BroadcastUpdate();
        }

        #endregion
    }
}
