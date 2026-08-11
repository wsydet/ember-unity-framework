//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class GameProgressBar : GameUIComponent
//    {
//        private Slider pb;
//        private Action<GameProgressBar, float> valueChangeAction;
//        private bool handlerAdded;
//        public override void OnInit()
//        {
//            pb = GetComponent<Slider>();
//        }
//
//        /// <summary>
//        /// 设置进度条是否可被用户拖动
//        /// </summary>
//        public bool Enable
//        {
//            get { return pb.interactable; }
//            set
//            {
//                if (pb)
//                {
//                    pb.interactable = value;
//                }
//            }
//        }
//
//
//        /// <summary>
//        /// 当进度状态变更时触发的事件
//        /// </summary>
//        public Action<GameProgressBar, float> OnValueChange
//        {
//            get => valueChangeAction;
//            set
//            {
//                if (!handlerAdded)
//                {
//                    handlerAdded = true;
//                    pb.onValueChanged.AddListener(HandleValueChange);
//                }
//                valueChangeAction = value;
//            }
//        }
//        /// <summary>
//        /// 获取或设置当前进度
//        /// </summary>
//        public float Value
//        {
//            get { return pb.value; }
//            set
//            {
//                if (!Mathf.Approximately(pb.value, value))
//                    pb.value = value;
//            }
//        }
//        public void TweenProgressTo(float to, float time = 0.3f, int curveType = 0, Action onEnd = null)
//        {
//            float oldVal = Value; 
//            var tween = UITweener.StartNewTween<TweenProgressBarValue>(gameObject, time);
//            tween.from = oldVal;
//            tween.to = to;
//
//            if (time <= 0f)
//            {
//                tween.Sample(1f, true);
//                tween.enabled = false;
//            }
//            else
//                tween.Sample(0, false);
//
//            tween.OnFinishCallback = onEnd;
//            tween.animationCurve = GetCurveByType(curveType);
//            CheckAndUpdateTweeners(tween);
//        }
//        void HandleValueChange(float value)
//        {
//            valueChangeAction?.Invoke(this, value);
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            valueChangeAction = null;
//        }
//    }
//}
