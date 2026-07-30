//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.UI;
//
///// <summary>
///// 透明度补间
///// </summary>
//namespace Burner.UIExtension
//{
//    public interface ITweenAlphaTarget
//    {
//        float Alpha { get; set; }
//    }
//    public class TweenAlpha : UITweener
//    {
//        [Range(0f, 1f)]
//        public float from = 1f;
//        [Range(0f, 1f)]
//        public float to = 1f;
//        MaskableGraphic mMaskableGraphic;
//        bool maskableGot = false;
//        CanvasGroup canvasG;
//        ITweenAlphaTarget target;
//        Color mColor;
//        bool triedCanvas = false;
//        bool triedTarget = false;
//        public ITweenAlphaTarget CachedAlphaTarget
//        {
//            get
//            {
//#if UNITY_EDITOR
//                if (Application.isPlaying)
//                {
//                    if (target == null && !triedTarget)
//                    {
//                        triedTarget = true;
//                        target = GetComponent<ITweenAlphaTarget>();
//                        if (target == null) target = GetComponentInChildren<ITweenAlphaTarget>();
//                    }
//                    return target;
//                }
//                else {
//                    
//                    target = GetComponent<ITweenAlphaTarget>();
//                    if (target == null) target = GetComponentInChildren<ITweenAlphaTarget>();
//                    
//                    return target;
//                }
//#else
//                if (target == null && !triedTarget)
//                {
//                    triedTarget = true;
//                    target = GetComponent<ITweenAlphaTarget>();
//                    if (target == null) target = GetComponentInChildren<ITweenAlphaTarget>();
//                }
//                return target;
//#endif
//
//
//            }
//        }
//        public CanvasGroup CachedCanvasGroup
//        {
//            get
//            {
//#if UNITY_EDITOR
//                if (Application.isPlaying)
//                {
//                    if (canvasG == null && !triedCanvas)
//                    {
//                        triedCanvas = true;
//                        canvasG = GetComponent<CanvasGroup>();
//                        if (canvasG == null) canvasG = GetComponentInChildren<CanvasGroup>();
//                    }
//                    return canvasG;
//                }
//                else {
//                    
//                    canvasG = GetComponent<CanvasGroup>();
//                    if (canvasG == null) canvasG = GetComponentInChildren<CanvasGroup>();
//                    
//                    return canvasG;
//                }
//#else
//                if (canvasG == null && !triedCanvas)
//                {
//                    triedCanvas = true;
//                    canvasG = GetComponent<CanvasGroup>();
//                    if (canvasG == null) canvasG = GetComponentInChildren<CanvasGroup>();
//                }
//                return canvasG;
//#endif
//
//
//            }
//        }
//        public MaskableGraphic cachedMaskableGraphic
//        {
//            get
//            {
//                if (mMaskableGraphic == null && !maskableGot)
//                {
//                    mMaskableGraphic = GetComponent<MaskableGraphic>();
//                    if (mMaskableGraphic == null) mMaskableGraphic = GetComponentInChildren<MaskableGraphic>();
//                    maskableGot = true;
//                }
//                if(mMaskableGraphic)
//                    mColor = mMaskableGraphic.color;
//                return mMaskableGraphic;
//            }
//        }
//
//        public float value
//        {
//            get
//            {
//                return alpha;
//            }
//            set
//            {
//                alpha = value;
//            }
//        }
//
//        public float alpha
//        {
//            get
//            {
//                if (CachedCanvasGroup)
//                    return CachedCanvasGroup.alpha;
//                else if(cachedMaskableGraphic)
//                    return cachedMaskableGraphic.color.a;
//                else
//                {
//                    return CachedAlphaTarget.Alpha;
//                }
//            }
//            set
//            {
//                if (CachedCanvasGroup)
//                    CachedCanvasGroup.alpha = value;
//                else if(cachedMaskableGraphic)
//                    cachedMaskableGraphic.color = new Color(mColor.r, mColor.g, mColor.b, value);
//                else
//                {
//                    CachedAlphaTarget.Alpha = value;
//                }
//            }
//        }
//
//        protected override void OnUpdate(float factor, bool isFinished) { value = Mathf.Lerp(from, to, factor); }
//
//        /// <summary>
//        /// 开始补间操作
//        /// </summary>
//
//        static public TweenAlpha Begin(GameObject go, float duration, float alpha)
//        {
//            TweenAlpha comp = UITweener.StartNewTween<TweenAlpha>(go, duration);
//            comp.from = comp.value;
//            comp.to = alpha;
//
//            if (duration <= 0f)
//            {
//                comp.Sample(1f, true);
//                comp.enabled = false;
//            }
//            else
//                comp.Sample(0, false);
//            return comp;
//        }
//
//        public override void SetStartToCurrentValue() { from = value; }
//        public override void SetEndToCurrentValue() { to = value; }
//    }
//}
