//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Burner.Extensions;
//
//namespace Burner.UIExtension
//{
//    [ExecuteAlways]
//    public class TransformCopier : MonoBehaviour
//    {
//        public  Transform copied;
//
//        private Transform selfTransform = null;
//
//        void Update()
//        {
//            if (copied.IsNull()) return;
//            if (selfTransform == null) selfTransform = transform;
//            
//            if (selfTransform.position != copied.transform.position)
//            {
//                selfTransform.position = copied.transform.position;
//            }
//
//            if (selfTransform.rotation != copied.transform.rotation)
//            {
//                selfTransform.rotation = copied.transform.rotation;
//            }
//
//            if (selfTransform.parent == null)
//            {
//                if (selfTransform.localScale != copied.lossyScale)
//                {
//                    selfTransform.localScale = copied.lossyScale;    
//                }
//                
//            }
//            else
//            {
//                var scale = Vector3.zero;
//                if(selfTransform.parent.lossyScale.x!=0) scale.x =  copied.lossyScale.x / selfTransform.parent.lossyScale.x;
//                if(selfTransform.parent.lossyScale.y!=0) scale.y =  copied.lossyScale.y / selfTransform.parent.lossyScale.y;
//                if(selfTransform.parent.lossyScale.z!=0) scale.z =  copied.lossyScale.z / selfTransform.parent.lossyScale.z;
//                if (scale != selfTransform.localScale)
//                {
//                    selfTransform.localScale = scale;
//                }
//            }
//
//        }
//    }
//}
