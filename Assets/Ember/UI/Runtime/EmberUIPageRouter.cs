// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Core;
using Ember.Basic;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 页面路由器（应用层）。
    ///
    /// 管理"打开什么/何时打开"的逻辑：路由分发、显示队列、父子页面追踪、
    /// BG Mask 自动管理、返回键逐层处理。
    ///
    /// <para>依赖 <see cref="EmberUIManager"/> 执行底层页面生命周期操作。</para>
    ///
    /// <para>使用示例：</para>
    /// <code>
    /// EmberUIPageRouter.Instance.ShowMainPage(GamePages.MainMenu);
    /// EmberUIPageRouter.Instance.ShowPopup(GamePages.Settings);
    /// EmberUIPageRouter.Instance.CloseTopPopup();
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.UI + 1)]
    public class EmberUIPageRouter : EmberMonoSingleton<EmberUIPageRouter>, IEmberManager
    {
        private const string TAG = LogTags.UIManager;

        #region 内部参数

        private EmberUIManager _uiManager;
        private EmberPageContext _context => _uiManager.PageContext;

        private readonly Queue<ShowRequest> _showQueue = new();
        private bool _isProcessingQueue;

        private readonly Dictionary<EmberPage, EmberPage> _parentPageMap = new(); // SubPage → ParentPage
        private readonly Dictionary<EmberPage, object> _returnValueMap = new();    // Page → ReturnValue

        private bool _initialized;

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        private struct ShowRequest
        {
            public PageDef PageDef;
            public object Args;
            public Action<EmberPage> OnComplete;
            public EmberPage ParentPage; // SubPage 时非空
        }

        #endregion

        // --------------------------------------------------------

        #region IEmberManager

        void IEmberManager.Init()
        {
            if (_initialized) return;

            _uiManager = EmberUIManager.Instance;
            _initialized = true;

            EmberEventBus.OnNext(EmberUIEvents.UIPageRouterReady);
            EmberDebug.LogInit(TAG, "EmberUIPageRouter 初始化完成。");
        }

        void IEmberManager.Destroy()
        {
            _showQueue.Clear();
            _parentPageMap.Clear();
            _returnValueMap.Clear();
            _initialized = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法（路由 API）

        /// <summary>
        /// 显示主页面。替换当前 MainPage。
        /// </summary>
        public void ShowMainPage(PageDef pageDef, object args = null, Action<EmberPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示弹窗。叠加在当前 MainPage 之上，自动创建 BG Mask。
        /// </summary>
        public void ShowPopup(PageDef pageDef, object args = null, Action<EmberPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示置顶弹窗。高于所有 Popup（如 Loading、全局提示）。
        /// </summary>
        public void ShowTopMost(PageDef pageDef, object args = null, Action<EmberPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete);
        }

        /// <summary>
        /// 显示子页面。嵌入父页面的指定区域，父关子关。
        /// </summary>
        public void ShowSubPage(PageDef pageDef, EmberPage parentPage, object args = null, Action<EmberPage> onComplete = null)
        {
            EnqueueShow(pageDef, args, onComplete, parentPage);
        }

        /// <summary>
        /// 关闭指定页面。
        /// </summary>
        public void ClosePage(EmberPage page, object returnValue = null)
        {
            if (page == null) return;

            if (returnValue != null)
                _returnValueMap[page] = returnValue;

            // 先关子页面
            foreach (var subPage in page.SubPages)
            {
                ClosePage(subPage);
            }

            // 从上下文中移除
            switch (page.PageDef.PageType)
            {
                case PageType.MainPage:
                    _context.PopMainPage(page);
                    break;
                case PageType.Popup:
                    _context.RemovePopup(page);
                    HideBgMaskForPopup(page);
                    break;
                case PageType.TopMost:
                    _context.RemoveTopMost(page);
                    break;
                case PageType.SubPage:
                    {
                        var parent = page.ParentPage;
                        if (parent != null)
                        {
                            parent.UnregisterSubPage(page);
                            _parentPageMap.Remove(page);
                        }
                        break;
                    }
                case PageType.Overlay:
                    _context.RemoveOverlay(page);
                    break;
            }

            _uiManager.ClosePage(page);
        }

        /// <summary>
        /// 关闭最顶层 Popup。
        /// </summary>
        public void CloseTopPopup()
        {
            var top = _context.GetTopPopup();
            if (top != null) ClosePage(top);
        }

        /// <summary>
        /// 关闭所有 Popup。
        /// </summary>
        public void CloseAllPopups()
        {
            while (_context.HasPopup())
            {
                CloseTopPopup();
            }
        }

        /// <summary>
        /// 获取页面的回传值。
        /// </summary>
        public object GetReturnValue(EmberPage page)
        {
            _returnValueMap.TryGetValue(page, out var val);
            _returnValueMap.Remove(page);
            return val;
        }

        /// <summary>
        /// 手动更新（由外部调用或 EmberUpdateManager 驱动）。
        /// </summary>
        public void Tick()
        {
            ProcessShowQueue();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void EnqueueShow(PageDef pageDef, object args, Action<EmberPage> onComplete, EmberPage parentPage = null)
        {
            _showQueue.Enqueue(new ShowRequest
            {
                PageDef = pageDef,
                Args = args,
                OnComplete = onComplete,
                ParentPage = parentPage,
            });
        }

        private void ProcessShowQueue()
        {
            if (_showQueue.Count == 0 || _isProcessingQueue) return;

            _isProcessingQueue = true;
            while (_showQueue.Count > 0)
            {
                var req = _showQueue.Dequeue();
                ProcessShowRequest(req);
            }
            _isProcessingQueue = false;
        }

        private void ProcessShowRequest(ShowRequest req)
        {
            var pageDef = req.PageDef;

            _uiManager.ResourceProvider.LoadPrefabAsync(pageDef.PrefabPath, prefab =>
            {
                if (prefab == null)
                {
                    EmberDebug.LogError(TAG, $"无法加载预制体: {pageDef.PrefabPath}");
                    return;
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;

                var page = instance.GetComponent<EmberPage>();
                if (page == null)
                {
                    EmberDebug.LogError(TAG, $"预制体 '{pageDef.PrefabPath}' 没有 EmberPage 组件。");
                    UnityEngine.Object.Destroy(instance);
                    return;
                }

                // 根据 PageType 执行路由
                switch (pageDef.PageType)
                {
                    case PageType.MainPage:
                        _context.PushMainPage(page);
                        break;

                    case PageType.Popup:
                        _context.AddPopup(page);
                        // 创建 BG Mask
                        ShowBgMaskForPopup(page);
                        break;

                    case PageType.TopMost:
                        _context.AddTopMost(page);
                        break;

                    case PageType.SubPage:
                        {
                            var parent = req.ParentPage;
                            if (parent != null)
                            {
                                parent.RegisterSubPage(page);
                                _parentPageMap[page] = parent;
                            }
                            break;
                        }

                    case PageType.Overlay:
                        _context.AddOverlay(page);
                        break;
                }

                _uiManager.OpenPage(page, pageDef, req.Args, () =>
                {
                    req.OnComplete?.Invoke(page);
                });
            });
        }

        // ── BG Mask 管理 ──

        private readonly Dictionary<EmberPage, GameObject> _activeMasks = new();

        private void ShowBgMaskForPopup(EmberPage popup)
        {
            var canvas = popup.GetComponent<Canvas>();
            var sortingOrder = canvas ? canvas.sortingOrder : (int)popup.PageDef.Layer;

            var mask = _uiManager.ShowBgMask(sortingOrder - 1, () =>
            {
                // 点击遮罩关闭 Popup
                ClosePage(popup);
            });

            if (mask != null)
                _activeMasks[popup] = mask;
        }

        private void HideBgMaskForPopup(EmberPage popup)
        {
            if (_activeMasks.TryGetValue(popup, out var mask))
            {
                _uiManager.HideBgMask(mask);
                _activeMasks.Remove(popup);
            }
        }

        #endregion
    }
}
