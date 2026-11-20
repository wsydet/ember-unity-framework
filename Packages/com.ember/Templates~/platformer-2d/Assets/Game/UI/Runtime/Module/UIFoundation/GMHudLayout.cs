using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>只避让显式登记的 UI 矩形；布局与命中使用同一圆。</summary>
    public static class GMHudLayout
    {
        #region 内部参数
        private static readonly List<Avoidance> Avoid = new();
        private static readonly float[] Scales = { .83f, .76f, .69f, .62f };
        private sealed class Avoidance : IDisposable
        {
            internal RectTransform Rect;
            public void Dispose() { Avoid.Remove(this); }
        }
        #endregion
        #region 内部方法
        private static bool Clear(Vector2[] points, float hit, Vector2 entry, float entrySize, Rect safe, IReadOnlyList<Rect> avoid, float guard)
        {
            float radius = hit * .5f + guard;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p = points[i];
                if (p.x - radius < safe.xMin || p.x + radius > safe.xMax || p.y - radius < safe.yMin || p.y + radius > safe.yMax) return false;
                if (Vector2.Distance(p, entry) < radius + entrySize * .5f + 5) return false;
                for (int j = 0; j < i; j++) if (Vector2.Distance(p, points[j]) < hit + 5 + guard * 2) return false;
                for (int j = 0; j < avoid.Count; j++)
                {
                    var rect = avoid[j]; var nearest = new Vector2(Mathf.Clamp(p.x, rect.xMin, rect.xMax), Mathf.Clamp(p.y, rect.yMin, rect.yMax));
                    if ((p - nearest).sqrMagnitude < radius * radius) return false;
                }
            }
            return true;
        }
        #endregion
        #region 外部方法
        [HasGC]
        public static IDisposable RegisterAvoidRect(RectTransform rect)
        { if (!rect) throw new ArgumentNullException(nameof(rect)); var lease = new Avoidance { Rect = rect }; Avoid.Add(lease); return lease; }
        [HasGC]
        public static List<Rect> GetAvoidRects(RectTransform parent)
        {
            var result = new List<Rect>(); var corners = new Vector3[4];
            foreach (var item in Avoid)
            {
                if (!item.Rect || !item.Rect.gameObject.activeInHierarchy) continue;
                item.Rect.GetWorldCorners(corners); var min = (Vector2)parent.InverseTransformPoint(corners[0]); var max = min;
                for (int i = 1; i < 4; i++) { var point = (Vector2)parent.InverseTransformPoint(corners[i]); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
                result.Add(Rect.MinMaxRect(min.x - 10, min.y - 10, max.x + 10, max.y + 10));
            }
            return result;
        }
        [HasGC]
        public static bool TryFit(int count, Vector2 entry, float entrySize, Rect safe, IReadOnlyList<Rect> avoid, int tier, float guard, out Vector2[] points, out float visible, out float hit)
        {
            visible = entrySize * Scales[tier]; hit = Mathf.Max(visible, entrySize * .64f, 48); points = new Vector2[count];
            if (count == 0) return true;
            float inward = entry.x >= safe.center.x ? -1 : 1;
            // Normal mild side arc, with only a small vertical correction.
            // guard 是每个命中圆额外的半径；候选间距必须同步增加两份，避免放大检查否定自身。
            float step = hit + Mathf.Max(12, 6 + guard * 2), span = (count - 1) * step;
            float correction = Mathf.Clamp(entry.y, safe.yMin + hit * .5f + guard + span * .5f, safe.yMax - hit * .5f - guard - span * .5f) - entry.y;
            if (Mathf.Abs(correction) <= hit * .45f)
            {
                for (int i = 0; i < count; i++)
                { float bend = count < 3 ? 0 : Mathf.Sin(i * Mathf.PI / (count - 1)) * visible * .38f; points[i] = entry + new Vector2(inward * (entrySize * .5f + hit * .5f + 12 + guard + bend), span * .5f - i * step + correction); }
                if (Clear(points, hit, entry, entrySize, safe, avoid, guard)) return true;
            }
            float preferred = entry.y < safe.center.y ? 1 : -1;
            foreach (float direction in new[] { preferred, -preferred })
            foreach (float degrees in count == 2 ? new[] { 55f, 70f } : new[] { 90f, 100f, 75f, 60f })
            foreach (float gap in new[] { 8f, 6f })
            {
                float angle = degrees * Mathf.Deg2Rad;
                float radius = count == 1 ? (entrySize + hit) * .5f + 8 + guard : Mathf.Max((entrySize + hit) * .5f + 6 + guard, (hit + gap + guard * 2) / (2 * Mathf.Sin(angle / (count - 1) * .5f)));
                float budget = entrySize * (count < 3 ? 1.7f : .6f * count + .2f);
                if (radius + hit * .5f + guard > budget) continue;
                for (int i = 0; i < count; i++) { float a = count == 1 ? 0 : i * angle / (count - 1); points[i] = entry + new Vector2(inward * radius * Mathf.Cos(a), direction * radius * Mathf.Sin(a)); }
                if (Clear(points, hit, entry, entrySize, safe, avoid, guard)) return true;
            }
            return false;
        }
        #endregion
    }
}
