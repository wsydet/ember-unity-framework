// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>SceneUI 热路径共用的无分配数学辅助。</summary>
    public static class SceneUIMath
    {
        #region 外部方法

        [NoGC]
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        [NoGC]
        public static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        [NoGC]
        public static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        [NoGC]
        public static Rect Intersection(Rect left, Rect right)
        {
            float xMin = Mathf.Max(left.xMin, right.xMin);
            float yMin = Mathf.Max(left.yMin, right.yMin);
            float xMax = Mathf.Min(left.xMax, right.xMax);
            float yMax = Mathf.Min(left.yMax, right.yMax);

            if (xMax <= xMin || yMax <= yMin)
                return Rect.zero;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        #endregion
    }

    /// <summary>将世界坐标投影到 Scene Camera 的屏幕坐标。</summary>
    public static class SceneUIWorldProjector
    {
        #region 外部方法

        [NoGC]
        public static bool TryProject(
            Camera sceneCamera,
            Vector3 worldPosition,
            out SceneUIProjectionResult result)
        {
            if (!sceneCamera || !sceneCamera.isActiveAndEnabled || !SceneUIMath.IsFinite(worldPosition))
            {
                result = default;
                return false;
            }

            Vector3 viewportPosition = sceneCamera.WorldToViewportPoint(worldPosition);
            Vector3 screenPosition3D = sceneCamera.ViewportToScreenPoint(viewportPosition);
            var screenPosition = new Vector2(screenPosition3D.x, screenPosition3D.y);

            if (!SceneUIMath.IsFinite(viewportPosition) || !SceneUIMath.IsFinite(screenPosition))
            {
                result = default;
                return false;
            }

            result = new SceneUIProjectionResult(
                true,
                viewportPosition,
                screenPosition,
                viewportPosition.z);
            return true;
        }

        #endregion
    }

    /// <summary>根据相机裁剪面和屏幕区域计算 SceneUI 空间可见性。</summary>
    public static class SceneUIVisibilityEvaluator
    {
        #region 外部方法

        [NoGC]
        public static SceneUIVisibilityResult Evaluate(
            in SceneUIProjectionResult projection,
            Camera sceneCamera,
            Rect visibleRect,
            in SceneUIVisibilityPolicy policy)
        {
            return EvaluateInternal(
                projection,
                sceneCamera,
                visibleRect,
                policy,
                projection.ScreenPosition,
                default,
                false);
        }

        /// <summary>使用 View 的屏幕矩形执行 PartialRect / FullRect 精确边界判定。</summary>
        [NoGC]
        public static SceneUIVisibilityResult Evaluate(
            in SceneUIProjectionResult projection,
            Camera sceneCamera,
            Rect visibleRect,
            in SceneUIVisibilityPolicy policy,
            Vector2 boundsAnchorScreenPosition,
            Rect viewScreenRect)
        {
            return EvaluateInternal(
                projection,
                sceneCamera,
                visibleRect,
                policy,
                boundsAnchorScreenPosition,
                viewScreenRect,
                true);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static SceneUIVisibilityResult EvaluateInternal(
            in SceneUIProjectionResult projection,
            Camera sceneCamera,
            Rect visibleRect,
            in SceneUIVisibilityPolicy policy,
            Vector2 boundsAnchorScreenPosition,
            Rect viewScreenRect,
            bool hasViewBounds)
        {
            if (!projection.IsValid || !sceneCamera)
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.ProjectionInvalid);

            if (projection.Depth <= 0f)
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.BehindCamera);

            if (projection.Depth <= sceneCamera.nearClipPlane)
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.BeforeNearClip);

            if (policy.CheckFarClip && projection.Depth >= sceneCamera.farClipPlane)
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.BeyondFarClip);

            float padding = Mathf.Max(0f, policy.ScreenPadding);
            visibleRect.xMin += padding;
            visibleRect.xMax -= padding;
            visibleRect.yMin += padding;
            visibleRect.yMax -= padding;

            if (visibleRect.width <= 0f || visibleRect.height <= 0f)
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.OutOfBounds);

            if (!TryGetAllowedAnchorRect(
                    visibleRect,
                    policy.BoundsMode,
                    boundsAnchorScreenPosition,
                    viewScreenRect,
                    hasViewBounds,
                    out Rect allowedAnchorRect))
                return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.OutOfBounds);

            bool isOnScreen = ContainsInclusive(allowedAnchorRect, boundsAnchorScreenPosition);
            if (isOnScreen)
            {
                return new SceneUIVisibilityResult(
                    true,
                    true,
                    projection.ScreenPosition,
                    Vector2.zero,
                    SceneUIInvisibleReason.None);
            }

            Vector2 direction = boundsAnchorScreenPosition - visibleRect.center;
            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();

            switch (policy.OutOfBoundsMode)
            {
                case SceneUIOutOfBoundsMode.Keep:
                    return new SceneUIVisibilityResult(
                        true,
                        false,
                        projection.ScreenPosition,
                        direction,
                        SceneUIInvisibleReason.None);

                case SceneUIOutOfBoundsMode.Clamp:
                    var clampedAnchor = new Vector2(
                        Mathf.Clamp(boundsAnchorScreenPosition.x, allowedAnchorRect.xMin, allowedAnchorRect.xMax),
                        Mathf.Clamp(boundsAnchorScreenPosition.y, allowedAnchorRect.yMin, allowedAnchorRect.yMax));
                    Vector2 clamped = projection.ScreenPosition
                        + (clampedAnchor - boundsAnchorScreenPosition);
                    return new SceneUIVisibilityResult(
                        true,
                        false,
                        clamped,
                        direction,
                        SceneUIInvisibleReason.None);

                default:
                    return Invisible(projection.ScreenPosition, SceneUIInvisibleReason.OutOfBounds);
            }
        }

        private static bool TryGetAllowedAnchorRect(
            Rect visibleRect,
            SceneUIBoundsMode boundsMode,
            Vector2 anchorScreenPosition,
            Rect viewScreenRect,
            bool hasViewBounds,
            out Rect allowedAnchorRect)
        {
            if (boundsMode == SceneUIBoundsMode.Point)
            {
                allowedAnchorRect = visibleRect;
                return true;
            }

            if (!hasViewBounds)
            {
                allowedAnchorRect = default;
                return false;
            }

            float left = viewScreenRect.xMin - anchorScreenPosition.x;
            float right = viewScreenRect.xMax - anchorScreenPosition.x;
            float bottom = viewScreenRect.yMin - anchorScreenPosition.y;
            float top = viewScreenRect.yMax - anchorScreenPosition.y;

            if (boundsMode == SceneUIBoundsMode.FullRect)
            {
                allowedAnchorRect = Rect.MinMaxRect(
                    visibleRect.xMin - left,
                    visibleRect.yMin - bottom,
                    visibleRect.xMax - right,
                    visibleRect.yMax - top);
            }
            else
            {
                allowedAnchorRect = Rect.MinMaxRect(
                    visibleRect.xMin - right,
                    visibleRect.yMin - top,
                    visibleRect.xMax - left,
                    visibleRect.yMax - bottom);
            }

            return SceneUIMath.IsFinite(new Vector2(allowedAnchorRect.xMin, allowedAnchorRect.yMin))
                && SceneUIMath.IsFinite(new Vector2(allowedAnchorRect.xMax, allowedAnchorRect.yMax))
                && allowedAnchorRect.xMin <= allowedAnchorRect.xMax
                && allowedAnchorRect.yMin <= allowedAnchorRect.yMax;
        }

        private static bool ContainsInclusive(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin
                && point.x <= rect.xMax
                && point.y >= rect.yMin
                && point.y <= rect.yMax;
        }

        private static SceneUIVisibilityResult Invisible(
            Vector2 screenPosition,
            SceneUIInvisibleReason reason)
        {
            return new SceneUIVisibilityResult(
                false,
                false,
                screenPosition,
                Vector2.zero,
                reason);
        }

        #endregion
    }

    /// <summary>把最终屏幕坐标映射到目标 LayerRoot 本地坐标。</summary>
    public static class SceneUICanvasMapper
    {
        #region 外部方法

        [NoGC]
        public static bool TryMap(
            Canvas canvas,
            RectTransform layerRoot,
            Camera uiCamera,
            Vector2 screenPosition,
            Vector2 uiOffset,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = default;
            if (!canvas || !layerRoot || !SceneUIMath.IsFinite(screenPosition))
                return false;

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && !eventCamera)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    layerRoot,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
                return false;

            anchoredPosition = localPosition + uiOffset;
            return SceneUIMath.IsFinite(anchoredPosition);
        }

        /// <summary>把 View 的 UI 矩形转换成屏幕像素矩形，供精确边界判断使用。</summary>
        [NoGC]
        public static bool TryGetScreenRect(
            Canvas canvas,
            RectTransform layerRoot,
            Camera uiCamera,
            Vector2 anchoredPosition,
            in SceneUIViewMetrics metrics,
            float scale,
            out Vector2 anchorScreenPosition,
            out Rect screenRect)
        {
            anchorScreenPosition = default;
            screenRect = default;
            if (!canvas
                || !layerRoot
                || !metrics.IsValid
                || !SceneUIMath.IsFinite(anchoredPosition)
                || !SceneUIMath.IsFinite(scale)
                || scale <= 0f)
                return false;

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && !eventCamera)
                return false;

            Vector2 scaledSize = metrics.Size * scale;
            Vector2 min = anchoredPosition - Vector2.Scale(scaledSize, metrics.Pivot);
            Vector2 max = min + scaledSize;

            Vector2 screen0 = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                layerRoot.TransformPoint(new Vector3(min.x, min.y, 0f)));
            Vector2 screen1 = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                layerRoot.TransformPoint(new Vector3(min.x, max.y, 0f)));
            Vector2 screen2 = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                layerRoot.TransformPoint(new Vector3(max.x, min.y, 0f)));
            Vector2 screen3 = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                layerRoot.TransformPoint(new Vector3(max.x, max.y, 0f)));
            anchorScreenPosition = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                layerRoot.TransformPoint(new Vector3(anchoredPosition.x, anchoredPosition.y, 0f)));

            if (!SceneUIMath.IsFinite(screen0)
                || !SceneUIMath.IsFinite(screen1)
                || !SceneUIMath.IsFinite(screen2)
                || !SceneUIMath.IsFinite(screen3)
                || !SceneUIMath.IsFinite(anchorScreenPosition))
                return false;

            float xMin = Mathf.Min(Mathf.Min(screen0.x, screen1.x), Mathf.Min(screen2.x, screen3.x));
            float xMax = Mathf.Max(Mathf.Max(screen0.x, screen1.x), Mathf.Max(screen2.x, screen3.x));
            float yMin = Mathf.Min(Mathf.Min(screen0.y, screen1.y), Mathf.Min(screen2.y, screen3.y));
            float yMax = Mathf.Max(Mathf.Max(screen0.y, screen1.y), Mathf.Max(screen2.y, screen3.y));
            screenRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return screenRect.width > 0f && screenRect.height > 0f;
        }

        #endregion
    }

    /// <summary>统一计算 SceneUI 根节点缩放。</summary>
    public static class SceneUIScaleEvaluator
    {
        #region 外部方法

        [NoGC]
        public static float Evaluate(in SceneUIScalePolicy policy, float depth)
        {
            if (policy.Mode == SceneUIScaleMode.FixedScreenSize)
                return 1f;

            float safeDepth = Mathf.Max(Mathf.Abs(depth), 0.0001f);
            float referenceDistance = Mathf.Max(policy.ReferenceDistance, 0.0001f);
            float scale = Mathf.Pow(referenceDistance / safeDepth, policy.Factor);
            float min = Mathf.Min(policy.MinScale, policy.MaxScale);
            float max = Mathf.Max(policy.MinScale, policy.MaxScale);
            return Mathf.Clamp(scale, min, max);
        }

        #endregion
    }

    /// <summary>使用 Scene Camera pixelRect 与 Screen.safeArea 交集作为可见区域。</summary>
    public sealed class CameraSafeAreaVisibleRegionProvider : ISceneUIVisibleRegionProvider
    {
        #region 内部参数

        private readonly Camera _sceneCamera;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public CameraSafeAreaVisibleRegionProvider(Camera sceneCamera)
        {
            _sceneCamera = sceneCamera;
        }

        [NoGC]
        public bool TryGetVisibleRect(out Rect visibleRect)
        {
            if (!_sceneCamera)
            {
                visibleRect = default;
                return false;
            }

            visibleRect = SceneUIMath.Intersection(_sceneCamera.pixelRect, Screen.safeArea);
            return visibleRect.width > 0f && visibleRect.height > 0f;
        }

        #endregion
    }

    /// <summary>固定屏幕像素可见区域，适合自定义 HUD 裁剪和测试。</summary>
    public sealed class FixedSceneUIVisibleRegionProvider : ISceneUIVisibleRegionProvider
    {
        #region 内部参数

        private Rect _visibleRect;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public FixedSceneUIVisibleRegionProvider(Rect visibleRect)
        {
            _visibleRect = visibleRect;
        }

        [NoGC]
        public void SetVisibleRect(Rect visibleRect)
        {
            _visibleRect = visibleRect;
        }

        [NoGC]
        public bool TryGetVisibleRect(out Rect visibleRect)
        {
            visibleRect = _visibleRect;
            return visibleRect.width > 0f && visibleRect.height > 0f;
        }

        #endregion
    }
}
