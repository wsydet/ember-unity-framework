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
//    /// 补间Size
//    /// </summary>
//
//    [RequireComponent(typeof(RectTransform))]
//    public class TweenSize : UITweener
//    {
//        public Vector2 from = Vector2.zero;
//        public Vector2 to = new Vector2(100, 100);
//        public bool updateTable = false;
//
//        RectTransform mRectTransform;
//
//        public RectTransform cachedWidget
//        {
//            get
//            {
//                if (mRectTransform == null)
//                    mRectTransform = GetComponent<RectTransform>();
//                return mRectTransform;
//            }
//        }
//
//        public Vector2 value
//        {
//            get
//            {
//                return  cachedWidget.sizeDelta;
//            }
//            set
//            {
//                cachedWidget.sizeDelta = value;
//            }
//        }
//
//        protected override void OnUpdate(float factor, bool isFinished)
//        {
//            value = from * (1f - factor) + to * factor;
//        }
//
//        /// <summary>
//        /// 开始补间操作
//        /// </summary>
//        static public TweenSize Begin(RectTransform rectTransform, float duration, Vector2 size)
//        {
//            TweenSize comp = UITweener.StartNewTween<TweenSize>(rectTransform.gameObject, duration);
//            comp.from = rectTransform.sizeDelta;
//            comp.to = size;
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
