//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.EventSystems;
//using System;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class DragEventTriggerListener : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
//    {
//        private void OnDisable()
//        {
//            if (isDragToDrop)
//            {
//                if(PointDragObject != null)
//                {
//                    PointDragObject = null;
//                }
//                if (isDragingToDrop)
//                {
//                    isDragingToDrop = false;
//                }
//                if (eventTriggerListener.isClicking)
//                {
//                    eventTriggerListener.HandleClickIngOut();
//                }
//            }
//        }
//
//        /// <summary>
//        /// 如果父物体有scrollect的话需要先赋值parentScrollRect
//        /// </summary>
//        ScrollRect parentScrollRect;
//        public ScrollRect ParentScrollrect
//        {
//            get
//            {
//                if (!go)
//                    go = gameObject;
//                if (parentScrollRect == null)
//                    parentScrollRect = go.GetComponentInParent<ScrollRect>();
//                return parentScrollRect;
//            }
//        }
//
//        DragEventTriggerListener parentDragEventListener;
//        public DragEventTriggerListener ParentDragEventListener
//        {
//            get
//            {
//                if (parentDragEventListener == null)
//                    parentDragEventListener = go.transform.parent.GetComponentInParent<DragEventTriggerListener>();
//                return parentDragEventListener;
//            }
//        }
//        /// <summary>
//        /// 拖拽时候赋值上该变量
//        /// </summary>
//        public static object PointDragObject;
//
//        /// <summary>
//        /// 需要移动的物体
//        /// </summary>
//        GameObject targetMoveObj;
//        RectTransform targetWidget;
//        RectTransform targetParentWidget;
//        /// <summary>
//        /// 当前位置
//        /// </summary>
//        Vector2 currenPos;
//        /// <summary>
//        /// 正在拖拽中
//        /// </summary>
//        [NonSerialized]
//        public bool isDragingToDrop = false;
//        /// <summary>
//        /// 拖住过程中是否移动了
//        /// </summary>
//        [NonSerialized]
//        public bool isDragingMove = false;
//        /// <summary>
//        /// 有没有拖拽事件
//        /// </summary>
//        [NonSerialized]
//        public bool isDragToDrop = false;
//        /// <summary>
//        /// 有没有覆盖掉父类的scrollrect
//        /// </summary>
//        [SerializeField]
//        bool isHaveCoverParentScrollRect = false;
//        /// <summary>
//        /// 有没有覆盖掉父类的dragevent
//        /// </summary>
//        [SerializeField]
//        bool isHaveCoverDragEventListener = false;
//        public EventTriggerListener eventTriggerListener
//        {
//            get;
//            set;
//        }
//
//        GameObject go;
//        public object parameter;
//        public EventTriggerListener.PointerEventDelegate onDrag;
//        public EventTriggerListener.PointerEventDelegate onDragStart;
//        public EventTriggerListener.PointerEventDelegate onDragEnd;
//        public EventTriggerListener.ObjectGameObjectDelegate onDragToDropStart;
//        public EventTriggerListener.ObjectGameObjectBoolDelegate onDragToDropEnd;
//        public EventTriggerListener.ObjectVoidDelegate onDragToDropLongPress;
//        public void SetPassThrough(bool val)
//        {
//            isHaveCoverDragEventListener = val;
//            isHaveCoverParentScrollRect = val;
//        }
//
//        static public DragEventTriggerListener Get(GameObject go)
//        {
//            DragEventTriggerListener listener = go.GetComponent<DragEventTriggerListener>();
//            if (listener == null) listener = go.AddComponent<DragEventTriggerListener>();
//            listener.go = go;
//            return listener;
//        }
//
//        static public DragEventTriggerListener Get(Transform transform)
//        {
//            DragEventTriggerListener listener = transform.GetComponent<DragEventTriggerListener>();
//            if (listener == null) listener = transform.gameObject.AddComponent<DragEventTriggerListener>();
//            return listener;
//        }
//
//        static public DragEventTriggerListener GetDragToDrop(GameObject go, GameObject targetMoveObj, bool haveCoverParentScrollRect, bool haveCoverDragEventListener)
//        {
//            DragEventTriggerListener listener = go.GetComponent<DragEventTriggerListener>();
//            if (listener == null) listener = go.AddComponent<DragEventTriggerListener>();
//            listener.go = go;
//            listener.targetMoveObj = targetMoveObj;
//            listener.targetWidget = targetMoveObj.GetComponent<RectTransform>();
//            listener.targetParentWidget = listener.targetWidget.parent.gameObject.GetComponent<RectTransform>();
//            listener.isDragToDrop = true;
//            listener.isDragingToDrop = false;
//            listener.isHaveCoverParentScrollRect = haveCoverParentScrollRect;
//            listener.isHaveCoverDragEventListener = haveCoverDragEventListener;
//            EventTriggerListener eventListener = EventTriggerListener.Get(go);
//            listener.eventTriggerListener = eventListener;
//            eventListener.dragEventListener = listener;
//            return listener;
//        }
//
//        public void HandleRemoveDragToDrop()
//        {
//            if(isDragToDrop && onDragToDropStart == null && onDragToDropEnd == null)
//            {
//                isDragToDrop = false;
//                isDragingToDrop = false;
//                isHaveCoverParentScrollRect = false;
//                isHaveCoverDragEventListener = false;
//                if(PointDragObject != null)
//                {
//                    PointDragObject = null;
//                }
//                if (eventTriggerListener.isClicking)
//                {
//                    eventTriggerListener.HandleClickIngOut();
//                }
//                eventTriggerListener.dragEventListener = null;
//            }
//        }
//
//        public void OnBeginDrag(PointerEventData eventData)
//        {
//            if (isDragingToDrop)
//            {
//                SetNowPos(eventData);
//            }
//            else
//            {
//                if (isDragToDrop)
//                {
//                    if (eventTriggerListener.isClicking && (isHaveCoverParentScrollRect || isHaveCoverDragEventListener))
//                    {
//                        eventTriggerListener.HandleClickIngOut();
//                    }
//                }
//                if (isHaveCoverParentScrollRect)
//                {
//                    ParentScrollrect.OnBeginDrag(eventData);
//                }
//                if (isHaveCoverDragEventListener && ParentDragEventListener?.onDragStart != null)
//                {
//                    parentDragEventListener.onDragStart(go, eventData);
//                }
//                if (onDragStart != null)
//                    onDragStart(go, eventData);
//            }
//        }
//
//        public void OnDrag(PointerEventData eventData)
//        {
//            isDragingMove = true;
//            if (isDragingToDrop)
//            {
//                SetNowPos(eventData);
//            }
//            else
//            {
//                if (isDragToDrop)
//                {
//                    if (isHaveCoverParentScrollRect)
//                    {
//                        ParentScrollrect.OnDrag(eventData);
//                    }
//                    else
//                    {
//                        eventTriggerListener.stratDragToDrop();
//                    }
//                }
//                else if (isHaveCoverParentScrollRect)
//                {
//                    ParentScrollrect.OnDrag(eventData);
//                }
//                if (isHaveCoverDragEventListener && ParentDragEventListener?.onDrag != null)
//                {
//                    parentDragEventListener.onDrag(go, eventData);
//                }
//                if (onDrag != null)
//                    onDrag(go, eventData);
//            }
//        }
//
//
//
//        public void OnEndDrag(PointerEventData eventData)
//        {
//            isDragingMove = false;
//            if (isDragingToDrop)
//            {
//                EndDrop(eventData);
//            }
//            else
//            {
//                if (isHaveCoverParentScrollRect)
//                {
//                    ParentScrollrect.OnEndDrag(eventData);
//                }
//                if (isHaveCoverDragEventListener && ParentDragEventListener?.onDragEnd != null)
//                {
//                    parentDragEventListener.onDragEnd(go, eventData);
//                }
//                if (onDragEnd != null)
//                    onDragEnd(go, eventData);
//            }
//        }
//
//        public void StratDrag(PointerEventData eventData)
//        {
//            isDragingToDrop = true;
//            isDragingMove = false;
//            SetAsLastSibling();
//            onDragToDropStart(parameter, targetMoveObj);
//            SetNowPos(eventData);
//            PointDragObject = parameter;
//        }
//
//        public void StartDragLongPress(PointerEventData eventData)
//        {
//            if(!isDragingMove && onDragToDropLongPress != null)
//            {
//                onDragToDropLongPress(targetMoveObj);
//                EndDrop(eventData);
//            }
//        }
//
//        public void EndDrop(PointerEventData eventData)
//        {
//            bool onDropSuccess = false;
//            GameObject obj = eventData.pointerCurrentRaycast.gameObject;
//            if (obj != null && obj != go)
//            {
//                EventTriggerListener ifhaveListener;
//                do
//                {
//                    ifhaveListener = obj.GetComponent<EventTriggerListener>();
//                    if (!ifhaveListener)
//                    {
//                        var parent = obj.transform.parent;
//                        if (parent)
//                            obj = parent.gameObject;
//                        else
//                            break;
//                    }
//                }
//                while (obj && !ifhaveListener);
//                if (ifhaveListener != null && ifhaveListener.onDrop != null)
//                {
//                    ifhaveListener.onDrop(parameter);
//                    onDropSuccess = true;
//                }
//            }
//            if (onDragToDropEnd != null)
//            {
//                onDragToDropEnd(parameter, targetMoveObj, onDropSuccess);
//            }
//            PointDragObject = null;
//            isDragingToDrop = false;
//            isDragingMove = false;
//        } 
//
//        void SetNowPos(PointerEventData eventData)
//        {
//            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParentWidget, eventData.position, eventData.pressEventCamera, out currenPos))
//            {
//                targetWidget.anchoredPosition = currenPos;
//            }
//        }
//
//        /// <summary>
//        /// 保证当前操作的对象能够优先渲染，即不会被其它对象遮挡住 
//        /// </summary>
//        void SetAsLastSibling()
//        {
//            targetWidget.SetAsLastSibling();
//        }
//    }   
//}
