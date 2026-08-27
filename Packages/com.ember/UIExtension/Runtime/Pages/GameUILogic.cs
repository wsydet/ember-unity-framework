//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using Burner.Extensions;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//using Burner.Basic;
//using UnityEngine.Profiling;
//
//namespace Burner.UIExtension
//{
//    public class CustomUILoader : ILoaderHandle
//    {
//        bool ready;
//        float startTime;
//        GameUILogic page;
//#if DEBUG
//        string stackTrace;
//#endif
//
//        public float MaximumLoadTime { get; set; } = 5f;
//        internal CustomUILoader(GameUILogic page)
//        {
//            this.page = page;
//            startTime = Time.realtimeSinceStartup;
//#if DEBUG
//            stackTrace = StackTraceUtility.ExtractStackTrace();
//#endif
//        }
//        public bool IsDisposed()
//        {
//            return false;
//        }
//
//        public void Finish()
//        {
//            ready = true;
//        }
//
//        public bool Ready()
//        {
//            if (Time.realtimeSinceStartup - startTime > MaximumLoadTime)
//            {
//                ready = true;
//#if DEBUG
//                Burner.Logger.Error($"Loader created by {page.PagePrefabName} required more than {MaximumLoadTime} seconds, forgot to call Finish?\nStacktrace:{stackTrace}");
//#else
//                Burner.Logger.Error($"Loader created by {page.PagePrefabName} required more than {MaximumLoadTime} seconds, forgot to call Finish?");
//#endif
//            }
//            return ready;
//        }
//    }
//    public class GameUILogic : IUIBehaviour
//    {
//        protected GamePage page;
//        protected GameUIBinding binding;
//        protected Dictionary<string, IUIBehaviour> controlMap = new Dictionary<string, IUIBehaviour>();
//        protected GameUIComponent selfComp;
//        List<IUIBehaviour> behaviours = new List<IUIBehaviour>();
//        bool disposed = false, logicDisposed = false;
//        IUIBehaviour[] copiedBehaviours;
//        bool behavioursDirty;
//
//
//        IResourceLoader currentLoader;
//        CustomUILoader currentPreloadLoader, currentLogicPreloader;
//        NodeEffectHandle blurEffectHandle;
//        bool hasComponentPreload;
//        bool postponeSetVisible;
//        Camera curCam, overridenCam;
//
//        internal GamePage Page => page;
//
//        public virtual bool NeedPreload { get => false; }
//
//        public virtual bool NeedPreloadOnShow { get => false; }
//
//        private bool isShowing;
//
//        public bool PostponeSetVisible
//        {
//            get => postponeSetVisible;
//            set
//            {
//                postponeSetVisible = value;
//            }
//        }
//
//        public bool HasComponentPreload => hasComponentPreload;
//
//        public bool AutoFinishPreload { get; set; }
//
//        public bool HasCanvas => page.HasCanvas;
//        public Canvas Canvas => page.Canvas;
//
//        public bool HasSafeArea => page.HasSafeArea;
//        public RectTransform SafeAreaRoot => page.SafeAreaRoot;
//        public BurnerSafeArea SafeArea => page.SafeArea;
//
//        public bool SkipOnHideOnHideLower
//        {
//            get => page.SkipOnHideOnHideLower;
//            set
//            {
//                page.SkipOnHideOnHideLower = value;
//            }
//        }
//
//
//        public bool ShouldHideLowerPage
//        {
//            get => page.ShouldHideLowerPage;
//            set
//            {
//                if (IsPopup && IsPage)
//                {
//                    page.ShouldHideLowerPage = value;
//                }
//                else
//                    throw new NotSupportedException("只有Popup Page才支持这个接口");
//            }
//        }
//
//
//        public IResourceLoader PreloadLoader => currentLoader;
//
//        public GameUILogic Parent
//        {
//            get
//            {
//                if (page.ParentPage != null)
//                    return page.ParentPage.UILogic;
//                else
//                    return null;
//            }
//        }
//
//        bool CanDisposeManually { get; set; }
//
//        internal void Initialize(GameUIBinding binding, GamePage page)
//        {
//            postponeSetVisible = BurnerUIManager.Instance.EnablePostponeSetVisible;
//            this.binding = binding;
//            this.page = page;
//            page.LoadTiming.StartInit();
//            InitControlMap(binding, controlMap, behaviours, ref selfComp, out hasComponentPreload);
//            CreateBindlessComponents();
//            behavioursDirty = true;
//            OnBind();
//            DoInit();
//            page.LoadTiming.FinishInit();
//        }
//
//        protected void InitializeFromLogic(GameUIBinding binding, GameUILogic parentLogic)
//        {
//            this.binding = binding;
//            this.page = parentLogic.page;
//            InitControlMap(binding, controlMap, behaviours, ref selfComp, out hasComponentPreload);
//            CreateBindlessComponents();
//            behavioursDirty = true;
//            OnBind();
//            DoInit();
//        }
//
//        void CreateBindlessComponents()
//        {
//            var bindless = binding.gameObject.GetComponentsInChildren<IBindlessUIBehaviour>(true);
//            foreach(var i in bindless)
//            {
//                if (i.BindingLogic == null)//避免重复绑定，如果GameUILogic嵌套，则子对象在此刻已被绑定
//                {
//                    var comp = i.CreateUIComponent();
//                    comp.UILogic = this;
//                    comp.Initialize((i as MonoBehaviour).gameObject);
//                    i.BindingLogic = this;
//                    AddBehaviour(comp);
//                    hasComponentPreload |= comp.NeedPreload;
//                }
//            }
//        }
//
//        public UnityEngine.Camera Camera
//        {
//            get
//            {
//                return curCam;
//            }
//            set
//            {
//                if (IsPage && page.Canvas)
//                {
//                    curCam = value;
//                    if (!overridenCam)
//                        page.Canvas.worldCamera = value;
//                }
//            }
//        }
//
//        public Camera OverridenCamera
//        {
//            get
//            {
//                return overridenCam;
//            }
//            set
//            {
//                overridenCam = value;
//                if (IsPage && page.Canvas)
//                {
//                    if (value)
//                    {
//                        page.Canvas.worldCamera = value;
//                    }
//                    else
//                        page.Canvas.worldCamera = curCam;
//                }
//            }
//        }
//
//        public int Layer
//        {
//            get
//            {
//                return page.GameObject.layer;
//            }
//            set
//            {
//                GameObjectUtils.SetLayer(page.GameObject, value);
//            }
//        }
//
//        /// <summary>
//        /// 获取当前逻辑是否为Page
//        /// </summary>
//        public bool IsPage => binding != null ? binding.IsPage : false;
//
//        public bool IsMainPage => page.IsMainPage;
//
//        public bool IsPopup => page.IsPopup;
//
//        public bool IsSubPage => page.IsSubPage;
//
//        public bool IsTopMost => page.IsTopMost;
//
//        public bool IsFreePage => page.IsFreePage;
//
//        public bool Disposed => page.Disposed;
//
//        public bool IsNull()
//        {
//            return page.Disposed;
//        }
//        public bool IsPreloading => page.IsPreloading;
//
//        public PageLoadTiming LoadTiming => page.LoadTiming;
//
//        /// <summary>
//        /// 获取或设置当前界面是否会在关闭时自动销毁，默认为true。如果有需要常驻的界面可设置为false。如果不再需要常驻时设置为true会自动进行销毁
//        /// </summary>
//        public bool AutoDestroy
//        {
//            get => page.AutoDestroy;
//            set
//            {
//                page.AutoDestroy = value;
//            }
//        }
//
//        /// <summary>
//        /// 设置自动销毁的延迟，默认为30秒
//        /// </summary>
//        public float DestroyDelay
//        {
//            get => page.DestroyValue;
//            set
//            {
//                page.DestroyValue = value;
//            }
//        }
//
//        /// <summary>
//        /// 获取或设置当前逻辑所在UIComponent的可见性
//        /// </summary>
//        public bool Visible
//        {
//            get => selfComp != null && selfComp.Visible;
//            set
//            {
//                if (selfComp == null)
//                    return;
//
//                if (selfComp.Visible != value)
//                {
//                    selfComp.Visible = value;
//                    if (value)
//                        DoShow();
//                    else
//                        DoHide();
//                }
//            }
//        }
//
//        /// <summary>
//        /// 获取当前逻辑所在UIComponent在层级中是否可见
//        /// </summary>
//        public bool VisibleInHierarchy
//        {
//            get => selfComp != null && selfComp.VisibleInHierarchy;
//        }
//
//        /// <summary>
//        /// 将当前Page通过Culling剔除渲染
//        /// </summary>
//        public bool RenderVisible
//        {
//            get
//            {
//                if (!IsPage || IsSubPage)
//                    throw new NotSupportedException("只有MainPage和PopupPage才支持该接口");
//                return page.RenderVisible;
//            }
//            set
//            {
//                if (!IsPage || IsSubPage)
//                    throw new NotSupportedException("只有MainPage和PopupPage才支持该接口");
//                page.RenderVisible = value;
//            }
//        }
//
//        /// <summary>
//        /// 当前Logic所在的GameUIComponent
//        /// </summary>
//        public GameUIComponent UIComponent => selfComp;
//
//        /// <summary>
//        /// 当前Logic所在Page的Prefab名
//        /// </summary>
//        public string PagePrefabName => page.PrefabName;
//
//        /// <summary>
//        /// 设置当前Logic是否需要Update
//        /// </summary>
//        public bool NeedUpdate { get; set; }
//        IUIBehaviour[] GetCopiedComonents()
//        {
//            if (copiedBehaviours == null)
//                copiedBehaviours = new IUIBehaviour[0];
//            if (behavioursDirty)
//            {
//                copiedBehaviours = new IUIBehaviour[behaviours.Count];
//                int idx = 0;
//                foreach (IUIBehaviour i in behaviours)
//                {
//                    copiedBehaviours[idx++] = i;
//                }
//                behavioursDirty = false;
//            }
//            return copiedBehaviours;
//        }
//
//        protected void InitControlMap(GameUIBinding binding, Dictionary<string, IUIBehaviour> controlMap, List<IUIBehaviour> behaviours, ref GameUIComponent selfComp, out bool hasComponentPreload)
//        {
//            hasComponentPreload = false;
//            selfComp = binding.gameObject.CreateUIBehaviour(this, "selfComp", binding.SelfWidgetType, binding.SelfWidgetClassName) as GameUIComponent;
//            if (selfComp.BindlessComponent != null)
//            {
//                //如果已手动绑定则不再自动创建GameUIComponent
//                selfComp.BindlessComponent.BindingLogic = this;
//            }
//            foreach (var b in binding.Bindings)
//            {
//                IUIBehaviour ctrl = b.GameObject.CreateUIBehaviour(this, b.Name, b.Type, b.ClassName);
//                if (ctrl != null)
//                {
//                    if (ctrl is GameUIComponent comp)
//                    {
//                        hasComponentPreload |= comp.NeedPreload;
//                        if (comp.BindlessComponent != null)
//                        {
//                            //如果已手动绑定则不再自动创建GameUIComponent
//                            comp.BindlessComponent.BindingLogic = this;
//                        }
//                    }
//                    controlMap[b.Name] = ctrl;
//                    behaviours?.Add(ctrl);
//                }
//                else
//                {
//                    Logger.Error($"{page.PrefabName}:{binding.name}({binding.ClassName}) has missing component binding for:{b.Name}");
//                }
//            }
//        }
//
//        protected virtual GameUILogic CreateNewLogicFromBinding(GameUIBinding binding)
//        {
//            GameUILogic logic;
//            if (binding.NoCodeGeneration)
//            {
//                logic = new GameUILogic();
//            }
//            else
//            {
//                logic = BurnerUIManager.Instance.MakeUILogic(binding.ClassName);
//                if(logic == null)
//                    logic = new GameUILogic();
//            }
//
//            logic.InitializeFromLogic(binding, this);
//            return logic;
//        }
//
//        internal GameUILogic DoCreateNewLogicFromBinding(GameUIBinding binding)
//        {
//            return CreateNewLogicFromBinding(binding);
//        }
//
//        public void AddBehaviour(IUIBehaviour behaviour)
//        {
//#if DEBUG
//            if (behaviours.Contains(behaviour))
//                Logger.Error("Duplicate behaviour in " + page.PrefabName);
//#endif
//            behavioursDirty = true;
//            behaviours.Add(behaviour);
//        }
//
//        public void RemoveBehaviour(IUIBehaviour behaviour)
//        {
//            behavioursDirty = true;
//            behaviours.Remove(behaviour);
//        }
//
//        /// <summary>
//        /// 获取当前Logic所在Page的上下文
//        /// </summary>
//        /// <returns></returns>
//        public PageContentContext GetContext()
//        {
//            return page.ContentContext;
//        }
//
//        /// <summary>
//        /// 获取打开当前Page的参数
//        /// </summary>
//        /// <returns></returns>
//        public object GetParameter()
//        {
//            return page.ContentContext?.Parameter;
//        }
//
//        /// <summary>
//        /// 获取打开当前Page的Page名
//        /// </summary>
//        /// <returns></returns>
//        public string GetOpenBy()
//        {
//            return page.ContentContext?.OpenBy;
//        }
//
//        /// <summary>
//        /// 获取之前被关闭的页面的返回值
//        /// </summary>
//        /// <returns></returns>
//        public object GetReturnValue()
//        {
//            return page.ContentContext?.ReturnValue;
//        }
//
//        /// <summary>
//        /// 创建新上下文时的回调
//        /// </summary>
//        /// <returns></returns>
//        // public virtual PageContentContext OnCreateContentContext()
//        // {
//        //     return new PageContentContext();
//        // }
//
//        /// <summary>
//        /// 当UI管理器需要获取上下文时的回调
//        /// </summary>
//        //public virtual void OnGetContentContext
//        //{
//
//        //}
//
//        /// <summary>
//        /// 当UI管理器需要设置上下文时的回调
//        /// </summary>
//        //public virtual void OnSetContentContext()
//        //{
//
//        //}
//
//        public virtual void OnBeginLoad()
//        {
//
//        }
//
//        internal void DoBeginLoad()
//        {
//            OnBeginLoad();
//        }
//
//        public virtual void OnBecomeVisible()
//        {
//
//        }
//
//        internal void DoBecomeVisible()
//        {
//            OnBecomeVisible();
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                    logic.DoBecomeVisible();
//                else
//                    b.OnBecomeVisible();
//            }
//            if (ShouldHideLowerPage && IsPage)
//                page.DoHideOtherPage(true, false);
//        }
//
//        /// <summary>
//        /// 使用ShouldHideLowerPage隐藏下方UI，在需要隐藏或显示下方UI时调用
//        /// </summary>
//        /// <param name="hide">需要隐藏还是显示下方UI</param>
//        /// <param name="allShown">在显示下方UI时，是否无其他需要隐藏下方界面，所有界面都已恢复显示</param>
//        /// <param name="coveredByOther">在隐藏下方UI时，是否被其他隐藏下方的页面遮挡</param>
//        public virtual void OnHideOtherPage(bool hide, bool allShown, bool coveredByOther)
//        {
//
//        }
//
//        public virtual void HideByOtherPage(bool hide, GameUILogic byPage)
//        {
//            RenderVisible = !hide;
//        }
//
//        public virtual void OnReopen(object param)
//        {
//
//        }
//
//        internal void DoReopen(object param)
//        {
//            CancelPlayCloseAni();
//            OnReopen(param);
//        }
//
//        public virtual void OnInit()
//        {
//
//        }
//
//        internal void DoInit()
//        {
//            if (IsPage && page.Canvas)
//            {
//                curCam = page.Canvas.worldCamera;
//            }
//            else
//                curCam = null;
//            OnInit();
//        }
//
//        public virtual void OnDispose()
//        {
//
//        }
//
//        public virtual void OnDisposeLogic()
//        {
//
//        }
//
//        public virtual void OnBind()
//        {
//
//        }
//
//        public virtual void OnClose()
//        {
//
//        }
//        internal void DoClose()
//        {
//            if (!needInvokeAfterClose)
//            {
//                CancelPlayCloseAni();
//            }
//
//            OnClose();
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                    logic.DoClose();
//                else
//                    b.OnClose();
//            }
//        }
//
//        public void Dispose()
//        {
//            if (CanDisposeManually)
//                DoDispose(true);
//        }
//
//        internal void DoDispose(bool destory)
//        {
//            if (!disposed)
//            {
//                disposed = true;
//                OnDispose();
//                var arr = GetCopiedComonents();
//                for (int i = 0; i < arr.Length; i++)
//                {
//                    var b = arr[i];
//                    try
//                    {
//                        if (b is GameUILogic logic)
//                            logic.DoDispose(destory);
//                        else
//                            ((GameUIComponent)b).DoDispose(destory);
//                    }
//                    catch(Exception e)
//                    {
//                        GameUIComponent curComp = null;
//                        if (b is GameUILogic logic)
//                            curComp = logic.selfComp;
//                        else
//                            curComp = (GameUIComponent)b;
//                        if (!curComp.GameObject)
//                            curComp = selfComp;
//                        string path = curComp.GameObject ? curComp.GameObject.transform.GetFullPathName(BurnerUIManager.Instance.RootNode.transform) : "";
//                        Burner.Logger.Error($"Failed to dispose children in ({PagePrefabName})({path})\n{e}");
//                    }
//                }
//            }
//            if (destory)
//            {
//                if (!logicDisposed)
//                {
//                    OnDisposeLogic();
//                    logicDisposed = true;
//                    behaviours.Clear();
//                    copiedBehaviours = null;
//                    selfComp.UILogic = null;
//                    selfComp = null;
//                }
//            }
//        }
//        public void FinishPreload()
//        {
//            if (currentLogicPreloader != null)
//                currentLogicPreloader.Finish();
//            else if (!AutoFinishPreload)
//            {
//                if (currentPreloadLoader != null)
//                {
//                    currentPreloadLoader.Finish();
//                    currentPreloadLoader = null;
//                }
//                page.FinishPreload();
//            }
//            else
//                throw new NotSupportedException("自动完成模式请不要手动调用FinishPreload");
//        }
//
//        /// <summary>
//        /// 默认进场动画名
//        /// </summary>
//        protected const string AnimationName_Ani = "Ani";
//
//        /// <summary>
//        /// 默认退场动画名
//        /// </summary>
//        protected const string AnimationName_Out = "Out";
//
//        private bool isPlayingCloseAni = false;
//
//        private bool needInvokeAfterClose = false;
//
//        /// <summary>
//        /// 关闭当前Page
//        /// </summary>
//        /// <param name="returnVal"></param>
//        public void ClosePage(object returnVal = null , Action afterClose = null)
//        {
//            if (GetNeedCloseAnimation() && selfComp.HasAnimation(AnimationName_Out))
//            {
//                if (isPlayingCloseAni)
//                {
//                    onPlayCloseAnimComplete = afterClose;
//                    return;
//                }
//
//                //if (!selfComp.IsPlayingAnimation(AnimationName_Out))
//                //{
//                    selfComp.StopAnimation();
//                    OnBeginPlayCloseAni(afterClose);
//                    selfComp.PlayAnimation(clipName: AnimationName_Out, onEnded: () =>
//                    {
//                        needInvokeAfterClose = true;
//                        BurnerUIManager.Instance.ClosePage(PagePrefabName, returnVal);
//                        needInvokeAfterClose = false;
//                        OnFinishPlayCloseAni();
//                    });
//                //}
//            }
//            else
//            {
//                BurnerUIManager.Instance.ClosePage(PagePrefabName,returnVal);
//                afterClose?.Invoke();
//            }
//        }
//
//        protected virtual bool GetNeedCloseAnimation()
//        {
//            return true;
//        }
//
//        private Action onPlayCloseAnimComplete;
//
//        private const int Close_Check_Duration = 3;
//        private float closeBeginTimeStamp;
//        private void OnBeginPlayCloseAni(Action action)
//        {
//            isPlayingCloseAni = true;
//            onPlayCloseAnimComplete = action;
//            closeBeginTimeStamp = Time.realtimeSinceStartup;
//        }
//
//        private void CheckAfterBeginPlayCloseAni()
//        {
//            if (isPlayingCloseAni)
//            {
//                isPlayingCloseAni = false;
//            }
//        }
//
//        private void OnFinishPlayCloseAni()
//        {
//            onPlayCloseAnimComplete?.Invoke();
//            onPlayCloseAnimComplete=null;
//            isPlayingCloseAni = false;
//        }
//
//        private void CancelPlayCloseAni()
//        {
//            selfComp?.StopAnimation();
//            isPlayingCloseAni = false;
//            onPlayCloseAnimComplete=null;
//        }
//
//        /// <summary>
//        /// 打开指定页面，并把当前主页面压入栈
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="param"></param>
//        // public PageAsyncOperations ShowMainPage(string prefabName, object param = null, bool clearContext = false)
//        // {
//        //     if (IsFreePage)
//        //     {
//        //         Burner.Logger.Error("FreePage无法打开MainPage，请使用BurnerUIManager打开");
//        //         return default;
//        //     }
//        //     if (!clearContext)
//        //     {
//        //         if (page.IsSubPage)
//        //             return BurnerUIManager.Instance.ShowPageInteral(prefabName, PageFlags.MainPage, param, page.ParentPage, page.ParentPage.PageGroup);
//        //         else
//        //             return BurnerUIManager.Instance.ShowPageInteral(prefabName, PageFlags.MainPage, param, page, page.PageGroup);
//        //     }
//        //     else
//        //     {
//        //         return BurnerUIManager.Instance.ShowPageInteral(prefabName, PageFlags.MainPage, param, null, null);
//        //     }
//        // }
//
//        /// <summary>
//        /// 打开指定弹窗
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="topmost"></param>
//        /// <param name="param"></param>
//        // public PageAsyncOperations ShowPopup(string prefabName, object param = null, bool topmost = false)
//        // {
//        //     if (IsFreePage)
//        //     {
//        //         Burner.Logger.Error("FreePage无法打开PopupPage，请使用BurnerUIManager打开");
//        //         return default;
//        //     }
//        //     PageFlags flags = PageFlags.Popup;
//        //     if (topmost)
//        //         flags |= PageFlags.TopMost;
//        //     return BurnerUIManager.Instance.ShowPageInteral(prefabName, flags, param, page, null);
//        // }
//
//        /// <summary>
//        /// 打开SubPage
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="param"></param>
//        /// <param name="onLoad"></param>
//        public PageAsyncOperations ShowSubPage(string prefabName, object param = null, GameUIComponent parentComponent = null, Action<GameUILogic> onLoad = null)
//        {
//            return page.ShowSubPage(prefabName, param, parentComponent, onLoad);
//        }
//
//        public PageAsyncOperations MakeSubPage<T>(string prefabName, object param = null, GameUIComponent parentComponent = null, Action<T> onLoad = null) where T : GameUILogic, new()
//        {
//            return page.ShowSubPage(prefabName, param, parentComponent, (Action<GameUILogic>)onLoad, () => new T());
//        }
//
//        public T GetSubPage<T>(string prefabName) where T : GameUILogic
//        {
//            var subPage = page.GetSubPage(prefabName);
//            if (subPage != null)
//                return subPage.UILogic as T;
//            else
//                return null;
//        }
//
//        /// <summary>
//        /// 隐藏SubPage
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void HideSubPage(string prefabName)
//        {
//            page.HideSubPage(prefabName);
//        }
//
//        /// <summary>
//        /// 关闭SubPage
//        /// </summary>
//        /// <param name="prefabName"></param>
//        public void CloseSubPage(string prefabName)
//        {
//            page.CloseSubPage(prefabName);
//        }
//
//        /// <summary>
//        /// 关闭所有SubPage
//        /// </summary>
//        public void CloseAllSubPages()
//        {
//            page.CloseAllSubPages();
//        }
//
//        internal void ResetDispose(bool callOnResetDispose)
//        {
//            disposed = false;
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                    logic.ResetDispose(callOnResetDispose);
//                else
//                    ((GameUIComponent)b).ResetDispose(callOnResetDispose);
//
//            }
//        }
//
//        public T Make<T>(GameUIComponent comp) where T : GameUILogic, new()
//        {
//            var go = comp.GameObject;
//            var binding = go.GetComponent<GameUIBinding>();
//            if (!binding)
//                throw new NotSupportedException($"{go} is missing GameUIBinding");
//            T res = new T();
//            res.Initialize(binding, page);
//            AddBehaviour(res);
//            return res;
//        }
//
//        public T Make<T>(GameObject go) where T : GameUILogic
//        {
//            var binding = go.GetComponent<GameUIBinding>();
//            if (!binding)
//                throw new NotSupportedException($"{go} is missing GameUIBinding");
//            GameUILogic logic;
//            if (binding.NoCodeGeneration)
//            {
//                logic = new GameUILogic();
//            }
//            else
//            {
//                logic = BurnerUIManager.Instance.MakeUILogic(binding.ClassName);
//                if (logic == null)
//                    logic = new GameUILogic();
//            }
//            logic.CanDisposeManually = true;
//            logic.InitializeFromLogic(binding, this);
//            return logic as T;
//        }
//
//        public virtual void OnShow()
//        {
//
//        }
//
//        internal void DoShow(bool skipUILogic = false)
//        {
//            if (isShowing)
//                return;
//            isShowing = true;
//            if (!skipUILogic)
//                OnShow();
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b.VisibleInHierarchy)
//                {
//                    if (b is GameUILogic logic)
//                    {
//                        if (!skipUILogic)
//                            logic.DoShow();
//                    }
//                    else
//                        b.OnShow();
//                }
//            }
//        }
//
//        public virtual void OnPreload(object param, bool isOpen)
//        {
//
//        }
//
//        public CustomUILoader AddCustomUILoader()
//        {
//
//            if (currentLoader != null)
//            {
//                var loader = new CustomUILoader(this);
//                currentLoader.ListenHandle(loader);
//                return loader;
//            }
//            else
//                return null;
//        }
//
//        internal void DoPreload(object param, bool isOpen)
//        {
//
//            currentLoader = null;
//            if (IsSubPage && Parent != null)
//            {
//                if (Parent.currentLoader != null)
//                {
//                    currentPreloadLoader = Parent.AddCustomUILoader();
//                }
//            }
//            bool hasLogicPreload = NeedPreload || NeedPreloadOnShow;
//            if (AutoFinishPreload || HasComponentPreload)
//            {
//                currentLoader = ResourceEngine.Proxy.CreateLoader("Preload Loader");
//                currentLoader.OnFinish(() =>
//                {
//                    currentLoader = null;
//                    page.FinishPreload();
//                    if (currentPreloadLoader != null)
//                    {
//                        currentPreloadLoader.Finish();
//                        currentPreloadLoader = null;
//                    }
//                });
//                currentLoader.BeginRecord(false);
//                if (!AutoFinishPreload && hasLogicPreload)
//                {
//                    currentLogicPreloader = AddCustomUILoader();
//                }
//            }
//            if (hasLogicPreload)
//                OnPreload(param, isOpen);
//            if (HasComponentPreload)
//            {
//                var arr = GetCopiedComonents();
//                var cnt = arr.Length;
//                for (int i = 0; i < cnt; i++)
//                {
//                    var obj = arr[i];
//                    if (obj is GameUIComponent comp && comp.NeedPreload)
//                    {
//                        comp.OnPreload();
//                    }
//                }
//            }
//
//            if (currentLoader != null)
//                currentLoader.EndRecord();
//        }
//
//        public virtual void OnOpen(object param)
//        {
//
//        }
//
//        internal void DoOpen(object param)
//        {
//            OnOpen(param);
//            /*子控件如果派发OnOpen会需要判断是否可见，如果不可见是否需要补派发，会引发很多问题，因此仅最上层派发
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                    logic.OnOpen(param);
//            }*/
//        }
//
//        public virtual void OnHide()
//        {
//
//        }
//
//        internal void DoHide(bool skipUILogic = false)
//        {
//            if (!isShowing)
//                return;
//            isShowing = false;
//            if (!skipUILogic)
//                OnHide();
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                {
//                    if (!skipUILogic)
//                        logic.DoHide();
//                }
//                else
//                    b.OnHide();
//            }
//        }
//
//        public virtual bool TryEscapeKeyClose()
//        {
//            return false;
//        }
//
//        public virtual void OnUpdate()
//        {
//
//        }
//
//        internal void DoUpdate()
//        {
//            if (!VisibleInHierarchy)
//                return;
//            Profiler.BeginSample("GameUILogic.DoUpdate");
//            if (NeedUpdate)
//                OnUpdate();
//            Profiler.EndSample();
//
//            Profiler.BeginSample("UIComponent.OnUpdate");
//            UIComponent?.OnUpdate();
//            Profiler.EndSample();
//
//            Profiler.BeginSample("ChildrenComponentUpdate");
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                {
//                    Profiler.BeginSample("ChildLogicUpdate");
//                    logic.DoUpdate();
//                    Profiler.EndSample();
//                }
//                else if (((GameUIComponent)b).VisibleInHierarchy)
//                {
//                    Profiler.BeginSample("ChildComponentUpdate");
//                    b.OnUpdate();
//                    Profiler.EndSample();
//                }
//            }
//            Profiler.EndSample();
//
//            if (isPlayingCloseAni)
//            {
//                if (Time.realtimeSinceStartup - closeBeginTimeStamp > Close_Check_Duration)
//                {
//                    CheckAfterBeginPlayCloseAni();
//                }
//            }
//        }
//
//        public virtual void OnLateUpdate()
//        {
//
//        }
//
//        internal void DoLateUpdate()
//        {
//            if (!VisibleInHierarchy)
//                return;
//
//            Profiler.BeginSample("GameUILogic.DoLateUpdate");
//            if (NeedUpdate)
//                OnLateUpdate();
//            Profiler.EndSample();
//
//            Profiler.BeginSample("UIComponent.OnLateUpdate");
//            UIComponent?.OnLateUpdate();
//            Profiler.EndSample();
//
//            Profiler.BeginSample("ChildrenComponentLateUpdate");
//            var arr = GetCopiedComonents();
//            for (int i = 0; i < arr.Length; i++)
//            {
//                var b = arr[i];
//                if (b is GameUILogic logic)
//                {
//                    Profiler.BeginSample("ChildLogicLateUpdate");
//                    logic.DoLateUpdate();
//                    Profiler.EndSample();
//                }
//                else if (((GameUIComponent)b).VisibleInHierarchy)
//                {
//                    Profiler.BeginSample("ChildComponentLateUpdate");
//                    b.OnLateUpdate();
//                    Profiler.EndSample();
//                }
//            }
//            Profiler.EndSample();
//        }
//
//        public virtual void OnRefreshItem(object data, int index)
//        {
//
//        }
//
//        public virtual void OnStreamingOut()
//        {
//
//        }
//
//        public void EnableBlurOtherPages(GameRawImage img, int downSampleNum = 1, float blurSpreadSize = 1, int blurIterations = 2, bool hideOther = false)
//        {
//            page.EnableBlurOtherPages(img, downSampleNum, blurSpreadSize, blurIterations, hideOther);
//        }
//
//        public ScreenShotHandle GetScreenShot()
//        {
//            Rect rect = default;
//            RenderTexture rt = null;
//            List<RectTransform> list = new List<RectTransform>();
//            list.Add(page.GameObject.transform as RectTransform);
//            NodePostProcessManager.GetNodeShotImage(list, ref rt, ref rect, true);
//            ScreenShotHandle handle = new ScreenShotHandle(rt, true);
//            return handle;
//        }
//
//        public void SaveScreenShotToJPG(string file)
//        {
//            var rt = GetScreenShot();
//            SaveRenderTextureToJPG(rt.ScreenShotTexture, file);
//            rt.Dispose();
//        }
//
//        void SaveRenderTextureToJPG(RenderTexture rt, string fileName)
//        {
//            RenderTexture prev = RenderTexture.active;
//            RenderTexture.active = rt;
//            Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
//            png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
//            byte[] bytes = png.EncodeToPNG();
//            FileStream file = File.Open(fileName, FileMode.Create);
//            BinaryWriter writer = new BinaryWriter(file);
//            writer.Write(bytes);
//            file.Close();
//            Texture2D.DestroyImmediate(png);
//            png = null;
//            RenderTexture.active = prev;
//        }
//
//        protected virtual void OnRenderVisibleChanged(bool visible)
//        {
//            if (page.HasCanvas)
//            {
//                if (visible)
//                {
//                    page.ResetPlanceDistance();
//                }
//                else
//                    page.Canvas.planeDistance = 100000;
//            }
//        }
//
//        internal void DoChangeRenderVisible(bool visible)
//        {
//            OnRenderVisibleChanged(visible);
//        }
//    }
//}
