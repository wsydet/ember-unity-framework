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
        private const int TOP_MOST_ORDER_LIMIT = 29000; // Guide overlay
        private const int TOP_MOST_ORDER_STEP = 50;
        private const int TOP_MOST_ORDER_CAPACITY =
            (TOP_MOST_ORDER_LIMIT - TopMostBaseOrder) / TOP_MOST_ORDER_STEP - 1;

        private readonly List<StackEntry> _mainPageStack = new List<StackEntry>();
        private readonly List<EUIPage> _topMostList   = new List<EUIPage>();
        private readonly List<EUIPage> _topMostOrders = new();
        private readonly Dictionary<EUIPage, EUIPage> _topMostRoots = new();
        private readonly HashSet<EUIPage> _closingMains = new();
        private readonly HashSet<EUIPage> _mainPaused = new();
        private readonly Dictionary<EUIPage, EUIPage> _fullscreenTargets = new();
        private readonly Dictionary<EUIPage, StackEntry> _closingPopups = new();
        private readonly List<EUIPage> _overlayList   = new List<EUIPage>();
        private EUIPage _backgroundPage;
        private readonly HashSet<EUIPage> _popupPaused = new();

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
                    if (current.Page.State != PageState.Opened || current.Page.CloseRequested) return;
                    _mainPaused.Add(current.Page);
                    current.Page.OnPause();
                    EUIObserver.NotifyPaused(current.Page.EUIPageDef);
                });

                // 隐藏当前 MainPage 的 Popup
                foreach (var popup in current.Popups)
                {
                    _uiManager.EnqueuePageOperation(() => PausePopup(popup));
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

            bool wasCurrent = index == _mainPageStack.Count - 1;
            _mainPageStack.RemoveAt(index);
            _mainPaused.Remove(page);
            if (IsPopupVisible(page)) _closingMains.Add(page);
            return wasCurrent ? CurrentMainPage : null;
        }

        /// <summary>获取指定 MainPage 当前持有的 Popup 快照源。调用方在遍历关闭前应自行复制。</summary>
        public IReadOnlyList<EUIPage> GetPopups(EUIPage mainPage)
        {
            int index = FindMainPageIndex(mainPage);
            return index >= 0 ? _mainPageStack[index].Popups : Array.Empty<EUIPage>();
        }

        internal void FinishMainPageClose(EUIPage page)
        {
            _closingMains.Remove(page);
        }

        /// <summary>关闭过渡完成后恢复仍位于栈顶的 MainPage。</summary>
        public void ResumeMainPageAfterClose(EUIPage mainPage)
        {
            if (mainPage == null) return;
            _uiManager.EnqueuePageOperation(() =>
            {
                if (CurrentMainPage == mainPage && !HasClosingMainAbove(mainPage))
                    ResumeMainIfPaused(mainPage);
            });
        }

        private void ResumeMainIfPaused(EUIPage page)
        {
            if (page.State == PageState.Paused && _mainPaused.Remove(page))
                ResumePausedPage(page);
        }

        private bool HasClosingMainAbove(EUIPage page)
        {
            foreach (var closing in _closingMains)
                if (closing.GameObject && closing.Canvas.sortingOrder > page.Canvas.sortingOrder)
                    return true;
            return false;
        }

        private static void ResumePausedPage(EUIPage page)
        {
            if (page.State != PageState.Paused || page.CloseRequested || page.IsDisposed || !page.GameObject)
                return;
            page.OnResume();
            EUIObserver.NotifyResumed(page.EUIPageDef);
        }

        // ── Popup ──

        /// <summary>在当前 MainPage 上添加一个 Popup。</summary>
        public void AddPopup(EUIPage popup)
        {
            if (_mainPageStack.Count == 0) return;
            var current = _mainPageStack[_mainPageStack.Count - 1];
            int order = current.SortingOrder;
            foreach (var previous in current.Popups)
                if (previous.Canvas) order = Mathf.Max(order, previous.Canvas.sortingOrder);
            foreach (var closing in _closingPopups)
                if (closing.Value == current && closing.Key.Canvas)
                    order = Mathf.Max(order, closing.Key.Canvas.sortingOrder);
            current.Popups.Add(popup);
            if (popup.Canvas)
            {
                popup.Canvas.overrideSorting = true;
                popup.Canvas.sortingOrder = order + PageGrowStep;
            }
        }

        /// <summary>采样完成后由这个全屏 Popup 持有对下层画面的裁剪。</summary>
        internal void CommitFullScreenPopup(EUIPage popup)
        {
            foreach (var entry in _mainPageStack)
            {
                int index = entry.Popups.IndexOf(popup);
                if (index < 0) continue;
                SetFullscreenTarget(popup, FindFullscreenTarget(entry, index));
                return;
            }
        }

        private EUIPage FindFullscreenTarget(StackEntry entry, int index)
        {
            if (entry.Popups[index].State == PageState.ViewHidden) return null;
            for (int i = index - 1; i >= 0; i--)
                if (CanBeCulled(entry.Popups[i])) return entry.Popups[i];
            return CanBeCulled(entry.Page) ? entry.Page : null;
        }

        private static bool CanBeCulled(EUIPage page)
        {
            return page.GameObject && !page.CloseRequested &&
                (page.State == PageState.Opened || page.State == PageState.Paused);
        }

        private void SetFullscreenTarget(EUIPage popup, EUIPage target)
        {
            _fullscreenTargets.TryGetValue(popup, out var previous);
            _fullscreenTargets[popup] = target;
            if (previous == target) return;
            target?.SetFullscreenCovered(true);
            if (previous != null && !_fullscreenTargets.ContainsValue(previous))
                previous.SetFullscreenCovered(false);
        }

        /// <summary>移除路由，保留仍在退场的 Popup 对下层的覆盖。</summary>
        public EUIPage RemovePopup(EUIPage popup)
        {
            foreach (var entry in _mainPageStack)
            {
                if (!entry.Popups.Remove(popup)) continue;
                _popupPaused.Remove(popup);
                if (IsPopupVisible(popup)) _closingPopups.Add(popup, entry);
                return entry.Popups.Count > 0 ? entry.Popups[entry.Popups.Count - 1] : entry.Page;
            }
            return null;
        }

        internal void FinishPopupClose(EUIPage popup)
        {
            _closingPopups.Remove(popup);
            if (_fullscreenTargets.ContainsKey(popup))
            {
                SetFullscreenTarget(popup, null);
                _fullscreenTargets.Remove(popup);
            }
        }

        /// <summary>关闭过渡后只恢复仍具备当前展示资格的 Popup。</summary>
        public void ResumePageAfterPopupClose(EUIPage page)
        {
            _uiManager.EnqueuePageOperation(() =>
            {
                if (page == GetTopPopup() && !HasClosingPopupAbove(page) && _popupPaused.Remove(page))
                    ResumePausedPage(page);
            });
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
            for (int i = popups.Count - 1; i >= 0; i--)
                if (IsPopupVisible(popups[i])) return popups[i];
            return null;
        }

        // Called before modal selection is reconciled. Only undo pauses owned by this route.
        internal void SynchronizePopupInteraction()
        {
            if (_mainPageStack.Count == 0) return;
            var entry = _mainPageStack[_mainPageStack.Count - 1];
            if (HasClosingMainAbove(entry.Page)) return;
            ResumeMainIfPaused(entry.Page);
            if (CurrentMainPage != entry.Page) return;

            for (int i = 0; i < entry.Popups.Count; i++)
            {
                var popup = entry.Popups[i];
                if (_fullscreenTargets.ContainsKey(popup))
                    SetFullscreenTarget(popup, FindFullscreenTarget(entry, i));
            }

            // OnPause/OnResume may synchronously close a page. Use a pooled snapshot for callbacks.
            using var pool = UnityEngine.Pool.ListPool<EUIPage>.Get(out var popups);
            popups.AddRange(entry.Popups);
            var top = GetTopPopup();
            foreach (var popup in popups)
            {
                if (CurrentMainPage != entry.Page) return;
                if (!IsPopupVisible(popup) || popup.CloseRequested) continue;
                if (popup != top || HasClosingPopupAbove(popup)) PausePopup(popup);
                else if (_popupPaused.Remove(popup)) ResumePausedPage(popup);
            }
        }

        private bool HasClosingPopupAbove(EUIPage page)
        {
            if (page == null || !page.Canvas) return false;
            var entry = _mainPageStack[_mainPageStack.Count - 1];
            foreach (var closing in _closingPopups)
                if (closing.Value == entry && closing.Key.GameObject &&
                    closing.Key.Canvas.sortingOrder > page.Canvas.sortingOrder)
                    return true;
            return false;
        }

        private void PausePopup(EUIPage popup)
        {
            if (popup.State != PageState.Opened || popup.CloseRequested || !popup.GameObject) return;
            _popupPaused.Add(popup);
            popup.OnPause();
        }

        private static bool IsPopupVisible(EUIPage page)
        {
            return page != null && !page.IsDisposed && page.GameObject && page.GameObject.activeInHierarchy &&
                (page.State == PageState.Opened || page.State == PageState.Paused || page.State == PageState.Showing);
        }

        // ── TopMost ──

        /// <summary>添加置顶层页面。</summary>
        public void AddTopMost(EUIPage page)
        {
            if (!TryAddTopMost(page))
                EmberDebug.LogWarning(TAG, $"TopMost 排序区间已满: {page.EUIPageDef}");
        }

        internal bool TryAddTopMost(EUIPage page)
        {
            if (_topMostList.Contains(page)) return true;
            if (!TryReserveTopMostOrder(page, page, PageGrowStep)) return false;
            _topMostList.Add(page);
            return true;
        }

        internal bool TrySetSubPageOrder(EUIPage page, EUIPage parent)
        {
            if (_topMostRoots.TryGetValue(parent, out var root))
                return !root.CloseRequested && TryReserveTopMostOrder(page, root, TOP_MOST_ORDER_STEP);

            if (page.Canvas) page.Canvas.sortingOrder = parent.GetNextSubPageSortingOrder();
            return true;
        }

        private bool TryReserveTopMostOrder(EUIPage page, EUIPage root, int step)
        {
            for (int i = _topMostOrders.Count - 1; i >= 0; i--)
                if (!_topMostOrders[i].Canvas || _topMostOrders[i].IsDisposed)
                    ReleaseTopMostOrder(_topMostOrders[i]);
            if (_topMostOrders.Count >= TOP_MOST_ORDER_CAPACITY || !page.Canvas || _topMostRoots.ContainsKey(page))
                return false;

            int index = _topMostOrders.Count;
            if (page != root)
                index = _topMostOrders.FindLastIndex(occupied => _topMostRoots[occupied] == root) + 1;

            int previous = index == 0 ? TopMostBaseOrder : _topMostOrders[index - 1].Canvas.sortingOrder;
            int next = index == _topMostOrders.Count ? TOP_MOST_ORDER_LIMIT : _topMostOrders[index].Canvas.sortingOrder;
            _topMostOrders.Insert(index, page);
            _topMostRoots.Add(page, root);
            if (previous + step < next)
            {
                SetTopMostOrder(page, previous + step);
            }
            else
            {
                // Compact the displayed order, including pages still drawing their exit animation.
                for (int i = 0; i < _topMostOrders.Count; i++)
                    SetTopMostOrder(_topMostOrders[i], TopMostBaseOrder + (i + 1) * TOP_MOST_ORDER_STEP);
            }
            _uiManager.MarkPageOrderDirty();
            return true;
        }

        private static void SetTopMostOrder(EUIPage page, int order)
        {
            page.Canvas.overrideSorting = true;
            page.Canvas.sortingOrder = order;
        }

        /// <summary>从查询路由移除。排序保留到退出过渡完成。</summary>
        public void RemoveTopMost(EUIPage page) => _topMostList.Remove(page);

        internal void ReleaseTopMostOrder(EUIPage page)
        {
            if (!_topMostRoots.Remove(page)) return;
            _topMostOrders.Remove(page);
            _topMostList.Remove(page);
            _uiManager.MarkPageOrderDirty();
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
                if (!_freePageList[i].ModalInputBlocked && ((IEUIView)_freePageList[i]).IsOpened && action(_freePageList[i]))
                    return;
            }

            // TopMost（后进先处理）
            for (int i = _topMostList.Count - 1; i >= 0; i--)
            {
                if (!_topMostList[i].ModalInputBlocked && ((IEUIView)_topMostList[i]).IsOpened && action(_topMostList[i]))
                    return;
            }

            // Popup（后进先处理）
            if (_mainPageStack.Count > 0)
            {
                var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
                for (int i = popups.Count - 1; i >= 0; i--)
                {
                    if (!popups[i].ModalInputBlocked && ((IEUIView)popups[i]).IsOpened && action(popups[i]))
                        return;
                }
            }

            // MainPage
            for (int i = _mainPageStack.Count - 1; i >= 0; i--)
            {
                if (!_mainPageStack[i].Page.ModalInputBlocked && ((IEUIView)_mainPageStack[i].Page).IsOpened && action(_mainPageStack[i].Page))
                    return;
            }

            // Background（最低优先级，最后遍历）
            if (_backgroundPage != null && !_backgroundPage.ModalInputBlocked && ((IEUIView)_backgroundPage).IsOpened)
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
            _mainPaused.Clear();
            _closingMains.Clear();
            _closingPopups.Clear();
            _fullscreenTargets.Clear();
            _popupPaused.Clear();

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

        #endregion
    }
}
