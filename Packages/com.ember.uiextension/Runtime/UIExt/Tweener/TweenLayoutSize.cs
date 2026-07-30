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
//    /// 补间Width
//    /// </summary>
//
//    [RequireComponent(typeof(LayoutElement))]
//    public class TweenLayoutSize : UITweener
//    {
//        public Vector2 from = Vector2.zero;
//        public Vector2 to = new Vector2(100, 100);
//
//        LayoutElement mLayoutElement;
//        //UITable mTable;
//
//        public LayoutElement CachedLayoutElement
//        {
//            get
//            {
//                if (mLayoutElement == null)
//                    mLayoutElement = GetComponent<LayoutElement>();
//                return mLayoutElement;
//            }
//
//        }
//
//
//
//        public Vector2 value
//        {
//            get
//            {
//                return new Vector2(CachedLayoutElement.preferredWidth, CachedLayoutElement.preferredHeight);
//            }
//            set
//            {
//                CachedLayoutElement.preferredWidth = value.x;
//                CachedLayoutElement.preferredHeight = value.y;
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
