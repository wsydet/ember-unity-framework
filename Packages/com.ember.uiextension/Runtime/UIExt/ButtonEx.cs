//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class ButtonEx : Button
//    {
//        [SerializeField]
//        private GameObject disableNode;
//        [SerializeField]
//        private Graphic[] additionalGraphics;
//        [SerializeField]
//        private GameObject enableNode;
//        [SerializeField]
//        private bool enableState;
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            RefreshEnableState();
//        }
//
//        public bool EnableState
//        {
//            get => enableState || (!enableNode && !disableNode);
//            set
//            {
//                if(enableState != value)
//                {
//                    enableState = value;
//                    RefreshEnableState();
//                }
//            }
//        }
//
//        public void RefreshEnableState()
//        {
//            if (enableState)
//            {
//                if (enableNode)
//                    enableNode.SetActive(true);
//                if (disableNode)
//                    disableNode.SetActive(false);
//            }
//            else
//            {
//                if (enableNode)
//                    enableNode.SetActive(false);
//                if (disableNode)
//                    disableNode.SetActive(true);
//            }
//        }
//
//        protected override void DoStateTransition(SelectionState state, bool instant)
//        {
//            if (!gameObject.activeInHierarchy)
//                return;
//            if (transition == Transition.ColorTint)
//            {
//                Color tintColor;
//
//                switch (state)
//                {
//                    case SelectionState.Normal:
//                        tintColor = colors.normalColor;
//                        break;
//                    case SelectionState.Highlighted:
//                        tintColor = colors.highlightedColor;
//                        break;
//                    case SelectionState.Pressed:
//                        tintColor = colors.pressedColor;
//                        break;
//                    case SelectionState.Selected:
//                        tintColor = colors.selectedColor;
//                        break;
//                    case SelectionState.Disabled:
//                        tintColor = colors.disabledColor;
//                        break;
//                    default:
//                        tintColor = Color.black;
//                        break;
//                }
//                if (targetGraphic != null)
//                    targetGraphic.CrossFadeColor(tintColor, instant ? 0f : colors.fadeDuration, true, true);
//                if(additionalGraphics!=null && additionalGraphics.Length > 0)
//                {
//                    foreach(var g in additionalGraphics)
//                    {
//                        g.CrossFadeColor(tintColor, instant ? 0f : colors.fadeDuration, true, true);
//                    }
//                }
//            }
//            else
//                base.DoStateTransition(state, instant);
//        }
//    }
//}
