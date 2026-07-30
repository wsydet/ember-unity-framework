//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using System.Linq;
//using Coffee.UIExtensions;
//using Burner.Basic;
//using UnityEngine;
//using Burner.Extensions;
//using Burner.Basic.Tasks;
//using Burner.UIExtension.Utils;
//using UnityEngine.UI;
//using UnityEngine.Profiling;
//
//namespace Burner.UIExtension
//{
//    public class PageAsyncOperations : ILoaderHandle
//    {
//        STTaskCompletionSource<GameUILogic> loadTCS;
//        STTaskCompletionSource<object> closeTCS;
//        GamePage openBy;//仅给SubPage使用
//        GameUILogic pendingOnShow;
//        bool ready;
//        (bool invoked, object returnVal) pendingOnClose;
//
//        public PageAsyncOperations(GamePage openby = null)
//        {
//            this.openBy = openby;
//        }
//
//        public STTask<GameUILogic> OnShow
//        {
//            get
//            {
//                if (pendingOnShow != null)
//                    return STTask.FromResult(pendingOnShow);
//                if (loadTCS == null)
//                    loadTCS = new STTaskCompletionSource<GameUILogic>();
//                return loadTCS.Task;
//            }
//        }
//
//        public STTask<object> OnClose
//        {
//            get
//            {
//                if (pendingOnClose.invoked)
//                    return STTask.FromResult(pendingOnClose.returnVal);
//                if (closeTCS == null)
//                    closeTCS = new STTaskCompletionSource<object>();
//                return closeTCS.Task;
//            }
//        }
//
//        public async void SetOnShow(Action action)
//        {
//            if (action == null)
//                return;
//            try
//            {
//                await OnShow;
//                action();
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Error(ex.ToString());
//            }
//        }
//
//        public async void SetOnClose(Action<object> action)
//        {
//            if (action == null)
//                return;
//            try
//            {
//                var returnVal = await OnClose;
//                action(returnVal);
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Error(ex.ToString());
//            }
//        }
//
//        internal void FinishLoad(GameUILogic page, bool setReadyOnly = false)
//        {
//            ready = true;
//            if (setReadyOnly)
//                return;
//            if (loadTCS != null)
//            {
//                loadTCS.SetResult(page);
//            }
//            else
//            {
//                pendingOnShow = page;
//            }
//        }
//
//        internal void FinishClose(object val)
//        {
//            if (closeTCS != null)
//            {
//                if (openBy != null && !openBy.Visible)
//                {
//                    if (openBy.IsClosing || openBy.Disposed)
//                        return;
//                    openBy.PendingOnShow = () => closeTCS.SetResult(val);
//                }
//                else
//                    closeTCS.SetResult(val);
//            }
//            else
//            {
//                pendingOnClose = (true, val);
//            }
//        }
//
//        public bool Ready()
//        {
//            return ready;
//        }
//
//        public bool IsDisposed()
//        {
//            return false;
//        }
//    }
//
//    [Flags]
//    public enum PageFlags
//    {
//        None,
//        MainPage = 1,
//        Popup = 2,
//        TopMost = 4,
//        SubPage = 8,
//        FreePage = 16,
//    }
//
//    internal enum LoadStages
//    {
//        None,
//        OnResLoad,
//        OnInit,
//        OnLoad,
//        OnBecomeVisible,
//        Loaded
//    }
//
//    public class PageLoadTiming
//    {
//        internal float LoadStartTime { get; private set; }
//
//        public float TotalLoadTime { get; private set; }
//
//        public float AssetLoadTime { get; private set; }
//
//        internal float InitStartTime { get; private set; }
//
//        public float InitTime { get; private set; }
//
//        internal float OpenStartTime { get; private set; }
//        public bool FirstOpened { get; internal set; } = true;
//
//        public float OpenTime { get; internal set; }
//
//        internal PageLoadTiming()
//        {
//        }
//
//        internal void StartLoad()
//        {
//            if (FirstOpened)
//                LoadStartTime = Time.realtimeSinceStartup;
//        }
//
//        internal void FinishLoad()
//        {
//            if (FirstOpened)
//                AssetLoadTime = Time.realtimeSinceStartup - LoadStartTime;
//        }
//
//        internal void StartInit()
//        {
//            if (FirstOpened)
//                InitStartTime = Time.realtimeSinceStartup;
//        }
//
//        internal void FinishInit()
//        {
//            if (FirstOpened)
//                InitTime = Time.realtimeSinceStartup - InitStartTime;
//        }
//
//        internal void StartOpen()
//        {
//            if (FirstOpened)
//                OpenStartTime = Time.realtimeSinceStartup;
//        }
//
//        internal void FinishOpen()
//        {
//            if (FirstOpened)
//            {
//                OpenTime = Time.realtimeSinceStartup - OpenStartTime;
//                TotalLoadTime = Time.realtimeSinceStartup - LoadStartTime;
//            }
//        }
//    }
//
//    public class GamePage
//    {
//        private static int hideUILayer = LayerMask.NameToLayer("UIHiden");
//        private static int defaultUILayer = LayerMask.NameToLayer("UI");
//
//        IResourceHandle resHandle;
//        GameObject go;
//        Canvas canvas;
//        private GraphicRaycaster canvasRaycaster;
//        private Canvas[] childCanvases;
//        string prefabName;
//        bool isLoading;
//        bool visible = false;
//        bool renderVisible = true;
//        bool isClosing = false;
//        float closeTime = 0;
//        bool disposed = false;
//        float curPlaneDistance = 0;
//        int curSubPageOrder;
//        int initialSortingOrder = -1;
//        const int SubPageOrderGrowStep = 50;
//        PageFlags flags;
//        GameUILogic logic;
//        bool isUpdating = false;
//        bool hasCanvas = true;
//        bool pendingPreload = false;
//        bool pendingOnOpen = false;
//        // GamePage waitingToClose = null;
//        Dictionary<string, GamePage> subPages;
//        List<GamePage> pendingDelete;
//        ICanvasSortingOrderHandler[] sortingorderHandlers;
//        GameUIComponent parentComponent;
//        NodeEffectHandle blurEffectHandle;
//        Action<GameUILogic> pendingOnLoad;
//
//        IResourceLoader currentLoader;
//        LoadStages curStage;
//        LoadStages pendingStage;
//        PageLoadTiming loadTiming;
//        bool shouldHideLowerPage;
//        private RenderMode defaultRenderMode;
//        BurnerSafeArea safeArea;
//        bool hasSafeArea = true;
//        private Animator animator;
//        public static HashSet<string> ShowHistory = new();
//        private static int inStateHash = Animator.StringToHash("In");
//        private static int aniStateHash = Animator.StringToHash("Ani");
//
//        struct ContentContextHolder
//        {
//            public object Parameter;
//            public string OpenBy;
//            public PageAsyncOperations AsyncOperations;
//            public int SortingOrder;
//            public string SortingLayer;
//        }
//
//        public string PageGroup { get; set; }
//
//        public PageLoadTiming LoadTiming => loadTiming;
//
//        public GameObject GameObject => go;
//
//        internal Action PendingOnShow { get; set; }
//        internal bool SkipOnHideOnHideLower { get; set; }
//
//        public Canvas Canvas
//        {
//            get
//            {
//                if (!canvas && hasCanvas)
//                {
//                    if (!go)
//                    {
//                        Logger.Error(
//                            $"go is null?????????? page={PrefabName} isClosing={isClosing} isDisposed={IsDisposed} isLoading={isLoading}");
//
//                        return null;
//                    }
//
//                    canvas = go.GetComponent<Canvas>();
//                    canvasRaycaster = go.GetComponent<GraphicRaycaster>();
//                    childCanvases = go.GetComponentsInChildren<Canvas>();
//                    hasCanvas = canvas;
//                }
//
//                return canvas;
//            }
//        }
//
//        public BurnerSafeArea SafeArea
//        {
//            get
//            {
//                if (!safeArea && hasSafeArea)
//                {
//                    if (!go)
//                    {
//                        Logger.Error(
//                            $"go is null?????????? page={PrefabName} isClosing={isClosing} isDisposed={IsDisposed} isLoading={isLoading}");
//                    }
//
//                    safeArea = go.GetComponentInChildren<BurnerSafeArea>();
//                    hasSafeArea = safeArea;
//                }
//
//                return safeArea;
//            }
//        }
//
//        public RectTransform SafeAreaRoot
//        {
//            get
//            {
//                if (SafeArea)
//                {
//                    return SafeArea.SafeAreaRoot;
//                }
//
//                if (Canvas)
//                {
//                    return Canvas.transform as RectTransform;
//                }
//
//                return go ? go.transform as RectTransform : null;
//            }
//        }
//
//        internal bool HasSafeArea
//        {
//            get
//            {
//                if (!safeArea && hasSafeArea)
//                {
//                    if (!go)
//                    {
//                        Logger.Error(
//                            $"go is null?????????? page={PrefabName} isClosing={isClosing} isDisposed={IsDisposed} isLoading={isLoading}");
//                    }
//
//                    safeArea = go.GetComponentInChildren<BurnerSafeArea>();
//                    hasSafeArea = safeArea;
//                }
//
//                return hasSafeArea;
//            }
//        }
//
//        public bool RenderVisible
//        {
//            get => renderVisible;
//            set
//            {
//                if (renderVisible != value)
//                {
//                    renderVisible = value;
//                    if (UILogic != null)
//                    {
//                        UILogic.DoChangeRenderVisible(value);
//                        if (value)
//                        {
//                            if (UILogic.Visible)
//                                UILogic.DoShow(SkipOnHideOnHideLower);
//                            if (subPages != null)
//                            {
//                                foreach (var i in subPages)
//                                {
//                                    var l = i.Value.UILogic;
//                                    if (l != null && l.Visible)
//                                        l.DoShow(SkipOnHideOnHideLower || l.SkipOnHideOnHideLower);
//                                }
//                            }
//                        }
//                        else
//                        {
//                            if (UILogic.Visible)
//                                UILogic.DoHide(SkipOnHideOnHideLower);
//                            if (subPages != null)
//                            {
//                                foreach (var i in subPages)
//                                {
//                                    var l = i.Value.UILogic;
//                                    if (l != null && l.Visible)
//                                        l.DoHide(SkipOnHideOnHideLower || l.SkipOnHideOnHideLower);
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }
//
//        public bool ShouldHideLowerPage
//        {
//            get => shouldHideLowerPage;
//            set
//            {
//                if (shouldHideLowerPage != value)
//                {
//                    shouldHideLowerPage = value;
//                    DoHideOtherPage(value, false);
//                }
//            }
//        }
//
//        internal void DoHideOtherPage(bool hide, bool force)
//        {
//            if ((visible || force) && CurrentContext != null)
//            {
//                bool allShown = true;
//                GamePage curCovering = null;
//                bool coveredByOther = false;
//                if (hide)
//                    coveredByOther = CurrentContext.HideLowerPage(this);
//                else
//                    (allShown, curCovering) = CurrentContext.ShowLowerPage(this);
//                if (curCovering == null || hide)
//                {
//                    UILogic?.OnHideOtherPage(hide, allShown, coveredByOther);
//                }
//                else
//                {
//                    UILogic?.OnHideOtherPage(hide, allShown, coveredByOther);
//                    //如果下方还有其他需要隐藏下方的PopUp，则应该转为由下方UI再次隐藏其他页面
//                    curCovering.UILogic?.OnHideOtherPage(true, false, false);
//                }
//            }
//        }
//
//        internal bool HasCanvas
//        {
//            get
//            {
//                if (!canvas && hasCanvas)
//                {
//                    if (!go)
//                    {
//                        Logger.Warn(
//                            $"go is null?????????? page={PrefabName} isClosing={isClosing} isDisposed={IsDisposed} isLoading={isLoading}");
//                        return false;
//                    }
//
//                    canvas = go.GetComponent<Canvas>();
//                    canvasRaycaster = go.GetComponent<GraphicRaycaster>();
//                    childCanvases = go.GetComponentsInChildren<Canvas>();
//                    hasCanvas = canvas;
//                }
//
//                return hasCanvas;
//            }
//        }
//
//        public float PlaneDistance
//        {
//            get { return curPlaneDistance; }
//            set
//            {
//                curPlaneDistance = value;
//                Canvas.planeDistance = value;
//            }
//        }
//
//        public void ResetPlanceDistance()
//        {
//            Canvas.planeDistance = curPlaneDistance;
//        }
//
//        public GamePage(string prefabName)
//        {
//            this.prefabName = prefabName;
//            loadTiming = new PageLoadTiming();
//            ContentContext = new PageContentContext();
//        }
//
//        public PageFlags Flags
//        {
//            get => flags;
//            internal set { flags = value; }
//        }
//
//        internal GamePage ParentPage { get; set; }
//        internal bool PendingCloseEvent { get; set; }
//
//        internal bool NeedDispose { get; set; }
//
//        public string PrefabName => prefabName;
//
//        public bool IsLoading => isLoading;
//
//        public bool IsPreloading => pendingPreload;
//
//        public bool Disposed => disposed;
//
//        public bool IsClosing
//        {
//            get { return isClosing; }
//        }
//
//        public bool IsDisposed => disposed;
//
//        public bool IsMainPage => (flags & PageFlags.MainPage) == PageFlags.MainPage;
//
//        public bool IsSubPage => (flags & PageFlags.SubPage) == PageFlags.SubPage;
//
//        public bool IsPopup => (flags & PageFlags.Popup) == PageFlags.Popup;
//
//        public bool IsTopMost => (flags & PageFlags.TopMost) == PageFlags.TopMost;
//
//        public bool IsFreePage => (flags & PageFlags.FreePage) == PageFlags.FreePage;
//
//        public bool AutoDestroy { get; set; } = true;
//
//        public float DestroyValue { get; set; } = BurnerUIManager.Instance.DefaultDestoryDelay;
//
//        public GameUILogic UILogic => logic;
//
//        public int SortingOrder
//        {
//            get { return HasCanvas ? Canvas.sortingOrder : initialSortingOrder; }
//            set
//            {
//                if (initialSortingOrder < 0)
//                {
//                    initialSortingOrder = value;
//                    curSubPageOrder = initialSortingOrder + SubPageOrderGrowStep;
//                }
//
//                Canvas.sortingOrder = value;
//
//                NotifySortingLayerChange();
//            }
//        }
//
//        public string SortingLayer
//        {
//            get { return HasCanvas ? Canvas.sortingLayerName : null; }
//            set
//            {
//                if (HasCanvas && Canvas.sortingLayerName != value)
//                {
//                    Canvas.sortingLayerName = value;
//
//                    NotifySortingLayerChange();
//                }
//            }
//        }
//
//        void NotifySortingLayerChange()
//        {
//            var cnt = sortingorderHandlers.Length;
//            for (int i = 0; i < cnt; i++)
//            {
//                //可能包含动态加载的组件，因此这里有可能已经卸载
//                if (!((UnityEngine.Object)sortingorderHandlers[i]))
//                    continue;
//                sortingorderHandlers[i].UpdateSortingOrder();
//            }
//        }
//
//        public Action OnPostponeSetActive { get; set; }
//        private Action OnLoaded { get; set; }
//
//        private Func<GameUILogic> OnCreateLogic { get; set; }
//
//        internal void SetPendingOnLoad(Action<GameUILogic> onload)
//        {
//            pendingOnLoad = onload;
//        }
//
//        public PageAsyncOperations ShowSubPage(string prefabName, object param, GameUIComponent parentComponent = null,
//            Action<GameUILogic> onLoad = null, Func<GameUILogic> createLogicCb = null)
//        {
//            //if (IsSubPage)
//            //    throw new NotSupportedException("Subpage cannot open another subpage");
//            if (subPages == null)
//                subPages = new Dictionary<string, GamePage>();
//            PageAsyncOperations op = new PageAsyncOperations(this);
//
//            if (currentLoader != null)
//                currentLoader.ListenHandle(op);
//
//            if (subPages.TryGetValue(prefabName, out var page))
//            {
//                //var ctx = page.CreateContext();
//                //ctx.Parameter = param;
//                //ctx.OpenBy = PrefabName;
//                //ctx.AsyncOperations = op;
//                //page.SetContentContext(ctx);
//                page.RefreshSubContext(param, prefabName, op);
//
//                if (page.IsResReady)
//                {
//                    if (!page.Visible && (!page.IsPreloading || page.IsClosing))
//                    {
//                        page.Flags = PageFlags.SubPage;
//                        page.parentComponent = parentComponent;
//                        page.SetParentComponent();
//                        var hasPreload = page.isClosing && page.logic != null &&
//                                         (page.logic.NeedPreload || page.logic.HasComponentPreload);
//                        if (hasPreload)
//                        {
//                            page.pendingOnLoad = (logic) =>
//                            {
//                                if (!Visible)
//                                {
//                                    page.pendingOnOpen = true;
//                                    page.isClosing = false;
//                                }
//                                else
//                                    page.ShowInternal(true);
//
//                                onLoad?.Invoke(logic);
//                            };
//                        }
//
//                        page.isClosing = false;
//                        if (!page.DoPreload())
//                        {
//                            if (!Visible)
//                            {
//                                page.pendingOnOpen = true;
//                                page.isClosing = false;
//                            }
//                            else
//                                page.ShowInternal(true);
//
//                            if (!hasPreload)
//                                onLoad?.Invoke(page.UILogic);
//                        }
//                    }
//                    else
//                    {
//                        if (Visible)
//                        {
//                            var curCtx = page.ContentContext;
//                            curCtx.Parameter = param;
//                            page.DoReopen(param);
//                            if (!page.RenderVisible)
//                            {
//                                page.RenderVisible = true;
//                            }
//
//                            op.FinishLoad(page.UILogic);
//                        }
//                        else
//                        {
//                            Logger.Warn($"{prefabName} is preloading, cannot be opened again");
//                        }
//                    }
//                }
//                else
//                {
//                    if (page.isClosing)
//                    {
//                        page.isClosing = false;
//                    }
//
//                    page.parentComponent = parentComponent;
//                    page.Flags = PageFlags.SubPage;
//                    page.OnLoaded = () => InitializeSubPage(page, param, onLoad);
//                }
//            }
//            else
//            {
//                page = new GamePage(prefabName);
//                page.CurrentContext = CurrentContext;
//                page.Flags = PageFlags.SubPage;
//                page.ParentPage = this;
//                page.OnCreateLogic = createLogicCb;
//                page.parentComponent = parentComponent;
//                subPages[prefabName] = page;
//                page.OnLoaded = () => InitializeSubPage(page, param, onLoad);
//
//                //var ctx = page.CreateContext();
//                //ctx.Parameter = param;
//                //ctx.OpenBy = PrefabName;
//                //ctx.AsyncOperations = op;
//                //page.SetContentContext(ctx);
//                page.RefreshSubContext(param, prefabName, op);
//                page.Load();
//            }
//
//            return op;
//        }
//
//        void InitializeSubPage(GamePage page, object param, Action<GameUILogic> onLoad)
//        {
//            if (page.canvas)
//            {
//                var cur = this;
//                while (cur.IsSubPage)
//                    cur = cur.ParentPage;
//                if (cur == null)
//                {
//                    Burner.Logger.Error($"Cannot find parent page for {this.PrefabName} and {page.PrefabName}");
//                    cur = this;
//                }
//
//                page.SortingOrder = cur.curSubPageOrder;
//                cur.curSubPageOrder = cur.curSubPageOrder + SubPageOrderGrowStep;
//            }
//
//            if (page.UILogic != null && UILogic != null)
//                page.UILogic.Layer = UILogic.Layer;
//            var hasPreload = page.logic != null && (page.logic.NeedPreload || page.logic.HasComponentPreload);
//            if (hasPreload)
//            {
//                page.pendingOnLoad = onLoad;
//            }
//
//            page.SetParentComponent();
//            if (Visible)
//            {
//                if (page.logic != null && page.logic.PostponeSetVisible)
//                {
//                    page.OnPostponeSetActive = () => page.ShowInternal(true,false);
//                    page.DoPostponeSetVisible();
//                }
//                else
//                    page.ShowInternal(true,false);
//            }
//            else
//            {
//                page.pendingOnOpen = true;
//                if (logic != null && logic.PostponeSetVisible)
//                {
//                    page.ContentContext?.AsyncOperations?.FinishLoad(page.UILogic, true);
//                }
//            }
//
//            if (!hasPreload)
//            {
//                onLoad?.Invoke(page.UILogic);
//                page.loadTiming.FirstOpened = false;
//            }
//
//
//            page.ExecutePendingOperationIfAny();
//        }
//
//        public void HideSubPage(string prefabName)
//        {
//            if (subPages != null && subPages.TryGetValue(prefabName, out var page))
//            {
//                page.CloseInternal(false);
//            }
//        }
//
//        public void CloseSubPage(string prefabName)
//        {
//            if (subPages != null && subPages.TryGetValue(prefabName, out var page))
//            {
//                page.CloseInternal(true);
//            }
//        }
//
//        public void CloseAllSubPages()
//        {
//            if (subPages != null)
//            {
//                var arr = subPages.ToArray();
//                foreach (var i in arr)
//                {
//                    CloseSubPage(i.Key);
//                }
//            }
//        }
//
//        public GamePage GetSubPage(string prefabName)
//        {
//            if (subPages != null && subPages.TryGetValue(prefabName, out var page))
//            {
//                return !page.IsClosing ? page : null;
//            }
//            else
//                return null;
//        }
//
//        internal void ReportSubPageClose(GamePage page)
//        {
//            if (subPages != null && subPages.ContainsKey(page.prefabName))
//            {
//                if (isUpdating)
//                {
//                    if (pendingDelete == null)
//                        pendingDelete = new List<GamePage>();
//                    pendingDelete.Add(page);
//                }
//                else
//                {
//                    subPages.Remove(page.prefabName);
//                    RefreshSubPageOrder();
//                }
//            }
//            else
//            {
//                Logger.Error($"Page:{prefabName} doesn't have subpage {page.prefabName}");
//            }
//        }
//
//        void RefreshSubPageOrder()
//        {
//            if (IsSubPage)
//            {
//                ParentPage.RefreshSubPageOrder();
//                return;
//            }
//
//            int maxSubOrder = initialSortingOrder - SubPageOrderGrowStep;
//            GetMaxSortingOrder(ref maxSubOrder);
//            maxSubOrder = Mathf.Max(initialSortingOrder, maxSubOrder);
//            curSubPageOrder = maxSubOrder + SubPageOrderGrowStep;
//        }
//
//        void GetMaxSortingOrder(ref int maxSubOrder)
//        {
//            if (subPages != null)
//            {
//                foreach (var i in subPages)
//                {
//                    if (i.Value.Disposed)
//                    {
//                        pendingDelete.Add(i.Value);
//                        continue;
//                    }
//
//                    if (!i.Value.isLoading && i.Value.HasCanvas)
//                    {
//                        if (i.Value.SortingOrder > maxSubOrder)
//                        {
//                            maxSubOrder = i.Value.SortingOrder;
//                        }
//                    }
//
//                    i.Value.GetMaxSortingOrder(ref maxSubOrder);
//                }
//            }
//        }
//
//
//        public void Load()
//        {
//            if (!isLoading)
//            {
//                isLoading = true;
//                loadTiming.StartLoad();
//                BurnerUIManager.Instance.RequestHighPriorityLoad();
//                if (BurnerUIManager.Instance.GetClassNameCallback != null)
//                {
//                    logic = OnCreateLogic != null
//                        ? OnCreateLogic()
//                        : BurnerUIManager.Instance.MakeUILogic(BurnerUIManager.Instance.GetClassNameCallback(prefabName));
//                    logic.DoBeginLoad();
//                }
//
//                // resHandle = Resource.CacheManager.Instance.GetObject(prefabName, (IResourceHandle handle)=>
//                //     {
//                //         DG.Tweening.DOVirtual.DelayedCall(2f, () =>
//                //         {
//                //             OnResLoad(handle);
//                //         });
//                //     },
//                //     parentComponent != null ? parentComponent.GameObject : BurnerUIManager.Instance.RootNode);
//
//                resHandle = CacheManager.Instance.GetObject(prefabName, OnResLoad,
//                    parentComponent != null ? parentComponent.GameObject : BurnerUIManager.Instance.RootNode);
//
//                BurnerUIManager.Instance.ReportPageStartLoad(prefabName);
//            }
//            else
//                Logger.Warn($"{prefabName} is already loading");
//        }
//
//        internal bool DoPreload(bool isFromClosing = false)
//        {
//            if ((logic != null && (logic.NeedPreload || logic.NeedPreloadOnShow || logic.HasComponentPreload)))
//            {
//                if (!pendingPreload)
//                {
//                    isClosing = false;
//                    pendingPreload = true;
//                    SetActive(false);
//                    BurnerUIManager.Instance.RequestHighPriorityLoad();
//                    logic?.DoPreload(isFromClosing ? null : ContentContext.Parameter, !isFromClosing);
//                }
//
//                return true;
//            }
//
//            return false;
//        }
//
//        void SetParentComponent()
//        {
//            var trans = go.transform;
//            trans.SetParent(
//                parentComponent != null ? parentComponent.Widget : BurnerUIManager.Instance.RootNode.transform, false);
//            trans.localPosition = Vector3.zero;
//            trans.localRotation = Quaternion.identity;
//            trans.localScale = Vector3.one;
//        }
//
//        void OnResLoad(IResourceHandle handle)
//        {
//            BurnerUIManager.Instance.FinishHighPriorityLoad();
//            if (disposed)
//            {
//                handle.Dispose();
//            }
//            else
//            {
//                resHandle = handle;
//                go = handle.ResObject as GameObject;
//                Logger.Assert(go, "Loading of {0} failed", prefabName);
//                isLoading = false;
//
//                loadTiming.FinishLoad();
//
//                //go.SetActive(false);
//                sortingorderHandlers = go.GetComponentsInChildren<ICanvasSortingOrderHandler>(true);
//                SetParentComponent();
//                canvas = go.GetComponent<Canvas>();
//                canvasRaycaster = go.GetComponent<GraphicRaycaster>();
//                childCanvases = go.GetComponentsInChildren<Canvas>();
//                safeArea = go.GetComponentInChildren<BurnerSafeArea>();
//                animator = go.GetComponentInChildren<Animator>();
//
//                if (parentComponent == null)
//                {
//                    if (canvas)
//                    {
//                        canvas.worldCamera = BurnerUIManager.Instance.UICamera;
//                        defaultRenderMode = canvas.renderMode;
//                    }
//                }
//
//                if (BurnerUIManager.Instance.AutoAdjustCanvasScaler)
//                    BurnerUIManager.AdjustCanvasScaler(go);
//
//                LoadTimeToken token;
//                if (curStage < LoadStages.Loaded)
//                {
//                    curStage = LoadStages.OnResLoad;
//                    pendingStage = LoadStages.OnResLoad;
//                    if (!BurnerUIManager.Instance.TryConsumeTimeSlice(out token))
//                    {
//                        pendingStage = LoadStages.OnInit;
//                        return;
//                    }
//
//                    using (token)
//                    {
//                        curStage = LoadStages.OnInit;
//                        pendingStage = LoadStages.OnInit;
//                        DoOnInit();
//                    }
//
//                    DoLoad();
//                }
//                else
//                {
//                    DoOnInit();
//                    DoLoad();
//                }
//            }
//        }
//
//        void DoLoad()
//        {
//            LoadTimeToken token;
//
//            if (!isClosing)
//            {
//                if (!DoPreload())
//                {
//                    if (curStage < LoadStages.Loaded)
//                    {
//                        if (!BurnerUIManager.Instance.TryConsumeTimeSlice(out token))
//                        {
//                            pendingStage = LoadStages.OnLoad;
//                            return;
//                        }
//
//                        using (token)
//                        {
//                            curStage = LoadStages.Loaded;
//                            pendingStage = LoadStages.Loaded;
//                            OnLoaded?.Invoke();
//                            return;
//                        }
//                    }
//                    else
//                        OnLoaded?.Invoke();
//                }
//                else
//                    return;
//            }
//            else
//            {
//                visible = false;
//                SetActive(false);
//            }
//
//            OnLoaded = null;
//        }
//
//        void DoOnInit()
//        {
//            Logger.Info(PrefabName + ".DoOnInit");
//            var binding = go.GetComponent<GameUIBinding>();
//            if (binding != null)
//            {
//                if (logic == null)
//                    logic = OnCreateLogic != null
//                        ? OnCreateLogic()
//                        : BurnerUIManager.Instance.MakeUILogic(binding.ClassName);
//                if (logic == null)
//                {
//                    OnClose(true);
//                    return;
//                }
//
//                logic.Initialize(binding, this);
//            }
//            else
//            {
//                Logger.Error($"{prefabName} doesn't have GameUIBinding component");
//            }
//        }
//
//        public bool IsResReady => go;
//
//        public bool Visible
//        {
//            get => visible;
//        }
//
//        internal PageContext CurrentContext { get; set; }
//
//        void OnOpen()
//        {
//            Logger.Info(PrefabName + ".OnOpen");
//            logic?.DoOpen(ContentContext.Parameter);
//        }
//
//        void OnHide()
//        {
//            Logger.Info(PrefabName + ".OnHide");
//            if (visible)
//            {
//                visible = false;
//                SetActive(false);
//                if (!IsSubPage)
//                    BurnerUIManager.Instance.ReportPageHidden(this);
//                if (renderVisible)
//                    logic?.DoHide();
//                if (ShouldHideLowerPage)
//                    DoHideOtherPage(false, true);
//                if (subPages != null)
//                {
//                    isUpdating = true;
//                    foreach (var i in subPages)
//                    {
//                        i.Value.CloseInternal(false);
//                    }
//
//                    isUpdating = false;
//                    ProcessPendingSubpageDelete();
//                }
//            }
//            else if (pendingOnOpen)
//            {
//                SetActive(false);
//            }
//        }
//
//        void OnClose(bool forceDestory)
//        {
//            Logger.Info(PrefabName + ".OnClose");
//            pendingPreload = false;
//            pendingOnLoad = null;
//            pendingOnOpen = false;
//            OnLoaded = null;
//            RenderVisible = true;
//            if (!isClosing && !disposed)
//            {
//                try
//                {
//                    logic?.DoClose();
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error(ex.ToString());
//                }
//
//                if (DestroyValue > 0 && !forceDestory)
//                {
//                    closeTime = Time.realtimeSinceStartup;
//                    isClosing = true;
//                    if (subPages != null)
//                    {
//                        bool needSetIsupdating = !isUpdating;
//                        if (needSetIsupdating)
//                            isUpdating = true;
//                        foreach (var i in subPages)
//                        {
//                            i.Value.CloseInternal(true);
//                        }
//
//                        if (needSetIsupdating)
//                            isUpdating = false;
//                        ProcessPendingSubpageDelete();
//                    }
//                }
//                else
//                    DoDispose();
//
//                if (ContentContext != null)
//                {
//                    ContentContext.AsyncOperations?.FinishClose(ContentContext.ReturnValue);
//                    ContentContext.AsyncOperations = null;
//                }
//            }
//
//            /*if (!isClosing)
//            {
//                isClosing = true;
//            }*/
//        }
//
//        internal void DoDispose()
//        {
//            //Logger.Info(PrefabName + ".DoDispose");
//            if (!disposed)
//            {
//                if (PendingCloseEvent)
//                {
//                    NeedDispose = true;
//                    return;
//                }
//
//                Profiler.BeginSample("Logic.DoDispose");
//                try
//                {
//                    logic?.DoDispose(true);
//                }
//                catch (Exception ex)
//                {
//                    Logger.Error(ex.ToString());
//                }
//                Profiler.EndSample();
//
//                //需要在清理当前Page的ResHandle前清理SubPage，否则会被GameObject.Destory连带着一起Destroy了，产生bug隐患
//                if (subPages != null)
//                {
//                    isUpdating = true;
//                    foreach (var i in subPages)
//                    {
//                        //这里已经在真正销毁了，直接DoDipose即可，subpage没有再被Update的机会，不销毁就泄露了
//                        i.Value.DoDispose();
//                    }
//
//                    isUpdating = false;
//
//                    //已经全卸载了，一口气全清空，后面也没有Update的机会再延迟删除了
//                    subPages.Clear();
//                    if (pendingDelete != null)
//                        pendingDelete.Clear();
//                }
//
//                if (resHandle != null)
//                    resHandle.Dispose();
//                resHandle = null;
//                OnPostponeSetActive = null;
//                go = null;
//                ContentContext = null;
//                parentComponent = null;
//                if (!IsSubPage)
//                    BurnerUIManager.Instance.ReportDispose(this);
//                else
//                    ParentPage.ReportSubPageClose(this);
//                disposed = true;
//                NeedDispose = false;
//            }
//        }
//
//        //public void Close(object returnVal = null)
//        //{
//        //    if (isClosing /*|| waitingToClose != null*/)
//        //        return;
//
//        //    if (contentContext != null)
//        //        contentContext.ReturnValue = returnVal;
//
//        //    if (IsMainPage)
//        //    {
//        //        CurrentContext.CloseMainPage(this);
//        //    }
//        //    else if (!IsSubPage && !IsFreePage)
//        //    {
//        //        CurrentContext.ClosePopup(this);
//        //    }
//        //    else
//        //    {
//        //        CloseInternal(true);
//        //    }
//
//            // if (IsMainPage && CurrentContext.CheckNeedWaitPreload(this, out var previousPage))
//            // {
//            //     previousPage.OnLoaded = () =>
//            //     {
//            //         waitingToClose = null;
//            //         contentContext.ReturnValue = returnVal;
//            //         CurrentContext.CloseMainPage(this);
//            //     };
//            //     waitingToClose = previousPage;
//            //     if (!previousPage.DoPreload(true))
//            //     {
//            //         waitingToClose = null;
//            //         previousPage.OnLoaded();
//            //         previousPage.OnLoaded = null;
//            //     }
//            //
//            //     return;
//            // }
//            // if (isLoading || pendingPreload)
//            // {
//            //     CloseInternal(true);
//            // }
//            // else
//            // {
//            // }
//        //}
//
//        void ProcessPendingSubpageDelete()
//        {
//            if (pendingDelete != null && pendingDelete.Count > 0)
//            {
//                foreach (var i in pendingDelete)
//                {
//                    subPages.Remove(i.prefabName);
//                }
//
//                pendingDelete.Clear();
//                RefreshSubPageOrder();
//            }
//        }
//
//
//        public void OnUpdate()
//        {
//            if (isClosing)
//            {
//                if (AutoDestroy && Time.realtimeSinceStartup - closeTime > DestroyValue)
//                {
//                    Profiler.BeginSample("GamePage.DoDispose");
//                    DoDispose();
//                    Profiler.EndSample();
//                }
//            }
//            else
//            {
//                if (curStage < LoadStages.Loaded && curStage != pendingStage)
//                {
//                    LoadTimeToken token;
//                    bool cando = BurnerUIManager.Instance.TryConsumeTimeSlice(out token);
//                    if (cando)
//                    {
//                        curStage = pendingStage;
//
//                        switch (pendingStage)
//                        {
//                            case LoadStages.OnInit:
//                                using (token)
//                                {
//                                    Profiler.BeginSample("GamePage.DoOnInit");
//                                    DoOnInit();
//                                    Profiler.EndSample();
//                                }
//
//                                Profiler.BeginSample("GamePage.DoLoad");
//                                DoLoad();
//                                Profiler.EndSample();
//                                break;
//                            case LoadStages.OnLoad:
//                                using (token)
//                                {
//                                    Profiler.BeginSample("GamePage.DoOnLoad");
//                                    OnLoaded?.Invoke();
//                                    Profiler.EndSample();
//                                }
//
//                                curStage = LoadStages.Loaded;
//                                break;
//                        }
//                    }
//                }
//
//                Profiler.BeginSample("Logic.DoUpdate");
//                logic?.DoUpdate();
//                Profiler.EndSample();
//                if (subPages != null)
//                {
//                    isUpdating = true;
//                    foreach (var i in subPages)
//                    {
//                        var page = i.Value;
//
//                        try
//                        {
//                            Profiler.BeginSample(page.PrefabName);
//                            page.OnUpdate();
//                            Profiler.EndSample();
//                        }
//                        catch (Exception ex)
//                        {
//                            Logger.Error($"Page {page.PrefabName} update failed\n{ex.ToString()}");
//                        }
//                    }
//
//                    isUpdating = false;
//                    Profiler.BeginSample("ProcessPendingSubpageDelete");
//                    ProcessPendingSubpageDelete();
//                    Profiler.EndSample();
//                }
//            }
//        }
//
//        public void OnLateUpdate()
//        {
//            if (disposed)
//                return;
//            Profiler.BeginSample("Logic.DoLateUpdate");
//            logic?.DoLateUpdate();
//            Profiler.EndSample();
//            if (subPages != null)
//            {
//                isUpdating = true;
//                foreach (var i in subPages)
//                {
//                    var page = i.Value;
//
//                    try
//                    {
//                        Profiler.BeginSample(page.PrefabName);
//                        page.OnLateUpdate();
//                        Profiler.EndSample();
//                    }
//                    catch (Exception ex)
//                    {
//                        Logger.Error($"Page {page.PrefabName} late update failed\n{ex.ToString()}");
//                    }
//                }
//
//                isUpdating = false;
//                Profiler.BeginSample("ProcessPendingSubpageDelete");
//                ProcessPendingSubpageDelete();
//                Profiler.EndSample();
//            }
//        }
//
//
//        #region PageContentContext
//
//
//        internal PageContentContext ContentContext { private set; get; }
//
//        //internal PageContentContext GetContentContext
//        //{
//        //    //logic?.OnGetContentContext;
//        //    return contentContext;
//        //}
//
//        //public T GetContentContext<T>() where T : PageContentContext
//        //{
//        //    return (T)GetContentContext;
//        //}
//
//        private void RefreshSubContext(object param, string openBy, PageAsyncOperations asyncOperations)
//        {
//            if (ContentContext == null)
//            {
//                ContentContext = new PageContentContext();
//            }
//            ContentContext.RefreshSubContext(param, openBy, asyncOperations);
//        }
//
//        internal void RefreshFreeContext(object param, int sortingOrder, string sortingLayer)
//        {
//            if (ContentContext == null)
//            {
//                ContentContext = new PageContentContext();
//            }
//            ContentContext.RefreshFreeContext(param, sortingOrder, sortingLayer);
//        }
//
//        internal void OnStackEntryChanged(PageContentContext entryContext)
//        {
//            ContentContext = entryContext;
//        }
//
//        #endregion
//
//
//        internal void FinishPreload()
//        {
//            if (pendingPreload)
//            {
//                pendingPreload = false;
//                BurnerUIManager.Instance.FinishHighPriorityLoad();
//                //ShowInternal(true, true);
//                if (OnLoaded != null)
//                {
//                    OnLoaded();
//                    OnLoaded = null;
//                }
//
//                if (pendingOnLoad != null)
//                {
//                    pendingOnLoad(logic);
//                    pendingOnLoad = null;
//                }
//
//                loadTiming.FirstOpened = false;
//            }
//        }
//
//        internal void DoPostponeSetVisible()
//        {
//            SetActive(false);
//
//            currentLoader = ResourceEngine.Proxy.CreateLoader(PrefabName);
//            currentLoader.BeginRecord(false);
//            loadTiming.StartOpen();
//            DoInvokeOpenShow(true);
//            loadTiming.FinishOpen();
//
//
//            currentLoader.OnFinish(() =>
//            {
//                currentLoader = null;
//                float t = Time.realtimeSinceStartup;
//                OnPostponeSetActive?.Invoke();
//                OnPostponeSetActive = null;
//                if (loadTiming.FirstOpened)
//                    loadTiming.OpenTime += (Time.realtimeSinceStartup - t);
//            });
//            currentLoader.EndRecord();
//        }
//
//        void DoInvokeOpenShow(bool doOpen)
//        {
//            //因业务需求，如需OnOpen，则需要在OnShow之前
//            if (doOpen)
//                OnOpen();
//            if (renderVisible)
//                logic?.DoShow();
//        }
//
//        internal void DoReopen(object param)
//        {
//            Logger.Info(PrefabName + ".DoReopen");
//            logic?.DoReopen(param);
//        }
//
//        internal void ShowInternal(bool doOpen,bool skipPendingQueue = true)
//        {
//            if (disposed)
//                throw new NotSupportedException("Cannot open disposed page");
//
//            Burner.Logger.Log($"Page '{PrefabName}' ShowInternal。doOpen is {doOpen},skipPendingQueue is {skipPendingQueue}");
//            if (skipPendingQueue)
//            {
//                if (NeedQueueOperation())
//                {
//                    if (pageTargetState.SwitchToShowState(doOpen))
//                    {
//                        Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起ShowInternal操作成功。");
//                    }
//                    else
//                    {
//                        Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起ShowInternal操作失败。");
//                    }
//                    return;
//                }
//            }
//
//            isClosing = false;
//            if (!visible)
//            {
//                visible = true;
//                bool postponeVisible = logic != null ? (doOpen && logic.PostponeSetVisible) : false;
//                if (!postponeVisible) //postpone的情况，这个时间点onopen和onshow已经执行完毕
//                {
//                    loadTiming.StartOpen();
//                    if (go)
//                    {
//                        SetActive(true);
//                        logic.DoBecomeVisible();
//                    }
//
//                    DoInvokeOpenShow(doOpen);
//
//                    loadTiming.FinishOpen();
//                }
//                else
//                {
//                    if (go)
//                    {
//                        SetActive(true);
//                        logic.DoBecomeVisible();
//                    }
//                }
//
//                if (subPages != null)
//                {
//                    isUpdating = true;
//                    foreach (var i in subPages)
//                    {
//                        if (!i.Value.IsClosing && i.Value.IsResReady)
//                        {
//                            i.Value.ShowInternal(i.Value.pendingOnOpen);
//                            i.Value.pendingOnOpen = false;
//                        }
//                    }
//
//                    isUpdating = false;
//                    ProcessPendingSubpageDelete();
//                }
//
//                DoAfterOnShow(doOpen);
//            }
//            else if (doOpen)
//            {
//                OnOpen();
//            }
//        }
//
//        void DoAfterOnShow(bool doOpen)
//        {
//            if (PendingOnShow != null)
//            {
//                PendingOnShow();
//                PendingOnShow = null;
//            }
//
//            if (doOpen)
//                ContentContext?.AsyncOperations?.FinishLoad(UILogic);
//
//            if (!IsSubPage)
//                BurnerUIManager.Instance.ReportPageShown(this);
//        }
//
//        internal void CloseInternal(bool doClose, bool forceDestory = false)
//        {
//            Burner.Logger.Log($"Page '{PrefabName}' CloseInternal。doClose is {doClose},forceDestory is {forceDestory}");
//            if (NeedQueueOperation())
//            {
//                if (pageTargetState.SwitchToCloseState(doClose, forceDestory))
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Close操作成功。");
//                }
//                else
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Close操作失败。");
//                }
//
//
//                return;
//            }
//
//            if (curStage == LoadStages.OnResLoad)
//                DoOnInit();
//            // if (waitingToClose != null)
//            // {
//            //     waitingToClose.OnLoaded = null;
//            //     waitingToClose.pendingPreload = false;
//            //     waitingToClose = null;
//            // }
//
//            if (blurEffectHandle != null)
//            {
//                blurEffectHandle.Dispose();
//                blurEffectHandle = null;
//            }
//
//
//            if (currentLoader != null)
//            {
//                currentLoader.Dispose();
//                currentLoader = null;
//            }
//
//            OnHide();
//            if (doClose)
//                OnClose(forceDestory);
//
//            BurnerUIManager.Instance.onFinalizePageClose?.Invoke(this);
//        }
//
//        //internal PageContentContext CreateContext()
//        //{
//        //    return new PageContentContext();
//        //    // return logic.OnCreateContentContext();
//        //}
//
//        //internal void SetContentContext(PageContentContext pageContentCtx)
//        //{
//        //    this.contentContext = pageContentCtx;
//        //    //logic.OnSetContentContext();
//        //}
//
//        public void EnableBlurOtherPages(GameRawImage img, int downSampleNum = 1, float blurSpreadSize = 1,
//            int blurIterations = 2, bool hideOther = false)
//        {
//            if (!IsPopup)
//                throw new NotSupportedException("Only popup page supports this feature");
//            if (blurEffectHandle != null)
//            {
//                blurEffectHandle.Dispose();
//            }
//
//            var (pList, list) = BurnerUIManager.Instance.GetTransformList(this);
//            var mgr = BurnerUIManager.Instance.GetNodeProcessManager();
//            Rect rect = default;
//            mgr.EnableNodeBlur(list, ref rect, img.RawImage, downSampleNum, blurSpreadSize, blurIterations);
//
//            if (hideOther)
//            {
//                if (IsPopup)
//                {
//                    if (!ShouldHideLowerPage)
//                        ShouldHideLowerPage = true;
//                    else
//                        hideOther = false;
//                }
//                else
//                {
//                    foreach (var i in pList)
//                    {
//                        i.GameObject.SetActive(false);
//                    }
//                }
//            }
//
//            blurEffectHandle = new NodeEffectHandle(mgr, pList, img.RawImage, this, hideOther);
//        }
//
//        private void SetActive(bool value)
//        {
//            if (go == null)
//                return;
//
//            if (IsSubPage)
//            {
//                go.SetActive(value);
//                return;
//            }
//
//            // var targetLayer = value ? defaultUILayer : hideUILayer;
//            // go.layer = targetLayer;
//
//            // 1. 先处理Canvas（确保渲染上下文就绪）
//            if (canvas != null)
//            {
//                canvas.enabled = value; // 先启用Canvas
//                canvas.renderMode = value ? defaultRenderMode : RenderMode.ScreenSpaceCamera;
//                if (childCanvases != null)
//                {
//                    foreach (var childCanvas in childCanvases)
//                    {
//                        childCanvas.enabled = value;
//                        childCanvas.renderMode = canvas.renderMode;
//                    }
//                }
//            }
//
//            // 2. 再处理GraphicRaycaster
//            if (canvasRaycaster != null)
//            {
//                canvasRaycaster.enabled = value;
//            }
//
//            // 3. 处理Animator
//            if (animator != null)
//            {
//                animator.enabled = value;
//                if (!value)
//                {
//                    animator.StopPlayback();
//                }
//                else
//                {
//                    if (ShowHistory.Add(prefabName))
//                    {
//                        animator.Play(animator.HasState(0, inStateHash) ? inStateHash : aniStateHash, 0, 0);
//                    }
//                    else
//                    {
//                        animator.Play(aniStateHash, 0, 0);
//                    }
//                }
//            }
//
//            // 4. 处理粒子系统
//            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
//            foreach (var ps in particleSystems)
//            {
//                if (value)
//                {
//                    ps.Play();
//                }
//                else
//                {
//                    ps.Stop();
//                    ps.Clear();
//                }
//            }
//
//            var uiParticle = go.GetComponentsInChildren<UIParticle>(true);
//            foreach (var ps in uiParticle)
//            {
//                if (value)
//                {
//                    ps.Play();
//                }
//                else
//                {
//                    ps.Stop();
//                    ps.Clear();
//                }
//            }
//
//        }
//        internal void RequestShow(object param)
//        {
//            if (NeedQueueOperation())
//            {
//                if(pageTargetState.SwitchToRequestShowState(param))
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起RequestShow操作成功。");
//                }
//                else
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起RequestShow操作失败。");
//                }
//
//                return;
//            }
//
//            if (!Visible)
//            {
//                if (IsResReady && !IsPreloading)
//                {
//                    // 页面资源已就绪且未在预加载状态
//                    bool hasPreload = UILogic != null && (UILogic.NeedPreload || UILogic.HasComponentPreload);
//                    if (hasPreload)
//                    {
//                        isClosing = false;
//                        SetPendingOnLoad((logic) => InitializeGamePage(false));
//                        DoPreload();
//                    }
//                    else
//                    {
//                        InitializeGamePage(false);
//                    }
//                }
//                else // 页面资源未就绪，或者正在预加载，或者处于关闭状态
//                {
//                    if (this.IsPreloading)
//                    {
//                        Burner.Logger.Warn($"页面 '{PrefabName}' 正在预加载中，无法再次打开。");
//                    }
//
//                    isClosing = false; // 如果页面之前处于关闭状态，重置此标志
//
//                    OnLoaded = () =>
//                    {
//                        BurnerUIManager.Instance.ReportPageLoadFinish(PrefabName);
//                        InitializeGamePage(false);
//                    };
//                }
//            }
//            else
//            {
//                // 页面已存在且当前可见
//
//                //SetupPageForOpening(page, flags, param, openBy, sortingOrder, sortingLayer, mainPageGroup, op);
//
//                DoReopen(param);
//                if (!RenderVisible)
//                {
//                    RenderVisible = true;
//                }
//            }
//        }
//
//
//        /// <summary>
//        /// 隐藏Page
//        /// </summary>
//        /// <param name="prefabName"></param>
//        internal void HidePage(bool renderOnly = false)
//        {
//            if (NeedQueueOperation())
//            {
//                if(pageTargetState.SwitchToHideState(renderOnly))
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Hide操作成功。");
//                }
//                else
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Hide操作失败。");
//                }
//
//                return;
//            }
//
//            if (renderOnly)
//            {
//                RenderVisible = false;
//            }
//            else
//                CloseInternal(false);
//        }
//
//
//        /// <summary>
//        /// 重新显示之前隐藏的Page
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void RestorePage()
//        {
//            if (NeedQueueOperation())
//            {
//                if(pageTargetState.SwitchToRestoreState())
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Restore操作成功。");
//                }
//                else
//                {
//                    Burner.Logger.Warn($"页面 '{PrefabName}' 正在加载或预加载，挂起Restore操作失败。");
//                }
//
//                return;
//            }
//
//            if (!Visible)
//                ShowInternal(false);
//            RenderVisible = true;
//        }
//
//
//        void InitializeGamePage(bool noPostpone = false, int sortingOrder = 0)
//        {
//            if (UILogic != null && UILogic.PostponeSetVisible && !noPostpone)
//            {
//                OnPostponeSetActive = () => InitializeGamePage(true, sortingOrder);
//                DoPostponeSetVisible();
//            }
//            else
//            {
//                var ctx = CurrentContext;
//                bool needRemoveFromPendingCloseEvents = NeedDispose;
//                if (NeedDispose)
//                {
//                    NeedDispose = false;
//                }
//                BurnerUIManager.Instance.OnInitializeGamePageBegin(this, needRemoveFromPendingCloseEvents);
//                if (IsFreePage)
//                {
//                    CurrentContext = null;
//                    var pCtx = ContentContext;
//                    SortingOrder = pCtx.SortingOrder;
//                    SortingLayer = pCtx.SortingLayer;
//                    PlaneDistance = PageContext.MainPageZ;
//                }
//                else if (IsPopup)
//                {
//                    ctx.FinalizePopupOpening(this/*,sortingOrder*/);
//                }
//                else
//                {
//                    ctx.FinalizeMainPageOpening(this, sortingOrder/*, mainPageGroup*/);
//                }
//
//                ShowInternal(true, false);
//                BurnerUIManager.Instance.OnInitializeGamePageFinish(this);
//            }
//
//            ExecutePendingOperationIfAny();
//        }
//
//
//
//        /// <summary>
//        /// 存储最新挂起的操作
//        /// </summary>
//        private PageTargetState pageTargetState = PageTargetState.Default();
//
//        /// <summary>
//        /// 尝试排队操作。
//        /// 如果页面正在加载/预加载，则返回 true，表示操作被挂起；否则返回 false。
//        /// </summary>
//        /// <param name="po"></param>
//        /// <returns></returns>
//        private bool NeedQueueOperation()
//        {
//            if (IsLoading || IsPreloading)
//            {
//                return true;
//            }
//            return false;
//        }
//
//        /// <summary>
//        /// 在页面完成初始化后检查并执行挂起的操作
//        /// </summary>
//        private void ExecutePendingOperationIfAny()
//        {
//            var op = pageTargetState;
//            pageTargetState.Reset();
//
//            Burner.Logger.Log($"页面 '{PrefabName}' 完成加载/预加载，执行挂起操作 '{op.Type}'。");
//
//            // 根据挂起操作类型执行相应的逻辑
//            switch (op.Type)
//            {
//                case PageTargetStateType.RequestShow:
//                    RequestShow(op.Param);
//                    break;
//                case PageTargetStateType.Show:
//                    ShowInternal(op.DoOpen);
//                    break;
//                case PageTargetStateType.Hide:
//                    HidePage(op.RenderOnly);
//                    break;
//                case PageTargetStateType.Close:
//                    CloseInternal(op.DoClose, op.ForceDestroy);
//                    break;
//                case PageTargetStateType.Restore:
//                    RestorePage();
//                    break;
//                case PageTargetStateType.None:
//                    // 无挂起操作
//                    break;
//                default:
//                    Burner.Logger.Error($"页面 '{PrefabName}' 处理了未知的挂起操作类型: {op.Type}。");
//                    break;
//            }
//        }
//    }
//
//
//    internal enum PageTargetStateType
//    {
//        None,
//        Show,
//        Hide,
//        Close,
//        Restore,
//        RequestShow,
//    }
//
//    // 挂起操作的结构体
//    internal struct PageTargetState
//    {
//        public PageTargetStateType Type;
//        public bool DoOpen;   // 针对Show
//        public bool RenderOnly;   // 针对Hide
//        public bool DoClose;   // 针对Close
//        public bool ForceDestroy;   // 针对Close
//        public object ReturnValue;
//        public object Param;
//
//        public static PageTargetState Default() => new() { Type =PageTargetStateType.None};
//
//        public void Reset()
//        {
//            Type = PageTargetStateType.None;
//            DoOpen = false;
//            RenderOnly = false;
//            DoClose = false;
//            ForceDestroy = false;
//            ReturnValue = null;
//            Param = null;
//        }
//
//        private bool SwitchState(PageTargetStateType state)
//        {
//            bool result=true;
//            switch (Type)
//            {
//                case PageTargetStateType.Close:
//                    result = state == PageTargetStateType.Show || state == PageTargetStateType.RequestShow;
//                    break;
//                case PageTargetStateType.None:
//                case PageTargetStateType.Show:
//                case PageTargetStateType.Hide:
//                case PageTargetStateType.Restore:
//                case PageTargetStateType.RequestShow:
//                default:
//                    break;
//            }
//
//            if (result)
//            {
//                Type = state;
//            }
//
//            return result;
//        }
//
//        public bool SwitchToCloseState(bool doClose,bool forceDestroy)
//        {
//            if (SwitchState(PageTargetStateType.Close))
//            {
//                DoClose = doClose;
//                ForceDestroy = forceDestroy;
//                return true;
//            }
//            return false;
//        }
//
//        public bool SwitchToHideState(bool renderOnly)
//        {
//            if (SwitchState(PageTargetStateType.Hide))
//            {
//                RenderOnly = renderOnly;
//                return true;
//            }
//            return false;
//        }
//
//        public bool SwitchToRequestShowState(object param)
//        {
//            if (SwitchState(PageTargetStateType.RequestShow))
//            {
//                Param = param;
//                return true;
//            }
//
//            return false;
//        }
//
//        public bool SwitchToRestoreState()
//        {
//            return SwitchState(PageTargetStateType.Restore);
//        }
//
//        public bool SwitchToShowState(bool doOpen)
//        {
//            if (SwitchState(PageTargetStateType.Show))
//            {
//                DoOpen = doOpen;
//                return true;
//            }
//
//            return false;
//        }
//    }
//}
