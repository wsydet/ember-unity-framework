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
//    /// Tween the object's local rotation.
//    /// </summary>
//
//    public class TweenRotation : UITweener
//    {
//        public float from = 0;
//        public float to = 360;
//        public bool updateTable = false;
//
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
//
//        public float value
//        {
//            get
//            {
//                return cachedTransform.localRotation.eulerAngles.z;
//            }
//            set
//            {
//                var rot = cachedTransform.localRotation.eulerAngles;
//                rot.z = value;
//                cachedTransform.localRotation = Quaternion.Euler(rot);
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
//        static public TweenRotation Begin(GameObject go, float duration, float rotation)
//        {
//            var curRotation = go.transform.localRotation.eulerAngles.z;
//            TweenRotation comp = UITweener.StartNewTween<TweenRotation>(go, duration);
//            comp.from = curRotation;
//            comp.to = rotation;
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
