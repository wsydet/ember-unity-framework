//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using JetBrains.Annotations;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    class PageContextEntry
//    {
//        public GamePage Page;
//        public PageContentContext Context;
//        public List<PageContextEntry> Popups = new List<PageContextEntry>();
//        public bool ClearPrevious;
//
//        public PageContextEntry(GamePage page)
//        {
//            this.Page = page;
//            Context = new PageContentContext();
//            RefreshPageContext();
//        }
//
//        public void RefreshPageContext()
//        {
//            Page.OnStackEntryChanged(Context);
//        }
//    }
//
//    public class PageContentContext
//    {
//        public object Parameter { get; set; }
//        public string OpenBy { get; set; }
//        public object ReturnValue { get; set; }
//        public int SortingOrder { get; set; }
//        public string SortingLayer { get; set; }
//        public float PlaneDistance { get; set; }
//
//        internal PageAsyncOperations AsyncOperations { get; set; }
//
//
//        internal void RefreshSubContext(object param, string openBy, PageAsyncOperations asyncOperations)
//        {
//            Parameter = param;
//            OpenBy = openBy;
//            AsyncOperations = asyncOperations;
//        }
//
//        internal void RefreshFreeContext(object param, int sortingOrder, string sortingLayer)
//        {
//            Parameter = param;
//            SortingOrder = sortingOrder;
//            SortingLayer = sortingLayer;
//        }
//
//        internal void RefreshContext(object param)
//        {
//            Parameter = param;
//        }
//
//    }
//
//    struct MainPageList
//    {
//        public StackList<PageContextEntry> Stack;
//        public int StackSortingOrder;
//    }
//
//    class StackList<T> : List<T>
//    {
//        public void Push(T item)
//        {
//            Add(item);
//        }
//        public bool Pop(out T result)
//        {
//            if (Count <= 0)
//            {
//                result = default;
//                return false;
//            }
//            else
//            {
//                T item = this[Count - 1];
//                RemoveAt(Count - 1);
//                result = item;
//                return true;
//            }
//        }
//        
//        public bool Peek(out T result)
//        {
//            if (Count <= 0)
//            {
//                result = default;
//                return false;
//            }
//            else
//            {
//                result = this[Count - 1];
//                return true;
//            }
//        }
//    }
//
//    class PageContext
//    {
//        MainPageList _mainPageList;
//        Dictionary<string, MainPageList> mainPageGroups;
//        //List<PageContextEntry> popups = new List<PageContextEntry>();
//
//        const int MainPageOrder = 1000;
//        // const int InitialPopupOrder = 10000;//改为与MainPage统一
//        const int InitialTopMostOrder = 25000;
//        internal const int PageGrowStep = 500;
//        internal const float MainPageZ = 250;
//        const float InitialPopupZ = 200;
//        const float InitialTopMostZ = 100;
//        internal const float PageZGrowStep = -10;
//        //int curMainPageOrder = MainPageOrder;
//        //int curPopupOrder = InitialPopupOrder;
//        //int curTopMostOrder = InitialTopMostOrder;
//
//        public MainPageList MainPageList => _mainPageList;
//
//        public Dictionary<string, MainPageList> MainPageGroups => mainPageGroups;
//
//        public bool HasMainPage(string group = null)
//        {
//            if (string.IsNullOrEmpty(group))
//                return _mainPageList.Stack.Count > 0;
//            else if (mainPageGroups != null && mainPageGroups.TryGetValue(group, out var stack))
//            {
//                return stack.Stack.Count > 0;
//            }
//            else
//                return false;
//        }
//
//        public PageContext()
//        {
//            _mainPageList.Stack = new StackList<PageContextEntry>();
//            _mainPageList.StackSortingOrder = MainPageOrder;
//            //curMainPageOrder = MainPageOrder + PageGrowStep;
//        }
//
//        public MainPageList GetCurrentMainPageStack(string pageGroup, bool createNew = false ,int setSortingOrderIfCreate = MainPageOrder)
//        {
//            MainPageList curList;
//            if (string.IsNullOrEmpty(pageGroup))
//                curList = _mainPageList;
//            else
//            {
//                if (mainPageGroups == null)
//                    mainPageGroups = new Dictionary<string, MainPageList>();
//                if (!mainPageGroups.TryGetValue(pageGroup, out curList) && createNew)
//                {
//                    curList = default;
//                    curList.Stack = new StackList<PageContextEntry>();
//                    curList.StackSortingOrder = setSortingOrderIfCreate;
//                    //curMainPageOrder += PageGrowStep;
//                    mainPageGroups[pageGroup] = curList;
//                }
//            }
//            return curList;
//        }
//
//        public void ShowMainPageGroup(string pageGroup)
//        {
//            try
//            {
//                var pageStack = GetCurrentMainPageStack(pageGroup, true);
//                var curStack = pageStack.Stack;
//                if (curStack.Peek(out var top))
//                {
//                    if (!top.Page.IsDisposed)
//                        top.Page.ShowInternal(false);
//                }
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        //void SortPopups()
//        //{
//        //    popups.Sort((a, b) => a.Page.SortingOrder - b.Page.SortingOrder);
//        //}
//
//
//        public (bool, GamePage) ShowLowerPage(GamePage page)
//        {
//            bool shouldShow = true;
//            GamePage currentVisibleCoveringPop = null;
//            bool hasHigherNonCoveringPopup = false;
//
//            if (page.IsPopup)
//            {
//                bool begin = false;
//                var popups = GetCurPopupsByGroup(page.PageGroup);
//                if (popups != null)
//                {
//                    for (int i = popups.Count - 1; i >= 0; i--)
//                    {
//                        var p = popups[i];
//                        if (p.Page.Disposed)
//                            continue;
//                        if (page == p.Page)
//                            begin = true;
//                        else if (begin)
//                        {
//                            if (shouldShow)
//                            {
//                                p.Page.UILogic?.HideByOtherPage(false, page.UILogic);
//                            }
//                            if (p.Page.ShouldHideLowerPage)
//                            {
//                                if (currentVisibleCoveringPop == null)
//                                {
//                                    currentVisibleCoveringPop = p.Page;
//                                    if (hasHigherNonCoveringPopup)
//                                    {
//                                        //上方有非遮挡下方的popup，这个情况下该popup需要变为遮挡下方的
//                                        for (int j = popups.Count - 1; j > i; j--)
//                                        {
//                                            var p2 = popups[j];
//                                            if (!p2.Page.ShouldHideLowerPage)
//                                            {
//                                                currentVisibleCoveringPop = p2.Page;
//                                                break;
//                                            }
//                                        }
//                                    }
//                                }
//                                shouldShow = false;
//                                break;
//                            }
//                            else if (shouldShow)
//                            {
//                                hasHigherNonCoveringPopup = true;
//                            }
//
//                        }
//                        else
//                        {
//                            if (p.Page.ShouldHideLowerPage)
//                            {
//                                //被关闭的页面上面还有遮挡全局的其他Popup， 这种情况当前界面的关闭实际不做任何事
//                                if (currentVisibleCoveringPop == null)
//                                    currentVisibleCoveringPop = p.Page;
//                                shouldShow = false;
//                            }
//                        }
//                    }
//                }
//                //Popup永远都在Mainpage 上方
//                if (shouldShow)
//                {
//                    if (_mainPageList.Stack.Count > 0)
//                    {
//
//                        if (_mainPageList.Stack.Peek(out var mp))
//                        {
//                            mp.Page.UILogic?.HideByOtherPage(false, page.UILogic);
//                        }
//
//                    }
//                    if (mainPageGroups != null)
//                    {
//                        foreach (var i in mainPageGroups)
//                        {
//                            if (i.Value.Stack.Peek(out var mp))
//                            {
//                                mp.Page.UILogic?.HideByOtherPage(false, page.UILogic);
//                            }
//                        }
//                    }
//                }
//            }
//            return (shouldShow, currentVisibleCoveringPop);
//        }
//
//        public bool HideLowerPage(GamePage page)
//        {
//            bool coveredByOther = false;
//            if (page.IsPopup)
//            {
//                bool begin = false;
//                var popups = GetCurPopupsByGroup(page.PageGroup);
//                if (popups != null)
//                {
//                    for (int i = popups.Count - 1; i >= 0; i--)
//                    {
//                        var p = popups[i];
//                        if (p.Page.Disposed)
//                            continue;
//                        if (page == p.Page)
//                            begin = true;
//                        else if (begin)
//                        {
//                            if ((!p.Page.IsTopMost || page.IsTopMost) && !coveredByOther)
//                                p.Page.UILogic?.HideByOtherPage(true, page.UILogic);
//                            else
//                            {
//                                p.Page.UILogic?.OnHideOtherPage(true, false, false);
//                                if (p.Page.ShouldHideLowerPage)
//                                    coveredByOther = true;
//                            }
//                        }
//                        else
//                        {
//                            if (p.Page.ShouldHideLowerPage)
//                                coveredByOther = false;
//                            else if (p.Page.IsTopMost)
//                            {
//                                p.Page.UILogic?.OnHideOtherPage(true, false, false);
//                            }
//                        }
//                    }
//                }
//
//                //Popup永远都在Mainpage 上方
//                if (mainPageGroups != null && !coveredByOther)
//                {
//                    if (_mainPageList.Stack.Peek(out var mp))
//                    {
//                        mp.Page.UILogic?.HideByOtherPage(true, page.UILogic);
//                    }
//                    foreach (var i in mainPageGroups)
//                    {
//                        if (i.Value.Stack.Peek(out mp))
//                        {
//                            mp.Page.UILogic?.HideByOtherPage(true, page.UILogic);
//                        }
//                    }
//                }
//            }
//            return coveredByOther;
//        }
//
//        public void HideMainPageGroup(string pageGroup)
//        {
//            try
//            {
//                var pageStack = GetCurrentMainPageStack(pageGroup, true);
//                var curStack = pageStack.Stack;
//                if (curStack.Peek(out var top))
//                {
//                    top.Page.CloseInternal(false);
//                }
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//
//        /// <summary>
//        /// 将页面注册到当前上下文，设置Flags、创建Context数据、计算层级并执行Prepare逻辑
//        /// </summary>
//        /// <returns>计算后的实际 SortingOrder</returns>
//        public bool RegisterPage(GamePage page, object param, bool clearPreviousPages, string mainPageGroup)
//        {
//            page.CurrentContext = this;
//
//            //var contentCtx = page.CreateContext();
//            //contentCtx.Parameter = param;
//            //page.SetContentContext(contentCtx);
//
//            if (page.IsPopup)
//            {
//                return PreparePopupOpening(page, mainPageGroup, param);
//            }
//            else // Main Page
//            {
//                PrepareMainPageOpening(page, param, mainPageGroup, clearPreviousPages);
//                return true;
//            }
//        }
//
//
//        /// <summary>
//        /// 更新界面数据，准备打开界面
//        /// </summary>
//        /// <param name="page"></param>
//        /// <param name="openBy"></param>
//        /// <param name="param"></param>
//        /// <param name="pageGroup"></param>
//        private void PrepareMainPageOpening(GamePage page, object param, string pageGroup, bool clearPreviousPages)
//        {
//            try
//            {
//                Burner.Logger.Log($"Page '{page.PrefabName}' PrepareMainPageOpening。pageGroup is {pageGroup}");
//
//                var pageStack = GetCurrentMainPageStack(pageGroup, true);
//                page.PageGroup = pageGroup;
//                var curStack = pageStack.Stack;
//                //if (curStack.Count > 0)
//                //{
//                //    //entry为值类型，修改后需要重新push，故此处需要Pop而不是Peek
//                //    var prevPageEntry = curStack.Pop();
//                //    var prevPage = prevPageEntry.Page;
//                //    if (prevPage == page)
//                //    {
//                //        curStack.Push(prevPageEntry);
//                //        return;
//                //    }
//                //}
//                if (!clearPreviousPages && curStack.Peek(out var top) && top?.Page == page)
//                {
//                    return;
//                }
//
//                PageContextEntry entry = new PageContextEntry(page);
//                entry.Context.SortingOrder = GetLastSortingOrderByGroup(pageGroup);
//                entry.Context.PlaneDistance = GetLastPlanDistanceOrderByGroup(pageGroup);
//                entry.Context.Parameter = param;
//                entry.ClearPrevious = clearPreviousPages;
//                curStack.Push(entry);
//            }
//
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        private const string Stack_Log_MainGroup = "MainGroup:";
//        private const string Stack_Log_OtherGroup = "OtherGroup:";
//        private const string Stack_Log_Indent = "  ";
//
//        public string GetStacks()
//        {
//            var stringBuilder = new StringBuilder();
//
//            AppendIndent(stringBuilder, 1).AppendLine(Stack_Log_MainGroup);
//            stringBuilder.AppendLine(GetMainGroupStack());
//
//            // AppendIndent(stringBuilder, 1).AppendLine(Stack_Log_OtherGroup);
//            // stringBuilder.AppendLine(GetOtherGroupStacks());
//
//            return stringBuilder.ToString();
//        }
//
//        private StringBuilder AppendIndent(StringBuilder sb,int count = 1)
//        {
//            for (int i = 0; i < count; i++)
//            {
//                sb.Append(Stack_Log_Indent);
//            }
//            return sb;
//        }
//
//        private string GetMainGroupStack()
//        {
//            if (_mainPageList.Stack?.Count <= 0)
//            {
//                return string.Empty;
//            }
//
//            var stringBuilder = new StringBuilder();
//            foreach (var stack in _mainPageList.Stack)
//            {
//                AppendIndent(stringBuilder, 2).Append("Page:").Append(stack.Page.PrefabName).Append($"(sorting:{stack.Context.SortingOrder})").AppendLine($"(PD:{stack.Context.PlaneDistance})");
//
//                AppendIndent(stringBuilder, 3).AppendLine("Popup:");
//                foreach (var popup in stack.Popups)
//                {
//                    AppendIndent(stringBuilder, 4).Append(popup.Page.PrefabName).Append($"(sorting:{popup.Context.SortingOrder})").AppendLine($"(PD:{popup.Context.PlaneDistance})");
//                }
//            }
//
//
//            AppendIndent(stringBuilder, 2).AppendLine("TopMost:");
//            foreach (var popup in topMostPopup)
//            {
//                AppendIndent(stringBuilder, 3).Append(popup.Page.PrefabName).Append($"(sorting:{popup.Context.SortingOrder})").AppendLine($"(PD:{popup.Context.PlaneDistance})");
//            }
//
//            return stringBuilder.ToString();
//        }
//
//        private string GetOtherGroupStacks()
//        {
//            if (mainPageGroups == null || mainPageGroups.Count <= 0)
//            {
//                return string.Empty;
//            }
//
//            var stringBuilder = new StringBuilder();
//            foreach (var kv in mainPageGroups)
//            {
//                AppendIndent(stringBuilder, 2).Append("Group:").AppendLine(kv.Key);
//                if (kv.Value.Stack == null)
//                {
//                    stringBuilder.AppendLine();
//                }
//                else
//                {
//                    foreach (var stack in kv.Value.Stack)
//                    {
//                        AppendIndent(stringBuilder, 3).Append("Page:").Append(stack.Page.PrefabName).Append($"(sorting:{stack.Context.SortingOrder})").AppendLine($"(PD:{stack.Context.PlaneDistance})");
//
//                        AppendIndent(stringBuilder, 4).Append("Popup:");
//                        foreach (var popup in stack.Popups)
//                        {
//                            AppendIndent(stringBuilder, 5).Append(popup.Page.PrefabName).Append($"(sorting:{popup.Context.SortingOrder})").AppendLine($"(PD:{popup.Context.PlaneDistance})");
//                        }
//                    }
//                }
//
//            }
//            return stringBuilder.ToString();
//        }
//
//        /// <summary>
//        /// 资源加载完毕，真正打开界面
//        /// </summary>
//        /// <param name="page"></param>
//        /// <param name="sortingOrder"></param>
//        /// <param name="pageGroup"></param>
//        internal void FinalizeMainPageOpening(GamePage page, int sortingOrder)
//        {
//            try
//            {
//                Burner.Logger.Log($"Page '{page.PrefabName}' FinalizeMainPageOpening.");
//
//                var pageStack = GetCurrentMainPageStack(page.PageGroup, true);
//                var curStack = pageStack.Stack;
//
//                int index = -1;
//                for (int i = curStack.Count - 1; i >= 0; i--)
//                {
//                    if (curStack[i].Page == page)
//                    {
//                        index = i;
//                        break;
//                    }
//                }
//
//                if (index == -1)
//                {
//                    Logger.Log($"Page '{page.PrefabName}' does not exist in the stack.");
//                    //page.CloseInternal(true);
//                    return;
//                }
//
//                var currentEntry = curStack[index];
//                if (currentEntry.ClearPrevious)
//                {
//                    for (int i = index - 1; i >= 0; i--)
//                    {
//                        var prevEntry = curStack[i];
//                        prevEntry.Page.CloseInternal(true);
//
//                        if (prevEntry.Popups != null)
//                        {
//                            foreach (var p in prevEntry.Popups)
//                            {
//                                p.Page.CloseInternal(true);
//                            }
//                        }
//
//                        curStack.RemoveAt(i);
//                    }
//
//                    //CloseAllPopups(false, true);
//
//                    index = 0;
//                }
//                else
//                {
//                    if (index > 0)
//                    {
//                        var prevEntry = curStack[index - 1];
//                        var prevPage = prevEntry.Page;
//
//
//                        var prePopups = prevEntry.Popups;
//                        for (int i = 0; i < prePopups.Count; i++)
//                        {
//                            var entry = prePopups[i];
//                            entry.Page.CloseInternal(false);
//                        }
//                        prevPage.CloseInternal(false);
//                        curStack[index - 1] = prevEntry;
//                    }
//                }
//
//
//                bool hasHideLower = false;
//                var popups = GetCurPopupsByGroup(page.PageGroup);
//                if (popups != null)
//                {
//                    foreach (var i in popups)
//                    {
//                        if (i.Page.IsDisposed)
//                            continue;
//                        if (i.Page.ShouldHideLowerPage)
//                            hasHideLower = true;
//                    }
//                }
//                page.RenderVisible = !hasHideLower;
//            
//                //page.SortingOrder = pageStack.StackSortingOrder + (index * PageGrowStep);
//                //page.PlaneDistance = GetZBySortingOrder(index, MainPageZ, PageZGrowStep);
//            
//                page.SortingOrder = page.ContentContext.SortingOrder;
//                page.PlaneDistance = page.ContentContext.PlaneDistance;
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        // internal static float GetZBySortingOrder(int order, int baseOrder, int growStep, float initialZ, float zStep)
//        // {
//        //     int steps = (order - baseOrder) / growStep;
//        //     return Mathf.Max(initialZ + (zStep * steps), 10);
//        // }
//
//        /// <summary>
//        /// 更新弹窗数据，准备打开弹窗
//        /// </summary>
//        /// <param name="page"></param>
//        /// <param name="openBy"></param>
//        /// <param name="param"></param>
//        public bool PreparePopupOpening(GamePage page, string pageGroup, object param)
//        {
//            try
//            {
//                page.CurrentContext = this;
//                var popups = page.IsTopMost ? topMostPopup : GetCurPopupsByGroup(pageGroup);
//
//                if (popups == null)
//                {
//                    Logger.Error($"There is no MainPage currently but a popup is requested to be opened ,please use FreePage,popup name is  {page.PrefabName}");
//                    return false;
//                }
//
//                foreach (var p in popups)
//                {
//                    if (p.Page == page)
//                    {
//                        Logger.Warn($"{page.PrefabName} is already in popup list");
//                        return true;
//                    }
//                }
//                PageContextEntry entry = new PageContextEntry(page);
//                entry.Context.SortingOrder = page.IsTopMost ? InitialTopMostOrder + (topMostPopup.Count * PageGrowStep) :GetLastSortingOrderByGroup(pageGroup);
//                entry.Context.PlaneDistance = page.IsTopMost ? InitialTopMostZ + (topMostPopup.Count * PageZGrowStep) : GetLastPlanDistanceOrderByGroup(pageGroup);
//                entry.Context.Parameter = param;
//                popups.Add(entry);
//                Logger.Log($"popups list add {page.PrefabName}");
//                return true;
//            }
//            
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//                return false;
//            }
//        }
//        
//        /// <summary>
//        /// 资源加载完毕，真正打开弹窗
//        /// </summary>
//        /// <param name="page"></param>
//        /// <param name="sortingOrder"></param>
//        /// <param name="pageGroup"></param>
//        public void FinalizePopupOpening(GamePage page)
//        {
//            try
//            {
//                bool hasHideLower = false;
//                page.SortingOrder = page.ContentContext.SortingOrder;
//                //if (page.IsTopMost)
//                //{
//                //    page.PlaneDistance = GetZBySortingOrder(page.SortingOrder, InitialTopMostOrder, PageGrowStep, InitialTopMostZ, PageZGrowStep);
//                //}
//                //else
//                //{
//                //    page.PlaneDistance = GetZBySortingOrder(page.SortingOrder, InitialPopupOrder, PageGrowStep, InitialPopupZ, PageZGrowStep);
//                //}
//
//                page.PlaneDistance = page.ContentContext.PlaneDistance;
//                var popups = GetCurPopupsByGroup(page.PageGroup);
//                if (popups != null)
//                {
//                    foreach (var i in popups)
//                    {
//                        if (i.Page == page)
//                            break;
//                        if (i.Page.ShouldHideLowerPage)
//                        {
//                            hasHideLower = true;
//                            break;
//                        }
//                    }
//                }
//                if (!page.ShouldHideLowerPage && hasHideLower)
//                {
//                    //如果不是遮挡下方的且下方有遮挡其他界面的，新开的popup也依然应该在上面
//                    page.UILogic?.OnHideOtherPage(true, false, false);
//                }
//            }
//            
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        //public int IncreasePopupOrder()
//        //{
//        //    int res = curPopupOrder;
//        //    curPopupOrder += PageGrowStep;
//        //    return res;
//        //}
//
//        //public int IncreaseTopMostOrder()
//        //{
//        //    int res = curTopMostOrder;
//        //    curTopMostOrder += PageGrowStep;
//        //    return res;
//        //}
//
//        //void RefreshPopupOrder()
//        //{
//        //    int maxOrder = InitialPopupOrder - PageGrowStep;
//        //    for (int i = 0; i < popups.Count; i++)
//        //    {
//        //        var entry = popups[i];
//        //        if (entry.Page.IsDisposed)
//        //        {
//        //            popups.RemoveAt(i);
//        //            Logger.Log($"popups list remove {entry.Page?.PrefabName}");
//        //            i--;
//        //            continue;
//        //        }
//        //        if (!entry.Page.IsLoading && entry.Page.HasCanvas)
//        //        {
//        //            int order = entry.Page.SortingOrder;
//        //            if (order < InitialTopMostOrder && order > maxOrder)
//        //            {
//        //                maxOrder = order;
//        //            }
//        //        }
//        //    }
//        //    if (popups.Count == 1)
//        //    {
//        //        var entry = popups[0];
//
//        //        if (!entry.Page.IsLoading && entry.Page.HasCanvas && !entry.Page.IsTopMost)
//        //        {
//        //            entry.Page.SortingOrder = InitialPopupOrder;
//        //            entry.Page.PlaneDistance = InitialPopupZ;
//        //            maxOrder = InitialPopupOrder;
//        //        }
//        //    }
//        //    curPopupOrder = maxOrder + PageGrowStep;
//        //}
//
//        //void RefreshTopMostOrder()
//        //{
//        //    int maxOrder = InitialTopMostOrder - PageGrowStep;
//        //    for (int i = 0; i < popups.Count; i++)
//        //    {
//        //        var entry = popups[i];
//        //        if (entry.Page.IsDisposed)
//        //        {
//        //            popups.RemoveAt(i);
//        //            Logger.Log($"popups list remove {entry.Page?.PrefabName}");
//        //            i--;
//        //            continue;
//        //        }
//        //        if (!entry.Page.IsLoading && entry.Page.HasCanvas)
//        //        {
//        //            int order = entry.Page.SortingOrder;
//        //            if (order > maxOrder)
//        //            {
//        //                maxOrder = order;
//        //            }
//        //        }
//        //    }
//
//        //    if (popups.Count == 1)
//        //    {
//        //        var entry = popups[0];
//
//        //        if (!entry.Page.IsLoading && entry.Page.HasCanvas && entry.Page.IsTopMost)
//        //        {
//        //            entry.Page.SortingOrder = InitialTopMostOrder;
//        //            entry.Page.PlaneDistance = InitialTopMostZ;
//        //            maxOrder = InitialTopMostOrder;
//        //        }
//        //    }
//        //    curTopMostOrder = maxOrder + PageGrowStep;
//        //}
//
//        public GamePage GetCurrentMainPage(string group)
//        {
//            var pageStack = GetCurrentMainPageStack(group);
//            if (pageStack.Stack != null && pageStack.Stack.Peek(out var top))
//            {
//                return top.Page;
//            }
//            return null;
//        }
//
//        public void CloseCurrentMainPage(string group)
//        {
//            var pageStack = GetCurrentMainPageStack(group);
//            if (pageStack.Stack.Peek(out var top))
//            {
//                CloseMainPage(top.Page);
//            }
//        }
//
//        //public bool CheckNeedWaitPreload(GamePage page, out GamePage previousPage)
//        //{
//        //    var pageStack = GetCurrentMainPageStack(page.PageGroup);
//        //    var curStack = pageStack.Stack;
//
//        //    Logger.Assert(page.IsMainPage, $"Page {page.PrefabName} is not main page");
//        //    Logger.Assert(curStack.Peek().Page == page, $"Page {page.PrefabName} is not at the top of current context");
//        //    var curEntry = curStack.Pop();
//        //    bool res = false;
//        //    previousPage = null;
//        //    if (curStack.Count > 0)
//        //    {
//        //        var previousEntry = curStack.Peek();
//        //        previousPage = previousEntry.Page;
//        //        res = previousPage.UILogic.NeedPreloadOnShow;
//        //    }
//        //    curStack.Push(curEntry);
//        //    return res;
//        //}
//
//        public void CloseAllPagesAfter(string prefabName, string pageGroup)
//        {
//            try
//            {
//                var pageStack = GetCurrentMainPageStack(pageGroup);
//                var curStack = pageStack.Stack;
//
//                bool found = false;
//                int foundIdx = -1;
//                //先找Popup再找MainPage
//                //TODO: 从栈里以前Mainpage的附属Popup里寻找
//                var popups = GetCurPopupsByGroup(pageGroup);
//                for (int i = popups.Count - 1; i >= 0; i--)
//                {
//                    var entry = popups[i];
//                    //TopMost的页面会被排除
//                    if (entry.Page.IsTopMost)
//                        continue;
//                    if (entry.Page.PrefabName == prefabName)
//                    {
//                        found = true;
//                        foundIdx = i;
//                        break;
//                    }
//                }
//                if (found)
//                {
//                    for (int i = foundIdx; i < popups.Count; i++)
//                    {
//                        var entry = popups[i];
//                        if (entry.Page.IsTopMost)
//                            continue;
//                        entry.Page.CloseInternal(true);
//                        popups.RemoveAt(i);
//                        Logger.Log($"popups list remove {entry.Page?.PrefabName}");
//                        i--;
//                    }
//                    //RefreshPopupOrder();
//                    return;
//                }
//
//                found = false;
//                int foundCount = 0;
//                foreach (var i in curStack)
//                {
//                    if (i.Page.PrefabName == prefabName)
//                    {
//                        foundCount++;
//                    }
//                }
//                if (foundCount > 0)
//                {
//                    CloseAllPopups(false,true, pageGroup);
//                    bool shouldBreak = false;
//                    do
//                    {
//                        if (curStack.Pop(out var entry))
//                        {
//                            if (entry.Page.PrefabName == prefabName)
//                            {
//                                shouldBreak = true;
//                            }
//                            entry.Page.CloseInternal(foundCount < 2);
//                            if (entry.Popups != null)
//                            {
//                                foreach (var i in entry.Popups)
//                                {
//                                    if (i.Page.IsDisposed)
//                                        continue;
//                                    i.Page.CloseInternal(true);
//                                }
//                                entry.Popups.Clear();
//                            }
//                        }
//                    }
//                    while (!shouldBreak);
//
//                    if (curStack.Peek(out var prevEntry))
//                    {
//                        prevEntry.RefreshPageContext();
//                        var prev = prevEntry.Page;
//                        var ctx = prevEntry.Context;
//                        ctx.ReturnValue = null;
//                        //prev.SetContentContext(ctx);
//                        prev.ShowInternal(prev.IsClosing);
//                        //curPopupOrder = prevEntry.CurrentPopupOrder;
//                        bool hasHideLower = false;
//                        if (prevEntry.Popups != null)
//                        {
//                            foreach (var i in prevEntry.Popups)
//                            {
//                                if (i.Page.IsDisposed)
//                                    continue;
//                                if (i.Page.ShouldHideLowerPage)
//                                    hasHideLower = true;
//                                i.RefreshPageContext();
//                                i.Page.ShowInternal(i.Page.IsClosing);
//                                if (!i.Page.ShouldHideLowerPage && hasHideLower)
//                                {
//                                    //如果不是遮挡下方的且下方有遮挡其他界面的，新开的popup也依然应该在上面
//                                    i.Page.UILogic?.OnHideOtherPage(true, false, false);
//                                }
//
//                                popups.Add(i);
//                                Logger.Log($"popups list add {i.Page?.PrefabName}");
//                            }
//                            prevEntry.Popups.Clear();
//                        }
//                        //SortPopups();
//                    }
//                    else
//                    {
//                        if (!string.IsNullOrEmpty(pageGroup) && mainPageGroups != null)
//                        {
//                            mainPageGroups.Remove(pageGroup);
//                            int maxMainPageSorting = MainPageOrder - PageGrowStep;
//                            foreach (var i in mainPageGroups)
//                            {
//                                if (i.Value.StackSortingOrder > maxMainPageSorting)
//                                    maxMainPageSorting = i.Value.StackSortingOrder;
//                            }
//                            //curMainPageOrder = maxMainPageSorting + PageGrowStep;
//                        }
//                    }
//                }
//            }
//            
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        public void CloseMainPage(GamePage page)
//        {
//            try
//            {
//                var pageStack = GetCurrentMainPageStack(page.PageGroup);
//                var curStack = pageStack.Stack;
//                Logger.Assert(page.IsMainPage, $"Page {page.PrefabName} is not main page");
//
//                int index = -1;
//                for (int i = curStack.Count-1; i >= 0; i--)
//                {
//                    if (curStack[i].Page == page)
//                    {
//                        index = i;
//                        break;
//                    }
//                }
//
//                bool presentInStack = (index != -1);
//                bool isTop = (presentInStack && index == curStack.Count - 1);
//
//                if (presentInStack)
//                {
//                    //清空界面对应的Popup
//                    var entryPopups = curStack[index].Popups;
//                    if (entryPopups != null && entryPopups.Count > 0)
//                    {
//                        foreach (var pEntry in entryPopups)
//                        {
//                            // 宿主页面被销毁，依附的 Popup 也应销毁
//                            if (!pEntry.Page.IsDisposed)
//                            {
//                                pEntry.Page.CloseInternal(true);
//                            }
//                        }
//                        entryPopups.Clear();
//                    }
//
//                    curStack.RemoveAt(index);
//                }
//                else
//                {
//                    Burner.Logger.Warn($"Page {page.PrefabName} not found in stack during CloseMainPage.");
//                }
//                var returnValue = page.ContentContext?.ReturnValue;
//                page.CloseInternal(true);
//                // CloseAllPopups(false,true,page.PageGroup);
//
//
//                if (isTop && curStack.Peek(out var prevEntry))
//                {
//                    prevEntry.RefreshPageContext();
//                    var prev = prevEntry.Page;
//                    prevEntry.Context.ReturnValue = returnValue;
//                    //prev.SetContentContext(ctx);
//                    prev.ShowInternal(prev.IsClosing);
//
//                    bool hasHideLower = false;
//                    if (prevEntry.Popups != null)
//                    {
//                        var popups = prevEntry.Popups;
//                        //需要将所有popup加回栈中后再进行页面打开，否则HideLower逻辑可能不正确
//                        foreach (var i in popups)
//                        {
//                            i.RefreshPageContext();
//                            if (i.Page.ShouldHideLowerPage)
//                                hasHideLower = true;
//                            i.Page.ShowInternal(i.Page.IsClosing);
//                            if (!i.Page.ShouldHideLowerPage && hasHideLower)
//                            {
//                                //如果不是遮挡下方的且下方有遮挡其他界面的，新开的popup也依然应该在上面
//                                i.Page.UILogic?.OnHideOtherPage(true, false, false);
//                            }
//                        }
//                        prev.RenderVisible = !hasHideLower;
//
//                        hasHideLower = false;
//                        for (int i = popups.Count - 1; i >= 0; i--)
//                        {
//                            if (popups[i].Page.IsDisposed)
//                                continue;
//                            if (!hasHideLower)
//                            {
//                                if (popups[i].Page.ShouldHideLowerPage)
//                                    hasHideLower = true;
//                                popups[i].Page.RenderVisible = true;
//                            }
//                            else
//                            {
//                                popups[i].Page.RenderVisible = false;
//                            }
//
//                        }
//                    }
//                }
//                else if (curStack.Count == 0)
//                {
//                    if (!string.IsNullOrEmpty(page.PageGroup) && mainPageGroups != null)
//                    {
//                        mainPageGroups.Remove(page.PageGroup);
//                        int maxMainPageSorting = MainPageOrder - PageGrowStep;
//                        foreach (var i in mainPageGroups)
//                        {
//                            if (i.Value.StackSortingOrder > maxMainPageSorting)
//                                maxMainPageSorting = i.Value.StackSortingOrder;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        public void ClosePopup(GamePage page,string group = null)
//        {
//            try
//            {
//                Logger.Assert(page.IsPopup, $"Page {page.PrefabName} is not a pop up");
//
//                //int lastTopmostIndex = -1;
//                //int lastPopIndex = -1;
//
//                var popups = page.IsTopMost ? topMostPopup : GetCurPopupsByGroup(group);
//                if (popups != null)
//                {
//                    for (int i = popups.Count - 1; i >= 0; i--)
//                    {
//                        var entry = popups[i];
//
//                        if (entry.Page == page)
//                        {
//                            popups.RemoveAt(i);
//                            Logger.Log($"popups list remove {entry.Page?.PrefabName}");
//                            page.CloseInternal(true);
//
//                            //if (entry.Page.IsTopMost&& lastTopmostIndex== i)
//                            //{
//                            //    RefreshTopMostOrder();
//                            //}
//                            //else if(lastPopIndex == i)
//                            //    RefreshPopupOrder();
//                            return;
//                        }
//                    }
//                }
//
//                foreach (var i in _mainPageList.Stack)
//                {
//                    if (i.Popups != null)
//                    {
//                        for (int j = 0; j < i.Popups.Count; j++)
//                        {
//                            var entry = i.Popups[j];
//                            if (entry.Page == page)
//                            {
//                                i.Popups.RemoveAt(j);
//                                page.CloseInternal(true);
//                                return;
//                            }
//                        }
//
//                    }
//                }
//
//                page.CloseInternal(true);
//
//                Logger.Warn($"Page {page.PrefabName} does not exist in current context");
//            }
//            
//            catch (Exception ex)
//            {
//                Burner.Logger.Exception(ex);
//            }
//        }
//
//        public void Dispose()
//        {
//            DisposeStack(_mainPageList);
//            if (mainPageGroups != null)
//            {
//                foreach (var i in mainPageGroups.ToArray())
//                {
//                    DisposeStack(i.Value);
//                }
//                mainPageGroups.Clear();
//            }
//            //foreach (var i in popups.ToArray())
//            //{
//            //    i.Page.CloseInternal(true, true);
//            //}
//            //popups.Clear();
//        }
//
//        void DisposeStack(MainPageList list)
//        {
//            while (list.Stack.Pop(out var cur))
//            {
//                cur.Page.DestroyValue = 0;
//                cur.Page.CloseInternal(true, true);
//                if (cur.Popups != null && cur.Popups.Count > 0)
//                {
//                    foreach (var i in cur.Popups)
//                    {
//                        i.Page.CloseInternal(true, true);
//                    }
//                }
//            }
//        }
//
//        public void HideAll(bool doClose, string groupName, bool preserveTopMost = true)
//        {
//            var pageStack = GetCurrentMainPageStack(groupName);
//            if (!doClose)
//            {
//                if (pageStack.Stack.Peek(out var entry))
//                {
//                    entry.Page.CloseInternal(false);
//                }
//            }
//            else
//            {
//                while (pageStack.Stack.Pop(out var entry))
//                {
//                    entry.Page.CloseInternal(true, true);
//                    if (entry.Popups != null && entry.Popups.Count > 0)
//                    {
//                        foreach (var i in entry.Popups)
//                        {
//                            i.Page.CloseInternal(true, true);
//                        }
//                    }
//                }
//            }
//            CloseAllPopups(!preserveTopMost, doClose, groupName);
//        }
//
//        //public void TransferTopMost(PageContext prevCtx)
//        //{
//        //    foreach (var i in prevCtx.popups)
//        //    {
//        //        if (i.Page.IsTopMost && !HasCertainPopup(i.Page))
//        //        {
//        //            i.Page.CurrentContext = this;
//        //            i.Page.SetContentContext(i.Context);
//        //            i.Page.SortingOrder = IncreaseTopMostOrder();
//        //            i.Page.ShowInternal(false);
//        //            popups.Add(i);
//        //            Logger.Log($"popups list add {i.Page?.PrefabName}");
//        //        }
//        //    }
//        //    SortPopups();
//        //}
//
//        //bool HasCertainPopup(GamePage page)
//        //{
//        //    foreach (var i in popups)
//        //    {
//        //        if (i.Page == page)
//        //            return true;
//        //    }
//        //    return false;
//        //}
//
//        //public void Restore()
//        //{
//        //    if (_mainPageList.Stack.Count > 0)
//        //    {
//        //        var entry = _mainPageList.Stack.Peek();
//        //        var page = entry.Page;
//        //        page.CurrentContext = this;
//        //        page.SetContentContext(entry.Context);
//        //        page.ShowInternal(false);
//        //    }
//        //    if (mainPageGroups != null)
//        //    {
//        //        foreach (var i in mainPageGroups)
//        //        {
//        //            if (i.Value.Stack.Count > 0)
//        //            {
//        //                var entry = i.Value.Stack.Peek();
//        //                var page = entry.Page;
//        //                page.CurrentContext = this;
//        //                page.SetContentContext(entry.Context);
//        //                page.ShowInternal(false);
//        //            }
//        //        }
//        //    }
//        //    if (popups.Count > 0)
//        //    {
//        //        foreach (var i in popups)
//        //        {
//        //            var page = i.Page;
//
//        //            page.CurrentContext = this;
//        //            page.SetContentContext(i.Context);
//        //            page.ShowInternal(false);
//        //        }
//        //    }
//        //}
//
//        private List<PageContextEntry> topMostPopup = new List<PageContextEntry>();
//
//        public void CloseAllPopups(bool includingTopMost, bool doClose = true,string group = null)
//        {
//            //bool hasRemaining = false;
//            var popups = GetCurPopupsByGroup(group);
//            for (int i = 0; i < popups?.Count; i++)
//            {
//                var entry = popups[i];
//                if (!entry.Page.IsTopMost || includingTopMost)
//                {
//                    int last = popups.Count - 1;
//                    if (last > i)
//                    {
//                        popups[i] = popups[popups.Count - 1];
//                        i--;
//                    }
//                    popups.RemoveAt(last);
//                    entry.Page.CloseInternal(doClose);
//                }
//            }
//
//            if (includingTopMost)
//            {
//                for (int i = 0; i < topMostPopup?.Count; i++)
//                {
//                    var entry = popups[i];
//                    if (entry.Page.PageGroup.Equals(group))
//                    {
//                        if (!entry.Page.IsTopMost || includingTopMost)
//                        {
//                            int last = popups.Count - 1;
//                            if (last > i)
//                            {
//                                popups[i] = popups[popups.Count - 1];
//                                i--;
//                            }
//                            popups.RemoveAt(last);
//                            entry.Page.CloseInternal(doClose);
//                        }
//                    }
//                }
//            }
//
//            //if (hasRemaining)
//            //{
//            //    RefreshTopMostOrder();
//            //}
//            //else
//            //    curTopMostOrder = InitialTopMostOrder;
//            //curPopupOrder = InitialPopupOrder;
//        }
//
//        public int GetPopupsCountByGroup(string group = null, bool includeTopMost = false) => GetCurPopupsByGroup(group)?.Count ?? 0 + GetTopMostPopupsCount(group);
//
//        public List<PageContextEntry> GetCurPopupsByGroup(string group = null)
//        {
//            if (string.IsNullOrEmpty(group))
//            {
//                return _mainPageList.Stack?.Peek(out var mainTop) == true ? mainTop?.Popups : null;
//            }
//
//            return mainPageGroups?.TryGetValue(group, out var stack) == true
//                ? stack.Stack?.Peek(out var top) == true ? top.Popups : null
//                : null;
//        }
//
//        public List<PageContextEntry> GetTopMostPopupsByGroup(string group = null)
//        {
//            if (topMostPopup?.Count > 0)
//            {
//                List<PageContextEntry> result = new List<PageContextEntry>();
//                for (int i = 0; i < topMostPopup.Count; i++)
//                {
//                    var popup = topMostPopup[i];
//                    if (popup?.Page != null && string.Equals(popup.Page.PageGroup, group))
//                    {
//                        result.Add(popup);
//                    }
//                }
//
//                return result;
//            }
//            return null;
//        }
//
//        public int GetTopMostPopupsCount(string group = null)
//        {
//            int count = 0;
//            if (topMostPopup?.Count > 0)
//            {
//                for (int i = 0; i < topMostPopup.Count; i++)
//                {
//                    var popup = topMostPopup[i];
//                    if (popup?.Page != null && string.Equals(popup.Page.PageGroup, group))
//                    {
//                        count++;
//                    }
//                }
//            }
//            return count;
//        }
//
//        public bool HasPopup(string group = null, bool includeTopMost = false) => GetCurPopupsByGroup(group)?.Count > 0 && (!includeTopMost || GetTopMostPopupsCount(group) > 0);
//
//        private int GetLastSortingOrderByGroup(string group = null)
//        {
//            var pageList = GetCurrentMainPageStack(group, true);
//
//            if (!pageList.Stack.Peek(out var topMainPageEntry))
//                return pageList.StackSortingOrder;
//
//            int? lastPopupSortingOrder = null;
//            if (topMainPageEntry?.Popups?.Count > 0)
//            {
//                var lastPopup = topMainPageEntry.Popups[topMainPageEntry.Popups.Count - 1];
//                lastPopupSortingOrder = lastPopup?.Context?.SortingOrder;
//            }
//
//            // 确定基准排序值，按优先级回退
//            int baseSortingOrder = lastPopupSortingOrder
//                ?? topMainPageEntry?.Context?.SortingOrder
//                ?? pageList.StackSortingOrder;
//
//            return PageGrowStep + baseSortingOrder;
//        }
//
//        private float GetLastPlanDistanceOrderByGroup(string group = null)
//        {
//            var pageList = GetCurrentMainPageStack(group, true);
//
//            if (!pageList.Stack.Peek(out var topMainPageEntry))
//                return InitialPopupZ;
//
//            float? lastPopupPlanDistance = null;
//            if (topMainPageEntry?.Popups?.Count > 0)
//            {
//                var lastPopup = topMainPageEntry.Popups[topMainPageEntry.Popups.Count - 1];
//                lastPopupPlanDistance = lastPopup?.Context?.PlaneDistance;
//            }
//
//            // 确定基准排序值，按优先级回退
//            float basePlanDistance = lastPopupPlanDistance
//                ?? topMainPageEntry?.Context?.PlaneDistance
//                ?? InitialPopupZ;
//
//            return Mathf.Max(PageZGrowStep + basePlanDistance,10);
//        }
//    }
//}
