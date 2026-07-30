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
//using Burner.Extensions;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public class SoftMaskDisableListener : MonoBehaviour
//    {
//        public SoftMask softMask { get; internal set; }
//
//        private bool _expectEnable;
//        
//        private void OnDisable()
//        {
//            if(softMask.IsNotNull())
//            {
//                softMask.SetSoftMaskMode(false);
//                softMask.DisableSoftMaskInternal();
//            }
//        }
//
//        private void OnEnable()
//        {
//            _expectEnable = true;// enable soft mask in LateUpdate
//        }
//        
//        private void LateUpdate()
//        {
//            if(softMask.IsNotNull())
//            {
//                if(_expectEnable)
//                {
//                    _expectEnable = false;
//                    softMask.SetSoftMaskMode(true);
//                    softMask.LateUpdate();    
//                }
//            }
//        }
//
//        private void OnDestroy()
//        {
//            if(softMask.IsNotNull())
//            {
//                softMask.OnDestroyImpl();
//                softMask = null;
//            }
//        }
//    }
//}
