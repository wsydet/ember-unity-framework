// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// 页面上下文管理器 —— 管理 MainPage 栈 + 每层 Popup 列表的关系。
    ///
    /// <para>核心概念：</para>
    /// <list type="bullet">
    ///   <item><b>MainPage 栈</b>：全屏页面按打开顺序堆叠，新的替换旧的显示</item>
    ///   <item><b>Popup 列表</b>：每个 MainPage 维护自己的 Popup 列表</item>
    ///   <item><b>SortingOrder 自动计算</b>：MainPageOrder=1000, PageGrowStep=500, TopMostOrder=25000, FreePageOrder=30000（FreePage 需显式指定）</item>
    ///   <item><b>HideLowerPage</b>：Popup 打开时隐藏下方页面（不销毁），关闭时恢复</item>
    /// </list>
    /// </summary>
    public class EUIPageContext
    {
        #region 内部参数

        private const string TAG = LogTags.UIManager;

        private const int MainPageBaseOrder  = 1000;
        private const int PageGrowStep       = 500;
        private const int TopMostBaseOrder   = 25000;

        private readonly List<StackEntry> _mainPageStack = new List<StackEntry>();
        private readonly List<EUIPage> _topMostList   = new List<EUIPage>();
        private readonly List<EUIPage> _overlayList   = new List<EUIPage>();
        private EUIPage _backgroundPage;

        private readonly EUIViewEngine _uiManager;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        internal class StackEntry
        {
            public EUIPage Page;
            public int SortingOrder;
            public readonly List<EUIPage> Popups = new List<EUIPage>();
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public EUIPageContext(EUIViewEngine uiManager)
        {
            _uiManager = uiManager;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        // ── MainPage ──

        /// <summary>当前 MainPage 数量</summary>
        public int MainPageCount => _mainPageStack.Count;

        /// <summary>当前活动的 MainPage（栈顶）</summary>
        public EUIPage CurrentMainPage => _mainPageStack.Count > 0 ? _mainPageStack[_mainPageStack.Count - 1].Page : null;

        /// <summary>
        /// 将 MainPage 压入栈。暂停旧 MainPage，新页面显示。
        /// </summary>
        public void PushMainPage(EUIPage page)
        {
            // 暂停当前 MainPage
            if (_mainPageStack.Count > 0)
            {
                var current = _mainPageStack[_mainPageStack.Count - 1];
                _uiManager.EnqueuePageOperation(() =>
                {
                    ((IEUIView)current.Page).OnPause();
                    EUIObserver.NotifyPaused(current.Page.EUIPageDef);
                });

                // 隐藏当前 MainPage 的 Popup
                foreach (var popup in current.Popups)
                {
                    _uiManager.EnqueuePageOperation(() => ((IEUIView)popup).OnPause());
                }
            }

            // 新建 StackEntry
            var entry = new StackEntry
            {
                Page = page,
                SortingOrder = _mainPageStack.Count == 0
                    ? MainPageBaseOrder
                    : _mainPageStack[_mainPageStack.Count - 1].SortingOrder + PageGrowStep,
            };

            _mainPageStack.Add(entry);

            // 设置 Canvas sortingOrder
            var canvas = page.Canvas;
            if (canvas)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = entry.SortingOrder;
            }
        }

        /// <summary>
        /// 将 MainPage 从路由栈移除，返回退出过渡完成后需恢复的上一个 MainPage。
        /// 恢复动作必须由 <see cref="ResumeMainPageAfterClose"/> 在关闭过渡结束后执行。
        /// </summary>
        public EUIPage PopMainPage(EUIPage page)
        {
            var index = FindMainPageIndex(page);
            if (index < 0) return null;

            _mainPageStack.RemoveAt(index);

            return index > 0 && index - 1 < _mainPageStack.Count
                ? _mainPageStack[index - 1].Page
                : null;
        }

        /// <summary>获取指定 MainPage 当前持有的 Popup 快照源。调用方在遍历关闭前应自行复制。</summary>
        public IReadOnlyList<EUIPage> GetPopups(EUIPage mainPage)
        {
            int index = FindMainPageIndex(mainPage);
            return index >= 0 ? _mainPageStack[index].Popups : Array.Empty<EUIPage>();
        }

        /// <summary>关闭过渡完成后恢复上一个 MainPage 及其 Popup。</summary>
        public void ResumeMainPageAfterClose(EUIPage mainPage)
        {
            int index = FindMainPageIndex(mainPage);
            if (index < 0) return;

            var entry = _mainPageStack[index];
            _uiManager.EnqueuePageOperation(() =>
            {
                ((IEUIView)entry.Page).OnResume();
                EUIObserver.NotifyResumed(entry.Page.EUIPageDef);
            });

            foreach (var popup in entry.Popups)
            {
                var capturedPopup = popup;
                _uiManager.EnqueuePageOperation(() =>
                {
                    ((IEUIView)capturedPopup).OnResume();
                    EUIObserver.NotifyResumed(capturedPopup.EUIPageDef);
                });
            }
        }

        // ── Popup ──

        /// <summary>在当前 MainPage 上添加一个 Popup</summary>
        public void AddPopup(EUIPage popup)
        {
            if (_mainPageStack.Count == 0) return;

            var current = _mainPageStack[_mainPageStack.Count - 1];

            // 全屏弹窗标记：完全遮盖下层时才隐藏下方页面（推裁剪面远端）；
            // 普通弹窗不隐藏下层——下层保持渲染，弹窗四周露出的内容可见，靠遮罩拦截交互。
            bool fullScreen = popup.EUIPageDef != null && popup.EUIPageDef.IsFullScreen;

            // 如果之前没有 Popup：全屏弹窗隐藏 MainPage
            if (current.Popups.Count == 0)
            {
                if (fullScreen)
                    SetPageVisible(current.Page, false);
            }
            // 已有 Popup：全屏弹窗隐藏前一个 Popup（OnPause 语义保持——非全屏弹窗叠加也通知遮挡）
            else
            {
                var prevPopup = current.Popups[current.Popups.Count - 1];
                _uiManager.EnqueuePageOperation(() => ((IEUIView)prevPopup).OnPause());
                if (fullScreen)
                    SetPageVisible(prevPopup, false);
            }

            current.Popups.Add(popup);

            // 设置 Popup 的 SortingOrder
            var canvas = popup.Canvas;
            if (canvas)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = current.SortingOrder + current.Popups.Count * PageGrowStep;
            }
        }

        /// <summary>
        /// 将 Popup 从路由列表移除，返回退出过渡完成后需恢复的下层页面。
        /// 本方法不立即改变下层可见性或触发 OnResume。
        /// </summary>
        public EUIPage RemovePopup(EUIPage popup)
        {
            foreach (var entry in _mainPageStack)
            {
                if (entry.Popups.Remove(popup))
                {
                    return entry.Popups.Count > 0
                        ? entry.Popups[entry.Popups.Count - 1]
                        : entry.Page;
                }
            }

            return null;
        }

        /// <summary>Popup 关闭过渡完成后，恢复下层页面可见性与生命周期。</summary>
        public void ResumePageAfterPopupClose(EUIPage page)
        {
            if (page == null || !ContainsRoutedPage(page)) return;

            SetPageVisible(page, true);
            _uiManager.EnqueuePageOperation(() =>
            {
                ((IEUIView)page).OnResume();
                EUIObserver.NotifyResumed(page.EUIPageDef);
            });
        }

        /// <summary>页面是否仍在 MainPage / Popup 路由中，防止批量关闭时恢复已移除的下层页面。</summary>
        private bool ContainsRoutedPage(EUIPage page)
        {
            foreach (var entry in _mainPageStack)
            {
                if (entry.Page == page || entry.Popups.Contains(page))
                    return true;
            }

            return false;
        }

        /// <summary>获取指定 MainPage 上的 Popup 数量</summary>
        public int GetPopupCount(EUIPage mainPage = null)
        {
            var entry = mainPage != null
                ? _mainPageStack.Find(e => e.Page == mainPage)
                : (_mainPageStack.Count > 0 ? _mainPageStack[_mainPageStack.Count - 1] : null);

            return entry?.Popups.Count ?? 0;
        }

        /// <summary>是否有 Popup 在显示</summary>
        public bool HasPopup()
        {
            return _mainPageStack.Count > 0 && _mainPageStack[_mainPageStack.Count - 1].Popups.Count > 0;
        }

        /// <summary>获取最顶层 Popup</summary>
        public EUIPage GetTopPopup()
        {
            if (_mainPageStack.Count == 0) return null;
            var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
            return popups.Count > 0 ? popups[popups.Count - 1] : null;
        }

        // ── TopMost ──

        /// <summary>添加置顶层页面</summary>
        public void AddTopMost(EUIPage page)
        {
            _topMostList.Add(page);
            var order = TopMostBaseOrder + _topMostList.Count * PageGrowStep;
            var canvas = page.Canvas;
            if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = order; }
        }

        /// <summary>移除置顶层页面</summary>
        public void RemoveTopMost(EUIPage page)
        {
            _topMostList.Remove(page);
        }

        /// <summary>最顶层 TopMost 页面</summary>
        public EUIPage GetTopTopMost()
        {
            return _topMostList.Count > 0 ? _topMostList[_topMostList.Count - 1] : null;
        }

        /// <summary>按 prefab 路径查找 TopMost 页面</summary>
        public EUIPage FindTopMostByPath(string prefabPath)
        {
            foreach (var p in _topMostList)
                if (p.EUIPageDef?.PrefabPath == prefabPath) return p;
            return null;
        }

        /// <summary>按 prefab 路径查找 MainPage</summary>
        public EUIPage FindMainPageByPath(string prefabPath)
        {
            foreach (var entry in _mainPageStack)
                if (entry.Page.EUIPageDef?.PrefabPath == prefabPath) return entry.Page;
            return null;
        }

        // ── Overlay ──

        public void AddOverlay(EUIPage page)
        {
            _overlayList.Add(page);
            // 设置 Overlay 排序：优先使用 EUIPageDef.OverlaySortingOrder，否则用 Layer 值
            var canvas = page.Canvas;
            if (canvas)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = page.EUIPageDef?.OverlaySortingOrder ?? page.EUIPageDef?.Layer ?? 0;
            }
        }

        public void RemoveOverlay(EUIPage page) => _overlayList.Remove(page);

        // ── FreePage ──

        private const int FreePageBaseOrder = 30000;
        private readonly List<EUIPage> _freePageList = new();

        /// <summary>添加独立页面（高于 TopMost，不参与栈管理）。排序取页面显式指定的 FreePageSortingOrder，未指定则回退到 FreePageBaseOrder 并警告。</summary>
        public void AddFreePage(EUIPage page)
        {
            _freePageList.Add(page);

            // FreePage 数量少，排序由页面显式指定（对标 Burner ShowFreePage(prefabName, sortingOrder)）
            var explicitOrder = page.EUIPageDef?.FreePageSortingOrder;
            var order = explicitOrder ?? FreePageBaseOrder;
            if (!explicitOrder.HasValue)
                EmberDebug.LogWarning(TAG, $"FreePage 未指定固定 sortingOrder，回退到 {FreePageBaseOrder}: {page.EUIPageDef}");

            var canvas = page.Canvas;
            if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = order; }
        }

        /// <summary>移除独立页面</summary>
        public void RemoveFreePage(EUIPage page) => _freePageList.Remove(page);

        // ── Background ──

        /// <summary>当前背景页（单例）</summary>
        public EUIPage BackgroundPage => _backgroundPage;

        /// <summary>
        /// 设置背景页。单槽位：新的替换旧的，sortingOrder 固定为 0。
        /// </summary>
        public void SetBackground(EUIPage page)
        {
            if (_backgroundPage != null)
                _uiManager.ClosePageInternal(_backgroundPage);

            _backgroundPage = page;
            var canvas = page.Canvas;
            if (canvas)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 0;
            }
        }

        /// <summary>移除背景页。</summary>
        public void ClearBackground()
        {
            if (_backgroundPage != null)
            {
                _uiManager.ClosePageInternal(_backgroundPage);
                _backgroundPage = null;
            }
        }

        // ── 查询 ──

        /// <summary>按层级从高到低遍历所有可见页面，调用 action，返回 true 停止遍历</summary>
        public void ForEachVisiblePage(Func<EUIPage, bool> action)
        {
            // FreePage（后进先处理，最高优先级）
            for (int i = _freePageList.Count - 1; i >= 0; i--)
            {
                if (((IEUIView)_freePageList[i]).IsOpened && action(_freePageList[i]))
                    return;
            }

            // TopMost（后进先处理）
            for (int i = _topMostList.Count - 1; i >= 0; i--)
            {
                if (((IEUIView)_topMostList[i]).IsOpened && action(_topMostList[i]))
                    return;
            }

            // Popup（后进先处理）
            if (_mainPageStack.Count > 0)
            {
                var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
                for (int i = popups.Count - 1; i >= 0; i--)
                {
                    if (((IEUIView)popups[i]).IsOpened && action(popups[i]))
                        return;
                }
            }

            // MainPage
            for (int i = _mainPageStack.Count - 1; i >= 0; i--)
            {
                if (((IEUIView)_mainPageStack[i].Page).IsOpened && action(_mainPageStack[i].Page))
                    return;
            }

            // Background（最低优先级，最后遍历）
            if (_backgroundPage != null && ((IEUIView)_backgroundPage).IsOpened)
                action(_backgroundPage);
        }

        /// <summary>
        /// 按 PrefabPath 查找当前已显示（Opened/Paused/ViewHidden）的页面。
        /// 用于「已显示页面再次 Show → 数据刷新」（G1）与视图级隐藏/恢复（G4）。
        /// 覆盖 FreePage / TopMost / Overlay / Popup / MainPage / Background（SubPage 归属父页面，不在此扫描）。
        /// </summary>
        public EUIPage FindOpenedPage(EUIPageDef pageDef)
        {
            if (pageDef == null || string.IsNullOrEmpty(pageDef.PrefabPath)) return null;

            EUIPage Match(EUIPage p)
            {
                if (p == null || p.EUIPageDef == null) return null;
                if (p.EUIPageDef.PrefabPath != pageDef.PrefabPath) return null;
                var s = p.State;
                return s == PageState.Opened || s == PageState.Paused || s == PageState.ViewHidden ? p : null;
            }

            for (int i = _freePageList.Count - 1; i >= 0; i--) { var r = Match(_freePageList[i]); if (r != null) return r; }
            for (int i = _topMostList.Count - 1; i >= 0; i--) { var r = Match(_topMostList[i]); if (r != null) return r; }
            for (int i = _overlayList.Count - 1; i >= 0; i--) { var r = Match(_overlayList[i]); if (r != null) return r; }
            if (_mainPageStack.Count > 0)
            {
                var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
                for (int i = popups.Count - 1; i >= 0; i--) { var r = Match(popups[i]); if (r != null) return r; }
            }
            for (int i = _mainPageStack.Count - 1; i >= 0; i--) { var r = Match(_mainPageStack[i].Page); if (r != null) return r; }
            return Match(_backgroundPage);
        }

        /// <summary>关闭所有页面</summary>
        public void CloseAll()
        {
            // 关闭 FreePage
            for (int i = _freePageList.Count - 1; i >= 0; i--)
            {
                _uiManager.ClosePageInternal(_freePageList[i]);
            }
            _freePageList.Clear();

            // 关闭 TopMost
            for (int i = _topMostList.Count - 1; i >= 0; i--)
            {
                _uiManager.ClosePageInternal(_topMostList[i]);
            }
            _topMostList.Clear();

            // 关闭 Overlay
            for (int i = _overlayList.Count - 1; i >= 0; i--)
            {
                _uiManager.ClosePageInternal(_overlayList[i]);
            }
            _overlayList.Clear();

            // 关闭 Background
            ClearBackground();

            // 关闭所有 MainPage（含 Popup）
            for (int i = _mainPageStack.Count - 1; i >= 0; i--)
            {
                var entry = _mainPageStack[i];
                for (int j = entry.Popups.Count - 1; j >= 0; j--)
                {
                    _uiManager.ClosePageInternal(entry.Popups[j]);
                }
                entry.Popups.Clear();
                _uiManager.ClosePageInternal(entry.Page);
            }
            _mainPageStack.Clear();

            EUIObserver.NotifyAllClosed();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private int FindMainPageIndex(EUIPage page)
        {
            for (int i = 0; i < _mainPageStack.Count; i++)
            {
                if (_mainPageStack[i].Page == page) return i;
            }
            return -1;
        }

        /// <summary>
        /// 切换页面可见性。
        /// 隐藏时用 planeDistance=100000 推到相机远裁面之外，避免 Canvas 仍参与 Rebuild。
        /// 对标 Burner GamePage.SetActive 中的 planeDistance 裁剪逻辑。
        /// </summary>
        private static void SetPageVisible(EUIPage page, bool visible)
        {
            // CanvasGroup alpha 控制（兼容无 Canvas 的页面）
            var cg = page.CanvasGroup;
            if (cg)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = visible;
                cg.interactable = visible;
            }

            // planeDistance 裁剪（对标 Burner）
            var canvas = page.Canvas;
            if (canvas && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (visible)
                {
                    canvas.planeDistance = page.EUIPageDef != null
                        ? page.EUIPageDef.Layer
                        : 100f;
                }
                else
                {
                    canvas.planeDistance = 100000f; // 推到远裁面
                }
            }
        }

        #endregion
    }
}
