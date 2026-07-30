//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    /// <summary>
//    /// 坐标补间
//    /// </summary>
//    public class TweenPosition : UITweener
//    {
//        public Vector3 from;
//        public bool hasThroughPoint;
//        public Vector3 through;
//        public Vector3 to;
//
//        /// <summary>
//        /// 是否是世界坐标
//        /// </summary>
//        [HideInInspector]
//        public bool worldSpace = false;
//        /// <summary>
//        /// 是否有ugui
//        /// </summary>
//        public bool notUGUI = false;
//        RectTransform mRectTransform;
//        Transform mTrans;
//
//        public Transform cachedTransform
//        {
//            get
//            {
//                if (mTrans == null)
//                    mTrans = transform;
//                return mTrans;
//            }
//        }
//        public RectTransform cachedRectTransform
//        {
//            get
//            {
//                if (!notUGUI)
//                {
//                    if (mRectTransform == null)
//                    {
//                        mRectTransform = gameObject.GetComponent<RectTransform>();
//                        if (mRectTransform == null)
//                        {
//                            notUGUI = true;
//                            return transform as RectTransform;
//                        }
//                    }
//                    return mRectTransform;
//                }
//                else
//                {
//                    return transform as RectTransform;
//                }
//            }
//        }
//        public Vector3 value
//        {
//            get
//            {
//                return worldSpace ? cachedTransform.localPosition : cachedRectTransform.anchoredPosition3D;
//            }
//            set
//            {
//                if (worldSpace)
//                {
//                    cachedTransform.localPosition = value;
//                }
//                else
//                {
//                    cachedRectTransform.anchoredPosition3D = value;
//                }
//
//            }
//        }
//
//        void Awake()
//        {
//            mRectTransform = GetComponent<RectTransform>();
//            if (mRectTransform == null)
//            {
//                worldSpace = true; notUGUI = true;
//            } 
//        }
//
//
//        protected override void OnUpdate(float factor, bool isFinished)
//        {
//            if (hasThroughPoint)
//            {
//                value = (1f - factor) * (1f - factor) * from + 2 * (1 - factor) * factor * through + factor * factor * to;
//            }
//            else
//                value = from * (1f - factor) + to * factor;
//        }
//
//        /// <summary>
//        /// 开始补间操作
//        /// </summary>
//
//        static public TweenPosition Begin(GameObject go, float duration, Vector3 pos, bool hasThrough = false, Vector3 through = default)
//        {
//            Vector3 curPos;
//            if (go.transform is RectTransform rt)
//                curPos = rt.anchoredPosition3D;
//            else
//                curPos = go.transform.localPosition;
//            TweenPosition comp = UITweener.StartNewTween<TweenPosition>(go, duration);
//            comp.from = curPos;
//            comp.hasThroughPoint = hasThrough;
//            comp.through = through;
//            comp.to = pos;
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
//        [ContextMenu("设置当前值为From的值")]
//        public override void SetStartToCurrentValue() { from = value; }
//
//        [ContextMenu("设置当前值为To的值")]
//        public override void SetEndToCurrentValue() { to = value; }
//
//        [ContextMenu("切换到From值状态")]
//        void SetCurrentValueToStart() { value = from; }
//
//        [ContextMenu("切换到To值状态")]
//        void SetCurrentValueToEnd() { value = to; }
//    }
//}
