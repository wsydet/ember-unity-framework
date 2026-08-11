//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;
//
//using Burner.Basic.Tasks;
//
//namespace Burner.UIExtension
//{
//    public class GameTabLoader : GameUIComponent
//    {
//        struct LoadingPageInfo
//        {
//            public PageAsyncOperations AsyncOp;
//            public string Name;
//        }
//        TabLoader loader;
//        int curIndex;
//        LoadingPageInfo currentLoading;
//        bool isOpened = false;
//        object pendingParam = null;
//        STTaskCompletionSource<GameUILogic> pendingTCS;
//        public override void OnInit()
//        {
//            loader = GetComponent<TabLoader>();
//        }
//        public override IBindlessUIBehaviour BindlessComponent => loader;
//
//        public override bool NeedPreload => loader.PrefabMode;
//
//
//        public int CurrentIndex => curIndex;
//
//        public override async void OnPreload()
//        {
//            if (curIndex >= 0 && curIndex < loader.TabPageInfos.Length)
//            {
//                var l = UILogic.AddCustomUILoader();
//                var info = loader.TabPageInfos[curIndex];
//                var name = info.PrefabName;
//                currentLoading.Name = name;
//                currentLoading.AsyncOp = UILogic.ShowSubPage(name, pendingParam, this, (logic) =>
//                {
//                    pendingParam = null;
//                    isOpened = true;
//                    l.Finish();
//                });
//                var page = await currentLoading.AsyncOp.OnShow;
//                FinishLoadTab(page, info);
//            }
//        }
//
//        void FinishLoadTab(GameUILogic page, TabLoader.TabInfo info)
//        {
//            currentLoading.AsyncOp = null;
//            currentLoading.Name = null;
//            InitializePageTransform(page, info);
//            if (pendingTCS != null)
//            {
//                pendingTCS.SetResult(page);
//                pendingTCS = null;
//            }
//        }
//
//        void InitializePageTransform(GameUILogic page, TabLoader.TabInfo info)
//        {
//            var rt = page.UIComponent.Widget;
//            rt.anchorMin = info.AnchorMin;
//            rt.anchorMax = info.AnchorMax;
//            rt.pivot = info.Pivot;
//            rt.anchoredPosition = info.AnchoredPosition;
//            rt.sizeDelta = info.SizeDelta;
//        }
//
//        public async void ChangeTab(int index, object param = null)
//        {
//            if (loader.PrefabMode)
//            {
//                if (!isOpened && loader.PrefabMode)
//                {
//                    curIndex = index;
//                    pendingParam = param;
//                }
//                else
//                {
//                    if (index >= 0 && index < loader.TabPageInfos.Length)
//                    {
//                        var lastPage = loader.TabPageInfos[curIndex].PrefabName;
//                        var info = loader.TabPageInfos[index];
//                        if (currentLoading.AsyncOp != null)
//                        {
//                            if (currentLoading.Name == info.PrefabName)//当前已经在加载该页签
//                                return;
//
//                            UILogic.CloseSubPage(currentLoading.Name);//如果还在加载就切换新页签，需要把正在加载中的关闭
//                        }
//                        var name = info.PrefabName;
//                        currentLoading.Name = name;
//                        currentLoading.AsyncOp = UILogic.ShowSubPage(name, param, this);
//                        var page = await currentLoading.AsyncOp.OnShow;
//                        if (currentLoading.Name != name)//这种情况代表异步已被其他操作打断
//                            return;
//                        FinishLoadTab(page, info);
//                        curIndex = index;//成功打开后才改变当前页签ID，避免连续Change2次Tab，第一次还没加载完就变更导致黑屏
//                        if (lastPage != name)
//                            UILogic.CloseSubPage(lastPage);//新的页面打开后再关闭老页面避免闪屏
//                    }
//#if UNITY_EDITOR
//                    else
//                        throw new ArgumentOutOfRangeException(nameof(index), index, "Tab index is out of range");
//#endif
//                }
//            }
//            else
//            {
//                if (index >= 0 && index < loader.TabPages.Length)
//                {
//                    curIndex = index;
//                    for(int i = 0; i < loader.TabPages.Length; i++)
//                    {
//                        loader.TabPages[i].SetActive(i == index);
//                    }
//                }
//#if UNITY_EDITOR
//                else
//                    throw new ArgumentOutOfRangeException(nameof(index), index, "Tab index is out of range");
//#endif
//            }
//        }
//
//        public string GetCurrentTabName()
//        {
//            return loader.PrefabMode ? loader.TabPageInfos[curIndex].PrefabName : loader.TabPages[curIndex].name;
//        }
//
//        public STTask<GameUILogic> GetCurrentTabAsync()
//        {
//            if(currentLoading.AsyncOp != null || !isOpened)//如果当前Page还在加载中
//            {
//                if(pendingTCS == null)
//                {
//                    pendingTCS = new STTaskCompletionSource<GameUILogic>();
//                }
//                return pendingTCS.Task;
//            }
//            else
//            {
//                var res = UILogic.GetSubPage<GameUILogic>(loader.TabPageInfos[curIndex].PrefabName);
//                return STTask.FromResult(res);
//            }
//        }
//
//        public async void GetCurrentTabAsync(Action<GameUILogic> cb)
//        {
//            cb?.Invoke(await GetCurrentTabAsync());
//        }
//
//        public override void OnClose()
//        {
//            //Close时SubPage会自动Close，因此只需将当前还在加载中的异步操作句柄置空即可
//            currentLoading = default;
//            isOpened = false;
//            var lastPage = loader.TabPageInfos[curIndex].PrefabName;
//            UILogic.CloseSubPage(lastPage);
//        }
//    }
//}
