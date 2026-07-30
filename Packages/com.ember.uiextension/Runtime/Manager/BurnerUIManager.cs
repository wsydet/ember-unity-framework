//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using UnityEngine;
//using UnityEngine.UI;
//
//using Burner.Extensions;
//using UnityEngine.Profiling;
//using System.Linq;
//
//namespace Burner.UIExtension
//{
//    internal struct LoadTimeToken : IDisposable
//    {
//        Stopwatch sw;
//        bool disposed;
//        BurnerUIManager mgr;
//
//        public LoadTimeToken(Stopwatch sw, BurnerUIManager mgr)
//        {
//            this.sw = sw;
//            disposed = false;
//            this.mgr = mgr;
//            sw.Restart();
//        }
//        public void Dispose()
//        {
//            if (!disposed)
//            {
//                sw.Stop();
//                mgr.RegisterTimeSliceUsed((int)sw.ElapsedMilliseconds);
//            }
//        }
//    }
//    /// <summary>
//    /// UI管理器
//    /// </summary>
//    public class BurnerUIManager
//    {
//        struct ExtensionComponentInfo
//        {
//            public string Name;
//            public Type ComponentType;
//            public Type GameUIComponentType;
//        }
//        static BurnerUIManager instance = new BurnerUIManager();
//
//        /// <summary>
//        /// 管理器单例
//        /// </summary>
//        public static BurnerUIManager Instance => instance;
//
//        Dictionary<string, GamePage> pages = new Dictionary<string, GamePage>();
//        Dictionary<string, ExtensionComponentInfo> extensionComps = new Dictionary<string, ExtensionComponentInfo>();
//        HashSet<GamePage> visiblePages = new HashSet<GamePage>();
//        bool isUpdating = false;
//        List<GamePage> pendingLoadPageDelete = new List<GamePage>();//原pendingDelete
//        List<GamePage> pendingLoadPageAdd = new List<GamePage>();//原pendingAdd
//        List<GamePage> pendingCloseEvents = new List<GamePage>();
//        List<ILogicResolver> resolvers = new List<ILogicResolver>();
//        PageContext curMainCtx;
//        GameObject rootNode;
//        Camera uicam;
//        int openingCnt = 0;
//        int highPriorLoadCnt;
//        GlobalEvents globalEvents = new GlobalEvents();
//        NodePostProcessManager nodePPMgr;
//        Vector2Int displayResolution;
//        Action<GameUILogic> onPageOpenCb;
//        Action<GameUILogic> onPageCloseCb;
//        Action<string> onPageLoadCb;
//        Action<string> onPageLoadFinishCb;
//        Stopwatch sw = new Stopwatch();
//        int accumulatedTime = 0;
//
//
//        /// <summary>
//        /// 当界面请求打开时
//        /// </summary>
//        Action<GamePage> onPreparePageOpen;
//
//        /// <summary>
//        /// 当界面资源加载完毕打开时
//        /// </summary>
//        Action<GamePage> onFinalizePageOpen;
//
//        /// <summary>
//        /// 当界面请求关闭时
//        /// </summary>
//        Action<GamePage> onPreparePageClose;
//
//        /// <summary>
//        /// 当界面完成关闭时
//        /// </summary>
//        internal Action<GamePage> onFinalizePageClose;
//
//
//        /// <summary>
//        /// UI 根节点
//        /// </summary>
//        public GameObject RootNode => rootNode;
//
//        /// <summary>
//        /// UI 相机
//        /// </summary>
//        public Camera UICamera => uicam;
//
//        /// <summary>
//        /// 页面加载完毕后自动调节CanvasScaler适配设置。
//        /// 注意：此功能依赖 CanvasScaler.screenMatchMode = MatchWidthOrHeight。
//        /// 如需启用运行时动态适配（折叠屏/横竖屏切换等场景），需将相关 prefab 的 screenMatchMode 改为 MatchWidthOrHeight。
//        /// </summary>
//        public bool AutoAdjustCanvasScaler { get; set; } = true;
//
//        /// <summary>
//        /// 是否延迟GameObject的SetVisible于OnShow之后，开启后在SetVisible为true之前会派发OnBecomeVisible回调
//        /// </summary>
//        public bool EnablePostponeSetVisible { get; set; }
//
//        /// <summary>
//        /// 检测到屏幕分辨率变更时触发（折叠屏适配）
//        /// </summary>
//        public Action OnScreenResolutionChanged { get; set; }
//        /// <summary>
//        /// 当自动调整CanvasScaler时回调，回调时机为调整完毕后，供项目组做额外处理
//        /// </summary>
//        public Action<CanvasScaler> OnAdjustCanvasScaler { get; set; }
//
//        [Obsolete("Please use SetOnPageOpenHandler instead")]
//        public Action<string> OnPageOpen { get; set; }
//
//        [Obsolete("Please use SetOnPageCloseHandler instead")]
//        public Action<string> OnPageClose { get; set; }
//
//        /// <summary>
//        /// 页面默认销毁延迟
//        /// </summary>
//        public float DefaultDestoryDelay { get; set; } = 1;
//
//        public GlobalEvents GlobalEvents => globalEvents;
//
//        internal Func<string, string> GetClassNameCallback { get; set; }
//
//        /// <summary>
//        /// 每一帧用于加载的最大ms数，默认为500ms
//        /// </summary>
//        public int MaximalFrameTimeBudget { get; set; } = 500;
//
//        public void RequestHighPriorityLoad()
//        {
//            Application.backgroundLoadingPriority = ThreadPriority.High;
//            highPriorLoadCnt++;
//        }
//
//        public void FinishHighPriorityLoad()
//        {
//            highPriorLoadCnt--;
//            if (highPriorLoadCnt <= 0)
//            {
//                highPriorLoadCnt = 0;
//                Application.backgroundLoadingPriority = ThreadPriority.Normal;
//            }
//        }
//
//        /// <summary>
//        /// 初始化
//        /// </summary>
//        /// <param name="rootNode"></param>
//        /// <param name="uiCam"></param>
//        public void Initialize(GameObject rootNode, Camera uiCam)
//        {
//            this.rootNode = rootNode;
//            this.uicam = uiCam;
//
//            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
//            {
//                if (a.FullName.StartsWith("Unity") || a.FullName.StartsWith("System") || a.FullName.StartsWith("mscorlib"))
//                    continue;
//                foreach (var t in a.GetTypes())
//                {
//                    if (t.IsGenericTypeDefinition || t.IsAbstract)
//                        continue;
//                    var arr = t.GetCustomAttributes(typeof(BurnerUIExtensionAttribute), false);
//                    if (arr != null && arr.Length > 0)
//                    {
//                        BurnerUIExtensionAttribute attr = (BurnerUIExtensionAttribute)arr[0];
//                        string name = string.IsNullOrEmpty(attr.Name) ? attr.ComponentType.Name : attr.Name;
//                        extensionComps[name] = new ExtensionComponentInfo { Name = name, ComponentType = attr.ComponentType, GameUIComponentType = t };
//                    }
//                }
//            }
//        }
//
//        public T GetPopupPage<T>(int index,string group = null)
//            where T : GameUILogic
//        {
//            if (curMainCtx != null && curMainCtx.HasPopup(group))
//            {
//                var popups = curMainCtx.GetCurPopupsByGroup(group);
//                if (index >= 0 && index < popups?.Count)
//                {
//                    var cur = popups[index];
//                    return cur.Page.UILogic as T;
//                }
//                else
//                {
//                    throw new ArgumentOutOfRangeException();
//                }
//            }
//            else
//                return null;
//        }
//
//        public T GetTopMostPopupPage<T>(int index,string group = null)
//            where T : GameUILogic
//        {
//            if (curMainCtx != null)
//            {
//                var topMostPopups = curMainCtx.GetTopMostPopupsByGroup(group);
//                if (topMostPopups!=null && topMostPopups?.Count > 0)
//                {
//                    if (index >= 0 && index < topMostPopups?.Count)
//                    {
//                        var cur = topMostPopups[index];
//                        return cur.Page.UILogic as T;
//                    }
//                    else
//                    {
//                        throw new ArgumentOutOfRangeException();
//                    }
//                }
//                else
//                {
//                    return null;
//                }
//            }
//            else
//                return null;
//        }
//
//        public GamePage GetGamePage(string prefabName)
//        {
//            if (pages.TryGetValue(prefabName, out var page))
//            {
//                return page;
//            }
//            // 如果在 pages 字典中未找到，则检查 pendingAdd 列表
//            foreach (var p in pendingLoadPageAdd)
//            {
//                if (p.PrefabName == prefabName)
//                {
//                    return p;
//                }
//            }
//            return null;
//        }
//
//        public bool IsUIOpeningAtTop(string uiName)
//        {
//            uiName = uiName.ToLower();
//            var page = GetGamePage(uiName);
//
//            //暂时应该不会出现在引导状态下，page被塞入pendingLoadPageAdd的情况
//            if (page == null)
//            {
//                return false;
//            }
//
//            if (page.IsMainPage)
//            {
//                if (GetCurrentPopupPageName() != null)
//                    return false;
//                if (GetCurrentMainPageName() == uiName)
//                    return true;
//                if (pendingLoadPageAdd == null || pendingLoadPageAdd.Count <= 0)
//                    return false;
//                return pendingLoadPageAdd[pendingLoadPageAdd.Count - 1].PrefabName == uiName;
//            }
//
//            if (page.IsPopup)
//            {
//                return GetCurrentPopupPageName() == uiName;
//            }
//
//            return false;
//        }
//
//        public T GetPage<T>(string prefabName)
//            where T : GameUILogic
//        {
//            if (pages.TryGetValue(prefabName, out var page))
//            {
//                return page.UILogic as T;
//            }
//            else
//                return null;
//        }
//
//        internal PageContext GetPageContext()
//        {
//            return curMainCtx;
//        }
//        public void Dispose()
//        {
//            curMainCtx?.Dispose();
//            foreach (var i in resolvers)
//            {
//                i.Dispose();
//            }
//            resolvers.Clear();
//            curMainCtx = null;
//            isUpdating = true;
//            foreach (var i in pages)
//            {
//                i.Value.DoDispose();
//            }
//            isUpdating = false;
//            pendingLoadPageDelete.Clear();
//            pages.Clear();
//            globalEvents.Clear();
//#pragma warning disable CS0618
//            OnPageOpen = null;
//            OnPageClose = null;
//#pragma warning enable CS0618
//            onPageCloseCb = null;
//            onPageOpenCb = null;
//            GamePage.ShowHistory.Clear();
//        }
//
//        /// <summary>
//        /// 打开指定页面
//        /// </summary>
//        /// <param name="prefabName">页面资源名</param>
//        /// <param name="param">传递参数</param>
//        /// <param name="preserveContext">是否保留之前上下文以供恢复</param>
//        public void ShowMainPage(string prefabName, object param = null, string mainPageGroup = null, bool preserveContext = false)
//        {
//            //GamePage prevPage = null;
//            //if (preserveContext)
//            //{
//            //    prevPage = curMainCtx?.GetCurrentMainPage(mainPageGroup);
//            //}
//            ShowPageInteral(prefabName, PageFlags.MainPage, param, !preserveContext, mainPageGroup);
//        }
//
//        public void ShowFreePage(string prefabName, int sortingOrder, string sortingLayer = null, object param = null)
//        {
//            ShowPageInteral(prefabName, PageFlags.FreePage, param, false, null, sortingOrder, sortingLayer);
//        }
//
//        /// <summary>
//        /// 将指定页面和该页面之后打开的页面都关闭
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="mainPageGroup"></param>
//        public void CloseAllPagesAfter(string prefabName, string mainPageGroup = null)
//        {
//            curMainCtx?.CloseAllPagesAfter(prefabName, mainPageGroup);
//        }
//
//        /// <summary>
//        /// 以弹窗模式打开页面
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="param"></param>
//        public void ShowPopup(string prefabName, object param = null, bool isTopMost = false)
//        {
//            PageFlags flags = PageFlags.Popup;
//            if (isTopMost)
//                flags |= PageFlags.TopMost;
//            ShowPageInteral(prefabName, flags, param, false, null);
//        }
//
//        /// <summary>
//        /// 判断指定的页面是否可见
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <returns></returns>
//        public bool IsPageVisible(string prefabName)
//        {
//            var page = GetGamePage(prefabName);
//
//            return page != null && page.IsResReady && page.Visible;
//            // if (pages.TryGetValue(prefabName, out var page))
//            // {
//            //     return page.IsResReady && page.Visible;
//            // }
//            // else
//            //     return false;
//        }
//
//        /// <summary>
//        /// 隐藏指定Page
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void HidePage(string prefabName, bool renderOnly = false)
//        {
//            Burner.Logger.Log($"隐藏页面 '{prefabName}'。");
//            var page = GetGamePage(prefabName);
//            if (page != null)
//            {
//                page.HidePage(renderOnly);
//            }
//        }
//
//        /// <summary>
//        /// 重新显示之前隐藏的Page
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void RestorePage(string prefabName)
//        {
//            Burner.Logger.Log($"恢复页面 '{prefabName}'。");
//            var page = GetGamePage(prefabName);
//            if (page != null)
//            {
//                page.RestorePage();
//            }
//        }
//
//        /// <summary>
//        /// 关闭指定Page
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void ClosePage(string prefabName, object returnVal = null)
//        {
//            Burner.Logger.Log($"关闭页面 '{prefabName}'。");
//            ClosePageInternal(prefabName, returnVal);
//        }
//
//        public void ClosePageInternal(string prefabName, object returnVal = null)
//        {
//            var page = GetGamePage(prefabName);
//            if (page != null)
//            {
//                onPreparePageClose?.Invoke(page);
//                if (pendingLoadPageAdd.Contains(page) && !page.IsLoading && !page.IsPreloading)
//                {
//                    Burner.Logger.Warn($"页面 '{prefabName}' 正在pendingAdd队列中且尚未开始加载，对其执行关闭操作，将转换为取消其打开请求。");
//                    pendingLoadPageAdd.Remove(page);
//
//                    if (page.ContentContext.AsyncOperations != null)
//                    {
//                        page.ContentContext.AsyncOperations.FinishClose(returnVal);
//                    }
//                    //page.DoDispose();
//                }
//                //else
//                //{
//                //page.Close(returnVal);
//                //}
//
//
//                if (page.IsClosing /*|| waitingToClose != null*/)
//                    return;
//                if(page.ContentContext != null)
//                    page.ContentContext.ReturnValue = returnVal;
//
//                if (page.IsMainPage)
//                {
//                    curMainCtx.CloseMainPage(page);
//                }
//                //else if (!page.IsSubPage && !page.IsFreePage)
//                else if (page.IsPopup || page.IsTopMost)
//                {
//                    curMainCtx.ClosePopup(page);
//                }
//                else
//                {
//                    page.CloseInternal(true);
//                }
//            }
//        }
//
//        /// <summary>
//        /// 隐藏所有Page，TopMost的除外
//        /// </summary>
//        public void HideAllPages(/*string groupName = null*/)
//        {
//            var pageNames = pages.Keys.ToList();
//            foreach (var pageName in pageNames)
//            {
//                HidePage(pageName);
//            }
//            //curMainCtx?.HideAll(false, groupName);
//        }
//
//        /// <summary>
//        /// 关闭所有Page, TopMost的除外
//        /// </summary>
//        public void CloseAllPages(/*string groupName = null*/)
//        {
//            var pageNames = pages.Keys.ToList();
//            foreach (var pageName in pageNames)
//            {
//                if(string.Equals(pageName, "uigmpage"))
//                    continue;
//                ClosePage(pageName);
//            }
//            //curMainCtx?.HideAll(true, groupName);
//        }
//
//        /// <summary>
//        /// 获取当前所有MainPage界面数量
//        /// </summary>
//        /// <returns></returns>
//        public int GetMainPageCount(string group = null)
//        {
//            if (string.IsNullOrEmpty(group))
//            {
//                return curMainCtx?.MainPageList.Stack.Count ?? 0;
//            }
//            else
//            {
//                if (curMainCtx?.MainPageGroups.TryGetValue(group, out var groupCtx) == true)
//                {
//                    return groupCtx.Stack.Count;
//                }
//            }
//            return 0;
//        }
//
//        /// <summary>
//        /// 获取指定组当前的当前MainPage名称
//        /// </summary>
//        /// <param name="group"></param>
//        /// <returns></returns>
//        public string GetCurrentMainPageName(string group = null)
//        {
//            var page = curMainCtx?.GetCurrentMainPage(group);
//            return page != null ? page.PrefabName : null;
//        }
//
//        /// <summary>
//        /// 获取当前在最上面的Popup的名称
//        /// </summary>
//        /// <param name="includeTopMost">是否包含TopMost的页面</param>
//        /// <returns></returns>
//        public string GetCurrentPopupPageName(bool includeTopMost = false, bool includePending = true,string group = null)
//        {
//            if (curMainCtx != null && curMainCtx.HasPopup(group))
//            {
//                if (includeTopMost)
//                {
//                    var topMostPopups = curMainCtx.GetTopMostPopupsByGroup(group);
//                    if (topMostPopups!=null && topMostPopups?.Count > 0)
//                    {
//                        return topMostPopups[topMostPopups.Count - 1]?.Page?.PrefabName;
//                    }
//                }
//
//                var popups=curMainCtx.GetCurPopupsByGroup(group);
//                var cnt = popups.Count - 1;
//                for (int i = cnt; i >= 0; i--)
//                {
//                    var cur = popups[i];
//                    //if (!includeTopMost && cur.Page.IsTopMost)
//                    //    continue;
//                    return cur.Page.PrefabName;
//                }
//                return null;
//            }
//            return null;
//        }
//
//        /// <summary>
//        /// 获取当前所有Popup界面数量
//        /// </summary>
//        /// <returns></returns>
//        public int GetPopupPageCount(string group = null,bool includeTopMost = false)
//        {
//            int count = 0;
//            if (curMainCtx != null)
//            {
//                count += curMainCtx.GetPopupsCountByGroup(group, includeTopMost);
//
//                return count;
//            }
//            return 0;
//        }
//
//        /// <summary>
//        /// 获取当前所有TopMost界面数量
//        /// </summary>
//        /// <returns></returns>
//        public int GetTopMostPopupPageCount(string group = null)
//        {
//            int count = 0;
//            if (curMainCtx != null)
//            {
//                count += curMainCtx.GetTopMostPopupsCount(group);
//
//                return count;
//            }
//            return 0;
//        }
//
//        /// <summary>
//        /// 根据Index获取Popup窗口Prefab名
//        /// </summary>
//        /// <param name="index"></param>
//        /// <returns></returns>
//        public string GetPopupPageName(int index ,string group = null)
//        {
//            if (curMainCtx != null && curMainCtx.HasPopup(group))
//            {
//                var popups = curMainCtx.GetCurPopupsByGroup(group);
//                if (index >= 0 && index < popups?.Count)
//                {
//                    var cur = popups[index];
//                    return cur.Page.PrefabName;
//                }
//                else
//                {
//                    throw new ArgumentOutOfRangeException();
//                }
//            }
//            else
//                return null;
//        }
//
//        /// <summary>
//        /// 根据Index获取TopMostPopup窗口Prefab名
//        /// </summary>
//        /// <param name="index"></param>
//        /// <returns></returns>
//        public string GetTopMostPopupPageName(int index ,string group = null)
//        {
//            if (curMainCtx != null)
//            {
//                var topMostPopups = curMainCtx.GetTopMostPopupsByGroup(group);
//                if (topMostPopups?.Count > 0)
//                {
//                    if (index >= 0 && index < topMostPopups?.Count)
//                    {
//                        var cur = topMostPopups[index];
//                        return cur.Page.PrefabName;
//                    }
//                    else
//                    {
//                        throw new ArgumentOutOfRangeException();
//                    }
//                }
//                else
//                {
//                    return null;
//                }
//            }
//            else
//                return null;
//        }
//        /// <summary>
//        /// 关闭指定组的当前MainPage
//        /// </summary>
//        /// <param name="group"></param>
//        public void CloseCurrentMainPage(string group = null)
//        {
//            curMainCtx?.CloseCurrentMainPage(group);
//        }
//
//        /// <summary>
//        /// 关闭当前在最上面的非TopMost类型的Popup
//        /// </summary>
//        public void CloseCurrentPopup()
//        {
//            var pageName = GetCurrentPopupPageName();
//            if (!string.IsNullOrEmpty(pageName))
//                ClosePage(pageName);
//        }
//
//        /// <summary>
//        /// 当前是否有Popup窗口
//        /// </summary>
//        public bool HasPopup(string group = null) => curMainCtx != null ? curMainCtx.HasPopup(group) : false;
//
//        /// <summary>
//        /// 获取指定组是否包含MainPage
//        /// </summary>
//        /// <param name="group"></param>
//        /// <returns></returns>
//        public bool HasMainPage(string group = null)
//        {
//            return curMainCtx != null ? curMainCtx.HasMainPage(group) : false;
//        }
//
//        /// <summary>
//        /// 关闭当前上下文所有弹窗
//        /// </summary>
//        /// <param name="includingTopMost">是否包含置顶弹窗</param>
//        public void CloseAllPopups(bool includingTopMost = false,string group = null)
//        {
//            if (curMainCtx != null)
//            {
//                curMainCtx.CloseAllPopups(includingTopMost, true, group);
//            }
//        }
//
//        void CheckAndDispatchPendingCloseEvent()
//        {
//            if (pendingCloseEvents.Count > 0)
//            {
//                foreach (var i in pendingCloseEvents)
//                {
//#pragma warning disable CS0618
//                    OnPageClose?.Invoke(i.PrefabName);
//#pragma warning enable CS0618
//                    onPageCloseCb?.Invoke(i.UILogic);
//                    i.PendingCloseEvent = false;
//                    if (i.NeedDispose)
//                    {
//                        i.DoDispose();
//                    }
//                }
//                pendingCloseEvents.Clear();
//            }
//        }
//
//        public void ShowMainPageGroup(string mainPageGroup)
//        {
//            if (curMainCtx == null)
//                curMainCtx = new PageContext();
//            var ctx = curMainCtx;
//            ctx.ShowMainPageGroup(mainPageGroup);
//        }
//
//        public void HideMainPageGroup(string mainPageGroup)
//        {
//            if (curMainCtx == null)
//                curMainCtx = new PageContext();
//            var ctx = curMainCtx;
//            ctx.HideMainPageGroup(mainPageGroup);
//        }
//
//        /// <summary>
//        /// 是否有界面正在加载或等待加载
//        /// </summary>
//        public bool IsAnyPageLoading()
//        {
//            foreach (var page in pages.Values)
//            {
//                if (page.IsLoading || page.IsPreloading)
//                {
//                    return true;
//                }
//            }
//
//            return pendingLoadPageAdd.Count > 0;
//        }
//
//
//        /*旧版ShowPageInteral
//
//        internal PageAsyncOperations ShowPageInteral(string prefabName, PageFlags flags, object param, GamePage openBy, string mainPageGroup, int sortingOrder = 0, string sortingLayer = null)
//        {
//            if (curMainCtx == null)
//                curMainCtx = new PageContext();
//            var ctx = curMainCtx;
//            PageAsyncOperations op = new PageAsyncOperations(openBy);
//
//            if (pages.TryGetValue(prefabName, out var page))
//            {
//                if (!visiblePages.Contains(page))
//                {
//                    int finalSortingOrder = ConfigurePageForOpen(page, flags, param, openBy, mainPageGroup, sortingOrder, sortingLayer, op);
//
//                    if (page.IsResReady && !page.IsPreloading)
//                    {
//                        var contentCtx = page.CreateContext();
//                        contentCtx.Parameter = param;
//                        contentCtx.SortingOrder = sortingOrder;
//                        contentCtx.SortingLayer = sortingLayer;
//                        contentCtx.OpenBy = openBy != null ? openBy.PrefabName : null;
//                        contentCtx.AsyncOperations = op;
//                        page.Flags = flags;
//                        page.SetContentContext(contentCtx);
//                        var hasPreload = page.UILogic != null && (page.UILogic.NeedPreload || page.UILogic.HasComponentPreload);
//                        if (hasPreload)
//                        {
//                            page.IsClosing = false;
//                            int pageSortingOrder = 0;
//                            if (!page.IsFreePage)
//                            {
//                                if (page.IsPopup)
//                                {
//                                    ctx.OnOpenPopup(page, openBy, param);
//                                    pageSortingOrder = curMainCtx.IncreasePopupOrder();
//                                }
//                                else
//                                {
//                                    curMainCtx.OnOpenMainPage(page, openBy, param, mainPageGroup);
//                                    var pageStack = curMainCtx.GetCurrentMainPageStack(mainPageGroup, true);
//                                    pageSortingOrder = pageStack.StackSortingOrder;
//                                }
//                            }
//                            page.SetPendingOnLoad((logic) => InitializeGamePage(page, param, openBy, mainPageGroup, false, pageSortingOrder));
//                            page.DoPreload();
//                        }
//                        else
//                        {
//                            int pageSortingOrder = 0;
//                            if (!page.IsFreePage)
//                            {
//                                if (page.IsPopup)
//                                {
//                                    ctx.OnOpenPopup(page, openBy, param);
//                                    pageSortingOrder = curMainCtx.IncreasePopupOrder();
//                                }
//                                else
//                                {
//                                    curMainCtx.OnOpenMainPage(page, openBy, param, mainPageGroup);
//                                    var pageStack = curMainCtx.GetCurrentMainPageStack(mainPageGroup, true);
//                                    pageSortingOrder = pageStack.StackSortingOrder;
//                                }
//                            }
//                            InitializeGamePage(page, param, openBy, mainPageGroup, false, pageSortingOrder);
//                        }
//                    }
//                    else
//                    {
//                        if (page.IsClosing)
//                        {
//                            page.Flags = flags;
//                            page.IsClosing = false;
//                            page.SetContentContext(param, openBy != null ? openBy.PrefabName : null, op, sortingOrder, sortingLayer);
//
//                            int pageSortingOrder = 0;
//                            if (!page.IsFreePage)
//                            {
//                                if (page.IsPopup)
//                                {
//                                    ctx.OnOpenPopup(page, openBy, param);
//                                    pageSortingOrder = curMainCtx.IncreasePopupOrder();
//                                }
//                                else
//                                {
//                                    curMainCtx.OnOpenMainPage(page, openBy, param, mainPageGroup);
//                                    var pageStack = curMainCtx.GetCurrentMainPageStack(mainPageGroup, true);
//                                    pageSortingOrder = pageStack.StackSortingOrder;
//                                }
//                            }
//                            page.OnLoaded = () => InitializeGamePage(page, param, openBy, mainPageGroup,false,pageSortingOrder);
//                        }
//                        else if (page.IsPreloading)
//                            Logger.Warn($"{prefabName} is preloading, cannot be opened again");
//                        else
//                        {
//                            //page.IsResReady == false
//                            page.Flags = flags;
//                            page.SetContentContext(param, openBy != null ? openBy.PrefabName : null, op, sortingOrder, sortingLayer);
//
//                            int pageSortingOrder = 0;
//                            if (!page.IsFreePage)
//                            {
//                                if (page.IsPopup)
//                                {
//                                    ctx.OnOpenPopup(page, openBy, param);
//                                    pageSortingOrder = curMainCtx.IncreasePopupOrder();
//                                }
//                                else
//                                {
//                                    curMainCtx.OnOpenMainPage(page, openBy, param, mainPageGroup);
//                                    var pageStack = curMainCtx.GetCurrentMainPageStack(mainPageGroup, true);
//                                    pageSortingOrder = pageStack.StackSortingOrder;
//                                }
//                            }
//                            page.OnLoaded = () => InitializeGamePage(page, param, openBy, mainPageGroup,false,pageSortingOrder);
//                        }
//                    }
//                }
//                else
//                {
//                    if (page.Flags != flags)
//                    {
//                        throw new NotSupportedException($"{prefabName} is opened as {page.Flags}, cannot be reopened as {flags}");
//                    }
//                    var curCtx = page.GetContentContext;
//                    curCtx.Parameter = param;
//                    curCtx.OpenBy = openBy != null ? openBy.PrefabName : null;
//                    curCtx.AsyncOperations = op;
//                    page.DoReopen(param);
//                    if (!page.RenderVisible)
//                    {
//                        page.RenderVisible = true;
//                    }
//                    op.FinishLoad(page.UILogic);
//                    return op;
//                }
//            }
//            else
//            {
//                bool opening = false;
//                for (int i = 0; i < pendingAdd.Count; i++)
//                {
//                    var p = pendingAdd[i];
//                    if (p.PrefabName == prefabName)
//                    {
//                        opening = true;
//                        break;
//                    }
//                }
//                if (opening)
//                    return op;
//                page = new GamePage(prefabName);
//                page.CurrentContext = ctx;
//                page.Flags = flags;
//                page.SetContentContext(param, openBy != null ? openBy.PrefabName : null, op, sortingOrder, sortingLayer);
//
//                int pageSortingOrder = 0;
//                if (!page.IsFreePage)
//                {
//                    if (page.IsPopup)
//                    {
//                        ctx.OnOpenPopup(page, openBy, param);
//                        pageSortingOrder = curMainCtx.IncreasePopupOrder();
//                    }
//                    else
//                    {
//                        curMainCtx.OnOpenMainPage(page, openBy, param, mainPageGroup);
//                        var pageStack = curMainCtx.GetCurrentMainPageStack(mainPageGroup, true);
//                        pageSortingOrder = pageStack.StackSortingOrder;
//                    }
//                }
//                page.OnLoaded = () => InitializeGamePage(page, param, openBy, mainPageGroup,false,pageSortingOrder);
//                if (isUpdating)
//                    pendingAdd.Add(page);
//                else
//                {
//                    pages[prefabName] = page;
//                    page.Load();
//                }
//            }
//            return op;
//        }
//
//        */
//
//        public string GetStacksLog()
//        {
//            if (curMainCtx == null)
//                return null;
//            return curMainCtx.GetStacks();
//        }
//
//        internal void ShowPageInteral(string prefabName, PageFlags flags, object param, bool clearPrevious, string mainPageGroup, int sortingOrder = 0, string sortingLayer = null)
//        {
//            Burner.Logger.Log($"打开页面 '{prefabName}'。");
//
//            GamePage page = GetGamePage(prefabName);
//            bool isNewlyCreated = false;
//
//            if (page == null)
//            {
//                page = new GamePage(prefabName);
//                page.Flags = flags;
//                isNewlyCreated = true;
//            }
//            else
//            {
//                if (page.Flags != flags)
//                {
//                    throw new NotSupportedException($"页面 '{prefabName}' 已作为 '{page.Flags}' 类型打开，不能重新以 '{flags}' 类型打开。");
//                }
//            }
//
//
//            if (page.IsFreePage)
//            {
//                page.CurrentContext = null;
//                page.RefreshFreeContext(param, sortingOrder, sortingLayer);
//            }
//            else
//            {
//                if (curMainCtx == null)
//                    curMainCtx = new PageContext();
//
//                bool success = curMainCtx.RegisterPage(page, param, clearPrevious, mainPageGroup);
//
//                if (!success)
//                {
//                    Logger.Error($"RegisterPage failed,popup name is  {page.PrefabName}");
//                    return;
//                }
//            }
//
//            // if (!visiblePages.Contains(page))
//            page.RequestShow(param);
//
//
//            if (isNewlyCreated)
//            {
//                if (isUpdating)
//                {
//                    pendingLoadPageAdd.Add(page);
//                }
//                else
//                {
//                    pages[prefabName] = page;
//                    page.Load();
//                }
//            }
//
//            onPreparePageOpen?.Invoke(page);
//        }
//
//        internal void OnInitializeGamePageBegin(GamePage page,bool needRemove)
//        {
//            if (needRemove)
//            {
//                pendingCloseEvents.Remove(page);
//            }
//            openingCnt++;
//        }
//
//        internal void OnInitializeGamePageFinish(GamePage page)
//        {
//            openingCnt--;
//            if (openingCnt <= 0)
//            {
//                openingCnt = 0;
//                CheckAndDispatchPendingCloseEvent();
//            }
//            onFinalizePageOpen?.Invoke(page);
//        }
//
//        public void RegisterLogicResolver(ILogicResolver resolver)
//        {
//            if (resolvers.Contains(resolver))
//            {
//                Logger.Error("Cannot register logic resolver twice");
//                return;
//            }
//            resolvers.Add(resolver);
//        }
//
//        internal void ReportPageShown(GamePage page)
//        {
//            visiblePages.Add(page);
//#pragma warning disable CS0618
//            OnPageOpen?.Invoke(page.PrefabName);
//#pragma warning enable CS0618
//            onPageOpenCb?.Invoke(page.UILogic);
//            page.LoadTiming.FirstOpened = false;
//        }
//
//        internal void ReportPageHidden(GamePage page)
//        {
//            visiblePages.Remove(page);
//
//            if (openingCnt <= 0)
//            {
//#pragma warning disable CS0618
//                OnPageClose?.Invoke(page.PrefabName);
//#pragma warning enable CS0618
//                onPageCloseCb?.Invoke(page.UILogic);
//            }
//            else
//            {
//                page.PendingCloseEvent = true;
//                pendingCloseEvents.Add(page);
//            }
//        }
//
//        public void ReportPageStartLoad(string pageName)
//        {
//            onPageLoadCb?.Invoke(pageName);
//        }
//
//        public void ReportPageLoadFinish(string pageName)
//        {
//            onPageLoadFinishCb?.Invoke(pageName);
//        }
//
//        public void SetOnPageOpenHandler(Action<GameUILogic> cb)
//        {
//            onPageOpenCb = cb;
//        }
//
//        public void SetOnPageCloseHandler(Action<GameUILogic> cb)
//        {
//            onPageCloseCb = cb;
//        }
//
//        public void SetOnPageStartLoadHandler(Action<string> cb)
//        {
//            onPageLoadCb = cb;
//        }
//
//        public void SetOnPageLoadFinishHandler(Action<string> cb)
//        {
//            onPageLoadFinishCb = cb;
//        }
//
//        public void SetOnPreparePageOpenHandler(Action<GamePage> cb)
//        {
//            onPreparePageOpen = cb;
//        }
//
//        public void SetOnPreparePageCloseHandler(Action<GamePage> cb)
//        {
//            onPreparePageClose = cb;
//        }
//
//        public void SetOnFinalizePageOpenHandler(Action<GamePage> cb)
//        {
//            onFinalizePageOpen = cb;
//        }
//
//        public void SetOnFinalizePageCloseHandler(Action<GamePage> cb)
//        {
//            onFinalizePageClose = cb;
//        }
//
//        internal void ReportDispose(GamePage page)
//        {
//            if (!isUpdating)
//            {
//                pages.Remove(page.PrefabName);
//                visiblePages.Remove(page);
//            }
//            else
//            {
//                pendingLoadPageDelete.Add(page);
//            }
//        }
//
//        public void Update()
//        {
//            isUpdating = true;
//
//            bool resolutionChanged = false;
//            if (AutoAdjustCanvasScaler)
//            {
//                if (Screen.width != displayResolution.x || Screen.height != displayResolution.y)
//                {
//                    displayResolution = new Vector2Int(Screen.width, Screen.height);
//                    resolutionChanged = true;
//                    OnScreenResolutionChanged?.Invoke();
//                }
//            }
//
//            Profiler.BeginSample("BurnerUIManager_UpdatePages");
//            foreach (var i in pages)
//            {
//                var page = i.Value;
//
//                try
//                {
//                    Profiler.BeginSample("BurnerUIManager_AdjustCanvasScaler");
//                    if (resolutionChanged && page.GameObject)
//                        AdjustCanvasScaler(page.GameObject);
//                    Profiler.EndSample();
//                    Profiler.BeginSample(page.PrefabName);
//                    page.OnUpdate();
//                    Profiler.EndSample();
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error($"Page {page.PrefabName} update failed\n{ex.ToString()}");
//                }
//            }
//            Profiler.EndSample();
//            isUpdating = false;
//
//            ProcessPendingAddAndRemove();
//            accumulatedTime = 0;
//        }
//
//        public void LateUpdate()
//        {
//            isUpdating = true;
//
//            Profiler.BeginSample("BurnerUIManager_LateUpdatePages");
//            foreach (var i in pages)
//            {
//                var page = i.Value;
//
//                try
//                {
//                    Profiler.BeginSample(page.PrefabName);
//                    page.OnLateUpdate();
//                    Profiler.EndSample();
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error($"Page {page.PrefabName} late update failed\n{ex.ToString()}");
//                }
//            }
//            isUpdating = false;
//
//            Profiler.BeginSample("BurnerUIManager_ProcessPendingAddAndRemove");
//            ProcessPendingAddAndRemove();
//            Profiler.EndSample();
//
//            Profiler.EndSample();
//        }
//
//        void ProcessPendingAddAndRemove()
//        {
//            if (pendingLoadPageDelete.Count > 0)
//            {
//                foreach (var i in pendingLoadPageDelete)
//                {
//                    pages.Remove(i.PrefabName);
//                    visiblePages.Remove(i);
//                }
//                pendingLoadPageDelete.Clear();
//            }
//            if (pendingLoadPageAdd.Count > 0)
//            {
//                foreach (var i in pendingLoadPageAdd)
//                {
//                    pages[i.PrefabName] = i;
//                    i.Load();
//                }
//                pendingLoadPageAdd.Clear();
//            }
//        }
//
//        public GameUILogic MakeUILogic(string className)
//        {
//            foreach (var resolver in resolvers)
//            {
//                var logic = resolver.ResolveAndCreateLogic(className);
//                if (logic != null)
//                    return logic;
//            }
//
//            Logger.Error("Cannot resolve UI Logic:" + className);
//            return null;
//        }
//
//        public IUIBehaviour CreateBehaviourByComponentName(string compName)
//        {
//            if (extensionComps.TryGetValue(compName, out var info))
//            {
//                return Activator.CreateInstance(info.GameUIComponentType) as IUIBehaviour;
//            }
//            else
//            {
//                Logger.Error($"Cannot find extension:{compName}");
//                return null;
//            }
//        }
//
//        /// <summary>
//        /// 设置是否需要在开始加载时派发OnBeginLoad生命周期，如需派发需要设置一个pageName->className的回调解析UILogic
//        /// </summary>
//        /// <param name="clsNameCallback"></param>
//        public void SetNeedBeginLoad(Func<string, string> clsNameCallback)
//        {
//            GetClassNameCallback = clsNameCallback;
//        }
//
//        internal NodePostProcessManager GetNodeProcessManager()
//        {
//            if (!nodePPMgr)
//                nodePPMgr = rootNode.AddComponent<NodePostProcessManager>();
//            return nodePPMgr;
//        }
//
//        bool IsPageReadyForRT(GamePage page)
//        {
//            if (page.IsClosing || page.IsDisposed || !page.Visible || page.IsLoading || !page.GameObject)
//                return false;
//            if (page.UILogic == null || !page.UILogic.VisibleInHierarchy)
//                return false;
//            return true;
//        }
//        public (List<GamePage>, List<RectTransform>) GetTransformList(GamePage exclude = null)
//        {
//            List<RectTransform> list = new List<RectTransform>();
//            List<GamePage> pList = new List<GamePage>();
//            var ctx = BurnerUIManager.Instance.GetPageContext();
//            var stack = ctx.MainPageList;
//            if (stack.Stack.Count > 0)
//            {
//                if(stack.Stack.Peek(out var top))
//                {
//                    if (top.Page != exclude && IsPageReadyForRT(top.Page))
//                    {
//                        var rt = top.Page.GameObject?.transform as RectTransform;
//                        if (rt)
//                        {
//                            list.Add(rt);
//                            pList.Add(top.Page);
//                        }
//                    }
//                }
//            }
//
//            if (ctx.MainPageGroups != null)
//            {
//                foreach (var i in ctx.MainPageGroups)
//                {
//                    if (i.Value.Stack.Peek(out var top))
//                    {
//                        if (top.Page != exclude && IsPageReadyForRT(top.Page))
//                        {
//                            var rt = top.Page.GameObject?.transform as RectTransform;
//                            if (rt)
//                            {
//                                list.Add(rt);
//                                pList.Add(top.Page);
//                            }
//                        }
//                    }
//                }
//            }
//
//            //ctx.Popups.Sort((a, b) =>
//            //{
//            //    return a.Page.SortingOrder - b.Page.SortingOrder;
//            //});
//            var popups = ctx.GetCurPopupsByGroup();
//            if (popups != null)
//            {
//                foreach (var i in popups)
//                {
//                    if (i.Page != exclude && IsPageReadyForRT(i.Page))
//                    {
//                        var rt = i.Page.GameObject?.transform as RectTransform;
//                        if (rt)
//                        {
//                            list.Add(rt);
//                            pList.Add(i.Page);
//                        }
//                    }
//                }
//            }
//            return (pList, list);
//        }
//
//        public NodeEffectHandle EnableBlurPages(RawImage img, int downSampleNum = 1, float blurSpreadSize = 1, int blurIterations = 2, GameUILogic excludePageLogic = null, float RTScale = 1f)
//        {
//            if (excludePageLogic != null && (!excludePageLogic.IsPage || excludePageLogic.IsSubPage))
//                throw new NotSupportedException();
//            var (pList, list) = BurnerUIManager.Instance.GetTransformList(excludePageLogic.Page);
//            var mgr = BurnerUIManager.Instance.GetNodeProcessManager();
//            Rect rect = default;
//            mgr.EnableNodeBlur(list, ref rect, img, downSampleNum, blurSpreadSize, blurIterations, RTScale);
//            return new NodeEffectHandle(mgr, pList, img, null, false);
//        }
//
//        public NodeEffectHandle EnableNodeBlurEffect(IUIBehaviour behaviour, GameRawImage rawImg, int downSampleNum = 1, float blurSpreadSize = 1, int blurIterations = 2)
//        {
//            var mgr = GetNodeProcessManager();
//            List<RectTransform> nodes = new List<RectTransform>();
//            if (behaviour is GameUIComponent comp)
//            {
//                nodes.Add(comp.Widget);
//            }
//            else
//                nodes.Add(((GameUILogic)behaviour).UIComponent.Widget);
//            Rect rect = default;
//            mgr.EnableNodeBlur(nodes, ref rect, rawImg.RawImage, downSampleNum, blurSpreadSize, blurIterations);
//
//            return new NodeEffectHandle(mgr, null, rawImg.RawImage, null, false);
//        }
//
//        public ScreenShotHandle GetScreenShot()
//        {
//            var (_, list) = GetTransformList();
//            Rect rect = default;
//            RenderTexture rt = null;
//            NodePostProcessManager.GetNodeShotImage(list, ref rt, ref rect, false);
//
//            return new ScreenShotHandle(rt, false);
//        }
//
//        /// <summary>
//        /// 动态调整 CanvasScaler 的 matchWidthOrHeight，根据当前屏幕宽高比与参考分辨率的关系自动切换匹配策略。
//        /// 仅对 screenMatchMode = MatchWidthOrHeight 的 CanvasScaler 生效。
//        /// </summary>
//        public static void AdjustCanvasScaler(GameObject go)
//        {
//            if (go)
//            {
//                var scaler = go.GetComponent<CanvasScaler>();
//                if (scaler && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
//                {
//                    var sWToH = scaler.referenceResolution.x / scaler.referenceResolution.y;
//                    var vWToH = (float)Screen.width / Screen.height;
//                    if ((vWToH > 1 && sWToH < 1) || (vWToH < 1 && sWToH > 1))
//                    {
//                        scaler.referenceResolution = new Vector2(scaler.referenceResolution.y, scaler.referenceResolution.x);
//                        sWToH = 1 / sWToH;
//                    }
//                    float res;
//                    if (Screen.width < Screen.height)
//                    {
//                        if (vWToH > (9f / 16f))
//                            res = 1;
//                        else
//                            res = 0;
//                    }
//                    else
//                    {
//                        if (vWToH > (16f / 9f))
//                            res = 1;
//                        else
//                            res = 0;
//                    }
//                    scaler.matchWidthOrHeight = res;
//
//                    Instance?.OnAdjustCanvasScaler?.Invoke(scaler);
//                }
//            }
//        }
//
//        internal bool TryConsumeTimeSlice(out LoadTimeToken token)
//        {
//            token = default;
//            if (accumulatedTime < MaximalFrameTimeBudget)
//            {
//                token = new LoadTimeToken(sw, this);
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }
//
//        internal void RegisterTimeSliceUsed(int usedMiliseconds)
//        {
//            accumulatedTime += usedMiliseconds;
//        }
//    }
//
//    public class ScreenShotHandle : IDisposable
//    {
//        RenderTexture rt;
//        bool isTempRT;
//        internal ScreenShotHandle(RenderTexture rt, bool isTempRT)
//        {
//            this.rt = rt;
//            this.isTempRT = isTempRT;
//        }
//
//        public RenderTexture ScreenShotTexture => rt;
//
//        public void Dispose()
//        {
//            if (rt)
//            {
//                if (isTempRT)
//                    RenderTexture.ReleaseTemporary(rt);
//                else
//                    Texture2D.DestroyImmediate(rt);
//            }
//        }
//    }
//    public class NodeEffectHandle : IDisposable
//    {
//        List<GamePage> nodes;
//        RawImage img;
//        NodePostProcessManager mgr;
//        GamePage page;
//        bool hideOther;
//        internal NodeEffectHandle(NodePostProcessManager mgr, List<GamePage> nodes, RawImage img, GamePage page, bool hideOther)
//        {
//            this.mgr = mgr;
//            this.nodes = nodes;
//            this.img = img;
//            this.page = page;
//            this.hideOther = hideOther;
//        }
//
//        public void Dispose()
//        {
//            NodePostProcessManager.DisableNodeBlur(img);
//            if (hideOther)
//            {
//                if (page.ShouldHideLowerPage)
//                {
//                    page.ShouldHideLowerPage = false;
//                }
//                else
//                {
//                    if (nodes != null)
//                    {
//                        foreach (var i in nodes)
//                        {
//                            if (!i.IsDisposed && i.Visible && i.GameObject && !i.GameObject.activeSelf)
//                                i.GameObject.SetActive(true);
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
