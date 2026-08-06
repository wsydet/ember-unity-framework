// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Text;

using Ember.Basic;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// RectTransform / Transform 扩展方法集合。
    /// </summary>
    public static class RectTransformExtensions
    {
        /// <summary>
        /// 获取 Transform 从根到自身的完整层级路径。
        /// 格式如 "GrandParent:Parent:Self"
        /// </summary>
        /// <param name="from">起始 Transform</param>
        /// <param name="stopAt">到此 Transform 停止（不含），null 表示一直到根</param>
        [HasGC]
        public static string GetFullPathName(this Transform from, Transform stopAt = null)
        {
            var sb = new StringBuilder();
            var cur = from;
            do
            {
                sb.Insert(0, sb.Length > 0 ? cur.name + ":" : cur.name);
                cur = cur.parent;
            }
            while (cur && cur != stopAt);

            return sb.ToString();
        }

        /// <summary>
        /// 将 Transform 的 local 坐标转换为目标 RectTransform 的相对坐标。
        /// </summary>
        /// <param name="from">源 Transform</param>
        /// <param name="to">目标 RectTransform</param>
        /// <param name="position">源 local 空间中的偏移位置，默认 (0,0)</param>
        /// <returns>在目标 RectTransform local 空间中的位置</returns>
        [NoGC]
        public static Vector2 GetRelativePos(this Transform from, RectTransform to, Vector2 position = default)
        {
            var worldPos = from.TransformPoint(position);
            return to.InverseTransformPoint(worldPos);
        }

        /// <summary>
        /// 将 Transform 的 local 坐标转换为目标 RectTransform 的相对坐标（含 anchor 偏移修正）。
        /// 适用于需要精确坐标对齐的场景。
        /// </summary>
        [NoGC]
        public static Vector2 GetRelativePosWithAnchor(this Transform from, RectTransform to, Vector2 position = default)
        {
            if (from is RectTransform fRt)
            {
                var pivot = fRt.pivot;
                pivot.y = 1 - pivot.y;
                position -= pivot * fRt.rect.size;
            }
            var worldPos = from.TransformPoint(position);
            Vector3 localPoint3 = to.InverseTransformPoint(worldPos);
            var pivot2 = to.pivot;
            pivot2.y = 1 - pivot2.y;
            var pivotDerivedOffset = pivot2 * to.rect.size;
            pivotDerivedOffset.y = -pivotDerivedOffset.y;
            return (Vector2)localPoint3 + pivotDerivedOffset;
        }

        /// <summary>
        /// 矩形相交检测。
        /// </summary>
        [NoGC]
        public static bool Intersect(this Rect a, Rect b)
        {
            return (a.xMin < b.xMax) && (a.xMax > b.xMin) && (a.yMin < b.yMax) && (a.yMax > b.yMin);
        }
    }
}
