//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class GameToggleGroup : GameUIComponent
//    {
//        private ToggleGroup toggleGroup;
//        private ToggleGroupEx toggleGroupEx;
//        public override void OnInit()
//        {
//            base.OnInit();
//            toggleGroup = GetComponent<ToggleGroup>();
//            toggleGroupEx = toggleGroup as ToggleGroupEx;
//            if (toggleGroupEx)
//            {
//                toggleGroupEx.ToggleGroupValueChange.AddListener((cur, pre) =>
//                {
//                    if (onValueChange != null)
//                    {
//                        onValueChange.Invoke(cur, pre);
//                    }
//                });
//            }
//        }
//
//        Action<int, int> onValueChange;
//
//
//        /// <summary>
//        /// 设置选项变更的回调
//        /// </summary>
//        /// <param name="action"></param>
//        public void SetOnGroupToggleValeChange(Action<int, int> action)
//        {
//            onValueChange = action;
//        }
//        /// <summary>
//        /// 设置当前选中的选项Index
//        /// </summary>
//        /// <param name="index"></param>
//        public void SetToggleOn(int index)
//        {
//            toggleGroupEx.SetToggleOn(index);
//        }
//
//        /// <summary>
//        /// 获取当前选中的选项Index
//        /// </summary>
//        public int CurOn
//        {
//            set
//            {
//                toggleGroupEx.SetToggleOn(value);
//            }
//            get
//            {
//                return toggleGroupEx.GetCurrentOnToggle();
//            }
//        }
//
//        public bool AllowSwitchOff
//        {
//            set => toggleGroupEx.allowSwitchOff = value;
//            get => toggleGroupEx.allowSwitchOff;
//        }
//
//        protected override void ClearEventCallbacks()
//        {
//            base.ClearEventCallbacks();
//            onValueChange = null;
//        }
//    }
//}
