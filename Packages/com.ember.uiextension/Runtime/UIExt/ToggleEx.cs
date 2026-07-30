//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class ToggleEx : Toggle
//    {
//        [SerializeField]
//        private GameObject onNode;
//        [SerializeField]
//        private GameObject offNode;
//        [SerializeField]
//        private GameObject disableNode;
//        protected override void Awake()
//        {
//            base.Awake();
//            onValueChanged.AddListener(OnValueChanged);
//        }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            RefreshVisibility();
//        }
//
//        void RefreshVisibility()
//        {
//            if (interactable || !disableNode)
//            {
//                if (isOn)
//                {
//                    if (onNode)
//                        onNode.SetActive(true);
//                    if (offNode)
//                        offNode.SetActive(false);
//                }
//                else
//                {
//                    if (onNode)
//                        onNode.SetActive(false);
//                    if (offNode)
//                        offNode.SetActive(true);
//                }
//                if (disableNode)
//                    disableNode.SetActive(false);
//            }
//            else
//            {
//                if (disableNode)
//                    disableNode.SetActive(true);
//                if (onNode)
//                    onNode.SetActive(false);
//                if (offNode)
//                    offNode.SetActive(false);
//            }
//        }
//
//        void OnValueChanged(bool isOn)
//        {
//            RefreshVisibility();
//        }
//        protected override void DoStateTransition(SelectionState state, bool instant)
//        {
//            base.DoStateTransition(state, instant);
//            RefreshVisibility();
//        }
//    }
//}
