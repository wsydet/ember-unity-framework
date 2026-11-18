using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>原生 ScrollRect 的定位/吸附伴随组件。content/viewport 同向；不拥有布局。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ScrollRect)), DefaultExecutionOrder(1000)]
    [AddComponentMenu("UI/EUI/Scroll Controller")]
    [EUIExtension(typeof(EUIScrollController), AutoDetect=false)]
    public sealed class EUIScrollController : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        #region 编辑器面板参数
        [SerializeField] private RectTransform _snapContainer;
        [SerializeField] private EUIScrollAxis _snapAxis=EUIScrollAxis.Vertical;
        [SerializeField, Min(0)] private float _snapDuration=0.15f;
        [SerializeField, Min(0)] private float _settleDelay=0.12f;
        [SerializeField, Min(0)] private float _velocityThreshold=20f;
        [SerializeField] private bool _useUnscaledTime=true;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        private ScrollRect _scroll;
        private readonly Vector3[] _corners=new Vector3[4];
        private readonly List<RectTransform> _targets=new();
        private bool _explicitTargets, _dragging, _manual, _writing, _expectedValid;
        private Vector2 _expected;
        private float _lastMotion;
        private long _nextId;
        private Operation _operation;
        private RectTransform _focusedTarget;
        private sealed class Operation
        {
            internal long Id;
            internal RectTransform Target;
            internal bool HasTarget, Snap;
            internal int SnapSignature;
            internal EUIScrollAlignment Alignment;
            internal Vector2 Offset, Destination, Start;
            internal float Duration, Elapsed;
            internal Action<EUIScrollResult> Callback;
        }
        public ScrollRect Scroll => _scroll ? _scroll : _scroll=GetComponent<ScrollRect>();
        public bool IsScrolling => _operation!=null;
        public RectTransform FocusedTarget => _focusedTarget;
        public float SnapDuration { get=>_snapDuration; set=>_snapDuration=NonNegative(value); }
        public float SettleDelay { get=>_settleDelay; set=>_settleDelay=NonNegative(value); }
        public float VelocityThreshold { get=>_velocityThreshold; set=>_velocityThreshold=NonNegative(value); }
        public bool UseUnscaledTime { get=>_useUnscaledTime; set=>_useUnscaledTime=value; }
        public event Action<RectTransform> FocusChanged;
        public event Action<EUIScrollResult> SnapCompleted;
        private RectTransform View => Scroll.viewport ? Scroll.viewport : (RectTransform)transform;
        private float Now => _useUnscaledTime ? Time.unscaledTime : Time.time;
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            Scroll.onValueChanged.AddListener(OnValueChanged);
            CaptureExpected();
        }
        private void OnDisable()
        {
            if (_scroll) _scroll.onValueChanged.RemoveListener(OnValueChanged);
            _manual=false; _dragging=false;
            Finish(_operation, EUIScrollStatus.Disabled);
        }
        private void OnDestroy() { Finish(_operation, EUIScrollStatus.Disabled); }
        private void LateUpdate()
        {
            if(!ReferenceEquals(_focusedTarget,null) && !_focusedTarget)SetFocus(null);
            if (!Scroll.enabled || !Scroll.content) { _manual=false; Finish(_operation,EUIScrollStatus.Disabled); return; }
            DetectExternalMotion();
            var op=_operation;
            if(op!=null)
            {
                if(op.Snap && op.SnapSignature!=SnapSignature()) {Finish(op,EUIScrollStatus.Stopped);return;}
                if(op.HasTarget && !ValidTarget(op.Target)) { Finish(op,EUIScrollStatus.TargetLost); return; }
                op.Elapsed+=_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var dest=op.HasTarget ? TargetPosition(op.Target,op.Alignment,op.Offset) : Sanitize(op.Destination);
                float t=op.Duration<=0 ? 1 : Mathf.Clamp01(op.Elapsed/op.Duration);
                Write(Vector2.LerpUnclamped(op.Start,dest,t*t*(3-2*t)));
                if(t>=1) Finish(op,EUIScrollStatus.Completed);
            }
            else if(_manual && !_dragging && Now-_lastMotion>=NonNegative(_settleDelay))
            {
                int axis=_snapAxis==EUIScrollAxis.Horizontal ? 0 : 1;
                if(Mathf.Abs(Scroll.velocity[axis])<=NonNegative(_velocityThreshold) && AtReachableBounds())
                { _manual=false; SnapToNearest(); }
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private float NonNegative(float value) => float.IsFinite(value) ? Mathf.Max(0,value) : 0;
        private Vector2 Sanitize(Vector2 value)
        {
            if(!float.IsFinite(value.x))value.x=0;
            if(!float.IsFinite(value.y))value.y=1;
            value.x=Mathf.Clamp01(value.x); value.y=Mathf.Clamp01(value.y);
            return value;
        }
        private Bounds Project(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            var p=View.InverseTransformPoint(_corners[0]); var bounds=new Bounds(p,Vector3.zero);
            for(int i=1;i<4;i++)bounds.Encapsulate(View.InverseTransformPoint(_corners[i]));
            return bounds;
        }
        private bool ValidTarget(RectTransform target) => target && Scroll.content && target.IsChildOf(Scroll.content);
        private Vector2 Position()
        {
            var p=Sanitize(Scroll.normalizedPosition);
            if(!Scroll.content)return new Vector2(0,1);
            var c=Project(Scroll.content);var v=View.rect;
            if(c.size.x<=v.width+0.001f)p.x=0;
            if(c.size.y<=v.height+0.001f)p.y=1;
            return p;
        }
        private Vector2 TargetPosition(RectTransform target,EUIScrollAlignment alignment,Vector2 offset)
        {
            var c=Project(Scroll.content);var t=Project(target);var v=View.rect;var p=Position();
            Vector2 point=alignment==EUIScrollAlignment.Begin ? new Vector2(t.min.x,t.max.y) : alignment==EUIScrollAlignment.End ? new Vector2(t.max.x,t.min.y) : (Vector2)t.center;
            Vector2 anchor=alignment==EUIScrollAlignment.Begin ? new Vector2(v.xMin,v.yMax) : alignment==EUIScrollAlignment.End ? new Vector2(v.xMax,v.yMin) : v.center;
            var delta=anchor+offset-point;
            if(c.size.x>v.width+0.001f)p.x-=delta.x/(c.size.x-v.width);
            if(c.size.y>v.height+0.001f)p.y-=delta.y/(c.size.y-v.height);
            return Sanitize(p);
        }
        private void CaptureExpected()
        {
            _expectedValid=Scroll.content;
            if(_expectedValid)_expected=Scroll.content.anchoredPosition;
        }
        private void Write(Vector2 position)
        {
            if(!Scroll.content)return;
            position=Sanitize(position);
            _writing=true;
            try
            {
                Scroll.StopMovement();
                if(Scroll.horizontal)Scroll.horizontalNormalizedPosition=position.x;
                if(Scroll.vertical)Scroll.verticalNormalizedPosition=position.y;
                CaptureExpected();
            }
            finally { _writing=false; }
        }
        private void DetectExternalMotion()
        {
            if(_writing || !Scroll.content)return;
            if(_expectedValid && (Scroll.content.anchoredPosition-_expected).sqrMagnitude>0.0001f)
            {
                _manual=true; _lastMotion=Now;
                Finish(_operation,EUIScrollStatus.UserInterrupted);
            }
            CaptureExpected();
        }
        private void OnValueChanged(Vector2 _) { DetectExternalMotion(); }
        private bool AtReachableBounds()
        {
            var c=Project(Scroll.content);var v=View.rect;int a=_snapAxis==EUIScrollAxis.Horizontal?0:1;
            if(c.size[a]<=v.size[a])return true;
            return c.min[a]<=v.min[a]+0.5f && c.max[a]>=v.max[a]-0.5f;
        }
        private void Notify<T>(Action<T> callback,T value)
        {
            if(callback==null)return;
            foreach(Action<T> listener in callback.GetInvocationList())
            {try{listener(value);}catch(Exception e){EmberDebug.LogWarning(LogTags.UIManager,$"Scroll callback: {e}");}}
        }
        private void Finish(Operation op,EUIScrollStatus status)
        {
            if(op==null || _operation!=op)return;
            _operation=null;
            var result=new EUIScrollResult(op.Id,op.Target,Position(),status);
            Notify(op.Callback,result);
            if(op.Snap && status==EUIScrollStatus.Completed && _nextId==op.Id)Notify(SnapCompleted,result);
        }
        private long Begin(Vector2 destination,RectTransform target,bool hasTarget,EUIScrollAlignment alignment,Vector2 offset,float duration,Action<EUIScrollResult> callback,bool snap=false)
        {
            var old=_operation;
            var op=new Operation {Id=++_nextId,Target=target,HasTarget=hasTarget,Alignment=alignment,Offset=offset,Duration=NonNegative(duration),Destination=destination,Callback=callback,Snap=snap,SnapSignature=snap?SnapSignature():0};
            _operation=op; _manual=false;
            if(old!=null)Notify(old.Callback,new EUIScrollResult(old.Id,old.Target,Position(),EUIScrollStatus.Replaced));
            if(_operation!=op)return op.Id;
            if(!isActiveAndEnabled || !Scroll.isActiveAndEnabled) {Finish(op,EUIScrollStatus.Disabled);return op.Id;}
            if(!Scroll.content || (hasTarget && !ValidTarget(target)) || !float.IsFinite(offset.x) || !float.IsFinite(offset.y)) {Finish(op,EUIScrollStatus.Invalid);return op.Id;}
            RefreshLayout(); Scroll.StopMovement(); op.Start=Position(); CaptureExpected();
            if(op.Duration<=0) {Write(hasTarget?TargetPosition(target,alignment,offset):destination);Finish(op,EUIScrollStatus.Completed);}
            return op.Id;
        }
        private void SetFocus(RectTransform target)
        {if(ReferenceEquals(_focusedTarget,target))return;_focusedTarget=target;Notify(FocusChanged,target);}
        private int SnapSignature()
        {
            unchecked
            {
                int hash=17;
                if(!_explicitTargets && _snapContainer)
                {for(int i=0;i<_snapContainer.childCount;i++){var child=_snapContainer.GetChild(i);hash=hash*31+child.GetHashCode();hash=hash*31+(child.gameObject.activeInHierarchy?1:0);}}
                else foreach(var target in _targets){hash=hash*31+(target?target.GetHashCode():0);hash=hash*31+(ValidTarget(target)&&target.gameObject.activeInHierarchy?1:0);}
                return hash;
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public void RefreshLayout() { Canvas.ForceUpdateCanvases(); if(Scroll.content)LayoutRebuilder.ForceRebuildLayoutImmediate(Scroll.content); }
        /// <summary>Unity 原生约定：x=0 左/1 右；y=1 顶/0 底。仅写开启的轴。</summary>
        public long ScrollToNormalized(Vector2 position,float duration=0,Action<EUIScrollResult> completed=null) => Begin(Sanitize(position),null,false,default,default,duration,completed);
        public long ScrollToBegin(float duration=0,Action<EUIScrollResult> completed=null) => ScrollToNormalized(new Vector2(0,1),duration,completed);
        public long ScrollToEnd(float duration=0,Action<EUIScrollResult> completed=null) => ScrollToNormalized(new Vector2(1,0),duration,completed);
        /// <summary>UI 本地单位，正 x 向内容右端浏览，正 y 向底端浏览。</summary>
        public long ScrollByPixels(Vector2 offset,float duration=0,Action<EUIScrollResult> completed=null)
        {
            RefreshLayout();var p=Position();
            if(Scroll.content){var c=Project(Scroll.content);var v=View.rect;if(c.size.x>v.width+0.001f)p.x+=offset.x/(c.size.x-v.width);if(c.size.y>v.height+0.001f)p.y-=offset.y/(c.size.y-v.height);}
            return ScrollToNormalized(p,duration,completed);
        }
        /// <summary>offset 是 viewport 本地坐标，+x 向右、+y 向上；可达边界 clamp 优先。</summary>
        public long ScrollTo(RectTransform target,EUIScrollAlignment alignment=EUIScrollAlignment.Center,Vector2 offset=default,float duration=0,Action<EUIScrollResult> completed=null) => Begin(default,target,true,alignment,offset,duration,completed);
        public long ScrollToItem(EUIItem item,EUIScrollAlignment alignment=EUIScrollAlignment.Center,Vector2 offset=default,float duration=0,Action<EUIScrollResult> completed=null) => ScrollTo(item?.RectTransform,alignment,offset,duration,completed);
        public bool IsVisible(RectTransform target,bool fullyVisible=false)
        {
            if(!ValidTarget(target))return false;
            var t=Project(target);var v=View.rect;if(t.size.x<=0 || t.size.y<=0)return false;
            return fullyVisible ? t.min.x>=v.xMin && t.max.x<=v.xMax && t.min.y>=v.yMin && t.max.y<=v.yMax : t.max.x>v.xMin && t.min.x<v.xMax && t.max.y>v.yMin && t.min.y<v.yMax;
        }
        public void Stop() { _manual=false;Scroll.StopMovement();Finish(_operation,EUIScrollStatus.Stopped); }
        public void SetSnapTargets(IReadOnlyList<RectTransform> targets,EUIScrollAxis axis)
        {
            Stop();_snapContainer=null;_explicitTargets=true;_snapAxis=axis;_targets.Clear();
            if(targets!=null)for(int i=0;i<targets.Count;i++)_targets.Add(targets[i]);SetFocus(null);
        }
        public void SetSnapContainer(RectTransform container,EUIScrollAxis axis) {Stop();_explicitTargets=false;_snapContainer=container;_snapAxis=axis;_targets.Clear();SetFocus(null);}
        public void ClearSnapTargets() {SetSnapContainer(null,_snapAxis);}
        public bool SnapToNearest()
        {
            if(!isActiveAndEnabled || !Scroll.isActiveAndEnabled || !Scroll.content)return false;
            RefreshLayout();
            if(!_explicitTargets){_targets.Clear();if(_snapContainer)for(int i=0;i<_snapContainer.childCount;i++)if(_snapContainer.GetChild(i) is RectTransform rect)_targets.Add(rect);}
            RectTransform nearest=null;float distance=float.PositiveInfinity;int axis=_snapAxis==EUIScrollAxis.Horizontal?0:1;
            foreach(var target in _targets)
            {if(!ValidTarget(target)||!target.gameObject.activeInHierarchy)continue;float d=Mathf.Abs(Project(target).center[axis]-View.rect.center[axis]);if(d<distance){distance=d;nearest=target;}}
            if(!nearest){SetFocus(null);return false;}
            var id=_nextId;SetFocus(nearest);if(id!=_nextId)return false;
            Begin(default,nearest,true,EUIScrollAlignment.Center,default,_snapDuration,null,true);return true;
        }
        public void OnBeginDrag(PointerEventData eventData) {if(eventData.button!=PointerEventData.InputButton.Left)return;_dragging=true;_manual=true;_lastMotion=Now;Finish(_operation,EUIScrollStatus.UserInterrupted);}
        public void OnEndDrag(PointerEventData eventData) {if(eventData.button!=PointerEventData.InputButton.Left)return;_dragging=false;_manual=true;_lastMotion=Now;}
        public void OnScroll(PointerEventData eventData) {_manual=true;_lastMotion=Now;Finish(_operation,EUIScrollStatus.UserInterrupted);}
        #endregion
    }
}
