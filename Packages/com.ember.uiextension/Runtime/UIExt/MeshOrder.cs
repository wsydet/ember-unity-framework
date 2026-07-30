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
//
//namespace Burner.UIExtension
//{
//    [ExecuteAlways]
//    public class MeshOrder : MonoBehaviour, ICanvasSortingOrderHandler
//    {
//        public int orderOffset = 0;
//        
//        
//        int baseSortingOrder;
//        int curSortingOrder;
//        private Renderer[] allRenderers = null;
//
//        void Update()
//        {
//            if (curSortingOrder != orderOffset + baseSortingOrder)
//            {
//                UpdateSortingOrderImpl();
//            }
//        }
//
//        private void OnEnable()
//        {
//            UpdateSortingOrder();
//        }
//
//        public void UpdateSortingOrder()
//        {
//            var parentCanvas = transform.parent.gameObject.GetComponentInParent<Canvas>(true);
//            if (parentCanvas == null)
//                return;
//        
//            baseSortingOrder = parentCanvas.sortingOrder;
//
//            UpdateSortingOrderImpl();
//        }
//
//        public void UpdateRenderers()
//        {
//            allRenderers = GetComponentsInChildren<Renderer>(true);
//        }
//
//        private void UpdateSortingOrderImpl()
//        {
//            curSortingOrder = baseSortingOrder + orderOffset;
//            if(allRenderers==null)UpdateRenderers();
//            foreach (var renderer in allRenderers)
//            {
//                renderer.sortingOrder = curSortingOrder;
//            }
//        }
//    }
//}
