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
//using Burner.Extensions;
//using UnityEngine;
//using UnityEngine.Profiling;
//
//namespace Burner.UIExtension
//{
//    public class GameUIComponent : IUIBehaviour
//    {
//        public delegate void DragToDropStartDelegate(GameUIComponent comp, IUIBehaviour targetComp);
//        public delegate void DragToDropEndDelegate(GameUIComponent comp, IUIBehaviour targetComp, bool success);
//
//        protected GameObject gameObject;
//        RectTransform widget;
//        Transform savedTransform;
//        bool cachedVisible;
//        bool disposed;
//        bool lastLongPressState;
//        protected int longPressFrameCnt;
//        protected bool destroying;
//        static Material grayMaterial;
//
//        protected Action<GameUIComponent> clickAction;
//        protected Action<GameUIComponent> pressDownAction, pressUpAction;
//        protected Action<GameUIComponent, UnityEngine.EventSystems.PointerEventData> dragAction, dragStartAction, dragEndAction;
//        Action<string> animationEvtAction;
//        DragToDropStartDelegate dragDropStart;
//        DragToDropEndDelegate dragDropEnd;
//        Action<object> dragLongPress;
//
//        Action<IUIBehaviour> onDropAction, onDropEnterAction, onDropExitAction;
//        Action<GameUIComponent, bool> longPressAction;
//        UITweener[] tweeners;
//        Animator animator;
//        PendingAnimationInfo pendingPlayAnimation;
//        List<GameUIAttachment> attachments;
//        GameUIAttachment[] copiedAttachments;
//        bool isAttachmentsDirty;
//        
//        struct PendingAnimationInfo
//        {
//            public string ClipName;
//            public int Layer;
//            public float NormailizedTime;
//            public Action OnEnded;
//        }
//        bool longPressAdded;
//        protected bool clickAdded = false;
//        bool pressDownAdded, pressUpAdded;
//        bool dragAdded, dragStartAdded, dragEndAdded;
//        bool dragToDropAdded, dropAdded;
//
//        public GameUILogic UILogic { get; internal set; }
//        /// <summary>
//        /// 用户自定义数据
//        /// </summary>
//        public object UserState { get; set; }
//
//        /// <summary>
//        /// 当前X坐标
//        /// </summary>
//        public float X
//        {
//            get
//            {
//                if (widget)
//                {
//                    return widget.anchoredPosition.x;
//                }
//                else
//                {
//                    if (!savedTransform)
//                        savedTransform = gameObject.transform;
//                    return savedTransform.localPosition.x;
//                }
//            }
//            set
//            {
//                if (widget)
//                {
//                    Vector2 old = widget.anchoredPosition;
//                    Vector2 vec = new Vector3(value, old.y);
//                    widget.anchoredPosition = vec;
//                }
//                else
//                {
//                    if (!savedTransform)
//                        savedTransform = gameObject.transform;
//                    Vector3 old = savedTransform.localPosition;
//                    Vector3 vec = new Vector3(value, old.y, old.z);
//                    savedTransform.localPosition = vec;
//                }
//            }
//        }
//
//        /// <summary>
//        /// 当前Y坐标
//        /// </summary>
//        public float Y
//        {
//            get
//            {
//                if (widget)
//                {
//                    return widget.anchoredPosition.y;
//                }
//                else
//                {
//                    if (!savedTransform)
//                        savedTransform = gameObject.transform;
//                    return savedTransform.localPosition.y;
//                }
//            }
//            set
//            {
//                if (widget)
//                {
//                    Vector2 old = widget.anchoredPosition;
//                    Vector2 vec = new Vector3(old.x, value);
//                    widget.anchoredPosition = vec;
//                }
//                else
//                {
//                    if (!savedTransform)
//                        savedTransform = gameObject.transform;
//                    Vector3 old = savedTransform.localPosition;
//                    Vector3 vec = new Vector3(old.x, value, old.z);
//                    savedTransform.localPosition = vec;
//                }
//            }
//        }
//
//        public float Width
//        {
//            get
//            {
//                return widget.sizeDelta.x;
//            }
//            set
//            {
//                var size = widget.sizeDelta;
//                size.x = value;
//                widget.sizeDelta = size;
//            }
//        }
//
//        public float Height
//        {
//            get
//            {
//                return widget.sizeDelta.y;
//            }
//            set
//            {
//                var size = widget.sizeDelta;
//                size.y = value;
//                widget.sizeDelta = size;
//            }
//        }
//
//        public Vector2 Size
//        {
//            get => widget.sizeDelta;
//            set
//            {
//                widget.sizeDelta = value;
//            }
//        }
//
//        /// <summary>
//        /// 当前控件的UI Transform，需慎用
//        /// </summary>
//        public RectTransform Widget
//        {
//            get
//            {
//                return widget;
//            }
//        }
//
//        /// <summary>
//        /// 当前控件的GameObject 需慎用
//        /// </summary>
//        public GameObject GameObject
//        {
//            get
//            {
//                return gameObject;
//            }
//        }
//
//        /// <summary>
//        /// 当前位置
//        /// </summary>
//        public Vector2 UIPosition
//        {
//            set
//            {
//                if (widget != null)
//                {
//                    widget.anchoredPosition = value;
//                }
//            }
//
//            get
//            {
//                if (widget != null)
//                {
//                    return widget.anchoredPosition;
//                }
//                return Vector2.zero;
//            }
//        }
//
//        public float Rotation
//        {
//            get
//            {
//                return widget.localRotation.eulerAngles.z;
//            }
//            set
//            {
//                var rot = widget.localRotation.eulerAngles;
//                rot.z = value;
//                widget.localRotation = Quaternion.Euler(rot);
//            }
//        }
//
//        /// <summary>
//        /// 当前世界坐标
//        /// </summary>
//        public Vector3 Position
//        {
//            get
//            {
//                if (!savedTransform)
//                    savedTransform = gameObject.transform;
//                return savedTransform.position;
//            }
//
//            set
//            {
//                if (!savedTransform)
//                    savedTransform = gameObject.transform;
//                savedTransform.position = value;
//            }
//
//        }
//
//        internal bool CachedVisible
//        {
//            get
//            {
//                return cachedVisible;
//            }
//        }
//
//        /// <summary>
//        /// 当前是否可见
//        /// </summary>
//        public bool Visible
//        {
//            get
//            {
//                if (!gameObject)
//                    return false;
//                cachedVisible = gameObject.activeSelf;
//                return cachedVisible;
//            }
//            set
//            {
//                if (!gameObject)
//                    return;
//                cachedVisible = value;
//                if (value != gameObject.activeSelf)
//                {
//                    if (!gameObject.activeInHierarchy && !value)
//                    {
//                        SetVisible(value);
//                        return;
//                    }
//                    //加入UITweener前临时手动调用
//                    SetVisible(value);
//                    /*if (tweens == null)
//                        tweens = gameObject.GetComponentsInChildren<GOGUI.UITweener>(true);
//                    if (value)
//                        setShowEffect();
//                    else
//                        setHideEffect();*/
//                }
//                else if (value)
//                {
//                    /*if (tweens == null)
//                        tweens = gameObject.GetComponentsInChildren<GOGUI.UITweener>(true);
//                    setShowEffect();*/
//                }
//            }
//        }
//
//        public virtual bool NeedPreload { get => false; }
//
//        /// <summary>
//        /// 可以无绑定自动创建的组件直接返回该组件实例
//        /// </summary>
//        public virtual IBindlessUIBehaviour BindlessComponent { get => null; }
//
//        /// <summary>
//        /// 是否在界面层级中可见
//        /// </summary>
//        public bool VisibleInHierarchy
//        {
//            get
//            {
//                if (!gameObject)
//                    return false;
//                return gameObject.activeInHierarchy;
//            }
//        }
//
//        public bool Disposed => disposed;
//            
//        public bool IsNull()
//        {
//            return gameObject == null && !System.Object.Equals(gameObject, null);
//        }
//        public virtual bool Enable
//        {
//            get { return true; }
//            set { }
//        }
//        /// <summary>
//        /// 获取图片变灰的材质球
//        /// </summary>
//        protected static Material GrayMaterial
//        {
//            get
//            {
//                if (!grayMaterial)
//                {
//                    Shader grayShader = Shader.Find("UI/Gray");
//                    grayMaterial = new Material(grayShader);
//                }
//                return grayMaterial;
//            }
//        }
//
//        /// <summary>
//        /// 点击事件
//        /// </summary>
//        public Action<GameUIComponent> OnClick
//        {
//            get => clickAction;
//            set
//            {
//                if (!clickAdded)
//                {
//                    clickAdded = true;
//                    AddClickCallBack();
//                }
//                clickAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 按下事件
//        /// </summary>
//        public Action<GameUIComponent> OnPressDown
//        {
//            get => pressDownAction;
//            set
//            {
//                if (!pressDownAdded)
//                {
//                    pressDownAdded = true;
//                    AddPressDownCallBack(HandlePressDown);
//                }
//                pressDownAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 抬起事件
//        /// </summary>
//        public Action<GameUIComponent> OnPressUp
//        {
//            get => pressUpAction;
//            set
//            {
//                if (!pressUpAdded)
//                {
//                    pressUpAdded = true;
//                    AddPressUpCallBack(HandlePressUp);
//                }
//                pressUpAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 长按事件
//        /// </summary>
//        public Action<GameUIComponent, bool> OnLongPress
//        {
//            get => longPressAction;
//            set
//            {
//                if (!longPressAdded)
//                {
//                    longPressAdded = true;
//                    AddLongPressCallBack(HandleLongPress);
//                }
//                longPressAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 拖拽事件
//        /// </summary>
//        public Action<GameUIComponent, UnityEngine.EventSystems.PointerEventData> OnDrag
//        {
//            get => dragAction;
//            set
//            {
//                if (!dragAdded)
//                {
//                    dragAdded = true;
//                    AddDragCallBack(HandleDrag);
//                }
//                dragAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 开始拖拽事件
//        /// </summary>
//        public Action<GameUIComponent, UnityEngine.EventSystems.PointerEventData> OnDragStart
//        {
//            get => dragStartAction;
//            set
//            {
//                AddHandleDragStart();
//                dragStartAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 结束拖拽事件
//        /// </summary>
//        public Action<GameUIComponent, UnityEngine.EventSystems.PointerEventData> OnDragEnd
//        {
//            get => dragEndAction;
//            set
//            {
//                AddHandleDragEnd();
//                dragEndAction = value;
//            }
//        }
//
//        protected void AddHandleDragStart()
//        {
//            if (!dragStartAdded)
//            {
//                dragStartAdded = true;
//                AddDragStartCallBack(HandleDragStart);
//            }
//        }
//
//        protected void AddHandleDragEnd()
//        {
//            if (!dragEndAdded)
//            {
//                dragEndAdded = true;
//                AddDragEndCallBack(HandleDragEnd);
//            }
//        }
//
//        internal void Initialize(GameObject go)
//        {
//            this.gameObject = go;
//            savedTransform = go.transform;
//            widget = savedTransform as RectTransform;
//            OnInit();
//            var receiver = gameObject.GetComponent<AnimationEventReceiver>();
//            if (receiver)
//                receiver.AnimationEventCallback = OnAnimationEvent;
//        }
//        public virtual void OnInit()
//        {
//
//        }
//
//        public virtual void OnDispose()
//        {
//
//        }
//
//        public virtual void OnClose()
//        {
//            pendingAnimationEnd = null;
//            pendingAnimationPlaying = false;
//        }
//
//        internal void DoDispose(bool destroy)
//        {
//            if (!disposed)
//            {
//                disposed = true;
//                if (destroy)
//                    ClearEventCallbacks();
//                RestoreDefaultAnimation(true);
//                ClearAttachments();
//                destroying = destroy;
//                OnDispose();
//                destroying = false;
//                if (destroy)
//                    UILogic = null;
//            }
//        }
//
//        protected virtual void OnResetDispose()
//        {
//
//        }
//
//        internal void ResetDispose(bool callOnResetDispose)
//        {
//            disposed = false;
//            if (callOnResetDispose)
//                OnResetDispose();
//        }
//
//        internal void RestoreDefaultAnimation(bool isDispose)
//        {
//            if (animator)
//            {
//#if UNITY_2022_1_OR_NEWER
//                if (!isDispose && animator.keepAnimatorStateOnDisable)
//#else
//                if (!isDispose && animator.keepAnimatorControllerStateOnDisable)
//#endif
//                    return;
//                StopAnimation();
//                animator.StopPlayback();
//            }
//        }
//
//        void InvokeAnimationEnd()
//        {
//            Profiler.BeginSample("GameUIComponent_InvokeAnimationEnd");
//            pendingAnimationHash = 0;
//            BurnerUIManager.Instance.GlobalEvents.DispatchAnimationEvent(this, GlobalAnimationEvents.Stop, pendingAnimationName);
//            if (pendingAnimationEnd != null)
//            {
//                var cb = pendingAnimationEnd;
//                pendingAnimationEnd = null;
//                cb();
//            }
//            Profiler.EndSample();
//        }
//        bool wasAnimEnable;
//        public virtual void OnUpdate()
//        {
//            Profiler.BeginSample("GameUIComponent_OnUpdateAnimator");
//            if (pendingAnimationHash != 0 && animator)
//            {
//                var state = animator.GetCurrentAnimatorStateInfo(pendingAnimationEndLayer);
//                if (state.shortNameHash == pendingAnimationHash)
//                {
//                    if (pendingAnimationPlaying)
//                    {
//                        if (state.normalizedTime > 0.999f)
//                        {
//                            InvokeAnimationEnd();
//                        }
//                    }
//                    else
//                        pendingAnimationPlaying = true;
//                }
//                else if(pendingAnimationPlaying)
//                {
//                    InvokeAnimationEnd();
//                }
//            }
//            Profiler.EndSample();
//            Profiler.BeginSample("GameUIComponent_OnUpdateAttachments");
//            var arr = GetCopiedAttachments();
//            if(arr != null)
//            {
//                for (int i = 0; i < arr.Length; i++)
//                {
//                    var b = arr[i];
//                    b.OnUpdate();
//                }
//            }
//            Profiler.EndSample();
//        }
//
//        public virtual void OnShow()
//        {
//
//        }
//
//        public virtual void OnHide()
//        {
//
//        }
//
//        public virtual void OnLateUpdate()
//        {
//            var arr = GetCopiedAttachments();
//            if (arr != null)
//            {
//                for (int i = 0; i < arr.Length; i++)
//                {
//                    var b = arr[i];
//                    b.OnLateUpdate();
//                }
//            }
//        }
//
//        public virtual void OnPreload()
//        {
//
//        }
//
//        public virtual void OnBecomeVisible()
//        {
//            if (!string.IsNullOrEmpty(pendingPlayAnimation.ClipName))
//            {
//                PlayAnimation(pendingPlayAnimation.ClipName, pendingPlayAnimation.Layer, pendingPlayAnimation.NormailizedTime, pendingPlayAnimation.OnEnded);
//                pendingPlayAnimation = default;
//            }
//        }
//        void SetVisible(bool value)
//        {
//            if (Visible != value)
//            {
//                if (!value)
//                    RestoreDefaultAnimation(false);
//                gameObject.SetActive(value);
//                if (value)
//                    OnShow();
//                else
//                    OnHide();                
//            }
//        }
//
//        protected T GetComponent<T>()
//            where T : Component
//        {
//            T res = gameObject.GetComponent<T>();
//            if (res == null)
//            {
//                T[] array = gameObject.GetComponentsInChildren<T>(true);
//                if (array.Length > 0)
//                    res = array[0];
//            }
//            return res;
//        }
//        protected T[] GetComponents<T>() where T : Component
//        {
//            T[] array = gameObject.GetComponentsInChildren<T>(true);
//            return array;
//        }
//
//        /// <summary>
//        /// 将当前控件置灰
//        /// </summary>
//        /// <param name="gray"></param>
//        public virtual void SetGray(bool gray)
//        {
//        }
//
//        /// <summary>
//        /// 在当前节点添加挂件
//        /// </summary>
//        /// <param name="prefabName"></param>
//        /// <param name="disableCache"></param>
//        /// <returns></returns>
//        public GameUIAttachment AddAttachment(string prefabName, Action<GameUIAttachment> onLoad = null, bool disableCache = false)
//        {
//            GameUIAttachment attach = new GameUIAttachment(this, prefabName, disableCache, false);
//            if (attachments == null)
//                attachments = new List<GameUIAttachment>();
//            attachments.Add(attach);
//            isAttachmentsDirty = true;
//            attach.OnLoaded = onLoad;
//            attach.UILogic = UILogic;
//            attach.Load();
//            return attach;
//        }
//
//        public GameUIAttachment AddLogicAttachment(string prefabName, Action<GameUILogic> onLoad = null, bool disableCache = false)
//        {
//            GameUIAttachment attach = new GameUIAttachment(this, prefabName, disableCache, true);
//            if (attachments == null)
//                attachments = new List<GameUIAttachment>();
//            attachments.Add(attach);
//            isAttachmentsDirty = true;
//            attach.OnUILogicLoaded = onLoad;
//            attach.UILogic = UILogic;
//            attach.Load();
//            return attach;
//        }
//
//        public void RemoveAttachment(GameUIAttachment attachment)
//        {
//            attachment.DoDispose(true);
//            if (attachments != null)
//            {
//                attachments.Remove(attachment);
//                isAttachmentsDirty = true;
//            }
//        }
//
//        /// <summary>
//        /// 清除所有挂载物
//        /// </summary>
//        public void ClearAttachments()
//        {
//            if (attachments != null)
//            {
//                foreach (var i in attachments)
//                {
//                    i.DoDispose(true);
//                }
//                attachments.Clear();
//                copiedAttachments = null;
//                isAttachmentsDirty = true;
//            }
//        }
//
//        GameUIAttachment[] GetCopiedAttachments()
//        {
//            if (isAttachmentsDirty)
//            {
//                if (attachments != null && attachments.Count > 0)
//                {
//                    copiedAttachments = attachments.ToArray();
//                }
//                else
//                {
//                    copiedAttachments = null;
//                }
//                isAttachmentsDirty = false;
//            }
//            return copiedAttachments;
//        }
//
//        protected virtual void ClearEventCallbacks()
//        {
//            clickAction = null;
//            longPressAction = null;
//            pressDownAction = null;
//            pressUpAction = null;
//            dragAction = null;
//            dragStartAction = null;
//            dragEndAction = null;
//            dragDropStart = null;
//            dragDropEnd = null;
//            dragLongPress = null;
//            onDropAction = null;
//            onDropEnterAction = null;
//            onDropExitAction = null;
//            animationEvtAction = null;
//            pendingAnimationEnd = null;
//            if (tweeners != null)
//            {
//                foreach(var i in tweeners)
//                {
//                    i.OnFinishCallback = null;
//                }
//            }
//            if (gameObject)
//            {
//                if (clickAdded)
//                {
//                    EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onClick = null;
//                }
//                if (pressDownAdded)
//                {
//                    EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onDown = null;
//                }
//                if (pressUpAdded)
//                {
//                    EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onUp = null;
//                }
//                if (longPressAdded)
//                {
//                    EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onLongPressTime = null;
//                }
//                if (dragAdded)
//                {
//                    DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onDrag = null;
//                }
//                if (dragStartAdded)
//                {
//                    DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onDragStart = null;
//                }
//                if (dragEndAdded)
//                {
//                    DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//                    lis.parameter = null;
//                    lis.onDragEnd = null;
//                }
//                if (dragToDropAdded)
//                {
//                    RemoveDragToDropHandler();
//                }
//                if (dropAdded)
//                {
//                    EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//                    lis.onDrop = null;
//                    lis.onDropEnter = null;
//                    lis.onDropExit = null;
//                }
//            }
//        }
//
//        protected virtual void AddClickCallBack()
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.parameter = this;
//            lis.onClick += HandleClick;
//            //clickAction += func;
//        }
//
//        void HandleClick(GameObject go)
//        {
//            if (Enable)
//            {
//                if (longPressFrameCnt == Time.frameCount)
//                    return;
//                BurnerUIManager.Instance.GlobalEvents.DispatchClickEvent(this);
//                clickAction?.Invoke(this);
//            }
//        }
//
//        void HandlePressDown(GameObject go)
//        {
//            pressDownAction?.Invoke(this);
//        }
//
//        void HandlePressUp(GameObject go)
//        {
//            pressUpAction?.Invoke(this);
//        }
//
//        protected void AddPressDownCallBack(EventTriggerListener.VoidDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.parameter = this;
//            lis.onDown += func;
//        }
//
//        protected void AddPressUpCallBack(EventTriggerListener.VoidDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.parameter = this;
//            lis.onUp += func;
//        }
//
//        protected void RemovePressDownCallBack(EventTriggerListener.VoidDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onDown -= func;
//        }
//
//        protected void RemovePressUpCallBack(EventTriggerListener.VoidDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onUp -= func;
//        }
//
//        void HandleDrag(GameObject go, UnityEngine.EventSystems.PointerEventData evt)
//        {
//            dragAction?.Invoke(this, evt);
//        }
//
//        protected virtual void HandleDragStart(GameObject go, UnityEngine.EventSystems.PointerEventData evt)
//        {
//            dragStartAction?.Invoke(this, evt);
//        }
//
//        protected virtual void HandleDragEnd(GameObject go, UnityEngine.EventSystems.PointerEventData evt)
//        {
//            dragEndAction?.Invoke(this, evt);
//        }
//
//        public void SetDragPassThrough(bool value)
//        {
//            if (gameObject)
//            {
//                DragEventTriggerListener lis = gameObject.GetComponent<DragEventTriggerListener>();
//                lis?.SetPassThrough(value);
//            }
//        }
//
//        protected void AddDragCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDrag += func;
//        }
//
//        protected void RemoveDragCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDrag -= func;
//        }
//
//        protected void AddDragStartCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDragStart += func;
//        }
//        protected void RemoveDragStartCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDragStart -= func;
//        }
//
//        protected void AddDragEndCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDragEnd += func;
//        }
//
//        protected void RemoveDragEndCallBack(EventTriggerListener.PointerEventDelegate func)
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            lis.onDragEnd -= func;
//        }
//
//        void HandleLongPress(GameObject go, bool state)
//        {
//            if (lastLongPressState && !state)
//                longPressFrameCnt = Time.frameCount;
//            lastLongPressState = state;
//            longPressAction?.Invoke(this, state);
//        }
//
//        public void SetLongPressTime(float delayTime, float repeatTime)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.SetLongPressTime(delayTime, repeatTime);
//        }
//
//        protected void AddLongPressCallBack(EventTriggerListener.BoolDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onLongPressTime += func;
//        }
//        protected void RemoveLongPressCallBack(EventTriggerListener.BoolDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onLongPressTime -= func;
//            func?.Invoke(gameObject, false);
//        }
//
//        /// <summary>
//        /// 取消长按
//        /// </summary>
//        public void CancelLongPress()
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.OnLongPressTimeFuc(false, true);
//        }
//
//        protected void AddLongPressTimeCallBack(EventTriggerListener.BoolDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onLongPressTime += func;
//        }
//        protected void RemoveLongPressTimeCallBack(EventTriggerListener.BoolDelegate func)
//        {
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onLongPressTime -= func;
//        }
//
//        /// <summary>
//        /// 设置Drag & Drop事件处理器
//        /// </summary>
//        /// <param name="target"></param>
//        /// <param name="dragStart"></param>
//        /// <param name="dragEnd"></param>
//        /// <param name="longpressTimeToDrag"></param>
//        /// <param name="coverParentScrollrect"></param>
//        /// <param name="coverParentDragEvent"></param>
//        public void SetDragToDropHandler(IUIBehaviour target, DragToDropStartDelegate dragStart, DragToDropEndDelegate dragEnd,
//            float longpressTimeToDrag = 1.5f, bool coverParentScrollrect = false, bool coverParentDragEvent = false, 
//            Action<object> dragLongPress = null, float longDragPressTime = 0f)
//        {
//            SetDragToDropHandlerNew(target, dragStart, dragEnd, longpressTimeToDrag, coverParentScrollrect, coverParentDragEvent);
//        }
//
//        public void SetDragToDropHandlerNew(IUIBehaviour target, DragToDropStartDelegate dragStart, DragToDropEndDelegate dragEnd,
//            float longpressTimeToDrag = 1.5f, bool coverParentScrollrect = false, bool coverParentDragEvent = false,
//            Action<object> dragLongPress = null, float longDragPressTime = 0f)
//        {
//            GameUIComponent comp;
//            if (target is GameUILogic logic)
//                comp = logic.UIComponent;
//            else
//                comp = target as GameUIComponent;
//            this.dragDropStart = dragStart;
//            this.dragDropEnd = dragEnd;
//            var evt = EventTriggerListener.Get(comp.gameObject);
//            evt.parameter = target;
//            DragEventTriggerListener lis = DragEventTriggerListener.GetDragToDrop(gameObject, comp.gameObject, coverParentScrollrect, coverParentDragEvent);
//            lis.eventTriggerListener.longTimeToDrag = longpressTimeToDrag;
//            lis.parameter = this;
//            lis.onDragToDropStart -= HandleDragToDropStart;
//            lis.onDragToDropStart += HandleDragToDropStart;
//            lis.onDragToDropEnd -= HandleDragToDropEnd;
//            lis.onDragToDropEnd += HandleDragToDropEnd;
//            lis.onDragToDropLongPress -= HandleDragLongPress;
//            lis.onDragToDropLongPress += HandleDragLongPress;
//            lis.eventTriggerListener.longTimeDragWithPress = longDragPressTime;
//            this.dragLongPress = dragLongPress;
//            dragToDropAdded = true;
//        }
//
//        /// <summary>
//        /// 删除Drag & Drop事件处理器
//        /// </summary>
//        public void RemoveDragToDropHandler()
//        {
//            DragEventTriggerListener lis = DragEventTriggerListener.Get(gameObject);
//            dragDropStart = null;
//            dragDropEnd = null;
//            lis.onDragToDropStart -= HandleDragToDropStart;
//            lis.onDragToDropEnd -= HandleDragToDropEnd;
//            lis.HandleRemoveDragToDrop();
//        }
//
//        void HandleDragToDropStart(object go, GameObject targetGo)
//        {
//            var etl = EventTriggerListener.Get(targetGo);
//            dragDropStart?.Invoke(this, etl.parameter as IUIBehaviour);
//        }
//
//        void HandleDragToDropEnd(object go, GameObject targetGo, bool success)
//        {
//            var etl = EventTriggerListener.Get(targetGo);
//            dragDropEnd?.Invoke(this, etl.parameter as IUIBehaviour, success);
//        }
//
//        void HandleDragLongPress(object go)
//        {
//            dragLongPress?.Invoke(go);
//        }
//
//        /// <summary>
//        /// 设置Drop事件处理器
//        /// </summary>
//        /// <param name="onDrop"></param>
//        /// <param name="onDropEnter"></param>
//        /// <param name="onDropExit"></param>
//        public void SetDropEventHandler(Action<IUIBehaviour> onDrop, Action<IUIBehaviour> onDropEnter = null, Action<IUIBehaviour> onDropExit = null)
//        {
//            this.onDropAction = onDrop;
//            this.onDropEnterAction = onDropEnter;
//            this.onDropExitAction = onDropExit;
//            EventTriggerListener lis = EventTriggerListener.Get(gameObject);
//            lis.onDrop -= HandleDrop;
//            lis.onDrop += HandleDrop;
//            lis.onDropEnter -= HandleDropEnter;
//            lis.onDropEnter += HandleDropEnter;
//            lis.onDropExit -= HandleDropExit;
//            lis.onDropExit += HandleDropExit;
//            dropAdded = true;
//        }
//
//        void HandleDrop(object go)
//        {
//            IUIBehaviour behaviour = go as IUIBehaviour;
//            if (behaviour == null)
//            {
//                EventTriggerListener etl = EventTriggerListener.Get((GameObject)go);
//                behaviour = etl.parameter as IUIBehaviour;
//            }
//            onDropAction?.Invoke(behaviour);
//        }
//
//        void HandleDropEnter(object go)
//        {
//            IUIBehaviour behaviour = go as IUIBehaviour;
//            if (behaviour == null)
//            {
//                EventTriggerListener etl = EventTriggerListener.Get((GameObject)go);
//                behaviour = etl.parameter as IUIBehaviour;
//            }
//            onDropEnterAction?.Invoke(behaviour);
//        }
//
//        void HandleDropExit(object go)
//        {
//            IUIBehaviour behaviour = go as IUIBehaviour;
//            if (behaviour == null)
//            {
//                EventTriggerListener etl = EventTriggerListener.Get((GameObject)go);
//                behaviour = etl.parameter as IUIBehaviour;
//            }
//            onDropExitAction?.Invoke(behaviour);
//        }
//
//        public void PlayTweens(int groupId = 0, bool isReverse = false, Action onEnded = null)
//        {
//            if (tweeners == null)
//            {
//                tweeners = gameObject.GetComponents<UITweener>();
//            }
//            UITweener lastEndTween = null;
//            float lastEndTime = 0;
//            if (tweeners != null && tweeners.Length > 0)
//            {
//                foreach (var i in tweeners)
//                {
//                    if (i.tweenGroup == groupId)
//                    {
//                        float endTime = i.delay + i.duration;
//                        if (endTime > lastEndTime)
//                        {
//                            lastEndTween = i;
//                            lastEndTime = endTime;
//                        }
//                        if (isReverse)
//                        {
//                            i.ResetToEnding();
//                            i.PlayReverse();
//                        }
//                        else
//                        {
//                            i.ResetToBeginning();
//                            i.PlayForward();
//                        }
//                    }
//                }
//                if (onEnded != null && lastEndTween)
//                {
//                    lastEndTween.OnFinishCallback = onEnded;
//                }
//            }
//        }
//
//        public void StopTweens(int groupId = 0)
//        {
//            if (tweeners == null)
//            {
//                tweeners = gameObject.GetComponents<UITweener>();
//            }
//            if (tweeners != null && tweeners.Length > 0)
//            {
//                foreach (var i in tweeners)
//                {
//                    if (i.tweenGroup == groupId)
//                    {
//                        i.Stop();
//                    }
//                }
//            }
//        }
//
//        protected void CheckAndUpdateTweeners(UITweener tween)
//        {
//            bool exists = false;
//            if (tweeners != null)
//            {
//                foreach (var i in tweeners)
//                {
//                    if (i == tween)
//                    {
//                        exists = true;
//                        break;
//                    }
//                }
//            }
//            if (!exists)
//            {
//                if (tweeners == null)
//                    tweeners = new UITweener[] { tween };
//                else
//                {
//                    UITweener[] newArr = new UITweener[tweeners.Length + 1];
//                    tweeners.CopyTo(newArr, 0);
//                    newArr[tweeners.Length] = tween;
//                    tweeners = newArr;
//                }
//            }
//        }
//
//        protected static AnimationCurve GetCurveByType(int curveType)
//        {
//            switch (curveType)
//            {
//                case 0:
//                    return new AnimationCurve(DefaultAnimationCurves.Linear);
//                case 1:
//                    return new AnimationCurve(DefaultAnimationCurves.SlowStartFastProgression);
//                case 2:
//                    return new AnimationCurve(DefaultAnimationCurves.FastStartSlowProgression);
//                case 3:
//                    return new AnimationCurve(DefaultAnimationCurves.SlowStartSlowEnd);
//                default:
//                    throw new ArgumentException("Unknown curve type");
//            }
//        }
//        UITweener.Style GetTweenStyle(int style)
//        {
//            switch(style)
//            {
//                default:
//                    return UITweener.Style.Once;
//                case 1:
//                    return UITweener.Style.Loop;
//                case 2:
//                    return UITweener.Style.PingPong;
//            }
//        }
//        public void TweenPositionThroughTo(float x, float y, float x2, float y2, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tween = TweenPosition.Begin(gameObject, duration, new Vector3(x2, y2, widget.localPosition.z), true, new Vector3(x, y, widget.localPosition.z));
//            tween.OnFinishCallback = onEnd;
//            tween.style = GetTweenStyle(playStyle);
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//        public void TweenPositionTo(float x, float y, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tween = TweenPosition.Begin(gameObject, duration, new Vector3(x, y, widget.localPosition.z));
//            tween.OnFinishCallback = onEnd;
//            tween.style = GetTweenStyle(playStyle);
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//
//        public void TweenPositionToComponentThrough(GameUIComponent comp, float x, float y, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tarPos = comp.widget.GetRelativePos(Widget.parent as RectTransform);
//            TweenPositionThroughTo(x, y, tarPos.x, tarPos.y, duration, curveType, onEnd, playStyle);
//        }
//
//        public void TweenPositionToComponent(GameUIComponent comp, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tarPos = comp.widget.GetRelativePos(Widget.parent as RectTransform);
//            TweenPositionTo(tarPos.x, tarPos.y, duration, curveType, onEnd, playStyle);
//        }
//
//        public void TweenScaleTo(float scale, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tween = TweenScale.Begin(gameObject, duration, Vector3.one * scale);
//            tween.OnFinishCallback = onEnd;
//            tween.style = GetTweenStyle(playStyle);
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//
//        public void TweenScaleTo(Vector3 scale, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tween = TweenScale.Begin(gameObject, duration, scale);
//            tween.OnFinishCallback = onEnd;
//            tween.style = GetTweenStyle(playStyle);
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//
//        public void TweenRotationTo(float rotation, float duration = 1f, int curveType = 0, Action onEnd = null, int playStyle = 0)
//        {
//            var tween = TweenRotation.Begin(gameObject, duration, rotation);
//            tween.OnFinishCallback = onEnd;
//            tween.style = GetTweenStyle(playStyle);
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//
//        int pendingAnimationHash;
//        string pendingAnimationName;
//        int pendingAnimationEndLayer;
//        int defaultAnimationHash;
//        bool pendingAnimationPlaying = false;
//        Action pendingAnimationEnd;
//
//        public bool HasAnimator()
//        {
//            PrepareAnimator();
//            return animator;
//        }
//
//        public void SetAnimatorEnable(bool enable)
//        {
//            if (HasAnimator())
//            {
//                animator.enabled = enable;
//            }
//        }
//
//        protected void OverrideAnimator(Animator animator)
//        {
//            this.animator = animator;
//            defaultAnimationHash = 0;
//            pendingAnimationHash = 0;
//            pendingAnimationName = null;
//            pendingAnimationPlaying = false;
//            pendingAnimationEndLayer = 0;
//        }
//
//        void PrepareAnimator()
//        {
//            if (!animator)
//            {
//                animator = GetComponent<Animator>();
//            }
//        }
//
//        /// <summary>
//        /// 判断指定动画是否正在播放中
//        /// </summary>
//        /// <param name="clipName"></param>
//        /// <returns></returns>
//        public bool IsPlayingAnimation(string clipName, int layer = 0, bool needJudgeNormalizedTime = true)
//        {
//            PrepareAnimator();
//            if (animator)
//            {
//                int hash = Animator.StringToHash(clipName);
//                var state = animator.GetCurrentAnimatorStateInfo(layer);
//                if (needJudgeNormalizedTime)
//                {
//                    return state.shortNameHash == hash && state.normalizedTime < 1;
//                }
//                else
//                {
//                    return state.shortNameHash == hash;
//                }
//            }
//            else
//                return false;
//        }
//
//        /// <summary>
//        /// 控制当前节点动画是否在隐藏时保持状态
//        /// </summary>
//        /// <param name="val"></param>
//        public void SetAnimationKeepStateOnDisable(bool val)
//        {
//            PrepareAnimator();
//
//            if (animator)
//#if UNITY_2022_1_OR_NEWER
//                animator.keepAnimatorStateOnDisable = val;
//#else
//                animator.keepAnimatorControllerStateOnDisable = val;
//#endif
//        }
//
//        public void PlayAnimation(string clipName, int layer = 0, float normailizedTime = 0, Action onEnded = null)
//        {
//            PrepareAnimator();
//            if (!animator)
//                return;
//
//            if (!animator.isActiveAndEnabled)
//            {
//                pendingPlayAnimation = new PendingAnimationInfo()
//                {
//                    ClipName = clipName,
//                    Layer = layer,
//                    NormailizedTime = normailizedTime,
//                    OnEnded = onEnded
//                };
//                return;
//            }
//            if (defaultAnimationHash == 0)
//            {
//                defaultAnimationHash = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
//            }
//            animator.Play(clipName, layer, normailizedTime);
//            animator.Update(0.001f);
//            int hash = Animator.StringToHash(clipName);
//            if (pendingAnimationEnd != null && hash != pendingAnimationHash)
//            {
//                InvokeAnimationEnd();
//            }
//            pendingAnimationName = clipName;
//            pendingAnimationPlaying = false;
//            pendingAnimationHash = hash;
//            pendingAnimationEndLayer = layer;
//            pendingAnimationEnd = onEnded;
//            BurnerUIManager.Instance.GlobalEvents.DispatchAnimationEvent(this, GlobalAnimationEvents.Start, clipName);
//        }
//
//
//        /// <summary>
//        /// 设置动画事件处理器
//        /// </summary>
//        /// <param name="cb"></param>
//        public void SetAnimationEventHandler(Action<string> cb)
//        {
//            animationEvtAction = cb;
//        }
//
//        protected void OnAnimationEvent(string evt)
//        {
//            animationEvtAction?.Invoke(evt);
//            BurnerUIManager.Instance.GlobalEvents.DispatchAnimationEvent(this, GlobalAnimationEvents.Event, evt);
//        }
//        public void StopAnimation()
//        {
//            PrepareAnimator();
//
//            pendingAnimationEnd = null;
//            if (animator && defaultAnimationHash != 0 && animator.isActiveAndEnabled)
//            {
//                animator.Play(defaultAnimationHash, 0, 0.99f);
//                animator.Update(0.033f);
//            }
//        }
//
//        public bool HasAnimation(string clipName, int layer = 0)
//        {
//            PrepareAnimator();
//            if (!animator)
//                return false;
//            int hash = Animator.StringToHash(clipName);
//            return animator.HasState(layer, hash);
//        }
//    }
//}
