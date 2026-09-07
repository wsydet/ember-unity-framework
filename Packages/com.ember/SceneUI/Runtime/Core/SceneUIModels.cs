// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>注册一个 SceneUI Context 所需的全部显式依赖。</summary>
    public struct SceneUIContextDescriptor
    {
        #region 编辑器面板参数

        public Camera SceneCamera;
        public Canvas Canvas;
        public Camera UICamera;
        public RectTransform LayerRoot;
        public ISceneUIVisibleRegionProvider VisibleRegionProvider;
        public ISceneUIViewHost ViewHost;
        public ISceneUICameraUpdateSource CameraUpdateSource;
        public bool Visible;
        public bool OwnsViewHost;
        public SceneUISortingMode SortingMode;
        public int PrewarmInstancesPerFlush;
        public ISceneUIOcclusionTester OcclusionTester;
        public int OcclusionTestsPerFlush;

        #endregion
    }

    /// <summary>注册一个场景锚点 UI 的空间、View 和生命周期描述。</summary>
    public struct SceneUIRequest
    {
        #region 编辑器面板参数

        public SceneUIContextHandle Context;
        public SceneUIViewKey ViewKey;
        public ISceneUIAnchor Anchor;
        public ISceneUIBinder Binder;
        public Vector3 WorldOffset;
        public Vector2 UiOffset;
        public SceneUIUpdatePolicy UpdatePolicy;
        public SceneUIVisibilityPolicy VisibilityPolicy;
        public SceneUIScalePolicy ScalePolicy;
        public SceneUIViewLifetimePolicy ViewLifetimePolicy;
        public float RecycleDelaySeconds;
        public int SortingPriority;
        public SceneUIViewMetrics ViewMetrics;
        public SceneUIOcclusionPolicy OcclusionPolicy;
        public bool BusinessVisible;

        #endregion
    }

    /// <summary>View 根矩形在 LayerRoot UI 单位下的尺寸和 Pivot。</summary>
    public readonly struct SceneUIViewMetrics
    {
        #region 内部参数

        public readonly Vector2 Size;
        public readonly Vector2 Pivot;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public bool IsValid => SceneUIMath.IsFinite(Size)
            && SceneUIMath.IsFinite(Pivot)
            && Size.x > 0f
            && Size.y > 0f;

        public SceneUIViewMetrics(Vector2 size, Vector2 pivot)
        {
            Size = size;
            Pivot = pivot;
        }

        public static SceneUIViewMetrics From(RectTransform rectTransform)
        {
            return rectTransform
                ? new SceneUIViewMetrics(rectTransform.rect.size, rectTransform.pivot)
                : default;
        }

        #endregion
    }

    /// <summary>世界坐标投影到屏幕后的纯数据结果。</summary>
    public readonly struct SceneUIProjectionResult
    {
        #region 内部参数

        public readonly bool IsValid;
        public readonly Vector3 ViewportPosition;
        public readonly Vector2 ScreenPosition;
        public readonly float Depth;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIProjectionResult(
            bool isValid,
            Vector3 viewportPosition,
            Vector2 screenPosition,
            float depth)
        {
            IsValid = isValid;
            ViewportPosition = viewportPosition;
            ScreenPosition = screenPosition;
            Depth = depth;
        }

        #endregion
    }

    /// <summary>可见性判定后的屏幕空间结果。</summary>
    public readonly struct SceneUIVisibilityResult
    {
        #region 内部参数

        public readonly bool Visible;
        public readonly bool IsOnScreen;
        public readonly Vector2 ScreenPosition;
        public readonly Vector2 OffscreenDirection;
        public readonly SceneUIInvisibleReason InvisibleReason;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIVisibilityResult(
            bool visible,
            bool isOnScreen,
            Vector2 screenPosition,
            Vector2 offscreenDirection,
            SceneUIInvisibleReason invisibleReason)
        {
            Visible = visible;
            IsOnScreen = isOnScreen;
            ScreenPosition = screenPosition;
            OffscreenDirection = offscreenDirection;
            InvisibleReason = invisibleReason;
        }

        #endregion
    }

    /// <summary>最终应用到 View 根节点的空间状态。</summary>
    public readonly struct SceneUISpatialState
    {
        #region 内部参数

        public readonly Vector2 AnchoredPosition;
        public readonly float Scale;
        public readonly float Depth;
        public readonly bool IsOnScreen;
        public readonly Vector2 OffscreenDirection;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUISpatialState(
            Vector2 anchoredPosition,
            float scale,
            float depth,
            bool isOnScreen,
            Vector2 offscreenDirection)
        {
            AnchoredPosition = anchoredPosition;
            Scale = scale;
            Depth = depth;
            IsOnScreen = isOnScreen;
            OffscreenDirection = offscreenDirection;
        }

        #endregion
    }

    /// <summary>SceneUI Engine 的累计诊断统计。</summary>
    public readonly struct SceneUIDiagnostics
    {
        #region 内部参数

        public readonly int ActiveContextCount;
        public readonly int ActiveEntryCount;
        public readonly int ActiveViewCount;
        public readonly int PooledViewCount;
        public readonly int PoolHitCount;
        public readonly int PoolMissCount;
        public readonly int PendingPrewarmCount;
        public readonly int PrewarmedViewCount;
        public readonly int PrewarmFailureCount;
        public readonly long ProjectedCount;
        public readonly long CulledCount;
        public readonly long InvalidAnchorCount;
        public readonly long BehindCameraCount;
        public readonly int LastFlushFrame;
        public readonly double LastFlushDurationMs;
        public readonly long LastFlushAllocatedBytes;
        public readonly int PendingDelayedRecycleCount;
        public readonly long RecycledAfterDelayCount;
        public readonly long SortingPassCount;
        public readonly long ProjectionInvalidCount;
        public readonly long BeforeNearClipCount;
        public readonly long BeyondFarClipCount;
        public readonly long OutOfBoundsCount;
        public readonly long ViewUnavailableCount;
        public readonly long OcclusionTestCount;
        public readonly long OcclusionHitCount;
        public readonly long OcclusionFailureCount;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIDiagnostics(
            int activeContextCount,
            int activeEntryCount,
            int activeViewCount,
            int pooledViewCount,
            int poolHitCount,
            int poolMissCount,
            int pendingPrewarmCount,
            int prewarmedViewCount,
            int prewarmFailureCount,
            long projectedCount,
            long culledCount,
            long invalidAnchorCount,
            long behindCameraCount,
            int lastFlushFrame,
            double lastFlushDurationMs,
            long lastFlushAllocatedBytes,
            int pendingDelayedRecycleCount,
            long recycledAfterDelayCount,
            long sortingPassCount,
            long projectionInvalidCount,
            long beforeNearClipCount,
            long beyondFarClipCount,
            long outOfBoundsCount,
            long viewUnavailableCount,
            long occlusionTestCount,
            long occlusionHitCount,
            long occlusionFailureCount)
        {
            ActiveContextCount = activeContextCount;
            ActiveEntryCount = activeEntryCount;
            ActiveViewCount = activeViewCount;
            PooledViewCount = pooledViewCount;
            PoolHitCount = poolHitCount;
            PoolMissCount = poolMissCount;
            PendingPrewarmCount = pendingPrewarmCount;
            PrewarmedViewCount = prewarmedViewCount;
            PrewarmFailureCount = prewarmFailureCount;
            ProjectedCount = projectedCount;
            CulledCount = culledCount;
            InvalidAnchorCount = invalidAnchorCount;
            BehindCameraCount = behindCameraCount;
            LastFlushFrame = lastFlushFrame;
            LastFlushDurationMs = lastFlushDurationMs;
            LastFlushAllocatedBytes = lastFlushAllocatedBytes;
            PendingDelayedRecycleCount = pendingDelayedRecycleCount;
            RecycledAfterDelayCount = recycledAfterDelayCount;
            SortingPassCount = sortingPassCount;
            ProjectionInvalidCount = projectionInvalidCount;
            BeforeNearClipCount = beforeNearClipCount;
            BeyondFarClipCount = beyondFarClipCount;
            OutOfBoundsCount = outOfBoundsCount;
            ViewUnavailableCount = viewUnavailableCount;
            OcclusionTestCount = occlusionTestCount;
            OcclusionHitCount = occlusionHitCount;
            OcclusionFailureCount = occlusionFailureCount;
        }

        #endregion
    }

    /// <summary>单个 Entry 当前状态的只读诊断快照。</summary>
    public readonly struct SceneUIEntryDiagnostics
    {
        #region 内部参数

        public readonly bool BusinessVisible;
        public readonly bool SpatialVisible;
        public readonly bool HasView;
        public readonly bool ViewVisible;
        public readonly bool PendingDelayedRecycle;
        public readonly SceneUIInvisibleReason InvisibleReason;
        public readonly Vector3 LastWorldPosition;
        public readonly SceneUISpatialState SpatialState;
        public readonly int SortingPriority;
        public readonly SceneUIOcclusionState OcclusionState;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIEntryDiagnostics(
            bool businessVisible,
            bool spatialVisible,
            bool hasView,
            bool viewVisible,
            bool pendingDelayedRecycle,
            SceneUIInvisibleReason invisibleReason,
            Vector3 lastWorldPosition,
            in SceneUISpatialState spatialState,
            int sortingPriority,
            SceneUIOcclusionState occlusionState)
        {
            BusinessVisible = businessVisible;
            SpatialVisible = spatialVisible;
            HasView = hasView;
            ViewVisible = viewVisible;
            PendingDelayedRecycle = pendingDelayedRecycle;
            InvisibleReason = invisibleReason;
            LastWorldPosition = lastWorldPosition;
            SpatialState = spatialState;
            SortingPriority = sortingPriority;
            OcclusionState = occlusionState;
        }

        #endregion
    }
}
