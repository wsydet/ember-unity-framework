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
//    public class EventTriggerListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
//    {
//        protected GameObject go;
//        public delegate void VoidDelegate(GameObject go);
//        public delegate void BoolDelegate(GameObject go, bool state);
//        public delegate void FloatDelegate(GameObject go, float delta);
//        public delegate void PointerEventDelegate(GameObject go, PointerEventData data);
//        public delegate void ObjectDelegate(GameObject go, GameObject obj);
//        public delegate void KeyCodeDelegate(GameObject go, KeyCode key);
//        public delegate void ObjectGameObjectDelegate(object obj, GameObject targetObject);
//        public delegate void ObjectGameObjectBoolDelegate(object obj, GameObject targetObject, bool success);
//        public delegate void ObjectVoidDelegate(object obj);
//
//        public VoidDelegate onClick;
//        public VoidDelegate onDown;
//        public VoidDelegate onEnter;
//        public VoidDelegate onExit;
//        public VoidDelegate onUp;
//        public VoidDelegate onSelect;
//        public VoidDelegate onUpdateSelect;
//        public BoolDelegate onLongPressTime;
//        public object parameter;
//        public PointerEventData PointerEventData;
//        public static VoidDelegate GlobalClickCallback { get; set; }
//        private bool LongPressEnable = false;
//        float longPressStartTime;
//        float lastLongPressTriggerTime;
//        float longPressDelayTime = 1.0f;
//        float longPressRepeatTime = 0.3f;
//        bool longPressSpeedUp = true;//加速
//
//        /// <summary>多长时间开始拖拽</summary>
//        [HideInInspector]
//        public float longTimeToDrag = 1.5f;
//        [HideInInspector]
//        /// <summary>长按但是不拖拽 响应特殊的长按事件</summary>
//        public float longTimeDragWithPress = 0f;
//        [HideInInspector]
//        public bool isClicking = false;
//
//        public ObjectVoidDelegate onDropEnter;
//        public ObjectVoidDelegate onDropExit;
//        public ObjectVoidDelegate onDrop;
//
//        float startDragTime;
//        bool dragDropStarted;
//        bool dragLongPressStart;
//
//        public DragEventTriggerListener dragEventListener
//        {
//            get;
//            set;
//        }
//
//        static public EventTriggerListener Get(GameObject go)
//        {
//            EventTriggerListener listener = go.GetComponent<EventTriggerListener>();
//            if (listener == null) listener = go.AddComponent<EventTriggerListener>();
//            listener.go = go;
//            return listener;
//        }
//
//        static public EventTriggerListener Get(Transform transform)
//        {
//            EventTriggerListener listener = transform.GetComponent<EventTriggerListener>();
//            if (listener == null) listener = transform.gameObject.AddComponent<EventTriggerListener>();
//            return listener;
//        }
//
//        internal void stratDragToDrop()
//        {
//            if (isClicking)
//            {
//                dragEventListener.StratDrag(PointerEventData);
//                isClicking = false;
//            }
//            dragDropStarted = false;
//        }
//
//        internal void StartDragPress()
//        {
//            if(dragEventListener != null)
//                dragEventListener.StartDragLongPress(PointerEventData);
//            dragLongPressStart = false;
//        }
//
//        public virtual void OnPointerClick(PointerEventData eventData)
//        {
//            PointerEventData = eventData;
//            if (GlobalClickCallback != null) GlobalClickCallback(go);
//            /*if (dragEventListener != null && dragEventListener.isDragingToDrop)
//            {
//                dragEventListener.EndDrop(eventData);
//            }
//            else*/
//            {
//                //if ((eventData.pressPosition - eventData.position).magnitude < EventSystem.current.pixelDragThreshold)
//                {
//                    if (onClick != null) onClick(go);
//                }
//                LongPressEnable = false;
//            }
//        }
//        public virtual void OnPointerDown(PointerEventData eventData)
//        {
//            PointerEventData = eventData;
//            if (dragEventListener != null && !isClicking)
//            {
//                isClicking = true;
//                dragDropStarted = true;
//                startDragTime = Time.realtimeSinceStartup;
//                if (longTimeDragWithPress > 0 && !dragLongPressStart)
//                    dragLongPressStart = true;
//            }
//            else
//            {
//                if (onDown != null) onDown(go);
//                LongPressEnable = true;
//                if (onLongPressTime != null)
//                {
//                    OnLongPressTimeFuc(true);
//                }
//            }
//        }
//        private void OnDisable()
//        {
//            LongPressEnable = false;
//        }
//
//        public virtual void OnPointerEnter(PointerEventData eventData)
//        {
//            PointerEventData = eventData;
//            if (onDropEnter != null && DragEventTriggerListener.PointDragObject != null)
//            {
//                onDropEnter(DragEventTriggerListener.PointDragObject);
//            }
//            else
//            {
//                if (onEnter != null) onEnter(go);
//            }
//        }
//        public virtual void OnPointerExit(PointerEventData eventData)
//        {
//            PointerEventData = eventData;
//            if (onDropExit != null && DragEventTriggerListener.PointDragObject != null)
//            {
//                onDropExit(DragEventTriggerListener.PointDragObject);
//            }
//            else
//            {
//                if (onExit != null) onExit(go);
//                if (onLongPressTime != null)
//                {
//                    OnLongPressTimeFuc(false, false);
//                }
//            }
//            dragLongPressStart = false;
//        }
//
//        public void HandleClickIngOut()
//        {
//            isClicking = false;
//            dragDropStarted = false;
//        }
//        public virtual void OnPointerUp(PointerEventData eventData)
//        {
//            PointerEventData = eventData;
//            if (dragEventListener != null)
//            {
//                if (isClicking)
//                    HandleClickIngOut();
//                if (dragEventListener.isDragingToDrop)
//                {
//                    dragEventListener.EndDrop(eventData);
//                }
//            }
//            else
//            {
//                if (onUp != null) onUp(go);
//                LongPressEnable = false;
//                if (onLongPressTime != null)
//                {
//                    OnLongPressTimeFuc(false);
//                }
//            }
//            dragLongPressStart = false;
//        }
//        public void OnSelect(BaseEventData eventData)
//        {
//            PointerEventData = null;
//            if (onSelect != null) onSelect(go);
//        }
//        public void OnUpdateSelected(BaseEventData eventData)
//        {
//            PointerEventData = null;
//            if (onUpdateSelect != null) onUpdateSelect(go);
//        }
//
//        private bool CheckLongPressButtonEnable()
//        {
//            var button = go.GetComponent<UnityEngine.UI.Button>();
//            return !(button && !button.interactable);
//        }
//        public void SetLongPressTime(float delayTime, float repeatTime)
//        {
//            longPressDelayTime = delayTime;
//            longPressRepeatTime = repeatTime;
//        }
//        public void OnLongPressTimeFuc(bool _start, bool _cancel = true)
//        {
//            if (_start && CheckLongPressButtonEnable())
//            {
//                longPressStartTime = Time.realtimeSinceStartup;
//            }
//            else
//            {
//                if (_cancel)
//                {
//                    if (onLongPressTime != null)
//                        onLongPressTime(go, false);
//                }
//            }
//        }
//
//        void Update()
//        {
//            if (dragDropStarted)
//            {
//                var now = Time.realtimeSinceStartup;
//                var diffTime = now - startDragTime;
//                if (diffTime >= longTimeToDrag)
//                {
//                    stratDragToDrop();
//                }
//                
//            }
//            if(dragLongPressStart)
//            {
//                var now = Time.realtimeSinceStartup;
//                var diffTime = now - startDragTime;
//                if (diffTime >= longTimeDragWithPress)
//                {
//                    StartDragPress();
//                }
//            }
//            if (LongPressEnable)
//            {
//                var now = Time.realtimeSinceStartup;
//                if (now - longPressStartTime > longPressDelayTime)
//                {
//                    if (longPressRepeatTime > 0.00001)
//                    {
//                        if (now - lastLongPressTriggerTime > longPressRepeatTime)
//                        {
//                            if (onLongPressTime != null)
//                                onLongPressTime(go, true);
//                            lastLongPressTriggerTime = now;
//                        }
//                    }
//                    else
//                    {
//                        if (lastLongPressTriggerTime < longPressStartTime)
//                        {
//                            if (onLongPressTime != null)
//                                onLongPressTime(go, true);
//                            lastLongPressTriggerTime = now;
//                        }
//                    }
//                }
//            }
//        }
//    }
//}
