// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ember.UI.Tests")]

namespace Ember.UI
{
    /// <summary>
    /// UI 界面层级预设值。决定界面的渲染顺序和输入优先级。
    /// 值越大，渲染越靠前。也可使用任意 int 值实现更细粒度的层级。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Normal     = 100,
        Popup      = 200,
        TopMost    = 300,
    }

    /// <summary>
    /// 页面行为模式。
    /// 与 <see cref="UILayer"/>（渲染排序）正交 —— UILayer 决定渲染层级，
    /// PageType 决定入栈策略和生命周期行为。
    /// </summary>
    public enum PageType
    {
        /// <summary>全屏主页面。替换当前 MainPage，压入主栈。</summary>
        MainPage,

        /// <summary>弹窗。叠加在当前 MainPage 之上，不替换，自动创建 BG Mask。</summary>
        Popup,

        /// <summary>置顶弹窗。高于所有 Popup（如全局提示、Loading 遮罩）。</summary>
        TopMost,

        /// <summary>子页面。嵌入父页面的指定区域（Tab 切换内容等），父关子关。</summary>
        SubPage,

        /// <summary>覆盖层。不受 MainPage/Popup 栈管理（如 Guide Mask、点击特效层）。</summary>
        Overlay,
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
    }
}
