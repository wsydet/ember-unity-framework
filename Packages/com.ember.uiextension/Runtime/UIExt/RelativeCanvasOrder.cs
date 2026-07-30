//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//    [ExecuteAlways]
//    public class RelativeCanvasOrder : MonoBehaviour, ICanvasSortingOrderHandler
//    {
//        [SerializeField]
//        int orderOffset;
//
//        Canvas parentCanvas, cachedCanvas;
//
//        public int OrderOffset
//        {
//            get { return orderOffset; }
//            set
//            {
//                if (orderOffset != value)
//                {
//                    orderOffset = value;
//                    UpdateSortingOrder();
//                }
//            }
//        }
//        void Awake()
//        {
//            cachedCanvas = GetComponent<Canvas>();
//        }
//        void OnEnable()
//        {
//            if (!parentCanvas && transform.parent)
//            {   
//                parentCanvas = transform.parent.gameObject.GetComponentInParent<Canvas>();
//            }
//
//            UpdateSortingOrder();
//        }
//
//        void OnDisable()
//        {
//            parentCanvas = null;
//        }
//
//        public void UpdateSortingOrder()
//        {
//            if(cachedCanvas && parentCanvas)
//            {
//                cachedCanvas.overrideSorting = true;
//                cachedCanvas.sortingOrder = parentCanvas.sortingOrder + orderOffset;
//            }
//        }
//    }
//}
