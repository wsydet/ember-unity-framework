//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//
//namespace Burner.UIExtension
//{
//    public class GameScrollRect : GameUIComponent
//    {
//        ScrollRect scroll;
//        bool lockPos;
//        float lockTime;
//        Vector2 normalizedPos;
//        Vector2 curNormalizedPos;
//        Vector2 scrollSpeed;
//        bool scrolling;
//        float scrollTime;
//        Action<IUIBehaviour> onFocusCb, onSnapCb;
//        int focusingChildIdx;
//        int startingFocusingChildIdx;
//        int pendingSnapingIdx = -1;
//        IUIBehaviour focusingBehaviour;
//        bool shouldSnap = false;
//        float snapingTime = 0;
//
//        public Action<float> OnScrollChange;
//
//        GameUIContainer snapingContainer;
//        public override void OnInit()
//        {
//            scroll = GetComponent<ScrollRect>();
//            scroll.onValueChanged.AddListener(OnValueChange);
//        }
//        /// <summary>
//        /// 获取或设置
//        /// </summary>
//        public bool IsElastic
//        {
//            get => scroll.movementType == ScrollRect.MovementType.Elastic;
//            set
//            {
//                if (value)
//                    scroll.movementType = ScrollRect.MovementType.Elastic;
//                else
//                    scroll.movementType = ScrollRect.MovementType.Clamped;
//            }
//        }
//
//        public override bool Enable
//        {
//            get
//            {
//                return scroll.enabled;
//            }
//            set
//            {
//                scroll.enabled = value;
//            }
//        }
//
//        public bool NeedScrollHorizontal
//        {
//            get
//            {
//                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
//                return scroll.content.rect.width > scroll.viewport.rect.width + 0.1f;
//            }
//        }
//
//        public bool NeedScrollVertical
//        {
//            get
//            {
//                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
//                return scroll.content.rect.height > scroll.viewport.rect.height + 0.1f;
//            }
//        }
//
//        public float CurrentPosition
//        {
//            get
//            {
//                if (scroll.vertical)
//                    return scroll.verticalNormalizedPosition;
//                else
//                    return scroll.horizontalNormalizedPosition;
//            }
//            set
//            {
//                if (scroll.vertical)
//                {
//                    scroll.verticalNormalizedPosition = value;
//                }
//                else
//                {
//                    scroll.horizontalNormalizedPosition = value;
//                }
//            }
//        }
//
//        public Vector2 ViewportSize
//        {
//            get
//            {
//                return scroll.viewport.rect.size;
//            }
//        }
//
//        public Vector2 ContentSize
//        {
//            get
//            {
//                return scroll.content.rect.size;
//            }
//        }
//
//        public void ScrollToEnd(float scrollTime = 0)
//        {
//            if (!Enable)
//                return;
//            ScrollToInternal(new Vector2(1, 0), scrollTime);
//        }
//
//        public void ScrollToBegin(float scrollTime = 0)
//        {
//            if (!Enable)
//                return;
//            ScrollToInternal(new Vector2(0, 1), scrollTime);
//        }
//
//        public void ScrollTo2D(float x, float y, float scrollTime = 0)
//        {
//            if (!Enable)
//                return;
//            ScrollToInternal(new Vector2(x, 1 - y), scrollTime);
//        }
//
//        public void ScrollTo(float val, float scrollTime = 0)
//        {
//            if (!Enable)
//                return;
//
//            ScrollToInternal(new Vector2(val, 1 - val), scrollTime);
//        }
//
//        public void ScrollByPixel(float x, float y, float scrollTime = 0)
//        {
//            if (!Enable)
//                return;
//            var curPos = scroll.normalizedPosition;
//            curPos.x += GetNormalizedSize(x, true);
//            curPos.y -= GetNormalizedSize(y);
//
//            ScrollToInternal(curPos, scrollTime);
//        }
//
//        static float SafeClamp01(float val)
//        {
//            if (float.IsNaN(val))
//                return 0;
//            return Mathf.Clamp01(val);
//        }
//
//        public float GetNormalizedSize(float pixelSize, bool isHorizontal = false)
//        {
//            var contentSize = scroll.content.rect.size - scroll.viewport.rect.size;
//            float res = isHorizontal ? pixelSize / contentSize.x : pixelSize / contentSize.y;
//            return SafeClamp01(res);
//        }
//
//        public void ScrollToItem(IUIBehaviour behaviour, bool center = false, float scrollTime = 0, bool toBottom = false, float offsetX = 0, float offsetY = 0)
//        {
//            if (!Enable)
//                return;
//
//            var comp = behaviour is GameUIComponent ? ((GameUIComponent)behaviour) : ((GameUILogic)behaviour).UIComponent;
//            var rt = comp.Widget;
//            var size = rt.rect.size;
//            var halfSize = size / 2f;
//            var contentSize = scroll.content.rect.size - scroll.viewport.rect.size;
//
//            Vector2 anchorPos;
//            if (center)
//            {
//                anchorPos = halfSize;
//            }
//            else if (!toBottom)
//            {
//                anchorPos = new Vector2(0, 1) * size;
//            }
//            else
//            {
//                anchorPos = new Vector2(1, 0.5f) * size;
//            }
//
//            var pos = rt.GetRelativePosWithAnchor(scroll.content, anchorPos);
//            pos.y = -pos.y;
//            if (center)
//            {
//                pos.x -= scroll.viewport.rect.size.x / 2f;
//                pos.y -= scroll.viewport.rect.size.y / 2f;
//            }
//            else if (toBottom)
//            {
//                pos.x = pos.x - scroll.viewport.rect.size.x;
//                pos.y = pos.y - scroll.viewport.rect.size.y;
//            }
//            pos.x += offsetX;
//            pos.y += offsetY;
//            var value = center ? (pos) / contentSize : pos / contentSize;
//            value.x = SafeClamp01(value.x);
//            value.y = 1f - SafeClamp01(value.y);
//
//            ScrollToInternal(value, scrollTime);
//        }
//
//        public void ScrollToTransform(Transform trans, float scrollTime = 0, float offsetX = 0, float offsetY = 0)
//        {
//            if (!Enable)
//                return;
//
//            var rt = trans;
//            var contentSize = scroll.content.rect.size - scroll.viewport.rect.size;
//
//            var pos = rt.GetRelativePosWithAnchor(scroll.content);
//            pos.y = -pos.y;
//            pos.x += offsetX;
//            pos.y += offsetY;
//            pos.x -= scroll.viewport.rect.size.x / 2f;
//            pos.y -= scroll.viewport.rect.size.y / 2f;
//            var value = pos / contentSize;
//            value.x = SafeClamp01(value.x);
//            value.y = 1f - SafeClamp01(value.y);
//
//            ScrollToInternal(value, scrollTime);
//        }
//
//        void ScrollToInternal(Vector2 pos, float scrollTime)
//        {
//#if UNITY_EDITOR
//            if (!scroll.gameObject.activeInHierarchy)
//                Burner.Logger.Error($"Cannot use Scroll To with inactive scroll rect({Widget.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})");
//#endif
//            normalizedPos = pos;
//            if (scrollTime < 0.01f)
//            {
//                scroll.normalizedPosition = normalizedPos;
//                lockPos = true;
//                scrolling = false;
//                lockTime = Time.realtimeSinceStartup;
//            }
//            else
//            {
//                curNormalizedPos = scroll.normalizedPosition;
//                lockPos = false;
//                scrolling = true;
//                this.scrollTime = scrollTime;
//            }
//        }
//
//        public void RefreshContentLayout()
//        {
//            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
//        }
//
//        public void ScrollToContainerChildByName(GameUIContainer container, string name, bool center = false, float scrollTime = 0, bool toBottom = false, float offsetX = 0, float offsetY = 0)
//        {
//            var idx = container.GetChildIndexByName(name);
//            ScrollToContainerChild(container, idx, center, scrollTime, toBottom, offsetX, offsetY);
//        }
//        public void ScrollToContainerChild(GameUIContainer container, int index, bool center = false, float scrollTime = 0, bool toBottom = false, float offsetX = 0, float offsetY = 0)
//        {
//            if (!Enable)
//                return;
//
//            if (index < 0 || index >= container.Count)
//                return;
//            var rect = container.GetChildRect(index);
//
//            var size = rect.size;
//            size.y = -size.y;
//            var halfSize = size / 2f;
//            var contentSize = scroll.content.rect.size - scroll.viewport.rect.size;
//
//            Vector2 anchorPos;
//            if (center)
//            {
//                anchorPos = rect.position + halfSize;
//            }
//            else if (!toBottom)
//            {
//                anchorPos = rect.position;
//            }
//            else
//            {
//                anchorPos = rect.position + size;
//            }
//            var pos = container.Widget.GetRelativePosWithAnchor(scroll.content, anchorPos);
//            pos.y = -pos.y;
//            if (center)
//            {
//                pos.x -= scroll.viewport.rect.size.x / 2f;
//                pos.y -= scroll.viewport.rect.size.y / 2f;
//            }
//            else if (toBottom)
//            {
//                pos.x = pos.x - scroll.viewport.rect.size.x;
//                pos.y = pos.y - scroll.viewport.rect.size.y;
//            }
//            pos.x += offsetX;
//            pos.y += offsetY;
//            var value = center ? (pos) / contentSize : pos / contentSize;
//            value.x = SafeClamp01(value.x);
//            value.y = 1f - SafeClamp01(value.y);
//
//            ScrollToInternal(value, scrollTime);
//        }
//
//        public bool IsItemVisible(IUIBehaviour behaviour)
//        {
//            var comp = behaviour is GameUIComponent ? ((GameUIComponent)behaviour) : ((GameUILogic)behaviour).UIComponent;
//            var rt = comp.Widget;
//            
//            var pos = rt.GetRelativePosWithAnchor(scroll.viewport);
//            pos.y = -pos.y;
//
//            var itemRect = new Rect(pos, rt.rect.size);
//            var viewportRect = new Rect(Vector2.zero, scroll.viewport.rect.size);
//
//            return itemRect.Intersect(viewportRect);
//        }
//
//        public bool IsContainerChildVisible(GameUIContainer container, int index)
//        {
//            var rect = container.GetChildRect(index);
//
//            var pos = container.Widget.GetRelativePosWithAnchor(scroll.viewport, rect.position);
//            pos.y = -pos.y;
//
//            var itemRect = new Rect(pos, rect.size);
//            var viewportRect = new Rect(Vector2.zero, scroll.viewport.rect.size);
//
//            return itemRect.Intersect(viewportRect);
//        }
//
//        public void SnapToContainerChildren(GameUIContainer container, float snapTime = 0.1f, Action<IUIBehaviour> onFocus = null, Action<IUIBehaviour> onSnap = null)
//        {
//            shouldSnap = container != null;
//            if (shouldSnap)
//            {
//                snapingContainer = container;
//                container.OnFocus = SetFocusBehaviour;
//                snapingTime = snapTime;
//                onFocusCb = onFocus;
//                onSnapCb = onSnap;
//                AddHandleDragStart();
//                AddHandleDragEnd();
//            }
//            else
//            {
//                if (snapingContainer != null)
//                {
//                    snapingContainer.OnFocus -= SetFocusBehaviour;
//                    onFocusCb = null;
//                    onSnapCb = null;
//                    snapingContainer = null;
//                }
//            }
//        }
//
//        protected override void HandleDragStart(GameObject go, PointerEventData evt)
//        {
//            base.HandleDragStart(go, evt);
//            lockPos = false;
//            scrolling = false;
//            startingFocusingChildIdx = focusingChildIdx;
//            pendingSnapingIdx = -1;
//        }
//
//        protected override void HandleDragEnd(GameObject go, PointerEventData evt)
//        {
//            base.HandleDragEnd(go, evt);
//            if (focusingChildIdx != -1 && shouldSnap && Enable)
//            {
//                int idx = focusingChildIdx;
//                float maxV = Mathf.Max(Mathf.Abs(scroll.velocity.x), Mathf.Abs(scroll.velocity.y));
//                if (maxV > 100 && focusingChildIdx == startingFocusingChildIdx)
//                {
//                    if (Mathf.Abs(scroll.velocity.x) > Mathf.Abs(scroll.velocity.y))
//                    {
//                        if (scroll.velocity.x < 0)
//                        {
//                            idx++;
//                        }
//                        else
//                        {
//                            idx--;
//                        }
//                    }
//                    else
//                    {
//                        if (scroll.velocity.y < 0)
//                        {
//                            idx--;
//                        }
//                        else
//                        {
//                            idx++;
//                        }
//                    }
//                }
//                idx = Math.Min(Math.Max(idx, 0), snapingContainer.Count - 1);
//                ScrollToContainerChild(snapingContainer, idx, true, snapingTime);
//                if (focusingChildIdx == idx)
//                    onSnapCb?.Invoke(focusingBehaviour);
//                else
//                {
//                    pendingSnapingIdx = idx;
//                }
//            }
//        }
//        void SetFocusBehaviour(IUIBehaviour behaviour, int focusingIdx)
//        {
//            if (focusingIdx != focusingChildIdx)
//            {
//                focusingChildIdx = focusingIdx;
//                focusingBehaviour = behaviour;
//                onFocusCb?.Invoke(behaviour);
//                if(pendingSnapingIdx == focusingChildIdx)
//                {
//                    onSnapCb?.Invoke(focusingBehaviour);
//                    pendingSnapingIdx = -1;
//                }
//            }
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            onFocusCb = null;
//            onSnapCb = null;
//            if(snapingContainer !=null)
//            {
//                snapingContainer.OnFocus -= SetFocusBehaviour;
//            }
//        }
//
//        void OnValueChange(Vector2 newPos)
//        {
//            //至少锁定0.3秒以应对显示时状态变更
//            if (Time.realtimeSinceStartup - lockTime > 0.3f)
//                lockPos = false;
//            OnScrollChange?.Invoke(CurrentPosition);
//        }
//
//        public override void OnUpdate()
//        {
//            base.OnUpdate();
//            if (lockPos)
//            {
//                scroll.normalizedPosition = normalizedPos;
//                if (Time.realtimeSinceStartup - lockTime > 0.3f)
//                    lockPos = false;
//            }
//            else if (scrolling)
//            {
//                curNormalizedPos = Vector2.SmoothDamp(curNormalizedPos, normalizedPos, ref scrollSpeed, scrollTime);
//                var diff = curNormalizedPos - normalizedPos;
//                var contentSize = scroll.content.rect.size - scroll.viewport.rect.size;
//                diff = diff * contentSize; 
//                if (Mathf.Abs(diff.x) < 1 && Mathf.Abs(diff.y) < 1)
//                {
//                    curNormalizedPos = normalizedPos;
//                    scrolling = false;
//                    scrollSpeed = Vector2.zero;
//                }
//                scroll.normalizedPosition = curNormalizedPos;
//            }
//        }
//
//    }
//}
