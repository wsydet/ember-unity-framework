//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using Burner.Basic;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class GameUIContainer : GameUIComponent
//    {
//        public delegate void RefreshChildDelegate(IUIBehaviour behaviour, int index, object data);
//        private UIContainer container;
//        bool refreshActionAdded;
//        RefreshChildDelegate refreshAction;
//        
//        internal Action<IUIBehaviour, int> OnFocus { get; set; }
//
//        public int CurrentInstantiationPerFrame
//        {
//            get => container.CurrentInstantiationPerFrame;
//            set
//            {
//                container.CurrentInstantiationPerFrame = value;
//            }
//        }
//
//        public bool SupressRecyleModeCheck
//        {
//            get => container.SupressRecyleModeCheck;
//            set
//            {
//                container.SupressRecyleModeCheck = value;
//            }
//        }
//
//        public int VisibleElementCount => container.VisibleElementCount;
//
//        public Vector2 ChildVisibleRectShiftBufferSize
//        {
//            set
//            {
//                container.ChildVisibleRectShiftBufferSize = value;
//            }
//        }
//        
//        public bool DiscardRecycleModeCheck
//        {
//            get => container.DiscardRecycleModeCheck;
//            set
//            {
//                container.DiscardRecycleModeCheck = value;
//            }
//        }
//
//
//
//        public override void OnInit()
//        {
//            container = GetComponent<UIContainer>();
//            container.Initialize();
//            DoResetDispose(true);
//        }
//
//        public override void OnDispose()
//        {
//            base.OnDispose();
//            if (m_delayShowCoroutine != null)
//            {
//                container.StopCoroutine(m_delayShowCoroutine);
//                m_delayShowCoroutine = null;
//            }
//            container.DisposeChildren(destroying);
//            container.OnCreateChild -= OnCreateChild;
//            container.OnDisposeChild -= OnDisposeChild;
//            container.OnPoolSharedChild -= OnPoolSharedChild;
//            container.OnRefreshChild -= HandleRefreshChild;
//            container.OnStreamingOut -= HandleStreamingOutChild;
//            container.OnFocusChild -= HandleFocusChild;
//            if (container.IsPredefined)
//            {
//                foreach (var i in container.PredefinedChildren)
//                {
//                    OnDisposeChild(i, destroying);
//                }
//            }
//        }
//
//        protected override void OnResetDispose()
//        {
//            DoResetDispose(false);
//        }
//
//        void DoResetDispose(bool create)
//        {
//            container.OnCreateChild = OnCreateChild;
//            container.OnDisposeChild = OnDisposeChild;
//            container.OnRefreshChild = HandleRefreshChild;
//            container.OnShowChild = HandleShowChild;
//            container.OnHideChild = HandleHideChild;
//            container.OnStreamingOut = HandleStreamingOutChild;
//            container.OnFocusChild = HandleFocusChild;
//            container.OnPoolSharedChild = OnPoolSharedChild;
//            if (container.IsPredefined)
//            {
//                foreach (var i in container.PredefinedChildren)
//                {
//                    OnCreateChild(i, create);
//                }
//            }
//        }
//
//        public void ShareObjectPool(GameUIContainer origin)
//        {
//            if (UILogic.Page != origin.UILogic.Page)
//                throw new NotSupportedException("Only container within the same page can be shared");
//            container.ShareObjectPool(origin.container);
//        }
//
//        public void EnsureSize(int count)
//        {
//            container.EnsureSize(count);
//        }
//
//        public void SetChildrenData(IList<UIContainerData> data)
//        {
//            container.SetChildrenData(data);
//        }
//
//        public IUIBehaviour AddChild(object data = null, int templateIdx = 0)
//        {
//            var go = container.AddChild(data, templateIdx);
//            if (go)
//            {
//                var etl = EventTriggerListener.Get(go);
//                if (etl && etl.parameter is IUIBehaviour behaviour)
//                {
//                    return behaviour;
//                }
//            }
//            return null;
//        }
//
//        public void AddChildRecycled(object data)
//        {
//#pragma warning disable
//            container.AddChildRecycled(data);
//#pragma warning enable
//        }
//
//        public void Clear()
//        {
//            container.Clear(false);
//        }
//
//        public void RemoveChildByIndex(int idx)
//        {
//            container.RemoveChild(idx);
//        }
//
//        public void RemoveChild(IUIBehaviour behaviour)
//        {
//            if (behaviour is GameUILogic logic)
//                container.RemoveChild(logic.UIComponent.GameObject);
//            else
//                container.RemoveChild(((GameUIComponent)behaviour).GameObject);
//        }
//
//        public int Count => container.ChildCount;
//
//        IUIBehaviour GetChild(int index)
//        {
//            var go = container.GetChildByIndex(index);
//            if (go)
//            {
//                var etl = EventTriggerListener.Get(go);
//                if (etl && etl.parameter is IUIBehaviour behaviour)
//                {
//                    return behaviour;
//                }
//            }
//            return null;
//        }
//        public int GetChildIndexByName(string name)
//        {
//            return container.GetChildIndexByName(name);
//        }
//
//        public Rect GetChildRect(int index)
//        {
//            return container.GetChildRect(index);
//        }
//
//        public Vector2 GetPivot(int index)
//        {
//            return container.GetPivot(index);
//        }
//
//        public void RefreshItems()
//        {
//            container.RefreshItems();
//        }
//
//        public T GetChild<T>(int index) where T : class, IUIBehaviour
//        {
//            return GetChild(index) as T;
//        }
//
//        void OnCreateChild(GameObject go, bool create)
//        {
//            var etl = EventTriggerListener.Get(go);
//            if (create)
//            {
//                var behaviour = go.CreateUIBehaviour(UILogic, gameObject.name, container.TemplateType, container.TemplateClassName);
//                etl.parameter = behaviour;
//                UILogic.AddBehaviour(behaviour);
//            }
//            else
//            {
//                if (etl.parameter is GameUILogic logic)
//                {
//                    logic.ResetDispose(true);
//                }
//                else
//                {
//                    ((GameUIComponent)etl.parameter).ResetDispose(true);
//                }
//            }
//
//        }
//
//        public void SetChildPosition(int idx, float x = 0, float y = 0)
//        {
//            container.SetChildPosition(idx, x, y); 
//        }
//        public void SetChildData(int idx, object data, bool setPos = false, float x = 0, float y = 0)
//        {
//            container.SetChildData(idx, data, setPos, x, y);
//        }
//
//        public void SetChildData(int idx, UIContainerData data, bool setPos = false)
//        {
//            container.SetChildData(idx, ref data, setPos);
//        }
//
//        public void SetChildDataByName(string name, object data, bool setPos = false, float x = 0, float y = 0)
//        {
//            container.SetChildDataByName(name, data, setPos, x, y);
//        }
//
//        public override void OnClose()
//        {
//            for(int i = 0; i < container.RealChildCount; i++)
//            {
//                var go = container.GetChildByIndexInternal(i).GameObject;
//                IUIBehaviour behaviour = null;
//                if (go)
//                {
//                    var etl = EventTriggerListener.Get(go);
//                    if (etl && etl.parameter is IUIBehaviour b)
//                    {
//                        behaviour = b;
//                    }
//                }
//                if (behaviour is GameUILogic logic)
//                    logic.DoClose();
//                else
//                    behaviour?.OnClose();
//            }
//        }
//
//        void OnPoolSharedChild(GameObject go, bool inPool)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//            if (inPool)
//            {
//                UILogic.AddBehaviour(behaviour);
//            }
//            else
//            {
//                UILogic.RemoveBehaviour(behaviour);
//            }
//        }
//
//        void OnDisposeChild(GameObject go, bool destroy)
//        {
//            try
//            {
//                var etl = EventTriggerListener.Get(go);
//                var behaviour = etl.parameter as IUIBehaviour;
//                if (behaviour != null)
//                {
//                    if (behaviour is GameUILogic logic)
//                    {
//                        logic.ResetDispose(!destroy);
//                        logic.DoDispose(destroy);
//                    }
//                    else
//                    {
//                        ((GameUIComponent)behaviour).ResetDispose(!destroy);
//                        ((GameUIComponent)behaviour).DoDispose(destroy);
//                    }
//
//                    if (destroy)
//                    {
//                        UILogic.RemoveBehaviour(behaviour);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (Widget)
//                    Burner.Logger.Error($"Error while disposing child of ({Widget.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})\n{e}");
//                else
//                    Burner.Logger.Error($"Error while disposing child of UIContainer inside ({UILogic.PagePrefabName})\n{e}");
//            }
//        }
//
//        public void SetRecycleMode(bool enabled, RefreshChildDelegate updateCb = null)
//        {
//            container.SetRecycleMode(enabled);
//
//            if (!enabled)
//                refreshAction = null;
//            else
//                refreshAction = updateCb;
//        }
//
//        void HandleRefreshChild(GameObject go, int index, object data)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//            if (behaviour is GameUILogic logic)
//            {
//                logic.OnRefreshItem(data, index);
//            }
//            refreshAction?.Invoke(behaviour, index, data);
//        }
//
//        void HandleShowChild(GameObject go)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//            if (behaviour is GameUILogic logic)
//            {
//                logic.DoShow();
//            }
//        }
//
//        void HandleHideChild(GameObject go)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//            if (behaviour is GameUILogic logic)
//            {
//                logic.DoHide();
//            }
//        }
//
//        void HandleStreamingOutChild(GameObject go)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//            if (behaviour is GameUILogic logic)
//            {
//                logic.OnStreamingOut();
//            }
//        }
//
//        void HandleFocusChild(GameObject go, int index)
//        {
//            var etl = EventTriggerListener.Get(go);
//            var behaviour = etl.parameter as IUIBehaviour;
//
//            OnFocus?.Invoke(behaviour, index);
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            refreshAction = null;
//        }
//
//        public void RefreshLayout()
//        {
//            container.RefreshLayout();
//        }
//        
//        
//
//        /// <summary>
//        /// 刷新子节点（支持间隔调用）
//        /// </summary>
//        /// <param name="dataList">数据列表</param>
//        /// <param name="interval">间隔时长</param>
//        /// <param name="onDelayCall">间隔回调，空时默认按间隔实例化</param>
//        public void SetChildrenDataList(IList dataList)
//        {
//            if (m_delayShowCoroutine != null)
//            {
//                container.StopCoroutine(m_delayShowCoroutine);
//                m_delayShowCoroutine = null;
//            }
//            
//            EnsureSize(dataList.Count);
//            for (int i = 0; i < dataList.Count; i++)
//            {
//                SetChildData(i,dataList[i]);
//            }
//        }
//        
//        
//        private Coroutine m_delayShowCoroutine;
//        private WaitForSeconds delayShowWait;
//
//        /// <summary>
//        /// 刷新子节点（支持间隔调用）
//        /// </summary>
//        /// <param name="dataList">数据列表</param>
//        /// <param name="interval">间隔时长</param>
//        /// <param name="onDelayCall">间隔回调，空时默认按间隔实例化</param>
//        public void SetChildrenDataList(IList dataList, float interval, Action<GameUILogic> onDelayCall = null, Action onCompleteCall = null, Action<int> onExecuteCall = null)
//        {
//            if (m_delayShowCoroutine != null)
//            {
//                container.StopCoroutine(m_delayShowCoroutine);
//                m_delayShowCoroutine = null;
//            }
//    
//
//            //如果间隔小于0或回调不为空，就直接将所有Item刷出来
//            if (interval <= 0||onDelayCall!=null)
//            {
//                EnsureSize(dataList.Count);
//                for (int i = 0; i < dataList.Count; i++)
//                {
//                    SetChildData(i,dataList[i]);
//                }
//            }
//            
//            if(interval>0)
//            {
//                delayShowWait = new WaitForSeconds(interval);
//                m_delayShowCoroutine = container.StartCoroutine(ShowChildrenWithDelay(dataList,onDelayCall, onCompleteCall, onExecuteCall));
//            }
//        }
//
//        private IEnumerator ShowChildrenWithDelay(IList dataList,Action<GameUILogic> onDelayCall, Action onCompleteCall = null, Action<int> onExecuteCall = null)
//        {
//            for (int i = 0; i < dataList.Count; i++)
//            {
//                if (onDelayCall == null)
//                {
//                    EnsureSize(i+1);
//                    SetChildData(i, dataList[i]);
//                    onExecuteCall?.Invoke(i);
//                }
//                else
//                {
//                    onDelayCall.Invoke(GetChild<GameUILogic>(i));
//                }
//                yield return delayShowWait;
//            }
//            m_delayShowCoroutine = null;
//            onCompleteCall?.Invoke();
//        }
//
//        public ScrollRect GetScrollRect()
//        {
//            if (container == null)
//                return null;
//            return container.GetScrollRect();
//        }
//    }
//}
