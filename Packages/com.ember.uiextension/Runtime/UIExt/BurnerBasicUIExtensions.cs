//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Extensions;
//using UnityEngine;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    public static class BurnerBasicUIExtensions
//    {
//        public static Image GetChildImage(this Transform root, string childName)
//        {
//            var child = root.GetChildByName(childName);
//            return child != null ? child.GetComponent<Image>() : null;
//        }
//
//        public static Text GetChildText(this Transform root, string childName)
//        {
//            var child = root.GetChildByName(childName);
//            return child.IsNotNull() ? child.GetComponent<Text>() : null;
//        }
//
//        public static void SetTextColor(this Graphic graphic, float r, float g, float b, float a)
//        {
//            graphic.color = new Color(r, g, b, a);
//        }
//    }
//}
