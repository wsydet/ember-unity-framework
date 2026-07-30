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
//using System;
//
//namespace Burner.UIExtension
//{
//    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
//    public class BurnerUIExtensionAttribute : Attribute
//    {
//        Type componentType;
//        public string Name { get; set; }
//
//        public BurnerUIExtensionAttribute(Type componentType)
//        {
//            this.componentType = componentType;
//        }
//
//        public Type ComponentType
//        {
//            get { return componentType; }
//            set
//            {
//                componentType = value;
//            }
//        }
//    }
//}
