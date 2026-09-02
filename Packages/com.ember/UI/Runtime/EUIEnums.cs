// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ember.UI.Tests")]

namespace Ember.UI
{
    /// <summary>
    /// UI 界面层级预设值。决定界面的渲染顺序和输入优先级。
    /// 值越大，渲染越靠前。也可使用任意 int 值实现更细粒度的层级。
    ///
    /// <para>数值与 <see cref="EUIPageContext"/> 内部排序常量对齐（v0.10.0）：
    /// Normal = MainPageBaseOrder(1000)、TopMost = TopMostBaseOrder(25000)、
    /// Popup 居中（实际 Popup 从 MainPage + PageGrowStep(500) 起，恒落在 Normal 与 TopMost 之间）。
    /// SubPage 在父页排序基础上按 SubPageOrderGrowStep(50) 递增，不占用本枚举预设。</para>
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal     = 1000,
        Popup      = 2000,
        TopMost    = 25000,
    }

    /// <summary>
    /// 页面行为模式。
    /// 与 <see cref="UILayer"/>（渲染排序）正交 —— UILayer 决定渲染层级，
    /// PageType 决定入栈策略和生命周期行为。
    /// </summary>
    public enum PageType
    {
        /// <summary>背景层。单例，始终在最底层（sortingOrder=0），由 MainState 生命周期管理。</summary>
        Background = 0,

        /// <summary>全屏主页面。替换当前 MainPage，压入主栈。</summary>
        MainPage = 1,

        /// <summary>弹窗。叠加在当前 MainPage 之上，不替换，自动创建 BG Mask。</summary>
        Popup = 2,

        /// <summary>全屏弹窗。沿用 Popup 栈与遮罩，并在打开时隐藏下层页面。</summary>
        FullScreenPopup = 7,

        /// <summary>置顶弹窗。高于所有 Popup（如全局提示、Loading 遮罩）。</summary>
        TopMost = 3,

        /// <summary>子页面。嵌入父页面的指定区域（Tab 切换内容等），父关子关。</summary>
        SubPage = 4,

        /// <summary>覆盖层。不受 MainPage/Popup 栈管理（如 Guide Mask、点击特效层）。</summary>
        Overlay = 5,

        /// <summary>独立页面。高于 TopMost，不参与栈管理（如全局设置、帮助界面）。</summary>
        FreePage = 6,
    }

    /// <summary>
    /// 页面生命周期阶段。
    /// </summary>
    public enum PageState
    {
        /// <summary>未加载</summary>
        Unloaded,
        /// <summary>预制体加载中</summary>
        Loading,
        /// <summary>已加载，未显示</summary>
        Loaded,
        /// <summary>播放 Show 动画中</summary>
        Showing,
        /// <summary>已打开，可见且可交互</summary>
        Opened,
        /// <summary>被其他页面遮挡（暂停）</summary>
        Paused,
        /// <summary>播放 Hide 动画中</summary>
        Hiding,
        /// <summary>已关闭（等待销毁/回池）</summary>
        Closed,
        /// <summary>视图级隐藏（仅隐藏不销毁，逻辑存活；与 Opened 之间通过 HideViewOnly/RestoreViewOnly 切换）</summary>
        ViewHidden,
    }
}
