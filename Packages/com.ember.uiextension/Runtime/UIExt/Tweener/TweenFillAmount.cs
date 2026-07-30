//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.UI;
//namespace Burner.UIExtension
//{
//    /// <summary>
//    /// 补间FillAmount
//    /// </summary>
//
//    public class TweenFillAmount : UITweener
//    {
//        public float from = 0;
//        public float to = 1;
//
//        Image mImage;
//
//        public Image CachedLayoutElement
//        {
//            get
//            {
//                if (mImage == null)
//                    mImage = GetComponent<Image>();
//                return mImage;
//            }
//
//        }
//
//
//
//        public float value
//        {
//            get
//            {
//                return CachedLayoutElement.fillAmount;
//            }
//            set
//            {
//                CachedLayoutElement.fillAmount = value;
//            }
//        }
//
//        /// <summary>
//        /// Tween the value.
//        /// </summary>
//
//        protected override void OnUpdate(float factor, bool isFinished)
//        {
//            value = from * (1f - factor) + to * factor;
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
