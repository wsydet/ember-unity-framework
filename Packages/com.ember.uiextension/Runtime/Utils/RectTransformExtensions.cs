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
//    public static class RectTransformExtensions
//    {
//
//        public static string GetFullPathName(this Transform from, Transform stopAt = null)
//        {
//            StringBuilder sb = new StringBuilder();
//            Transform cur = from;
//            do
//            {
//                sb.Insert(0, sb.Length > 0 ? cur.name + ":" : cur.name);
//                cur = cur.parent;
//            }
//            while (cur && cur != stopAt);
//
//            return sb.ToString();
//        }
//        /// <summary>
//        /// Converts the anchoredPosition of the first RectTransform to the second RectTransform,
//        /// taking into consideration offset, anchors and pivot, and returns the new anchoredPosition
//        /// </summary>
//        public static Vector2 GetRelativePos(this Transform from, RectTransform to, Vector2 position = default(Vector2))
//        {
//            Vector2 localPoint;
//            //Vector2 fromPivotDerivedOffset = new Vector2(from.rect.width * 0.5f + from.rect.xMin, from.rect.height * 0.5f + from.rect.yMin);
//            var worldPos = from.TransformPoint(position);
//            localPoint = to.InverseTransformPoint(worldPos);
//            //Vector2 screenP = RectTransformUtility.WorldToScreenPoint(BurnerUIManager.Instance.UICamera, worldPos);
//            //screenP += fromPivotDerivedOffset;
//            //RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, BurnerUIManager.Instance.UICamera, out localPoint);
//            //Vector2 pivotDerivedOffset = new Vector2(to.rect.width * 0.5f + to.rect.xMin, to.rect.height * 0.5f + to.rect.yMin);
//            //return to.anchoredPosition + localPoint;// - pivotDerivedOffset;
//            return localPoint;
//        }
//
//        public static Vector2 GetRelativePosWithAnchor(this Transform from, RectTransform to, Vector2 position = default(Vector2))
//        {
//            Vector2 localPoint;
//            Vector2 pivot;
//            if (from is RectTransform fRt)
//            {
//                pivot = fRt.pivot;
//                pivot.y = 1 - pivot.y;
//                position -= pivot * fRt.rect.size;
//            }
//            var worldPos = from.TransformPoint(position);
//            localPoint = to.InverseTransformPoint(worldPos);
//            pivot = to.pivot;
//            pivot.y = 1 - pivot.y;
//            Vector2 pivotDerivedOffset = pivot * to.rect.size;// to.offsetMin;// new Vector2(to.rect.xMin, -to.rect.yMin);
//            pivotDerivedOffset.y = -pivotDerivedOffset.y;
//            return localPoint + pivotDerivedOffset;
//        }
//
//        public static bool Intersect(this Rect a, Rect b)
//        {
//            if ((a.xMin < b.xMax) && (a.xMax > b.xMin) && (a.yMin < b.yMax) && (a.yMax > b.yMin))
//                return true;
//            return false;
//        }
//    }
//}
