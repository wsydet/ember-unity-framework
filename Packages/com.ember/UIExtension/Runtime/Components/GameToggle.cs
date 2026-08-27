//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using System;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class GameToggle : GameSelectable
//    {
//        private Toggle toggle;
//        private Action<GameToggle, bool> valueChangeAction;
//        private bool handlerAdded;
//        public override void OnInit()
//        {
//            base.OnInit();
//            toggle = selectable as Toggle;
//            if (!clickAdded)
//            {
//                clickAdded = true;
//                AddClickCallBack();
//            }
//        }
//
//        /// <summary>
//        /// 当勾选状态变更时触发的事件
//        /// </summary>
//        public Action<GameToggle, bool> OnValueChange
//        {
//            get => valueChangeAction;
//            set
//            {
//                if (!handlerAdded)
//                {
//                    handlerAdded = true;
//                    toggle.onValueChanged.AddListener(HandleValueChange);
//                }
//                valueChangeAction = value;
//            }
//        }
//
//        /// <summary>
//        /// 获取或设置勾选框是否可点
//        /// </summary>
//        public override bool Enable
//        {
//            get
//            {
//                if (toggle)
//                    return toggle.interactable;
//                return false;
//            }
//            set
//            {
//                if (toggle)
//                    toggle.interactable = value;
//            }
//        }
//
//        /// <summary>
//        /// 获取或设置当前勾选状态
//        /// </summary>
//        public bool IsChecked
//        {
//            get
//            {
//                return toggle.isOn;
//            }
//            set
//            {
//                if (toggle)
//                {
//                    toggle.isOn = value;
//                }
//            }
//        }
//
//        void HandleValueChange(bool isOn)
//        {
//            valueChangeAction?.Invoke(this, isOn);
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            valueChangeAction = null;
//        }
//    }
//}
