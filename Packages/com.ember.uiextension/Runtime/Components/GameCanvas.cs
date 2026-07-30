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
//    public class GameCanvas : GameUIComponent
//    {
//        Canvas canvas;
//        RelativeCanvasOrder relativeOrder;
//        public override void OnInit()
//        {
//            canvas = GetComponent<Canvas>();
//            relativeOrder = GetComponent<RelativeCanvasOrder>();
//        }
//
//        public int SortingOrder
//        {
//            get
//            {
//                return canvas.sortingOrder;
//            }
//            set
//            {
//                if (relativeOrder && relativeOrder.enabled)
//                    throw new NotSupportedException($"当前节点({Widget.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})已被RelativeCanvasOrder控制，无法手动设置SortingOrder");
//                if (!canvas.isRootCanvas)
//                    canvas.overrideSorting = true;
//                canvas.sortingOrder = value;
//            }
//        }
//
//        public int RelativeSortingOrder
//        {
//            get
//            {
//                if(!relativeOrder)
//                    throw new NotSupportedException($"当前节点({Widget.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})不包含RelativeCanvasOrder，无法获取相对Order");
//                return relativeOrder.OrderOffset;
//            }
//            set
//            {
//                if (!relativeOrder)
//                    throw new NotSupportedException($"当前节点({Widget.GetFullPathName(BurnerUIManager.Instance.RootNode.transform)})不包含RelativeCanvasOrder，无法设置相对Order");
//                relativeOrder.OrderOffset = value;
//            }
//        }
//    }
//}
