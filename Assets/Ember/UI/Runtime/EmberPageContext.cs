// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

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
    ///   <item><b>SortingOrder 自动计算</b>：MainPageOrder=1000, PageGrowStep=500, TopMostOrder=25000</item>
    ///   <item><b>HideLowerPage</b>：Popup 打开时隐藏下方页面（不销毁），关闭时恢复</item>
    /// </list>
    /// </summary>
    public class EmberPageContext
    {
        #region 内部参数

        private const int MainPageBaseOrder  = 1000;
        private const int PageGrowStep       = 500;
        private const int TopMostBaseOrder   = 25000;

        private readonly List<StackEntry> _mainPageStack = new List<StackEntry>();
        private readonly List<EmberPage> _topMostList   = new List<EmberPage>();
        private readonly List<EmberPage> _overlayList   = new List<EmberPage>();

        private readonly EmberUIManager _uiManager;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        internal class StackEntry
        {
            public EmberPage Page;
            public int SortingOrder;
            public readonly List<EmberPage> Popups = new List<EmberPage>();
        }

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        public EmberPageContext(EmberUIManager uiManager)
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
        public EmberPage CurrentMainPage => _mainPageStack.Count > 0 ? _mainPageStack[_mainPageStack.Count - 1].Page : null;

        /// <summary>
        /// 将 MainPage 压入栈。暂停旧 MainPage，新页面显示。
        /// </summary>
        public void PushMainPage(EmberPage page)
        {
            // 暂停当前 MainPage
            if (_mainPageStack.Count > 0)
            {
                var current = _mainPageStack[_mainPageStack.Count - 1];
                _uiManager.EnqueuePageOperation(() =>
                {
                    ((IUIView)current.Page).OnPause();
                    EmberUIObserver.NotifyPaused(current.Page.PageDef);
                });

                // 隐藏当前 MainPage 的 Popup
                foreach (var popup in current.Popups)
                {
                    _uiManager.EnqueuePageOperation(() => ((IUIView)popup).OnPause());
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
        /// 关闭 MainPage。如果存在上一个 MainPage，恢复它。
        /// </summary>
        public void PopMainPage(EmberPage page)
        {
            var index = FindMainPageIndex(page);
            if (index < 0) return;

            // 关闭此 MainPage 上的所有 Popup
            var entry = _mainPageStack[index];
            for (int i = entry.Popups.Count - 1; i >= 0; i--)
            {
                var popup = entry.Popups[i];
                _uiManager.ClosePageInternal(popup);
            }
            entry.Popups.Clear();

            // 移除
            _mainPageStack.RemoveAt(index);

            // 如果存在前一个 MainPage，恢复它
            if (index > 0 && index - 1 < _mainPageStack.Count)
            {
                var previous = _mainPageStack[index - 1];
                _uiManager.EnqueuePageOperation(() =>
                {
                    ((IUIView)previous.Page).OnResume();
                    EmberUIObserver.NotifyResumed(previous.Page.PageDef);
                });

                // 恢复 Popup
                foreach (var popup in previous.Popups)
                {
                    _uiManager.EnqueuePageOperation(() =>
                    {
                        ((IUIView)popup).OnResume();
                        EmberUIObserver.NotifyResumed(popup.PageDef);
                    });
                }
            }
        }

        // ── Popup ──

        /// <summary>在当前 MainPage 上添加一个 Popup</summary>
        public void AddPopup(EmberPage popup)
        {
            if (_mainPageStack.Count == 0) return;

            var current = _mainPageStack[_mainPageStack.Count - 1];

            // HideLowerPage：如果之前没有 Popup，隐藏 MainPage
            if (current.Popups.Count == 0)
            {
                SetPageVisible(current.Page, false);
            }

            // 隐藏前一个 Popup（如果有）
            if (current.Popups.Count > 0)
            {
                var prevPopup = current.Popups[current.Popups.Count - 1];
                _uiManager.EnqueuePageOperation(() => ((IUIView)prevPopup).OnPause());
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

        /// <summary>移除 Popup，恢复前一个 Popup 或 MainPage</summary>
        public void RemovePopup(EmberPage popup)
        {
            foreach (var entry in _mainPageStack)
            {
                if (entry.Popups.Remove(popup))
                {
                    // 如果还有 Popup，恢复上一个
                    if (entry.Popups.Count > 0)
                    {
                        var prev = entry.Popups[entry.Popups.Count - 1];
                        _uiManager.EnqueuePageOperation(() =>
                        {
                            SetPageVisible(prev, true);
                            ((IUIView)prev).OnResume();
                            EmberUIObserver.NotifyResumed(prev.PageDef);
                        });
                    }
                    else
                    {
                        // 恢复 MainPage
                        SetPageVisible(entry.Page, true);
                        _uiManager.EnqueuePageOperation(() =>
                        {
                            ((IUIView)entry.Page).OnResume();
                            EmberUIObserver.NotifyResumed(entry.Page.PageDef);
                        });
                    }
                    return;
                }
            }
        }

        /// <summary>获取指定 MainPage 上的 Popup 数量</summary>
        public int GetPopupCount(EmberPage mainPage = null)
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
        public EmberPage GetTopPopup()
        {
            if (_mainPageStack.Count == 0) return null;
            var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
            return popups.Count > 0 ? popups[popups.Count - 1] : null;
        }

        // ── TopMost ──

        /// <summary>添加置顶层页面</summary>
        public void AddTopMost(EmberPage page)
        {
            _topMostList.Add(page);
            var order = TopMostBaseOrder + _topMostList.Count * PageGrowStep;
            var canvas = page.Canvas;
            if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = order; }
        }

        /// <summary>移除置顶层页面</summary>
        public void RemoveTopMost(EmberPage page)
        {
            _topMostList.Remove(page);
        }

        /// <summary>最顶层 TopMost 页面</summary>
        public EmberPage GetTopTopMost()
        {
            return _topMostList.Count > 0 ? _topMostList[_topMostList.Count - 1] : null;
        }

        // ── Overlay ──

        public void AddOverlay(EmberPage page) => _overlayList.Add(page);
        public void RemoveOverlay(EmberPage page) => _overlayList.Remove(page);

        // ── 查询 ──

        /// <summary>按层级从高到低遍历所有可见页面，调用 action，返回 true 停止遍历</summary>
        public void ForEachVisiblePage(Func<EmberPage, bool> action)
        {
            // TopMost（后进先处理）
            for (int i = _topMostList.Count - 1; i >= 0; i--)
            {
                if (((IUIView)_topMostList[i]).IsOpened && action(_topMostList[i]))
                    return;
            }

            // Popup（后进先处理）
            if (_mainPageStack.Count > 0)
            {
                var popups = _mainPageStack[_mainPageStack.Count - 1].Popups;
                for (int i = popups.Count - 1; i >= 0; i--)
                {
                    if (((IUIView)popups[i]).IsOpened && action(popups[i]))
                        return;
                }
            }

            // MainPage
            for (int i = _mainPageStack.Count - 1; i >= 0; i--)
            {
                if (((IUIView)_mainPageStack[i].Page).IsOpened && action(_mainPageStack[i].Page))
                    return;
            }
        }

        /// <summary>关闭所有页面</summary>
        public void CloseAll()
        {
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

            EmberUIObserver.NotifyAllClosed();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private int FindMainPageIndex(EmberPage page)
        {
            for (int i = 0; i < _mainPageStack.Count; i++)
            {
                if (_mainPageStack[i].Page == page) return i;
            }
            return -1;
        }

        private static void SetPageVisible(EmberPage page, bool visible)
        {
            var cg = page.CanvasGroup;
            if (cg)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = visible;
                cg.interactable = visible;
            }
        }

        #endregion
    }
}
