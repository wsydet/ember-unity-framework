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
//    [RequireComponent(typeof(LayoutGroup))]
//    public class TweenLayoutGroupSize : UITweener
//    {
//        public Vector2 from = Vector2.zero;
//        public Vector2 to = new Vector2(1, 1);
//
//        LayoutElement mLayoutElement;
//        LayoutGroup mLayoutGroup;
//        //UITable mTable;
//        Vector2 prog;
//
//        public LayoutElement CachedLayoutElement
//        {
//            get
//            {
//                if (mLayoutElement == null)
//                {
//                    mLayoutElement = GetComponent<LayoutElement>();
//                    if (!mLayoutElement)
//                    {
//                        mLayoutElement = gameObject.AddComponent<LayoutElement>();
//                    }
//                }
//                return mLayoutElement;
//            }
//
//        }
//
//        public LayoutGroup CachedLayoutGroup
//        {
//            get
//            {
//                if (mLayoutGroup == null)
//                {
//                    mLayoutGroup = GetComponent<LayoutGroup>();
//                }
//                return mLayoutGroup;
//            }
//        }
//
//        public Vector2 value
//        {
//            get
//            {
//                return prog;
//            }
//            set
//            {
//                prog = value;
//                CachedLayoutElement.minWidth = 0;
//                CachedLayoutElement.minHeight = 0;
//                CachedLayoutElement.preferredWidth = Mathf.Max(CachedLayoutGroup.preferredWidth, CachedLayoutGroup.minWidth) * value.x;
//                CachedLayoutElement.preferredHeight = Mathf.Max(CachedLayoutGroup.preferredHeight, CachedLayoutGroup.minHeight) * value.y;
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
//    }
//}
