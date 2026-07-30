//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//
//namespace Burner.UIExtension
//{
//    public static class DefaultAnimationCurves
//    {
//        public static Keyframe[] Linear = { new Keyframe(0f, 0f, 0f, 1f), new Keyframe(1f, 1f, 1f, 0f) };
//        public static Keyframe[] SlowStartFastProgression = { new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f) };
//        public static Keyframe[] FastStartSlowProgression = { new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f) };
//        public static Keyframe[] SlowStartSlowEnd = { new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f) };
//    }
//    /// <summary>
//    /// 所有补间操作的基类
//    /// </summary>
//    [ExecuteAlways]
//    public abstract class UITweener : MonoBehaviour
//    {
//        /// <summary>
//        /// 当前的补间动画触发回调函数。
//        /// </summary>
//        static public UITweener current;
//
//        public EaseType easeType = EaseType.linear;
//        //[HideInInspector]
//        public Style style = Style.Once;
//
//
//        /// <summary>
//        /// 动画曲线
//        /// </summary>
//        [HideInInspector]
//        public AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 1f), new Keyframe(1f, 1f, 1f, 0f));
//
//        public enum Style
//        {
//            Once,
//            Loop,
//            PingPong,
//            OnHide,
//            OnShowHide
//        }
//        /// <summary>
//        /// 补间是否忽略时标
//        /// </summary>
//
//        [HideInInspector]
//        public bool ignoreTimeScale = false;
//
//        /// <summary>
//        /// 延迟
//        /// </summary>
//
//        [HideInInspector]
//        public float delay = 0f;
//
//        /// <summary>
//        /// 补间时常
//        /// </summary>
//
//
//        public float duration = 1f;
//
//        /// <summary>
//        /// 是否使用较陡的曲线 便于in/out风格插值。
//        /// </summary>
//
//        [HideInInspector]
//        public bool steeperCurves = false;
//
//        /// <summary>
//        /// 补间序列
//        /// </summary>
//
//        [HideInInspector]
//        public int tweenGroup = 0;
//
//        public bool playOnEnable = true;
//
//        /// <summary>
//        /// 动画结束时回调
//        /// </summary>
//
//        [HideInInspector]
//        public List<EventDelegate> onFinished = new List<EventDelegate>();
//
//        public System.Action OnFinishCallback { get; set; }
//
//        bool mStarted = false;
//        float mStartTime = 0f;
//        float mDuration = 0f;
//        float step = 1000f;
//        float curFactor = 0f;
//        bool disableOnFinish = false;
//
//        private void Awake()
//        {
//            disableOnFinish = !enabled;
//        }
//
//        /// <summary>
//        /// 每次增量
//        /// </summary>
//
//        public float Step
//        {
//            get
//            {
//                if (mDuration != duration)
//                {
//                    mDuration = duration;
//                    step = Mathf.Abs((duration > 0f) ? 1f / duration : 1000f) * Mathf.Sign(step);
//                }
//                return step;
//            }
//        }
//
//        /// <summary>
//        /// 补间因子，0-1
//        /// </summary>
//
//        public float tweenFactor { get { return curFactor; } set { curFactor = Mathf.Clamp01(value); } }
//
//        /// <summary>
//        /// Direction that the tween is currently playing in.
//        /// 当前使用的补间动画
//        /// </summary>
//
//        public Direction direction { get { return Step < 0f ? Direction.Reverse : Direction.Forward; } }
//
//        /// <summary>
//        /// 添加组件时自动重置.
//        /// </summary>
//        void Reset()
//        {
//            if (!mStarted)
//            {
//                SetStartToCurrentValue();
//                SetEndToCurrentValue();
//            }
//        }
//
//        private void OnEnable()
//        {
//            if (Application.isPlaying && playOnEnable)
//            {
//                ResetToBeginning();
//                PlayForward();
//            }
//        }
//
//        protected virtual void Start() { Update(); }
//
//#if UNITY_EDITOR
//        double editorDeltaTime = 0f;
//        double lastTimeSinceStartup = 0f;
//        private void SetEditorDeltaTime()
//        {
//            if (lastTimeSinceStartup == 0f)
//            {
//                lastTimeSinceStartup = UnityEditor.EditorApplication.timeSinceStartup;
//            }
//            editorDeltaTime = UnityEditor.EditorApplication.timeSinceStartup - lastTimeSinceStartup;
//            lastTimeSinceStartup = UnityEditor.EditorApplication.timeSinceStartup;
//        }
//#endif
//        void Update()
//        {
//            if (mStarted)
//            {
//#if UNITY_EDITOR
//                float delta = 0;
//                float time = 0;
//                if (Application.isPlaying)
//                {
//                    delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
//                    time = ignoreTimeScale ? Time.unscaledTime : Time.time;
//                }
//                else
//                {
//                    SetEditorDeltaTime();
//                    delta = (float)editorDeltaTime;
//                    time = (float)lastTimeSinceStartup;
//                }
//                
//#else
//                float delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
//                float time = ignoreTimeScale ? Time.unscaledTime : Time.time;
//#endif
//                /*if (!mStarted)
//                {
//                    mStarted = true;
//                    mStartTime = time + delay;
//                }*/
//
//                if (time < mStartTime) return;
//
//                curFactor += Step * delta;
//
//                if (style == Style.Loop)
//                {
//                    if (curFactor > 1f)
//                    {
//                        curFactor -= Mathf.Floor(curFactor);
//                    }
//                }
//                else if (style == Style.PingPong)
//                {
//                    if (curFactor > 1f)
//                    {
//                        curFactor = 1f - (curFactor - Mathf.Floor(curFactor));
//                        step = -step;
//                    }
//                    else if (curFactor < 0f)
//                    {
//                        curFactor = -curFactor;
//                        curFactor -= Mathf.Floor(curFactor);
//                        step = -step;
//                    }
//                }
//
//                if ((style == Style.Once) && (duration == 0f || curFactor > 1f || curFactor < 0f))
//                {
//                    curFactor = Mathf.Clamp01(curFactor);
//                    Sample(curFactor, true);
//
//                    if (duration == 0f || (curFactor == 1f && step > 0f || curFactor == 0f && step < 0f))
//                        mStarted = false;
//                    if (OnFinishCallback != null)
//                    {
//                        var callback = OnFinishCallback;
//                        OnFinishCallback = null;
//                        callback();
//                    }
//                    if (disableOnFinish && !mStarted)
//                        enabled = false;
//
//                    if (current != this)
//                    {
//                        current = this;
//
//                        if (onFinished != null)
//                        {
//                            mTemp = onFinished;
//                            onFinished = new List<EventDelegate>();
//
//                            EventDelegate.Execute(mTemp);
//
//                            for (int i = 0; i < mTemp.Count; ++i)
//                            {
//                                EventDelegate ed = mTemp[i];
//                                if (ed != null) EventDelegate.Add(onFinished, ed, ed.oneShot);
//                            }
//                            mTemp = null;
//                        }
//
//                        current = null;
//                    }
//                }
//                else Sample(curFactor, false);
//            }
//        }
//#if UNITY_EDITOR
//        void OnDrawGizmos()
//        {
//            // Your gizmo drawing thing goes here if required...
//
//            // Ensure continuous Update calls.
//            if (!Application.isPlaying && mStarted)
//            {
//                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
//                UnityEditor.SceneView.RepaintAll();
//            }
//        }
//#endif
//        List<EventDelegate> mTemp = null;
//
//        /// <summary>
//        /// 设置一个新的委托事件
//        /// </summary>
//
//        public void SetOnFinished(EventDelegate.Callback del) { EventDelegate.Set(onFinished, del); }
//
//        /// <summary>
//        /// 设置一个新的委托事件
//        /// </summary>
//
//        public void SetOnFinished(EventDelegate del) { EventDelegate.Set(onFinished, del); }
//
//        /// <summary>
//        /// 添加新的委托.
//        /// </summary>
//
//        public void AddOnFinished(EventDelegate.Callback del) { EventDelegate.Add(onFinished, del); }
//
//        /// <summary>
//        /// 添加新的委托
//        /// </summary>
//
//        public void AddOnFinished(EventDelegate del) { EventDelegate.Add(onFinished, del); }
//
//        /// <summary>
//        /// 移除委托
//        /// </summary>
//
//        public void RemoveOnFinished(EventDelegate del)
//        {
//            if (onFinished != null) onFinished.Remove(del);
//            if (mTemp != null) mTemp.Remove(del);
//        }
//
//        /// <summary>
//        /// 标记为未启动
//        /// </summary>
//
//        void OnDisable()
//        {
//            mStarted = false;
//        }
//
//        /// <summary>
//        /// 在指定因素采样补间动画
//        /// </summary>
//
//        public void Sample(float factor, bool isFinished)
//        {
//            float val = Mathf.Clamp01(factor);
//            val = EaseManager.EasingFromType(0, 1, val, easeType);
//            // Add animationCurve By sxb
//            OnUpdate((animationCurve != null) ? animationCurve.Evaluate(val) : val, isFinished);
//            //OnUpdate(val, isFinished);
//        }
//
//        /// <summary>
//        /// 反弹逻辑
//        /// </summary>
//
//        float BounceLogic(float val)
//        {
//            if (val < 0.363636f) // 0.363636 = (1/ 2.75)
//            {
//                val = 7.5685f * val * val;
//            }
//            else if (val < 0.727272f) // 0.727272 = (2 / 2.75)
//            {
//                val = 7.5625f * (val -= 0.545454f) * val + 0.75f; // 0.545454f = (1.5 / 2.75) 
//            }
//            else if (val < 0.909090f) // 0.909090 = (2.5 / 2.75) 
//            {
//                val = 7.5625f * (val -= 0.818181f) * val + 0.9375f; // 0.818181 = (2.25 / 2.75) 
//            }
//            else
//            {
//                val = 7.5625f * (val -= 0.9545454f) * val + 0.984375f; // 0.9545454 = (2.625 / 2.75) 
//            }
//            return val;
//        }
//
//        /// <summary>
//        /// 正向播放
//        /// </summary>
//
//        public void PlayForward() { Play(true); }
//
//        /// <summary>
//        /// 反向播放
//        /// </summary>
//
//        public void PlayReverse() { Play(false); }
//
//        /// <summary>
//        /// 播放补间
//        /// </summary>
//
//        public void Play(bool forward)
//        {
//            step = Mathf.Abs(Step);
//            if (!forward) step = -step;
//            enabled = true;
//            float time = ignoreTimeScale ? Time.unscaledTime : Time.time;
//            mStarted = true;
//#if UNITY_EDITOR
//            if (!Application.isPlaying)
//            {
//                SetEditorDeltaTime();
//                time = (float)lastTimeSinceStartup;
//            }
//            mStartTime = time + delay;
//#else
//            mStartTime = time + delay;
//
//#endif
//            Update();
//        }
//
//        public void Stop()
//        {
//            mStarted = false;
//        }
//
//        public void ResetToEnding()
//        {
//            mStarted = false;
//            step = -1 * Mathf.Abs(step);
//            curFactor = (Step < 0f) ? 1f : 0f;
//            Sample(curFactor, false);
//        }
//
//        /// <summary>
//        /// 复位补间动画
//        /// </summary>
//
//        public void ResetToBeginning()
//        {
//            mStarted = false;
//            curFactor = (Step < 0f) ? 1f : 0f;
//            Sample(curFactor, false);
//        }
//
//        /// <summary>
//        /// 反转补间动画方向
//        /// </summary>
//
//        public void Toggle()
//        {
//            if (curFactor > 0f)
//            {
//                step = -Step;
//            }
//            else
//            {
//                step = Mathf.Abs(Step);
//            }
//            enabled = true;
//        }
//
//        /// <summary>
//        /// 实际补间的逻辑-继承
//        /// </summary>
//
//        abstract protected void OnUpdate(float factor, bool isFinished);
//
//        /// <summary>
//        /// 开始补间操作
//        /// </summary>
//
//        static public T StartNewTween<T>(GameObject go, float duration) where T : UITweener
//        {
//            T comp = go.GetComponent<T>();
//            //找到未设置id组的补间
//            if (comp != null && comp.tweenGroup != 0)
//            {
//                comp = null;
//                T[] comps = go.GetComponents<T>();
//                for (int i = 0, imax = comps.Length; i < imax; ++i)
//                {
//                    comp = comps[i];
//                    if (comp != null && comp.tweenGroup == 0) break;
//                    comp = null;
//                }
//            }
//
//            if (comp == null)
//            {
//                comp = go.AddComponent<T>();
//                comp.playOnEnable = false;//通过代码新建的Tween不应在OnEnable自动播
//            }
//            comp.duration = duration;
//            comp.curFactor = 0f;
//            comp.step = Mathf.Abs(comp.Step);
//            comp.mStartTime = Time.time;
//            comp.style = Style.Once;
//            comp.enabled = true;
//            comp.disableOnFinish = true;
//            comp.mStarted = true;
//
//            if (duration <= 0f)
//            {
//                comp.Sample(1f, true);
//                comp.enabled = false;
//            }
//            return comp;
//        }
//
//        /// <summary>
//        /// 设置开始(form)值
//        /// </summary>
//
//        public virtual void SetStartToCurrentValue() { }
//
//        /// <summary>
//        /// 设置结束(to)值
//        /// </summary>
//        public virtual void SetEndToCurrentValue() { }
//    }
//}
